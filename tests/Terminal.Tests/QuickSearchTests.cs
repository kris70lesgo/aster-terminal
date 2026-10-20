using Aster.App.Services;

namespace Aster.Tests;

public sealed class QuickSearchTests
{
    [Theory]
    [InlineData("zsh", "Session · -zsh · Shell · /Users/me", true)]
    [InlineData("ssh", "Session · -zsh · Shell · /Users/me", true)]
    [InlineData("python", "New tab · Python · python3", true)]
    [InlineData("zzz", "New tab · Python · python3", false)]
    public void SearchUsesCaseInsensitiveSubsequenceMatching(string query, string candidate, bool expected)
        => Assert.Equal(expected, QuickSearch.Score(query, candidate).HasValue);

    [Fact]
    public void SearchRanksContiguousPrefixAboveLooserSubsequence()
    {
        var exactStart = QuickSearch.Score("zsh", "zsh · shell · /bin/zsh");
        var loose = QuickSearch.Score("zsh", "Session · -zsh · Shell · /Users/me");
        Assert.True(exactStart > loose);
    }

    [Fact]
    public void EmptyQueryKeepsAllChoicesAndNullNeverMatches()
    {
        Assert.Equal(0, QuickSearch.Score("  ", "anything"));
        Assert.Null(QuickSearch.Score("x", ""));
    }
}
