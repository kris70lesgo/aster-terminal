using System.Buffers.Binary;
using System.Text;
using Aster.Sessions.Hosting;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class HostProtocolTests
{
    [Fact]
    public async Task FramingRejectsOversizeLengthsAndInvalidDimensions()
    {
        using var stream = new MemoryStream(); var header = new byte[5]; header[0] = HostProtocol.Output;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(1), HostProtocol.MaxFrame + 1); stream.Write(header); stream.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => HostProtocol.Read(stream, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentException>(() => HostProtocol.GridData(0, 24));
        Assert.Throws<ArgumentException>(() => HostProtocol.GridData(1000, 500));
    }
    [Fact]
    public void GridFramesRetainPixelGeometryForImagesAndWindowQueries()
    {
        var frame = HostProtocol.GridData(80, 24, 1600, 960);
        Assert.Equal((80, 24), HostProtocol.ReadGrid(frame)); Assert.Equal((1600, 960), HostProtocol.ReadPixels(frame));
        using var engine = new ItermTerminalEngine(80, 24); engine.Resize(80, 24, 20, 40); engine.ResizeKeepingCellMetrics(100, 30);
        Assert.Equal((20d, 40d), engine.CellPixels);
        engine.Feed("\u001bPq#2;2;100;0;0!12~\u001b\\");
        Assert.Equal(20d, Assert.Single(engine.CreateSnapshot().Images).CellGeometry!.CellWidth);
    }
    [Fact]
    public void TranscriptCompactionRetainsRealTextAndBoundsTinyFrames()
    {
        using var host = new ItermTerminalEngine(40, 4, 100);
        var transcript = new HostTranscript();
        var output = Encoding.UTF8.GetBytes("real output\r\n");
        for (var i = 0; i < 4096; i++) { host.Feed(output); transcript.Append(new(HostProtocol.Output, output), host); }
        host.Feed("\u001b[2J\u001b[H\u001b[38;2;12;34;56mREAL_CHECKPOINT_é界");
        transcript.Append(new(HostProtocol.Output, Encoding.UTF8.GetBytes("")), host);
        Assert.True(transcript.Checkpointed);
        Assert.True(transcript.Snapshot().Sum(f => f.Data.Length + 5) <= HostTranscript.Capacity);
        Assert.True(transcript.Snapshot().Count < 4096);
        using var recovered = new ItermTerminalEngine(80, 24);
        foreach (var frame in transcript.Snapshot())
        {
            if (frame.Kind == HostProtocol.Grid) { var grid = HostProtocol.ReadGrid(frame.Data); recovered.Resize(grid.Columns, grid.Rows); }
            if (frame.Kind == HostProtocol.Output) recovered.Feed(frame.Data);
        }
        Assert.Equal(TerminalBufferExport.ToPlainText(host.CreateSnapshot().Buffer), TerminalBufferExport.ToPlainText(recovered.CreateSnapshot().Buffer));
        Assert.Equal(host.Buffer.GetCell(0, 0).Attributes.Foreground.ToArgb(host.Scheme, true), recovered.Buffer.GetCell(0, 0).Attributes.Foreground.ToArgb(recovered.Scheme, true));
    }
}
