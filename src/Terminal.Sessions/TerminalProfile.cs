using Devolutions.Terminal.Settings;

namespace Aster.Sessions;

public sealed record TerminalProfile(string Name, string Command, string Directory, double FontSize = 14)
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string FontFamily { get; init; } = "Cascadia Mono";
    public int HistorySize { get; init; } = 10000;
    public string CursorShape { get; init; } = "filledBox";
    public string? Foreground { get; init; }
    public string? Background { get; init; }
    public bool ShellIntegration { get; init; }
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
    public static TerminalProfile Default()
    {
        var shell = OperatingSystem.IsWindows() ? "powershell.exe" : Environment.GetEnvironmentVariable("SHELL");
        if (string.IsNullOrWhiteSpace(shell) || (!OperatingSystem.IsWindows() && !File.Exists(shell)))
            shell = OperatingSystem.IsMacOS() ? "/bin/zsh" : "/bin/bash";
        var command = OperatingSystem.IsWindows() ? shell : $"'{shell.Replace("'", "'\\''")}' -l";
        return new(Path.GetFileNameWithoutExtension(shell), command,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    public ProfileSettings ToControlProfile(bool light = false) => new()
    {
        Name = Name,
        Commandline = Command,
        StartingDirectory = Directory,
        FontSize = FontSize,
        FontFace = FontFamily,
        Foreground = Foreground ?? (light ? "#243044" : "#D8DEE9"),
        Background = Background ?? (light ? "#FAFAFC" : "#171A20"),
        CursorColor = light ? "#243044" : "#D8DEE9",
        SelectionBackground = light ? "#B9D3EF" : "#3B506B",
        Padding = "10",
        CursorShape = CursorShape,
        HistorySize = HistorySize,
        CloseOnExit = CloseOnExitMode.Never,
        AllowVtClipboardWrite = false,
        AllowOscNotifications = false,
        ReloadEnvironmentVariables = false,
        Environment = BuildEnvironment()
    };

    private Dictionary<string, string?> BuildEnvironment()
    {
        var result = EnvironmentVariables.ToDictionary(pair => pair.Key, pair => (string?)pair.Value);
        result["TERM"] = "xterm-256color";
        result["COLORTERM"] = "truecolor";
        result["TERM_PROGRAM"] = "AsterTerminal";
        return result;
    }

    public static IReadOnlyList<TerminalProfile> InstalledDefaults()
    {
        var primary = Default();
        var profiles = new List<TerminalProfile> { primary };
        if (OperatingSystem.IsWindows())
            profiles.Add(new("Command Prompt", "cmd.exe", primary.Directory));
        else
            foreach (var shell in new[] { "/bin/bash", "/bin/zsh", "/bin/sh", "/usr/bin/fish" })
                if (File.Exists(shell) && !primary.Command.StartsWith($"'{shell}'", StringComparison.Ordinal))
                    profiles.Add(new(Path.GetFileName(shell), $"'{shell}' -l", primary.Directory));
        return profiles;
    }
}
