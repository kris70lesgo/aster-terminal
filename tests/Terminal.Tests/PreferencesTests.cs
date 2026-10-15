using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Aster.App.Services;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public void PreferencesSaveValidationAndCancelAreTransactional()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-preferences-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var dialog = new PreferencesWindow(store); dialog.Show();
            dialog.FindControl<TextBox>("ProfileName")!.Text = "Configured profile";
            dialog.FindControl<NumericUpDown>("FontSizeBox")!.Value = 18;
            dialog.FindControl<ComboBox>("ThemeBox")!.SelectedItem = "Light";
            dialog.FindControl<CheckBox>("HotkeyWindowBox")!.IsChecked = true;
            dialog.FindControl<CheckBox>("ScriptingEnabledBox")!.IsChecked = true;
            dialog.FindControl<NumericUpDown>("ScriptingPortBox")!.Value = 1913;
            var save = dialog.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Save preferences"));
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.NotNull(dialog.Result); Assert.False(dialog.IsVisible);
            Assert.Equal("Configured profile", new SettingsStore(store.Path).Settings.Profiles[0].Name);
            Assert.Equal("Light", store.Settings.Theme); Assert.True(store.Settings.HotkeyWindowEnabled); Assert.True(store.Settings.ScriptingEnabled); Assert.Equal(1913, store.Settings.ScriptingPort);
            var cancel = new PreferencesWindow(store); cancel.Show(); cancel.FindControl<TextBox>("ProfileName")!.Text = "Not saved"; cancel.Close();
            Assert.Equal("Configured profile", store.Settings.Profiles[0].Name);
            var invalid = new PreferencesWindow(store); invalid.Show(); invalid.FindControl<TextBox>("ForegroundBox")!.Text = "broken";
            invalid.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Save preferences")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(invalid.IsVisible); Assert.NotEmpty(invalid.FindControl<TextBlock>("ErrorText")!.Text!); invalid.Close();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [AvaloniaFact]
    public async Task NestedSplitZoomThemeTitlesAndLayoutReopenUseFreshProcesses()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-layout-" + Guid.NewGuid().ToString("N"));
        MainWindow? firstWindow = null, restored = null;
        var allPanes = new List<TerminalPane>();
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = new TerminalProfile("layout", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath());
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, ConfirmClose = false, Theme = "Light" });
            firstWindow = new MainWindow(store); firstWindow.Show();
            await Until(() => Panes(firstWindow).Length == 1 && Panes(firstWindow)[0].Terminal.IsRunning);
            Assert.Equal(ThemeVariant.Light, firstWindow.ActualThemeVariant);
            Assert.Equal(0xFFFAFAFCu, Panes(firstWindow)[0].Terminal.Engine.Scheme.Background);
            var changed = profile with { FontSize = 18 };
            firstWindow.ApplySettings(store.Settings with { Profiles = [changed] });
            Assert.Equal(18, Panes(firstWindow)[0].Terminal.FontSize);
            var command = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift;
            SendKey(firstWindow, Key.D, command);
            await Until(() => Panes(firstWindow).Length == 2 && Panes(firstWindow).All(p => p.Terminal.IsRunning));
            SendKey(firstWindow, Key.D, command | (OperatingSystem.IsMacOS() ? RawInputModifiers.Shift : RawInputModifiers.Alt));
            await Until(() => Panes(firstWindow).Length == 3 && Panes(firstWindow).All(p => p.Terminal.IsRunning));
            allPanes.AddRange(Panes(firstWindow));
            var oldIds = allPanes.Select(p => p.Terminal.ProcessMetadata!.SessionId).ToArray();
            SendKey(firstWindow, Key.Enter, command); SendKey(firstWindow, Key.Enter, command);
            var layout = firstWindow.CaptureLayout();
            Assert.Single(layout); Assert.Equal("Columns", layout[0].Root.Direction); Assert.Equal("Rows", layout[0].Root.Children[1].Direction);
            allPanes[^1].Terminal.Engine.Feed("\u001b]2;Updated title\u0007");
            await Until(() => firstWindow.Title!.Contains("Updated title"));
            firstWindow.Close(); await Until(() => !firstWindow.IsVisible && allPanes.All(p => !p.Terminal.IsRunning));
            restored = new MainWindow(new SettingsStore(store.Path)); restored.Show();
            await Until(() => Panes(restored).Length == 3 && Panes(restored).All(p => p.Terminal.IsRunning));
            Assert.DoesNotContain(Panes(restored), p => oldIds.Contains(p.Terminal.ProcessMetadata!.SessionId));
            allPanes.AddRange(Panes(restored));
        }
        finally
        {
            firstWindow?.Close(); restored?.Close();
            await Until(() => allPanes.All(p => !p.Terminal.IsRunning));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ShortcutsRejectDuplicatesAndInvalidOverrides()
    {
        Assert.Throws<ArgumentException>(() => ShortcutMap.Resolve(new Dictionary<string, string> { ["NewTab"] = "Control+Tab" }));
        Assert.Throws<ArgumentException>(() => ShortcutMap.Resolve(new Dictionary<string, string> { ["NewTab"] = "broken+key+combo" }));
        Assert.NotEmpty(ShortcutMap.Resolve(new Dictionary<string, string>()));
    }
}
