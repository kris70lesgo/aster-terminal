using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Hosting;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    [AvaloniaFact]
    public async Task DetachedWindowReopensTheSameLivePaneAndNormalCloseTerminatesIt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-window-recovery-" + Guid.NewGuid().ToString("N"));
        MainWindow? first = null, second = null; TerminalPane? pane = null;
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = new TerminalProfile("Live recovery", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
            { EnvironmentVariables = new() { ["PROMPT"] = "ASTER_READY$G" } };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, ConfirmClose = false, RestoreLayout = false, KeepSessionsAlive = true });
            first = new(store); first.Show();
            await Until(() => Panes(first).Length == 1 && Panes(first)[0].Terminal.IsRunning);
            pane = Panes(first)[0]; await Until(() => Text(pane).Contains("ASTER_READY>"));
            void Command(TerminalPane target, string text)
            {
                target.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
                target.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            }
            Command(pane, OperatingSystem.IsWindows() ? "set ASTER_RECOVERY_VALUE=kept" : "ASTER_RECOVERY_VALUE=kept");
            var id = pane.HostedSession!.Id; var process = pane.Terminal.ProcessMetadata;
            first.DetachAndClose(); await Until(() => !first.IsVisible && !pane.Terminal.IsRunning);
            second = new(new SettingsStore(store.Path)); second.Show();
            try { await Until(() => Panes(second).Length == 1 && Panes(second)[0].Terminal.IsRunning); }
            catch (TimeoutException error)
            {
                var details = string.Join(" | ", second.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
                throw new TimeoutException($"Recovered window did not start: panes={Panes(second).Length}; exits={string.Join(";", Panes(second).Select(p => p.Terminal.ConnectionState))}; {details}", error);
            }
            var recovered = Panes(second)[0]; Assert.Equal(id, recovered.HostedSession!.Id); Assert.Equal(process, recovered.Terminal.ProcessMetadata);
            Command(recovered, OperatingSystem.IsWindows() ? "echo __WINDOW_RECOVERED__%ASTER_RECOVERY_VALUE%" : "printf '__WINDOW_RECOVERED__%s\\n' \"$ASTER_RECOVERY_VALUE\"");
            try { await Until(() => Text(recovered).Contains("__WINDOW_RECOVERED__kept")); }
            catch (TimeoutException error) { throw new TimeoutException($"Recovered shell did not confirm its value: state={recovered.Terminal.ConnectionState}; {Text(recovered)}", error); }
            second.Close(); await Until(() => !second.IsVisible && !recovered.Terminal.IsRunning);
            var catalog = new SessionCatalog(Path.Combine(directory, "sessions"));
            await Until(() => !File.Exists(catalog.FilePath(id)));
        }
        finally
        {
            first?.Close(); second?.Close();
            if (Directory.Exists(Path.Combine(directory, "sessions")))
            {
                var catalog = new SessionCatalog(Path.Combine(directory, "sessions"));
                foreach (var path in Directory.EnumerateFiles(catalog.DirectoryPath, "*.json")) try { await catalog.TerminateAsync(SessionCatalog.Read(path)); } catch (IOException) { }
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
