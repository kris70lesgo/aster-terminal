using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Aster.Sessions.Hotkeys;

/// <summary>Registers only Control+Alt+Space. Never observes arbitrary keyboard input.</summary>
public static class NativeGlobalHotkey
{
    public static IDisposable Register(Action pressed, Action<Exception>? failed = null)
    {
        ArgumentNullException.ThrowIfNull(pressed);
        void Failed(Exception error) { try { failed?.Invoke(error); } catch { /* Never unwind a native callback or shutdown worker. */ } }
        void Pressed() { try { pressed(); } catch (Exception error) { Failed(error); } }
        if (OperatingSystem.IsMacOS()) return new CarbonHotkey(Pressed);
        if (OperatingSystem.IsWindows()) return new WindowsHotkey(Pressed, Failed);
        if (OperatingSystem.IsLinux()) return new XcbHotkey(Pressed, Failed);
        throw new PlatformNotSupportedException("Global hotkeys require macOS, Windows or Linux X11.");
    }
    [SupportedOSPlatform("macos")]
    private sealed class CarbonHotkey : IDisposable
    {
        private const string Library = "/System/Library/Frameworks/Carbon.framework/Carbon";
        [StructLayout(LayoutKind.Sequential)] private struct EventSpec { public uint Class, Kind; }
        [StructLayout(LayoutKind.Sequential)] private struct HotkeyId { public uint Signature, Id; }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Handler(IntPtr next, IntPtr ev, IntPtr data);
        private readonly Handler _handler;
        private IntPtr _handlerRef, _keyRef;
        private bool _pressed, _disposed;
        public CarbonHotkey(Action callback)
        {
            _handler = (_, ev, _) =>
            {
                if (_disposed || GetEventParameter(ev, 0x2d2d2d2d, 0x686b6964, IntPtr.Zero, 8, IntPtr.Zero, out var id) != 0 || id.Signature != 0x41535452 || id.Id != 1) return -9874;
                if (GetEventKind(ev) == 6) _pressed = false;
                else if (!_pressed) { _pressed = true; callback(); }
                return 0;
            };
            var specs = new[] { new EventSpec { Class = 0x6b657962, Kind = 5 }, new EventSpec { Class = 0x6b657962, Kind = 6 } };
            var status = InstallEventHandler(GetEventDispatcherTarget(), _handler, 2, specs, IntPtr.Zero, out _handlerRef);
            if (status != 0) throw new IOException($"macOS hotkey handler failed ({status}).");
            status = RegisterEventHotKey(49, 4096 | 2048, new HotkeyId { Signature = 0x41535452, Id = 1 }, GetEventDispatcherTarget(), 0, out _keyRef);
            if (status != 0) { Dispose(); throw new IOException($"Control+Option+Space could not be registered ({status}). Another application may use it."); }
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if (_keyRef != IntPtr.Zero) { UnregisterEventHotKey(_keyRef); _keyRef = IntPtr.Zero; }
            if (_handlerRef != IntPtr.Zero) { RemoveEventHandler(_handlerRef); _handlerRef = IntPtr.Zero; }
            GC.KeepAlive(_handler);
        }
        [DllImport(Library)] private static extern IntPtr GetEventDispatcherTarget();
        [DllImport(Library)] private static extern int InstallEventHandler(IntPtr target, Handler handler, uint count, [In] EventSpec[] specs, IntPtr data, out IntPtr reference);
        [DllImport(Library)] private static extern int RegisterEventHotKey(uint code, uint modifiers, HotkeyId id, IntPtr target, uint options, out IntPtr reference);
        [DllImport(Library)] private static extern int UnregisterEventHotKey(IntPtr reference);
        [DllImport(Library)] private static extern int RemoveEventHandler(IntPtr reference);
        [DllImport(Library)] private static extern uint GetEventKind(IntPtr ev);
        [DllImport(Library)] private static extern int GetEventParameter(IntPtr ev, uint name, uint type, IntPtr actualType, nuint size, IntPtr actualSize, out HotkeyId id);
    }
    [SupportedOSPlatform("windows")]
    private sealed class WindowsHotkey : IDisposable
    {
        private readonly Thread _thread;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private uint _threadId;
        private volatile bool _disposed;
        public WindowsHotkey(Action callback, Action<Exception>? failed)
        {
            _thread = new Thread(() =>
            {
                var registered = false;
                try
                {
                    _threadId = GetCurrentThreadId();
                    // Force creation of the thread's message queue before disposal
                    // can post WM_QUIT. Registration is owned by this same thread.
                    PeekMessageW(out _, IntPtr.Zero, 0, 0, 0);
                    if (_disposed) return;
                    if (!RegisterHotKey(IntPtr.Zero, 0xa57, 0x4003, 0x20)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Ctrl+Alt+Space could not be registered. Another application may use it.");
                    registered = true; _ready.TrySetResult();
                    while (!_disposed)
                    {
                        var result = GetMessageW(out var message, IntPtr.Zero, 0, 0);
                        if (result == 0) break;
                        if (result < 0) throw new Win32Exception(Marshal.GetLastWin32Error());
                        if (message.Id == 0x0312 && message.WParam == 0xa57) callback();
                    }
                }
                catch (Exception error) { if (!_ready.TrySetException(error) && !_disposed) failed?.Invoke(error); }
                finally { if (registered) UnregisterHotKey(IntPtr.Zero, 0xa57); _ready.TrySetException(new IOException("Hotkey registration stopped.")); }
            }) { IsBackground = true, Name = "Aster registered hotkey" };
            _thread.Start();
            try { _ready.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult(); }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if (_threadId != 0) PostThreadMessageW(_threadId, 0x0012, 0, IntPtr.Zero);
            if (Thread.CurrentThread != _thread) _thread.Join(1500);
        }
        [StructLayout(LayoutKind.Sequential)] private struct Message
        { public IntPtr Window; public uint Id; public nuint WParam; public IntPtr LParam; public uint Time; public int X, Y; public uint Private; }
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetMessageW(out Message message, IntPtr window, uint min, uint max);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PeekMessageW(out Message message, IntPtr window, uint min, uint max, uint remove);
        [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostThreadMessageW(uint thread, uint message, nuint wParam, IntPtr lParam);
    }
    [SupportedOSPlatform("linux")]
    private sealed class XcbHotkey : IDisposable
    {
        private readonly IntPtr _connection;
        private readonly Thread _thread;
        private volatile bool _disposed;
        private readonly byte _keycode;
        public XcbHotkey(Action callback, Action<Exception>? failed)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase)) throw new PlatformNotSupportedException("System-wide summon is not available on Wayland yet. XWayland does not provide compositor-wide shortcuts.");
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))) throw new PlatformNotSupportedException("Global hotkeys require an X11 display.");
            _connection = xcb_connect(IntPtr.Zero, out var screenNumber);
            if (_connection == IntPtr.Zero || xcb_connection_has_error(_connection) != 0) { if (_connection != IntPtr.Zero) xcb_disconnect(_connection); throw new IOException("Could not open the X11 hotkey connection."); }
            try
            {
                var setup = xcb_get_setup(_connection); var iterator = xcb_setup_roots_iterator(setup);
                for (var i = 0; i < screenNumber && iterator.Rem > 0; i++) xcb_screen_next(ref iterator);
                if (iterator.Rem <= 0 || iterator.Data == IntPtr.Zero) throw new IOException("No X11 screen is available.");
                var root = unchecked((uint)Marshal.ReadInt32(iterator.Data));
                var min = Marshal.ReadByte(setup, 34); var max = Marshal.ReadByte(setup, 35);
                var reply = xcb_get_keyboard_mapping_reply(_connection, xcb_get_keyboard_mapping(_connection, min, checked((byte)(max - min + 1))), out var error);
                if (error != IntPtr.Zero) { free(error); throw new IOException("Unable to query the X11 keyboard map."); }
                if (reply == IntPtr.Zero) throw new IOException("X11 keyboard map is unavailable.");
                try
                {
                    var perKey = Marshal.ReadByte(reply, 1); var symbols = xcb_get_keyboard_mapping_keysyms(reply); var length = xcb_get_keyboard_mapping_keysyms_length(reply);
                    if (perKey == 0 || length > 65536) throw new IOException("Invalid X11 keyboard map.");
                    for (var i = 0; i < length; i++) if (Marshal.ReadInt32(symbols, i * 4) == 0x20) { _keycode = checked((byte)(min + i / perKey)); break; }
                    if (_keycode == 0) throw new IOException("No space key exists in the X11 keyboard map.");
                }
                finally { free(reply); }
                // Ignore Caps Lock and the conventional Num Lock modifier.
                foreach (var locks in new ushort[] { 0, 2, 16, 18 })
                {
                    error = xcb_request_check(_connection, xcb_grab_key_checked(_connection, 0, root, (ushort)(4 | 8 | locks), _keycode, 1, 1));
                    if (error != IntPtr.Zero) { var code = Marshal.ReadByte(error, 1); free(error); throw new IOException($"Ctrl+Alt+Space could not be registered on X11 ({code}). Another application may use it."); }
                }
                xcb_flush(_connection);
                _thread = new Thread(() => Read(callback, failed)) { IsBackground = true, Name = "Aster X11 registered hotkey" }; _thread.Start();
            }
            catch { xcb_disconnect(_connection); throw; }
        }
        private void Read(Action callback, Action<Exception>? failed)
        {
            var pressed = false; uint? releasedAt = null; var releaseClock = System.Diagnostics.Stopwatch.StartNew();
            var descriptor = new PollFd { Descriptor = xcb_get_file_descriptor(_connection), Events = 1 };
            try
            {
                while (!_disposed)
                {
                    IntPtr ev;
                    while ((ev = xcb_poll_for_event(_connection)) != IntPtr.Zero)
                    {
                        try
                        {
                            var kind = Marshal.ReadByte(ev) & 0x7f; var code = Marshal.ReadByte(ev, 1); var time = unchecked((uint)Marshal.ReadInt32(ev, 4));
                            if (code != _keycode || kind is not (2 or 3)) continue;
                            if (kind == 3) { releasedAt = time; releaseClock.Restart(); continue; }
                            if (releasedAt == time) { releasedAt = null; continue; } // X11 auto-repeat release/press pair
                            if (releasedAt is not null) { pressed = false; releasedAt = null; }
                            if (!pressed) { pressed = true; callback(); }
                        }
                        finally { free(ev); }
                    }
                    if (releasedAt is not null && releaseClock.ElapsedMilliseconds >= 10) { releasedAt = null; pressed = false; }
                    if (xcb_connection_has_error(_connection) != 0) throw new IOException("The X11 hotkey connection ended.");
                    descriptor.Returned = 0; poll(ref descriptor, 1, releasedAt is null ? 200 : 10);
                }
            }
            catch (Exception error) { if (!_disposed) failed?.Invoke(error); }
            finally { xcb_disconnect(_connection); }
        }
        public void Dispose() { if (_disposed) return; _disposed = true; if (Thread.CurrentThread != _thread) _thread.Join(1500); }
        [StructLayout(LayoutKind.Sequential)] private struct Iterator { public IntPtr Data; public int Rem, Index; }
        [StructLayout(LayoutKind.Sequential)] private struct PollFd { public int Descriptor; public short Events, Returned; }
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_connect(IntPtr display, out int screen);
        [DllImport("libxcb.so.1")] private static extern void xcb_disconnect(IntPtr connection);
        [DllImport("libxcb.so.1")] private static extern int xcb_connection_has_error(IntPtr connection);
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_get_setup(IntPtr connection);
        [DllImport("libxcb.so.1")] private static extern Iterator xcb_setup_roots_iterator(IntPtr setup);
        [DllImport("libxcb.so.1")] private static extern void xcb_screen_next(ref Iterator iterator);
        [DllImport("libxcb.so.1")] private static extern uint xcb_get_keyboard_mapping(IntPtr connection, byte first, byte count);
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_get_keyboard_mapping_reply(IntPtr connection, uint cookie, out IntPtr error);
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_get_keyboard_mapping_keysyms(IntPtr reply);
        [DllImport("libxcb.so.1")] private static extern int xcb_get_keyboard_mapping_keysyms_length(IntPtr reply);
        [DllImport("libxcb.so.1")] private static extern uint xcb_grab_key_checked(IntPtr connection, byte owner, uint root, ushort modifiers, byte code, byte pointerMode, byte keyboardMode);
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_request_check(IntPtr connection, uint cookie);
        [DllImport("libxcb.so.1")] private static extern int xcb_flush(IntPtr connection);
        [DllImport("libxcb.so.1")] private static extern int xcb_get_file_descriptor(IntPtr connection);
        [DllImport("libxcb.so.1")] private static extern IntPtr xcb_poll_for_event(IntPtr connection);
        [DllImport("libc")] private static extern void free(IntPtr pointer);
        [DllImport("libc", SetLastError = true)] private static extern int poll(ref PollFd descriptor, nuint count, int timeout);
    }
}
