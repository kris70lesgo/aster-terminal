using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Aster.App.Views;

/// <summary>Same caption for auxiliary windows, retaining their real close lifecycle.</summary>
public sealed class WindowCaption : Border
{
    private readonly TextBlock _title = new() { FontSize = 13, FontWeight = FontWeight.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis };
    private Window? _owner;
    public WindowCaption()
    {
        Height = 32; BorderThickness = new Thickness(0, 0, 0, 1);
        this.Bind(BackgroundProperty, this.GetResourceObservable("ChromeBackground"));
        this.Bind(BorderBrushProperty, this.GetResourceObservable("TabDivider"));
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("78,*,78") };
        grid.Children.Add(new WindowButtons()); Grid.SetColumn(_title, 1); grid.Children.Add(_title); Child = grid;
        WindowButtons.EnableCaptionDrag(this);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _owner = TopLevel.GetTopLevel(this) as Window;
        if (_owner is not null) { _title.Text = _owner.Title; _owner.PropertyChanged += TitleChanged; }
    }
    private void TitleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == Window.TitleProperty) _title.Text = _owner?.Title; }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= TitleChanged;
        _owner = null; base.OnDetachedFromVisualTree(e);
    }
    public static Control Wrap(Window window, Control content)
    {
        window.WindowDecorations = WindowDecorations.BorderOnly; window.ExtendClientAreaToDecorationsHint = true;
        var root = new Grid { RowDefinitions = new RowDefinitions("32,*") };
        root.Children.Add(new WindowCaption()); Grid.SetRow(content, 1); root.Children.Add(content); return root;
    }
}
