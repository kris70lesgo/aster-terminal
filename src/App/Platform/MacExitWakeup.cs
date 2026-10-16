using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Aster.App.Platform;

/// <summary>Wake AppKit's outer loop after graceful dispatcher shutdown starts.</summary>
internal static class MacExitWakeup
{
    private static bool _started;
    public static void Start()
    {
        if (!OperatingSystem.IsMacOS() || _started) return;
        Dispatcher.UIThread.VerifyAccess();
        BeginExit(); _started = true;
    }
    public static void Stop()
    {
        if (!_started) return;
        EndExit(); _started = false;
    }
    [DllImport("libaster-appkit.dylib", EntryPoint = "aster_appkit_begin_exit")] private static extern void BeginExit();
    [DllImport("libaster-appkit.dylib", EntryPoint = "aster_appkit_end_exit")] private static extern void EndExit();
}
