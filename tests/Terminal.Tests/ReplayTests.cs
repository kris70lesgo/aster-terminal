using Aster.Sessions.Protocols;
using Aster.Sessions.Replay;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class ReplayTests
{
    [Fact]
    public void FramesPreserveUnicodeColoursCursorAndOldState()
    {
        using var engine = new ItermTerminalEngine(30, 4);
        var replay = new ScreenReplayBuffer();
        var timestamp = DateTimeOffset.UtcNow;
        engine.Feed("\u001b[38;2;12;34;56ma界e\u0301");
        replay.Append(engine.CreateSnapshot(), engine.Scheme, timestamp);
        engine.Resize(50, 8); engine.Feed("\u001b[2J\u001b[Hnew screen");
        replay.Append(engine.CreateSnapshot(), engine.Scheme, timestamp.AddSeconds(1));
        var frames = replay.Snapshot();
        Assert.Equal(2, frames.Count);
        Assert.Equal(30, frames[0].Snapshot.Buffer.Columns);
        Assert.Equal(4, frames[0].Snapshot.Buffer.CursorX);
        Assert.StartsWith("a界e\u0301", TerminalBufferExport.ToPlainText(frames[0].Snapshot.Buffer));
        Assert.Equal(TermColor.FromRgb(12, 34, 56), frames[0].Snapshot.Buffer.Lines[0].Cells[0].Attributes.Foreground);
        using var display = new ReplayTerminalEngine(); display.ShowFrame(frames[0]);
        display.Resize(1, 1, 2, 2); display.Feed("unwanted input"); display.Reset();
        Assert.Equal(30, display.Columns); Assert.Equal(4, display.Rows);
        Assert.StartsWith("a界e\u0301", TerminalBufferExport.ToPlainText(display.CreateSnapshot().Buffer));
        Assert.Equal(50, frames[1].Snapshot.Buffer.Columns);
        Assert.StartsWith("new screen", TerminalBufferExport.ToPlainText(frames[1].Snapshot.Buffer));
    }

    [Fact]
    public void ReplayEvictsOldFramesRejectsOversizeAndClampsClockRegression()
    {
        using var engine = new ItermTerminalEngine(20, 3);
        var replay = new ScreenReplayBuffer(20000, 3);
        var time = DateTimeOffset.UtcNow;
        for (var i = 0; i < 10; i++)
        {
            engine.Feed($"\u001b[Hframe {i}");
            Assert.True(replay.Append(engine.CreateSnapshot(), engine.Scheme, time.AddSeconds(i)));
            Assert.InRange(replay.RetainedBytes, 1, replay.CapacityBytes);
        }
        var retained = replay.Snapshot(); Assert.Equal(3, retained.Count);
        Assert.Contains("frame 7", TerminalBufferExport.ToPlainText(retained[0].Snapshot.Buffer));
        replay.Append(engine.CreateSnapshot(), engine.Scheme, time);
        Assert.Equal(retained[^1].Timestamp, replay.Snapshot()[^1].Timestamp);
        engine.Resize(200, 100);
        Assert.False(replay.Append(engine.CreateSnapshot(), engine.Scheme, time));
        Assert.Equal(3, replay.Snapshot().Count);
        replay.Clear(); Assert.Empty(replay.Snapshot()); Assert.Equal(0, replay.RetainedBytes);
        engine.Resize(20, 3);
        var singleFrame = new ScreenReplayBuffer(20000, 1);
        singleFrame.Append(engine.CreateSnapshot(), engine.Scheme, time);
        singleFrame.Append(engine.CreateSnapshot(), engine.Scheme, time.AddSeconds(-10));
        Assert.Equal(time, Assert.Single(singleFrame.Snapshot()).Timestamp);
    }
}
