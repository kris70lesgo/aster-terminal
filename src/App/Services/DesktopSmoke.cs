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
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Native platform acceptance: real window/backend, real PTY, routed input and rendering.</summary>
public static class DesktopSmoke
{
    public static async Task Run(MainWindow window, IClassicDesktopStyleApplicationLifetime lifetime, string reportDirectory)
    {
        Directory.CreateDirectory(reportDirectory);
        var checks = new List<string>();
        Exception? failure = null;
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var terminal = Panes(window)[0].Terminal;
            terminal.Focus();
            terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "echo __NATIVE_OK__" : "printf '%s%s\\n' '__NATIVE_' 'OK__'" });
            Enter(terminal);
            await Until(() => TerminalBufferExport.ToPlainText(terminal.Engine.CreateSnapshot().Buffer).Contains("__NATIVE_OK__"));
            checks.Add("Native window, PTY and routed text/Enter → real shell output");
            var columns = terminal.Engine.Columns;
            window.Width = 850; window.Height = 570;
            await Until(() => terminal.Engine.Columns != columns);
            checks.Add("Native layout resized terminal grid");
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "dark.png"));
            window.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(300);
            SaveImage(window, Path.Combine(reportDirectory, "light.png"));
            checks.Add("Dark and light native render snapshots");
        }
        catch (Exception error) { failure = error; }
        finally
        {
            window.Close();
            try { await Until(() => !window.IsVisible && Panes(window).All(p => !p.Terminal.IsRunning)); checks.Add("Window/session cleanup completed"); }
            catch (Exception error) { failure ??= error; }
            File.WriteAllText(Path.Combine(reportDirectory, "native-smoke.json"), JsonSerializer.Serialize(new
            {
                passed = failure is null, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                timestamp = DateTimeOffset.UtcNow, checks, error = failure?.ToString()
            }, new JsonSerializerOptions { WriteIndented = true }));
            lifetime.Shutdown(failure is null ? 0 : 1);
        }
    }
    private static TerminalPane[] Panes(Window window) => window.GetVisualDescendants().OfType<TerminalPane>().ToArray();
    private static void Enter(InputElement target) => target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, KeyModifiers = KeyModifiers.None });
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
