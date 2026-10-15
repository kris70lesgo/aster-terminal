using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Hosting;
using Aster.Sessions.Persistence;
using Devolutions.Terminal.Connection;
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Isolated native crash/reopen fixture. No user settings or sessions are touched.</summary>
public static class RecoverySmoke
{
    private sealed record Before(Guid SessionId, TerminalProcessMetadata Process, string StateDirectory, string ShellPid);
    public static MainWindow Configure(IClassicDesktopStyleApplicationLifetime desktop, string[] args)
    {
        var index = Array.IndexOf(args, "--recovery-report");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("A recovery report directory is required.");
        var report = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(report);
        var resume = args.Contains("--recovery-resume");
        var state = resume ? JsonSerializer.Deserialize<Before>(File.ReadAllText(Path.Combine(report, "before.json")))!.StateDirectory
            : Path.Combine(Path.GetTempPath(), "aster-native-recovery-" + Guid.NewGuid().ToString("N"));
        if (!Path.GetFullPath(state).StartsWith(Path.Combine(Path.GetTempPath(), "aster-native-recovery-"), StringComparison.Ordinal)) throw new ArgumentException("Recovery fixture state must be isolated in its temporary directory.");
        var store = new SettingsStore(Path.Combine(state, "settings.json"));
        if (!resume)
        {
            var profile = new TerminalProfile("Live shell recovery", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
            { EnvironmentVariables = new() { ["PROMPT"] = "ASTER_READY$G", ["PS1"] = "ASTER_READY> " } };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, KeepSessionsAlive = true, RestoreLayout = false, ConfirmClose = false });
        }
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var window = new MainWindow(store);
        window.Opened += async (_, _) => await Run(window, desktop, state, report, resume);
        return window;
    }
    private static async Task Run(MainWindow window, IClassicDesktopStyleApplicationLifetime lifetime, string state, string report, bool resume)
    {
        Exception? failure = null;
        var checks = new List<string>();
        var renderScaling = window.RenderScaling;
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var pane = Panes(window)[0];
            await Until(() => Text(pane).Contains("ASTER_READY>")); pane.Terminal.Focus();
            if (!resume)
            {
                Command(pane, OperatingSystem.IsWindows() ? "set ASTER_RECOVERY_VALUE=retained" : "ASTER_RECOVERY_VALUE=retained");
                Command(pane, OperatingSystem.IsWindows() ? "echo __RECOVERY_BEFORE__%ASTER_RECOVERY_VALUE%" : "printf '__RECOVERY_BEFORE__%s__PID__%s\\n' \"$ASTER_RECOVERY_VALUE\" \"$$\"");
                await Until(() => Text(pane).Split('\n').Any(l => l.Trim().StartsWith("__RECOVERY_BEFORE__retained", StringComparison.Ordinal)));
                var line = Text(pane).Split('\n').First(l => l.Trim().StartsWith("__RECOVERY_BEFORE__retained", StringComparison.Ordinal)).Trim();
                var pid = OperatingSystem.IsWindows() ? pane.Terminal.ProcessMetadata!.ProcessId.ToString() : line.Split("__PID__")[1];
                File.WriteAllText(Path.Combine(report, "before.json"), JsonSerializer.Serialize(new Before(pane.HostedSession!.Id, pane.Terminal.ProcessMetadata!, state, pid), new JsonSerializerOptions { WriteIndented = true }));
                await Task.Delay(150); Image(window, Path.Combine(report, "before.png"));
                // Bypass Closing/Dispose like a lost UI process. The separate host
                // must retain the actual PTY; phase two proves this with a shell variable/PID.
                Environment.Exit(73);
            }
            var before = JsonSerializer.Deserialize<Before>(File.ReadAllText(Path.Combine(report, "before.json")))!;
            if (pane.HostedSession?.Id != before.SessionId || pane.Terminal.ProcessMetadata != before.Process) throw new InvalidOperationException("Reopen launched a replacement process instead of recovering the live shell.");
            Command(pane, OperatingSystem.IsWindows() ? "echo __RECOVERY_AFTER__%ASTER_RECOVERY_VALUE%" : "printf '__RECOVERY_AFTER__%s__PID__%s\\n' \"$ASTER_RECOVERY_VALUE\" \"$$\"");
            await Until(() => Text(pane).Split('\n').Any(l => l.Trim().StartsWith("__RECOVERY_AFTER__retained", StringComparison.Ordinal)));
            var after = Text(pane).Split('\n').First(l => l.Trim().StartsWith("__RECOVERY_AFTER__retained", StringComparison.Ordinal)).Trim();
            if (!OperatingSystem.IsWindows() && after.Split("__PID__")[1] != before.ShellPid) throw new InvalidOperationException("Recovered shell PID changed.");
            checks.Add("Abrupt native UI process exit and reopen recover the same host session/process metadata");
            checks.Add("Shell-local variable and actual Unix shell PID survive UI termination; fresh shells cannot satisfy this");
            var columns = pane.Terminal.Engine.Columns; window.Width = 820; window.Height = 520;
            await Until(() => pane.Terminal.Engine.Columns != columns && pane.IsHostGridSynchronized);
            if (!OperatingSystem.IsWindows())
            {
                Command(pane, "stty size");
                await Until(() => Text(pane).Contains($"{pane.Terminal.Engine.Rows} {pane.Terminal.Engine.Columns}"));
            }
            checks.Add("Recovered shell accepts routed input and native resize");
            await Task.Delay(200); Image(window, Path.Combine(report, "after.png"));
            window.Close(); await Until(() => !window.IsVisible && !pane.Terminal.IsRunning);
            await Until(() => !Directory.EnumerateFiles(Path.Combine(state, "sessions"), "*.json").Any());
            checks.Add("Normal window close terminates the recovered host/PTY and removes its descriptor");
        }
        catch (Exception error) { failure = error; Image(window, Path.Combine(report, "failure.png")); }
        finally
        {
            window.Close();
            var catalog = new SessionCatalog(Path.Combine(state, "sessions"));
            foreach (var path in Directory.EnumerateFiles(catalog.DirectoryPath, "*.json"))
                try { await catalog.TerminateAsync(SessionCatalog.Read(path)); } catch (Exception error) when (error is IOException or OperationCanceledException) { failure ??= error; }
            if (!Directory.EnumerateFiles(catalog.DirectoryPath, "*.json").Any()) try { Directory.Delete(state, true); } catch (IOException) { }
            File.WriteAllText(Path.Combine(report, "recovery.json"), JsonSerializer.Serialize(new { passed = failure is null, timestamp = DateTimeOffset.UtcNow, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, renderScaling, checks, error = failure?.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            lifetime.Shutdown(failure is null ? 0 : 1);
        }
    }
    private static TerminalPane[] Panes(Window window) => window.GetVisualDescendants().OfType<TerminalPane>().ToArray();
    private static string Text(TerminalPane pane) => TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot(true).Buffer);
    private static void Command(TerminalPane pane, string text)
    {
        pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
        pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
    }
    private static async Task Until(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Native recovery acceptance timed out."); await Task.Delay(25); }
    }
    private static void Image(Window window, string file)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)window.Bounds.Width), Math.Max(1, (int)window.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(file, PngBitmapEncoderOptions.Default);
    }
}
