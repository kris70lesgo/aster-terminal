using Aster.Sessions;
using Aster.Sessions.Protocols;
using Aster.Terminal;
using Aster.Terminal.Render;
using Devolutions.Terminal.Core;
using SkiaSharp;
using Avalonia.Headless.XUnit;

namespace Aster.Tests;

public sealed class ReferenceRenderingTests
{
    [Fact]
    public void AsymmetricPaddingAlignsCursorAndKeepsViewportBackgroundPainted()
    {
        using var engine = new ItermTerminalEngine(2, 2);
        engine.Scheme = TerminalProfile.Default().ResolveColors(false);
        var frame = TerminalRenderPlanner.Create(engine.CreateSnapshot(), engine.Scheme,
            new TerminalRenderOptions { CursorStyle = TerminalCursorStyle.FilledBox });
        using var renderer = new SkiaTerminalRenderer();
        renderer.Resize(new RenderViewport(2, 2, 1));
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Magenta);
        renderer.Render(canvas, frame, TerminalRenderOverlays.Empty,
            new SKRect(0, 0, 100, 100), 5, true, paddingTop: 4);
        Assert.Equal(new SKColor(20, 25, 30), bitmap.GetPixel(0, 0));
        Assert.Equal(new SKColor(20, 25, 30), bitmap.GetPixel(99, 99));
        Assert.Equal(new SKColor(20, 25, 30), bitmap.GetPixel(5, 3));
        Assert.Equal(SKColors.White, bitmap.GetPixel(5, 4));
    }

    [Fact]
    public void NativeEmojiPresentationDoesNotFallBackToMonochromeMonacoSymbol()
    {
        if (!OperatingSystem.IsMacOS()) return;
        using var engine = new ItermTerminalEngine(4, 2);
        engine.Feed("☕");
        using var renderer = new SkiaTerminalRenderer(new TerminalRendererSettings { FontFamily = "Monaco", FontSize = 9 });
        renderer.Resize(new RenderViewport(4, 2, 2));
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        renderer.Render(canvas, TerminalRenderPlanner.Create(engine.CreateSnapshot(), engine.Scheme),
            TerminalRenderOverlays.Empty, new SKRect(0, 0, 100, 100), 5, false);
        Assert.Equal("Apple Color Emoji", renderer.LastResolvedFontFamily);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void BoldBrightensLowAnsiForegroundWithoutChangingBufferOrBackground(int index)
    {
        using var engine = new ItermTerminalEngine(8, 2);
        engine.Scheme = TerminalProfile.Default().ResolveColors(false);
        engine.Feed($"\u001b[1;{30 + index};{40 + index}mX");
        var snapshot = engine.CreateSnapshot();
        var cell = snapshot.Buffer.Lines[0].Cells[0];
        var frame = TerminalRenderPlanner.Create(snapshot, engine.Scheme);
        var run = frame.RowsData[0].Runs[0];
        Assert.Equal(engine.Scheme.Resolve(index + 8), run.Attributes.Foreground);
        Assert.Equal(engine.Scheme.Resolve(index), run.Attributes.Background);
        Assert.Equal(TermColor.FromIndex(index), cell.Attributes.Foreground);
        Assert.Equal(TermColor.FromIndex(index), cell.Attributes.Background);
    }

    [Theory]
    [InlineData("1;38;5;12", 0xFFA7ABF2u)]
    [InlineData("1;38;5;196", 0xFFFF0000u)]
    [InlineData("1;38;2;12;34;56", 0xFF0C2238u)]
    [InlineData("34", 0xFF2744C7u)]
    public void ExplicitBrightExtendedAndTrueColorRemainExact(string sgr, uint expected)
    {
        using var engine = new ItermTerminalEngine(8, 2);
        engine.Scheme = TerminalProfile.Default().ResolveColors(false);
        engine.Feed($"\u001b[{sgr}mX");
        var frame = TerminalRenderPlanner.Create(engine.CreateSnapshot(), engine.Scheme);
        Assert.Equal(expected, frame.RowsData[0].Runs[0].Attributes.Foreground);
    }

    [Fact]
    public void BoldInverseSwapsResolvedColors()
    {
        var scheme = TerminalProfile.Default().ResolveColors(false);
        var attributes = new CellAttributes
        {
            Foreground = TermColor.FromIndex(4), Background = TermColor.FromIndex(1),
            Flags = CellFlags.Bold | CellFlags.Inverse
        };
        var resolved = TerminalRenderPlanner.Resolve(attributes, null, scheme, false);
        Assert.Equal(scheme.Resolve(1), resolved.Foreground);
        Assert.Equal(scheme.Resolve(12), resolved.Background);
    }

    [AvaloniaFact]
    public void ReferenceMonacoCellGeometryUsesTheSameMeasurementAtBothScales()
    {
        if (!OperatingSystem.IsMacOS()) return; // Monaco is an installed system font, never bundled.
        var profile = new TerminalProfile("Reference", "/bin/zsh -l", "/", 12) { FontFamily = "Monaco" }.ToControlProfile();
        var normal = TermControl.MeasureCell(profile, 1);
        var retina = TermControl.MeasureCell(profile, 2);
        Assert.Equal(7, normal.Width);
        Assert.Equal(17, normal.Height);
        Assert.Equal(normal, retina);
    }
}
