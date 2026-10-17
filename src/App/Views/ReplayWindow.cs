using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Aster.Sessions;
using Aster.Sessions.Replay;
using Aster.Terminal;
using Devolutions.Terminal.Core;

namespace Aster.App.Views;

/// <summary>Independent text-screen viewer. It never owns or writes a connection.</summary>
public sealed class ReplayWindow : Window
{
    private readonly IReadOnlyList<ScreenReplayFrame> _frames;
    private readonly ReplayTerminalEngine _engine = new();
    private readonly TermControl _screen;
    private readonly Slider _timeline = new() { Minimum = 0, TickFrequency = 1, IsSnapToTickEnabled = true };
    private readonly TextBlock _timestamp = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TerminalProfile _profile;
    private bool _closed, _closing;
    public int FrameCount => _frames.Count;
    public int SelectedFrameIndex { get; private set; }
    public ITerminalEngine DisplayEngine => _engine;

    public ReplayWindow(ScreenReplayBuffer buffer, TerminalProfile profile)
    {
        _frames = buffer.Snapshot(); _profile = profile;
        Title = "Instant Replay"; Width = 920; Height = 620; MinWidth = 500; MinHeight = 320;
        this.Bind(BackgroundProperty, this.GetResourceObservable("WindowBackground"));
        _screen = new TermControl(_engine) { Focusable = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        _screen.ApplyAppearance(profile.ToControlProfile()); _screen.ResetFontSize();
        _screen.IsHitTestVisible = false;
        var previous = new Button { Content = "←", MinWidth = 28 }; previous.Click += (_, _) => SelectFrame(SelectedFrameIndex - 1);
        var next = new Button { Content = "→", MinWidth = 28 }; next.Click += (_, _) => SelectFrame(SelectedFrameIndex + 1);
        var done = new Button { Content = "Done" }; done.Click += (_, _) => Close();
        var toolbar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto"), Margin = new Thickness(10, 7), ColumnSpacing = 8 };
        toolbar.Children.Add(previous); Grid.SetColumn(next, 1); toolbar.Children.Add(next);
        Grid.SetColumn(_timeline, 2); toolbar.Children.Add(_timeline); Grid.SetColumn(_timestamp, 3); toolbar.Children.Add(_timestamp);
        Grid.SetColumn(done, 4); toolbar.Children.Add(done);
        var scroll = new ScrollViewer { Content = _screen, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") }; root.Children.Add(scroll); Grid.SetRow(toolbar, 1); root.Children.Add(toolbar); Content = WindowCaption.Wrap(this, root);
        _timeline.Maximum = Math.Max(0, _frames.Count - 1); _timeline.IsEnabled = previous.IsEnabled = next.IsEnabled = _frames.Count > 1;
        _timeline.ValueChanged += (_, _) => SelectFrame((int)Math.Round(_timeline.Value));
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Left) { SelectFrame(SelectedFrameIndex - 1); e.Handled = true; }
            if (e.Key == Key.Right) { SelectFrame(SelectedFrameIndex + 1); e.Handled = true; }
            if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        }, RoutingStrategies.Tunnel);
        Opened += (_, _) => { Platform.MacWindowColorSpace.Configure(this); SelectFrame(SelectedFrameIndex); };
        Closing += async (_, e) =>
        {
            if (_closed) return; e.Cancel = true; if (_closing) return; _closing = true;
            _screen.IsVisible = false; scroll.Content = null;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            await _screen.CloseAsync(); _closed = true; Close();
        };
        if (_frames.Count == 0) _timestamp.Text = "No recorded frames yet. Replay can be enabled in Settings → General.";
        else SelectFrame(_frames.Count - 1);
    }

    public void SelectFrame(int index)
    {
        if (_frames.Count == 0 || _closing) return;
        SelectedFrameIndex = Math.Clamp(index, 0, _frames.Count - 1);
        var frame = _frames[SelectedFrameIndex]; var snapshot = frame.Snapshot; var buffer = snapshot.Buffer;
        _engine.ShowFrame(frame);
        var cell = TermControl.MeasureCell(_profile.ToControlProfile(), RenderScaling);
        _screen.Width = Math.Ceiling(buffer.Columns * cell.Width + 10); _screen.Height = Math.Ceiling(buffer.Rows * cell.Height + 4);
        _screen.InvalidateVisual();
        _timestamp.Text = $"{frame.Timestamp.ToLocalTime():HH:mm:ss.fff} · {SelectedFrameIndex + 1}/{_frames.Count}";
        if ((int)Math.Round(_timeline.Value) != SelectedFrameIndex) _timeline.Value = SelectedFrameIndex;
    }
}
