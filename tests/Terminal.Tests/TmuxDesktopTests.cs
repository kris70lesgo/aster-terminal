using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Tmux;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task NativeTmuxControlsFollowRealSplitsRouteInputAndDetachWithoutKillingShells()
    {
        if (OperatingSystem.IsWindows()) Assert.Skip("Local tmux control mode requires Unix.");
        await using var server = new TmuxTests.Server(); await server.Start();
        var window = new TmuxWindow(new TmuxController("acceptance", server.Socket), TerminalProfile.Default()); window.Show();
        try
        {
            await Until(() => window.IsReady && window.Panes.Count == 1);
            var first = window.Panes.Single().Value; await Until(() => Text(first).Contains("ASTER_READY>"));
            first.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = "ASTER_NATIVE_TMUX=alive; printf '__NATIVE_TMUX__%s\\n' \"$ASTER_NATIVE_TMUX\"" });
            first.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Until(() => Text(first).Split('\n').Any(l => l.Trim() == "__NATIVE_TMUX__alive"));
            await window.SplitAsync(false); await Until(() => window.Panes.Count == 2 && window.Panes.Values.All(p => p.Terminal.IsRunning));
            Assert.Contains(first, window.Panes.Values);
            await window.SplitAsync(true); await Until(() => window.Panes.Count == 3);
            window.Width = 800; window.Height = 500;
            await Until(() => first.Terminal.Engine.Columns < 80);
            window.Close(); await Until(() => !window.IsVisible && window.Panes.Values.All(p => !p.Terminal.IsRunning));
            Assert.Contains("acceptance", await server.Run("list-sessions"));
            Assert.Equal("3", (await server.Run("list-panes", "-F", "#{pane_id}")).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ToString());
        }
        finally { window.Close(); await Until(() => !window.IsVisible); }
    }
}
