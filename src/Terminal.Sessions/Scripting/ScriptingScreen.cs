using System.Text;
using Aster.Sessions.Protocols;
using Devolutions.Terminal.Core;
using API = Aster.Sessions.Scripting.Protocol;

namespace Aster.Sessions.Scripting;

public static class ScriptingScreen
{
    // This subset deliberately exposes live viewport coordinates, not invented
    // absolute scrollback offsets after eviction/reflow.
    public static API.GetBufferResponse Read(ItermTerminalEngine engine, API.GetBufferRequest request)
    {
        if (request.LineRange is not { ScreenContentsOnly: true } range || range.HasTrailingLines || range.WindowedCoordRange is not null)
            return new() { Status = API.GetBufferResponse.Types.Status.InvalidLineRange };
        var (snapshot, scheme) = engine.CaptureLiveScreen(); var buffer = snapshot.Buffer;
        var response = new API.GetBufferResponse { Cursor = new() { X = buffer.CursorX, Y = buffer.CursorY },
            WindowedCoordRange = new() { CoordRange = new() { Start = new() { X = 0, Y = 0 }, End = new() { X = 0, Y = buffer.Rows } }, Columns = new() { Location = 0, Length = buffer.Columns } } };
        foreach (var row in buffer.Lines)
        {
            var line = new API.LineContents { Continuation = row.Wrapped ? API.LineContents.Types.Continuation.SoftEol : API.LineContents.Types.Continuation.HardEol };
            var text = new StringBuilder();
            foreach (var cell in row.Cells)
            {
                var glyph = cell.Text; var count = glyph.EnumerateRunes().Count(); text.Append(glyph);
                if (line.CodePointsPerCell.LastOrDefault() is { } prior && prior.NumCodePoints == count) prior.Repeats++;
                else line.CodePointsPerCell.Add(new API.CodePointsPerCell { NumCodePoints = count, Repeats = 1 });
                if (!request.IncludeStyles) continue;
                var attributes = cell.Attributes; var flags = attributes.Flags;
                static API.RGBColor Rgb(uint argb) => new() { Red = (argb >> 16) & 255, Green = (argb >> 8) & 255, Blue = argb & 255 };
                var style = new API.CellStyle { Repeats = 1, FgRgb = Rgb(attributes.Foreground.ToArgb(scheme, true)), BgRgb = Rgb(attributes.Background.ToArgb(scheme, false)),
                    Bold = flags.HasFlag(CellFlags.Bold), Faint = flags.HasFlag(CellFlags.Faint), Italic = flags.HasFlag(CellFlags.Italic), Blink = flags.HasFlag(CellFlags.Blink),
                    Underline = (flags & (CellFlags.Underline | CellFlags.DoubleUnderline | CellFlags.CurlyUnderline)) != 0, Strikethrough = flags.HasFlag(CellFlags.Strikethrough),
                    Invisible = flags.HasFlag(CellFlags.Invisible), Inverse = flags.HasFlag(CellFlags.Inverse), Guarded = cell.IsProtected };
                if (line.Style.LastOrDefault() is { } previous)
                { style.Repeats = previous.Repeats; if (style.Equals(previous)) { previous.Repeats++; continue; } style.Repeats = 1; }
                line.Style.Add(style);
            }
            line.Text = text.ToString(); response.Contents.Add(line);
        }
        return response;
    }
}
