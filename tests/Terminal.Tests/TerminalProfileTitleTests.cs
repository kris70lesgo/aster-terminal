using Aster.Sessions;
using Aster.App.Views;
using Avalonia.Headless.XUnit;

namespace Aster.Tests;

public sealed class TerminalProfileTitleTests
{
    [Theory]
    [InlineData("zsh", "'/bin/zsh' -l", "-zsh")]
    [InlineData("bash", "/bin/bash --login", "-bash")]
    [InlineData("zsh", "exec '/usr/local/bin/zsh' -li", "-zsh")]
    [InlineData("Work", "'/bin/zsh' -l", "Work")]
    [InlineData("zsh", "'/bin/zsh' -i", "zsh")]
    [InlineData("PowerShell", "powershell.exe", "PowerShell")]
    public void InitialTitleMatchesLoginShellCaptionWithoutOverridingCustomNames(
        string name,
        string command,
        string expected)
    {
        var profile = new TerminalProfile(name, command, Path.GetTempPath());

        Assert.Equal(expected, profile.InitialSessionTitle);
    }

    [AvaloniaFact]
    public async Task PaneUsesTheItermStyleLoginShellTitleBeforeShellOutputArrives()
    {
        var pane = new TerminalPane(new TerminalProfile("zsh", "'/bin/zsh' -l", Path.GetTempPath()));
        try
        {
            Assert.Equal("-zsh", pane.SessionTitle);
        }
        finally
        {
            await pane.CloseAsync();
        }
    }
}
