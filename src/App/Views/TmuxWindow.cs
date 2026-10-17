using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aster.App.Services;
using Aster.Sessions;
using Aster.Sessions.Tmux;

namespace Aster.App.Views;

/// <summary>Native control-mode view; tmux, rather than this window, owns its shells.</summary>
public sealed class TmuxWindow : Window
{
    private readonly TmuxController _controller;
    private readonly TerminalProfile _profile;
    private readonly TerminalTabs _tabs = new();
    private readonly FindBar _findBar = new() { Name = "SearchBar" };
    private readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8, 4), IsVisible = false };
    private readonly Dictionary<int, TerminalPane> _panes = [];
    private readonly Dictionary<int, TabItem> _windows = [];
    private readonly DispatcherTimer _refresh;
    private readonly DispatcherTimer _resize;
    private readonly SemaphoreSlim _mutation = new(1, 1);
    private bool _closing, _closed, _updating;
    private int? _active;
    private IReadOnlyList<TmuxWindowInfo> _layouts = [];
    private string _layoutShape = "";
    private (int Columns, int Rows) _lastSize;
    private readonly Dictionary<string, KeyGesture> _shortcuts = ShortcutMap.Resolve(new Dictionary<string, string>());
    public bool IsReady { get; private set; }
    public IReadOnlyDictionary<int, TerminalPane> Panes => _panes;
    public TmuxWindow(TmuxController controller, TerminalProfile profile)
    {
        _controller = controller; _profile = profile;
        Title = "tmux — Aster Terminal"; Width = 1080; Height = 680; MinWidth = 620; MinHeight = 320;
        this.Bind(BackgroundProperty, this.GetResourceObservable("WindowBackground"));
        WindowDecorations = WindowDecorations.BorderOnly; ExtendClientAreaToDecorationsHint = true;
        var menu = new Menu(); var session = new MenuItem { Header = "_tmux" }; menu.Items.Add(session);
        void Item(string label, Func<Task> action)
        {
            var item = new MenuItem { Header = label }; item.Click += (_, _) => Observe(action()); session.Items.Add(item);
        }
        Item("New window", NewWindowAsync);
        Item("Split right", () => SplitAsync(false)); Item("Split below", () => SplitAsync(true));
        Item("Close pane", ClosePaneAsync);
        Item("Copy", async () => { if (_active is { } id && _panes.TryGetValue(id, out var pane)) await pane.Terminal.CopyAsync(); });
        Item("Paste", async () => { if (_active is { } id && _panes.TryGetValue(id, out var pane)) await pane.Terminal.PasteAsync(); });
        Item("Find…", FindAsync);
        Item("Detach", () => { Close(); return Task.CompletedTask; });
        _tabs.MenuBar = menu; _tabs.FindContent = _findBar; _tabs.StatusContent = _message; Content = _tabs;
        TerminalWindowNavigation.Attach(this, _tabs);
        _tabs.NewTabRequested += (_, _) => Observe(NewWindowAsync());
        if (OperatingSystem.IsMacOS()) { menu.IsVisible = false; NativeMenu.SetMenu(this, NativeMenuBridge.FromMenu(menu, _shortcuts)); }
        _refresh = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(75) };
        _refresh.Tick += (_, _) => { _refresh.Stop(); Observe(RefreshAsync()); };
        _resize = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _resize.Tick += (_, _) => { _resize.Stop(); Observe(ResizeClientAsync()); };
        SizeChanged += (_, _) => ScheduleResize();
        _tabs.SizeChanged += (_, _) => ScheduleResize();
        _controller.LayoutChanged += LayoutChanged;
        _controller.Faulted += Failed;
        _tabs.SelectionChanged += (_, e) =>
        {
            if (e.Source != _tabs || _updating || _closing || _tabs.SelectedItem is not TabItem { Tag: int id }) return;
            // Selected tab's last active pane is recorded by its content tag.
            if (_windows[id].Content is Grid { Tag: int paneId }) SetActive(paneId, true);
        };
        Opened += (_, _) => Observe(StartAsync()); Closing += ClosingWindow;
        AddHandler(KeyDownEvent, Shortcut, RoutingStrategies.Tunnel);
    }
    private async Task ClosePaneAsync()
    {
        if (_active is { } id && await DialogWindow.Confirm(this, "Close tmux pane", "End this pane's process in tmux?", "Close pane"))
        { await _controller.KillPaneAsync(id); await RefreshAsync(); }
    }
    private Task FindAsync()
    {
        if (_active is { } id && _panes.TryGetValue(id, out var pane)) _findBar.Open(pane);
        return Task.CompletedTask;
    }
    private async Task NewWindowAsync()
    {
        var existing = _windows.Keys.ToHashSet();
        await _controller.NewWindowAsync(); await RefreshAsync();
        var created = _windows.FirstOrDefault(pair => !existing.Contains(pair.Key)).Value;
        if (created is not null) _tabs.SelectedItem = created;
    }
    private void Shortcut(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _findBar.IsVisible) { _findBar.Close(); e.Handled = true; return; }
        var action = _shortcuts.FirstOrDefault(p => p.Value.Matches(e)).Key;
        Task? task = action switch
        {
            "NewTab" or "NewWindow" => NewWindowAsync(),
            "ClosePane" => ClosePaneAsync(), "Find" => FindAsync(),
            "SplitRight" => SplitAsync(false), "SplitBelow" => SplitAsync(true),
            "Copy" when _active is { } copy => _panes[copy].Terminal.CopyAsync(),
            "Paste" when _active is { } paste => _panes[paste].Terminal.PasteAsync(),
            _ => null
        };
        if (task is not null) { e.Handled = true; Observe(task); }
        else if (action is "NextPane" or "PreviousPane")
        {
            var panes = _tabs.SelectedItem is TabItem { Content: Grid root } ? root.GetVisualDescendants().OfType<TerminalPane>().ToArray() : [];
            if (panes.Length == 0) return;
            var index = Array.FindIndex(panes, p => _active is { } id && _panes[id] == p);
            var pane = panes[(index + (action == "NextPane" ? 1 : panes.Length - 1)) % panes.Length];
            SetActive(_panes.Single(p => p.Value == pane).Key, true); e.Handled = true;
        }
        else if (action is "NextTab" or "PreviousTab")
        { if (_tabs.Items.Count > 0) _tabs.SelectedIndex = (_tabs.SelectedIndex + (action == "NextTab" ? 1 : _tabs.Items.Count - 1)) % _tabs.Items.Count; e.Handled = true; }
    }
    private void LayoutChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => { if (!_closing) { _refresh.Stop(); _refresh.Start(); } });
    private void Failed(object? sender, Exception e) => Dispatcher.UIThread.Post(() => ShowError(e.Message));
    private void ShowError(string text) { _message.Text = text; _message.IsVisible = true; }
    private async void Observe(Task task) { try { await task; } catch (Exception e) { if (!_closing) ShowError(e.Message); } }
    private async Task StartAsync()
    {
        await _controller.StartAsync(); await RefreshAsync(); IsReady = true; await ResizeClientAsync();
    }
    private void SetActive(int id, bool select)
    {
        var changed = _active != id; _active = id;
        foreach (var pair in _panes) pair.Value.SetActive(pair.Key == id);
        if (select && changed && _controller.IsRunning) Observe(_controller.SelectPaneAsync(id));
        if (_panes.TryGetValue(id, out var pane))
        {
            if (_findBar.IsVisible) _findBar.SetPane(pane);
            // A metadata/layout refresh must not interrupt typing in Find.
            if (select || !_findBar.IsVisible) pane.Terminal.Focus();
        }
    }
    public async Task SplitAsync(bool below)
    {
        if (_active is not { } id) return; await _controller.SplitAsync(id, below); await RefreshAsync();
    }
    public async Task RefreshAsync()
    {
        await _mutation.WaitAsync();
        try
        {
            if (_closing) return;
            var windows = await _controller.WindowsAsync(); _layouts = windows;
            static string Shape(TmuxLayout node) => node.PaneId is { } id ? id.ToString() : (node.Vertical ? "v(" : "h(") + string.Join(",", node.Children.Select(Shape)) + ")";
            var shape = string.Join("|", windows.Select(w => w.Id + ":" + Shape(w.Layout)));
            var structureChanged = shape != _layoutShape; _layoutShape = shape;
            var wanted = windows.SelectMany(w => w.Layout.Panes).Select(p => p.PaneId!.Value).ToHashSet();
            if (_active is { } activeId && !wanted.Contains(activeId)) _findBar.SetPane(null);
            _updating = true;
            // Detach visual parents without destroying engines/connections when
            // tmux rearranges existing panes.
            foreach (var pane in _panes.Values) if (pane.Parent is Panel parent) parent.Children.Remove(pane);
            foreach (var id in _panes.Keys.Where(id => !wanted.Contains(id)).ToArray()) { await _panes[id].CloseAsync(); _panes.Remove(id); }
            var created = new List<TerminalPane>();
            foreach (var id in wanted.Where(id => !_panes.ContainsKey(id)))
            {
                var connection = _controller.CreatePane(id);
                var pane = new TerminalPane(_profile with { Name = $"tmux %{id}" }, ActualThemeVariant == ThemeVariant.Light, connection: connection);
                pane.Activated += (_, _) => { if (!_updating) SetActive(id, true); };
                _panes.Add(id, pane); created.Add(pane);
            }
            var selected = (_tabs.SelectedItem as TabItem)?.Tag;
            foreach (var id in _windows.Keys.Where(id => windows.All(w => w.Id != id)).ToArray()) { _tabs.Items.Remove(_windows[id]); _windows.Remove(id); }
            foreach (var window in windows)
            {
                if (!_windows.TryGetValue(window.Id, out var tab)) { tab = new TabItem { Tag = window.Id }; _windows.Add(window.Id, tab); _tabs.Items.Add(tab); }
                tab.Header = window.Name; var root = new Grid { Tag = window.ActivePane }; root.Children.Add(Layout(window.Layout)); tab.Content = root;
                root.SizeChanged += (_, _) => ScheduleResize();
            }
            _tabs.Classes.Set("single", windows.Count <= 1);
            if (selected is int old && _windows.TryGetValue(old, out var selectedTab)) _tabs.SelectedItem = selectedTab;
            else if (_tabs.Items.Count > 0) _tabs.SelectedIndex = 0;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            foreach (var pane in created) await pane.StartAsync();
            if (_tabs.SelectedItem is TabItem { Content: Grid { Tag: int active } }) SetActive(active, false);
            if (windows.Count == 0) ShowError("The tmux session has ended.");
            if (IsReady && structureChanged) { _resize.Stop(); _resize.Start(); }
        }
        finally { _updating = false; _mutation.Release(); }
    }
    private Control Layout(TmuxLayout node)
    {
        if (node.PaneId is { } id) return _panes[id];
        var grid = new Grid();
        var cell = Devolutions.Terminal.TermControl.MeasureCell(_profile.ToControlProfile(ActualThemeVariant == ThemeVariant.Light), RenderScaling);
        var sizes = string.Join(",1,", node.Children.Select(c => (node.Vertical ? Required(c, cell.Width, cell.Height).Height : Required(c, cell.Width, cell.Height).Width).ToString(System.Globalization.CultureInfo.InvariantCulture) + "*"));
        if (node.Vertical) grid.RowDefinitions = new(sizes); else grid.ColumnDefinitions = new(sizes);
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = Layout(node.Children[i]); if (node.Vertical) Grid.SetRow(child, i * 2); else Grid.SetColumn(child, i * 2); grid.Children.Add(child);
            if (i == 0) continue;
            var divider = new Border { Background = Brush.Parse("#555761") }; if (node.Vertical) Grid.SetRow(divider, i * 2 - 1); else Grid.SetColumn(divider, i * 2 - 1); grid.Children.Add(divider);
        }
        return grid;
    }
    private static (double Width, double Height) Required(TmuxLayout node, double cellWidth, double cellHeight)
    {
        if (node.PaneId is not null) return (node.Columns * cellWidth + 10 + TerminalScrollBar.GutterWidth, node.Rows * cellHeight + 10);
        var children = node.Children.Select(c => Required(c, cellWidth, cellHeight)).ToArray();
        return node.Vertical ? (children.Max(c => c.Width), children.Sum(c => c.Height) + children.Length - 1)
            : (children.Sum(c => c.Width) + children.Length - 1, children.Max(c => c.Height));
    }
    private void ScheduleResize() { if (IsReady && !_closing) { _resize.Stop(); _resize.Start(); } }
    public async Task SynchronizeSizeAsync() { _resize.Stop(); await ResizeClientAsync(); await RefreshAsync(); }
    private async Task ResizeClientAsync()
    {
        if (_closing || !IsReady) return;
        var cell = Devolutions.Terminal.TermControl.MeasureCell(_profile.ToControlProfile(ActualThemeVariant == ThemeVariant.Light), RenderScaling);
        if (_tabs.SelectedItem is not TabItem { Content: Grid content } || content.Bounds.Width <= 0 || content.Bounds.Height <= 0 || _layouts.Count == 0) return;
        // Account for each pane's padding and pixel dividers. tmux reserves whole
        // character separators. Control clients have no rendered status rows.
        var extraWidth = _layouts.Max(w => Required(w.Layout, cell.Width, cell.Height).Width - w.Layout.Columns * cell.Width);
        var extraHeight = _layouts.Max(w => Required(w.Layout, cell.Width, cell.Height).Height - w.Layout.Rows * cell.Height);
        var size = (Columns: Math.Clamp((int)((content.Bounds.Width - extraWidth) / cell.Width), 2, 1000), Rows: Math.Clamp((int)((content.Bounds.Height - extraHeight) / cell.Height), 2, 500));
        if (size == _lastSize) return; _lastSize = size; await _controller.ResizeAsync(size.Columns, size.Rows);
    }
    private async void ClosingWindow(object? sender, WindowClosingEventArgs e)
    {
        if (_closed) return; e.Cancel = true; if (_closing) return; _closing = true;
        _findBar.Close(restoreFocus: false);
        _refresh.Stop(); _resize.Stop(); _controller.LayoutChanged -= LayoutChanged; _controller.Faulted -= Failed;
        await _mutation.WaitAsync();
        try
        {
            foreach (var pane in _panes.Values) await pane.CloseAsync();
            await _controller.DisposeAsync(); _closed = true; Close();
        }
        catch (Exception error) { ShowError(error.Message); _closing = false; }
        finally { _mutation.Release(); }
    }
}
