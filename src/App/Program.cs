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
        int exitCode;
        try { exitCode = AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args); }
        finally { Platform.MacExitWakeup.Stop(); }
        if (args.Contains("--scripting-smoke"))
        {
            var index = Array.IndexOf(args, "--scripting-report");
            if (index >= 0 && index + 1 < args.Length)
                File.AppendAllText(Path.Combine(Path.GetFullPath(args[index + 1]), "lifecycle.log"), $"{DateTimeOffset.UtcNow:O} pid={Environment.ProcessId} main loop returned exitCode={exitCode}\n");
        }
        return exitCode;
    }
}
