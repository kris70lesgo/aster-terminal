using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aster.App.Services;

/// <summary>Exports the same command handlers to the OS menu bar; no second command implementation.</summary>
public static class NativeMenuBridge
{
    private static readonly Dictionary<string, string> Actions = new()
    {
        ["New tab"] = "NewTab", ["New window"] = "NewWindow", ["Close pane"] = "ClosePane",
        ["Split right"] = "SplitRight", ["Split below"] = "SplitBelow", ["Find…"] = "Find",
        ["Copy"] = "Copy", ["Paste"] = "Paste", ["Preferences…"] = "Preferences",
        ["Next tab"] = "NextTab", ["Next pane"] = "NextPane", ["Zoom active pane"] = "ZoomPane",
        ["Open quickly…"] = "OpenQuickly", ["Larger font"] = "LargerFont", ["Smaller font"] = "SmallerFont",
        ["Reset font size"] = "ResetFont", ["Previous mark"] = "PreviousMark", ["Next mark"] = "NextMark",
        ["Instant replay…"] = "Replay"
    };

    public static NativeMenu FromMenu(Menu menu, IReadOnlyDictionary<string, KeyGesture> gestures) => Convert(menu.Items, gestures, true);

    private static NativeMenu Convert(IEnumerable<object?> controls, IReadOnlyDictionary<string, KeyGesture> gestures, bool root = false)
    {
        var native = new NativeMenu();
        foreach (var control in controls)
        {
            if (control is Separator) { native.Add(new NativeMenuItemSeparator()); continue; }
            if (control is not MenuItem source) continue;
            var header = source.Header?.ToString() ?? "";
            if (root) header = header.Replace("_", "");
            var item = new NativeMenuItem(header) { IsEnabled = source.IsEnabled, ToggleType = source.ToggleType, IsChecked = source.IsChecked };
            if (source.Items.Count > 0) item.Menu = Convert(source.Items, gestures);
            else
            {
                if (Actions.TryGetValue(header, out var action)) item.Gesture = gestures.GetValueOrDefault(action);
                item.Click += (_, _) => source.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            native.Add(item);
        }
        return native;
    }
}
