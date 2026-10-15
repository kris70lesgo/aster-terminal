using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Tmux;
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Native GUI acceptance with an isolated tmux server; never touches existing servers.</summary>
public static class TmuxSmoke
{
    public static Window Configure(IClassicDesktopStyleApplicationLifetime desktop, string[] args)
    {
        var index = Array.IndexOf(args, "--tmux-report"); if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("A tmux report directory is required.");
        var report = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(report);
        var socket = "aster-native-tmux-" + Guid.NewGuid().ToString("N");
        var executable = OperatingSystem.IsMacOS() ? new[] { "/opt/homebrew/bin/tmux", "/usr/local/bin/tmux" }.FirstOrDefault(File.Exists) ?? "tmux" : "tmux";
        Task<string> Run(params string[] commands) => RunTmux(executable, socket, commands);
        Run("-f", "/dev/null", "new-session", "-d", "-s", "acceptance", "-x", "80", "-y", "24", "env PS1='ASTER_READY> ' /bin/sh").GetAwaiter().GetResult();
        Run("set-option", "-t", "acceptance", "default-shell", "/bin/sh").GetAwaiter().GetResult();
        Run("set-option", "-t", "acceptance", "default-command", "env PS1='ASTER_READY> ' /bin/sh").GetAwaiter().GetResult();
        var window = new TmuxWindow(new TmuxController("acceptance", socket, executable), TerminalProfile.Default());
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window.Opened += async (_, _) =>
        {
            Exception? failure = null; var checks = new List<string>(); TmuxWindow? recovered = null; var scale = window.RenderScaling; string? expectedSize = null;
            try
            {
                await Until(() => window.IsReady && window.Panes.Count == 1);
                var id = window.Panes.Keys.Single(); var first = window.Panes[id]; await Until(() => Text(first).Contains("ASTER_READY>"));
                Command(first, "ASTER_NATIVE_TMUX=retained; printf '__TMUX_NATIVE__%s\\n' \"$ASTER_NATIVE_TMUX\"");
                await Until(() => Text(first).Split('\n').Any(l => l.Trim() == "__TMUX_NATIVE__retained"));
                await window.SplitAsync(false); await Until(() => window.Panes.Count == 2 && window.Panes.Values.All(p => p.Terminal.IsRunning));
                var second = window.Panes.Single(p => p.Key != id).Value;
                Command(second, "printf '__TMUX_SECOND__%s\\n' 'λ界'"); await Until(() => Text(second).Contains("__TMUX_SECOND__λ界"));
                if (Text(first).Contains("__TMUX_SECOND__λ界")) throw new InvalidOperationException("tmux pane output crossed connections.");
                await window.SplitAsync(true); await Until(() => window.Panes.Count == 3);
                window.Width = 850; window.Height = 540; await Task.Delay(250); await window.SynchronizeSizeAsync(); await Task.Delay(100);
                expectedSize = (await Run("display-message", "-p", "-t", "%" + id, "#{pane_height} #{pane_width}")).Trim();
                Command(first, "stty size"); await Until(() => Text(first).Split('\n').Any(l => l.Trim() == expectedSize));
                await Task.Delay(150); Image(window, Path.Combine(report, "tmux-splits.png"));
                checks.Add("Native Avalonia panes mirror actual tmux nested splits; independent routed input and Unicode output");
                checks.Add("Native window resize reaches tmux PTYs and stty reports matching dimensions");
                foreach (var pair in window.Panes)
                {
                    var dimensions = (await Run("display-message", "-p", "-t", "%" + pair.Key, "#{pane_width} #{pane_height}")).Trim().Split(' ').Select(int.Parse).ToArray();
                    if (pair.Value.Terminal.Engine.Columns != dimensions[0] || pair.Value.Terminal.Engine.Rows != dimensions[1]) throw new InvalidOperationException("Native pane grid differs from tmux's actual PTY grid.");
                    var cell = Devolutions.Terminal.TermControl.MeasureCell(pair.Value.Profile.ToControlProfile(), window.RenderScaling);
                    if (pair.Value.Terminal.Bounds.Width + .5 < dimensions[0] * cell.Width + 10 || pair.Value.Terminal.Bounds.Height + .5 < dimensions[1] * cell.Height + 10) throw new InvalidOperationException($"Native pane %{pair.Key} clips its tmux grid: {pair.Value.Terminal.Bounds.Width}x{pair.Value.Terminal.Bounds.Height}; needs {dimensions[0] * cell.Width + 10}x{dimensions[1] * cell.Height + 10} ({dimensions[0]}x{dimensions[1]} at {cell.Width}x{cell.Height}).");
                }
                checks.Add("Every native pane uses tmux's exact PTY grid and fits its cells without clipping");
                window.Close(); await Until(() => !window.IsVisible && window.Panes.Values.All(p => !p.Terminal.IsRunning));
                recovered = new TmuxWindow(new TmuxController("acceptance", socket, executable), TerminalProfile.Default()); recovered.Show();
                await Until(() => recovered.IsReady && recovered.Panes.Count == 3);
                Command(recovered.Panes[id], "printf '__TMUX_REATTACH__%s\\n' \"$ASTER_NATIVE_TMUX\"");
                await Until(() => Text(recovered.Panes[id]).Split('\n').Any(l => l.Trim() == "__TMUX_REATTACH__retained"));
                await Task.Delay(150); Image(recovered, Path.Combine(report, "tmux-reattach.png"));
                checks.Add("Closing the native control window detaches; a new native window recovers the same live shell variable");
            }
            catch (Exception error) { failure = error; if ((recovered ?? window).IsVisible) Image(recovered ?? window, Path.Combine(report, "failure.png")); }
            finally
            {
                recovered?.Close(); window.Close();
                try { await Run("kill-server"); } catch (IOException error) { failure ??= error; }
                if (recovered is not null) await Until(() => !recovered.IsVisible); await Until(() => !window.IsVisible);
                File.WriteAllText(Path.Combine(report, "tmux.json"), JsonSerializer.Serialize(new { passed = failure is null, timestamp = DateTimeOffset.UtcNow, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, renderScaling = scale, checks, expectedSize, error = failure?.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
                desktop.Shutdown(failure is null ? 0 : 1);
            }
        };
        return window;
    }
    private static async Task<string> RunTmux(string executable, string socket, string[] args)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-L"); start.ArgumentList.Add(socket); foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment.Remove("TMUX");
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); if (process.ExitCode != 0) throw new IOException("tmux fixture: " + await error.ConfigureAwait(false)); return await output.ConfigureAwait(false);
    }
    private static string Text(TerminalPane pane) => TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot(true).Buffer);
    private static void Command(TerminalPane pane, string text)
    {
        pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
        pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
    }
    private static async Task Until(Func<bool> condition)
    { var clock = Stopwatch.StartNew(); while (!condition()) { if (clock.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Native tmux acceptance timed out."); await Task.Delay(25); } }
    private static void Image(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)window.Bounds.Width), Math.Max(1, (int)window.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }
}
