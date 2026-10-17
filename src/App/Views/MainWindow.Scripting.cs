using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Aster.Sessions;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.App.Views;

public partial class MainWindow
{
    internal string ScriptingWindowId { get; } = Guid.NewGuid().ToString("N");
    internal IEnumerable<TerminalPane> ScriptingPanes => _workspaces.SelectMany(w => w.Panes);
    internal TerminalPane? ScriptingActivePane => Active;
    internal string? ScriptingTabId => Current?.ScriptingId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    internal IReadOnlyList<TerminalProfile> ScriptingProfiles => _settings.Profiles;
    internal bool ScriptingReady => _initialized && !_closing && _workspaces.Count > 0;
    internal static MainWindow ScriptingCreateWindow(Aster.Sessions.Persistence.SettingsStore store, TerminalProfile profile)
    {
        var profiles = store.Settings.Profiles.Select(p => p.Id == profile.Id ? profile : p).ToList();
        return new MainWindow(store.Settings with { Profiles = profiles, DefaultProfileId = profile.Id }, store, hotkeyWindow: true) { ShowActivated = false };
    }
    internal bool ScriptingHasTab(string id) => _workspaces.Any(w => w.ScriptingId.ToString(System.Globalization.CultureInfo.InvariantCulture) == id);
    internal API.ListSessionsResponse.Types.Window ScriptingSnapshot()
    {
        var result = new API.ListSessionsResponse.Types.Window { WindowId = ScriptingWindowId, Frame = ScriptingFrame(Position.X, Position.Y, Bounds.Width, Bounds.Height), SelectedTabId = ScriptingTabId ?? "" };
        foreach (var workspace in _workspaces)
        {
            var tab = new API.ListSessionsResponse.Types.Tab { TabId = workspace.ScriptingId.ToString(System.Globalization.CultureInfo.InvariantCulture), ActiveSessionId = workspace.Active?.ScriptingId ?? "" };
            if (workspace.Root.Children.Count > 0) tab.Root = ScriptingNode(workspace.Root.Children[0], workspace);
            result.Tabs.Add(tab);
        }
        return result;
    }
    private static API.Frame ScriptingFrame(double x, double y, double width, double height) => new()
    { Origin = new() { X = (int)x, Y = (int)y }, Size = new() { Width = (int)width, Height = (int)height } };
    private static API.SplitTreeNode ScriptingNode(Control control, Workspace workspace)
    {
        if (control is TerminalPane pane) return new() { Links = { new API.SplitTreeNode.Types.SplitTreeLink { Session = ScriptingSummary(pane, workspace) } } };
        var grid = (Grid)control; var below = grid.RowDefinitions.Count > 0;
        var result = new API.SplitTreeNode { Vertical = !below };
        foreach (var child in grid.Children.Where(c => c is not GridSplitter).OrderBy(c => below ? Grid.GetRow(c) : Grid.GetColumn(c)))
            result.Links.Add(child is TerminalPane leaf ? new API.SplitTreeNode.Types.SplitTreeLink { Session = ScriptingSummary(leaf, workspace) } : new API.SplitTreeNode.Types.SplitTreeLink { Node = ScriptingNode(child, workspace) });
        // A zoomed pane is temporarily outside the split tree; its slot is still
        // represented by its original parent/row/column, preserving API identity.
        if (workspace.Zoom is { } zoom && zoom.Parent == grid)
        {
            var index = (below ? zoom.Row : zoom.Column) == 0 ? 0 : result.Links.Count;
            result.Links.Insert(index, new() { Session = ScriptingSummary(zoom.Pane, workspace) });
        }
        return result;
    }
    private static API.SessionSummary ScriptingSummary(TerminalPane pane, Workspace workspace)
    {
        var origin = pane.TranslatePoint(default, workspace.Root) ?? default;
        return new() { UniqueIdentifier = pane.ScriptingId, Title = pane.SessionTitle,
            Frame = ScriptingFrame(origin.X, origin.Y, pane.Bounds.Width, pane.Bounds.Height),
            GridSize = new() { Width = pane.Terminal.Engine.Columns, Height = pane.Terminal.Engine.Rows } };
    }
    internal async Task<API.CreateTabResponse> ScriptingCreateTab(TerminalProfile? profile, API.CreateTabRequest request)
    {
        var response = new API.CreateTabResponse();
        await ChangeLayoutAsync(async () =>
        {
            var previous = Tabs.SelectedItem;
            await NewTabCoreAsync(profile ?? DefaultProfile);
            var created = Current!;
            if (request.HasTabIndex && request.TabIndex < _workspaces.Count)
            {
                MoveTabCore(created, (int)request.TabIndex);
            }
            else if (request.HasTabIndex) response.Status = API.CreateTabResponse.Types.Status.InvalidTabIndex;
            response.WindowId = ScriptingWindowId; response.TabId = created.ScriptingId; response.SessionId = created.Active!.ScriptingId;
            Tabs.SelectedItem = request.HasSelectTab && !request.SelectTab ? previous : created.Tab;
        });
        return response;
    }
    internal async Task<TerminalPane?> ScriptingSplit(TerminalPane old, bool below, bool before, TerminalProfile? profile)
    {
        TerminalPane? created = null;
        await ChangeLayoutAsync(async () =>
        {
            var workspace = _workspaces.FirstOrDefault(w => w.Panes.Contains(old)); if (workspace is null || workspace.Panes.Count >= 64) return;
            created = await SplitPaneCoreAsync(workspace, old, below, before, profile);
        });
        return created;
    }
    internal async Task<bool> ScriptingClose(string id, bool tab, bool force)
    {
        var result = false;
        await ChangeLayoutAsync(async () =>
        {
            var workspace = _workspaces.FirstOrDefault(w => tab ? w.ScriptingId.ToString(System.Globalization.CultureInfo.InvariantCulture) == id : w.Panes.Any(p => p.ScriptingId == id));
            if (workspace is null) return;
            var panes = tab ? workspace.Panes.ToArray() : workspace.Panes.Where(p => p.ScriptingId == id).ToArray();
            if (!force && !await ConfirmCloseAsync(panes)) return;
            foreach (var pane in panes) await RemovePaneCoreAsync(workspace, pane);
            result = true;
        });
        return result;
    }
    internal bool ScriptingActivate(API.ActivateRequest request)
    {
        var workspace = request.IdentifierCase switch
        {
            API.ActivateRequest.IdentifierOneofCase.TabId => _workspaces.FirstOrDefault(w => w.ScriptingId.ToString(System.Globalization.CultureInfo.InvariantCulture) == request.TabId),
            API.ActivateRequest.IdentifierOneofCase.SessionId => _workspaces.FirstOrDefault(w => w.Panes.Any(p => p.ScriptingId == request.SessionId)),
            _ => Current
        };
        if (workspace is null) return false;
        if (request.SelectTab) Tabs.SelectedItem = workspace.Tab;
        if (request.SelectSession && workspace.Panes.FirstOrDefault(p => p.ScriptingId == request.SessionId) is { } pane) { Activate(workspace, pane); pane.Terminal.Focus(); }
        if (request.OrderWindowFront || request.ActivateApp is not null) { if (!IsVisible) Show(); Activate(); }
        return true;
    }
}
