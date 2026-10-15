using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Aster.Sessions;
using Aster.Sessions.Hotkeys;
using Aster.Sessions.Persistence;
using Devolutions.Terminal.Core;
using Aster.App.Views;

namespace Aster.App.Services;

/// <summary>Isolated native registration/dispatch fixture. Global key injection is restricted to CI.</summary>
public static class HotkeySmoke
{
    public static Window Configure(IClassicDesktopStyleApplicationLifetime desktop, string[] args)
    {
        var index = Array.IndexOf(args, "--hotkey-report"); if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("A hotkey report directory is required.");
        var report = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(report);
        var peerMode = args.Contains("--hotkey-peer"); var global = args.Contains("--hotkey-global-test"); var manual = args.Contains("--hotkey-manual");
        if ((peerMode || global) && Environment.GetEnvironmentVariable("CI") != "true") throw new ArgumentException("Global key injection and foreground peer fixtures only run in CI.");
        var window = new Window { Title = peerMode ? "Hotkey focus peer" : "Aster hotkey acceptance", Width = 700, Height = 380, Content = new TextBlock { Margin = new Thickness(24), TextWrapping = Avalonia.Media.TextWrapping.Wrap, Text = peerMode ? "Isolated focus peer for global hotkey acceptance." : manual ? "Hotkey test ready. With another app focused, press Control+Option+Space once. The test terminal will appear briefly." : "Native hotkey registration and dispatch acceptance." } };
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (peerMode)
        {
            window.Activated += (_, _) =>
            {
                var path = Path.Combine(report, "peer.json"); var temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(new { pid = Environment.ProcessId })); File.Move(temp, path, true);
            };
            window.Opened += (_, _) => window.Activate(); return window;
        }
        window.Opened += async (_, _) => await Run(window, desktop, report, global, manual);
        return window;
    }
    private static async Task Run(Window owner, IClassicDesktopStyleApplicationLifetime desktop, string report, bool global, bool manual)
    {
        Exception? failure = null; var checks = new List<string>(); var skips = new List<string>(); var scale = owner.RenderScaling;
        var state = Path.Combine(Path.GetTempPath(), "aster-hotkey-native-" + Guid.NewGuid().ToString("N"));
        HotkeyWindowService? service = null; Process? peer = null; MainWindow? terminalWindow = null;
        try
        {
            var callbacks = 0;
            using (NativeGlobalHotkey.Register(() => Interlocked.Increment(ref callbacks)))
            {
                checks.Add("Native system-wide Control+Alt+Space registration succeeds");
                if (OperatingSystem.IsMacOS())
                {
                    SendCarbonEvent(); await Until(() => callbacks == 1);
                    checks.Add("macOS Carbon event dispatcher routes the registered signature/id to the native callback");
                }
            }
            using (NativeGlobalHotkey.Register(() => { })) checks.Add("Native unregister/re-register succeeds without leaking the binding");
            if (global && OperatingSystem.IsMacOS() && !CanPostMacEvents())
            { skips.Add("macOS CI has not granted Post Event access; cross-application physical-key injection was not attempted."); global = false; }
            if (!global && !manual)
            {
                skips.Add("Actual cross-application summon requires a physical/manual test or an authorised CI input injector."); return;
            }
            var store = new SettingsStore(Path.Combine(state, "settings.json"));
            var profile = new TerminalProfile("Hotkey acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "export PS1='ASTER_READY> '; exec /bin/sh", Path.GetTempPath())
            { EnvironmentVariables = new() { ["PROMPT"] = "ASTER_READY$G" } };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id, RestoreLayout = false, ConfirmClose = false, KeepSessionsAlive = true, HotkeyWindowEnabled = true });
            service = new(); service.Configure(store);
            if (global)
            {
                var executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "Aster.Terminal.exe" : "Aster.Terminal");
                var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
                foreach (var arg in new[] { "--hotkey-peer", "--hotkey-report", Path.Combine(report, "peer") }) start.ArgumentList.Add(arg);
                peer = Process.Start(start) ?? throw new IOException("Unable to start the isolated focus peer.");
                await Until(() => File.Exists(Path.Combine(report, "peer/peer.json")));
                await FocusPeer(peer); await InjectChord();
            }
            await Until(() => service.Window is { IsVisible: true, IsActive: true }, manual ? 180 : 30);
            terminalWindow = service.Window!;
            await Until(() => Pane(terminalWindow) is { Terminal.IsRunning: true } && Text(Pane(terminalWindow)!).Contains("ASTER_READY>"));
            var pane = Pane(terminalWindow)!; var session = pane.HostedSession!.Id;
            Command(pane, OperatingSystem.IsWindows() ? "set ASTER_HOTKEY_VALUE=retained" : "ASTER_HOTKEY_VALUE=retained");
            Command(pane, OperatingSystem.IsWindows() ? "echo __HOTKEY_READY__%ASTER_HOTKEY_VALUE%" : "printf '__HOTKEY_READY__%s\\n' \"$ASTER_HOTKEY_VALUE\"");
            await Until(() => Text(pane).Split('\n').Any(l => l.Trim() == "__HOTKEY_READY__retained"));
            checks.Add(manual ? "Human physical shortcut summons and focuses the real terminal from another foreground application" : "Injected global chord summons and focuses a real terminal while the isolated peer process owns foreground focus");
            if (global)
            {
                await InjectChord(); await Until(() => !terminalWindow.IsVisible); if (!pane.Terminal.IsRunning) throw new InvalidOperationException("Hiding ended the live shell.");
                await FocusPeer(peer!); await InjectChord(); await Until(() => terminalWindow.IsVisible && terminalWindow.IsActive);
                if (pane.HostedSession!.Id != session || Pane(terminalWindow) != pane) throw new InvalidOperationException("Summon replaced the existing pane/session.");
                Command(pane, OperatingSystem.IsWindows() ? "echo __HOTKEY_AGAIN__%ASTER_HOTKEY_VALUE%" : "printf '__HOTKEY_AGAIN__%s\\n' \"$ASTER_HOTKEY_VALUE\"");
                await Until(() => Text(pane).Split('\n').Any(l => l.Trim() == "__HOTKEY_AGAIN__retained"));
                checks.Add("Global chord hides/summons the same live shell with retained variables and session identity");
            }
            await Task.Delay(150); Image(terminalWindow, Path.Combine(report, "hotkey-window.png"));
            terminalWindow.Close(); await Until(() => !terminalWindow.IsVisible && !pane.Terminal.IsRunning);
            service.Dispose(); using (NativeGlobalHotkey.Register(() => { })) { }
            checks.Add("Normal close cleans up the hosted PTY; disposing the service releases the native shortcut");
        }
        catch (TimeoutException) when (manual && service?.Window is null) { skips.Add("No human physical chord arrived before the manual fixture deadline; cross-application summon remains unverified."); }
        catch (Exception error) { failure = error; if ((terminalWindow ?? owner).IsVisible) Image(terminalWindow ?? owner, Path.Combine(report, "failure.png")); }
        finally
        {
            terminalWindow?.Close(); service?.Window?.Close(); service?.Dispose();
            if (peer is not null) { if (!peer.HasExited) peer.Kill(); await peer.WaitForExitAsync(); peer.Dispose(); }
            if (Directory.Exists(Path.Combine(state, "sessions")))
            {
                var catalog = new Aster.Sessions.Hosting.SessionCatalog(Path.Combine(state, "sessions"));
                foreach (var path in Directory.EnumerateFiles(catalog.DirectoryPath, "*.json")) try { await catalog.TerminateAsync(Aster.Sessions.Hosting.SessionCatalog.Read(path)); } catch (IOException error) { failure ??= error; }
                if (!Directory.EnumerateFiles(catalog.DirectoryPath, "*.json").Any()) try { Directory.Delete(state, true); } catch (IOException) { }
            }
            File.WriteAllText(Path.Combine(report, "hotkey.json"), JsonSerializer.Serialize(new { passed = failure is null, timestamp = DateTimeOffset.UtcNow, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, renderScaling = scale, checks, skips, error = failure?.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            owner.Close(); desktop.Shutdown(failure is null ? 0 : 1);
        }
    }
    private static TerminalPane? Pane(Window window) => window.GetVisualDescendants().OfType<TerminalPane>().SingleOrDefault();
    private static string Text(TerminalPane pane) => TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot(true).Buffer);
    private static void Command(TerminalPane pane, string text)
    {
        pane.Terminal.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = text });
        pane.Terminal.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
    }
    private static async Task Until(Func<bool> condition, int seconds = 30)
    { var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(seconds)) throw new TimeoutException("Native hotkey acceptance timed out."); await Task.Delay(25); } }
    private static void Image(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)window.Bounds.Width), Math.Max(1, (int)window.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }
    private static async Task<string> Tool(string executable, params string[] args)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); if (process.ExitCode != 0) throw new IOException(await error); return await output;
    }
    private static async Task FocusPeer(Process peer)
    {
        if (OperatingSystem.IsWindows())
        {
            peer.Refresh(); if (!SetForegroundWindow(peer.MainWindowHandle)) throw new IOException("The focus peer could not become foreground.");
            await Until(() => { GetWindowThreadProcessId(GetForegroundWindow(), out var pid); return pid == peer.Id; });
        }
        else if (OperatingSystem.IsLinux())
        {
            var ids = (await Tool("xdotool", "search", "--onlyvisible", "--pid", peer.Id.ToString(), "--name", "Hotkey focus peer")).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            await Tool("xdotool", "windowfocus", "--sync", ids[0]);
            var pid = (await Tool("xdotool", "getwindowfocus", "getwindowpid")).Trim(); if (pid != peer.Id.ToString()) throw new IOException("X11 focus does not belong to the isolated peer.");
        }
        else if (OperatingSystem.IsMacOS()) await Until(() => MacForegroundPid() == peer.Id);
    }
    private static async Task InjectChord()
    {
        if (OperatingSystem.IsWindows())
        {
            var keys = new[] { (0x11, 0), (0x12, 0), (0x20, 0), (0x20, 2), (0x12, 2), (0x11, 2) };
            var inputs = keys.Select(k => new Input { Type = 1, Keyboard = new Keyboard { Key = (ushort)k.Item1, Flags = (uint)k.Item2 } }).ToArray();
            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length) throw new IOException("Windows blocked global test input.");
        }
        else if (OperatingSystem.IsLinux()) await Tool("xdotool", "key", "ctrl+alt+space");
        else if (OperatingSystem.IsMacOS())
        {
            foreach (var key in new[] { (59, true, 1UL << 18), (58, true, (1UL << 18) | (1UL << 19)), (49, true, (1UL << 18) | (1UL << 19)), (49, false, (1UL << 18) | (1UL << 19)), (58, false, 1UL << 18), (59, false, 0UL) })
            {
                var ev = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key.Item1, key.Item2); if (ev == IntPtr.Zero) throw new IOException("Could not create macOS test input.");
                try { CGEventSetFlags(ev, key.Item3); CGEventPost(0, ev); } finally { CFRelease(ev); }
            }
        }
        await Task.Delay(100);
    }
    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private const string Graphics = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    [StructLayout(LayoutKind.Sequential)] private struct HotkeyId { public uint Signature, Id; }
    private static void SendCarbonEvent()
    {
        var result = CreateEvent(IntPtr.Zero, 0x6b657962, 5, 0, 0, out var ev); if (result != 0) throw new IOException("Could not create Carbon fixture event.");
        try
        {
            var id = new HotkeyId { Signature = 0x41535452, Id = 1 };
            if (SetEventParameter(ev, 0x2d2d2d2d, 0x686b6964, 8, ref id) != 0 || SendEventToEventTarget(ev, GetEventDispatcherTarget()) != 0) throw new IOException("Carbon fixture dispatch failed.");
        }
        finally { ReleaseEvent(ev); }
    }
    private static int MacForegroundPid()
    {
        var workspace = SendPointer(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
        var app = SendPointer(workspace, sel_registerName("frontmostApplication")); return SendInt(app, sel_registerName("processIdentifier"));
    }
    private static bool CanPostMacEvents() => CGPreflightPostEventAccess();
    [DllImport(Carbon)] private static extern int CreateEvent(IntPtr allocator, uint eventClass, uint kind, double when, uint attributes, out IntPtr ev);
    [DllImport(Carbon)] private static extern int SetEventParameter(IntPtr ev, uint name, uint type, nuint size, ref HotkeyId id);
    [DllImport(Carbon)] private static extern int SendEventToEventTarget(IntPtr ev, IntPtr target);
    [DllImport(Carbon)] private static extern IntPtr GetEventDispatcherTarget();
    [DllImport(Carbon)] private static extern void ReleaseEvent(IntPtr ev);
    [DllImport(Graphics)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CGPreflightPostEventAccess();
    [DllImport(Graphics)] private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort key, [MarshalAs(UnmanagedType.I1)] bool down);
    [DllImport(Graphics)] private static extern void CGEventSetFlags(IntPtr ev, ulong flags);
    [DllImport(Graphics)] private static extern void CGEventPost(uint location, IntPtr ev);
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")] private static extern void CFRelease(IntPtr value);
    [DllImport("/usr/lib/libobjc.A.dylib")] private static extern IntPtr objc_getClass(string name);
    [DllImport("/usr/lib/libobjc.A.dylib")] private static extern IntPtr sel_registerName(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static extern IntPtr SendPointer(IntPtr receiver, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")] private static extern int SendInt(IntPtr receiver, IntPtr selector);
    [StructLayout(LayoutKind.Sequential)] private struct Keyboard { public ushort Key, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct Input { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public Keyboard Keyboard; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, [In] Input[] inputs, int size);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
}
