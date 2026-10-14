using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions.Persistence;
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Native platform acceptance: real window/backend, real PTY, routed input and rendering.</summary>
public static class DesktopSmoke
{
    public static async Task Run(MainWindow window, IClassicDesktopStyleApplicationLifetime lifetime, string reportDirectory, SettingsStore store)
    {
        Directory.CreateDirectory(reportDirectory);
        var checks = new List<string>();
        Exception? failure = null;
        double? startupMs = null, resizeMs = null;
        long? workingSetBytes = null;
        var allPanes = new List<TerminalPane>();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var terminal = Panes(window)[0].Terminal;
            allPanes.Add(Panes(window)[0]);
            // A PTY may be running before an interactive shell finishes setting
            // terminal attributes. Wait for the fixture's real prompt before typing.
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Contains("ASTER_READY>"));
            terminal.Focus();
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "echo __NATIVE_OK__" : "printf '%s%s\\n' '__NATIVE_' 'OK__'" });
            Enter(terminal);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Contains("__NATIVE_OK__"));
            startupMs = Program.Startup.Elapsed.TotalMilliseconds;
            using (var process = Process.GetCurrentProcess()) workingSetBytes = process.WorkingSet64;
            checks.Add("Native window, PTY and routed text/Enter → real shell output");
            var columns = terminal.Engine.Columns;
            var resizeWatch = Stopwatch.StartNew();
            window.Width = 850; window.Height = 570;
            await Until(() => terminal.Engine.Columns != columns);
            resizeMs = resizeWatch.Elapsed.TotalMilliseconds;
            checks.Add("Native layout resized terminal grid");
            // Reprint through the shell after reflow so the visible screenshot contains the proof.
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "cls & echo NATIVE_ACCEPTANCE & echo Real shell - resize - Unicode" : "printf '\\033[2J\\033[HNATIVE_ACCEPTANCE\\r\\nReal shell - resize - Unicode: hé界\\r\\n'" });
            Enter(terminal);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Contains("NATIVE_ACCEPTANCE"));
            await Task.Delay(300);
            terminal.InvalidateVisual();
            SaveImage(window, Path.Combine(reportDirectory, "dark.png"));
            window.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(300);
            if (terminal.Engine.Scheme.Background != 0xFFFAFAFCu) throw new InvalidOperationException("Light appearance did not reach the terminal engine.");
            SaveImage(window, Path.Combine(reportDirectory, "light.png"));
            checks.Add("Dark and light native render snapshots");
            Shortcut(window, "NewTab");
            await Until(() => window.FindControl<TabControl>("Tabs")!.ItemCount == 2 && Panes(window).Any(p => p.Terminal.IsRunning));
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "tabs.png"));
            Shortcut(window, "SplitRight");
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => p.Terminal.IsRunning));
            Shortcut(window, "SplitBelow");
            await Until(() => Panes(window).Length == 3 && Panes(window).All(p => p.Terminal.IsRunning));
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "splits.png"));
            allPanes.AddRange(Panes(window));
            Shortcut(window, "Find");
            await Task.Delay(200);
            SaveImage(window, Path.Combine(reportDirectory, "search.png"));
            checks.Add("Native shortcuts create tabs, nested panes and search");
            var preferences = new PreferencesWindow(store) { RequestedThemeVariant = ThemeVariant.Light };
            var dialog = preferences.ShowDialog(window);
            await Task.Delay(300);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences.png"));
            preferences.Close();
            await dialog;
            checks.Add("Native preferences render");
        }
        catch (Exception error)
        {
            failure = error;
            File.WriteAllText(Path.Combine(reportDirectory, "failure-buffer.txt"), string.Join("\n--- pane ---\n", Panes(window).Select(p => TerminalBufferExport.ToPlainText(p.Terminal.Engine.CreateSnapshot(true).Buffer))));
            SaveImage(window, Path.Combine(reportDirectory, "failure.png"));
        }
        finally
        {
            window.Close();
            try { await Until(() => !window.IsVisible && allPanes.All(p => !p.Terminal.IsRunning) && Panes(window).All(p => !p.Terminal.IsRunning)); checks.Add("Window/session cleanup completed"); }
            catch (Exception error) { failure ??= error; }
            File.WriteAllText(Path.Combine(reportDirectory, "native-smoke.json"), JsonSerializer.Serialize(new
            {
                passed = failure is null, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                timestamp = DateTimeOffset.UtcNow, startupToShellOutputMs = startupMs, nativeResizeToGridMs = resizeMs, initialWorkingSetBytes = workingSetBytes,
                measurementScope = "One native acceptance launch; startup includes sentinel command/polling. Resize includes dispatcher/polling. Not a cold-start or comparative benchmark.",
                checks, error = failure?.ToString()
            }, new JsonSerializerOptions { WriteIndented = true }));
            try { Directory.Delete(Path.GetDirectoryName(store.Path)!, true); } catch (IOException) { }
            lifetime.Shutdown(failure is null ? 0 : 1);
        }
    }
    private static TerminalPane[] Panes(Window window) => window.GetVisualDescendants().OfType<TerminalPane>().ToArray();
    private static void Enter(InputElement target) => target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, KeyModifiers = KeyModifiers.None });
    private static void Shortcut(Window window, string action)
    {
        var gesture = KeyGesture.Parse(ShortcutMap.Defaults()[action]);
        window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = gesture.Key, KeyModifiers = gesture.KeyModifiers });
    }
    private static async Task Until(Func<bool> predicate)
    {
        var watch = Stopwatch.StartNew();
        while (!predicate()) { if (watch.Elapsed > TimeSpan.FromSeconds(20)) throw new TimeoutException("Native desktop acceptance timed out."); await Task.Delay(25); }
    }
    private static void SaveImage(Window window, string file)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(file, PngBitmapEncoderOptions.Default);
    }
}
