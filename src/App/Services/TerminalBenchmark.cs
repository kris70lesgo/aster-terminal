using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Core;

namespace Aster.App.Services;

/// <summary>Repeatable engine workload. Native startup/layout is measured separately by DesktopSmoke.</summary>
public static class TerminalBenchmark
{
    public static int Run(string destination)
    {
        using var process = Process.GetCurrentProcess();
        using var engine = new ItermTerminalEngine(120, 35, 10000);
        var data = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("benchmark hé界 \u001b[38;2;12;34;56mtruecolor\u001b[0m\r\n", 256)));
        engine.Feed(data); // warm parser, glyph cells and JIT
        var cpu = process.TotalProcessorTime;
        var watch = Stopwatch.StartNew();
        const int batches = 256;
        for (var i = 0; i < batches; i++) engine.Feed(data);
        var outputMs = watch.Elapsed.TotalMilliseconds;
        var cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
        watch.Restart();
        for (var i = 0; i < 20; i++) TextBufferSearch.FindAll(engine.CreateSnapshot(true).Buffer, "truecolor");
        var searchMs = watch.Elapsed.TotalMilliseconds / 20;
        watch.Restart();
        for (var i = 0; i < 100; i++) engine.Resize(i % 2 == 0 ? 100 : 120, i % 2 == 0 ? 30 : 35);
        var resizeMs = watch.Elapsed.TotalMilliseconds / 100;
        process.Refresh();
        var report = new
        {
            timestamp = DateTimeOffset.UtcNow, os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(), runtime = RuntimeInformation.FrameworkDescription,
            scope = "Single warmed engine; 120x35, 10000-line bounded history. No PTY/GPU/desktop; not a comparative benchmark.",
            outputBytes = (long)data.Length * batches, outputMs, outputCpuMs = cpuMs,
            outputMiBPerSecond = data.Length * (double)batches / 1048576 / (outputMs / 1000),
            historyLines = engine.HistoryCount, meanFullHistorySnapshotAndSearchMs = searchMs,
            meanAlternatingResizeMs = resizeMs, workingSetBytes = process.WorkingSet64,
            peakWorkingSetBytes = process.PeakWorkingSet64 > 0 ? (long?)process.PeakWorkingSet64 : null
        };
        var path = Path.GetFullPath(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
