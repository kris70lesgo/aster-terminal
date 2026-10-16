using System.Net.WebSockets;
using Aster.Sessions.Scripting;
using Google.Protobuf;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.Tests;

public sealed class ScriptingServerTests
{
    private sealed class Target : IScriptingTarget
    {
        public int Calls;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<API.ServerOriginatedMessage>? Pending;
        public event EventHandler<ScriptingNotification>? Changed;
        public Task<API.ServerOriginatedMessage> HandleAsync(API.ClientOriginatedMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls); Entered.TrySetResult();
            if (Pending is { } pending) return pending.Task;
            return Task.FromResult(new API.ServerOriginatedMessage { ListSessionsResponse = new() });
        }
        public void Notify() => Changed?.Invoke(this, new(API.NotificationType.NotifyOnLayoutChange, new() { LayoutChangedNotification = new() { ListSessionsResponse = new() } }));
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "aster-api-tests-" + Guid.NewGuid().ToString("N"));
        public Target Target { get; } = new();
        public ScriptingServer Server { get; }
        public Fixture() { Server = new(Target, Root, 0); }
        public Task Start() => Server.StartAsync(TestContext.Current.CancellationToken);
        public ClientWebSocket Client(string? cookie = null, string origin = "ws://localhost/", bool protocol = true)
        {
            var client = new ClientWebSocket(); client.Options.SetRequestHeader("Origin", origin);
            client.Options.SetRequestHeader("x-iterm2-cookie", cookie ?? Server.Endpoint!.Token);
            if (protocol) client.Options.AddSubProtocol("api.iterm2.com"); return client;
        }
        public Task Connect(ClientWebSocket client) => client.ConnectAsync(new Uri($"ws://127.0.0.1:{Server.Endpoint!.Port}/"), TestContext.Current.CancellationToken);
        public async ValueTask DisposeAsync() { await Server.DisposeAsync(); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }
    [Fact]
    public async Task LoopbackCookieOriginAndSubprotocolAreRequiredAndCredentialsArePrivate()
    {
        await using var fixture = new Fixture(); await fixture.Start();
        var file = ScriptingEndpoint.FilePath(fixture.Root); var endpoint = ScriptingEndpoint.Read(file);
        Assert.Equal(fixture.Server.Endpoint, endpoint); Assert.True(endpoint.IsOwnerRunning()); Assert.Equal(64, endpoint.Token.Length);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(file));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(Path.GetDirectoryName(file)!));
        }
        using var wrong = fixture.Client(new string('0', 64)); await Assert.ThrowsAsync<WebSocketException>(() => fixture.Connect(wrong));
        using var origin = fixture.Client(origin: "https://untrusted.example"); await Assert.ThrowsAsync<WebSocketException>(() => fixture.Connect(origin));
        using var protocol = fixture.Client(protocol: false); await Assert.ThrowsAsync<WebSocketException>(() => fixture.Connect(protocol));
        Assert.Equal(0, fixture.Target.Calls);
        await fixture.Server.DisposeAsync(); Assert.False(File.Exists(file));
    }
    [Fact]
    public async Task OnlyOneApiOwnsCredentialDirectoryAndStoppingReleasesItsLease()
    {
        await using var fixture = new Fixture(); await fixture.Start();
        await using var duplicate = new ScriptingServer(fixture.Target, fixture.Root, 0);
        await Assert.ThrowsAsync<IOException>(() => duplicate.StartAsync(TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Server.Endpoint, ScriptingEndpoint.Read(ScriptingEndpoint.FilePath(fixture.Root)));
        using var client = fixture.Client(); await fixture.Connect(client);
        await Send(client, new() { Id = 1, ListSessionsRequest = new() }); Assert.NotNull((await Read(client)).ListSessionsResponse);
        client.Abort(); await fixture.Server.DisposeAsync();
        await using var replacement = new ScriptingServer(fixture.Target, fixture.Root, 0); await replacement.StartAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(fixture.Server.Endpoint!.Token, replacement.Endpoint!.Token);
    }
    [Fact]
    public async Task OriginalProtobufEnvelopesAndBoundedSubscriptionsRouteWithoutChangingRequestIds()
    {
        await using var fixture = new Fixture(); await fixture.Start(); using var client = fixture.Client(); await fixture.Connect(client);
        await Send(client, new() { Id = 123, ListSessionsRequest = new() }); var response = await Read(client);
        Assert.Equal(123, response.Id); Assert.NotNull(response.ListSessionsResponse); Assert.Equal(1, fixture.Target.Calls);
        await Send(client, new() { Id = 124, NotificationRequest = new() { Subscribe = true, NotificationType = API.NotificationType.NotifyOnKeystroke } });
        Assert.Equal(API.NotificationResponse.Types.Status.RequestMalformed, (await Read(client)).NotificationResponse.Status);
        await Send(client, new() { Id = 125, NotificationRequest = new() { Subscribe = true, NotificationType = API.NotificationType.NotifyOnLayoutChange } });
        Assert.Equal(API.NotificationResponse.Types.Status.Ok, (await Read(client)).NotificationResponse.Status);
        fixture.Target.Notify(); response = await Read(client); Assert.False(response.HasId); Assert.NotNull(response.Notification.LayoutChangedNotification);
    }
    [Fact]
    public async Task StoppingWithAnInFlightDesktopRequestDoesNotWaitForItsUiContinuation()
    {
        await using var fixture = new Fixture();
        fixture.Target.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await fixture.Start(); using var client = fixture.Client(); await fixture.Connect(client);
        await Send(client, new() { Id = 1, ListSessionsRequest = new() });
        await fixture.Target.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var stop = fixture.Server.DisposeAsync().AsTask();
        try
        {
            // The UI may be in its Exit event and unable to resume an API action.
            // Transport shutdown must not require that action's continuation.
            Assert.Same(stop, await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)));
            await stop;
            Assert.False(File.Exists(ScriptingEndpoint.FilePath(fixture.Root)));
            await using var replacement = new ScriptingServer(fixture.Target, fixture.Root, 0);
            await replacement.StartAsync(TestContext.Current.CancellationToken);
            Assert.NotEqual(fixture.Server.Endpoint!.Token, replacement.Endpoint!.Token);
        }
        finally
        {
            fixture.Target.Pending.TrySetResult(new() { ListSessionsResponse = new() });
            client.Abort(); await stop.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }
    [Fact]
    public async Task OversizeAndMalformedBinaryFramesDisconnectBeforeReachingTheDesktop()
    {
        await using var fixture = new Fixture(); await fixture.Start();
        foreach (var bytes in new[] { new byte[ScriptingServer.MaximumRequest + 1], new byte[] { 255, 255, 255 } })
        {
            using var client = fixture.Client(); await fixture.Connect(client);
            try
            {
                await client.SendAsync(bytes, WebSocketMessageType.Binary, true, TestContext.Current.CancellationToken);
                await Assert.ThrowsAnyAsync<WebSocketException>(async () => await Read(client));
            }
            catch (WebSocketException) { }
        }
        Assert.Equal(0, fixture.Target.Calls);
    }
    private static Task Send(ClientWebSocket client, API.ClientOriginatedMessage request) => client.SendAsync(request.ToByteArray(), WebSocketMessageType.Binary, true, TestContext.Current.CancellationToken);
    private static async Task<API.ServerOriginatedMessage> Read(ClientWebSocket client)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var data = new MemoryStream(); var chunk = new byte[16384]; WebSocketReceiveResult result;
        do
        {
            result = await client.ReceiveAsync(chunk, deadline.Token);
            if (result.MessageType == WebSocketMessageType.Close) throw new WebSocketException("Server disconnected.");
            data.Write(chunk, 0, result.Count);
        } while (!result.EndOfMessage);
        return API.ServerOriginatedMessage.Parser.ParseFrom(data.ToArray());
    }
}
