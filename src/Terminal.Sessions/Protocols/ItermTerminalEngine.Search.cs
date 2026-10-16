using Aster.Sessions.Search;
using Devolutions.Terminal.Core;

namespace Aster.Sessions.Protocols;

public sealed partial class ItermTerminalEngine
{
    private TextBuffer? _searchBuffer;
    private long _searchEpoch;
    private bool _searchDisposed;
    private long SearchEpoch()
    {
        if (!ReferenceEquals(_searchBuffer, _inner.Buffer)) { _searchBuffer = _inner.Buffer; _searchEpoch++; }
        return _searchEpoch;
    }
    public FindSnapshot CaptureFindSnapshot(CancellationToken token = default, int maximumCells = ScrollbackSearch.MaximumCells)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_searchDisposed, this); token.ThrowIfCancellationRequested();
            var buffer = _inner.Buffer; var offset = buffer.ScrollOffset;
            var total = buffer.TotalLines; var count = Math.Min(total, Math.Min(ScrollbackSearch.MaximumRows, Math.Max(1, maximumCells / buffer.Columns)));
            var first = total - count; var lines = new List<TextBufferLineSnapshot>(count);
            try
            {
                for (var row = first; row < total;)
                {
                    token.ThrowIfCancellationRequested(); buffer.ScrollOffset = Math.Max(0, buffer.HistoryCount - row);
                    var viewportTop = buffer.HistoryCount - buffer.ScrollOffset;
                    var chunk = buffer.CreateSnapshot(); var start = row - viewportTop;
                    var copied = Math.Min(total - row, chunk.Lines.Count - start);
                    lines.AddRange(chunk.Lines.Skip(start).Take(copied)); row += copied;
                }
                return new(new(buffer.Columns, buffer.Rows, buffer.CursorX, buffer.CursorY, buffer.HistoryCount, offset, lines), SearchEpoch(), Revision, first > 0);
            }
            finally { buffer.ScrollOffset = offset; }
        }
    }
    public bool RevealFindHit(FindResult result, FindHit hit)
    {
        lock (_gate)
        {
            if (_searchDisposed || result.Epoch != SearchEpoch() || !_inner.Buffer.TryResolveImageAnchor(hit.Start, out var row, out _)) return false;
            _inner.SetScrollOffset(Math.Clamp(_inner.HistoryCount - Math.Max(0, row - _inner.Rows / 2), 0, _inner.HistoryCount)); _revision++; return true;
        }
    }
    public IReadOnlyList<FindVisibleRange> ProjectFindHits(FindResult result, FindHit? current)
    {
        lock (_gate)
        {
            if (_searchDisposed || result.Epoch != SearchEpoch()) return [];
            var snapshot = _inner.CreateSnapshot().Buffer; var ranges = new List<FindVisibleRange>();
            for (var row = 0; row < snapshot.Lines.Count; row++)
            {
                var line = snapshot.Lines[row]; if (!result.Groups.TryGetValue(line.LogicalLineId, out var hits)) continue;
                var low = 0; var high = hits.Length;
                while (low < high) { var middle = low + (high - low) / 2; if (hits[middle].End.LogicalOffset < line.LogicalOffset) low = middle + 1; else high = middle; }
                for (var index = low; index < hits.Length && hits[index].Start.LogicalOffset < line.LogicalOffset + snapshot.Columns; index++)
                {
                    var hit = hits[index];
                    var start = Math.Max(0, hit.Start.LogicalOffset - line.LogicalOffset);
                    var end = Math.Min(snapshot.Columns - 1, hit.End.LogicalOffset - line.LogicalOffset);
                    if (start <= end) ranges.Add(new(row, start, end, hit == current));
                }
            }
            return ranges;
        }
    }
}
