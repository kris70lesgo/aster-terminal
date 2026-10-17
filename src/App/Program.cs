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
        var reportIndex = Array.FindIndex(args, arg => arg is "--smoke-report" or "--hotkey-report" or "--tmux-report" or "--recovery-report" or "--scripting-report");
        var fixtureReport = reportIndex >= 0 && reportIndex + 1 < args.Length ? Path.GetFullPath(args[reportIndex + 1]) : null;
        void TraceFixture(string text)
        {
            if (fixtureReport is null) return;
            // Keep bootstrap diagnostics beside the report: scripting requires
            // its report directory to be fresh when it configures the fixture.
            Directory.CreateDirectory(Path.GetDirectoryName(fixtureReport)!);
            File.AppendAllText(fixtureReport + ".process.log", $"{DateTimeOffset.UtcNow:O} pid={Environment.ProcessId} {text}\n");
        }
        TraceFixture("starting native desktop");
        int exitCode;
        try
        {
            var builder = AppBuilder.Configure<App>().UsePlatformDetect();
            if (OperatingSystem.IsMacOS())
                builder.With(new AvaloniaNativePlatformOptions
                {
                    RenderingMode = [AvaloniaNativeRenderingMode.Metal, AvaloniaNativeRenderingMode.Software]
                });
            exitCode = builder.LogToTrace().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception error)
        {
            TraceFixture("native desktop failed: " + error);
            throw;
        }
        finally { Platform.MacExitWakeup.Stop(); }
        TraceFixture($"main loop returned exitCode={exitCode}");
        if (args.Contains("--scripting-smoke"))
        {
            var index = Array.IndexOf(args, "--scripting-report");
            if (index >= 0 && index + 1 < args.Length)
                File.AppendAllText(Path.Combine(Path.GetFullPath(args[index + 1]), "lifecycle.log"), $"{DateTimeOffset.UtcNow:O} pid={Environment.ProcessId} main loop returned exitCode={exitCode}\n");
        }
        return exitCode;
    }
}
