using System.Diagnostics;
using System.Text.Json;
using System.Text;
using SkiaSharp;
using Aster.Sessions.Protocols;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
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
        var renderScaling = window.RenderScaling;
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
            await Until(() => terminal.Engine.Columns != columns && Panes(window)[0].IsHostGridSynchronized);
            resizeMs = resizeWatch.Elapsed.TotalMilliseconds;
            checks.Add("Native layout resized terminal grid");
            // Reprint through the shell after reflow so the visible screenshot contains the proof.
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "cls & echo NATIVE_ACCEPTANCE & echo Real shell - resize - Unicode" : "printf '\\033[2J\\033[HNATIVE_ACCEPTANCE\\r\\nReal shell - resize - Unicode: hé界\\r\\n'" });
            Enter(terminal);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Contains("NATIVE_ACCEPTANCE"));
            await Task.Delay(300);
            terminal.InvalidateVisual();
            SaveImage(window, Path.Combine(reportDirectory, "dark.png"));
            var pane = Panes(window)[0]; pane.CaptureReplay();
            var oldFrame = pane.Replay.Snapshot().Count - 1;
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "cls & echo CURRENT_SCREEN" : "printf '\\033[2J\\033[H%s%s\\r\\n' 'CURRENT_' 'SCREEN'" });
            Enter(terminal);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).StartsWith("CURRENT_SCREEN")); pane.CaptureReplay();
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "ping -n 3 127.0.0.1 >nul & echo __REPLAY_LIVE__" : "sleep 1; printf '%s%s\\n' '__REPLAY_' 'LIVE__'" });
            Enter(terminal);
            var replay = new ReplayWindow(pane.Replay, pane.Profile);
            var replayDialog = replay.ShowDialog(window); replay.SelectFrame(oldFrame);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Split('\n').Any(line => line.Trim() == "__REPLAY_LIVE__"));
            if (!TerminalBufferExport.ToPlainText(replay.DisplayEngine.CreateSnapshot().Buffer).Contains("NATIVE_ACCEPTANCE")) throw new InvalidOperationException("Replay changed while the live shell produced output.");
            var historicalColumns = replay.DisplayEngine.Columns; var historicalRows = replay.DisplayEngine.Rows;
            replay.Width = 620; replay.Height = 360; await Task.Delay(150);
            if (replay.DisplayEngine.Columns != historicalColumns || replay.DisplayEngine.Rows != historicalRows || !TerminalBufferExport.ToPlainText(replay.DisplayEngine.CreateSnapshot().Buffer).Contains("NATIVE_ACCEPTANCE")) throw new InvalidOperationException("Viewer layout reflowed the historical frame.");
            replay.Width = 920; replay.Height = 620; await Task.Delay(150);
            await Task.Delay(200); SaveImage(replay, Path.Combine(reportDirectory, "replay.png"));
            replay.SelectFrame(replay.FrameCount - 1);
            if (!TerminalBufferExport.ToPlainText(replay.DisplayEngine.CreateSnapshot().Buffer).Contains("CURRENT_SCREEN")) throw new InvalidOperationException("Replay seek failed.");
            replay.Close(); await replayDialog;
            checks.Add("Native screen replay seeks historical text while the real shell continues; independent read-only renderer");
            checks.Add("Historical grid survives viewer resize without reflow");
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "echo TRIGGERED_OUTPUT" : "printf '%s%s\\n' 'TRIGGERED_' 'OUTPUT'" });
            Enter(terminal);
            var triggerEngine = (ItermTerminalEngine)terminal.Engine;
            await Until(() => triggerEngine.TriggerMatches.Count == 1);
            if (triggerEngine.TriggerMatches[0].Name != "Build errors") throw new InvalidOperationException("Profile output rule did not fire.");
            SaveImage(window, Path.Combine(reportDirectory, "triggers.png"));
            checks.Add("Profile output rule matches real shell output and exposes per-pane history");
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = GraphicsCommand() });
            Enter(terminal);
            await Until(() => terminal.Engine.CreateSnapshot().Images.Count == 3);
            await Task.Delay(300);
            var graphicsFile = Path.Combine(reportDirectory, "graphics.png");
            SaveImage(window, graphicsFile);
            using (var rendered = SKBitmap.Decode(graphicsFile))
            {
                var red = 0; var green = 0; var blue = 0;
                foreach (var color in rendered.Pixels)
                {
                    if (color.Red > 200 && color.Green < 40 && color.Blue < 40) red++;
                    if (color.Green > 200 && color.Red < 40 && color.Blue < 40) green++;
                    if (color.Blue > 200 && color.Red < 40 && color.Green < 40) blue++;
                }
                if (red < 16 || green < 16 || blue < 16) throw new InvalidOperationException($"Native inline image pixels missing: red={red}, green={green}, blue={blue}.");
            }
            checks.Add("Real shell sixel / OSC1337 PNG / kitty RGBA render red / green / blue native pixels");
            if (OperatingSystem.IsMacOS())
            {
                if (window.FindControl<Menu>("MainMenu")!.IsVisible || NativeMenu.GetMenu(window)?.Items.Count != 5) throw new InvalidOperationException("macOS native menu export configuration missing.");
                checks.Add("macOS native menu tree configured; in-window menu hidden");
            }
            window.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(300);
            if (terminal.Engine.Scheme.Background != 0xFFFAFAFCu) throw new InvalidOperationException("Light appearance did not reach the terminal engine.");
            SaveImage(window, Path.Combine(reportDirectory, "light.png"));
            checks.Add("Dark and light native render snapshots");
            Shortcut(window, "NewTab");
            await Until(() => window.FindControl<TabControl>("Tabs")!.ItemCount == 2 && Panes(window).Any(p => p.Terminal.IsRunning));
            if (OperatingSystem.IsMacOS() && NativeMenu.GetMenu(Application.Current!)?.Items.OfType<NativeMenuItem>().Any(item => item.Header == "Quit") != true) throw new InvalidOperationException("Standard macOS application commands were removed.");
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "tabs.png"));
            Shortcut(window, "SplitRight");
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => p.Terminal.IsRunning));
            Shortcut(window, "SplitBelow");
            await Until(() => Panes(window).Length == 3 && Panes(window).All(p => p.Terminal.IsRunning));
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "splits.png"));
            allPanes.AddRange(Panes(window));
            var findPane = Panes(window).Last();
            await Until(() => TerminalBufferExport.ToPlainText(findPane.Terminal.Engine.CreateSnapshot().Buffer).Contains("ASTER_READY>"));
            findPane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ?
                "echo FIND_Alpha42&echo FIND_alpha7&echo FIND_alphabet9&echo FIND_alpha&echo FIND_東京é" :
                "printf '\\nFIND_%s\\n' 'Alpha42' 'alpha7' 'alphabet9' 'alpha' '東京é'" });
            Enter(findPane.Terminal);
            await Until(() => TerminalBufferExport.ToPlainText(findPane.Terminal.Engine.CreateSnapshot().Buffer).Contains("FIND_東京é"));
            Shortcut(window, "Find");
            var findBox = window.FindControl<TextBox>("SearchBox")!;
            var regexBox = window.FindControl<ToggleButton>("RegexBox")!;
            var caseBox = window.FindControl<ToggleButton>("MatchCaseBox")!;
            var wordBox = window.FindControl<ToggleButton>("WholeWordBox")!;
            regexBox.IsChecked = true; findBox.Text = "^FIND_alpha[0-9]+$";
            await Until(() => !findPane.Find.Busy && findPane.Find.Matches.Count == 2);
            findBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Until(() => findPane.Find.CurrentIndex == 1);
            caseBox.IsChecked = true; await Until(() => !findPane.Find.Busy && findPane.Find.Matches.Count == 1);
            findBox.Text = "^FIND_alpha"; wordBox.IsChecked = true;
            await Until(() => !findPane.Find.Busy && findPane.Find.Matches.Count == 1);
            findBox.Text = "["; await Until(() => findPane.Find.Status.StartsWith("Invalid regex", StringComparison.Ordinal));
            findBox.Text = "^FIND_東京é$"; wordBox.IsChecked = false;
            await Until(() => !findPane.Find.Busy && findPane.Find.Matches.Count == 1);
            window.Width = 620; await Task.Delay(200);
            if (findBox.Bounds.Width < 99) throw new InvalidOperationException("Compact Find squeezed the text field below its usable width.");
            SaveImage(window, Path.Combine(reportDirectory, "find-compact.png"));
            window.Width = 850; await Task.Delay(200);
            SaveImage(window, Path.Combine(reportDirectory, "search.png"));
            checks.Add("Native Find searches real shell Unicode with regex/case/whole-word, navigates, rejects malformed patterns and fits a compact window");
            checks.Add("Native shortcuts create tabs, nested panes and search");
            var preferences = new PreferencesWindow(store) { RequestedThemeVariant = ThemeVariant.Light };
            var dialog = preferences.ShowDialog(window);
            await Task.Delay(300);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences.png"));
            preferences.FindControl<TabControl>("ProfileTabs")!.SelectedIndex = 1; await Task.Delay(150);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences-text.png"));
            preferences.FindControl<TabControl>("ProfileTabs")!.SelectedIndex = 2; await Task.Delay(150);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences-colours.png"));
            preferences.FindControl<TabControl>("ProfileTabs")!.SelectedIndex = 3; await Task.Delay(150);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences-triggers.png"));
            preferences.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 0; await Task.Delay(150);
            SaveImage(preferences, Path.Combine(reportDirectory, "preferences-general.png"));
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
                timestamp = DateTimeOffset.UtcNow, renderScaling, startupToShellOutputMs = startupMs, nativeResizeToGridMs = resizeMs, initialWorkingSetBytes = workingSetBytes,
                measurementScope = "One native acceptance launch; startup includes sentinel command/polling. Resize includes dispatcher/polling. Not a cold-start or comparative benchmark.",
                checks, error = failure?.ToString()
            }, new JsonSerializerOptions { WriteIndented = true }));
            try { Directory.Delete(Path.GetDirectoryName(store.Path)!, true); } catch (IOException) { }
            lifetime.Shutdown(failure is null ? 0 : 1);
        }
    }
    private static string GraphicsCommand()
    {
        using var source = new SKBitmap(4, 4); source.Erase(SKColors.Lime);
        using var png = source.Encode(SKEncodedImageFormat.Png, 100);
        var blue = Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 16).SelectMany(b => b).ToArray();
        var stream = "\u001b[2J\u001b[HINLINE GRAPHICS: sixel / iTerm2 / kitty" +
            "\u001b[3;3H\u001bPq#1;2;100;0;0#1!24~-!24~-!24~-!24~\u001b\\" +
            $"\u001b[3;12H\u001b]1337;File=inline=1;width=24px;height=24px:{Convert.ToBase64String(png.ToArray())}\u0007" +
            $"\u001b[3;22H\u001b_Ga=T,f=32,s=4,v=4,c=4,r=2,i=42,q=2,C=1;{Convert.ToBase64String(blue)}\u001b\\" +
            "\u001b[7;1HGRAPHICS_COMPLETE\r\n";
        if (OperatingSystem.IsWindows()) return "powershell.exe -NoProfile -NonInteractive -Command \"[Console]::Write([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + Convert.ToBase64String(Encoding.UTF8.GetBytes(stream)) + "')))\"";
        return "printf '" + string.Concat(Encoding.UTF8.GetBytes(stream).Select(b => "\\" + Convert.ToString(b, 8).PadLeft(3, '0'))) + "'";
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
