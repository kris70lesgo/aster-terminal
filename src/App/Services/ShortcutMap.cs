using Avalonia.Input;

namespace Aster.App.Services;

public static class ShortcutMap
{
    public static Dictionary<string, string> Defaults()
    {
        var command = OperatingSystem.IsMacOS() ? "Meta" : "Control+Shift";
        return new()
        {
            ["NewTab"] = command + "+T", ["NewWindow"] = command + "+N", ["ClosePane"] = command + "+W",
            ["SplitRight"] = command + "+D", ["SplitBelow"] = command + (OperatingSystem.IsMacOS() ? "+Shift" : "+Alt") + "+D",
            ["Find"] = command + "+F", ["Copy"] = command + "+C", ["Paste"] = command + "+V",
            ["Preferences"] = command + "+OemComma", ["ZoomPane"] = command + "+Enter",
            ["NextTab"] = "Control+Tab", ["PreviousTab"] = "Control+Shift+Tab",
            ["NextPane"] = command + "+OemCloseBrackets", ["PreviousPane"] = command + "+OemOpenBrackets",
            ["LargerFont"] = command + (OperatingSystem.IsMacOS() ? "+Shift" : "") + "+OemPlus", ["SmallerFont"] = command + "+OemMinus", ["ResetFont"] = command + "+D0",
            ["PreviousMark"] = command + "+Up", ["NextMark"] = command + "+Down", ["OpenQuickly"] = command + "+Shift+O",
            ["Replay"] = command + "+Alt+B",
        };
    }

    public static Dictionary<string, KeyGesture> Resolve(IReadOnlyDictionary<string, string> overrides)
    {
        var bindings = Defaults();
        foreach (var item in overrides)
        {
            if (!bindings.ContainsKey(item.Key)) throw new ArgumentException($"Unknown shortcut action {item.Key}.");
            bindings[item.Key] = item.Value;
        }
        var result = new Dictionary<string, KeyGesture>();
        foreach (var item in bindings)
            try { result[item.Key] = KeyGesture.Parse(item.Value); }
            catch (Exception error) { throw new ArgumentException($"Invalid shortcut for {item.Key}: {item.Value}", error); }
        if (result.Values.Select(g => (g.Key, g.KeyModifiers)).Distinct().Count() != result.Count)
            throw new ArgumentException("Each shortcut must have a unique key combination.");
        return result;
    }
}
