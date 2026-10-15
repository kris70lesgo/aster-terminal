using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Aster.Sessions.Triggers;

namespace Aster.App.Views;

public sealed class TriggerEditor : UserControl
{
    private readonly ListBox _list = new() { MinHeight = 100 };
    private readonly TextBox _name = new() { Name = "TriggerName" };
    private readonly TextBox _pattern = new() { Name = "TriggerPattern", FontFamily = new Avalonia.Media.FontFamily("monospace") };
    private readonly ComboBox _action = new() { ItemsSource = new[] { "Highlight", "Bookmark" }, SelectedIndex = 0, MinWidth = 140 };
    private readonly CheckBox _enabled = new() { Content = "Enabled" };
    private readonly TextBox _foreground = new() { PlaceholderText = "#FFFFFF" };
    private readonly TextBox _background = new() { PlaceholderText = "#9B3038" };
    private readonly StackPanel _fields = new() { Spacing = 10, Margin = new Thickness(10, 0, 0, 0) };
    private readonly TextBlock _error = new() { Foreground = Avalonia.Media.Brushes.IndianRed, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private List<OutputTrigger> _rules = [];
    private int _selected = -1;
    private bool _loading;

    public TriggerEditor()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*"), Margin = new Thickness(8, 16) };
        var sidebar = new Grid { RowDefinitions = new RowDefinitions("*,Auto") }; sidebar.Children.Add(_list);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
        var add = new Button { Content = "+", Name = "AddTrigger" }; var remove = new Button { Content = "−", Name = "RemoveTrigger" };
        Avalonia.Automation.AutomationProperties.SetName(add, "Add output rule");
        Avalonia.Automation.AutomationProperties.SetName(remove, "Remove output rule");
        buttons.Children.Add(add); buttons.Children.Add(remove); Grid.SetRow(buttons, 1); sidebar.Children.Add(buttons); grid.Children.Add(sidebar);
        void Field(string label, Control control) { _fields.Children.Add(new TextBlock { Text = label }); _fields.Children.Add(control); }
        Field("Name", _name); Field("Regular expression", _pattern); Field("Action", _action);
        _fields.Children.Add(_enabled);
        var colors = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") }; colors.Children.Add(_foreground); Grid.SetColumn(_background, 2); colors.Children.Add(_background);
        Field("Text / background colours (#RRGGBB)", colors);
        _fields.Children.Add(new TextBlock { Text = "Matches completed lines, including wrapping. No lookarounds or backreferences. Highlight and bookmark actions appear in Triggered lines; no commands are sent.", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        _fields.Children.Add(_error);
        var scroll = new ScrollViewer { Content = _fields }; Grid.SetColumn(scroll, 1); grid.Children.Add(scroll); Content = grid;
        _list.SelectionChanged += (_, _) =>
        {
            if (_loading) return;
            Commit(); LoadSelected(_list.SelectedIndex);
        };
        add.Click += (_, _) => { Commit(); if (_rules.Count >= 32) { _error.Text = "Keep at most 32 rules."; return; } _rules.Add(new()); Refresh(_rules.Count - 1); };
        remove.Click += (_, _) => { if (_selected < 0) return; _rules.RemoveAt(_selected); Refresh(Math.Min(_selected, _rules.Count - 1)); };
        _action.SelectionChanged += (_, _) => _foreground.IsEnabled = _background.IsEnabled = Equals(_action.SelectedItem, "Highlight");
        SetRules([]);
    }
    public void SetRules(IEnumerable<OutputTrigger> rules) { _rules = rules.ToList(); Refresh(_rules.Count > 0 ? 0 : -1); }
    public List<OutputTrigger> GetRules() { Commit(); foreach (var rule in _rules) rule.Validate(); return _rules.ToList(); }
    private void Commit()
    {
        if (_selected < 0) return;
        _rules[_selected] = new() { Name = _name.Text?.Trim() ?? "", Pattern = _pattern.Text ?? "", Action = _action.SelectedItem as string ?? "Highlight", Enabled = _enabled.IsChecked == true, Foreground = _foreground.Text ?? "", Background = _background.Text ?? "" };
    }
    private void Refresh(int index)
    {
        _loading = true; _list.ItemsSource = _rules.Select(r => r.Name).ToArray(); _list.SelectedIndex = index; _loading = false; LoadSelected(index);
    }
    private void LoadSelected(int index)
    {
        _selected = index; _fields.IsVisible = index >= 0; _error.Text = "";
        if (index < 0) return;
        var rule = _rules[index]; _name.Text = rule.Name; _pattern.Text = rule.Pattern; _action.SelectedItem = rule.Action; _enabled.IsChecked = rule.Enabled; _foreground.Text = rule.Foreground; _background.Text = rule.Background;
    }
}
