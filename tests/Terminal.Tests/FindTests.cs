using Aster.Sessions.Protocols;
using Aster.Sessions.Search;

namespace Aster.Tests;

public sealed class FindTests
{
    private static FindResult Find(ItermTerminalEngine engine, string query, bool regex = false, bool matchCase = false, bool word = false) => ScrollbackSearch.Find(engine.CaptureFindSnapshot(TestContext.Current.CancellationToken), new(query, regex, matchCase, word), TestContext.Current.CancellationToken);
    [Fact]
    public void UnicodeCanonicalGraphemesWideCellsAndSoftWrapsMapToWholeDisplayCells()
    {
        using var engine = new ItermTerminalEngine(8, 4);
        engine.Feed("123456東京e\u0301 👩‍💻!");
        var found = Find(engine, "東京é"); var hit = Assert.Single(found.Hits);
        Assert.Equal(6, hit.Start.LogicalOffset); Assert.Equal(10, hit.End.LogicalOffset);
        Assert.True(engine.RevealFindHit(found, hit));
        var ranges = engine.ProjectFindHits(found, hit);
        Assert.Contains(ranges, r => r.StartColumn == 6 && r.EndColumn == 7);
        Assert.Contains(ranges, r => r.StartColumn == 0 && r.EndColumn == 2);
        Assert.Empty(Find(engine, "👩").Hits); Assert.Single(Find(engine, "👩‍💻").Hits);
        Assert.Single(Find(engine, "東京é", regex: true).Hits);
    }
    [Fact]
    public void RegexCaseWordsHardLinesAndInvalidPatternsHaveExplicitBehavior()
    {
        using var engine = new ItermTerminalEngine(24, 8);
        engine.Feed("ERROR 42\r\nerror 7\r\nterror 8\r\nERROR 123東京\r\nfoo\r\nbar");
        Assert.Equal(3, Find(engine, "error", word: true).Hits.Count);
        Assert.Equal(2, Find(engine, "ERROR", matchCase: true, word: true).Hits.Count);
        Assert.Equal(2, Find(engine, "^error [0-9]+$", regex: true).Hits.Count);
        Assert.Single(Find(engine, "(?<=ERROR )42", regex: true).Hits);
        Assert.Empty(Find(engine, "foo.*bar", regex: true).Hits);
        Assert.NotNull(Find(engine, "[", regex: true).Error);
        Assert.Empty(Find(engine, "^", regex: true).Hits);
        Assert.Single(Find(engine, "東京", word: false).Hits);
        Assert.Empty(Find(engine, "東京", word: true).Hits); // Adjacent decimal digits remain a word.
    }
    [Fact]
    public void LogicalAnchorsSurviveReflowAndEvictionButNeverResetOrAnotherBuffer()
    {
        using var engine = new ItermTerminalEngine(12, 3, 8);
        engine.Feed("prefix target suffix\r\nsecond target\r\nthird");
        var result = Find(engine, "target"); var first = result.Hits[0]; var last = result.Hits[1];
        engine.Resize(8, 3); Assert.True(engine.RevealFindHit(result, first)); Assert.NotEmpty(engine.ProjectFindHits(result, first));
        engine.Feed("\u001b[?1049hALTERNATE"); Assert.False(engine.RevealFindHit(result, first));
        engine.Feed("\u001b[?1049l"); Assert.False(engine.RevealFindHit(result, last)); // Buffer round-trip invalidates a captured result.
        result = Find(engine, "target"); first = result.Hits[0];
        engine.Feed(string.Concat(Enumerable.Repeat("\r\nnew output", 20))); Assert.False(engine.RevealFindHit(result, first));
        engine.Reset(); engine.Feed("target"); Assert.False(engine.RevealFindHit(result, first));
    }
    [Fact]
    public void BoundedCapturePreservesScrollAndRejectsTruncatedLogicalPrefixes()
    {
        using var engine = new ItermTerminalEngine(8, 3, 50);
        engine.Feed("old target\r\n" + string.Concat(Enumerable.Repeat("abcdefgh", 10)) + "TAIL\r\nnew target");
        engine.SetScrollOffset(4); var offset = engine.ScrollOffset;
        var snapshot = engine.CaptureFindSnapshot(TestContext.Current.CancellationToken, maximumCells: 40);
        Assert.Equal(offset, engine.ScrollOffset); Assert.True(snapshot.Limited); Assert.Equal(5, snapshot.Buffer.Lines.Count);
        var result = ScrollbackSearch.Find(snapshot, new("^.*TAIL$", Regex: true), TestContext.Current.CancellationToken);
        Assert.Empty(result.Hits); Assert.NotNull(result.Warning);
        Assert.Single(ScrollbackSearch.Find(snapshot, new("new target"), TestContext.Current.CancellationToken).Hits);
        using var evicted = new ItermTerminalEngine(8, 3, 0); evicted.Feed(new string('x', 200));
        var surviving = evicted.CaptureFindSnapshot(TestContext.Current.CancellationToken); Assert.False(surviving.Limited);
        var incomplete = ScrollbackSearch.Find(surviving, new("^x+", Regex: true), TestContext.Current.CancellationToken);
        Assert.Empty(incomplete.Hits); Assert.Contains("Truncated", incomplete.Warning);
    }
    [Fact]
    public void InvalidUnicodeOversizeQueriesCancellationAndPathologicalRegexAreBounded()
    {
        using var engine = new ItermTerminalEngine(100, 2, 1000);
        engine.Feed(new string('a', 50000) + "!");
        Assert.NotNull(Find(engine, new string('x', 1025)).Error);
        Assert.NotNull(Find(engine, "\ud800").Error);
        var expensive = Find(engine, "^(a+)+$", regex: true); Assert.NotNull(expensive.Warning);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        Assert.Throws<OperationCanceledException>(() => engine.CaptureFindSnapshot(canceled.Token));
        var snapshot = engine.CaptureFindSnapshot(TestContext.Current.CancellationToken);
        Assert.Throws<OperationCanceledException>(() => ScrollbackSearch.Find(snapshot, new("x"), canceled.Token));
    }
    [Fact]
    public void DenseMatchesAreCappedAndReportedWithoutFakingAnExactCount()
    {
        using var engine = new ItermTerminalEngine(100, 3, 1000);
        engine.Feed(string.Concat(Enumerable.Repeat("x ", 11000)));
        var result = Find(engine, "x"); Assert.Equal(ScrollbackSearch.MaximumHits, result.Hits.Count); Assert.Contains("10,000", result.Warning);
    }
}
