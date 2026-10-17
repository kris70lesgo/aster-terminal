using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Aster.Sessions;
using Aster.Sessions.Hosting;
using Aster.Sessions.Protocols;
using Aster.Sessions.ShellIntegration;
using Devolutions.Terminal;
using Devolutions.Terminal.Connection;
using Aster.Sessions.Tmux;

namespace Aster.App.Views;

public sealed class TerminalPane : Border
{
    internal ItermTerminalEngine ScriptingEngine => _engine;
    internal string ScriptingId { get; } = Guid.NewGuid().ToString("N");
    private readonly TextBlock _status = new() { FontSize = 11, Margin = new Thickness(8, 4), Foreground = Brushes.LightGray, IsVisible = false, TextWrapping = TextWrapping.Wrap };
    private readonly DispatcherTimer _replayTimer;
    private readonly Button _triggerHistory = new() { Content = "Triggered lines", IsVisible = false, FontSize = 11, MinHeight = 24, Padding = new Thickness(6, 2), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
    private readonly ItermTerminalEngine _engine;
    private readonly TerminalScrollBar _scrollbar;
    private readonly HostedConnection? _hosted;
    public HostedSession? HostedSession => _hosted?.Session;
    public bool IsHostGridSynchronized => _hosted is null || (_hosted.Columns == Terminal.Engine.Columns && _hosted.Rows == Terminal.Engine.Rows);
    public bool IsHosted => _hosted is not null;
    public void DetachOnClose() => _hosted?.DetachOnClose();
    private bool _active;
    private Task? _start;
    private Task? _close;
    public Aster.App.Services.PaneFindController Find { get; }
    public TermControl Terminal { get; }
    public Aster.Sessions.Replay.ScreenReplayBuffer Replay { get; } = new();
    public TerminalProfile Profile { get; private set; }
    public string SessionTitle { get; private set; }
    public event EventHandler? Activated;
    public event EventHandler<string>? TitleChanged;

    public TerminalPane(TerminalProfile profile, bool light = false, bool copyOnSelect = false, bool replayEnabled = true, SessionCatalog? sessions = null, HostedSession? recovered = null, IRestartableTerminalConnection? connection = null)
    {
        Profile = profile;
        SessionTitle = profile.Name;
        BorderThickness = new Thickness(0);
        _engine = new ItermTerminalEngine(historySize: profile.HistorySize);
        _engine.ConfigureTriggers(profile.Triggers);
        _engine.TriggersChanged += TriggersChanged;
        if (connection is TmuxPaneConnection tmux)
        {
            _engine.ProtocolResponsesEnabled = false; _engine.GridOwnedExternally = true;
            tmux.GridChanged += (_, grid) => _engine.ResizeKeepingCellMetrics(grid.Columns, grid.Rows);
        }
        if (sessions is not null && connection is null)
        {
            _engine.ProtocolResponsesEnabled = false;
            _hosted = new(sessions, profile.Id, profile.Name, recovered);
            _hosted.Restoring += (_, _) => _engine.Reset();
            _hosted.GridChanged += (_, grid) =>
            {
                if (grid.PixelWidth > 0 && grid.PixelHeight > 0) _engine.Resize(grid.Columns, grid.Rows, (double)grid.PixelWidth / grid.Columns, (double)grid.PixelHeight / grid.Rows);
                else _engine.ResizeKeepingCellMetrics(grid.Columns, grid.Rows);
            };
        }
        Terminal = new TermControl(_engine)
        {
            ConnectionFactory = _ => connection ?? (_hosted is not null ? _hosted : PlatformConnectionFactory.Create()),
            AccessibleName = $"{profile.Name} terminal",
            InteractionOptions = new() { CopyOnSelect = copyOnSelect }
        };
        Find = new(_engine, Terminal);
        _scrollbar = new(Terminal);
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto,Auto") };
        var viewport = new Grid { ColumnDefinitions = new ColumnDefinitions("*," + TerminalScrollBar.GutterWidth) };
        viewport.Children.Add(Terminal); Grid.SetColumn(_scrollbar, 1); viewport.Children.Add(_scrollbar);
        layout.Children.Add(viewport);
        Grid.SetRow(_status, 1);
        layout.Children.Add(_status);
        Grid.SetRow(_triggerHistory, 2); layout.Children.Add(_triggerHistory);
        _triggerHistory.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window owner) _ = new TriggerHistoryWindow(_engine, () => Terminal.Focus()) { RequestedThemeVariant = owner.ActualThemeVariant }.ShowDialog(owner);
        };
        Child = layout;
        Terminal.GotFocus += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        Terminal.TitleChanged += (_, title) => { SessionTitle = string.Concat(title.Where(c => !char.IsControl(c))).Trim(); TitleChanged?.Invoke(this, SessionTitle); };
        Terminal.SessionExited += (_, info) => ShowMessage($"Session ended ({info.ExitCode}). Close this pane to dismiss.");
        Terminal.InteractionError += (_, _) => ShowMessage("Terminal interaction failed; check process state.");
        Terminal.ConfirmPasteAsync = request => TopLevel.GetTopLevel(this) is Window owner
            ? DialogWindow.Confirm(owner, "Paste into shell", $"Paste {request.LineCount} lines ({request.CharacterCount} characters) into this shell?", "Paste")
            : Task.FromResult(false);
        _light = light;
        _status.Foreground = Brush.Parse(light ? "#455368" : "#B5BEC9");
        _replayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _replayTimer.Tick += (_, _) => CaptureReplay();
        SetReplayEnabled(replayEnabled);
    }
    private bool _light;
    private void TriggersChanged(object? sender, EventArgs args) => Dispatcher.UIThread.Post(() =>
    {
        if (_close is not null) return;
        var count = _engine.TriggerMatches.Count;
        _triggerHistory.Content = $"Triggered lines ({count})";
        _triggerHistory.IsVisible = count > 0 || _engine.TriggerWarning is not null;
    });

    public Task StartAsync() => _start ??= StartCoreAsync();
    private async Task StartCoreAsync()
    {
        try
        {
            var settings = ShellIntegrationLauncher.Prepare(Profile, _light);
            var cell = TermControl.MeasureCell(settings, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
            var columns = Math.Max(2, (int)((Terminal.Bounds.Width - 10) / cell.Width));
            var rows = Math.Max(2, (int)((Terminal.Bounds.Height - 10) / cell.Height));
            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            var pixelWidth = Math.Max(1, (int)Math.Round(cell.Width * scale)); var pixelHeight = Math.Max(1, (int)Math.Round(cell.Height * scale));
            if (_hosted is not null) _hosted.InitialCellPixels = (pixelWidth, pixelHeight);
            await Terminal.StartAsync(settings, columns, rows);
            _hosted?.Resize(columns, rows, columns * pixelWidth, rows * pixelHeight);
            Terminal.Engine.Scheme = Profile.ResolveColors(_light);
            _status.IsVisible = false;
            if (_hosted?.RecoveryNotice is { } notice) ShowMessage(notice);
            Terminal.Focus();
        }
        catch (Exception error)
        {
            ShowMessage($"Shell startup failed: {error.Message}");
        }
    }

    public void SetActive(bool active)
    {
        _active = active;
        Terminal.Opacity = active ? 1 : .92;
    }
    private void ShowMessage(string text) { _status.Text = text; _status.IsVisible = true; }
    private long _capturedRevision = -1;
    private bool _replayEnabled;
    public void SetReplayEnabled(bool enabled)
    {
        _replayEnabled = enabled;
        if (enabled && _close is null) _replayTimer.Start();
        else { _replayTimer.Stop(); Replay.Clear(); _capturedRevision = -1; }
    }
    public void CaptureReplay()
    {
        if (!_replayEnabled || _close is not null || Terminal.Profile is null || _capturedRevision == _engine.Revision) return;
        var (revision, snapshot, scheme) = _engine.CaptureScreen();
        Replay.Append(snapshot, scheme, DateTimeOffset.UtcNow);
        _capturedRevision = revision;
    }
    public void ApplyAppearance(TerminalProfile profile, bool light, bool copyOnSelect)
    {
        Profile = profile; _light = light;
        _engine.ConfigureTriggers(profile.Triggers);
        Terminal.InteractionOptions = Terminal.InteractionOptions with { CopyOnSelect = copyOnSelect };
        if (_close is null && Terminal.Profile is not null)
        {
            Terminal.Engine.Scheme = profile.ResolveColors(light);
            Terminal.ApplyAppearance(profile.ToControlProfile(light));
            Terminal.ResetFontSize();
        }
        _status.Foreground = Brush.Parse(light ? "#455368" : "#B5BEC9");
        Terminal.Opacity = _active ? 1 : .92;
    }
    public Task CloseAsync() => _close ??= CloseCoreAsync();
    private async Task CloseCoreAsync()
    {
        Find.Dispose();
        _scrollbar.Dispose();
        if (_start is not null) await _start;
        _replayTimer.Stop(); Replay.Clear();
        _engine.TriggersChanged -= TriggersChanged; _engine.ClearTriggerMatches();
        // The dependency disposes its renderer after an off-thread connection await.
        // Detach and drain queued UI render work first, so a visible control cannot
        // enter Render during the renderer's disposal/flag update interval.
        Terminal.IsVisible = false;
        if (Terminal.Parent is Panel parent) parent.Children.Remove(Terminal);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Terminal.CloseAsync();
    }
}
