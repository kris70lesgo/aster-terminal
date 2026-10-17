using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

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
        var dialog = new Window { RequestedThemeVariant = owner.ActualThemeVariant, Title = title, Width = 620, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var query = new TextBox { PlaceholderText = "Search titles, profiles and working directories" };
        var list = new ListBox();
        var indices = Enumerable.Range(0, choices.Count).ToList();
        void Update() { indices = Enumerable.Range(0, choices.Count).Where(i => choices[i].Contains(query.Text ?? "", StringComparison.OrdinalIgnoreCase)).ToList(); list.ItemsSource = indices.Select(i => choices[i]).ToArray(); list.SelectedIndex = indices.Count > 0 ? 0 : -1; }
        void Select() { if (list.SelectedIndex >= 0) dialog.Close((int?)indices[list.SelectedIndex]); }
        var grid = new Grid { Margin = new Thickness(16), RowDefinitions = new RowDefinitions("Auto,*") }; grid.Children.Add(query); Grid.SetRow(list, 1); grid.Children.Add(list); dialog.Content = WindowCaption.Wrap(dialog, grid);
        query.TextChanged += (_, _) => Update(); list.DoubleTapped += (_, _) => Select();
        dialog.KeyDown += (_, e) => { if (e.Key == Avalonia.Input.Key.Enter) Select(); if (e.Key == Avalonia.Input.Key.Escape) dialog.Close(); if (e.Key == Avalonia.Input.Key.Down && list.SelectedIndex + 1 < indices.Count) list.SelectedIndex++; if (e.Key == Avalonia.Input.Key.Up && list.SelectedIndex > 0) list.SelectedIndex--; };
        Update(); dialog.Opened += (_, _) => query.Focus();
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
