using Avalonia;

namespace Aster.App;

internal static class Program
{
    internal static readonly System.Diagnostics.Stopwatch Startup = System.Diagnostics.Stopwatch.StartNew();
    [STAThread]
    public static int Main(string[] args)
    {
        Startup.Restart();
        if (args is ["--session-host", var descriptor]) return Aster.Sessions.Hosting.SessionHost.RunAsync(descriptor).GetAwaiter().GetResult();
        if (args is ["--benchmark", var report]) return Services.TerminalBenchmark.Run(report);
        return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}
