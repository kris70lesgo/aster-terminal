using System.Text;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Aster.Sessions.Protocols;
using Aster.Sessions.Triggers;
using Devolutions.Terminal.Core;

namespace Aster.Tests;

public sealed class TriggerTests
{
    [Fact]
    public void FragmentedWrappedUnicodeMatchesActualCellsAndNavigatesAfterResize()
    {
        using var engine = new ItermTerminalEngine(8, 3, 100);
        engine.ConfigureTriggers([new() { Name = "Unicode", Pattern = "界éERROR" }]);
        foreach (var b in Encoding.UTF8.GetBytes("prefix界éERROR")) engine.Feed(new[] { b });
        Assert.Empty(engine.TriggerMatches);
        engine.Feed("\r\n");
        var hit = Assert.Single(engine.TriggerMatches);
        Assert.Contains("prefix界éERROR", hit.Text);
        var highlighted = engine.CreateSnapshot(true).Buffer.Lines.SelectMany(l => l.Cells).Where(c => c.Attributes.Background.Kind == ColorKind.Rgb).ToArray();
        Assert.Equal(8, highlighted.Length); // wide glyph + combining cell + ERROR
        Assert.True(highlighted[1].IsWideContinuation);
        for (var i = 0; i < 12; i++) engine.Feed($"other {i}\r\n");
        engine.Resize(12, 4);
        Assert.True(engine.NavigateToTrigger(hit)); Assert.True(engine.ScrollOffset > 0);
        Assert.Contains("prefix", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
        engine.ClearTriggerMatches(); Assert.Empty(engine.TriggerMatches);
    }

    [Fact]
    public void MatchingUsesRewrittenScreenTextAndIgnoresEscapePayloadsAndAlternateScreen()
    {
        using var engine = new ItermTerminalEngine(40, 4);
        engine.ConfigureTriggers([new() { Pattern = "ERROR", Action = "Bookmark" }]);
        engine.Feed("ERROR\rCLEAR\r\n\u001b]2;ERROR\u0007okay\r\n\u001b[?1049hERROR\r\n\u001b[?1049l");
        Assert.Empty(engine.TriggerMatches);
        engine.Feed("\u001b[31mERROR\u001b[0m\r\n");
        Assert.Single(engine.TriggerMatches);
        Assert.DoesNotContain(engine.CreateSnapshot(true).Buffer.Lines.SelectMany(l => l.Cells), c => c.Attributes.Background.Kind == ColorKind.Rgb);
        engine.Reset(); Assert.Empty(engine.TriggerMatches);
    }

    [Fact]
    public void MatchingWhileScrolledPreservesTheViewAndSkipsOversizeLines()
    {
        using var engine = new ItermTerminalEngine(40, 4, 100);
        for (var i = 0; i < 20; i++) engine.Feed($"old {i}\r\n");
        engine.SetScrollOffset(8);
        var oldView = TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer);
        engine.ConfigureTriggers([new() { Pattern = "^ERROR$" }]);
        engine.Feed("ERROR\r\n");
        Assert.Single(engine.TriggerMatches);
        Assert.True(engine.ScrollOffset >= 8);
        Assert.Equal(oldView, TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
        Assert.DoesNotContain("ERROR", TerminalBufferExport.ToPlainText(engine.CreateSnapshot().Buffer));
        Assert.Contains("ERROR", TerminalBufferExport.ToPlainText(engine.CreateSnapshot(true).Buffer));
        engine.Feed(new string('x', 5000) + "ERROR\r\n");
        Assert.Single(engine.TriggerMatches);
    }

    [Fact]
    public void ProfileRulesAreBoundedValidatedAndPersistedWithoutChangingOriginalOnFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-trigger-settings-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = store.Settings.Profiles[0] with { Triggers = [new() { Pattern = "(?i)error|warning", Action = "Bookmark" }] };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id });
            Assert.Equal(profile.Triggers, new SettingsStore(store.Path).Settings.Profiles[0].Triggers);
            var original = File.ReadAllText(store.Path);
            foreach (var pattern in new[] { "[", "(?=a)", "(a)\\1", "" })
                Assert.Throws<ArgumentException>(() => store.Save(store.Settings with { Profiles = [profile with { Triggers = [new() { Pattern = pattern }] }] }));
            Assert.Throws<ArgumentException>(() => new OutputTrigger { Background = "#oops" }.Validate());
            Assert.Throws<ArgumentException>(() => new OutputTrigger { Action = "RunCommand" }.Validate());
            Assert.Throws<ArgumentException>(() => store.Save(store.Settings with { Profiles = [profile with { Triggers = Enumerable.Repeat(new OutputTrigger(), 33).ToList() }] }));
            Assert.Equal(original, File.ReadAllText(store.Path));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void MatchHistoryIsBoundedEvictedAnchorsFailAndDisabledRulesDoNotFire()
    {
        using var engine = new ItermTerminalEngine(40, 3, 10);
        engine.ConfigureTriggers([new() { Pattern = "record", Action = "Bookmark" }, new() { Pattern = ".", Enabled = false }]);
        engine.Feed("record 0\r\n"); var first = Assert.Single(engine.TriggerMatches);
        for (var i = 1; i <= 240; i++) engine.Feed($"record {i}\r\n");
        Assert.Equal(200, engine.TriggerMatches.Count);
        Assert.False(engine.NavigateToTrigger(first));
        engine.ConfigureTriggers([]); engine.Feed("record disabled\r\n");
        Assert.Equal(200, engine.TriggerMatches.Count);
    }
}
