// Inspired by iTerm2 Trigger/HighlightTrigger/MarkTrigger, pinned 069b6e0.
// New C# implementation, GPL-3.0-or-later; no host actions are supported.
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Devolutions.Terminal.Core;

namespace Aster.Sessions.Triggers;

public sealed record OutputTriggerMatch(long Sequence, string Name, string Text, string Action, TerminalImageAnchor Anchor);

internal sealed class OutputTriggerProcessor
{
    private sealed record Rule(OutputTrigger Settings, Regex Regex) { public bool Failed { get; set; } }
    private Rule[] _rules = [];
    private readonly List<OutputTriggerMatch> _matches = [];
    private long _sequence;
    private long _feedStarted;
    public void BeginFeed() => _feedStarted = 0;
    private bool BudgetExpired()
    {
        if (_feedStarted == 0) return false;
        if (Stopwatch.GetElapsedTime(_feedStarted).TotalMilliseconds < 25) return false;
        Warning = "Some output rules were skipped during a high-output burst."; return true;
    }
    public bool Enabled => _rules.Any(r => !r.Failed);
    public string? Warning { get; private set; }
    public IReadOnlyList<OutputTriggerMatch> Matches => _matches.ToArray();

    public void Configure(IReadOnlyList<OutputTrigger> rules)
    {
        if (rules.Count > 32) throw new ArgumentException("Keep at most 32 output rules per profile.");
        foreach (var rule in rules) rule.Validate();
        if (_rules.Select(r => r.Settings).SequenceEqual(rules.Where(r => r.Enabled))) return;
        _rules = rules.Where(r => r.Enabled).Select(r => new Rule(r, r.Compile())).ToArray();
        Warning = null;
    }
    public void Clear() { _matches.Clear(); Warning = null; }

    public bool ProcessLine(TextBuffer buffer)
    {
        if (!Enabled) return false;
        if (BudgetExpired()) return true;
        var anchor = buffer.CreateImageAnchor(0, buffer.CursorY);
        if (anchor.LogicalOffset > 4096 || buffer.Columns > 4096) return false;
        var currentRow = buffer.HistoryCount + buffer.CursorY;
        (TextBufferLineSnapshot line, int index)[] lines;
        if (anchor.LogicalOffset == 0)
        {
            // GetRow is a view-relative API. Read the live row under the engine
            // gate without posting scroll events; restore the user's view even
            // if capture fails. Engine ScrollOffset/snapshots share that gate.
            var offset = buffer.ScrollOffset;
            try
            {
                buffer.ScrollOffset = 0;
                lines = [(new(buffer.GetRow(buffer.CursorY).ToArray(), false, []) { LogicalLineId = anchor.LogicalLineId }, currentRow)];
            }
            finally { buffer.ScrollOffset = offset; }
        }
        else
        {
            // Wrapped logical lines need the retained prefix, including history.
            lines = buffer.CreateSnapshot(includeHistory: true).Lines.Select((line, index) => (line, index))
                .Where(p => p.line.LogicalLineId == anchor.LogicalLineId && p.index <= currentRow).ToArray();
        }
        if (lines.Length == 0) return false;
        var changed = false;
        foreach (var group in lines.GroupBy(p => p.line.LogicalLineId))
        {
            // Do not reinterpret the surviving tail of a line truncated by history eviction.
            if (group.First().line.LogicalOffset != 0) continue;
            if (group.Sum(p => p.line.Cells.Sum(c => c.Text.Length)) > 4096) continue;
            var text = new StringBuilder();
            var positions = new List<(BufferPosition Start, BufferPosition End)>();
            foreach (var (line, row) in group)
            {
                for (var column = 0; column < line.Cells.Count && text.Length < 4096; column++)
                {
                    var cell = line.Cells[column];
                    if (cell.IsWideContinuation) continue;
                    var value = cell.Text;
                    if (text.Length + value.Length > 4096) break;
                    text.Append(value);
                    for (var i = 0; i < value.Length; i++) positions.Add((new(row, column), new(row, Math.Min(line.Cells.Count - 1, column + cell.DisplayWidth - 1))));
                }
            }
            var input = text.ToString().TrimEnd(' ');
            if (input.Length == 0) continue;
            foreach (var rule in _rules)
            {
                if (BudgetExpired()) return true;
                if (rule.Failed) continue;
                try
                {
                    var count = 0;
                    foreach (var match in rule.Regex.EnumerateMatches(input))
                    {
                        if (match.Length == 0) continue;
                        var start = positions[match.Index].Start;
                        var end = positions[match.Index + match.Length - 1].End;
                        if (rule.Settings.Action == "Highlight") buffer.ApplyColors(start, end, Color(rule.Settings.Foreground), Color(rule.Settings.Background));
                        if (count == 0)
                        {
                            var source = lines.First(p => p.index == start.Line).line;
                            _matches.Add(new(++_sequence, rule.Settings.Name, input[..Math.Min(240, input.Length)], rule.Settings.Action, new(source.LogicalLineId, source.LogicalOffset + start.Column)));
                            if (_matches.Count > 200) _matches.RemoveAt(0);
                        }
                        changed = true;
                        if (++count == 32) break;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    rule.Failed = true; Warning = $"Rule '{rule.Settings.Name}' was paused after its matching time limit."; changed = true;
                }
                finally { if (_feedStarted == 0) _feedStarted = Stopwatch.GetTimestamp(); }
            }
        }
        return changed;
    }
    private static TermColor Color(string text)
    {
        var rgb = uint.Parse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return TermColor.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
