using System.Text.RegularExpressions;

namespace Aster.Sessions.Triggers;

public sealed record OutputTrigger
{
    public string Name { get; init; } = "New rule";
    public string Pattern { get; init; } = "ERROR";
    public bool Enabled { get; init; } = true;
    public string Action { get; init; } = "Highlight";
    public string Foreground { get; init; } = "#FFFFFF";
    public string Background { get; init; } = "#9B3038";

    public Regex Compile() => new(Pattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80) throw new ArgumentException("Rule names must contain 1–80 characters.");
        if (string.IsNullOrWhiteSpace(Pattern) || Pattern.Length > 512) throw new ArgumentException("Rule patterns must contain 1–512 characters.");
        if (Action is not ("Highlight" or "Bookmark")) throw new ArgumentException("Choose Highlight or Bookmark.");
        foreach (var color in new[] { Foreground, Background })
            if (color is null || color.Length != 7 || color[0] != '#' || color.Skip(1).Any(c => !Uri.IsHexDigit(c)))
                throw new ArgumentException("Rule colours must use #RRGGBB.");
        try { _ = Compile(); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException)
        { throw new ArgumentException($"Invalid rule '{Name}': use a regular expression without lookarounds or backreferences. {error.Message}"); }
    }
}
