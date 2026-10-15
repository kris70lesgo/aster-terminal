using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aster.App.Views;
using Aster.App.Services;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.App;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.Contains("--scripting-smoke") == true) desktop.MainWindow = ScriptingSmoke.Configure(desktop, desktop.Args);
            else if (desktop.Args?.Any(arg => arg is "--hotkey-smoke" or "--hotkey-peer" or "--hotkey-manual") == true) desktop.MainWindow = HotkeySmoke.Configure(desktop, desktop.Args);
            else if (desktop.Args?.Contains("--tmux-smoke") == true) desktop.MainWindow = TmuxSmoke.Configure(desktop, desktop.Args);
            else if (desktop.Args?.Any(arg => arg is "--recovery-start" or "--recovery-resume") == true) desktop.MainWindow = RecoverySmoke.Configure(desktop, desktop.Args);
            else if (desktop.Args?.Contains("--smoke-test") == true)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                var index = Array.IndexOf(desktop.Args, "--smoke-report");
                var destination = index >= 0 && index + 1 < desktop.Args.Length ? desktop.Args[index + 1] : "artifacts/native-smoke";
                var store = new SettingsStore(Path.Combine(Path.GetTempPath(), "aster-native-" + Guid.NewGuid().ToString("N"), "settings.json"));
                var profile = new TerminalProfile("Native acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
                {
                    Triggers = [new() { Name = "Build errors", Pattern = "^TRIGGERED_OUTPUT$", Action = "Highlight" }],
                    EnvironmentVariables = new() { ["PS1"] = "ASTER_READY> ", ["PROMPT"] = "ASTER_READY$G" }
                };
                store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, RestoreLayout = false, ConfirmClose = false });
                var window = new MainWindow(store);
                desktop.MainWindow = window;
                window.Opened += async (_, _) => await DesktopSmoke.Run(window, desktop, Path.GetFullPath(destination), store);
            }
            else
            {
                var store = new SettingsStore(); var window = new MainWindow(store); desktop.MainWindow = window;
                window.Opened += async (_, _) =>
                {
                    try { await ScriptingService.Shared.ConfigureAsync(store); }
                    catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or UnauthorizedAccessException) { window.ReportApplicationError("Scripting API unavailable: " + error.Message); }
                };
                try { HotkeyWindowService.Shared.Configure(store); } catch (IOException error) { window.ReportApplicationError(error.Message); }
            }
            desktop.Exit += (_, _) => { HotkeyWindowService.Shared.Dispose(); ScriptingService.Shared.StopAtExit(); };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
