using System.Text;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class EngineTests
{
    [Theory]
    [InlineData("D;0", 0)]
    [InlineData("D;aid=opaque;23;k=s", 23)]
    [InlineData("D;bad;aid=x;7;8", 7)]
    [InlineData("D;-1", -1)]
    [InlineData("D;123x", null)]
    [InlineData("D;aid=42", null)]
    [InlineData("D;2147483648", null)]
    [InlineData("A;9", null)]
    public void ItermExitCodeMatchesPositionalAttributeScan(string payload, int? expected) =>
        Assert.Equal(expected, ItermSemanticPrompt.ExitCode(payload));

    [Fact]
    public void FragmentedUtf8CombiningAndWideGlyphs()
    {
        using var engine = new ItermTerminalEngine(30, 4);
        foreach (var value in Encoding.UTF8.GetBytes("aé界e\u0301")) engine.Feed(new[] { value });
        Assert.Equal("a", engine.Buffer.GetCell(0, 0).Text);
        Assert.Equal("é", engine.Buffer.GetCell(1, 0).Text);
        Assert.Equal(2, engine.Buffer.GetCell(2, 0).DisplayWidth);
        Assert.True(engine.Buffer.GetCell(3, 0).IsWideContinuation);
        Assert.Equal("e\u0301", engine.Buffer.GetCell(4, 0).Text);
        Assert.Equal(5, engine.CursorX);
    }

    [Fact]
    public void CursorEraseTrueColorAndAttributes()
    {
        using var engine = new ItermTerminalEngine(20, 4);
        engine.Feed("abc\u001b[2;3H\u001b[1;38;2;12;34;56mX");
        var cell = engine.Buffer.GetCell(2, 1);
        Assert.Equal("X", cell.Text);
        Assert.True(cell.Attributes.Flags.HasFlag(CellFlags.Bold));
        Assert.Equal(TermColor.FromRgb(12, 34, 56), cell.Attributes.Foreground);
        engine.Feed("\u001b[2J\u001b[H");
        Assert.Equal(" ", engine.Buffer.GetCell(2, 1).Text);
        Assert.Equal(0, engine.CursorX);
        Assert.Equal(0, engine.CursorY);
    }

    [Fact]
    public void AlternateScreenRestoresMainBufferAndPasteMode()
    {
        using var engine = new ItermTerminalEngine(20, 4);
        engine.Feed("main\u001b[?1049hfullscreen\u001b[?2004h");
        Assert.True(engine.AlternateBufferActive);
        Assert.Equal("\u001b[200~hello\u001b[201~", engine.WrapPaste("hello"));
        engine.Feed("\u001b[?1049l\u001b[?2004l");
        Assert.False(engine.AlternateBufferActive);
        Assert.StartsWith("main", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
        Assert.Equal("hello", engine.WrapPaste("hello"));
    }

    [Fact]
    public void ItermMetadataAndCommandMarksSurviveByteFragmentation()
    {
        using var engine = new ItermTerminalEngine(80, 8);
        var stream = "\u001b]1337;CurrentDir=/tmp/example\u0007\u001b]1337;RemoteHost=me@host\u001b\\" +
            "\u001b]133;A\u0007$ \u001b]133;B\u0007false\u001b]133;C\u0007\r\noutput\r\n\u001b]133;D;aid=one;7\u0007";
        foreach (var value in Encoding.UTF8.GetBytes(stream)) engine.Feed(new[] { value });
        Assert.Equal("/tmp/example", engine.WorkingDirectory);
        Assert.Equal("me@host", engine.RemoteHost);
        Assert.Equal(7, engine.LastCommandExitCode);
        var commands = TerminalBufferExport.GetShellCommandRanges(engine.CreateSnapshot(true).Buffer);
        Assert.Single(commands);
        Assert.Equal(7u, commands[0].Mark.ExitCode);
        Assert.NotNull(commands[0].Command);
        Assert.NotNull(commands[0].Output);
    }

    [Fact]
    public void SearchScrollbackAfterResize()
    {
        using var engine = new ItermTerminalEngine(30, 3, 100);
        for (var i = 0; i < 12; i++) engine.Feed($"line {i}\r\n");
        Assert.True(engine.HistoryCount > 0);
        engine.Resize(15, 5);
        Assert.Equal(15, engine.Columns);
        Assert.Single(TextBufferSearch.FindAll(engine.CreateSnapshot(true).Buffer, "line 4"));
    }

    [Fact]
    public void UntrustedOscCannotWriteClipboardAndOversizeStringsRecover()
    {
        using var engine = new ItermTerminalEngine(30, 3);
        var clipboardWrites = 0;
        engine.ClipboardWriteRequested += (_, _) => clipboardWrites++;
        engine.Feed("\u001b]52;c;aGVsbG8=\u0007\u001b]1337;StealFocus=yes\u0007");
        engine.Feed("\u001b]1337;RemoteHost=" + new string('x', 4200) + "\u0007");
        Assert.Null(engine.RemoteHost);
        engine.Feed("\u001b]2;" + new string('x', 2 * 1024 * 1024) + "\u0007OK");
        Assert.Equal(0, clipboardWrites);
        Assert.Contains("OK", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
    }
}
