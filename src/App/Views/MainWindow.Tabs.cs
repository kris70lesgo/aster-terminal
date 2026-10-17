using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Aster.App.Views;

public partial class MainWindow
{
    private readonly ObservableCollection<TabItem> _tabItems = [];
    private bool _reorderingTabs, _draggingTab;
    private TabItem? _dragTab, _dropTab;
    private IPointer? _tabPointer;
    private Point _tabDragStart;
    private bool _dropAfter;

    private void InitializeTabReordering()
    {
        Tabs.ItemsSource = _tabItems;
        Tabs.AddHandler(PointerPressedEvent, TabPointerPressed, RoutingStrategies.Tunnel);
        Tabs.AddHandler(PointerMovedEvent, TabPointerMoved, RoutingStrategies.Tunnel);
        Tabs.AddHandler(PointerReleasedEvent, TabPointerReleased, RoutingStrategies.Tunnel);
        Tabs.PointerCaptureLost += (_, _) => CancelTabDrag();
        Deactivated += (_, _) => CancelTabDrag();
    }

    private Task MoveCurrentTabAsync(int step) => ChangeLayoutAsync(() =>
    {
        // iTerm2's ungrouped keyboard movement wraps at either end.
        if (Current is { } workspace && _workspaces.Count > 1)
            MoveTabCore(workspace, (_workspaces.IndexOf(workspace) + step + _workspaces.Count) % _workspaces.Count);
        return Task.CompletedTask;
    });

    private void MoveTabCore(Workspace workspace, int index)
    {
        var previous = _workspaces.IndexOf(workspace);
        if (previous < 0 || index < 0 || index >= _workspaces.Count || previous == index) return;
        var selected = Tabs.SelectedItem;
        var focused = FocusManager?.GetFocusedElement();
        _reorderingTabs = true;
        try
        {
            _workspaces.RemoveAt(previous); _workspaces.Insert(index, workspace);
            _tabItems.Move(previous, index);
            Tabs.SelectedItem = selected;
        }
        finally { _reorderingTabs = false; }
        // A collection move must not redirect Find input into the shell.
        focused?.Focus();
    }

    private void TabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_closing || e.Pointer.Type != PointerType.Mouse ||
            !e.GetCurrentPoint(Tabs).Properties.IsLeftButtonPressed || e.Source is not Visual visual) return;
        var ancestry = visual.GetSelfAndVisualAncestors().TakeWhile(v => v != Tabs).ToArray();
        if (ancestry.OfType<Button>().Any()) return; // Close stays a normal button.
        var tab = ancestry.OfType<TabItem>().FirstOrDefault();
        if (tab is null || !_tabItems.Contains(tab)) return;
        CancelTabDrag();
        _dragTab = tab; _tabPointer = e.Pointer; _tabDragStart = e.GetPosition(Tabs);
        // Do not capture/handle a click; normal tab selection still runs.
    }

    private void TabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragTab is null || e.Pointer != _tabPointer) return;
        if (!e.GetCurrentPoint(Tabs).Properties.IsLeftButtonPressed) { CancelTabDrag(); return; }
        var point = e.GetPosition(Tabs);
        var delta = point - _tabDragStart;
        if (!_draggingTab && delta.X * delta.X + delta.Y * delta.Y < 36) return;
        if (!_draggingTab) { _draggingTab = true; e.Pointer.Capture(Tabs); }
        UpdateTabDrop(point);
        e.Handled = true;
    }

    private void UpdateTabDrop(Point point)
    {
        ClearTabDrop();
        if (!new Rect(Tabs.Bounds.Size).Contains(point)) return;
        foreach (var tab in _tabItems)
        {
            if (tab.TranslatePoint(default, Tabs) is not { } origin) continue;
            var bounds = new Rect(origin, tab.Bounds.Size);
            if (!bounds.Contains(point)) continue;
            if (tab == _dragTab) return;
            _dropTab = tab; _dropAfter = point.X >= bounds.Center.X;
            tab.Classes.Set(_dropAfter ? "drop-after" : "drop-before", true);
            return;
        }
    }

    private void TabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragTab is null || e.Pointer != _tabPointer || e.InitialPressMouseButton != MouseButton.Left) return;
        var dragging = _draggingTab;
        if (dragging) UpdateTabDrop(e.GetPosition(Tabs));
        var source = _dragTab; var target = _dropTab; var after = _dropAfter;
        CancelTabDrag();
        if (!dragging) return;
        e.Handled = true;
        if (target is null) return;
        Observe(ChangeLayoutAsync(() =>
        {
            var workspace = _workspaces.FirstOrDefault(w => w.Tab == source);
            var from = _tabItems.IndexOf(source); var to = _tabItems.IndexOf(target);
            if (workspace is not null && from >= 0 && to >= 0)
                MoveTabCore(workspace, to + (after ? 1 : 0) - (from < to ? 1 : 0));
            return Task.CompletedTask;
        }));
    }

    private void ClearTabDrop()
    {
        _dropTab?.Classes.Remove("drop-before"); _dropTab?.Classes.Remove("drop-after");
        _dropTab = null;
    }

    private bool CancelTabDrag()
    {
        var dragging = _draggingTab; var pointer = _tabPointer;
        _draggingTab = false; _dragTab = null; _tabPointer = null;
        ClearTabDrop();
        if (pointer?.Captured == Tabs) pointer.Capture(null);
        return dragging;
    }

    private void MoveTabLeftClicked(object? sender, RoutedEventArgs e) => Observe(MoveCurrentTabAsync(-1));
    private void MoveTabRightClicked(object? sender, RoutedEventArgs e) => Observe(MoveCurrentTabAsync(1));
}
