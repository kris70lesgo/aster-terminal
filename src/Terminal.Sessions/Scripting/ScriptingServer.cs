using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.Sessions.Scripting;

public sealed record ScriptingNotification(API.NotificationType Type, API.Notification Value);
public interface IScriptingTarget
{
    event EventHandler<ScriptingNotification>? Changed;
    Task<API.ServerOriginatedMessage> HandleAsync(API.ClientOriginatedMessage request, CancellationToken token);
}

/// <summary>Opt-in original iTerm2 wire protocol. No network listener exists until StartAsync is called.</summary>
public sealed class ScriptingServer : IAsyncDisposable
{
    public const int MaximumRequest = 256 * 1024, MaximumResponse = 1024 * 1024;
    private readonly IScriptingTarget _target;
    private readonly string _directory;
    private readonly int _requestedPort;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _slots = new(4);
    private readonly ConcurrentDictionary<Guid, Peer> _peers = [];
    private FileStream? _lease;
    private WebApplication? _application;
    private Task? _close;
    public ScriptingEndpoint? Endpoint { get; private set; }
    public ScriptingServer(IScriptingTarget target, string settingsDirectory, int port = 1912)
    {
        if (port is < 0 or > 65535 || port is > 0 and < 1024) throw new ArgumentOutOfRangeException(nameof(port));
        _target = target; _directory = Path.GetFullPath(settingsDirectory); _requestedPort = port;
    }
    public async Task StartAsync(CancellationToken token = default)
    {
        if (_application is not null || _close is not null) throw new InvalidOperationException("API server already started or disposed.");
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ApplicationName = typeof(ScriptingServer).Assembly.GetName().Name, ContentRootPath = AppContext.BaseDirectory });
        // Desktop launch environment / appsettings cannot add public endpoints.
        builder.Configuration.Sources.Clear(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Listen(IPAddress.Loopback, _requestedPort, endpoint => endpoint.Protocols = HttpProtocols.Http1);
            options.Limits.MaxConcurrentConnections = 16; options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeaderCount = 32; options.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
        });
        _application = builder.Build();
        _application.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30), KeepAliveTimeout = TimeSpan.FromSeconds(30) });
        _application.Run(HandleConnection);
        try
        {
            _lease = ScriptingEndpoint.AcquireLease(_directory);
            await _application.StartAsync(token).ConfigureAwait(false);
            var addresses = _application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
            var port = new Uri(addresses.Single()).Port;
            Endpoint = ScriptingEndpoint.Create(port); Endpoint.Save(_directory); _target.Changed += Changed;
        }
        catch { await DisposeAsync().ConfigureAwait(false); throw; }
    }
    private bool Authenticated(HttpContext context)
    {
        var endpoint = Endpoint;
        if (endpoint is null || context.Connection.RemoteIpAddress is not { } address || !IPAddress.IsLoopback(address)) return false;
        var host = context.Request.Host;
        if (host.Port != endpoint.Port || !(host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Host == "127.0.0.1")) return false;
        if (context.Request.Headers.Origin.ToString() != "ws://localhost/") return false;
        var candidate = context.Request.Headers["x-iterm2-cookie"].ToString();
        return candidate.Length == 64 && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(endpoint.Token));
    }
    private async Task HandleConnection(HttpContext context)
    {
        if (context.Request.Path != "/" || context.Request.Method != "GET") { context.Response.StatusCode = 404; return; }
        if (!Authenticated(context)) { context.Response.StatusCode = 401; return; }
        if (!context.WebSockets.IsWebSocketRequest || !context.WebSockets.WebSocketRequestedProtocols.Contains("api.iterm2.com")) { context.Response.StatusCode = 400; return; }
        if (!await _slots.WaitAsync(0, _lifetime.Token)) { context.Response.StatusCode = 503; return; }
        var id = Guid.NewGuid(); Peer? peer = null;
        try
        {
            context.Response.Headers["X-iTerm2-Protocol-Version"] = "1.0";
            var socket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext { SubProtocol = "api.iterm2.com", DangerousEnableCompression = false });
            peer = new(socket, _target, _lifetime.Token); _peers.TryAdd(id, peer); await peer.RunAsync();
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException or IOException) { }
        finally { _peers.TryRemove(id, out _); if (peer is not null) await peer.DisposeAsync(); _slots.Release(); }
    }
    private void Changed(object? sender, ScriptingNotification notification)
    {
        foreach (var peer in _peers.Values) peer.Publish(notification);
    }
    public ValueTask DisposeAsync() => new(_close ??= CloseAsync());
    private async Task CloseAsync()
    {
        Endpoint?.Remove(_directory); _target.Changed -= Changed; _lifetime.Cancel();
        foreach (var peer in _peers.Values) peer.Abort();
        if (_application is not null)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await _application.StopAsync(deadline.Token).ConfigureAwait(false); } catch (OperationCanceledException) { }
            await _application.DisposeAsync().ConfigureAwait(false);
        }
        _lease?.Dispose(); _lease = null;
    }
    private sealed class Peer : IAsyncDisposable
    {
        private readonly WebSocket _socket;
        private readonly IScriptingTarget _target;
        private readonly CancellationTokenSource _stop;
        private readonly Channel<byte[]> _outgoing = Channel.CreateBounded<byte[]>(32);
        private readonly HashSet<API.NotificationType> _subscriptions = [];
        private readonly object _gate = new();
        private int _aborted, _disposed;
        private readonly TokenBucketRateLimiter _rate = new(new TokenBucketRateLimiterOptions { TokenLimit = 64, TokensPerPeriod = 32, ReplenishmentPeriod = TimeSpan.FromSeconds(1), AutoReplenishment = true, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst });
        public Peer(WebSocket socket, IScriptingTarget target, CancellationToken lifetime)
        { _socket = socket; _target = target; _stop = CancellationTokenSource.CreateLinkedTokenSource(lifetime); }
        private static bool Supported(API.NotificationType type) => type is API.NotificationType.NotifyOnNewSession or API.NotificationType.NotifyOnTerminateSession or API.NotificationType.NotifyOnLayoutChange or API.NotificationType.NotifyOnFocusChange or API.NotificationType.NotifyOnBroadcastChange;
        public void Publish(ScriptingNotification notification)
        {
            lock (_gate) if (_subscriptions.Contains(notification.Type)) Queue(new API.ServerOriginatedMessage { Notification = notification.Value });
        }
        private void Queue(API.ServerOriginatedMessage message)
        {
            if (Volatile.Read(ref _aborted) != 0) return;
            if (message.CalculateSize() > MaximumResponse || !_outgoing.Writer.TryWrite(message.ToByteArray())) Abort();
        }
        public async Task RunAsync()
        {
            var writer = WriteAsync();
            try
            {
                var chunk = new byte[16384];
                while (!_stop.IsCancellationRequested)
                {
                    using var frame = new MemoryStream(); ValueWebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(chunk.AsMemory(), _stop.Token);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        if (result.MessageType != WebSocketMessageType.Binary || frame.Length + result.Count > MaximumRequest) { Abort(); return; }
                        frame.Write(chunk, 0, result.Count);
                    } while (!result.EndOfMessage);
                    using var lease = _rate.AttemptAcquire(); if (!lease.IsAcquired) { Abort(); return; }
                    API.ClientOriginatedMessage request;
                    try { request = API.ClientOriginatedMessage.Parser.ParseFrom(frame.ToArray()); }
                    catch (InvalidProtocolBufferException) { Abort(); return; }
                    if (!request.HasId || request.SubmessageCase == API.ClientOriginatedMessage.SubmessageOneofCase.None) { Abort(); return; }
                    API.ServerOriginatedMessage response;
                    if (request.NotificationRequest is { } subscription)
                    {
                        var status = API.NotificationResponse.Types.Status.RequestMalformed;
                        if (subscription.HasSubscribe && subscription.HasNotificationType && Supported(subscription.NotificationType) && subscription.ArgumentsCase == API.NotificationRequest.ArgumentsOneofCase.None)
                        {
                            lock (_gate)
                            {
                                var changed = subscription.Subscribe ? _subscriptions.Add(subscription.NotificationType) : _subscriptions.Remove(subscription.NotificationType);
                                status = changed ? API.NotificationResponse.Types.Status.Ok : subscription.Subscribe ? API.NotificationResponse.Types.Status.AlreadySubscribed : API.NotificationResponse.Types.Status.NotSubscribed;
                            }
                        }
                        response = new() { NotificationResponse = new() { Status = status } };
                    }
                    else
                    {
                        // A desktop action may need the UI dispatcher to resume.
                        // Stopping the transport must not wait for that continuation
                        // when the UI is already inside its Exit event.
                        try { response = await _target.HandleAsync(request, _stop.Token).WaitAsync(_stop.Token); }
                        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { return; }
                        catch (Exception) { response = new() { Error = "The request could not be completed." }; }
                    }
                    response.Id = request.Id;
                    if (response.CalculateSize() > MaximumResponse) response = new() { Id = request.Id, Error = "Response exceeds Aster's 1 MiB scripting limit." };
                    Queue(response);
                }
            }
            catch (Exception error) when (error is WebSocketException or OperationCanceledException or IOException) { }
            finally { Abort(); _outgoing.Writer.TryComplete(); await writer; }
        }
        private async Task WriteAsync()
        {
            try
            {
                await foreach (var bytes in _outgoing.Reader.ReadAllAsync(_stop.Token))
                    await _socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Binary, true, _stop.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), _stop.Token);
            }
            catch (Exception error) when (error is WebSocketException or OperationCanceledException or TimeoutException) { Abort(); }
        }
        public void Abort()
        {
            if (Interlocked.Exchange(ref _aborted, 1) != 0) return;
            try { _stop.Cancel(); } catch (ObjectDisposedException) { }
            try { _socket.Abort(); } catch (ObjectDisposedException) { }
        }
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return ValueTask.CompletedTask;
            Abort(); _rate.Dispose(); _socket.Dispose(); _stop.Dispose(); return ValueTask.CompletedTask;
        }
    }
}
