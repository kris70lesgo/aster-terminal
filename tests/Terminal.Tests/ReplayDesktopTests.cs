using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Aster.App.Views;
using Aster.Sessions;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task ReplaySeekIsIndependentOfTheLiveShellAndDiscardsOnClose()
    {
        var window = new MainWindow(new TerminalProfile("replay", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath()));
        window.Show(); ReplayWindow? viewer = null; TerminalPane? pane = null;
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            pane = Panes(window)[0];
            pane.Terminal.Focus();
            await Until(() => !string.IsNullOrWhiteSpace(Text(pane)));
            void Command(string text)
            {
                pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
                pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            }
            string Visible() => TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot().Buffer);
            Command(OperatingSystem.IsWindows() ? "cls & echo FIRST_RECORDED" : "printf '\\033[2J\\033[H%s%s\\r\\n' 'FIRST_' 'RECORDED'");
            await Until(() => Visible().StartsWith("FIRST_RECORDED")); pane.CaptureReplay();
            var firstIndex = pane.Replay.Snapshot().Count - 1;
            Command(OperatingSystem.IsWindows() ? "cls & echo SECOND_RECORDED" : "printf '\\033[2J\\033[H%s%s\\r\\n' 'SECOND_' 'RECORDED'");
            await Until(() => Visible().StartsWith("SECOND_RECORDED")); pane.CaptureReplay();
            // Output arrives while the replay window has focus, without typing into
            // a disabled modal owner or sending anything from the replay renderer.
            Command(OperatingSystem.IsWindows() ? "ping -n 3 127.0.0.1 >nul & echo __LIVE_AFTER_REPLAY__" : "sleep 1; printf '%s%s\\n' '__LIVE_' 'AFTER_REPLAY__'");
            viewer = new ReplayWindow(pane.Replay, pane.Profile); _ = viewer.ShowDialog(window); viewer.SelectFrame(firstIndex);
            SendKey(viewer, Key.Right); Assert.Equal(firstIndex + 1, viewer.SelectedFrameIndex);
            SendKey(viewer, Key.Left); Assert.Equal(firstIndex, viewer.SelectedFrameIndex);
            Assert.StartsWith("FIRST_RECORDED", TerminalBufferExport.ToPlainText(viewer.DisplayEngine.CreateSnapshot().Buffer));
            Assert.Contains("SECOND_RECORDED", Text(pane)); Assert.True(pane.Terminal.IsRunning);
            await Until(() => Visible().Split('\n').Any(line => line.Trim() == "__LIVE_AFTER_REPLAY__"));
            Assert.StartsWith("FIRST_RECORDED", TerminalBufferExport.ToPlainText(viewer.DisplayEngine.CreateSnapshot().Buffer));
            viewer.SelectFrame(viewer.FrameCount - 1);
            Assert.Contains("SECOND_RECORDED", TerminalBufferExport.ToPlainText(viewer.DisplayEngine.CreateSnapshot().Buffer));
            viewer.Close(); await Until(() => !viewer.IsVisible);
            pane.SetReplayEnabled(false); pane.CaptureReplay(); Assert.Empty(pane.Replay.Snapshot());
            pane.SetReplayEnabled(true); pane.CaptureReplay(); Assert.Single(pane.Replay.Snapshot());
            window.Close(); await Until(() => !window.IsVisible && !pane.Terminal.IsRunning);
            Assert.Empty(pane.Replay.Snapshot());
        }
        finally { viewer?.Close(); window.Close(); if (pane is not null) await Until(() => !pane.Terminal.IsRunning); }
    }
}
