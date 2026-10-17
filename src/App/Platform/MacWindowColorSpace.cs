using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Aster.App.Platform;

internal static class MacWindowColorSpace
{
    public static void Configure(Window window)
    {
        if (!OperatingSystem.IsMacOS()) return;
        // The compositor has attached the backing layer by the next UI turn.
        Dispatcher.UIThread.Post(() =>
        {
            if (window.TryGetPlatformHandle() is { HandleDescriptor: "NSWindow" } handle)
                SetSrgb(handle.Handle);
        }, DispatcherPriority.Loaded);
    }

    [DllImport("libaster-appkit.dylib", EntryPoint = "aster_appkit_srgb_window")]
    private static extern void SetSrgb(nint window);
}
