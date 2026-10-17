using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Aster.Sessions;
using Aster.Sessions.Protocols;
using Aster.Sessions.ShellIntegration;
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
            // Terminal applications query capabilities/cursor/palette during
            // startup. Mirror TermControl's response routing in this PTY fixture.
            Engine.ResponseReady += (_, response) => { if (Connection.IsRunning) Connection.Write(response); };
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
        private const string TestShellCommand = "export PS1='__ASTER_PROMPT__ '; exec /bin/sh";
        public async Task Start(string command = TestShellCommand)
        {
            await Connection.StartAsync(new TerminalLaunchOptions
            {
                CommandLine = command,
                WorkingDirectory = Path.GetTempPath(), Columns = 80, Rows = 24,
                EnvironmentVariables = new Dictionary<string, string?> { ["TERM"] = "xterm-256color" }
            });
            if (command == TestShellCommand) await Until(() => Snapshot().Contains("__ASTER_PROMPT__"));
        }
        public Task StartProfile(TerminalProfile profile)
        {
            var settings = ShellIntegrationLauncher.Prepare(profile, false);
            return Connection.StartAsync(new TerminalLaunchOptions { CommandLine = settings.Commandline, WorkingDirectory = profile.Directory, Columns = 80, Rows = 24, EnvironmentVariables = settings.Environment });
        }
        public string Snapshot() { lock (_gate) return TerminalBufferExport.ToPlainText(Engine.CreateSnapshot(true).Buffer); }
        public async Task Until(Func<bool> condition, TimeSpan? timeout = null)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (Fault.Task.IsCompletedSuccessfully) throw new IOException("PTY fault", Fault.Task.Result);
                if (watch.Elapsed > (timeout ?? TimeSpan.FromSeconds(10))) throw new TimeoutException(Output);
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
        // Emit readiness from inside the foreground job, not before it is spawned.
        shell.Connection.Write("sh -c \"printf '%s%s\\n' '__ASTER_' 'SLEEP__'; exec sleep 30\"\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_SLEEP__"));
        var prompts = Regex.Matches(shell.Snapshot(), "__ASTER_PROMPT__").Count;
        shell.Connection.Write("\u0003");
        // SIGINT/termios may flush input; type only after the shell resumes.
        await shell.Until(() => Regex.Matches(shell.Snapshot(), "__ASTER_PROMPT__").Count > prompts);
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
    public async Task UpstreamBashIntegrationTracksCommandStatusDirectoryAndMarks()
    {
        await VerifyIntegration("/bin/bash");
    }

    [UnixFact]
    public async Task UpstreamZshIntegrationTracksCommandStatusDirectoryAndMarks()
    {
        if (!File.Exists("/bin/zsh")) Assert.Skip("zsh is not installed on this Unix runner.");
        await VerifyIntegration("/bin/zsh");
    }

    private static async Task VerifyIntegration(string executable)
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-hooks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await using var shell = new Shell();
            await shell.StartProfile(new TerminalProfile("hooks", executable + (executable.EndsWith("zsh", StringComparison.Ordinal) ? " -d -i" : " -i"), directory)
            {
                ShellIntegration = true, EnvironmentVariables = new() { ["HOME"] = directory, ["ZDOTDIR"] = directory, ["PS1"] = "$ " }
            });
            await shell.Until(() => shell.Engine.WorkingDirectory?.Contains(directory, StringComparison.Ordinal) == true);
            shell.Connection.Write("false\n");
            await shell.Until(() => shell.Engine.LastCommandExitCode == 1);
            var child = Path.Combine(directory, "child"); Directory.CreateDirectory(child);
            shell.Connection.Write("cd child\n");
            await shell.Until(() => shell.Engine.WorkingDirectory?.Contains("child", StringComparison.Ordinal) == true);
            Assert.NotEmpty(TerminalBufferExport.GetShellCommandRanges(shell.Engine.CreateSnapshot(true).Buffer));
            shell.Connection.Write("exit\n"); await shell.Exit.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { Directory.Delete(directory, true); }
    }

    [UnixFact]
    public async Task OrdinaryTmuxAndLessReturnToInteractiveShell()
    {
        await using var shell = new Shell(); await shell.Start();
        await using var server = new TmuxTests.Server(); var socket = server.Socket;
        // A unique server avoids touching the user's existing tmux sessions.
        shell.Connection.Write($"tmux -L {socket} -f /dev/null new-session -s acceptance \"env PS1='__TMUX_READY__ ' /bin/sh\"\n");
        await shell.Until(() => shell.Engine.AlternateBufferActive && shell.Snapshot().Contains("__TMUX_READY__"));
        shell.Connection.Write("printf '%s%s\\n' '__TMUX_' 'OK__'\n");
        await shell.Until(() => shell.Snapshot().Contains("__TMUX_OK__"));
        // Match the control's grid/PTY resize and wait for a real command in the
        // resized tmux pane before exiting, so capability replies are consumed.
        shell.Engine.Resize(105, 35); shell.Connection.Resize(105, 35);
        // The outer PTY resize is asynchronous; tmux must then resize its own
        // pane PTY. Query that owned device before sending a one-shot command.
        var tty = (await server.Run("display-message", "-p", "-t", "acceptance:0.0", "#{pane_tty}")).Trim();
        var timer = Stopwatch.StartNew(); string size;
        while ((size = await OwnedPtySize(tty)) != "34 105")
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException($"Owned tmux PTY remained {size}; expected 34 105.");
            await Task.Delay(20);
        }
        shell.Connection.Write("stty size; printf '%s%s\\n' '__TMUX_RESIZED_' 'READY__'\n");
        await shell.Until(() => shell.Snapshot().Contains("34 105") && shell.Snapshot().Contains("__TMUX_RESIZED_READY__"));
        shell.Connection.Write("exit\n");
        await shell.Until(() => !shell.Engine.AlternateBufferActive);
        shell.Connection.Write("printf 'FIRST\\nSECOND\\n' | less -+F\n");
        await shell.Until(() => shell.Engine.AlternateBufferActive);
        await shell.Until(() => shell.Snapshot().Contains("SECOND"));
        shell.Connection.Write("q"); await shell.Until(() => !shell.Engine.AlternateBufferActive);
        shell.Connection.Write("printf '%s%s\\n' '__AFTER_' 'COMPAT__'\n");
        await shell.Until(() => shell.Output.Contains("__AFTER_COMPAT__"));
    }

    private static async Task<string> OwnedPtySize(string tty)
    {
        if (!tty.StartsWith("/dev/", StringComparison.Ordinal) || tty.Length > 256 || Path.GetFullPath(tty) != tty || tty.Any(char.IsControl))
            throw new IOException("Invalid owned tmux PTY path.");
        var start = new ProcessStartInfo("/bin/stty") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(OperatingSystem.IsMacOS() ? "-f" : "-F"); start.ArgumentList.Add(tty); start.ArgumentList.Add("size");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        if (process.ExitCode != 0) throw new IOException("Owned PTY dimensions: " + await error);
        return (await output).Trim();
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
        await shell.Until(() => shell.Engine.AlternateBufferActive && shell.Snapshot().Contains("VIM - Vi IMproved"));
        shell.Connection.Write("iASTER_VIM_ACCEPTANCE\u001b");
        await shell.Until(() => shell.Snapshot().Contains("ASTER_VIM_ACCEPTANCE"));
        shell.Connection.Resize(100, 35);
        shell.Connection.Write(":q!\r");
        await shell.Until(() => !shell.Engine.AlternateBufferActive);
        shell.Connection.Write("printf '%s%s\\n' '__ASTER_' 'AFTER_VIM__'\n");
        await shell.Until(() => shell.Output.Contains("__ASTER_AFTER_VIM__"));
    }

    [WindowsFact]
    public async Task PowerShellConPtyExecutesUnicodeAndCleansUp()
    {
        await using var shell = new Shell();
        await shell.Start("powershell.exe -NoLogo -NoProfile");
        // Process creation precedes PSReadLine readiness. Do not type a command
        // into PowerShell's startup/capability negotiation, especially on cold CI.
        await shell.Until(() => shell.Snapshot().Contains("PS ") && shell.Snapshot().TrimEnd().EndsWith('>'), TimeSpan.FromSeconds(30));
        // Use PowerShell's host output; legacy .NET Console.WriteLine chooses
        // the shell's OEM code page and can replace non-ASCII before ConPTY.
        shell.Connection.Write("Write-Output ('POWER' + 'SHELL_OK'); Write-Output ([char]0x754c)\r");
        await shell.Until(() => shell.Snapshot().Contains("POWERSHELL_OK") && shell.Snapshot().Contains("界"));
        shell.Connection.Resize(90, 30);
        Assert.Equal(90, shell.Connection.Columns);
        shell.Connection.Write("exit 0\r");
        Assert.Equal(0, await shell.Exit.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.False(shell.Connection.IsRunning);
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
