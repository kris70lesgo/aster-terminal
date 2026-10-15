namespace Aster.Sessions.Tmux;

public sealed record TmuxLayout(int Columns, int Rows, int X, int Y, int? PaneId, bool Vertical, IReadOnlyList<TmuxLayout> Children)
{
    public IEnumerable<TmuxLayout> Panes => PaneId is not null ? [this] : Children.SelectMany(c => c.Panes);
    public static TmuxLayout Parse(string text)
    {
        if (text.Length > 16384 || text.Length < 6 || text[4] != ',' || !text.AsSpan(0, 4).ToString().All(Uri.IsHexDigit)) throw new InvalidDataException("Invalid tmux layout header.");
        var position = 5; var count = 0;
        int Number(char separator)
        {
            var start = position; while (position < text.Length && char.IsAsciiDigit(text[position])) position++;
            if (start == position || !int.TryParse(text.AsSpan(start, position - start), out var value) || value > 1000000) throw new InvalidDataException("Invalid tmux layout number.");
            if (separator != '\0' && (position >= text.Length || text[position++] != separator)) throw new InvalidDataException("Invalid tmux layout separator.");
            return value;
        }
        TmuxLayout Node(int depth)
        {
            if (depth > 16 || ++count > 127) throw new InvalidDataException("Tmux layout is too complex.");
            var columns = Number('x'); var rows = Number(','); var x = Number(','); var y = Number('\0');
            if (columns is < 1 or > 1000 || rows is < 1 or > 500 || columns * rows > 100000) throw new InvalidDataException("Unsupported tmux dimensions.");
            if (position >= text.Length) throw new InvalidDataException("Incomplete tmux layout.");
            var next = text[position++];
            if (next == ',') return new(columns, rows, x, y, Number('\0'), false, []);
            if (next is not ('{' or '[')) throw new InvalidDataException("Invalid tmux layout split.");
            var children = new List<TmuxLayout>(); var end = next == '{' ? '}' : ']';
            while (true)
            {
                children.Add(Node(depth + 1));
                if (position >= text.Length) throw new InvalidDataException("Unclosed tmux layout.");
                var delimiter = text[position++]; if (delimiter == end) break;
                if (delimiter != ',') throw new InvalidDataException("Invalid tmux layout children.");
            }
            if (children.Count < 2) throw new InvalidDataException("Empty tmux split.");
            var offset = next == '[' ? y : x;
            foreach (var child in children)
            {
                if (next == '[' ? child.X != x || child.Columns != columns || child.Y != offset : child.Y != y || child.Rows != rows || child.X != offset)
                    throw new InvalidDataException("Inconsistent tmux child geometry.");
                offset += (next == '[' ? child.Rows : child.Columns) + 1;
            }
            if (offset - 1 != (next == '[' ? y + rows : x + columns)) throw new InvalidDataException("Tmux children do not fill their parent.");
            return new(columns, rows, x, y, null, next == '[', children);
        }
        var root = Node(0);
        if (root.Panes.Count() > 64 || position != text.Length || root.Panes.Select(p => p.PaneId).Distinct().Count() != root.Panes.Count()) throw new InvalidDataException("Invalid or duplicate tmux panes.");
        return root;
    }
}
