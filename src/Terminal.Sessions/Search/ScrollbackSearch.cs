// Cell/NFC/grapheme and Unicode-word mapping adapted from Devolutions
// TextBufferSearch.cs, fa23083f1bda1a60fe2b322c6cfab4a616af9edb.
// Copyright (c) Devolutions Inc.; portions Copyright (c) Microsoft Corporation.
// Original MIT notice retained in licenses/Devolutions-MIT.txt.
// Aster adaptations 2026-10-16: bounded regex, cancellation and logical anchors.
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Devolutions.Terminal.Core;

namespace Aster.Sessions.Search;

public sealed record FindOptions(string Query, bool Regex = false, bool CaseSensitive = false, bool WholeWord = false, bool SmartCase = false);
public sealed record FindHit(TerminalImageAnchor Start, TerminalImageAnchor End);
public sealed record FindSnapshot(TextBufferSnapshot Buffer, long Epoch, long Revision, bool Limited);
public sealed record FindVisibleRange(int Row, int StartColumn, int EndColumn, bool Current);
public sealed record FindResult(FindOptions Options, IReadOnlyList<FindHit> Hits, long Epoch, long Revision, string? Warning = null, string? Error = null)
{
    internal IReadOnlyDictionary<long, FindHit[]> Groups { get; } = Hits.GroupBy(h => h.Start.LogicalLineId).ToDictionary(g => g.Key, g => g.ToArray());
}

public static class ScrollbackSearch
{
    public const int MaximumCells = 1_000_000, MaximumRows = 20_000, MaximumQuery = 1024, MaximumHits = 10_000;
    private const int MaximumLine = 65536, MaximumText = 4 * 1024 * 1024;
    public static FindResult Find(FindSnapshot snapshot, FindOptions options, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        FindResult Result(List<FindHit>? hits = null, string? warning = null, string? error = null) => new(options, hits?.ToArray() ?? [], snapshot.Epoch, snapshot.Revision, warning, error);
        if (options.Query.Length == 0) return Result();
        if (options.Query.Length > MaximumQuery) return Result(error: "Find text must contain at most 1,024 characters.");
        string query;
        try { query = options.Query.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return Result(error: "Find text contains invalid Unicode."); }
        var smartCaseSensitive = options.SmartCase && query.EnumerateRunes().Any(rune =>
            Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.TitlecaseLetter);
        var caseSensitive = options.CaseSensitive || smartCaseSensitive;
        Regex? regex = null;
        try { if (options.Regex) regex = new Regex(query, RegexOptions.CultureInvariant | (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase), TimeSpan.FromMilliseconds(25)); }
        catch (RegexParseException error) { return Result(error: $"Invalid regex at character {error.Offset + 1}."); }
        var hits = new List<FindHit>(); var watch = Stopwatch.StartNew(); var totalText = 0;
        var warning = snapshot.Limited ? "Older output was outside the search limit." : null;
        var line = new LogicalLine(); var skipping = false;
        for (var rowIndex = 0; rowIndex < snapshot.Buffer.Lines.Count; rowIndex++)
        {
            var row = snapshot.Buffer.Lines[rowIndex];
            token.ThrowIfCancellationRequested();
            if (watch.ElapsedMilliseconds > 300) return Result(hits, "Search time limit reached; results are partial.");
            if (line.Length == 0 && row.LogicalOffset != 0) { skipping = true; warning ??= "Truncated logical lines were skipped."; }
            if (!skipping && !line.Append(row)) { skipping = true; warning = "Some long output was outside the search limit."; }
            if (row.Wrapped && rowIndex + 1 < snapshot.Buffer.Lines.Count) continue;
            if (!skipping)
            {
                totalText += line.Length;
                if (totalText > MaximumText) return Result(hits, "Search text limit reached; results are partial.");
                var text = line.Text;
                try
                {
                    if (regex is not null)
                    {
                        foreach (var match in regex.EnumerateMatches(text))
                        {
                            token.ThrowIfCancellationRequested();
                            if (watch.ElapsedMilliseconds > 300) return Result(hits, "Search time limit reached; results are partial.");
                            Add(match.Index, match.Length);
                            if (hits.Count >= MaximumHits) return Result(hits, "First 10,000 matches shown.");
                        }
                    }
                    else
                    {
                        var start = 0;
                        while (start <= text.Length - query.Length)
                        {
                            token.ThrowIfCancellationRequested();
                            var index = text.IndexOf(query, start, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                            if (index < 0) break;
                            Add(index, query.Length); start = index + Math.Max(1, query.Length);
                            if (hits.Count >= MaximumHits) return Result(hits, "First 10,000 matches shown.");
                        }
                    }
                }
                catch (RegexMatchTimeoutException) { return Result(hits, "Regex time limit reached; try a narrower expression."); }
                void Add(int index, int length)
                {
                    if (length == 0 || !line.Boundary(index) || !line.Boundary(index + length)) return;
                    if (options.WholeWord && !WholeWord(text, index, index + length)) return;
                    hits.Add(new(line.Start(index), line.End(index + length - 1)));
                }
            }
            line = new(); skipping = false;
        }
        return Result(hits, warning);
    }
    private static bool WholeWord(string text, int start, int end) => (start == 0 || !WordRune(Before(text, start))) && (end == text.Length || !WordRune(At(text, end)));
    private static Rune Before(string text, int index) => Rune.DecodeLastFromUtf16(text.AsSpan(0, index), out var value, out _) == OperationStatus.Done ? value : Rune.ReplacementChar;
    private static Rune At(string text, int index) => Rune.DecodeFromUtf16(text.AsSpan(index), out var value, out _) == OperationStatus.Done ? value : Rune.ReplacementChar;
    private static bool WordRune(Rune value) => Rune.GetUnicodeCategory(value) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.ConnectorPunctuation or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
    private sealed class LogicalLine
    {
        private readonly StringBuilder _text = new();
        private readonly List<TerminalImageAnchor> _starts = [], _ends = [];
        private readonly List<bool> _boundaries = [true];
        public int Length => _text.Length;
        public string Text => _text.ToString();
        public bool Boundary(int index) => _boundaries[index];
        public TerminalImageAnchor Start(int index) => _starts[index];
        public TerminalImageAnchor End(int index) => _ends[index];
        public bool Append(TextBufferLineSnapshot row)
        {
            var count = row.Cells.Count;
            if (!row.Wrapped) while (count > 0 && row.Cells[count - 1].IsBlank) count--;
            for (var col = 0; col < count; col++)
            {
                var cell = row.Cells[col]; if (cell.IsWideContinuation) continue;
                if ((cell.CombiningCharacters?.Length ?? 0) > MaximumLine) return false;
                var text = cell.Text.Normalize(NormalizationForm.FormC);
                if (_text.Length + text.Length > MaximumLine) return false;
                var width = col + 1 < row.Cells.Count && row.Cells[col + 1].IsWideContinuation ? 2 : 1;
                _text.Append(text);
                for (var unit = 0; unit < text.Length; unit++)
                {
                    _starts.Add(new(row.LogicalLineId, row.LogicalOffset + col));
                    _ends.Add(new(row.LogicalLineId, row.LogicalOffset + col + width - 1));
                    _boundaries.Add(unit == text.Length - 1);
                }
            }
            return true;
        }
    }
}
