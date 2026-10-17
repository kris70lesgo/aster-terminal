using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aster.App.Views;

/// <summary>Numbered terminal-window shortcuts shown by the reference caption.</summary>
internal static class TerminalWindowNavigation
{
    private static readonly List<(WeakReference<Window> Window, WeakReference<TerminalTabs> Tabs)> Windows = [];
    public static void Attach(Window window, TerminalTabs tabs)
    {
        window.Opened += (_, _) => { Windows.Add((new(window), new(tabs))); Refresh(); };
        window.Closed += (_, _) => { Windows.RemoveAll(pair => !pair.Window.TryGetTarget(out var target) || target == window); Refresh(); };
        window.AddHandler(Window.KeyDownEvent, (_, e) =>
        {
            var modifiers = OperatingSystem.IsMacOS() ? KeyModifiers.Meta | KeyModifiers.Alt : KeyModifiers.Alt;
            if (e.KeyModifiers != modifiers || e.Key < Key.D1 || e.Key > Key.D9) return;
            var index = e.Key - Key.D1;
            var live = Live();
            if (index >= live.Count) return;
            var target = live[index].Window;
            if (target.WindowState == WindowState.Minimized) target.WindowState = WindowState.Normal;
            target.Activate(); e.Handled = true;
        }, RoutingStrategies.Tunnel);
    }
    private static List<(Window Window, TerminalTabs Tabs)> Live()
    {
        Windows.RemoveAll(pair => !pair.Window.TryGetTarget(out _) || !pair.Tabs.TryGetTarget(out _));
        return Windows.Select(pair => { pair.Window.TryGetTarget(out var w); pair.Tabs.TryGetTarget(out var t); return (Window: w!, Tabs: t!); }).ToList();
    }
    private static void Refresh()
    {
        var live = Live();
        for (var i = 0; i < live.Count; i++)
            live[i].Tabs.WindowShortcut = i < 9 ? (OperatingSystem.IsMacOS() ? "⌥⌘" : "Alt+") + (i + 1) : "";
    }
}
