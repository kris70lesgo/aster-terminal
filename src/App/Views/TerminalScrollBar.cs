using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Devolutions.Terminal;

namespace Aster.App.Views;

/// <summary>Native scrollbar over the terminal's actual retained history.</summary>
public sealed class TerminalScrollBar : ScrollBar, IDisposable
{
    public const double GutterWidth = 12;
    private readonly TermControl _terminal;
    private bool _updating, _disposed;

    public TerminalScrollBar(TermControl terminal)
    {
        _terminal = terminal;
        Name = "TerminalHistoryScrollBar";
        Orientation = Orientation.Vertical;
        Width = GutterWidth; MinWidth = 0; MinHeight = 0;
        AllowAutoHide = false; Visibility = ScrollBarVisibility.Visible;
        Focusable = false;
        AutomationProperties.SetName(this, "Terminal scrollback");
        this.Bind(ThemeProperty, this.GetResourceObservable("TerminalScrollBarTheme"));
        _terminal.ViewportChanged += ViewportChanged;
        ValueChanged += PositionChanged;
        Refresh();
    }

    private void ViewportChanged(object? sender, EventArgs e) => Refresh();
    private void Refresh()
    {
        if (_disposed) return;
        _updating = true;
        try
        {
            var engine = _terminal.Engine;
            var history = engine.AlternateBufferActive ? 0 : engine.HistoryCount;
            Maximum = history;
            ViewportSize = Math.Max(1, engine.Rows);
            LargeChange = Math.Max(1, engine.Rows - 1); SmallChange = 1;
            Value = history - Math.Clamp(engine.ScrollOffset, 0, history);
            IsEnabled = history > 0;
        }
        finally { _updating = false; }
    }
    private void PositionChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_updating && !_disposed)
            _terminal.SetScrollOffset(Math.Clamp((int)Math.Round(Maximum - e.NewValue), 0, (int)Maximum));
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _terminal.ViewportChanged -= ViewportChanged;
        ValueChanged -= PositionChanged;
    }
}
