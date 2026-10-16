using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Aster.Sessions.Search;

namespace Aster.App.Views;

/// <summary>Compact Find UI bound to one pane; the pane owns matching and cancellation.</summary>
public partial class FindBar : Border
{
    private TerminalPane? _pane;
    public FindBar() => InitializeComponent();

    public void Open(TerminalPane? pane)
    {
        IsVisible = true; SetPane(pane); SearchBox.Focus(); SearchBox.SelectAll();
    }
    public void Close(bool restoreFocus = true)
    {
        var pane = _pane;
        IsVisible = false; SetPane(null);
        if (restoreFocus) pane?.Terminal.Focus();
    }
    public void SetPane(TerminalPane? pane)
    {
        if (!IsVisible) pane = null;
        if (_pane == pane) return;
        if (_pane is { } old) { old.Find.Changed -= FindStateChanged; old.Find.Deactivate(); }
        _pane = pane;
        if (pane is not null) { pane.Find.Activate(); pane.Find.Changed += FindStateChanged; }
        SearchStatus.Text = ""; ToolTip.SetTip(SearchStatus, null);
        if (pane is not null && SearchBox.Text?.Length > 0) Observe(SearchAsync(navigate: false, debounce: true));
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SetPane(null);
        base.OnDetachedFromVisualTree(e);
    }
    private void FindStateChanged(object? sender, EventArgs args)
    {
        SearchStatus.Text = _pane?.Find.Status ?? "";
        ToolTip.SetTip(SearchStatus, SearchStatus.Text);
    }
    private Task SearchAsync(bool previous = false, bool navigate = true, bool debounce = false) =>
        _pane?.Find.SearchAsync(new FindOptions(SearchBox.Text ?? "", RegexBox.IsChecked == true, MatchCaseBox.IsChecked == true, WholeWordBox.IsChecked == true), previous, navigate, debounce) ?? Task.CompletedTask;
    private async void Observe(Task task)
    {
        try { await task; }
        catch (Exception error)
        {
            if (!IsVisible || _pane is null) return;
            SearchStatus.Text = "Find unavailable: " + error.Message;
            ToolTip.SetTip(SearchStatus, SearchStatus.Text);
        }
    }
    private void SearchKeyDown(object? sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) { Observe(SearchAsync(e.KeyModifiers.HasFlag(KeyModifiers.Shift))); e.Handled = true; } }
    private void SearchTextChanged(object? sender, TextChangedEventArgs e)
    { if (IsVisible) Observe(SearchAsync(navigate: false, debounce: true)); }
    private void SearchOptionChanged(object? sender, RoutedEventArgs e)
    { if (IsVisible) Observe(SearchAsync(navigate: false, debounce: true)); }
    private void PreviousClicked(object? sender, RoutedEventArgs e) => Observe(SearchAsync(previous: true));
    private void NextClicked(object? sender, RoutedEventArgs e) => Observe(SearchAsync());
    private void DoneClicked(object? sender, RoutedEventArgs e) => Close();
}
