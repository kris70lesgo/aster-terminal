using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Aster.App.Services;

namespace Aster.App.Views;

public static class DialogWindow
{
    public static async Task<string?> Prompt(Window owner, string title, string initial)
    {
        var dialog = new Window { RequestedThemeVariant = owner.ActualThemeVariant, Title = title, Width = 440, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var text = new TextBox { Text = initial }; var save = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Right };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 }; panel.Children.Add(text); panel.Children.Add(save); dialog.Content = WindowCaption.Wrap(dialog, panel);
        save.Click += (_, _) => dialog.Close(text.Text);
        text.KeyDown += (_, e) => { if (e.Key == Avalonia.Input.Key.Enter) dialog.Close(text.Text); if (e.Key == Avalonia.Input.Key.Escape) dialog.Close(); };
        dialog.Opened += (_, _) => { text.Focus(); text.SelectAll(); };
        return await dialog.ShowDialog<string?>(owner);
    }
    public static async Task<int?> Choose(Window owner, string title, IReadOnlyList<string> choices)
    {
        var dialog = new Window { RequestedThemeVariant = owner.ActualThemeVariant, Title = title, Width = 600, Height = 420, MinWidth = 420, MinHeight = 280, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var query = new TextBox { Name = "QuickSearchBox", PlaceholderText = "Search sessions and profiles", MinHeight = 34, Padding = new Thickness(10, 6), FontSize = 14 };
        var list = new ListBox { Name = "QuickSearchResults", Margin = new Thickness(0, 10, 0, 0), SelectionMode = SelectionMode.Single };
        var empty = new TextBlock { Text = "No matching sessions or profiles", IsVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = .65 };
        var count = new TextBlock { FontSize = 11, Opacity = .65, VerticalAlignment = VerticalAlignment.Center };
        var indices = new List<int>();
        void Update()
        {
            indices = Enumerable.Range(0, choices.Count)
                .Select(i => (Index: i, Score: QuickSearch.Score(query.Text ?? "", choices[i])))
                .Where(result => result.Score.HasValue)
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Index)
                .Select(result => result.Index)
                .ToList();
            list.ItemsSource = indices.Select(i => choices[i]).ToArray();
            list.SelectedIndex = indices.Count > 0 ? 0 : -1;
            empty.IsVisible = indices.Count == 0;
            count.Text = indices.Count == 1 ? "1 result" : $"{indices.Count} results";
        }
        void Select() { if (list.SelectedIndex >= 0) dialog.Close((int?)indices[list.SelectedIndex]); }
        var grid = new Grid { Margin = new Thickness(16), RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        grid.Children.Add(query);
        var results = new Grid(); results.Children.Add(list); results.Children.Add(empty); Grid.SetRow(results, 1); grid.Children.Add(results);
        Grid.SetRow(count, 2); count.Margin = new Thickness(2, 10, 0, 0); grid.Children.Add(count);
        dialog.Content = WindowCaption.Wrap(dialog, grid);
        query.TextChanged += (_, _) => Update(); list.DoubleTapped += (_, _) => Select();
        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) { Select(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Escape) { dialog.Close(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Down && list.SelectedIndex + 1 < indices.Count) { list.SelectedIndex++; e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Up && list.SelectedIndex > 0) { list.SelectedIndex--; e.Handled = true; }
        };
        Update(); dialog.Opened += (_, _) => { query.Focus(); query.CaretIndex = query.Text?.Length ?? 0; };
        return await dialog.ShowDialog<int?>(owner);
    }
    public static async Task<bool> Confirm(Window owner, string title, string message, string accept = "Close sessions")
    {
        var dialog = new Window { RequestedThemeVariant = owner.ActualThemeVariant, Title = title, Width = 440, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var yes = new Button { Content = accept }; var no = new Button { Content = "Cancel", IsCancel = true };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        buttons.Children.Add(no); buttons.Children.Add(yes); panel.Children.Add(buttons); dialog.Content = WindowCaption.Wrap(dialog, panel);
        yes.Click += (_, _) => dialog.Close(true); no.Click += (_, _) => dialog.Close(false);
        return await dialog.ShowDialog<bool>(owner);
    }
}
