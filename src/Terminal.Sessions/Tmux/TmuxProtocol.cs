using System.Text;

namespace Aster.Sessions.Tmux;

internal static class TmuxProtocol
{
    public static byte[] DecodeOutput(string value)
    {
        using var bytes = new MemoryStream();
        for (var i = 0; i < value.Length;)
        {
            if (value[i] == '\\')
            {
                if (i + 3 >= value.Length || value[i + 1] is < '0' or > '3' || value[i + 2] is < '0' or > '7' || value[i + 3] is < '0' or > '7') throw new InvalidDataException("Invalid tmux output escape.");
                bytes.WriteByte((byte)((value[i + 1] - '0') * 64 + (value[i + 2] - '0') * 8 + value[i + 3] - '0')); i += 4;
            }
            else
            {
                var start = i; while (i < value.Length && value[i] != '\\') i++;
                bytes.Write(Encoding.Latin1.GetBytes(value.AsSpan(start, i - start).ToString()));
            }
        }
        return bytes.ToArray();
    }
    public static int Id(string value, char prefix)
    {
        if (value.Length < 2 || value[0] != prefix || !value.AsSpan(1).ToString().All(char.IsAsciiDigit) || !int.TryParse(value.AsSpan(1), out var id)) throw new InvalidDataException("Invalid tmux identifier.");
        return id;
    }
    public static string Quote(string value)
    {
        if (value.Length > 256 || value.Any(char.IsControl)) throw new ArgumentException("Invalid tmux session name.");
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`") + "\"";
    }
}
