using System.Text;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Core;
using Aster.Terminal.Render;
using SkiaSharp;

namespace Aster.Tests;

public sealed class GraphicsTests
{
    [Theory]
    [InlineData("iterm", TerminalImageProtocol.Iterm2Inline)]
    [InlineData("sixel", TerminalImageProtocol.Sixel)]
    [InlineData("kitty", TerminalImageProtocol.KittyGraphics)]
    public void FragmentedImageProtocolsSurviveAdapterAndRenderActualPixels(string protocol, TerminalImageProtocol expected)
    {
        using var engine = new ItermTerminalEngine(16, 4);
        using var source = new SKBitmap(4, 4); source.Erase(SKColors.Red);
        using var png = source.Encode(SKEncodedImageFormat.Png, 100);
        var payload = protocol switch
        {
            "iterm" => $"\u001b]1337;File=inline=1;width=2;height=1:{Convert.ToBase64String(png.ToArray())}\u0007",
            "sixel" => "\u001bPq#2;2;100;0;0!12~\u001b\\",
            _ => $"\u001b_Ga=T,f=32,s=4,v=4,i=1,q=2,C=1;{Convert.ToBase64String(Enumerable.Repeat(new byte[] { 255, 0, 0, 255 }, 16).SelectMany(b => b).ToArray())}\u001b\\"
        };
        foreach (var b in Encoding.UTF8.GetBytes(payload)) engine.Feed(new[] { b });
        Assert.Equal(expected, Assert.Single(engine.CreateSnapshot().Images).Protocol);
        var frame = TerminalRenderPlanner.Create(engine.CreateSnapshot(), engine.Scheme);
        using var renderer = new SkiaTerminalRenderer();
        using var bitmap = new SKBitmap(400, 150); using var canvas = new SKCanvas(bitmap);
        renderer.Resize(new RenderViewport(frame.Columns, frame.Rows, 1));
        renderer.Render(canvas, frame, TerminalRenderOverlays.Empty, new SKRect(0, 0, 400, 150), 8, drawCursor: false);
        var pixel = bitmap.GetPixel(9, 9);
        Assert.True(pixel.Red > 200 && pixel.Green < 30 && pixel.Blue < 30, $"Expected red {protocol} image, found {pixel}");
        engine.Reset(); Assert.Empty(engine.CreateSnapshot().Images);
    }

    [Fact]
    public void ImagePayloadsCannotReadHostFilesAndMalformedSequencesRecover()
    {
        using var engine = new ItermTerminalEngine(40, 4);
        engine.Feed("\u001b_Ga=T,t=f,f=100,i=1;L2V0Yy9wYXNzd2Q=\u001b\\");
        engine.Feed("\u001b]1337;File=inline=1:@@@\u0007\u001b_Ga=T,f=32,s=99999,v=99999;AQID\u001b\\");
        engine.Feed("RECOVERED");
        Assert.Empty(engine.CreateSnapshot().Images);
        Assert.Contains("RECOVERED", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
    }
}
