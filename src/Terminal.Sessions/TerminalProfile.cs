using Devolutions.Terminal.Settings;

namespace Aster.Sessions;

public sealed record TerminalProfile(string Name, string Command, string Directory, double FontSize = 14)
{
    public static TerminalProfile Default()
    {
        var shell = OperatingSystem.IsWindows() ? "powershell.exe" : Environment.GetEnvironmentVariable("SHELL");
        if (string.IsNullOrWhiteSpace(shell) || (!OperatingSystem.IsWindows() && !File.Exists(shell)))
            shell = OperatingSystem.IsMacOS() ? "/bin/zsh" : "/bin/bash";
        var command = OperatingSystem.IsWindows() ? shell : $"'{shell.Replace("'", "'\\''")}' -l";
        return new(Path.GetFileNameWithoutExtension(shell), command,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    public ProfileSettings ToControlProfile() => new()
    {
        Name = Name,
        Commandline = Command,
        StartingDirectory = Directory,
        FontSize = FontSize,
        FontFace = "Cascadia Mono",
        Foreground = "#D8DEE9",
        Background = "#171A20",
        CursorColor = "#D8DEE9",
        SelectionBackground = "#3B506B",
        Padding = "10",
        CursorShape = "filledBox",
        HistorySize = 10000,
        CloseOnExit = CloseOnExitMode.Never,
        AllowVtClipboardWrite = false,
        AllowOscNotifications = false,
        ReloadEnvironmentVariables = false,
        Environment = new() { ["TERM"] = "xterm-256color", ["COLORTERM"] = "truecolor" }
    };
}
