using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;
using Devolutions.Terminal.Connection;

namespace Aster.Sessions.Hosting;

public sealed class HostedConnection : IRestartableTerminalConnection
{
    private readonly SessionCatalog _catalog;
    private readonly string _profileId, _name;
    private NamedPipeClientStream? _pipe;
    private Process? _host;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _send = new(1, 1);
    private readonly Channel<HostFrame> _input = Channel.CreateBounded<HostFrame>(128);
    private Task? _reader, _writer, _close;
    private bool _attached, _detaching, _disposed, _supportsDetachAck;
    private readonly TaskCompletionSource _detachAck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _queuedBytes;
    public HostedSession? Session { get; private set; }
    public bool WasRecovered { get; }
    internal bool SupportsDetachAck => _supportsDetachAck;
    internal bool DetachAcknowledged => _detachAck.Task.IsCompletedSuccessfully;
    public string? RecoveryNotice { get; private set; }
    public event EventHandler? Restoring;
    public event EventHandler<(int Columns, int Rows, int PixelWidth, int PixelHeight)>? GridChanged;
    public (int Width, int Height) InitialCellPixels { get; set; } = (10, 20);
    private int _pixelWidth, _pixelHeight;
    public event EventHandler<ReadOnlyMemory<byte>>? OutputReceived;
    public event EventHandler<int>? Exited;
    public event EventHandler<TerminalExitInfo>? SessionExited;
    public event EventHandler<Exception>? Faulted;
    public bool IsRunning { get; private set; }
    public int Columns { get; private set; } = 80;
    public int Rows { get; private set; } = 24;
    public TerminalConnectionCapabilities Capabilities => TerminalConnectionCapabilities.Resize | TerminalConnectionCapabilities.ProcessMetadata;
    public TerminalConnectionState State { get; private set; } = TerminalConnectionState.NotConnected;
    public TerminalProcessMetadata? ProcessMetadata { get; private set; }
    public TerminalExitInfo? LastExitInfo { get; private set; }

    public HostedConnection(SessionCatalog catalog, string profileId, string name, HostedSession? recovered = null)
    {
        _catalog = catalog; _profileId = profileId; _name = name; Session = recovered; WasRecovered = recovered is not null;
    }
    public Task StartAsync(string commandLine, string? workingDirectory, int columns, int rows, CancellationToken cancellationToken = default) => StartAsync(new TerminalLaunchOptions { CommandLine = commandLine, WorkingDirectory = workingDirectory, Columns = columns, Rows = rows }, cancellationToken);
    public async Task StartAsync(TerminalLaunchOptions options, CancellationToken cancellationToken = default)
    {
        if (State != TerminalConnectionState.NotConnected) throw new InvalidOperationException("A hosted connection can only be started once.");
        State = TerminalConnectionState.Connecting;
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); startup.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            if (Session is null)
            {
                Session = _catalog.Create(_profileId, _name); _host = _catalog.Launch(Session);
            }
            while (true)
            {
                startup.Token.ThrowIfCancellationRequested();
                _pipe = SessionCatalog.CreatePipe(Session);
                try { await _pipe.ConnectAsync(250, startup.Token); break; }
                catch (TimeoutException) { await _pipe.DisposeAsync(); if (_host?.HasExited == true) throw new IOException("The session host exited during startup."); }
            }
            await SendAsync(HostProtocol.Hello, HostProtocol.Encode(new HostHello(Session.Token, "Attach", options, options.Columns * InitialCellPixels.Width, options.Rows * InitialCellPixels.Height)), startup.Token);
            var frame = await HostProtocol.Read(_pipe, startup.Token);
            if (frame.Kind != HostProtocol.Ready) throw new IOException("Invalid session host greeting.");
            var ready = HostProtocol.Decode<HostReady>(frame.Data);
            if (ready.Error is not null) throw new IOException(ready.Error);
            ProcessMetadata = ready.Process ?? throw new IOException("The session host has no running process.");
            _attached = true; _supportsDetachAck = ready.SupportsDetachAck; Columns = ready.Columns; Rows = ready.Rows;
            if (ready.Checkpointed) RecoveryNotice = "Recovered the same live shell. Older display output exceeded the 8 MiB journal; only a text checkpoint and newer output were restored. Graphics and full prior terminal state may need a redraw.";
            Restoring?.Invoke(this, EventArgs.Empty);
            while (true)
            {
                frame = await HostProtocol.Read(_pipe, startup.Token);
                if (frame.Kind == HostProtocol.ReplayEnd) break;
                HandleFrame(frame);
            }
            IsRunning = true; State = TerminalConnectionState.Connected;
            _reader = Task.Run(ReadLoopAsync); _writer = Task.Run(WriteLoopAsync);
        }
        catch
        {
            State = TerminalConnectionState.Failed;
            if (_pipe is not null) await _pipe.DisposeAsync();
            if (!WasRecovered && _host is not null)
            {
                try { if (!_host.HasExited) _host.Kill(); } catch (InvalidOperationException) { }
                if (Session is not null) File.Delete(_catalog.FilePath(Session.Id));
            }
            throw;
        }
    }
    public void DetachOnClose() => _detaching = true;
    /// <summary>Simulates loss of the UI transport; never sends a process termination request.</summary>
    public async Task DetachAsync()
    {
        _detaching = true;
        await CloseAsync();
    }
    public void Write(string text) => Write(Encoding.UTF8.GetBytes(text));
    public void Write(ReadOnlySpan<byte> data)
    {
        if (!IsRunning || _disposed) throw new InvalidOperationException("The hosted session is not connected.");
        foreach (var bytes in data.ToArray().Chunk(32768)) Queue(new(HostProtocol.Input, bytes));
    }
    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); Write(data.Span); return ValueTask.CompletedTask; }
    public void Resize(int columns, int rows) => Resize(columns, rows, 0, 0);
    public void Resize(int columns, int rows, int pixelWidth, int pixelHeight)
    {
        HostProtocol.ValidateGrid(columns, rows);
        if (!IsRunning || (columns == Columns && rows == Rows && pixelWidth == _pixelWidth && pixelHeight == _pixelHeight)) return;
        Queue(new(HostProtocol.Grid, HostProtocol.GridData(columns, rows, pixelWidth, pixelHeight)));
    }
    private void Queue(HostFrame frame)
    {
        var count = Interlocked.Add(ref _queuedBytes, frame.Data.Length);
        if (count > 4 * 1024 * 1024 || !_input.Writer.TryWrite(frame))
        {
            Interlocked.Add(ref _queuedBytes, -frame.Data.Length); throw new IOException("Hosted session input queue is full.");
        }
    }
    private async Task SendAsync(byte kind, byte[] data, CancellationToken token)
    {
        await _send.WaitAsync(token);
        try { await HostProtocol.Write(_pipe ?? throw new IOException("No host pipe."), kind, data, token); }
        finally { _send.Release(); }
    }
    private async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var frame in _input.Reader.ReadAllAsync(_lifetime.Token))
            {
                Interlocked.Add(ref _queuedBytes, -frame.Data.Length);
                await SendAsync(frame.Kind, frame.Data, _lifetime.Token);
            }
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ObjectDisposedException) { if (!_lifetime.IsCancellationRequested) ReportFault(error); }
    }
    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                var frame = await HostProtocol.Read(_pipe!, _lifetime.Token); HandleFrame(frame);
                if (frame.Kind is HostProtocol.Exit or HostProtocol.Detached) return;
            }
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ObjectDisposedException)
        { if (!_lifetime.IsCancellationRequested && LastExitInfo is null) ReportFault(error); }
    }
    private void HandleFrame(HostFrame frame)
    {
        switch (frame.Kind)
        {
            case HostProtocol.Output: OutputReceived?.Invoke(this, frame.Data); break;
            case HostProtocol.Grid:
                (Columns, Rows) = HostProtocol.ReadGrid(frame.Data); (_pixelWidth, _pixelHeight) = HostProtocol.ReadPixels(frame.Data); GridChanged?.Invoke(this, (Columns, Rows, _pixelWidth, _pixelHeight)); break;
            case HostProtocol.Detached: _detachAck.TrySetResult(); break;
            case HostProtocol.Exit:
                LastExitInfo = HostProtocol.Decode<TerminalExitInfo>(frame.Data); IsRunning = false; State = TerminalConnectionState.Closed;
                Exited?.Invoke(this, LastExitInfo.ExitCode ?? 0); SessionExited?.Invoke(this, LastExitInfo); break;
            default: throw new InvalidDataException("Unsupported host response.");
        }
    }
    private void ReportFault(Exception error) { IsRunning = false; State = TerminalConnectionState.Failed; Faulted?.Invoke(this, error); }
    public Task RestartAsync(TerminalLaunchOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException("Close this hosted pane and open a new shell to restart it.");
    public Task CloseAsync(CancellationToken cancellationToken = default) => _close ??= CloseCoreAsync(cancellationToken);
    private async Task CloseCoreAsync(CancellationToken token)
    {
        State = TerminalConnectionState.Closing;
        // Explicit detach preserves pending user input in order before the detach
        // request. Abrupt IPC loss cannot make this guarantee.
        _input.Writer.TryComplete();
        if (_writer is not null && !_lifetime.IsCancellationRequested)
            try { await _writer.WaitAsync(TimeSpan.FromSeconds(5), token); }
            catch (Exception error) when (error is IOException or OperationCanceledException or TimeoutException) { }
        var terminateSent = false;
        if (_attached && LastExitInfo is null && _pipe?.IsConnected == true)
        {
            try
            {
                await SendAsync(_detaching ? HostProtocol.Detach : HostProtocol.Terminate, [], token).WaitAsync(TimeSpan.FromSeconds(5), token); terminateSent = !_detaching;
                if (_detaching && _supportsDetachAck) await _detachAck.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or TimeoutException or ObjectDisposedException) { }
        }
        if (_attached && LastExitInfo is null && !_detaching && !terminateSent && Session is not null && File.Exists(_catalog.FilePath(Session.Id)))
        {
            try { await _catalog.TerminateAsync(Session); }
            catch (Exception error) when (error is IOException or OperationCanceledException)
            { if (File.Exists(_catalog.FilePath(Session.Id))) throw new IOException("The live host could not confirm termination.", error); }
        }
        _lifetime.Cancel(); _input.Writer.TryComplete();
        if (_pipe is not null) await _pipe.DisposeAsync();
        if (_reader is not null) await _reader; if (_writer is not null) await _writer;
        IsRunning = false; _attached = false; State = TerminalConnectionState.Closed;
        if (!_detaching && _host is not null)
        {
            try { await _host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); } catch (TimeoutException) { }
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return; await CloseAsync(); _disposed = true; _host?.Dispose(); State = TerminalConnectionState.Disposed;
    }
}
