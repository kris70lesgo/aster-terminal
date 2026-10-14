using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Aster.Sessions;
using Aster.Sessions.Protocols;
using Devolutions.Terminal;

namespace Aster.App.Views;

public sealed class TerminalPane : Border
{
    private readonly TextBlock _status = new() { FontSize = 11, Margin = new Thickness(8, 3), Foreground = Brushes.LightGray };
    private Task? _start;
    private Task? _close;
    public TermControl Terminal { get; }
    public TerminalProfile Profile { get; }
    public event EventHandler? Activated;
    public event EventHandler<string>? TitleChanged;

    public TerminalPane(TerminalProfile profile)
    {
        Profile = profile;
        BorderThickness = new Thickness(1);
        BorderBrush = Brush.Parse("#343A44");
        Terminal = new TermControl(new ItermTerminalEngine())
        {
            ConnectionFactory = _ => PlatformConnectionFactory.Create(),
            AccessibleName = $"{profile.Name} terminal"
        };
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        layout.Children.Add(Terminal);
        Grid.SetRow(_status, 1);
        layout.Children.Add(_status);
        Child = layout;
        Terminal.GotFocus += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        Terminal.TitleChanged += (_, title) => TitleChanged?.Invoke(this, title);
        Terminal.SessionExited += (_, info) => _status.Text = $"Exited · code {info.ExitCode} · close this pane to dismiss";
        Terminal.InteractionError += (_, _) => _status.Text = "Terminal interaction failed; check process state.";
        Terminal.Engine.WorkingDirectoryChanged += (_, directory) => Dispatcher.UIThread.Post(() =>
            _status.Text = $"{profile.Name} · {directory}");
        Terminal.SizeChanged += (_, _) =>
        {
            if (Terminal.IsRunning) _status.Text = $"{profile.Name} · {Terminal.Engine.Columns} × {Terminal.Engine.Rows}";
        };
    }

    public Task StartAsync() => _start ??= StartCoreAsync();
    private async Task StartCoreAsync()
    {
        try
        {
            var settings = Profile.ToControlProfile();
            var cell = TermControl.MeasureCell(settings);
            var columns = Math.Max(2, (int)((Bounds.Width - 20) / cell.Width));
            var rows = Math.Max(2, (int)((Bounds.Height - 40) / cell.Height));
            await Terminal.StartAsync(settings, columns, rows);
            _status.Text = $"{Profile.Name} · {columns} × {rows}";
            Terminal.Focus();
        }
        catch (Exception error)
        {
            _status.Text = $"Shell startup failed: {error.Message}";
        }
    }

    public void SetActive(bool active) => BorderBrush = Brush.Parse(active ? "#7EA8D8" : "#343A44");
    public Task CloseAsync() => _close ??= CloseCoreAsync();
    private async Task CloseCoreAsync()
    {
        if (_start is not null) await _start;
        await Terminal.CloseAsync();
    }
}
