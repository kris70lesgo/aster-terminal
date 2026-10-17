using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Aster.App.Views;

/// <summary>Real window actions in the compact iTerm-style caption.</summary>
public sealed class WindowButtons : UserControl
{
    private Window? _window;
    private readonly Button[] _buttons;
    private static readonly string[] Colors = ["#FF5F57", "#FEBC2E", "#28C840"];
    public WindowButtons()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        string[] labels = ["Close window", "Minimize window", "Maximize window"];
        string[] shapes = ["M1,1 L7,7 M7,1 L1,7", "M1,4 L7,4", "M1,1 L7,1 L7,7 L1,7 Z"];
        _buttons = Enumerable.Range(0, 3).Select(i =>
        {
            var button = new Button { Name = "Caption" + i, Width = 14, Height = 14, MinWidth = 0, MinHeight = 0,
                Padding = default, CornerRadius = new CornerRadius(7), Focusable = false,
                Content = new Avalonia.Controls.Shapes.Path { Data = Geometry.Parse(shapes[i]), Stroke = Brush.Parse("#453B32"), StrokeThickness = 1, Width = 8, Height = 8 } };
            button.Bind(ThemeProperty, button.GetResourceObservable("CaptionButtonTheme"));
            ToolTip.SetTip(button, labels[i]); Avalonia.Automation.AutomationProperties.SetName(button, labels[i]);
            WindowDecorationProperties.SetElementRole(button, WindowDecorationsElementRole.User);
            panel.Children.Add(button); return button;
        }).ToArray();
        _buttons[0].Click += (_, _) => _window?.Close();
        _buttons[1].Click += (_, _) => { if (_window?.CanMinimize == true) _window.WindowState = WindowState.Minimized; };
        _buttons[2].Click += (_, _) => { if (_window?.CanMaximize == true) _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; };
        Content = panel;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null) _window.PropertyChanged += WindowChanged;
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_window is not null) _window.PropertyChanged -= WindowChanged;
        _window = null; base.OnDetachedFromVisualTree(e);
    }
    private void WindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.IsActiveProperty || e.Property == Window.CanMinimizeProperty || e.Property == Window.CanMaximizeProperty) Update();
    }
    private void Update()
    {
        for (var i = 0; i < 3; i++) _buttons[i].Background = Brush.Parse(_window?.IsActive == true ? Colors[i] : "#444450");
        _buttons[1].IsEnabled = _window?.CanMinimize == true; _buttons[2].IsEnabled = _window?.CanMaximize == true;
    }

    public static void EnableCaptionDrag(Control caption)
    {
        WindowDecorationProperties.SetElementRole(caption, WindowDecorationsElementRole.User);
        caption.PointerPressed += (_, args) =>
        {
            if (args.Source is not Visual source || source.GetSelfAndVisualAncestors().Any(v => v is Button or TabItem) ||
                !args.GetCurrentPoint(caption).Properties.IsLeftButtonPressed || TopLevel.GetTopLevel(caption) is not Window window) return;
            if (args.ClickCount == 2 && window.CanMaximize) window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else window.BeginMoveDrag(args);
            args.Handled = true;
        };
    }
}
