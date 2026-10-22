using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Search;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    private static void FindCommand(TerminalPane pane, string command)
    {
        pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = command });
        pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
    }
    [AvaloniaFact]
    public async Task FindBarSearchesRealOutputWithOptionsNavigationErrorsAndPaneIsolation()
    {
        var profile = new TerminalProfile("Find acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q /K prompt FIND_READY$G" : "export PS1='FIND_READY> '; exec /bin/sh", Path.GetTempPath());
        var window = new MainWindow(profile);
        window.Show();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var pane = Panes(window)[0];
            await Until(() => Text(pane).Contains("FIND_READY>"), () => "shell prompt not ready: " + Text(pane));
            FindCommand(pane, OperatingSystem.IsWindows() ? "echo FIND_Alpha42&echo FIND_alpha7&echo FIND_alphabet9&echo FIND_alpha&echo FIND_東京é" : "printf '\\nFIND_%s\\n' 'Alpha42' 'alpha7' 'alphabet9' 'alpha' '東京é'");
            await Until(() => Text(pane).Contains("FIND_東京é"), () => "shell output not received: " + Text(pane));
            SendKey(window, Key.F, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            var box = window.FindControl<FindBar>("SearchBar")!.FindControl<TextBox>("SearchBox")!; var regex = window.FindControl<FindBar>("SearchBar")!.FindControl<ToggleButton>("RegexBox")!;
            var matchCase = window.FindControl<FindBar>("SearchBar")!.FindControl<ToggleButton>("MatchCaseBox")!; var smartCase = window.FindControl<FindBar>("SearchBar")!.FindControl<ToggleButton>("SmartCaseBox")!; var word = window.FindControl<FindBar>("SearchBar")!.FindControl<ToggleButton>("WholeWordBox")!;
            regex.IsChecked = true; box.Text = "^FIND_alpha[0-9]+$";
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 2,
                () => $"regex search did not return two matches (busy={pane.Find.Busy}, matches={pane.Find.Matches.Count}, status='{pane.Find.Status}'); buffer: {Text(pane)}");
            Assert.Equal(0, pane.Find.CurrentIndex); Assert.Equal("1 / 2", pane.Find.Status);
            SendKey(window, Key.Enter); await Until(() => pane.Find.CurrentIndex == 1);
            SendKey(window, Key.Enter); await Until(() => pane.Find.CurrentIndex == 0);
            SendKey(window, Key.Enter, RawInputModifiers.Shift); await Until(() => pane.Find.CurrentIndex == 1);
            matchCase.IsChecked = true; await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 1);
            box.Text = "^FIND_alpha"; word.IsChecked = true;
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 1);
            box.Text = "["; await Until(() => pane.Find.Status.StartsWith("Invalid regex"));
            box.Text = "^FIND_DOES_NOT_EXIST$"; box.Text = "^FIND_東京é$"; word.IsChecked = false;
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 1 && !pane.Find.Status.StartsWith("Invalid"));
            matchCase.IsChecked = false; regex.IsChecked = true; smartCase.IsChecked = true;
            box.Text = "^FIND_alpha[0-9]+$";
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 1);
            box.Text = "^find_alpha[0-9]+$";
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 2);
            smartCase.IsChecked = false; box.Text = "^FIND_alpha[0-9]+$";
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 2);
            box.Text = "^FIND_東京é$";
            await Until(() => !pane.Find.Busy && pane.Find.Matches.Count == 1);
            window.Width = 620; await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.True(box.Bounds.Width >= 99); Assert.True(window.FindControl<Border>("SearchBar")!.IsVisible);
            SendKey(window, Key.Escape); await Until(() => !window.FindControl<Border>("SearchBar")!.IsVisible);
            Assert.Empty(pane.Find.Matches); Assert.True(pane.Terminal.IsFocused);
            SendKey(window, Key.D, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => p.Terminal.IsRunning));
            var second = Panes(window).Single(p => p != pane);
            FindCommand(second, OperatingSystem.IsWindows() ? "echo FIND_東京é" : "printf '\\nFIND_%s\\n' '東京é'");
            await Until(() => Text(second).Contains("FIND_東京é"));
            SendKey(window, Key.F, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
            await Until(() => !second.Find.Busy && second.Find.Matches.Count == 1);
            Assert.Empty(pane.Find.Matches);
        }
        finally { window.Close(); await Until(() => !window.IsVisible); }
    }
    [AvaloniaFact]
    public async Task FindCancellationAndClosingCannotPublishStaleResultsOrWriteToTheShell()
    {
        var pane = new TerminalPane(new TerminalProfile("Find cancellation", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath()));
        var window = new Window { Content = pane }; window.Show();
        try
        {
            await pane.StartAsync(); pane.Find.Activate();
            var old = pane.Find.SearchAsync(new("OLD_QUERY"), debounce: true);
            var latest = pane.Find.SearchAsync(new("NEVER_EXECUTE_THIS_COMMAND"), debounce: true);
            await Task.WhenAll(old, latest);
            Assert.Empty(pane.Find.Matches); Assert.Equal("No matches", pane.Find.Status);
            Assert.DoesNotContain("NEVER_EXECUTE_THIS_COMMAND", Text(pane));
            var closing = pane.Find.SearchAsync(new("closing"), debounce: true);
            await pane.CloseAsync(); await closing;
            Assert.False(pane.Find.Busy); Assert.Empty(pane.Find.Matches); Assert.False(pane.Terminal.IsRunning);
        }
        finally { await pane.CloseAsync(); window.Close(); }
    }
}
