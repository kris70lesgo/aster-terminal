using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Aster.Sessions;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Connection;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class UnixFactAttribute : FactAttribute
{
    public UnixFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()) Skip = "Unix PTY acceptance requires Unix.";
    }
}

public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows()) Skip = "ConPTY acceptance requires Windows.";
    }
}

public sealed class PtyTests
{
    private sealed class Shell : IAsyncDisposable
    {
        public IRestartableTerminalConnection Connection { get; } = PlatformConnectionFactory.Create();
        private readonly object _gate = new();
        public ItermTerminalEngine Engine { get; } = new(80, 24);
        private readonly StringBuilder _output = new();
        public TaskCompletionSource<int> Exit { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Exception> Fault { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Output { get { lock (_gate) return _output.ToString(); } }

        public Shell()
        {
            Connection.OutputReceived += (_, data) =>
            {
                lock (_gate)
                {
                    _output.Append(Encoding.UTF8.GetString(data.Span));
                    Engine.Feed(data.Span);
                }
            };
            Connection.Exited += (_, code) => Exit.TrySetResult(code);
            Connection.Faulted += (_, error) => Fault.TrySetResult(error);
        }
        public Task Start(string command = "exec /bin/sh") => Connection.StartAsync(new TerminalLaunchOptions
        {
            CommandLine = command,
            WorkingDirectory = Path.GetTempPath(), Columns = 80, Rows = 24,
            EnvironmentVariables = new Dictionary<string, string?> { ["TERM"] = "xterm-256color", ["PS1"] = "" }
        });
        public string Snapshot() { lock (_gate) return TerminalBufferExport.ToPlainText(Engine.CreateSnapshot(true).Buffer); }
        public async Task Until(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (Fault.Task.IsCompletedSuccessfully) throw new IOException("PTY fault", Fault.Task.Result);
                if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException(Output);
                await Task.Delay(20);
            }
        }
        public async ValueTask DisposeAsync() { await Connection.DisposeAsync(); Engine.Dispose(); }
    }

    [UnixFact]
    public async Task InteractiveShellTtyUnicodeResizeInterruptAndExit()
    {
        await using var shell = new Shell();
        await shell.Start();
        shell.Connection.Write("test -t 0 && test -t 1 && printf '%s%s\\n' '__ASTER_' 'TTY__'\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_TTY__"));
        shell.Connection.Write("printf '\\033[38;2;12;34;56m%s%s\\033[0m\\n' 'hé' '界'\n");
        await shell.Until(() => shell.Snapshot().Contains("hé界"));
        shell.Connection.Resize(101, 41);
        shell.Connection.Write("stty size\n");
        await shell.Until(() => shell.Output.Contains("41 101"));
        shell.Connection.Write("printf '%s%s\\n' '__ASTER_' 'SLEEP__'; sleep 30\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_SLEEP__"));
        shell.Connection.Write("\u0003");
        shell.Connection.Write("printf '%s%s\\n' '__ASTER_' 'INTERRUPTED__'\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_INTERRUPTED__"));
        shell.Connection.Write("exit 7\n");
        Assert.Equal(7, await shell.Exit.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.False(shell.Connection.IsRunning);
        Assert.Equal(TerminalExitReason.ProcessExited, shell.Connection.LastExitInfo!.Reason);
        Assert.Equal(7, shell.Connection.LastExitInfo.ExitCode);
    }

    [UnixFact]
    public async Task IndependentSessionsAndCloseReapHelper()
    {
        await using var first = new Shell();
        await using var second = new Shell();
        await first.Start(); await second.Start();
        first.Connection.Write("ASTER_PRIVATE=first; printf '%s%s\\n' '__FIRST_' 'READY__'\n");
        second.Connection.Write("printf '%s:%s%s\\n' \"${ASTER_PRIVATE:-unset}\" '__SECOND_' 'READY__'\n");
        await first.Until(() => first.Output.Contains("__FIRST_READY__"));
        await second.Until(() => second.Output.Contains("unset:__SECOND_READY__"));
        var helperPid = first.Connection.ProcessMetadata!.ProcessId;
        await first.Connection.CloseAsync();
        Assert.False(first.Connection.IsRunning);
        Assert.True(second.Connection.IsRunning);
        Assert.Throws<ArgumentException>(() => Process.GetProcessById(helperPid));
    }

    [UnixFact]
    public async Task CloseTerminatesShellAndBackgroundChild()
    {
        await using var shell = new Shell();
        await shell.Start();
        shell.Connection.Write("if [ -n \"$BASH_VERSION\" ]; then set +H; fi\n");
        shell.Connection.Write("printf 'ASTERPID:%s\\n' $$; sleep 30 & printf 'CHILDPID:%s\\n' $!; wait\n");
        await shell.Until(() => Regex.IsMatch(shell.Output, @"CHILDPID:\d+"));
        var shellPid = int.Parse(Regex.Match(shell.Output, @"ASTERPID:(\d+)").Groups[1].Value);
        var childPid = int.Parse(Regex.Match(shell.Output, @"CHILDPID:(\d+)").Groups[1].Value);
        await shell.Connection.CloseAsync();
        await shell.Until(() => HasExited(shellPid) && HasExited(childPid));

        static bool HasExited(int pid)
        {
            try { using var process = Process.GetProcessById(pid); return process.HasExited; }
            catch (ArgumentException) { return true; }
        }
    }

    [UnixFact]
    public async Task VimUsesAlternateBufferAndReturnsToShell()
    {
        await using var shell = new Shell();
        await shell.Start();
        shell.Connection.Write("vim -Nu NONE -n -i NONE\n");
        await shell.Until(() => shell.Engine.AlternateBufferActive);
        shell.Connection.Write("iASTER_VIM_ACCEPTANCE\u001b");
        await shell.Until(() => shell.Snapshot().Contains("ASTER_VIM_ACCEPTANCE"));
        shell.Connection.Resize(100, 35);
        shell.Connection.Write(":q!\r");
        await shell.Until(() => !shell.Engine.AlternateBufferActive);
        shell.Connection.Write("printf '%s%s\\n' '__ASTER_' 'AFTER_VIM__'\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_AFTER_VIM__"));
    }

    [WindowsFact]
    public async Task CmdConPtyCommandResizeAndExit()
    {
        await using var shell = new Shell();
        await shell.Start("cmd.exe /Q");
        shell.Connection.Write("echo %COMSPEC%\r");
        await shell.Until(() => shell.Snapshot().Contains("cmd.exe", StringComparison.OrdinalIgnoreCase));
        shell.Connection.Resize(101, 41);
        Assert.Equal(101, shell.Connection.Columns);
        Assert.Equal(41, shell.Connection.Rows);
        shell.Connection.Write("exit 7\r");
        Assert.Equal(7, await shell.Exit.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }
}
