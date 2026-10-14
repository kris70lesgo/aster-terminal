using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Aster.Sessions;

namespace Aster.App.Views;

public partial class MainWindow : Window
{
    private sealed class Workspace
    {
        public TabItem Tab { get; } = new();
        public Grid Root { get; } = new();
        public List<TerminalPane> Panes { get; } = [];
        public TerminalPane? Active { get; set; }
    }
    private readonly List<Workspace> _workspaces = [];
    private readonly SemaphoreSlim _layoutMutation = new(1, 1);
    private bool _closing;
    private bool _closed;
    private readonly TerminalProfile _profile;
    private Workspace? Current => _workspaces.FirstOrDefault(w => w.Tab == Tabs.SelectedItem);
    private TerminalPane? Active => Current?.Active;

    public MainWindow() : this(TerminalProfile.Default()) { }

    public MainWindow(TerminalProfile profile)
    {
        _profile = profile;
        InitializeComponent();
        Opened += async (_, _) => await NewTabAsync();
        Closing += OnClosing;
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);
    }

    private TerminalPane CreatePane(Workspace workspace)
    {
        var pane = new TerminalPane(_profile);
        workspace.Panes.Add(pane);
        pane.Activated += (_, _) => Activate(workspace, pane);
        pane.TitleChanged += (_, title) =>
        {
            if (workspace.Active == pane)
                workspace.Tab.Header = title.Length > 60 ? title[..60] : title;
        };
        return pane;
    }

    private void Activate(Workspace workspace, TerminalPane pane)
    {
        workspace.Active = pane;
        foreach (var candidate in workspace.Panes) candidate.SetActive(candidate == pane);
        WindowStatus.Text = $"{pane.Profile.Name} · {workspace.Panes.Count} pane(s) · Aster Terminal";
        if (SearchBar.IsVisible) SearchStatus.Text = "Press Enter to search this pane";
    }

    private Task NewTabAsync() => ChangeLayoutAsync(NewTabCoreAsync);
    private Task SplitAsync(bool below) => ChangeLayoutAsync(() => SplitCoreAsync(below));
    private Task ClosePaneAsync() => ChangeLayoutAsync(ClosePaneCoreAsync);

    private async Task ChangeLayoutAsync(Func<Task> change)
    {
        await _layoutMutation.WaitAsync();
        try { if (!_closing) await change(); }
        finally { _layoutMutation.Release(); }
    }

    private async Task NewTabCoreAsync()
    {
        if (_closing) return;
        var workspace = new Workspace();
        var pane = CreatePane(workspace);
        workspace.Root.Children.Add(pane);
        workspace.Tab.Header = pane.Profile.Name;
        workspace.Tab.Content = workspace.Root;
        _workspaces.Add(workspace);
        Tabs.Items.Add(workspace.Tab);
        Tabs.SelectedItem = workspace.Tab;
        Activate(workspace, pane);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await pane.StartAsync();
    }

    private async Task SplitCoreAsync(bool below)
    {
        if (_closing || Current is not { } workspace || Active is not { } old || old.Parent is not Grid parent) return;
        var pane = CreatePane(workspace);
        var split = new Grid();
        if (below) split.RowDefinitions = new RowDefinitions("*,5,*");
        else split.ColumnDefinitions = new ColumnDefinitions("*,5,*");
        var oldRow = Grid.GetRow(old);
        var oldColumn = Grid.GetColumn(old);
        parent.Children.Remove(old);
        Grid.SetRow(split, oldRow);
        Grid.SetColumn(split, oldColumn);
        parent.Children.Add(split);
        Grid.SetRow(old, 0); Grid.SetColumn(old, 0);
        split.Children.Add(old);
        var divider = new GridSplitter
        {
            Background = Avalonia.Media.Brush.Parse("#343A44"),
            ResizeDirection = below ? GridResizeDirection.Rows : GridResizeDirection.Columns,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        if (below) { Grid.SetRow(divider, 1); Grid.SetRow(pane, 2); }
        else { Grid.SetColumn(divider, 1); Grid.SetColumn(pane, 2); }
        split.Children.Add(divider);
        split.Children.Add(pane);
        Activate(workspace, pane);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await pane.StartAsync();
    }

    private async Task ClosePaneCoreAsync()
    {
        if (_closing || Current is not { } workspace || Active is not { } pane) return;
        await pane.CloseAsync();
        workspace.Panes.Remove(pane);
        if (workspace.Panes.Count == 0)
        {
            Tabs.Items.Remove(workspace.Tab);
            _workspaces.Remove(workspace);
            if (_workspaces.Count == 0) { Close(); return; }
            Tabs.SelectedItem = _workspaces[^1].Tab;
            Current?.Active?.Terminal.Focus();
            return;
        }
        if (pane.Parent is Grid split && split.Parent is Grid parent)
        {
            var remaining = split.Children.First(c => c != pane && c is not GridSplitter);
            var row = Grid.GetRow(split); var column = Grid.GetColumn(split);
            split.Children.Remove(remaining);
            parent.Children.Remove(split);
            Grid.SetRow(remaining, row); Grid.SetColumn(remaining, column);
            parent.Children.Add(remaining);
        }
        Activate(workspace, workspace.Panes[^1]);
        workspace.Active?.Terminal.Focus();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closed) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        WindowStatus.Text = "Closing shell sessions…";
        await _layoutMutation.WaitAsync();
        try
        {
            foreach (var workspace in _workspaces.ToArray())
                foreach (var pane in workspace.Panes.ToArray()) await pane.CloseAsync();
            _closed = true;
            Close();
        }
        catch (Exception error)
        {
            _closing = false;
            WindowStatus.Text = $"Session cleanup failed: {error.Message}";
        }
        finally { _layoutMutation.Release(); }
    }

    private void OnShortcut(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control) && _workspaces.Count > 0)
        {
            var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
            Tabs.SelectedIndex = (Tabs.SelectedIndex + step + _workspaces.Count) % _workspaces.Count;
            e.Handled = true;
            return;
        }
        var command = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control | KeyModifiers.Shift;
        if ((e.KeyModifiers & command) != command) return;
        e.Handled = true;
        switch (e.Key)
        {
            case Key.T: Observe(NewTabAsync()); break;
            case Key.D: Observe(SplitAsync(OperatingSystem.IsMacOS() ? e.KeyModifiers.HasFlag(KeyModifiers.Shift) : e.KeyModifiers.HasFlag(KeyModifiers.Alt))); break;
            case Key.W: Observe(ClosePaneAsync()); break;
            case Key.F: ShowSearch(); break;
            case Key.C: if (Active is { } copyPane) Observe(copyPane.Terminal.CopyAsync()); break;
            case Key.V: if (Active is { } pastePane) Observe(pastePane.Terminal.PasteAsync()); break;
            case Key.OemPlus: Active?.Terminal.AdjustFontSize(1); break;
            case Key.OemMinus: Active?.Terminal.AdjustFontSize(-1); break;
            default: e.Handled = false; break;
        }
    }

    private async void Observe(Task task)
    {
        try { await task; }
        catch (Exception error) { WindowStatus.Text = error.Message; }
    }
    private void ShowSearch() { SearchBar.IsVisible = true; SearchBox.Focus(); }
    private void HideSearch() { SearchBar.IsVisible = false; Active?.Terminal.Search.Clear(); Active?.Terminal.SetSearchHighlights([]); Active?.Terminal.Focus(); }
    private void Search(bool previous)
    {
        if (Active is not { } pane) return;
        if (string.IsNullOrWhiteSpace(SearchBox.Text)) { pane.Terminal.Search.Clear(); pane.Terminal.SetSearchHighlights([]); SearchStatus.Text = ""; return; }
        var found = pane.Terminal.Find(SearchBox.Text, previous);
        SearchStatus.Text = found ? $"{pane.Terminal.Search.CurrentIndex + 1} / {pane.Terminal.Search.Matches.Count}" : "No matches";
    }
    private void TabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Current?.Active is { } pane) Dispatcher.UIThread.Post(() => pane.Terminal.Focus());
    }
    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Search(e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; }
        if (e.Key == Key.Escape) { HideSearch(); e.Handled = true; }
    }
    private void NewTabClicked(object? sender, RoutedEventArgs e) => Observe(NewTabAsync());
    private void SplitRightClicked(object? sender, RoutedEventArgs e) => Observe(SplitAsync(false));
    private void SplitBelowClicked(object? sender, RoutedEventArgs e) => Observe(SplitAsync(true));
    private void ClosePaneClicked(object? sender, RoutedEventArgs e) => Observe(ClosePaneAsync());
    private void FindClicked(object? sender, RoutedEventArgs e) => ShowSearch();
    private void CopyClicked(object? sender, RoutedEventArgs e) { if (Active is { } pane) Observe(pane.Terminal.CopyAsync()); }
    private void PasteClicked(object? sender, RoutedEventArgs e) { if (Active is { } pane) Observe(pane.Terminal.PasteAsync()); }
    private void PreviousClicked(object? sender, RoutedEventArgs e) => Search(true);
    private void NextClicked(object? sender, RoutedEventArgs e) => Search(false);
    private void DoneClicked(object? sender, RoutedEventArgs e) => HideSearch();
}
