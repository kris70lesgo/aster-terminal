using Avalonia;

namespace Aster.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
}
