using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Aster.Sessions.Scripting;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.App.Services;

/// <summary>Dispatch authenticated script actions to the existing desktop/session controls.</summary>
public sealed class DesktopScriptingTarget : IScriptingTarget, IDisposable
{
    private readonly SettingsStore _store;
    private readonly SemaphoreSlim _requests = new(1, 1);
    private readonly Func<IReadOnlyList<MainWindow>> _windows;
    private readonly DispatcherTimer _changes;
    private API.ListSessionsResponse? _previous;
    private API.FocusResponse? _previousFocus;
    public event EventHandler<ScriptingNotification>? Changed;
    public DesktopScriptingTarget(SettingsStore store, Func<IReadOnlyList<MainWindow>>? windows = null)
    {
        _store = store; _windows = windows ?? (() => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.OfType<MainWindow>().ToArray() ?? []);
        _changes = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => PublishChanges());
    }
    private IReadOnlyList<MainWindow> Windows => _windows().Where(w => w.ScriptingReady).ToArray();
    private MainWindow? Current => Windows.FirstOrDefault(w => w.IsActive) ?? Windows.LastOrDefault();
    private TerminalPane? Pane(string id) => id == "active" ? Current?.ScriptingActivePane : Windows.SelectMany(w => w.ScriptingPanes).FirstOrDefault(p => p.ScriptingId == id);
    private MainWindow? Owner(TerminalPane pane) => Windows.FirstOrDefault(w => w.ScriptingPanes.Contains(pane));
    private API.ListSessionsResponse Snapshot()
    {
        var response = new API.ListSessionsResponse(); var number = 0;
        foreach (var window in Windows) { var value = window.ScriptingSnapshot(); value.Number = ++number; response.Windows.Add(value); }
        return response;
    }
    private API.FocusResponse Focus()
    {
        var response = new API.FocusResponse(); response.Notifications.Add(new API.FocusChangedNotification { ApplicationActive = Windows.Any(w => w.IsActive) });
        if (Current is { } current)
        {
            response.Notifications.Add(new API.FocusChangedNotification { Window = new() { WindowId = current.ScriptingWindowId, WindowStatus = current.IsActive ? API.FocusChangedNotification.Types.Window.Types.WindowStatus.TerminalWindowBecameKey : API.FocusChangedNotification.Types.Window.Types.WindowStatus.TerminalWindowIsCurrent } });
            foreach (var window in Windows)
            {
                if (window.ScriptingTabId is { } tab) response.Notifications.Add(new API.FocusChangedNotification { SelectedTab = tab });
                if (window.ScriptingActivePane is { } pane) response.Notifications.Add(new API.FocusChangedNotification { Session = pane.ScriptingId });
            }
        }
        return response;
    }
    private void PublishChanges()
    {
        var snapshot = Snapshot();
        if (!snapshot.Equals(_previous))
        {
            var sessions = Windows.SelectMany(w => w.ScriptingPanes).Select(p => p.ScriptingId).ToHashSet();
            static IEnumerable<string> Ids(API.SplitTreeNode node) => node.Links.SelectMany(link => link.Session is { } session ? [session.UniqueIdentifier] : link.Node is { } child ? Ids(child) : []);
            var old = _previous?.Windows.SelectMany(w => w.Tabs).SelectMany(t => t.Root is { } root ? Ids(root) : []).ToHashSet() ?? [];
            foreach (var id in sessions.Except(old)) Changed?.Invoke(this, new(API.NotificationType.NotifyOnNewSession, new() { NewSessionNotification = new() { SessionId = id } }));
            foreach (var id in old.Except(sessions)) Changed?.Invoke(this, new(API.NotificationType.NotifyOnTerminateSession, new() { TerminateSessionNotification = new() { SessionId = id } }));
            Changed?.Invoke(this, new(API.NotificationType.NotifyOnLayoutChange, new() { LayoutChangedNotification = new() { ListSessionsResponse = snapshot } }));
            _previous = snapshot;
        }
        var focus = Focus();
        if (!focus.Equals(_previousFocus))
        {
            foreach (var notification in focus.Notifications) Changed?.Invoke(this, new(API.NotificationType.NotifyOnFocusChange, new() { FocusChangedNotification = notification }));
            _previousFocus = focus;
        }
    }
    public async Task<API.ServerOriginatedMessage> HandleAsync(API.ClientOriginatedMessage request, CancellationToken token)
    {
        await _requests.WaitAsync(token);
        try
        {
            var pending = await Dispatcher.UIThread.InvokeAsync(() => HandleOnUi(request, token), DispatcherPriority.Normal, token);
            return await pending;
        }
        finally { _requests.Release(); }
    }

    private async Task<API.ServerOriginatedMessage> HandleOnUi(API.ClientOriginatedMessage request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        API.ServerOriginatedMessage response;
        switch (request.SubmessageCase)
        {
            case API.ClientOriginatedMessage.SubmessageOneofCase.ListSessionsRequest: return new() { ListSessionsResponse = Snapshot() };
            case API.ClientOriginatedMessage.SubmessageOneofCase.FocusRequest: return new() { FocusResponse = Focus() };
            case API.ClientOriginatedMessage.SubmessageOneofCase.GetBroadcastDomainsRequest: return new() { GetBroadcastDomainsResponse = new() };
            case API.ClientOriginatedMessage.SubmessageOneofCase.SendTextRequest:
                var send = request.SendTextRequest;
                var targets = send.Session == "all" ? Windows.SelectMany(w => w.ScriptingPanes).ToArray() : Pane(send.Session) is { } selected ? [selected] : Array.Empty<TerminalPane>();
                if (!send.HasText) return new() { Error = "send_text requires text." };
                if (targets.Any(p => !p.Terminal.IsRunning)) return new() { Error = "A selected session has exited." };
                foreach (var pane in targets) pane.Terminal.WriteInput(send.Text);
                return new() { SendTextResponse = new() { Status = targets.Length > 0 ? API.SendTextResponse.Types.Status.Ok : API.SendTextResponse.Types.Status.SessionNotFound } };
            case API.ClientOriginatedMessage.SubmessageOneofCase.GetBufferRequest:
                var read = request.GetBufferRequest;
                return new() { GetBufferResponse = Pane(read.Session) is { } screen ? ScriptingScreen.Read(screen.ScriptingEngine, read) : new() { Status = API.GetBufferResponse.Types.Status.SessionNotFound } };
            case API.ClientOriginatedMessage.SubmessageOneofCase.CreateTabRequest:
                var create = request.CreateTabRequest;
                var window = create.HasWindowId ? Windows.FirstOrDefault(w => w.ScriptingWindowId == create.WindowId) : null;
                if (create.HasWindowId && window is null) return new() { CreateTabResponse = new() { Status = API.CreateTabResponse.Types.Status.InvalidWindowId } };
                var profile = Profile(create.HasProfileName ? create.ProfileName : null, create.CustomProfileProperties);
                if (profile is null) return new() { CreateTabResponse = new() { Status = API.CreateTabResponse.Types.Status.InvalidProfileName } };
#pragma warning disable CS0612 // Preserve the original deprecated wire field for older scripts.
                if (create.HasCommand) profile = profile with { Command = create.Command };
#pragma warning restore CS0612
                Validate(profile);
                if (Windows.Count >= 16 || Windows.Sum(w => w.ScriptingPanes.Count()) >= 64) return new() { Error = "Scripting is limited to 16 windows and 64 live panes." };
                if (window is null)
                {
                    window = MainWindow.ScriptingCreateWindow(_store, profile); window.Show();
                    var deadline = System.Diagnostics.Stopwatch.StartNew();
                    while (!window.ScriptingReady || !window.ScriptingPanes.All(p => p.Terminal.IsRunning))
                    { if (deadline.Elapsed > TimeSpan.FromSeconds(15)) throw new IOException("The requested shell did not start."); await Task.Delay(25, token); }
                    var snapshot = window.ScriptingSnapshot(); var tab = snapshot.Tabs.Single();
                    response = new() { CreateTabResponse = new() { WindowId = snapshot.WindowId, TabId = int.Parse(tab.TabId, System.Globalization.CultureInfo.InvariantCulture), SessionId = tab.ActiveSessionId } };
                }
                else response = new() { CreateTabResponse = await window.ScriptingCreateTab(profile, create) };
                break;
            case API.ClientOriginatedMessage.SubmessageOneofCase.SplitPaneRequest:
                var split = request.SplitPaneRequest; var old = Pane(split.Session);
                if (old is null) return new() { SplitPaneResponse = new() { Status = API.SplitPaneResponse.Types.Status.SessionNotFound } };
                var splitProfile = Profile(split.HasProfileName ? split.ProfileName : null, split.CustomProfileProperties);
                if (splitProfile is null) return new() { SplitPaneResponse = new() { Status = API.SplitPaneResponse.Types.Status.InvalidProfileName } };
                if (Windows.Sum(w => w.ScriptingPanes.Count()) >= 64) return new() { SplitPaneResponse = new() { Status = API.SplitPaneResponse.Types.Status.CannotSplit } };
                var added = await Owner(old)!.ScriptingSplit(old, split.SplitDirection == API.SplitPaneRequest.Types.SplitDirection.Horizontal, split.Before, splitProfile);
                response = new() { SplitPaneResponse = new() { Status = added is null ? API.SplitPaneResponse.Types.Status.CannotSplit : API.SplitPaneResponse.Types.Status.Ok } };
                if (added is not null) response.SplitPaneResponse.SessionId.Add(added.ScriptingId);
                break;
            case API.ClientOriginatedMessage.SubmessageOneofCase.ActivateRequest:
                var activate = request.ActivateRequest;
                var owner = activate.IdentifierCase switch
                {
                    API.ActivateRequest.IdentifierOneofCase.WindowId => Windows.FirstOrDefault(w => w.ScriptingWindowId == activate.WindowId),
                    API.ActivateRequest.IdentifierOneofCase.TabId => Windows.FirstOrDefault(w => w.ScriptingHasTab(activate.TabId)),
                    API.ActivateRequest.IdentifierOneofCase.SessionId => Pane(activate.SessionId) is { } pane ? Owner(pane) : null,
                    _ => Current
                };
                response = new() { ActivateResponse = new() { Status = owner?.ScriptingActivate(activate) == true ? API.ActivateResponse.Types.Status.Ok : API.ActivateResponse.Types.Status.BadIdentifier } }; break;
            case API.ClientOriginatedMessage.SubmessageOneofCase.ListProfilesRequest:
                var profiles = request.ListProfilesRequest; var listed = new API.ListProfilesResponse();
                foreach (var entry in _store.Settings.Profiles.Where(p => profiles.Guids.Count == 0 || profiles.Guids.Contains(p.Id)))
                {
                    var item = new API.ListProfilesResponse.Types.Profile();
                    foreach (var (name, value) in new[] { ("Guid", entry.Id), ("Name", entry.Name), ("Command", entry.Command), ("Working Directory", entry.Directory) })
                        if (profiles.Properties.Count == 0 || profiles.Properties.Contains(name)) item.Properties.Add(new API.ProfileProperty { Key = name, JsonValue = JsonSerializer.Serialize(value) });
                    listed.Profiles.Add(item);
                }
                return new() { ListProfilesResponse = listed };
            case API.ClientOriginatedMessage.SubmessageOneofCase.CloseRequest:
                var close = request.CloseRequest; var result = new API.CloseResponse();
                if (close.Windows is not null) return new() { Error = "Close windows via their tabs; window close RPC is not supported yet." };
                var ids = close.Tabs?.TabIds ?? close.Sessions?.SessionIds;
                if (ids is null || ids.Count > 64) return new() { Error = "Specify at most 64 tabs or sessions to close." };
                foreach (var id in ids)
                {
                    var found = close.Tabs is not null ? Windows.FirstOrDefault(w => w.ScriptingHasTab(id)) : Pane(id) is { } pane ? Owner(pane) : null;
                    result.Statuses.Add(found is null ? API.CloseResponse.Types.Status.NotFound : await found.ScriptingClose(id, close.Tabs is not null, close.Force) ? API.CloseResponse.Types.Status.Ok : API.CloseResponse.Types.Status.UserDeclined);
                }
                response = new() { CloseResponse = result }; break;
            default: return new() { Error = "This iTerm2 RPC is not implemented by Aster." };
        }
        PublishChanges(); return response;
    }
    private TerminalProfile? Profile(string? name, IEnumerable<API.ProfileProperty> properties)
    {
        var profile = name is null ? _store.Settings.Profiles.First(p => p.Id == _store.Settings.DefaultProfileId) : _store.Settings.Profiles.FirstOrDefault(p => p.Name == name);
        if (profile is null) return null;
        foreach (var property in properties)
        {
            using var json = JsonDocument.Parse(property.JsonValue); var value = json.RootElement;
            profile = property.Key switch
            {
                "Command" when value.ValueKind == JsonValueKind.String => profile with { Command = value.GetString()! },
                "Working Directory" when value.ValueKind == JsonValueKind.String => profile with { Directory = value.GetString()! },
                "Name" when value.ValueKind == JsonValueKind.String => profile with { Name = value.GetString()! },
                "Custom Command" when value.ValueKind == JsonValueKind.String && value.GetString() == "Yes" => profile,
                _ => throw new ArgumentException("Unsupported custom profile property: " + property.Key)
            };
        }
        Validate(profile); return profile;
    }
    private static void Validate(TerminalProfile profile) => SettingsStore.Validate(new() { Profiles = [profile], DefaultProfileId = profile.Id });
    public void Dispose() => _changes.Stop();
}
