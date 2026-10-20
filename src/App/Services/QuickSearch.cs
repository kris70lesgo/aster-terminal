namespace Aster.App.Services;

/// <summary>Small, allocation-light subsequence scoring for the Open Quickly palette.</summary>
internal static class QuickSearch
{
    public static int? Score(string query, string candidate)
    {
        if (string.IsNullOrWhiteSpace(query)) return 0;
        if (string.IsNullOrEmpty(candidate)) return null;

        var score = 0;
        var candidateIndex = 0;
        var previousMatch = -2;
        foreach (var wanted in query.Trim())
        {
            var found = -1;
            for (var i = candidateIndex; i < candidate.Length; i++)
            {
                if (char.ToUpperInvariant(candidate[i]) == char.ToUpperInvariant(wanted))
                {
                    found = i;
                    break;
                }
            }
            if (found < 0) return null;

            var wordStart = found == 0 || !char.IsLetterOrDigit(candidate[found - 1]);
            score += found == previousMatch + 1 ? 12 : 0;
            score += wordStart ? 9 : 0;
            score += found == candidateIndex ? 2 : 0;
            score -= Math.Min(found - candidateIndex, 8);
            previousMatch = found;
            candidateIndex = found + 1;
        }

        if (candidate.StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase)) score += 24;
        if (string.Equals(candidate, query.Trim(), StringComparison.OrdinalIgnoreCase)) score += 40;
        return score;
    }
}
