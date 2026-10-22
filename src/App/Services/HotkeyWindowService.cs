using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using Aster.App.Views;
using Aster.Sessions.Hotkeys;
using Aster.Sessions.Persistence;

namespace Aster.App.Services;

/// <summary>One opt-in native registration and one lazy real terminal window per UI process.</summary>
public sealed class HotkeyWindowService : IDisposable
{
    public static HotkeyWindowService Shared { get; } = new();
    private readonly Func<Action, Action<Exception>, IDisposable> _register;
    private IDisposable? _registration;
    private SettingsStore? _store;
    private MainWindow? _window;
    private bool _disposed;
    public bool IsRegistered => _registration is not null;
    public MainWindow? Window => _window;
    public string? RegistrationError { get; private set; }
    public bool? LastMacActivationRequestSucceeded { get; private set; }
    public HotkeyWindowService(Func<Action, Action<Exception>, IDisposable>? register = null)
    { _register = register ?? ((pressed, failed) => NativeGlobalHotkey.Register(pressed, failed)); }
    public void Configure(SettingsStore store)
    {
        Dispatcher.UIThread.VerifyAccess(); ObjectDisposedException.ThrowIf(_disposed, this);
        _store = store;
        if (!store.Settings.HotkeyWindowEnabled)
        {
            _registration?.Dispose(); _registration = null; RegistrationError = null;
            // Disabling the binding cannot strand a hidden live terminal.
            if (_window is { IsVisible: false }) { _window.ShowActivated = false; _window.Show(); }
            return;
        }
        if (_registration is not null) return;
        try
        {
            _registration = _register(() => Dispatcher.UIThread.Post(() => { if (!_disposed && _registration is not null) Toggle(); }), error => Dispatcher.UIThread.Post(() => Failed(error)));
            RegistrationError = null;
        }
        catch (Exception error) when (error is IOException or System.ComponentModel.Win32Exception or PlatformNotSupportedException or DllNotFoundException or EntryPointNotFoundException)
        { RegistrationError = error.Message; throw new IOException("Global hotkey unavailable: " + error.Message, error); }
    }
    private void Failed(Exception error)
    {
        if (_disposed) return; RegistrationError = error.Message; _registration?.Dispose(); _registration = null;
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Windows.OfType<MainWindow>().FirstOrDefault(w => w.IsVisible)?.ReportApplicationError("Global hotkey stopped: " + error.Message);
    }
    public void Toggle()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_disposed || _store is null) return;
        if (_window is { IsVisible: true, IsActive: true }) { _window.Hide(); return; }
        Show();
    }
    public void Show()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_disposed || _store is null) return;
        if (_window is null)
        {
            var window = new MainWindow(_store, hotkeyWindow: true) { ShowActivated = true };
            _window = window; window.Closed += (_, _) => { if (_window == window) _window = null; };
        }
        if (!_window.IsVisible) _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        if (OperatingSystem.IsMacOS()) LastMacActivationRequestSucceeded = ActivateMacApplication();
        _window.Activate();
    }

    private static bool ActivateMacApplication()
    {
        var application = SendPointer(objc_getClass("NSRunningApplication"), sel_registerName("currentApplication"));
        return application != IntPtr.Zero && SendBoolUnsigned(application, sel_registerName("activateWithOptions:"), 2); // NSApplicationActivateIgnoringOtherApps
    }

    [DllImport("/usr/lib/libobjc.A.dylib")] private static extern IntPtr objc_getClass(string name);
    [DllImport("/usr/lib/libobjc.A.dylib")] private static extern IntPtr sel_registerName(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static extern IntPtr SendPointer(IntPtr receiver, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] [return: MarshalAs(UnmanagedType.I1)] private static extern bool SendBoolUnsigned(IntPtr receiver, IntPtr selector, nuint value);

    public void Dispose() { if (_disposed) return; _disposed = true; _registration?.Dispose(); _registration = null; }
}
