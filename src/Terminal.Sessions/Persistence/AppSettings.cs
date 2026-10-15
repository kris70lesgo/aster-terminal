namespace Aster.Sessions.Persistence;

public sealed record PaneLayout
{
    public string? ProfileId { get; init; }
    public string? Title { get; init; }
    public string? Direction { get; init; }
    public double Ratio { get; init; } = .5;
    public List<PaneLayout> Children { get; init; } = [];
}

public sealed record TabLayout(string? Title, PaneLayout Root);

public sealed record AppSettings
{
    public int Version { get; init; } = 1;
    public List<TerminalProfile> Profiles { get; init; } = [];
    public string DefaultProfileId { get; init; } = "";
    public string Theme { get; init; } = "Dark";
    public bool ScriptingEnabled { get; init; }
    public int ScriptingPort { get; init; } = 1912;
    public bool HotkeyWindowEnabled { get; init; }
    public bool KeepSessionsAlive { get; init; } = true;
    public bool RestoreLayout { get; init; } = true;
    public bool CopyOnSelect { get; init; }
    public bool ShowStatusBar { get; init; }
    public bool AlwaysShowTabBar { get; init; }
    public bool ScreenReplayEnabled { get; init; } = true;
    public bool ConfirmClose { get; init; } = true;
    public double WindowWidth { get; init; } = 1080;
    public double WindowHeight { get; init; } = 680;
    public List<TabLayout> Layout { get; init; } = [];
    public Dictionary<string, string> Shortcuts { get; init; } = new();

    public static AppSettings Default()
    {
        var profiles = TerminalProfile.InstalledDefaults().ToList();
        return new() { Profiles = profiles, DefaultProfileId = profiles[0].Id };
    }
}
