using System.Text;
using System.Threading.Channels;
using Devolutions.Terminal.Connection;

namespace Aster.Sessions.Tmux;

public sealed class TmuxPaneConnection : IRestartableTerminalConnection
{
    private readonly TmuxController _controller;
    private readonly Channel<byte[]> _input = Channel.CreateBounded<byte[]>(128);
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _writer;
    private long _queued;
    private bool _started, _ready, _disposed;
    public TerminalConnectionCapabilities Capabilities => TerminalConnectionCapabilities.None;
    public TerminalConnectionState State { get; private set; } = TerminalConnectionState.NotConnected;
    public TerminalProcessMetadata? ProcessMetadata => null;
    public TerminalExitInfo? LastExitInfo { get; private set; }
    public event EventHandler<TerminalExitInfo>? SessionExited;
    public int Id { get; }
    public bool IsRunning { get; private set; }
    public int Columns { get; private set; } = 80;
    public int Rows { get; private set; } = 24;
    public event EventHandler<ReadOnlyMemory<byte>>? OutputReceived;
    public event EventHandler<int>? Exited;
    public event EventHandler<Exception>? Faulted;
    public event EventHandler<(int Columns, int Rows)>? GridChanged;
    internal TmuxPaneConnection(TmuxController controller, int id) { _controller = controller; Id = id; }
    public Task StartAsync(string commandLine, string? workingDirectory, int columns, int rows, CancellationToken cancellationToken = default) => StartAsync(new TerminalLaunchOptions { CommandLine = commandLine, Columns = columns, Rows = rows }, cancellationToken);
    public async Task StartAsync(TerminalLaunchOptions options, CancellationToken cancellationToken = default)
    {
        if (_started || _disposed) throw new InvalidOperationException("Tmux pane already started.");
        _started = true; State = TerminalConnectionState.Connecting; await _controller.HydrateAsync(this, cancellationToken);
        IsRunning = true; State = TerminalConnectionState.Connected; _writer = Task.Run(WriteLoopAsync);
    }
    internal void SetGrid(int columns, int rows)
    {
        if (Columns == columns && Rows == rows) return;
        Columns = columns; Rows = rows; GridChanged?.Invoke(this, (columns, rows));
    }
    internal void Hydrate(TmuxPaneState state, string[] lines)
    {
        Columns = state.Columns; Rows = state.Rows; GridChanged?.Invoke(this, (Columns, Rows));
        var text = new StringBuilder("\u001bc\u001b[?7l");
        if (state.Alternate) text.Append("\u001b[?1049h");
        text.Append("\u001b[2J");
        for (var y = 0; y < Math.Min(Rows, lines.Length); y++) text.Append($"\u001b[{y + 1};1H").Append(lines[y]);
        text.Append($"\u001b[0m\u001b[{Math.Clamp(state.CursorY + 1, 1, Rows)};{Math.Clamp(state.CursorX + 1, 1, Columns)}H\u001b[?7h");
        text.Append(state.CursorVisible ? "\u001b[?25h" : "\u001b[?25l");
        text.Append(state.ApplicationCursor ? "\u001b[?1h" : "\u001b[?1l");
        OutputReceived?.Invoke(this, Encoding.UTF8.GetBytes(text.ToString())); _ready = true;
    }
    internal void Receive(byte[] bytes) { if (_ready && !_disposed) OutputReceived?.Invoke(this, bytes); }
    internal void Fail(Exception error) { if (_disposed) return; IsRunning = false; Faulted?.Invoke(this, error); End(); }
    internal void End()
    {
        if (_disposed || LastExitInfo is not null) return; IsRunning = false; State = TerminalConnectionState.Closed;
        _lifetime.Cancel(); _input.Writer.TryComplete(); LastExitInfo = new(null, 0, TerminalExitReason.Closed, false, DateTimeOffset.UtcNow);
        Exited?.Invoke(this, 0); SessionExited?.Invoke(this, LastExitInfo);
    }
    public Task RestartAsync(TerminalLaunchOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException("Reconnect using a new tmux control client.");
    public async Task CloseAsync(CancellationToken cancellationToken = default) => await DisposeAsync();
    public void Write(string text) => Write(Encoding.UTF8.GetBytes(text));
    public void Write(ReadOnlySpan<byte> data)
    {
        if (!IsRunning || _disposed) throw new InvalidOperationException("Tmux pane is detached.");
        foreach (var bytes in data.ToArray().Chunk(32768))
        {
            if (Interlocked.Add(ref _queued, bytes.Length) > 4 * 1024 * 1024 || !_input.Writer.TryWrite(bytes))
            { Interlocked.Add(ref _queued, -bytes.Length); throw new IOException("Tmux input queue is full."); }
        }
    }
    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); Write(data.Span); return ValueTask.CompletedTask; }
    private async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var bytes in _input.Reader.ReadAllAsync(_lifetime.Token))
            { Interlocked.Add(ref _queued, -bytes.Length); await _controller.SendAsync(Id, bytes, _lifetime.Token); }
        }
        catch (Exception error) when (error is IOException or OperationCanceledException) { if (!_lifetime.IsCancellationRequested) Fail(error); }
    }
    // The control client advertises the complete native window size, not the
    // size of each pane. tmux owns layout and applies its resulting PTY sizes.
    public void Resize(int columns, int rows) { }
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return; _disposed = true; IsRunning = false;
        _input.Writer.TryComplete();
        if (_writer is not null && !_lifetime.IsCancellationRequested)
            try { await _writer.WaitAsync(TimeSpan.FromSeconds(5)); } catch (TimeoutException) { }
        _controller.RemovePane(this); _lifetime.Cancel();
        if (_writer is not null) await _writer; State = TerminalConnectionState.Disposed;
    }
}
