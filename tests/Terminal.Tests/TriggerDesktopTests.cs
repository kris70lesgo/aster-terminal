using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Aster.Sessions.Protocols;
using Aster.Sessions.Triggers;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task RealShellTriggersHighlightHistoryAndLiveProfileChanges()
    {
        var profile = new TerminalProfile("triggers", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
        { Triggers = [new() { Pattern = "^__TRIGGER_OUTPUT__$", Name = "Build error" }] };
        var window = new MainWindow(profile); window.Show(); TerminalPane? pane = null;
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            pane = Panes(window)[0]; var engine = (ItermTerminalEngine)pane.Terminal.Engine;
            await Until(() => !string.IsNullOrWhiteSpace(Text(pane)));
            pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = OperatingSystem.IsWindows() ? "echo __TRIGGER_OUTPUT__" : "printf '%s%s\\n' '__TRIGGER_' 'OUTPUT__'" });
            pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Until(() => engine.TriggerMatches.Count == 1);
            Assert.Equal("Build error", engine.TriggerMatches[0].Name);
            Assert.Contains(engine.CreateSnapshot(true).Buffer.Lines.SelectMany(l => l.Cells), c => c.Attributes.Background == TermColor.FromRgb(155, 48, 56));
            await Until(() => pane.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Triggered lines (1)") && b.IsVisible));
            var viewer = new TriggerHistoryWindow(engine, () => pane.Terminal.Focus()); _ = viewer.ShowDialog(window);
            viewer.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Go to line")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(viewer.IsVisible); Assert.True(pane.Terminal.IsRunning);
            pane.ApplyAppearance(profile with { Triggers = [] }, false, false);
            engine.Feed("__TRIGGER_OUTPUT__\r\n"); Assert.Single(engine.TriggerMatches);
        }
        finally { window.Close(); if (pane is not null) { await Until(() => !pane.Terminal.IsRunning); Assert.Empty(((ItermTerminalEngine)pane.Terminal.Engine).TriggerMatches); } }
    }

    [AvaloniaFact]
    public void ProfileTriggerEditorSaveCancelAndInvalidPatternsAreTransactional()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-trigger-editor-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            void Save(PreferencesWindow window) => window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Save preferences")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var first = new PreferencesWindow(store); first.Show();
            var editor = first.FindControl<ContentControl>("TriggerEditorHost")!.Content as TriggerEditor;
            editor!.SetRules([new() { Name = "Errors", Pattern = "(?i)error" }]); Save(first);
            Assert.Equal("(?i)error", new SettingsStore(store.Path).Settings.Profiles[0].Triggers.Single().Pattern);
            var cancel = new PreferencesWindow(store); cancel.Show();
            ((TriggerEditor)cancel.FindControl<ContentControl>("TriggerEditorHost")!.Content!).SetRules([]); cancel.Close();
            Assert.Single(store.Settings.Profiles[0].Triggers);
            var invalid = new PreferencesWindow(store); invalid.Show();
            ((TriggerEditor)invalid.FindControl<ContentControl>("TriggerEditorHost")!.Content!).SetRules([new() { Pattern = "[" }]); Save(invalid);
            Assert.True(invalid.IsVisible); Assert.NotEmpty(invalid.FindControl<TextBlock>("ErrorText")!.Text!); invalid.Close();
            Assert.Single(store.Settings.Profiles[0].Triggers);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
