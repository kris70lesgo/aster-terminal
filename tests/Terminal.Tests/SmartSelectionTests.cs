using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Aster.Sessions.Protocols;
using Aster.Terminal;
using Aster.App.Views;

namespace Aster.Tests;

public sealed class SmartSelectionTests
{
    [Theory]
    [InlineData("See https://example.com/path, then stop", "example", "https://example.com/path")]
    [InlineData("Mail dev@example.org today", "example", "dev@example.org")]
    [InlineData("Host 192.168.1.42 online", "168", "192.168.1.42")]
    [InlineData("Read /var/log/aster.log now", "aster", "/var/log/aster.log")]
    public void SmartSelectionExpandsToCommonSemanticTokens(string line, string clickedPart, string expected)
    {
        var engine = new ItermTerminalEngine();
        engine.Resize(120, 2);
        engine.Feed(line);
        var snapshot = engine.CreateSnapshot().Buffer;
        var selection = TerminalInteractionModel.SmartSelectAt(
            snapshot,
            new TerminalSelectionPoint(line.IndexOf(clickedPart, StringComparison.Ordinal), 0));

        Assert.NotNull(selection);
        Assert.Equal(expected, TerminalInteractionModel.GetSelectedText(snapshot, selection!, trimBlockSelection: false));
    }

    [Fact]
    public void InvalidIpAddressAndOrdinaryWordsFallBackToNormalWordSelection()
    {
        var engine = new ItermTerminalEngine();
        engine.Resize(120, 2);
        engine.Feed("999.168.1.42 ordinary");
        var snapshot = engine.CreateSnapshot().Buffer;

        Assert.Null(TerminalInteractionModel.SmartSelectAt(snapshot, new TerminalSelectionPoint(5, 0)));
        Assert.Null(TerminalInteractionModel.SmartSelectAt(snapshot, new TerminalSelectionPoint(18, 0)));
    }

    [AvaloniaFact]
    public async Task DoubleClickUsesSavedSmartSelectionOptionInRealTerminalControl()
    {
        const string line = "Visit https://example.com/path today";
        var pane = new TerminalPane(new("smart", "unused", Path.GetTempPath()));
        pane.Terminal.Engine.Resize(100, 3);
        pane.Terminal.InteractionOptions = pane.Terminal.InteractionOptions with { DoubleClickSmartSelection = true };
        var window = new Window { Content = pane, Width = 700, Height = 120 };
        window.Show();
        try
        {
            window.UpdateLayout();
            await Task.Delay(30);
            pane.Terminal.Engine.Feed(line);
            var column = line.IndexOf("example", StringComparison.Ordinal) + 2;
            var viewport = pane.Terminal.Engine.CreateSnapshot().Buffer;
            var row = viewport.Lines.Select((value, index) => (value, index))
                .Single(item => string.Concat(item.value.Cells.Select(cell => cell.Text)).Contains(line, StringComparison.Ordinal)).index;
            var point = pane.Terminal.TranslatePoint(new Point(5 + column * pane.Terminal.CellSize.Width, 4 + (row + .5) * pane.Terminal.CellSize.Height), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal("https://example.com/path", pane.Terminal.BuildCopyPayload()?.Text);
        }
        finally
        {
            window.Close();
            await pane.CloseAsync();
        }
    }
}
