using System.Text;
using Devolutions.Terminal.Core;
using Aster.Sessions.Protocols;

namespace Aster.Sessions.Hosting;

internal sealed class HostTranscript
{
    public const int Capacity = 8 * 1024 * 1024;
    private readonly List<HostFrame> _frames = [];
    private int _bytes;
    public bool Checkpointed { get; private set; }
    public IReadOnlyList<HostFrame> Snapshot() => _frames.ToArray();
    public void Append(HostFrame frame, ItermTerminalEngine engine)
    {
        if (_bytes + frame.Data.Length + 5 > Capacity || _frames.Count >= 4096)
        {
            // The actual shell and host engine remain alive. Compact the display
            // journal to the current real text viewport, never fabricated output.
            _frames.Clear(); _bytes = 0; Checkpointed = true;
            var view = engine.CreateSnapshot();
            Add(new(HostProtocol.Grid, HostProtocol.GridData(engine.Columns, engine.Rows, (int)(engine.CellPixels.Width * engine.Columns), (int)(engine.CellPixels.Height * engine.Rows))));
            var text = new StringBuilder("\u001bc\u001b[?7l\u001b[2J");
            if (view.AlternateBufferActive) text.Append("\u001b[?1049h");
            for (var y = 0; y < view.Buffer.Lines.Count; y++)
            {
                text.Append($"\u001b[{y + 1};1H");
                CellAttributes? previous = null;
                foreach (var cell in view.Buffer.Lines[y].Cells)
                {
                    if (cell.IsWideContinuation) continue;
                    if (previous is null || !previous.Value.Equals(cell.Attributes))
                    {
                        text.Append("\u001b[0m");
                        var foreground = cell.Attributes.Foreground.ToArgb(engine.Scheme, true); var background = cell.Attributes.Background.ToArgb(engine.Scheme, false);
                        text.Append($"\u001b[38;2;{(foreground >> 16) & 255};{(foreground >> 8) & 255};{foreground & 255}m\u001b[48;2;{(background >> 16) & 255};{(background >> 8) & 255};{background & 255}m");
                        if (cell.Attributes.Flags.HasFlag(CellFlags.Bold)) text.Append("\u001b[1m");
                        if (cell.Attributes.Flags.HasFlag(CellFlags.Italic)) text.Append("\u001b[3m");
                        if (cell.Attributes.Flags.HasFlag(CellFlags.Underline)) text.Append("\u001b[4m");
                        previous = cell.Attributes;
                    }
                    text.Append(cell.Text);
                }
            }
            text.Append($"\u001b[0m\u001b[{engine.CursorY + 1};{engine.CursorX + 1}H");
            text.Append(engine.CursorVisible ? "\u001b[?25h" : "\u001b[?25l");
            text.Append(engine.AutoWrap ? "\u001b[?7h" : "\u001b[?7l");
            text.Append(engine.ApplicationCursorKeys ? "\u001b[?1h" : "\u001b[?1l");
            text.Append(engine.BracketedPaste ? "\u001b[?2004h" : "\u001b[?2004l");
            text.Append(engine.InsertMode ? "\u001b[4h" : "\u001b[4l");
            var bytes = Encoding.UTF8.GetBytes(text.ToString());
            foreach (var chunk in bytes.Chunk(32768)) Add(new(HostProtocol.Output, chunk));
            // This checkpoint already includes the frame that overflowed.
            return;
        }
        Add(frame);
    }
    private void Add(HostFrame frame) { _frames.Add(frame); _bytes += frame.Data.Length + 5; }
}
