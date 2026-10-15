using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aster.Sessions.Persistence;

/// <summary>Atomic settings replacement with a backup; corrupt files are never silently overwritten.</summary>
public sealed partial class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, MaxDepth = 32 };
    public string Path { get; }
    public AppSettings Settings { get; private set; }
    public string? LoadWarning { get; private set; }

    public SettingsStore(string? path = null)
    {
        Path = path ?? DefaultPath();
        Settings = Load();
    }

    public static string DefaultPath()
    {
        string directory;
        if (OperatingSystem.IsMacOS())
            directory = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Aster Terminal");
        else if (OperatingSystem.IsWindows())
            directory = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Aster Terminal");
        else
            directory = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".config"), "aster-terminal");
        return System.IO.Path.Combine(directory, "settings.json");
    }

    private AppSettings Load()
    {
        if (!File.Exists(Path)) return AppSettings.Default();
        foreach (var candidate in new[] { Path, Path + ".bak" })
        {
            try
            {
                if (!File.Exists(candidate)) continue;
                if (new FileInfo(candidate).Length > 2 * 1024 * 1024) throw new InvalidDataException("Settings file exceeds 2 MiB.");
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(candidate), Json) ?? throw new InvalidDataException("Empty settings file.");
                Validate(settings);
                if (candidate != Path) LoadWarning = "Settings were damaged. Recovered the previous backup; the damaged file is preserved until you save.";
                return settings;
            }
            catch (Exception error) when (error is JsonException or IOException or ArgumentException)
            {
                LoadWarning = $"Could not load settings: {error.Message} Using defaults; the original file is preserved until you save.";
            }
        }
        return AppSettings.Default();
    }

    public void Save(AppSettings settings)
    {
        Validate(settings);
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Json));
            if (File.Exists(Path))
            {
                // Invalid originals get a separate recovery copy, not a misleading last-good backup.
                File.Copy(Path, LoadWarning is null ? Path + ".bak" : Path + ".damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), true);
            }
            File.Move(temporary, Path, true);
            Settings = settings;
            LoadWarning = null;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void Validate(AppSettings settings)
    {
        if (settings.ScriptingPort is < 1024 or > 65535) throw new ArgumentException("Scripting port must be 1024–65535.");
        if (settings.Version != 1) throw new ArgumentException("Unsupported settings version.");
        if (settings.Profiles is null || settings.Profiles.Count is < 1 or > 100) throw new ArgumentException("Keep between 1 and 100 profiles.");
        if (settings.Profiles.Any(p => p is null)) throw new ArgumentException("A profile is damaged.");
        if (settings.Profiles.Select(p => p.Id).Distinct().Count() != settings.Profiles.Count) throw new ArgumentException("Profile IDs must be unique.");
        if (!settings.Profiles.Any(p => p.Id == settings.DefaultProfileId)) throw new ArgumentException("Choose an existing default profile.");
        if (settings.Theme is not ("Dark" or "Light" or "System")) throw new ArgumentException("Choose Dark, Light or System theme.");
        if (!double.IsFinite(settings.WindowWidth) || !double.IsFinite(settings.WindowHeight)) throw new ArgumentException("Invalid window dimensions.");
        foreach (var profile in settings.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 80) throw new ArgumentException("Profile names must contain 1–80 characters.");
            if (string.IsNullOrWhiteSpace(profile.Command) || profile.Command.Length > 4096 || profile.Command.Contains('\0')) throw new ArgumentException("A valid shell command is required.");
            if (string.IsNullOrWhiteSpace(profile.Directory) || profile.Directory.Contains('\0')) throw new ArgumentException("A working directory is required.");
            if (string.IsNullOrWhiteSpace(profile.FontFamily) || !double.IsFinite(profile.FontSize) || profile.FontSize is < 6 or > 48) throw new ArgumentException("Font size must be 6–48.");
            if (profile.HistorySize is < 100 or > 100000) throw new ArgumentException("Scrollback must be 100–100,000 lines.");
            if (profile.CursorShape is not ("filledBox" or "bar" or "underscore")) throw new ArgumentException("Invalid cursor style.");
            if (profile.Triggers is null || profile.Triggers.Count > 32 || profile.Triggers.Any(t => t is null)) throw new ArgumentException("Keep at most 32 output rules per profile.");
            foreach (var trigger in profile.Triggers) trigger.Validate();
            foreach (var color in new[] { profile.Foreground, profile.Background })
                if (color is not null && !HexColor().IsMatch(color)) throw new ArgumentException("Colours must use #RRGGBB.");
            if (profile.EnvironmentVariables is null || profile.EnvironmentVariables.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Key.Contains('=') || p.Key.Contains('\0') || p.Value is null || p.Value.Contains('\0')))
                throw new ArgumentException("Invalid environment variable.");
        }
        if (settings.Layout is null || settings.Layout.Count > 50 || settings.Shortcuts is null) throw new ArgumentException("Invalid layout or shortcuts.");
        foreach (var tab in settings.Layout) ValidateLayout(tab.Root, 0);
    }

    private static void ValidateLayout(PaneLayout node, int depth)
    {
        if (node is null || depth > 12 || node.Children is null) throw new ArgumentException("Layout is too deep or damaged.");
        if (node.Children.Count == 0) { if (string.IsNullOrWhiteSpace(node.ProfileId)) throw new ArgumentException("Pane profile is missing."); return; }
        if (node.Children.Count != 2 || node.Direction is not ("Columns" or "Rows") || !double.IsFinite(node.Ratio) || node.Ratio is < .05 or > .95) throw new ArgumentException("Invalid split layout.");
        foreach (var child in node.Children) ValidateLayout(child, depth + 1);
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
