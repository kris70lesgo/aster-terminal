using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aster.App.Views;
using Aster.App.Services;
using Aster.Sessions;

namespace Aster.App;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.Contains("--smoke-test") == true)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                var window = new MainWindow(new TerminalProfile("Native acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath()));
                desktop.MainWindow = window;
                var index = Array.IndexOf(desktop.Args, "--smoke-report");
                var destination = index >= 0 && index + 1 < desktop.Args.Length ? desktop.Args[index + 1] : "artifacts/native-smoke";
                window.Opened += async (_, _) => await DesktopSmoke.Run(window, desktop, Path.GetFullPath(destination));
            }
            else desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
