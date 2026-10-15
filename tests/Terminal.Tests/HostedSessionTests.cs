using System.Diagnostics;
using System.Text;
using Aster.Sessions.Hosting;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Connection;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class HostedSessionTests
{
    private sealed class Client : IAsyncDisposable
    {
        public HostedConnection Connection { get; }
        public ItermTerminalEngine Engine { get; } = new(80, 24) { ProtocolResponsesEnabled = false };
        public Client(SessionCatalog catalog, HostedSession? session = null)
        {
            Connection = new(catalog, "test-profile", "Recovery test", session);
            Connection.Restoring += (_, _) => Engine.Reset();
            Connection.GridChanged += (_, grid) => Engine.Resize(grid.Columns, grid.Rows);
            Connection.OutputReceived += (_, bytes) => Engine.Feed(bytes.Span);
        }
        public string Text => TerminalBufferExport.ToPlainText(Engine.CreateSnapshot(true).Buffer);
        public async Task Start()
        {
            await Connection.StartAsync(new TerminalLaunchOptions { CommandLine = OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", WorkingDirectory = Path.GetTempPath(), EnvironmentVariables = new Dictionary<string, string?> { ["TERM"] = "xterm-256color", ["PROMPT"] = "ASTER_READY$G" }, Columns = 80, Rows = 24 });
            await Until(() => Text.Contains("ASTER_READY>"));
        }
        public void Command(string text) => Connection.Write(text + (OperatingSystem.IsWindows() ? "\r" : "\n"));
        public async ValueTask DisposeAsync() { await Connection.DisposeAsync(); Engine.Dispose(); }
    }
    [Fact]
    public async Task RealShellSurvivesDetachedClientAndRetainsVariablesIdentityOutputAndResize()
    {
        var root = Path.Combine(Path.GetTempPath(), "aster-host-tests-" + Guid.NewGuid().ToString("N"));
        var catalog = new SessionCatalog(root); HostedSession? session = null;
        try
        {
            TerminalProcessMetadata metadata;
            await using (var first = new Client(catalog))
            {
                await first.Start(); session = first.Connection.Session!; metadata = first.Connection.ProcessMetadata!;
                first.Command(OperatingSystem.IsWindows() ? "set ASTER_LIVE_VALUE=still-alive" : "ASTER_LIVE_VALUE=still-alive");
                first.Command(OperatingSystem.IsWindows() ? "echo __BEFORE_DETACH__" : "printf '%s%s\\n' '__BEFORE_' 'DETACH__'");
                await Until(() => first.Text.Contains("__BEFORE_DETACH__"));
                await first.Connection.DetachAsync();
            }
            await UntilAsync(async () => (await catalog.DetachedAsync()).Count == 1);
            await using var second = new Client(catalog, session); await second.Start();
            Assert.Equal(metadata, second.Connection.ProcessMetadata);
            Assert.Contains("__BEFORE_DETACH__", second.Text);
            second.Command(OperatingSystem.IsWindows() ? "echo __RECOVERED__%ASTER_LIVE_VALUE%" : "printf '__RECOVERED__%s\\n' \"$ASTER_LIVE_VALUE\"");
            await Until(() => second.Text.Contains("__RECOVERED__still-alive"));
            second.Connection.Resize(103, 37); await Until(() => second.Connection.Columns == 103 && second.Connection.Rows == 37);
            if (!OperatingSystem.IsWindows()) { second.Command("stty size"); await Until(() => second.Text.Contains("37 103")); }
            second.Command("exit 7"); await Until(() => second.Connection.LastExitInfo is not null);
            Assert.Equal(7, second.Connection.LastExitInfo!.ExitCode);
            await Until(() => !File.Exists(catalog.FilePath(session.Id)));
        }
        finally
        {
            if (session is not null && File.Exists(catalog.FilePath(session.Id))) try { await catalog.TerminateAsync(session); } catch (IOException) { }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    [Fact]
    public async Task AttachedSessionsAreExclusiveAndInvalidCapabilitiesCannotControlThem()
    {
        var root = Path.Combine(Path.GetTempPath(), "aster-host-auth-" + Guid.NewGuid().ToString("N")); var catalog = new SessionCatalog(root);
        try
        {
            await using var first = new Client(catalog); await first.Start(); var session = first.Connection.Session!;
            Assert.Empty(await catalog.DetachedAsync(TestContext.Current.CancellationToken));
            await using var duplicate = new Client(catalog, session);
            var error = await Assert.ThrowsAsync<IOException>(() => duplicate.Start()); Assert.Contains("already attached", error.Message);
            await using var wrongToken = new Client(catalog, session with { Token = new string('0', 64) });
            await Assert.ThrowsAnyAsync<IOException>(() => wrongToken.Start());
            Assert.True(first.Connection.IsRunning);
            first.Command(OperatingSystem.IsWindows() ? "echo __STILL_OWNED__" : "printf '%s%s\\n' '__STILL_' 'OWNED__'");
            await Until(() => first.Text.Contains("__STILL_OWNED__"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static async Task Until(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Hosted session acceptance timed out."); await Task.Delay(25); }
    }
    private static async Task UntilAsync(Func<Task<bool>> condition)
    {
        var watch = Stopwatch.StartNew(); while (!await condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Hosted session recovery timed out."); await Task.Delay(25); }
    }
}
