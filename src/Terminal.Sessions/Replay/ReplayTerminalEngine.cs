using Devolutions.Terminal.Core;

namespace Aster.Sessions.Replay;

/// <summary>Historical viewport: control layout and input cannot reflow or mutate its cells.</summary>
public sealed class ReplayTerminalEngine : ITerminalEngine
{
    private readonly TerminalEngine _inner = new(historySize: 0);
    public void ShowFrame(ScreenReplayFrame frame)
    {
        var snapshot = frame.Snapshot; var buffer = snapshot.Buffer;
        _inner.Reset(); _inner.Scheme = frame.Scheme;
        _inner.Resize(buffer.Columns, buffer.Rows);
        _inner.Feed(snapshot.CursorVisible ? "\u001b[?25h" : "\u001b[?25l");
        if (snapshot.ReverseVideo) _inner.Feed("\u001b[?5h");
        _inner.Buffer.ReplaceViewport(buffer.Lines.Select(line => new TextBufferProjectionRow(line.Cells.ToArray(), line.Wrapped, line.Marks) { Rendition = line.Rendition }).ToArray(), buffer.CursorX, buffer.CursorY);
    }
    public void Feed(ReadOnlySpan<byte> data) { }
    public void Feed(string text) { }
    public void Resize(int columns, int rows, double cellWidth = 1, double cellHeight = 1) { }
    public void Reset() { }
    public void SetScrollOffset(int offset) { }
    public void ConfigureOptionalFeatures(bool allowClipboardWrite, bool allowNotifications, bool allowKittyKeyboard = true) => _inner.ConfigureOptionalFeatures(false, false, false);
    public TerminalSnapshot CreateSnapshot(bool includeHistory = false) => _inner.CreateSnapshot();
    public string WrapPaste(string text) => "";
    public void Dispose() => _inner.Dispose();
    public TerminalEngineCapabilities Capabilities => _inner.Capabilities;
    public TextBuffer Buffer => _inner.Buffer;
    public ColorScheme Scheme { get => _inner.Scheme; set => _inner.Scheme = value; }
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
    public int ScrollOffset => _inner.ScrollOffset;
    public event EventHandler? Invalidated { add => _inner.Invalidated += value; remove => _inner.Invalidated -= value; }
    public event EventHandler<string>? TitleChanged { add => _inner.TitleChanged += value; remove => _inner.TitleChanged -= value; }
    public event EventHandler<string?>? WorkingDirectoryChanged { add => _inner.WorkingDirectoryChanged += value; remove => _inner.WorkingDirectoryChanged -= value; }
    public event EventHandler? ShellIntegrationChanged { add => _inner.ShellIntegrationChanged += value; remove => _inner.ShellIntegrationChanged -= value; }
    public event EventHandler<string>? ClipboardWriteRequested { add => _inner.ClipboardWriteRequested += value; remove => _inner.ClipboardWriteRequested -= value; }
    public event EventHandler<TerminalNotification>? NotificationRequested { add => _inner.NotificationRequested += value; remove => _inner.NotificationRequested -= value; }
    public event EventHandler<TerminalEngineDiagnostic>? Diagnostic { add => _inner.Diagnostic += value; remove => _inner.Diagnostic -= value; }
    public event EventHandler? Bell { add => _inner.Bell += value; remove => _inner.Bell -= value; }
    public event EventHandler<byte[]>? ResponseReady { add => _inner.ResponseReady += value; remove => _inner.ResponseReady -= value; }
}
