using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    private static readonly RawInputModifiers ReorderModifiers = RawInputModifiers.Control | RawInputModifiers.Shift;
    private static RawInputModifiers CommandModifiers => OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : ReorderModifiers;

    private static SettingsStore ReorderStore(string directory)
    {
        var store = new SettingsStore(Path.Combine(directory, "settings.json"));
        var profile = new TerminalProfile("Reorder acceptance", OperatingSystem.IsWindows() ?
            "cmd.exe /Q /K prompt REORDER_READY$G" : "export PS1='REORDER_READY> '; exec /bin/sh", directory);
        store.Save(store.Settings with
        {
            Profiles = [profile], DefaultProfileId = profile.Id, KeepSessionsAlive = false, ConfirmClose = false,
            Layout = [new("One", new() { ProfileId = profile.Id }), new("Two", new() { ProfileId = profile.Id }), new("Three", new() { ProfileId = profile.Id })]
        });
        return store;
    }

    private static Point TabPoint(Window window, TabItem tab, double fraction = .2) =>
        tab.TranslatePoint(new Point(tab.Bounds.Width * fraction, tab.Bounds.Height / 2), window)!.Value;

    [AvaloniaFact]
    public async Task TabKeyboardReorderPreservesShellsSplitZoomFindAndSavedOrder()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-tab-order-" + Guid.NewGuid().ToString("N"));
        var store = ReorderStore(directory); var window = new MainWindow(store); window.Show();
        var panes = new List<TerminalPane>();
        try
        {
            await Until(() => window.ScriptingPanes.Count() == 3 && window.ScriptingPanes.All(p => Text(p).Contains("REORDER_READY>")));
            panes.AddRange(window.ScriptingPanes);
            var tabs = window.FindControl<TabControl>("Tabs")!; var original = tabs.Items.Cast<TabItem>().ToArray();
            var identities = panes.Select(p => p.Terminal.ProcessMetadata!.SessionId).ToArray();
            var scriptIds = window.ScriptingSnapshot().Tabs.Select(t => t.TabId).ToArray();
            tabs.SelectedIndex = 1; await Until(() => Panes(window).Single() == panes[1]);
            SendKey(window, Key.D, CommandModifiers);
            await Until(() => Panes(window).Length == 2 && Panes(window).All(p => Text(p).Contains("REORDER_READY>")));
            var split = Panes(window).Single(p => p != panes[1]); panes.Add(split);
            FindCommand(split, OperatingSystem.IsWindows() ? "set ASTER_REORDER_VALUE=preserved" : "ASTER_REORDER_VALUE=preserved");
            FindCommand(split, OperatingSystem.IsWindows() ? "echo FIND_retained" : "printf '\\nFIND_%s\\n' retained");
            await Until(() => Text(split).Contains("FIND_retained"));
            SendKey(window, Key.Enter, CommandModifiers); // Zoom the active split.
            SendKey(window, Key.F, CommandModifiers);
            var box = window.FindControl<FindBar>("SearchBar")!.FindControl<TextBox>("SearchBox")!;
            box.Text = "FIND_retained"; await Until(() => !split.Find.Busy && split.Find.Matches.Count > 0);
            var matches = split.Find.Matches.ToArray();
            SendKey(window, Key.PageUp, ReorderModifiers);
            Assert.Equal(new[] { original[1], original[0], original[2] }, tabs.Items.Cast<TabItem>());
            Assert.Same(original[1], tabs.SelectedItem); Assert.Same(split, window.ScriptingActivePane);
            Assert.True(box.IsFocused); Assert.Equal(matches, split.Find.Matches);
            // Moving left from the first tab wraps, as in ungrouped iTerm2 tabs.
            SendKey(window, Key.PageUp, ReorderModifiers);
            Assert.Equal(new[] { original[0], original[2], original[1] }, tabs.Items.Cast<TabItem>());
            SendKey(window, Key.PageDown, ReorderModifiers);
            Assert.Equal(new[] { original[1], original[0], original[2] }, tabs.Items.Cast<TabItem>());
            SendKey(window, Key.PageUp, ReorderModifiers);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.Equal(new[] { scriptIds[0], scriptIds[2], scriptIds[1] }, window.ScriptingSnapshot().Tabs.Select(t => t.TabId));
            Assert.True(box.IsFocused); Assert.All(panes, p => Assert.True(p.Terminal.IsRunning));
            Assert.Equal(identities, panes.Take(3).Select(p => p.Terminal.ProcessMetadata!.SessionId));
            SendKey(window, Key.Escape); SendKey(window, Key.Enter, CommandModifiers); // Restore the same split.
            await Until(() => Panes(window).Count(p => p.IsEffectivelyVisible) == 2);
            FindCommand(split, OperatingSystem.IsWindows() ? "echo __STATE_%ASTER_REORDER_VALUE%__" : "printf '\\n__STATE_%s__\\n' \"$ASTER_REORDER_VALUE\"");
            await Until(() => Text(split).Contains("__STATE_preserved__"));
            Assert.Equal(new[] { "One", "Three", "Two" }, window.CaptureLayout().Select(t => t.Title));
            Assert.Equal(2, window.CaptureLayout()[2].Root.Children.Count);
            window.Close(); await Until(() => !window.IsVisible && panes.All(p => !p.Terminal.IsRunning));
            var saved = new SettingsStore(store.Path).Settings;
            Assert.Equal(new[] { "One", "Three", "Two" }, saved.Layout.Select(t => t.Title));
            Assert.Equal(2, saved.Layout[2].Root.Children.Count);
        }
        finally
        {
            window.Close(); await Until(() => !window.IsVisible && panes.All(p => !p.Terminal.IsRunning));
            Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task TabPointerReorderHandlesBothDirectionsCancellationAndCloseWithoutRestartingShells()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-tab-drag-" + Guid.NewGuid().ToString("N"));
        var store = ReorderStore(directory); var window = new MainWindow(store); window.Show();
        var panes = new List<TerminalPane>();
        try
        {
            await Until(() => window.ScriptingPanes.Count() == 3 && window.ScriptingPanes.All(p => Text(p).Contains("REORDER_READY>")));
            panes.AddRange(window.ScriptingPanes);
            var tabs = window.FindControl<TabControl>("Tabs")!; var original = tabs.Items.Cast<TabItem>().ToArray();
            await Until(() => original.All(t => t.Bounds.Width > 100));
            var click = TabPoint(window, original[1]);
            window.MouseDown(click, MouseButton.Left);
            window.MouseMove(click + new Point(2, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(click + new Point(2, 0), MouseButton.Left);
            Assert.Equal(original, tabs.Items.Cast<TabItem>());
            Assert.Same(original[1], tabs.SelectedItem);
            void Begin(TabItem from, TabItem to, double fraction)
            {
                window.MouseDown(TabPoint(window, from), MouseButton.Left);
                window.MouseMove(TabPoint(window, to, fraction), RawInputModifiers.LeftMouseButton);
            }
            Begin(original[0], original[2], .8);
            Assert.Contains("drop-after", original[2].Classes);
            window.MouseUp(TabPoint(window, original[2], .8), MouseButton.Left);
            Assert.Equal(new[] { original[1], original[2], original[0] }, tabs.Items.Cast<TabItem>());
            Assert.Same(original[0], tabs.SelectedItem);
            Begin(original[0], original[1], .2);
            Assert.Contains("drop-before", original[1].Classes);
            window.MouseUp(TabPoint(window, original[1]), MouseButton.Left);
            Assert.Equal(original, tabs.Items.Cast<TabItem>());
            Begin(original[0], original[2], .8);
            SendKey(window, Key.Escape);
            window.MouseUp(TabPoint(window, original[2], .8), MouseButton.Left);
            Assert.Equal(original, tabs.Items.Cast<TabItem>());
            Begin(original[0], original[2], .8);
            window.MouseUp(new Point(300, 250), MouseButton.Left); // Outside the strip.
            Assert.Equal(original, tabs.Items.Cast<TabItem>());
            Assert.All(original, t => Assert.DoesNotContain(t.Classes, c => c.StartsWith("drop-")));
            Assert.All(panes, p => Assert.True(p.Terminal.IsRunning));
            var close = original[1].GetVisualDescendants().OfType<Button>().Single();
            var closePoint = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            window.MouseDown(closePoint, MouseButton.Left); window.MouseUp(closePoint, MouseButton.Left);
            await Until(() => tabs.ItemCount == 2 && !panes[1].Terminal.IsRunning);
            Assert.Equal(new[] { original[0], original[2] }, tabs.Items.Cast<TabItem>());
            Assert.True(panes[0].Terminal.IsRunning); Assert.True(panes[2].Terminal.IsRunning);
        }
        finally
        {
            window.Close(); await Until(() => !window.IsVisible && panes.All(p => !p.Terminal.IsRunning));
            Directory.Delete(directory, true);
        }
    }
}
