using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aster.App.Views;

/// <summary>Numbered shortcuts for visible terminal windows in this application.</summary>
internal static class TerminalWindowNavigation
{
    private static readonly ConditionalWeakTable<Application, Registry> Registries = new();
    public static void Attach(Window window, TerminalTabs tabs) =>
        Registries.GetValue(Application.Current!, _ => new Registry()).Attach(window, tabs);

    private sealed class Registry
    {
        private readonly List<(WeakReference<Window> Window, WeakReference<TerminalTabs> Tabs)> _windows = [];
        public void Attach(Window window, TerminalTabs tabs)
        {
            window.Opened += (_, _) =>
            {
                Platform.MacWindowColorSpace.Configure(window);
                if (!_windows.Any(pair => pair.Window.TryGetTarget(out var target) && target == window))
                    _windows.Add((new(window), new(tabs)));
                Refresh();
            };
            void VisibilityChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            { if (e.Property == Window.IsVisibleProperty) Refresh(); }
            window.PropertyChanged += VisibilityChanged;
            window.Closed += (_, _) =>
            {
                window.PropertyChanged -= VisibilityChanged;
                _windows.RemoveAll(pair => !pair.Window.TryGetTarget(out var target) || target == window);
                Refresh();
            };
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
        private List<(Window Window, TerminalTabs Tabs)> Live()
        {
            _windows.RemoveAll(pair => !pair.Window.TryGetTarget(out _) || !pair.Tabs.TryGetTarget(out _));
            return _windows.Select(pair => { pair.Window.TryGetTarget(out var w); pair.Tabs.TryGetTarget(out var t); return (Window: w!, Tabs: t!); })
                .Where(pair => pair.Window.IsVisible).ToList();
        }
        private void Refresh()
        {
            var live = Live();
            for (var i = 0; i < live.Count; i++)
                live[i].Tabs.WindowShortcut = i < 9 ? (OperatingSystem.IsMacOS() ? "⌥⌘" : "Alt+") + (i + 1) : "";
        }
    }
}
