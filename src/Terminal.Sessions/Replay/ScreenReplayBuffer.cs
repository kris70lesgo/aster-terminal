using Devolutions.Terminal.Core;

namespace Aster.Sessions.Replay;

public sealed record ScreenReplayFrame(long Sequence, DateTimeOffset Timestamp, TerminalSnapshot Snapshot, ColorScheme Scheme, long EstimatedBytes);

/// <summary>
/// Bounded timestamped screen keyframes, following iTerm2 DVR's capacity/oldest-eviction
/// model (DVR.m/DVRBuffer.m at 069b6e0...). Rewritten for portable cell snapshots;
/// not compatible with iTerm2's binary delta format. Copyright George Nachman and
/// contributors for the studied design. Aster implementation GPL-3.0-or-later.
/// </summary>
public sealed class ScreenReplayBuffer
{
    private readonly object _gate = new();
    private readonly LinkedList<ScreenReplayFrame> _frames = [];
    private long _sequence;
    private long _bytes;
    public long CapacityBytes { get; }
    public int MaximumFrames { get; }
    public long RetainedBytes { get { lock (_gate) return _bytes; } }
    public ScreenReplayBuffer(long capacityBytes = 16 * 1024 * 1024, int maximumFrames = 256)
    {
        if (capacityBytes < 1024 || maximumFrames < 1) throw new ArgumentOutOfRangeException(nameof(capacityBytes));
        CapacityBytes = capacityBytes; MaximumFrames = maximumFrames;
    }
    public bool Append(TerminalSnapshot snapshot, ColorScheme scheme, DateTimeOffset timestamp)
    {
        // Drop graphics payloads: replay currently promises text, attributes and cursor only.
        var bytes = 512L + 2L * (snapshot.Title.Length + (snapshot.WorkingDirectory?.Length ?? 0)) + snapshot.Buffer.Lines.Sum(line => 128L + line.Cells.Sum(cell => 64L +
            2L * ((cell.CombiningCharacters?.Length ?? 0) + (cell.HyperlinkUri?.Length ?? 0))) + line.Marks.Count * 32L);
        if (bytes > CapacityBytes / 2) return false; // upstream's oversized-frame guard
        var lines = snapshot.Buffer.Lines.Select(line => line with
        {
            Cells = Array.AsReadOnly(line.Cells.ToArray()),
            Marks = Array.AsReadOnly(line.Marks.Select(mark => new ShellMark(mark.StartColumn, mark.ExitCode)).ToArray())
        }).ToArray();
        var copy = snapshot with { Buffer = snapshot.Buffer with { Lines = Array.AsReadOnly(lines) }, CursorVisible = snapshot.CursorVisible && snapshot.Buffer.ScrollOffset == 0, Images = [], DrcsGlyphs = new Dictionary<int, DrcsGlyph>() };
        var colors = new ColorScheme { Name = scheme.Name, Foreground = scheme.Foreground, Background = scheme.Background, Cursor = scheme.Cursor, SelectionBackground = scheme.SelectionBackground, Table = scheme.Table.ToArray() };
        lock (_gate)
        {
            while (_frames.Count > 0 && (_bytes + bytes > CapacityBytes || _frames.Count >= MaximumFrames))
            {
                _bytes -= _frames.First!.Value.EstimatedBytes; _frames.RemoveFirst();
            }
            if (_frames.Last is { } last && timestamp < last.Value.Timestamp) timestamp = last.Value.Timestamp;
            _frames.AddLast(new ScreenReplayFrame(++_sequence, timestamp, copy, colors, bytes)); _bytes += bytes;
        }
        return true;
    }
    public IReadOnlyList<ScreenReplayFrame> Snapshot() { lock (_gate) return _frames.ToArray(); }
    public void Clear() { lock (_gate) { _frames.Clear(); _bytes = 0; } }
}
