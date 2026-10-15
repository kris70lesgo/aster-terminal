using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using Avalonia.Headless.XUnit;
using Aster.App.Services;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Aster.Sessions.Scripting;
using Google.Protobuf;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task AuthenticatedScriptingControlsRealTabsPanesAndReadsUnicodeWithoutMovingScrollback()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-api-desktop-" + Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(Path.Combine(directory, "settings.json"));
        var profile = new TerminalProfile("API shell", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='API_READY> '; exec /bin/sh", Path.GetTempPath()) { EnvironmentVariables = new() { ["PROMPT"] = "API_READY$G" } };
        store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, RestoreLayout = false, ConfirmClose = false });
        var window = new MainWindow(store); window.Show();
        using var target = new DesktopScriptingTarget(store, () => [window]);
        await using var server = new ScriptingServer(target, directory, 0);
        using var socket = new ClientWebSocket(); long id = 0; var notifications = new List<API.Notification>();
        try
        {
            await Until(() => Panes(window).Length == 1 && Text(Panes(window)[0]).Contains("API_READY>"));
            await server.StartAsync(); var endpoint = server.Endpoint!;
            socket.Options.AddSubProtocol("api.iterm2.com"); socket.Options.SetRequestHeader("Origin", "ws://localhost/"); socket.Options.SetRequestHeader("x-iterm2-cookie", endpoint.Token);
            await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{endpoint.Port}/"), TestContext.Current.CancellationToken);
            async Task<API.ServerOriginatedMessage> Rpc(API.ClientOriginatedMessage request)
            {
                request.Id = ++id; await socket.SendAsync(request.ToByteArray().AsMemory(), WebSocketMessageType.Binary, true, TestContext.Current.CancellationToken);
                while (true)
                {
                    using var message = new MemoryStream(); var bytes = new byte[16384]; ValueWebSocketReceiveResult read;
                    do { read = await socket.ReceiveAsync(bytes.AsMemory(), TestContext.Current.CancellationToken); Assert.Equal(WebSocketMessageType.Binary, read.MessageType); message.Write(bytes, 0, read.Count); } while (!read.EndOfMessage);
                    var response = API.ServerOriginatedMessage.Parser.ParseFrom(message.ToArray());
                    if (response.Notification is { } notification) { notifications.Add(notification); continue; }
                    Assert.Equal(request.Id, response.Id); Assert.False(response.HasError, response.Error); return response;
                }
            }
            var first = (await Rpc(new() { ListSessionsRequest = new() })).ListSessionsResponse.Windows.Single();
            var firstSession = first.Tabs.Single().ActiveSessionId;
            Assert.Equal(Panes(window)[0].ScriptingId, firstSession);
            await Rpc(new() { NotificationRequest = new() { Subscribe = true, NotificationType = API.NotificationType.NotifyOnLayoutChange } });
            var created = (await Rpc(new() { CreateTabRequest = new() { WindowId = first.WindowId, ProfileName = profile.Name, SelectTab = false } })).CreateTabResponse;
            Assert.Equal(API.CreateTabResponse.Types.Status.Ok, created.Status); Assert.NotEqual(firstSession, created.SessionId);
            var split = (await Rpc(new() { SplitPaneRequest = new() { Session = created.SessionId, SplitDirection = API.SplitPaneRequest.Types.SplitDirection.Horizontal, Before = true } })).SplitPaneResponse;
            Assert.Equal(API.SplitPaneResponse.Types.Status.Ok, split.Status); var session = split.SessionId.Single();
            var panes = Panes(window); var splitPane = panes.Single(p => p.ScriptingId == session);
            await Until(() => Text(splitPane).Contains("API_READY>"));
            var command = OperatingSystem.IsWindows() ? "echo __API_REAL__東京 é\r" : "printf '\\n__API_REAL__%s\\n' '東京 é'\n";
            await Rpc(new() { SendTextRequest = new() { Session = session, Text = command } });
            await Until(() => Text(splitPane).Contains("__API_REAL__東京 é"));
            splitPane.ScriptingEngine.Feed(string.Concat(Enumerable.Repeat("\r\nscroll line", 40)));
            splitPane.ScriptingEngine.SetScrollOffset(3); var offset = splitPane.ScriptingEngine.ScrollOffset;
            var contents = (await Rpc(new() { GetBufferRequest = new() { Session = session, LineRange = new() { ScreenContentsOnly = true }, IncludeStyles = true } })).GetBufferResponse;
            Assert.Equal(API.GetBufferResponse.Types.Status.Ok, contents.Status);
            Assert.Equal(splitPane.ScriptingEngine.Rows, contents.Contents.Count); Assert.Equal(offset, splitPane.ScriptingEngine.ScrollOffset);
            Assert.All(contents.Contents, line => { Assert.Equal(splitPane.ScriptingEngine.Columns, line.CodePointsPerCell.Sum(c => c.Repeats)); Assert.Equal(line.Text.EnumerateRunes().Count(), line.CodePointsPerCell.Sum(c => c.Repeats * c.NumCodePoints)); });
            var snapshot = (await Rpc(new() { ListSessionsRequest = new() })).ListSessionsResponse.Windows.Single();
            Assert.Equal(2, snapshot.Tabs.Count); Assert.Equal(2, snapshot.Tabs.Single(t => t.TabId == created.TabId.ToString()).Root.Links.Count);
            Assert.Contains(notifications, n => n.LayoutChangedNotification is not null);
            var close = (await Rpc(new() { CloseRequest = new() { Sessions = new() { SessionIds = { session } }, Force = true } })).CloseResponse;
            Assert.Equal(API.CloseResponse.Types.Status.Ok, close.Statuses.Single()); Assert.False(splitPane.Terminal.IsRunning);
            var bad = (await Rpc(new() { GetBufferRequest = new() { Session = firstSession, LineRange = new() { TrailingLines = 5 } } })).GetBufferResponse;
            Assert.Equal(API.GetBufferResponse.Types.Status.InvalidLineRange, bad.Status);
            var profiles = (await Rpc(new() { ListProfilesRequest = new() { Properties = { "Guid", "Name" } } })).ListProfilesResponse;
            Assert.Equal(profile.Id, JsonSerializer.Deserialize<string>(profiles.Profiles.Single().Properties.Single(p => p.Key == "Guid").JsonValue));
        }
        finally
        {
            socket.Abort(); await server.DisposeAsync(); window.Close();
            await Until(() => !window.IsVisible);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
