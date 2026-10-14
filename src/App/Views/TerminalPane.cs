using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Aster.Sessions;
using Aster.Sessions.Protocols;
using Aster.Sessions.ShellIntegration;
using Devolutions.Terminal;

namespace Aster.App.Views;

public sealed class TerminalPane : Border
{
    private readonly TextBlock _status = new() { FontSize = 11, Margin = new Thickness(8, 3), Foreground = Brushes.LightGray };
    private Task? _start;
    private Task? _close;
    public TermControl Terminal { get; }
    public TerminalProfile Profile { get; private set; }
    public string SessionTitle { get; private set; }
    public event EventHandler? Activated;
    public event EventHandler<string>? TitleChanged;

    public TerminalPane(TerminalProfile profile, bool light = false, bool copyOnSelect = false)
    {
        Profile = profile;
        SessionTitle = profile.Name;
        BorderThickness = new Thickness(1);
        BorderBrush = Brush.Parse("#343A44");
        Terminal = new TermControl(new ItermTerminalEngine(historySize: profile.HistorySize))
        {
            ConnectionFactory = _ => PlatformConnectionFactory.Create(),
            AccessibleName = $"{profile.Name} terminal",
            InteractionOptions = new() { CopyOnSelect = copyOnSelect }
        };
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        layout.Children.Add(Terminal);
        Grid.SetRow(_status, 1);
        layout.Children.Add(_status);
        Child = layout;
        Terminal.GotFocus += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        Terminal.TitleChanged += (_, title) => { SessionTitle = string.Concat(title.Where(c => !char.IsControl(c))).Trim(); TitleChanged?.Invoke(this, SessionTitle); };
        Terminal.SessionExited += (_, info) => _status.Text = $"Exited · code {info.ExitCode} · close this pane to dismiss";
        Terminal.InteractionError += (_, _) => _status.Text = "Terminal interaction failed; check process state.";
        Terminal.Engine.WorkingDirectoryChanged += (_, directory) => Dispatcher.UIThread.Post(() =>
            _status.Text = $"{profile.Name} · {directory}");
        Terminal.ConfirmPasteAsync = request => TopLevel.GetTopLevel(this) is Window owner
            ? DialogWindow.Confirm(owner, "Paste into shell", $"Paste {request.LineCount} lines ({request.CharacterCount} characters) into this shell?", "Paste")
            : Task.FromResult(false);
        _light = light;
        _status.Foreground = Brush.Parse(light ? "#455368" : "#B5BEC9");
        Terminal.SizeChanged += (_, _) =>
        {
            if (Terminal.IsRunning) _status.Text = $"{profile.Name} · {Terminal.Engine.Columns} × {Terminal.Engine.Rows}";
        };
    }
    private bool _light;

    public Task StartAsync() => _start ??= StartCoreAsync();
    private async Task StartCoreAsync()
    {
        try
        {
            var settings = ShellIntegrationLauncher.Prepare(Profile, _light);
            var cell = TermControl.MeasureCell(settings);
            var columns = Math.Max(2, (int)((Bounds.Width - 20) / cell.Width));
            var rows = Math.Max(2, (int)((Bounds.Height - 40) / cell.Height));
            await Terminal.StartAsync(settings, columns, rows);
            Terminal.Engine.Scheme = Profile.ResolveColors(_light);
            _status.Text = $"{Profile.Name} · {Terminal.Engine.Columns} × {Terminal.Engine.Rows}";
            Terminal.Focus();
        }
        catch (Exception error)
        {
            _status.Text = $"Shell startup failed: {error.Message}";
        }
    }

    public void SetActive(bool active) => BorderBrush = Brush.Parse(active ? "#7EA8D8" : "#343A44");
    public void ApplyAppearance(TerminalProfile profile, bool light, bool copyOnSelect)
    {
        Profile = profile; _light = light;
        Terminal.InteractionOptions = Terminal.InteractionOptions with { CopyOnSelect = copyOnSelect };
        if (_close is null && Terminal.Profile is not null)
        {
            Terminal.Engine.Scheme = profile.ResolveColors(light);
            Terminal.ApplyAppearance(profile.ToControlProfile(light));
            Terminal.ResetFontSize();
        }
        _status.Foreground = Brush.Parse(light ? "#455368" : "#B5BEC9");
    }
    public Task CloseAsync() => _close ??= CloseCoreAsync();
    private async Task CloseCoreAsync()
    {
        if (_start is not null) await _start;
        // The dependency disposes its renderer after an off-thread connection await.
        // Detach and drain queued UI render work first, so a visible control cannot
        // enter Render during the renderer's disposal/flag update interval.
        Terminal.IsVisible = false;
        if (Terminal.Parent is Panel parent) parent.Children.Remove(Terminal);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Terminal.CloseAsync();
    }
}
