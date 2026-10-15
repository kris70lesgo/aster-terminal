using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Aster.Sessions.Scripting;
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Runs the original Python client against an isolated native app and owned loopback endpoint.</summary>
public static class ScriptingSmoke
{
    public static Window Configure(IClassicDesktopStyleApplicationLifetime desktop, string[] args)
    {
        string Option(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? Path.GetFullPath(args[i + 1]) : throw new ArgumentException(name + " is required."); }
        var report = Option("--scripting-report"); var python = Option("--scripting-python"); var launcher = Option("--scripting-launcher"); var script = Option("--scripting-script");
        Directory.CreateDirectory(report); desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var state = Path.Combine(Path.GetTempPath(), "aster-api-native-" + Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(Path.Combine(state, "settings.json"));
        var profile = new TerminalProfile("Python acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='API_READY> '; exec /bin/sh", Path.GetTempPath()) { EnvironmentVariables = new() { ["PROMPT"] = "API_READY$G" } };
        store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, RestoreLayout = false, ConfirmClose = false });
        var window = new MainWindow(store) { ShowActivated = false };
        window.Opened += async (_, _) =>
        {
            Exception? failure = null; var checks = new List<string>(); Process? client = null;
            using var target = new DesktopScriptingTarget(store);
            await using var server = new ScriptingServer(target, state, 0);
            var captured = false; var renderScaling = window.RenderScaling;
            var capture = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) =>
            {
                if (captured || !window.GetVisualDescendants().OfType<TerminalPane>().Any(p => TerminalBufferExport.ToPlainText(p.Terminal.Engine.CreateSnapshot().Buffer).Contains("__PYTHON_API__live", StringComparison.Ordinal))) return;
                renderScaling = window.RenderScaling; Image(window, Path.Combine(report, "python-panes.png")); captured = true;
            });
            try
            {
                await Until(() => window.ScriptingReady && window.ScriptingPanes.All(p => p.Terminal.IsRunning && TerminalBufferExport.ToPlainText(p.Terminal.Engine.CreateSnapshot().Buffer).Contains("API_READY>")));
                await server.StartAsync();
                var start = new ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                start.Environment["PYTHONUTF8"] = "1";
                foreach (var arg in new[] { launcher, "--endpoint", ScriptingEndpoint.FilePath(state), script, Path.Combine(report, "python-client.json") }) start.ArgumentList.Add(arg);
                client = Process.Start(start) ?? throw new IOException("Unable to start original Python client acceptance.");
                var stdout = client.StandardOutput.ReadToEndAsync(); var stderr = client.StandardError.ReadToEndAsync();
                await client.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
                if (client.ExitCode != 0) throw new IOException("Python client failed: " + await stdout + await stderr);
                using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(report, "python-client.json")));
                if (!result.RootElement.GetProperty("passed").GetBoolean()) throw new IOException("Python client did not pass.");
                if (!captured) throw new IOException("The native terminal never rendered the script's expanded Unicode output.");
                checks.Add("Original Python client controls actual native tabs/panes, sends real shell commands and reads Unicode screen contents");
                if (desktop.Windows.OfType<MainWindow>().Count(w => w.ScriptingReady) != 1 || window.ScriptingSnapshot().Tabs.Count != 1 || window.ScriptingPanes.Count() != 1) throw new IOException("Script-created panes were not cleaned up.");
                checks.Add("Only the initial real shell remains after script closes its created tab and split");
                await server.DisposeAsync();
                if (File.Exists(ScriptingEndpoint.FilePath(state))) throw new IOException("API credentials remained after stop.");
                checks.Add("API listener stops and removes private credentials");
            }
            catch (Exception error) { failure = error; if (window.IsVisible) Image(window, Path.Combine(report, "failure.png")); }
            finally
            {
                capture.Stop();
                if (client is not null) { if (!client.HasExited) client.Kill(); await client.WaitForExitAsync(); client.Dispose(); }
                await server.DisposeAsync(); target.Dispose();
                var windows = desktop.Windows.OfType<MainWindow>().ToArray();
                var panes = windows.SelectMany(w => w.ScriptingPanes).ToArray();
                foreach (var owned in windows) owned.Close();
                try { await Until(() => panes.All(p => !p.Terminal.IsRunning) && windows.All(w => !w.IsVisible)); checks.Add("Normal native window close cleans up remaining PTY"); }
                catch (Exception error) { failure ??= error; }
                File.WriteAllText(Path.Combine(report, "scripting.json"), JsonSerializer.Serialize(new { passed = failure is null, timestamp = DateTimeOffset.UtcNow, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, renderScaling, checks, error = failure?.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
                if (Directory.Exists(state)) try { Directory.Delete(state, true); } catch (IOException) { }
                desktop.Shutdown(failure is null ? 0 : 1);
            }
        };
        return window;
    }
    private static async Task Until(Func<bool> condition)
    { var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(20)) throw new TimeoutException("Native scripting acceptance timed out."); await Task.Delay(25); } }
    private static void Image(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)window.Bounds.Width), Math.Max(1, (int)window.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }
}
