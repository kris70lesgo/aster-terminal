using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Tmux;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task TmuxFindUsesRealOutputAndKeepsSearchIndependentAcrossPanesAndTabs()
    {
        if (OperatingSystem.IsWindows()) Assert.Skip("Local tmux control mode requires Unix.");
        await using var server = new TmuxTests.Server(); await server.Start();
        var window = new TmuxWindow(new TmuxController("acceptance", server.Socket), TerminalProfile.Default()); window.Show();
        try
        {
            await Until(() => window.IsReady && window.Panes.Count == 1);
            var first = window.Panes.Single().Value;
            await Until(() => Text(first).Contains("ASTER_READY>"));
            FindCommand(first, "printf '\\nTMUX_FIND_%s\\n' 'Alpha42' 'alpha7' 'alphabet9' 'alpha' '東京é'");
            await Until(() => Text(first).Contains("TMUX_FIND_東京é"));
            var bar = window.GetVisualDescendants().OfType<Border>().SingleOrDefault(b => b.Name == "SearchBar");
            Assert.NotNull(bar);
            SendKey(window, Key.F, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.True(bar.IsVisible);
            var box = bar.FindControl<TextBox>("SearchBox")!;
            var regex = bar.FindControl<ToggleButton>("RegexBox")!;
            var matchCase = bar.FindControl<ToggleButton>("MatchCaseBox")!;
            var smartCase = bar.FindControl<ToggleButton>("SmartCaseBox")!;
            var word = bar.FindControl<ToggleButton>("WholeWordBox")!;
            regex.IsChecked = true; box.Text = "^TMUX_FIND_alpha[0-9]+$";
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 2);
            Assert.StartsWith("1 / 2", first.Find.Status);
            SendKey(window, Key.Enter); await Until(() => first.Find.CurrentIndex == 1);
            SendKey(window, Key.Enter); await Until(() => first.Find.CurrentIndex == 0);
            SendKey(window, Key.Enter, RawInputModifiers.Shift); await Until(() => first.Find.CurrentIndex == 1);
            matchCase.IsChecked = true; await Until(() => !first.Find.Busy && first.Find.Matches.Count == 1);
            box.Text = "^TMUX_FIND_alpha"; word.IsChecked = true;
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 1);
            box.Text = "["; await Until(() => first.Find.Status.StartsWith("Invalid regex"));
            box.Text = "^NEVER_EXECUTE_THIS_COMMAND$"; box.Text = "^TMUX_FIND_東京é$"; word.IsChecked = false;
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 1);
            matchCase.IsChecked = false; regex.IsChecked = true; smartCase.IsChecked = true;
            box.Text = "^TMUX_FIND_alpha[0-9]+$";
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 1);
            box.Text = "^tmux_find_alpha[0-9]+$";
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 2);
            smartCase.IsChecked = false; box.Text = "^TMUX_FIND_alpha[0-9]+$";
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 2);
            box.Text = "^TMUX_FIND_東京é$";
            await Until(() => !first.Find.Busy && first.Find.Matches.Count == 1);
            Assert.DoesNotContain("NEVER_EXECUTE_THIS_COMMAND", Text(first));
            window.Width = 620; await window.SynchronizeSizeAsync();
            await Until(() => box.Bounds.Width >= 99);
            Assert.True(box.IsFocused);
            await window.RefreshAsync(); Assert.True(box.IsFocused);
            await window.SplitAsync(false); await Until(() => window.Panes.Count == 2);
            var second = window.Panes.Values.Single(p => p != first);
            FindCommand(second, "printf '\\nTMUX_FIND_%s\\n' '東京é'");
            await Until(() => Text(second).Contains("TMUX_FIND_東京é"));
            SendKey(window, Key.Enter);
            await Until(() => !second.Find.Busy && second.Find.Matches.Count == 1);
            Assert.Empty(first.Find.Matches);
            SendKey(window, Key.T, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            await Until(() => window.Panes.Count == 3);
            var third = window.Panes.Values.Single(p => p != first && p != second);
            // Pane discovery precedes async startup/tab selection. Wait for an
            // actual query on the new controller, not its initial empty state.
            await Until(() => !third.Find.Busy && third.Find.Status == "No matches");
            Assert.Empty(second.Find.Matches);
            SendKey(window, Key.Escape); Assert.False(bar.IsVisible); Assert.True(third.Terminal.IsFocused);
            Assert.Empty(third.Find.Matches);
        }
        finally { window.Close(); await Until(() => !window.IsVisible); }
    }

    [AvaloniaFact]
    public async Task TmuxPaneRemovalAndDetachCancelPendingFindWithoutEndingServerShells()
    {
        if (OperatingSystem.IsWindows()) Assert.Skip("Local tmux control mode requires Unix.");
        await using var server = new TmuxTests.Server(); await server.Start();
        var window = new TmuxWindow(new TmuxController("acceptance", server.Socket), TerminalProfile.Default()); window.Show();
        try
        {
            await Until(() => window.IsReady && window.Panes.Count == 1);
            await window.SplitAsync(false); await Until(() => window.Panes.Count == 2);
            SendKey(window, Key.F, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            var bar = window.GetVisualDescendants().OfType<Border>().SingleOrDefault(b => b.Name == "SearchBar");
            Assert.NotNull(bar);
            var active = (await server.Run("display-message", "-p", "#{pane_id}")).Trim();
            var removed = window.Panes[int.Parse(active[1..])];
            var pending = removed.Find.SearchAsync(new("pending"), debounce: true);
            await server.Run("kill-pane", "-t", active);
            await Until(() => window.Panes.Count == 1); await pending;
            Assert.Empty(removed.Find.Matches); Assert.False(removed.Find.Busy);
            var survivor = window.Panes.Values.Single();
            var closing = survivor.Find.SearchAsync(new("close pending"), debounce: true);
            window.Close(); await Until(() => !window.IsVisible); await closing;
            Assert.Empty(survivor.Find.Matches); Assert.False(survivor.Find.Busy);
            Assert.Contains("acceptance", await server.Run("list-sessions"));
        }
        finally { window.Close(); await Until(() => !window.IsVisible); }
    }
}
