using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Aster.Sessions.Protocols;
using Aster.Sessions.Triggers;

namespace Aster.App.Views;

public sealed class TriggerHistoryWindow : Window
{
    public TriggerHistoryWindow(ItermTerminalEngine engine, Action focusTerminal)
    {
        Title = "Triggered lines"; Width = 700; Height = 420; MinWidth = 440; MinHeight = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var matches = engine.TriggerMatches.Reverse().ToArray();
        var list = new ListBox { ItemsSource = matches.Select(m => $"{m.Name} · {m.Text}").ToArray(), SelectedIndex = matches.Length > 0 ? 0 : -1 };
        var message = new TextBlock { Text = engine.TriggerWarning ?? "Last 200 matching lines in this pane. Choose a line to return to its scrollback.", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var go = new Button { Content = "Go to line", IsDefault = true }; var clear = new Button { Content = "Clear history" }; var done = new Button { Content = "Done", IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 }; buttons.Children.Add(clear); buttons.Children.Add(done); buttons.Children.Add(go);
        var grid = new Grid { Margin = new Thickness(16), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 12 }; grid.Children.Add(message); Grid.SetRow(list, 1); grid.Children.Add(list); Grid.SetRow(buttons, 2); grid.Children.Add(buttons); Content = WindowCaption.Wrap(this, grid);
        void Navigate()
        {
            if (list.SelectedIndex < 0) return;
            if (!engine.NavigateToTrigger(matches[list.SelectedIndex])) { message.Text = "This line has left the retained scrollback."; return; }
            Close(); focusTerminal();
        }
        go.Click += (_, _) => Navigate(); list.DoubleTapped += (_, _) => Navigate(); done.Click += (_, _) => Close();
        clear.Click += (_, _) => { engine.ClearTriggerMatches(); Close(); };
    }
}
