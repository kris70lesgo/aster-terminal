using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Devolutions.Terminal.Connection;
using Aster.Sessions.Protocols;

namespace Aster.Sessions.Hosting;

/// <summary>Headless owner of one native PTY. A lost UI pipe never closes its shell.</summary>
public sealed class SessionHost
{
    private readonly HostedSession _session;
    private readonly string _descriptor;
    private readonly object _gate = new();
    private readonly IRestartableTerminalConnection _native = PlatformConnectionFactory.Create();
    private readonly ItermTerminalEngine _engine = new();
    private readonly HostTranscript _transcript = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Channel<HostFrame>? _subscriber;
    private CancellationTokenSource? _attachment;
    private bool _started, _attached;
    private DateTime _detachedSince = DateTime.UtcNow;

    private SessionHost(string path)
    {
        _descriptor = Path.GetFullPath(path); _session = SessionCatalog.Read(path);
        _native.OutputReceived += (_, data) =>
        {
            lock (_gate)
            {
                _engine.Feed(data.Span);
                foreach (var chunk in data.ToArray().Chunk(32768)) Publish(new(HostProtocol.Output, chunk));
            }
        };
        _engine.ResponseReady += (_, bytes) => { if (_native.IsRunning) _native.Write(bytes); };
        _native.SessionExited += (_, info) =>
        {
            lock (_gate) _subscriber?.Writer.TryWrite(new(HostProtocol.Exit, HostProtocol.Encode(info)));
            _ended.TrySetResult();
        };
        _native.Faulted += (_, _) => _ended.TrySetResult();
    }
    public static async Task<int> RunAsync(string descriptor)
    {
        var host = new SessionHost(descriptor);
        return await host.RunCoreAsync();
    }
    private async Task<int> RunCoreAsync()
    {
        new SessionCatalog(Path.GetDirectoryName(_descriptor)!).Write(_session with { HostProcessId = Environment.ProcessId });
        var listener = ListenAsync();
        try
        {
            while (!_ended.Task.IsCompleted)
            {
                await Task.WhenAny(_ended.Task, Task.Delay(1000));
                lock (_gate)
                {
                    var limit = _started ? TimeSpan.FromHours(24) : TimeSpan.FromSeconds(20);
                    if (!_attached && DateTime.UtcNow - _detachedSince > limit) _ended.TrySetResult();
                }
            }
            // Let the attached writer deliver a natural process exit before closing IPC.
            await Task.Delay(150);
            return 0;
        }
        finally
        {
            _stop.Cancel();
            lock (_gate) _attachment?.Cancel();
            await _native.DisposeAsync(); _engine.Dispose();
            try { await listener; } catch (OperationCanceledException) { }
            try { File.Delete(_descriptor); } catch (IOException) { }
        }
    }
    private async Task ListenAsync()
    {
        using var slots = new SemaphoreSlim(4);
        var handlers = new List<Task>();
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await slots.WaitAsync(_stop.Token);
                var pipe = new NamedPipeServerStream(_session.PipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 65536, 65536);
                try { await pipe.WaitForConnectionAsync(_stop.Token); }
                catch { await pipe.DisposeAsync(); slots.Release(); throw; }
                handlers.RemoveAll(t => t.IsCompleted);
                handlers.Add(Task.Run(async () => { try { await HandleAsync(pipe); } finally { slots.Release(); } }));
            }
        }
        catch (OperationCanceledException) { }
        finally { await Task.WhenAll(handlers); }
    }
    private async Task HandleAsync(NamedPipeServerStream pipe)
    {
        await using var owned = pipe;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token); timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            var greeting = await HostProtocol.Read(pipe, timeout.Token);
            if (greeting.Kind != HostProtocol.Hello) return;
            var hello = HostProtocol.Decode<HostHello>(greeting.Data);
            if (hello.Token is null || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hello.Token), Encoding.ASCII.GetBytes(_session.Token))) return;
            if (hello.Action == "Info")
            {
                HostReady info; lock (_gate) info = Ready(_started && _native.ProcessMetadata is not null ? null : "Starting");
                await HostProtocol.Write(pipe, HostProtocol.Ready, HostProtocol.Encode(info), timeout.Token); return;
            }
            if (hello.Action == "Terminate")
            {
                await HostProtocol.Write(pipe, HostProtocol.Ready, HostProtocol.Encode(Ready()), timeout.Token);
                await _native.CloseAsync(); _ended.TrySetResult(); return;
            }
            if (hello.Action != "Attach") return;
            lock (_gate)
            {
                if (_attached) throw new InvalidOperationException("This session is already attached to another window.");
                _attached = true;
            }
            try { await AttachAsync(pipe, hello, _stop.Token); }
            finally
            {
                lock (_gate) { _subscriber = null; _attachment = null; _attached = false; _detachedSince = DateTime.UtcNow; }
            }
        }
        catch (InvalidOperationException error)
        {
            try { await HostProtocol.Write(pipe, HostProtocol.Ready, HostProtocol.Encode(Ready(error.Message)), timeout.Token); } catch (IOException) { }
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ArgumentException or System.Text.Json.JsonException) { }
    }
    private HostReady Ready(string? error = null) => new(_native.ProcessMetadata, _native.Columns, _native.Rows, _attached, _transcript.Checkpointed, error);
    private async Task AttachAsync(Stream pipe, HostHello hello, CancellationToken token)
    {
        var options = hello.Options;
        if (!_started)
        {
            if (options is null) throw new InvalidOperationException("No shell launch options were provided.");
            HostProtocol.ValidateGrid(options.Columns, options.Rows);
            if (options.CommandLine.Length is < 1 or > 4096 || options.Elevate) throw new ArgumentException("Invalid local shell launch.");
            var initial = HostProtocol.GridData(options.Columns, options.Rows, hello.PixelWidth, hello.PixelHeight);
            var pixels = HostProtocol.ReadPixels(initial);
            _engine.Resize(options.Columns, options.Rows, pixels.Width > 0 ? (double)pixels.Width / options.Columns : 10, pixels.Height > 0 ? (double)pixels.Height / options.Rows : 20);
            _transcript.Append(new(HostProtocol.Grid, initial), _engine);
            await _native.StartAsync(options, token);
            _native.Resize(options.Columns, options.Rows, pixels.Width, pixels.Height);
            _started = true;
        }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        var channel = Channel.CreateBounded<HostFrame>(new BoundedChannelOptions(128) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        IReadOnlyList<HostFrame> replay;
        lock (_gate)
        {
            replay = _transcript.Snapshot(); _subscriber = channel; _attachment = lifetime;
            if (!_native.IsRunning && _native.LastExitInfo is { } exited) channel.Writer.TryWrite(new(HostProtocol.Exit, HostProtocol.Encode(exited)));
        }
        await HostProtocol.Write(pipe, HostProtocol.Ready, HostProtocol.Encode(Ready()), token);
        var writer = WriteOutputAsync(pipe, replay, channel.Reader, lifetime.Token);
        var reader = ReadInputAsync(pipe, lifetime.Token);
        await Task.WhenAny(writer, reader);
        lifetime.Cancel(); channel.Writer.TryComplete();
        try { await Task.WhenAll(writer, reader); } catch (Exception error) when (error is IOException or OperationCanceledException) { }
    }
    private static async Task WriteOutputAsync(Stream pipe, IReadOnlyList<HostFrame> replay, ChannelReader<HostFrame> output, CancellationToken token)
    {
        foreach (var frame in replay) await HostProtocol.Write(pipe, frame.Kind, frame.Data, token);
        await HostProtocol.Write(pipe, HostProtocol.ReplayEnd, ReadOnlyMemory<byte>.Empty, token);
        await foreach (var frame in output.ReadAllAsync(token))
        {
            await HostProtocol.Write(pipe, frame.Kind, frame.Data, token);
            if (frame.Kind == HostProtocol.Exit) return;
        }
    }
    private async Task ReadInputAsync(Stream pipe, CancellationToken token)
    {
        while (true)
        {
            var frame = await HostProtocol.Read(pipe, token);
            switch (frame.Kind)
            {
                case HostProtocol.Input:
                    if (frame.Data.Length > 32768) throw new InvalidDataException("Input chunk is too large.");
                    await _native.WriteAsync(frame.Data, token); break;
                case HostProtocol.Grid:
                    var (columns, rows) = HostProtocol.ReadGrid(frame.Data);
                    var pixels = HostProtocol.ReadPixels(frame.Data);
                    lock (_gate)
                    {
                        if (pixels.Width > 0 && pixels.Height > 0) _engine.Resize(columns, rows, (double)pixels.Width / columns, (double)pixels.Height / rows);
                        else _engine.ResizeKeepingCellMetrics(columns, rows);
                        _native.Resize(columns, rows, pixels.Width, pixels.Height); Publish(frame);
                    } break;
                case HostProtocol.Detach: return;
                case HostProtocol.Terminate: await _native.CloseAsync(); _ended.TrySetResult(); return;
                default: throw new InvalidDataException("Unsupported host request.");
            }
        }
    }
    private void Publish(HostFrame frame)
    {
        _transcript.Append(frame, _engine);
        if (_subscriber is not null && !_subscriber.Writer.TryWrite(frame)) _attachment?.Cancel(); // disconnect a slow UI; never block the PTY or grow without bound
    }
}
