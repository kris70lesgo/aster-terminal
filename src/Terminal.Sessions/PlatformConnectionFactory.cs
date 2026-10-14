using Devolutions.Terminal.Connection;

namespace Aster.Sessions;

/// <summary>Only this composition boundary selects native process transports.</summary>
public static class PlatformConnectionFactory
{
    public static IRestartableTerminalConnection Create()
    {
        if (OperatingSystem.IsWindows()) return new ConPtyConnection();
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) return new LinuxPtyConnection();
        throw new PlatformNotSupportedException("Aster requires Windows, macOS or Linux.");
    }
}
