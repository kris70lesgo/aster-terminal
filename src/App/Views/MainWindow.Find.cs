using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Aster.Sessions.Search;

namespace Aster.App.Views;

public partial class MainWindow
{
    private TerminalPane? _findPane;
    private void ShowSearch()
    {
        SearchBar.IsVisible = true; SetFindPane(Active); SearchBox.Focus(); SearchBox.SelectAll();
    }
    private void HideSearch()
    {
        SearchBar.IsVisible = false; SetFindPane(null); Active?.Terminal.Focus();
    }
    private void SetFindPane(TerminalPane? pane)
    {
        if (_findPane == pane) return;
        if (_findPane is { } old) { old.Find.Changed -= FindStateChanged; old.Find.Deactivate(); }
        _findPane = pane;
        if (pane is not null) { pane.Find.Activate(); pane.Find.Changed += FindStateChanged; }
        SearchStatus.Text = "";
        if (pane is not null && SearchBox.Text?.Length > 0) Observe(SearchAsync(navigate: false, debounce: true));
    }
    private void FindStateChanged(object? sender, EventArgs args)
    {
        SearchStatus.Text = _findPane?.Find.Status ?? "";
        ToolTip.SetTip(SearchStatus, SearchStatus.Text);
    }
    private Task SearchAsync(bool previous = false, bool navigate = true, bool debounce = false) =>
        _findPane?.Find.SearchAsync(new FindOptions(SearchBox.Text ?? "", RegexBox.IsChecked == true, MatchCaseBox.IsChecked == true, WholeWordBox.IsChecked == true), previous, navigate, debounce) ?? Task.CompletedTask;
    private void SearchKeyDown(object? sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) { Observe(SearchAsync(e.KeyModifiers.HasFlag(KeyModifiers.Shift))); e.Handled = true; } }
    private void SearchTextChanged(object? sender, TextChangedEventArgs e)
    { if (SearchBar?.IsVisible == true) Observe(SearchAsync(navigate: false, debounce: true)); }
    private void SearchOptionChanged(object? sender, RoutedEventArgs e)
    { if (SearchBar?.IsVisible == true) Observe(SearchAsync(navigate: false, debounce: true)); }
    private void PreviousClicked(object? sender, RoutedEventArgs e) => Observe(SearchAsync(previous: true));
    private void NextClicked(object? sender, RoutedEventArgs e) => Observe(SearchAsync());
    private void DoneClicked(object? sender, RoutedEventArgs e) => HideSearch();
}
