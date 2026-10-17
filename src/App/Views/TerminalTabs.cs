using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Threading;

namespace Aster.App.Views;

/// <summary>Shared compact caption/tab layout; sessions remain owned by panes.</summary>
public sealed class TerminalTabs : TabControl
{
    public static readonly StyledProperty<Control?> MenuBarProperty = AvaloniaProperty.Register<TerminalTabs, Control?>(nameof(MenuBar));
    public static readonly StyledProperty<Control?> FindContentProperty = AvaloniaProperty.Register<TerminalTabs, Control?>(nameof(FindContent));
    public static readonly StyledProperty<Control?> StatusContentProperty = AvaloniaProperty.Register<TerminalTabs, Control?>(nameof(StatusContent));
    public Control? MenuBar { get => GetValue(MenuBarProperty); set => SetValue(MenuBarProperty, value); }
    public Control? FindContent { get => GetValue(FindContentProperty); set => SetValue(FindContentProperty, value); }
    public Control? StatusContent { get => GetValue(StatusContentProperty); set => SetValue(StatusContentProperty, value); }
    public event EventHandler? NewTabRequested;
    private ScrollViewer? _viewport;
    public TerminalTabs()
    {
        this.Bind(ThemeProperty, this.GetResourceObservable("TerminalTabsTheme"));
        ItemsView.CollectionChanged += (_, _) => RefreshHeader();
        SelectionChanged += (_, args) =>
        {
            if (args.Source == this) Dispatcher.UIThread.Post(() => (SelectedItem as Control)?.BringIntoView(), DispatcherPriority.Loaded);
        };
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _viewport = e.NameScope.Find<ScrollViewer>("HeaderViewport");
        if (_viewport is not null) _viewport.SizeChanged += (_, _) => RefreshHeader();
        if (e.NameScope.Find<Button>("NewTabButton") is { } add) add.Click += (_, _) => NewTabRequested?.Invoke(this, EventArgs.Empty);
        if (e.NameScope.Find<Border>("WindowHeader") is { } header)
            WindowButtons.EnableCaptionDrag(header);
        RefreshHeader();
    }
    private void RefreshHeader()
    {
        if (_viewport is null || _viewport.Bounds.Width <= 0 || ItemCount == 0) return;
        var width = Math.Max(100, _viewport.Bounds.Width / ItemCount);
        foreach (var tab in Items.OfType<TabItem>()) tab.Width = width;
    }
}
