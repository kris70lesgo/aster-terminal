using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Aster.App.Services;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    private sealed class HotkeyRegistration : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
    [AvaloniaFact]
    public async Task GlobalHotkeyWindowIsLazyRetainsRealShellAcrossHideAndPreservesNormalLayout()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-hotkey-window-" + Guid.NewGuid().ToString("N"));
        Action? pressed = null; var registration = new HotkeyRegistration();
        using var service = new HotkeyWindowService((callback, _) => { pressed = callback; return registration; });
        MainWindow? window = null;
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = new TerminalProfile("Hotkey fixture", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
            { EnvironmentVariables = new() { ["PROMPT"] = "ASTER_READY$G" } };
            var layout = new List<TabLayout> { new("Normal saved tab", new PaneLayout { ProfileId = profile.Id }) };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, ConfirmClose = false, HotkeyWindowEnabled = true, KeepSessionsAlive = true, RestoreLayout = true, Layout = layout });
            service.Configure(store); Assert.True(service.IsRegistered); Assert.Null(service.Window);
            pressed!(); await Until(() => service.Window is { IsVisible: true }); window = service.Window!;
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var pane = Panes(window)[0]; await Until(() => Text(pane).Contains("ASTER_READY>"));
            void Command(string text)
            {
                pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
                pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            }
            Command(OperatingSystem.IsWindows() ? "set ASTER_HOTKEY_VALUE=kept" : "ASTER_HOTKEY_VALUE=kept");
            // Hide and summon without Close/Dispose; this must be the same actual session.
            var session = pane.HostedSession!.Id; pressed(); await Until(() => !window.IsVisible); Assert.True(pane.Terminal.IsRunning);
            pressed(); await Until(() => window.IsVisible); Assert.Same(pane, Panes(window).Single());
            Assert.Equal(session, pane.HostedSession!.Id);
            Command(OperatingSystem.IsWindows() ? "echo __HOTKEY__%ASTER_HOTKEY_VALUE%" : "printf '__HOTKEY__%s\\n' \"$ASTER_HOTKEY_VALUE\"");
            await Until(() => Text(pane).Contains("__HOTKEY__kept"));
            window.Hide(); store.Save(store.Settings with { HotkeyWindowEnabled = false }); service.Configure(store);
            Assert.True(registration.Disposed); Assert.False(service.IsRegistered); Assert.True(window.IsVisible);
            window.Close(); await Until(() => !window.IsVisible && !pane.Terminal.IsRunning);
            Assert.Single(new SettingsStore(store.Path).Settings.Layout); Assert.Equal("Normal saved tab", new SettingsStore(store.Path).Settings.Layout[0].Title);
            service.Show(); await Until(() => service.Window is { IsVisible: true } next && next != window);
            var replacement = service.Window!; await Until(() => Panes(replacement).Length == 1 && Panes(replacement)[0].Terminal.IsRunning);
            Assert.NotEqual(session, Panes(replacement)[0].HostedSession!.Id);
            replacement.Close(); await Until(() => !replacement.IsVisible);
        }
        finally
        {
            window?.Close(); service.Window?.Close();
            if (Directory.Exists(Path.Combine(directory, "sessions")))
            {
                var catalog = new Aster.Sessions.Hosting.SessionCatalog(Path.Combine(directory, "sessions"));
                foreach (var path in Directory.EnumerateFiles(catalog.DirectoryPath, "*.json")) try { await catalog.TerminateAsync(Aster.Sessions.Hosting.SessionCatalog.Read(path)); } catch (IOException) { }
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
    [AvaloniaFact]
    public void GlobalHotkeyRegistrationFailureIsVisibleAndDoesNotPretendToBeEnabled()
    {
        var path = Path.Combine(Path.GetTempPath(), "aster-hotkey-error-" + Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            var store = new SettingsStore(path); store.Save(store.Settings with { HotkeyWindowEnabled = true });
            using var service = new HotkeyWindowService((_, _) => throw new IOException("fixture key collision"));
            Assert.Throws<IOException>(() => service.Configure(store)); Assert.False(service.IsRegistered); Assert.Contains("collision", service.RegistrationError);
        }
        finally { Directory.Delete(Path.GetDirectoryName(path)!, true); }
    }
}
