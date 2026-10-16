using Avalonia.Threading;
using Aster.Sessions.Protocols;
using Aster.Sessions.Search;
using Devolutions.Terminal;
using Devolutions.Terminal.Render;

namespace Aster.App.Services;

/// <summary>Cancelable background Find; only viewport overlays/navigation run on the dispatcher.</summary>
public sealed class PaneFindController : IDisposable
{
    private readonly ItermTerminalEngine _engine;
    private readonly TermControl _terminal;
    private CancellationTokenSource? _request;
    private FindResult? _result;
    private int _generation, _paintQueued;
    private volatile bool _active, _disposed;
    private long _observedRevision;
    public int CurrentIndex { get; private set; } = -1;
    public IReadOnlyList<FindHit> Matches => _result?.Hits ?? [];
    public FindHit? Current => CurrentIndex >= 0 && CurrentIndex < Matches.Count ? Matches[CurrentIndex] : null;
    public string Status { get; private set; } = "";
    public bool Busy { get; private set; }
    public event EventHandler? Changed;
    public PaneFindController(ItermTerminalEngine engine, TermControl terminal)
    { _engine = engine; _terminal = terminal; _engine.Invalidated += Invalidated; }
    public void Activate()
    {
        Dispatcher.UIThread.VerifyAccess(); if (_disposed) return; _active = true;
        if (_terminal.Search.Query.Length > 0) _terminal.Search.Clear();
    }
    public void Deactivate() { Clear(); _active = false; }
    public void Clear()
    {
        Dispatcher.UIThread.VerifyAccess(); _request?.Cancel(); _generation++;
        _result = null; CurrentIndex = -1; Busy = false; Status = "";
        _terminal.SetSearchHighlights([]); Changed?.Invoke(this, EventArgs.Empty);
    }
    public async Task SearchAsync(FindOptions options, bool previous = false, bool navigate = true, bool debounce = false)
    {
        Dispatcher.UIThread.VerifyAccess(); if (_disposed || !_active) return;
        if (options.Query.Length == 0) { Clear(); return; }
        var prior = Current; var priorIndex = CurrentIndex; var same = _result?.Options == options;
        _request?.Cancel(); var request = new CancellationTokenSource(); _request = request; var generation = ++_generation;
        Busy = true; Status = "Searching…"; _terminal.SetSearchHighlights([]); Changed?.Invoke(this, EventArgs.Empty);
        try
        {
            if (debounce) await Task.Delay(150, request.Token);
            var result = same && _result is { } cached && _observedRevision == _engine.Revision ? cached :
                await Task.Run(() => ScrollbackSearch.Find(_engine.CaptureFindSnapshot(request.Token), options, request.Token), request.Token);
            if (request.IsCancellationRequested || generation != _generation || !_active || _disposed) return;
            var expectedRevision = ReferenceEquals(result, _result) ? _observedRevision : result.Revision;
            _result = result;
            if (result.Hits.Count == 0) CurrentIndex = -1;
            else if (!same || prior is null) CurrentIndex = previous && navigate ? result.Hits.Count - 1 : 0;
            else
            {
                var index = -1;
                for (var i = 0; i < result.Hits.Count; i++) if (result.Hits[i] == prior) { index = i; break; }
                CurrentIndex = index < 0 ? Math.Clamp(priorIndex, 0, result.Hits.Count - 1) : navigate ?
                    (index + (previous ? -1 : 1) + result.Hits.Count) % result.Hits.Count : index;
            }
            _observedRevision = expectedRevision;
            if (Current is { } hit && !_engine.RevealFindHit(result, hit, expectedRevision, out _observedRevision))
            { _result = null; CurrentIndex = -1; Status = "Output changed; search again."; }
            Busy = false;
            Render(); UpdateStatus(); Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (ObjectDisposedException) { }
        finally
        {
            if (_request == request) { _request = null; Busy = false; }
            request.Dispose();
        }
    }
    private void Invalidated(object? sender, EventArgs args)
    {
        if (!_active || _disposed) return;
        if (Interlocked.Exchange(ref _paintQueued, 1) != 0) return;
        Dispatcher.UIThread.Post(() =>
        {
            Interlocked.Exchange(ref _paintQueued, 0);
            if (!_active || _disposed || Busy || _result is null) return;
            Render(); UpdateStatus(); Changed?.Invoke(this, EventArgs.Empty);
        }, DispatcherPriority.Background);
    }
    private void UpdateStatus()
    {
        if (_result is null) return;
        Status = _result.Error ?? (Matches.Count == 0 ? "No matches" : $"{CurrentIndex + 1} / {Matches.Count}");
        if (_result.Warning is { } warning) Status += " · " + warning;
        if (_observedRevision != _engine.Revision) Status += " · Output changed; Enter refreshes";
    }
    private void Render() => _terminal.SetSearchHighlights(_result is null ? [] : _engine.ProjectFindHits(_result, Current)
        .Select(r => new TerminalCellRange(r.Row, r.StartColumn, r.EndColumn, r.Current ? 0x90FFC44Du : 0x604080FFu)).ToArray());
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; Deactivate(); _engine.Invalidated -= Invalidated;
    }
}
