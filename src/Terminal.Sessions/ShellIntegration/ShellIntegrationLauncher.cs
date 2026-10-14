using System.Text.RegularExpressions;
using Devolutions.Terminal.Settings;

namespace Aster.Sessions.ShellIntegration;

public static partial class ShellIntegrationLauncher
{
    public static ProfileSettings Prepare(TerminalProfile profile, bool light)
    {
        var settings = profile.ToControlProfile(light);
        if (!profile.ShellIntegration) return settings;
        if (OperatingSystem.IsWindows()) throw new ArgumentException("Automatic command marks currently support local bash and zsh profiles.");
        var match = LocalShell().Match(profile.Command);
        if (!match.Success) throw new ArgumentException("For automatic marks, use a local bash or zsh command with optional -l/-i flags.");
        var shell = match.Groups["shell"].Value;
        var root = Path.Combine(AppContext.BaseDirectory, "ShellIntegration");
        if (!File.Exists(Path.Combine(root, "iterm2.bash"))) throw new FileNotFoundException("Bundled shell integration is missing.");
        settings.Environment["ASTER_INTEGRATION_ROOT"] = root;
        settings.Environment["ASTER_LOGIN_SHELL"] = profile.Command.Contains("-l", StringComparison.Ordinal) || profile.Command.Contains("--login", StringComparison.Ordinal) ? "1" : "0";
        if (shell.EndsWith("zsh", StringComparison.Ordinal))
        {
            settings.Environment["ASTER_USER_ZDOTDIR"] = profile.EnvironmentVariables.GetValueOrDefault("ZDOTDIR") ?? Environment.GetEnvironmentVariable("ZDOTDIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            settings.Environment["ZDOTDIR"] = Path.Combine(root, "zsh");
            settings.Commandline = Quote(shell) + (profile.Command.Contains(" -d", StringComparison.Ordinal) ? " -d" : "") + (settings.Environment["ASTER_LOGIN_SHELL"] == "1" ? " -l -i" : " -i");
        }
        else settings.Commandline = Quote(shell) + " --rcfile " + Quote(Path.Combine(root, "aster.bash")) + " -i";
        return settings;
    }
    private static string Quote(string text) => "'" + text.Replace("'", "'\\''") + "'";
    [GeneratedRegex("^\\s*(?:exec\\s+)?['\"]?(?<shell>(?:/[^'\"\\s]+/)?(?:bash|zsh))['\"]?(?:\\s+(?:-l|-i|-d|-il|-li|--login))*\\s*$")]
    private static partial Regex LocalShell();
}
