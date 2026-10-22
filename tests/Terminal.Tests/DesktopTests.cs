using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Devolutions.Terminal.Core;

[assembly: AvaloniaTestApplication(typeof(Aster.Tests.DesktopTestApplication))]

namespace Aster.Tests;

public static class DesktopTestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Aster.App.App>()
        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task HyperlinksRequireUserActionAndBlockUnsafeSchemes()
    {
        var pane = new TerminalPane(new TerminalProfile("links", "unused", Path.GetTempPath()));
        var window = new Window { Content = pane }; window.Show();
        try
        {
            var requests = 0;
            pane.Terminal.HyperlinkOpenRequested += (_, _) => requests++;
            pane.Terminal.Engine.Feed("\u001b]8;;https://iterm2.com\u001b\\web\u001b]8;;\u001b\\ \u001b]8;;file:///tmp/test\u001b\\file\u001b]8;;\u001b\\");
            Assert.Equal(0, requests);
            Assert.True(pane.Terminal.HitTestHyperlink(0, 0)!.CanOpen);
            var unsafeLink = pane.Terminal.HitTestHyperlink(4, 0)!;
            Assert.False(unsafeLink.CanOpen);
            await Assert.ThrowsAsync<InvalidOperationException>(() => pane.Terminal.OpenHyperlinkAsync(unsafeLink));
        }
        finally { window.Close(); await pane.CloseAsync(); }
    }

    private static void SendKey(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
    }
    private static async Task Until(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Desktop acceptance condition not met.");
            await Task.Delay(25);
        }
    }
    private static async Task Until(Func<bool> condition, Func<string> timeoutDetails)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Desktop acceptance condition not met: " + timeoutDetails());
            await Task.Delay(25);
        }
    }
    private static string Text(TerminalPane pane) => TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot(true).Buffer);
    private static TerminalPane[] Panes(Window window) => window.GetVisualDescendants().OfType<TerminalPane>().ToArray();

    [AvaloniaFact]
    public async Task KeyboardToRealShellClipboardSearchResizeTabsSplitsAndShutdown()
    {
        var unix = !OperatingSystem.IsWindows();
        var window = new MainWindow(new TerminalProfile("acceptance", unix ? "exec /bin/sh" : "cmd.exe /Q", Path.GetTempPath()));
        window.Show();
        var all = new List<TerminalPane>();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var first = Panes(window)[0]; all.Add(first);
            first.Terminal.Focus();
            window.KeyTextInput(unix ? "printf '%s%s\\n' '__GUI_' 'OK__'" : "echo __GUI_OK__");
            SendKey(window, Key.Enter);
            if (unix) await Until(() => Text(first).Contains("__GUI_OK__"));
            else await Until(() => Text(first).Contains("OK__"));

            // Headless clipboard belongs to the test platform, never the user's desktop clipboard.
            first.Terminal.SelectAll();
            await first.Terminal.CopyAsync();
            var copied = await window.Clipboard!.TryGetTextAsync();
            Assert.Contains(unix ? "__GUI_OK__" : "OK__", copied);
            first.Terminal.ClearSelection();
            await window.Clipboard!.SetTextAsync(unix ? "printf '%s%s\\n' '__PASTE_' 'OK__'" : "echo __PASTE_OK__");
            await first.Terminal.PasteAsync();
            SendKey(window, Key.Enter);
            await Until(() => Text(first).Contains("__PASTE_OK__"));
            Assert.True(first.Terminal.Find("__PASTE_OK__"));

            var oldColumns = first.Terminal.Engine.Columns;
            window.Width = 740; window.Height = 480;
            await Until(() => first.Terminal.Engine.Columns != oldColumns);
            if (unix)
            {
                await Task.Delay(250); // dependency's resize timer must deliver TIOCSWINSZ
                window.KeyTextInput("stty size"); SendKey(window, Key.Enter);
                var size = $"{first.Terminal.Engine.Rows} {first.Terminal.Engine.Columns}";
                await Until(() => Text(first).Contains(size));
            }
            var command = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift;
            SendKey(window, Key.D, command);
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => p.Terminal.IsRunning));
            all.Add(Panes(window).Single(p => p != first));
            Assert.Contains("__PASTE_OK__", Text(first));
            SendKey(window, Key.T, command);
            await Until(() => window.FindControl<TabControl>("Tabs")!.Items.Count == 2 && Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            all.Add(Panes(window)[0]);
            Assert.Equal(3, all.Select(p => p.Terminal.ProcessMetadata!.SessionId).Distinct().Count());
            SendKey(window, Key.Tab, RawInputModifiers.Control);
            await Until(() => Panes(window).Length == 2);
            SendKey(window, Key.W, command);
            await Until(() => Panes(window).Length == 1);
            Assert.Single(all, p => !p.Terminal.IsRunning);
        }
        finally
        {
            window.Close();
            await Until(() => !window.IsVisible && all.All(p => !p.Terminal.IsRunning));
        }
    }
}
