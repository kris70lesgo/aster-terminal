using System.Diagnostics;
using System.Text;
using Aster.Sessions.Protocols;
using Aster.Sessions.Tmux;
using Devolutions.Terminal.Connection;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class TmuxTests
{
    [Fact]
    public void EscapedByteOutputAndNestedLayoutAreStrictAndBounded()
    {
        Assert.Equal(Encoding.UTF8.GetBytes("\u001b[31mλ\\\r\n"), TmuxProtocol.DecodeOutput("\\033[31m\\316\\273\\134\\015\\012"));
        Assert.Throws<InvalidDataException>(() => TmuxProtocol.DecodeOutput("bad\\9"));
        using var engine = new ItermTerminalEngine(80, 24);
        engine.Feed(TmuxProtocol.DecodeOutput("\xCE")); engine.Feed(TmuxProtocol.DecodeOutput("\xBB"));
        Assert.Contains("λ", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
        engine.GridOwnedExternally = true; engine.Resize(30, 10, 11, 22);
        Assert.Equal(80, engine.Columns); Assert.Equal(24, engine.Rows);
        engine.ResizeKeepingCellMetrics(60, 20); Assert.Equal(60, engine.Columns); Assert.Equal((11d, 22d), engine.CellPixels);
        var layout = TmuxLayout.Parse("abcd,120x40,0,0{60x40,0,0,1,59x40,61,0[59x20,61,0,2,59x19,61,21,3]}");
        Assert.Equal(new int?[] { 1, 2, 3 }, layout.Panes.Select(p => p.PaneId));
        Assert.True(layout.Children[1].Vertical);
        Assert.Throws<InvalidDataException>(() => TmuxLayout.Parse("abcd,999999x40,0,0,1"));
        Assert.Throws<InvalidDataException>(() => TmuxLayout.Parse("abcd,80x24,0,0{40x24,0,0,1,39x24,41,0,1}"));
        Assert.Throws<ArgumentException>(() => new TmuxController("session\nkill-server"));
    }
    internal sealed class Server : IAsyncDisposable
    {
        public string Socket { get; } = "aster-control-tests-" + Guid.NewGuid().ToString("N");
        public async Task Start()
        {
            await Run("-f", "/dev/null", "new-session", "-d", "-s", "acceptance", "-x", "80", "-y", "24", "env PS1='ASTER_READY> ' /bin/sh");
            await Run("set-option", "-t", "acceptance", "default-shell", "/bin/sh");
            await Run("set-option", "-t", "acceptance", "default-command", "env PS1='ASTER_READY> ' /bin/sh");
        }
        public async Task<string> Run(params string[] args)
        {
            var start = new ProcessStartInfo("tmux") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add("-L"); start.ArgumentList.Add(Socket); foreach (var arg in args) start.ArgumentList.Add(arg);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            var errors = await error; if (process.ExitCode != 0) throw new IOException("tmux fixture: " + errors); return await output;
        }
        public async ValueTask DisposeAsync() { try { await Run("kill-server"); } catch (IOException) { } }
    }
    private sealed class Pane : IAsyncDisposable
    {
        public TmuxPaneConnection Connection { get; }
        public ItermTerminalEngine Engine { get; } = new(80, 24) { ProtocolResponsesEnabled = false };
        public string Text => TerminalBufferExport.ToPlainText(Engine.CreateSnapshot(true).Buffer);
        public Pane(TmuxController client, int id)
        {
            Connection = client.CreatePane(id); Connection.GridChanged += (_, size) => Engine.Resize(size.Columns, size.Rows);
            Connection.OutputReceived += (_, bytes) => Engine.Feed(bytes.Span);
        }
        public Task Start() => Connection.StartAsync(new TerminalLaunchOptions { CommandLine = "unused" });
        public async ValueTask DisposeAsync() { await Connection.DisposeAsync(); Engine.Dispose(); }
    }
    [UnixFact]
    public async Task ControlModeRoutesIndependentPanesResizesAndPreservesLiveShellAcrossDetach()
    {
        await using var server = new Server(); await server.Start();
        var client = new TmuxController("acceptance", server.Socket);
        try
        {
            await client.StartAsync(); var windows = await client.WindowsAsync(); Assert.Single(windows);
            var id = windows[0].ActivePane;
            await using (var first = new Pane(client, id))
            {
                await first.Start(); await Until(() => first.Text.Contains("ASTER_READY>"));
                first.Connection.Write("ASTER_TMUX_VALUE=retained; printf '__FIRST__%s\\n' \"$ASTER_TMUX_VALUE\"\r");
                await Until(() => first.Text.Split('\n').Any(l => l.Trim() == "__FIRST__retained"));
                await client.SplitAsync(id, false); windows = await client.WindowsAsync(); Assert.Equal(2, windows[0].Layout.Panes.Count());
                var secondId = windows[0].Layout.Panes.Single(p => p.PaneId != id).PaneId!.Value;
                await using var second = new Pane(client, secondId); await second.Start();
                second.Connection.Write("printf '__SECOND__%s\\n' 'λ界'; printf '\\033[31m__RED__\\033[0m\\n'\r");
                await Until(() => second.Text.Contains("__SECOND__λ界") && second.Text.Contains("__RED__"));
                Assert.DoesNotContain("__SECOND__λ界", first.Text);
                await client.ResizeAsync(112, 39); await UntilAsync(async () => (await client.WindowsAsync())[0].Layout.Columns == 112);
                var dimensions = await server.Run("display-message", "-p", "-t", "%" + id, "#{pane_width} #{pane_height}");
                Assert.Contains("39", dimensions);
                first.Connection.Write("printf '__FRAGMENT__\\316'; sleep .05; printf '\\273\\n'\r");
                await Until(() => first.Text.Contains("__FRAGMENT__λ"));
                first.Connection.Write("vim -Nu NONE -n -i NONE -c 'call setline(1, \"ASTER_CONTROL_READY\")'\r");
                try { await Until(() => first.Engine.AlternateBufferActive && first.Text.Contains("ASTER_CONTROL_READY")); }
                catch (TimeoutException error) { throw new TimeoutException($"Vim did not initialize: alternate={first.Engine.AlternateBufferActive}; {first.Text}", error); }
                first.Connection.Write("iASTER_CONTROL_VIM\u001b"); await Until(() => first.Text.Contains("ASTER_CONTROL_VIM"));
                first.Connection.Write(":q!\r"); await Until(() => !first.Engine.AlternateBufferActive);
                await client.KillPaneAsync(secondId); Assert.Single((await client.WindowsAsync())[0].Layout.Panes);
                await client.NewWindowAsync(); Assert.Equal(2, (await client.WindowsAsync()).Count);
            }
            await client.DisposeAsync(); Assert.Contains("acceptance", await server.Run("list-sessions"));
            await using var recovered = new TmuxController("acceptance", server.Socket); await recovered.StartAsync();
            await using var same = new Pane(recovered, id); await same.Start();
            same.Connection.Write("printf '__REATTACHED__%s\\n' \"$ASTER_TMUX_VALUE\"\r");
            await Until(() => same.Text.Split('\n').Any(l => l.Trim() == "__REATTACHED__retained"));
        }
        finally { await client.DisposeAsync(); }
    }
    private static async Task Until(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew(); while (!condition()) { if (timer.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Tmux acceptance timed out."); await Task.Delay(25); }
    }
    private static async Task UntilAsync(Func<Task<bool>> condition)
    {
        var timer = Stopwatch.StartNew(); while (!await condition()) { if (timer.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Tmux acceptance timed out."); await Task.Delay(25); }
    }
}
