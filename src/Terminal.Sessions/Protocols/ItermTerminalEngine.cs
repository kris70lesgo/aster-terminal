// iTerm2 metadata behavior adapted 2026-10-14 from VT100Terminal.m,
// commit 069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e.
// Copyright George Nachman and contributors. GPL-3.0-or-later.
using System.Text;
using Aster.Sessions.Triggers;
using Devolutions.Terminal.Core;

namespace Aster.Sessions.Protocols;

/// <summary>Uses the established VT parser and engine, adapting only iTerm2 OSC behavior.</summary>
public sealed class ItermTerminalEngine : ITerminalEngine, IVtDispatch
{
    private readonly TerminalEngine _inner;
    private readonly IVtDispatch _dispatch;
    private readonly VtParser _parser;
    private readonly object _gate = new();
    private long _revision;
    private double _cellWidth = 10, _cellHeight = 20;
    public (double Width, double Height) CellPixels { get { lock (_gate) return (_cellWidth, _cellHeight); } }
    public void ResizeKeepingCellMetrics(int columns, int rows) { lock (_gate) { _inner.Resize(columns, rows, _cellWidth, _cellHeight); _revision++; } }
    private readonly OutputTriggerProcessor _triggers = new();
    private int _triggerLinesThisFeed;
    private bool _triggerActivity;
    public event EventHandler? TriggersChanged;
    public IReadOnlyList<OutputTriggerMatch> TriggerMatches { get { lock (_gate) return _triggers.Matches; } }
    public string? TriggerWarning { get { lock (_gate) return _triggers.Warning; } }
    public void ConfigureTriggers(IReadOnlyList<OutputTrigger> rules) { lock (_gate) _triggers.Configure(rules); }
    public void ClearTriggerMatches() { lock (_gate) _triggers.Clear(); TriggersChanged?.Invoke(this, EventArgs.Empty); }
    public bool NavigateToTrigger(OutputTriggerMatch match)
    {
        lock (_gate)
        {
            if (_inner.AlternateBufferActive || !_inner.Buffer.TryResolveImageAnchor(match.Anchor, out var row, out _)) return false;
            _inner.SetScrollOffset(Math.Clamp(_inner.HistoryCount - row, 0, _inner.HistoryCount));
            _revision++; return true;
        }
    }
    public bool ProtocolResponsesEnabled { get; set; } = true;
    public bool GridOwnedExternally { get; set; }
    public long Revision => Interlocked.Read(ref _revision);
    public string? RemoteHost { get; private set; }
    public int? LastCommandExitCode { get; private set; }
    public event EventHandler? MetadataChanged;

    public ItermTerminalEngine(int columns = 80, int rows = 24, int historySize = 10000)
    {
        _inner = new TerminalEngine(columns, rows, historySize);
        _dispatch = _inner;
        _parser = new VtParser(this);
        _inner.ConfigureOptionalFeatures(false, false);
        _inner.ResponseReady += (_, data) => { if (ProtocolResponsesEnabled) ResponseReady?.Invoke(this, data); };
    }

    public void Feed(ReadOnlySpan<byte> data)
    {
        lock (_gate)
        {
            var originalBuffer = _inner.Buffer;
            TerminalImageAnchor? scrolledAnchor = null;
            if (_inner.ScrollOffset > 0 && !data.IsEmpty)
            {
                var firstLine = _inner.CreateSnapshot().Buffer.Lines[0];
                scrolledAnchor = new(firstLine.LogicalLineId, firstLine.LogicalOffset);
            }
            _triggerLinesThisFeed = 0; _triggerActivity = false; _triggers.BeginFeed();
            _parser.Process(data);
            if (scrolledAnchor is { } anchor && ReferenceEquals(originalBuffer, _inner.Buffer))
            {
                _inner.SetScrollOffset(_inner.Buffer.TryResolveImageAnchor(anchor, out var row, out _)
                    ? Math.Clamp(_inner.HistoryCount - row, 0, _inner.HistoryCount) : _inner.HistoryCount);
            }
            if (_triggerActivity) TriggersChanged?.Invoke(this, EventArgs.Empty);
            // Runs the engine's image pruning and synchronized-output invalidation policy.
            _inner.Feed(ReadOnlySpan<byte>.Empty);
            if (!data.IsEmpty) _revision++;
        }
    }
    public void Feed(string text) => Feed(Encoding.UTF8.GetBytes(text));
    public void Reset() { lock (_gate) { _parser.Reset(); _inner.Reset(); _triggers.Clear(); _triggerLinesThisFeed = 0; RemoteHost = null; LastCommandExitCode = null; _revision++; } }
    public void Dispose() => _inner.Dispose();

    public void OscDispatch(int command, ReadOnlySpan<char> data)
    {
        if (command == 133 && data.Length <= 4096 && (data.SequenceEqual("D") || data.StartsWith("D;")))
        {
            LastCommandExitCode = ItermSemanticPrompt.ExitCode(data.ToString());
            // The underlying engine stores unsigned status; negative/noninteger status stays unknown.
            var normalized = LastCommandExitCode is >= 0 ? $"D;{LastCommandExitCode}" : "D";
            _dispatch.OscDispatch(command, normalized);
            MetadataChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (command == 1337 && data.Length <= 4096)
        {
            var separator = data.IndexOf('=');
            if (separator > 0)
            {
                var key = data[..separator];
                var value = data[(separator + 1)..];
                if (key.SequenceEqual("CurrentDir"))
                {
                    // Display-only metadata: never change the shell or host working directory.
                    _dispatch.OscDispatch(7, value);
                    MetadataChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
                if (key.SequenceEqual("RemoteHost"))
                {
                    RemoteHost = value.ToString();
                    MetadataChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
        }
        _dispatch.OscDispatch(command, data);
    }

    public void Print(Rune rune) => _dispatch.Print(rune);
    public void ExecuteC0(byte control)
    {
        if (control is 0x0A or 0x0B or 0x0C && !_inner.AlternateBufferActive && _triggers.Enabled)
        {
            if (_triggerLinesThisFeed++ < 512) _triggerActivity |= _triggers.ProcessLine(_inner.Buffer);
        }
        _dispatch.ExecuteC0(control);
    }
    public void EscDispatch(char final, byte intermediate) => _dispatch.EscDispatch(final, intermediate);
    public void EscDispatch(char final, ReadOnlySpan<byte> intermediates) => _dispatch.EscDispatch(final, intermediates);
    public void Vt52Dispatch(char final, byte row = 0, byte column = 0) => _dispatch.Vt52Dispatch(final, row, column);
    public void CsiDispatch(char final, ReadOnlySpan<int> parameters, byte intermediate, bool privateMarker) => _dispatch.CsiDispatch(final, parameters, intermediate, privateMarker);
    public void CsiDispatch(char final, ReadOnlySpan<int> parameters, byte intermediate, byte privateMarker) => _dispatch.CsiDispatch(final, parameters, intermediate, privateMarker);
    public void CsiDispatch(char final, ReadOnlySpan<int> parameters, ReadOnlySpan<bool> subparameters, byte intermediate, byte privateMarker) => _dispatch.CsiDispatch(final, parameters, subparameters, intermediate, privateMarker);
    public void DcsDispatch(char final, ReadOnlySpan<int> parameters, ReadOnlySpan<byte> intermediates, byte privateMarker, ReadOnlySpan<byte> data) => _dispatch.DcsDispatch(final, parameters, intermediates, privateMarker, data);
    public void ApcDispatch(ReadOnlySpan<char> data) => _dispatch.ApcDispatch(data);

    public TerminalEngineCapabilities Capabilities => _inner.Capabilities;
    public TextBuffer Buffer => _inner.Buffer;
    public ColorScheme Scheme { get => _inner.Scheme; set { lock (_gate) { _inner.Scheme = value; _revision++; } } }
    public string Title => _inner.Title;
    public string? WorkingDirectory => _inner.WorkingDirectory;
    public bool AlternateBufferActive => _inner.AlternateBufferActive;
    public bool CursorVisible => _inner.CursorVisible;
    public bool CursorBlinking => _inner.CursorBlinking;
    public int CursorStyle => _inner.CursorStyle;
    public bool ApplicationCursorKeys => _inner.ApplicationCursorKeys;
    public bool BracketedPaste => _inner.BracketedPaste;
    public bool MouseTracking => _inner.MouseTracking;
    public TerminalMouseTrackingMode MouseTrackingMode => _inner.MouseTrackingMode;
    public bool SgrMouse => _inner.SgrMouse;
    public bool FocusTracking => _inner.FocusTracking;
    public bool AutoWrap => _inner.AutoWrap;
    public bool InsertMode => _inner.InsertMode;
    public bool ReverseVideo => _inner.ReverseVideo;
    public TerminalInputMode InputMode => _inner.InputMode;
    public int Columns => _inner.Columns;
    public int Rows => _inner.Rows;
    public int CursorX => _inner.CursorX;
    public int CursorY => _inner.CursorY;
    public int HistoryCount => _inner.HistoryCount;
    public int ScrollOffset { get { lock (_gate) return _inner.ScrollOffset; } }
    public void Resize(int columns, int rows, double cellWidth = 1, double cellHeight = 1)
    {
        lock (_gate) { _cellWidth = cellWidth; _cellHeight = cellHeight; _inner.Resize(GridOwnedExternally ? _inner.Columns : columns, GridOwnedExternally ? _inner.Rows : rows, cellWidth, cellHeight); _revision++; }
    }
    public void SetScrollOffset(int offset)
    {
        lock (_gate) { var previous = _inner.ScrollOffset; _inner.SetScrollOffset(offset); if (_inner.ScrollOffset != previous) _revision++; }
    }
    public void ConfigureOptionalFeatures(bool allowClipboardWrite, bool allowNotifications, bool allowKittyKeyboard = true) => _inner.ConfigureOptionalFeatures(allowClipboardWrite, allowNotifications, allowKittyKeyboard);
    public TerminalSnapshot CreateSnapshot(bool includeHistory = false) { lock (_gate) return _inner.CreateSnapshot(includeHistory); }
    public (long Revision, TerminalSnapshot Snapshot, ColorScheme Scheme) CaptureScreen()
    {
        lock (_gate)
        {
            var colors = _inner.Scheme;
            return (Revision, _inner.CreateSnapshot(), new ColorScheme
            {
                Name = colors.Name, Foreground = colors.Foreground, Background = colors.Background,
                Cursor = colors.Cursor, SelectionBackground = colors.SelectionBackground, Table = colors.Table.ToArray()
            });
        }
    }
    public string WrapPaste(string text) => _inner.WrapPaste(text);

    public event EventHandler? Invalidated { add => _inner.Invalidated += value; remove => _inner.Invalidated -= value; }
    public event EventHandler<string>? TitleChanged { add => _inner.TitleChanged += value; remove => _inner.TitleChanged -= value; }
    public event EventHandler<string?>? WorkingDirectoryChanged { add => _inner.WorkingDirectoryChanged += value; remove => _inner.WorkingDirectoryChanged -= value; }
    public event EventHandler? ShellIntegrationChanged { add => _inner.ShellIntegrationChanged += value; remove => _inner.ShellIntegrationChanged -= value; }
    public event EventHandler<string>? ClipboardWriteRequested { add => _inner.ClipboardWriteRequested += value; remove => _inner.ClipboardWriteRequested -= value; }
    public event EventHandler<TerminalNotification>? NotificationRequested { add => _inner.NotificationRequested += value; remove => _inner.NotificationRequested -= value; }
    public event EventHandler<TerminalEngineDiagnostic>? Diagnostic { add => _inner.Diagnostic += value; remove => _inner.Diagnostic -= value; }
    public event EventHandler? Bell { add => _inner.Bell += value; remove => _inner.Bell -= value; }
    public event EventHandler<byte[]>? ResponseReady;
}
