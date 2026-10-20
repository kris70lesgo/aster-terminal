using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Aster.App.Services;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.App.Views;

public partial class PreferencesWindow : Window
{
    private readonly SettingsStore _store;
    private readonly List<TerminalProfile> _profiles;
    private int _selected = -1;
    private string _defaultId;
    private bool _loading;
    private readonly TriggerEditor _triggerEditor = new();
    private static readonly string[] CursorShapes = ["filledBox", "bar", "underscore"];
    public AppSettings? Result { get; private set; }

    public PreferencesWindow() : this(new SettingsStore()) { }

    public PreferencesWindow(SettingsStore store)
    {
        _store = store;
        _profiles = store.Settings.Profiles.ToList();
        _defaultId = store.Settings.DefaultProfileId;
        InitializeComponent();
        SettingsTitle.RenderTransform = new TranslateTransform(0, -1.5);
        WindowButtons.EnableCaptionDrag(SettingsCaption);
        TriggerEditorHost.Content = _triggerEditor;
        ProfileList.SelectionChanged += ProfileSelected;
        AlwaysShowTabBarBox.IsChecked = store.Settings.AlwaysShowTabBar;
        ShowStatusBarBox.IsChecked = store.Settings.ShowStatusBar;
        ReplayEnabledBox.IsChecked = store.Settings.ScreenReplayEnabled;
        ThemeBox.ItemsSource = new[] { "Dark", "Light", "System" };
        ThemeBox.SelectedItem = store.Settings.Theme;
        CursorBox.ItemsSource = new[] { "Block", "Vertical bar", "Underline" };
        ScriptingEnabledBox.IsChecked = store.Settings.ScriptingEnabled;
        ScriptingPortBox.Value = store.Settings.ScriptingPort;
        HotkeyWindowBox.IsChecked = store.Settings.HotkeyWindowEnabled;
        KeepSessionsAliveBox.IsChecked = store.Settings.KeepSessionsAlive;
        RestoreLayoutBox.IsChecked = store.Settings.RestoreLayout;
        CopyOnSelectBox.IsChecked = store.Settings.CopyOnSelect;
        SmartSelectionBox.IsChecked = store.Settings.DoubleClickSmartSelection;
        ConfirmCloseBox.IsChecked = store.Settings.ConfirmClose;
        SettingsPath.Text = store.Path;
        ResetShortcuts(false);
        ReloadProfiles(0);
    }

    private void ReloadProfiles(int index)
    {
        _loading = true;
        ProfileList.ItemsSource = null; ProfileList.ItemsSource = _profiles;
        _selected = -1; ProfileList.SelectedIndex = index;
        _loading = false;
        LoadProfile(index);
    }
    private void LoadProfile(int index)
    {
        if (index < 0 || index >= _profiles.Count) return;
        _selected = index;
        var p = _profiles[index];
        _triggerEditor.SetRules(p.Triggers);
        ProfileName.Text = p.Name; ShellCommand.Text = p.Command; StartingDirectory.Text = p.Directory;
        FontFamilyBox.Text = p.FontFamily; FontSizeBox.Value = (decimal)p.FontSize; HistorySizeBox.Value = p.HistorySize;
        CursorBox.SelectedIndex = Array.IndexOf(CursorShapes, p.CursorShape); ForegroundBox.Text = p.Foreground; BackgroundBox.Text = p.Background;
        DefaultProfileBox.IsChecked = p.Id == _defaultId; ShellIntegrationBox.IsChecked = p.ShellIntegration;
        EnvironmentBox.Text = string.Join('\n', p.EnvironmentVariables.Select(p => p.Key + "=" + p.Value));
    }
    private void CommitProfile()
    {
        if (_selected < 0) return;
        var current = _profiles[_selected];
        var environment = ParsePairs(EnvironmentBox.Text);
        _profiles[_selected] = current with
        {
            Name = ProfileName.Text?.Trim() ?? "", Command = ShellCommand.Text?.Trim() ?? "",
            Directory = StartingDirectory.Text?.Trim() ?? "", FontFamily = FontFamilyBox.Text?.Trim() ?? "",
            FontSize = (double)(FontSizeBox.Value ?? 12), HistorySize = (int)(HistorySizeBox.Value ?? 10000),
            CursorShape = CursorShapes[Math.Clamp(CursorBox.SelectedIndex, 0, CursorShapes.Length - 1)],
            Foreground = EmptyToNull(ForegroundBox.Text), Background = EmptyToNull(BackgroundBox.Text),
            ShellIntegration = ShellIntegrationBox.IsChecked == true, EnvironmentVariables = environment, Triggers = _triggerEditor.GetRules(),
        };
        if (DefaultProfileBox.IsChecked == true) _defaultId = current.Id;
    }
    private static string? EmptyToNull(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static Dictionary<string, string> ParsePairs(string? text)
    {
        var result = new Dictionary<string, string>();
        foreach (var line in (text ?? "").Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var equal = line.IndexOf('=');
            if (equal < 1) throw new ArgumentException("Use NAME=value on each line.");
            if (!result.TryAdd(line[..equal].Trim(), line[(equal + 1)..].Trim())) throw new ArgumentException("Duplicate names are not allowed.");
        }
        return result;
    }
    private void ProfileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        try { CommitProfile(); LoadProfile(ProfileList.SelectedIndex); ErrorText.Text = ""; }
        catch (ArgumentException error) { ErrorText.Text = error.Message; _loading = true; ProfileList.SelectedIndex = _selected; _loading = false; }
    }
    private void AddClicked(object? sender, RoutedEventArgs e) => Change(() => { CommitProfile(); _profiles.Add(TerminalProfile.Default() with { Name = "New profile" }); ReloadProfiles(_profiles.Count - 1); });
    private void DuplicateClicked(object? sender, RoutedEventArgs e) => Change(() => { CommitProfile(); _profiles.Add(_profiles[_selected] with { Id = Guid.NewGuid().ToString("N"), Name = _profiles[_selected].Name + " copy" }); ReloadProfiles(_profiles.Count - 1); });
    private void RemoveClicked(object? sender, RoutedEventArgs e) => Change(() =>
    {
        if (_profiles.Count == 1) throw new ArgumentException("Keep at least one profile.");
        _profiles.RemoveAt(_selected);
        if (!_profiles.Any(p => p.Id == _defaultId)) _defaultId = _profiles[0].Id;
        ReloadProfiles(0);
    });
    private void Change(Action action) { try { action(); ErrorText.Text = ""; } catch (Exception error) { ErrorText.Text = error.Message; } }
    private void ResetShortcuts(bool defaults)
    {
        var bindings = ShortcutMap.Defaults();
        if (!defaults) foreach (var pair in _store.Settings.Shortcuts) bindings[pair.Key] = pair.Value;
        ShortcutsBox.Text = string.Join('\n', bindings.Select(p => p.Key + "=" + p.Value));
    }
    private void ResetShortcutsClicked(object? sender, RoutedEventArgs e) => ResetShortcuts(true);
    private void ReferenceAppearanceClicked(object? sender, RoutedEventArgs e)
    {
        FontFamilyBox.Text = OperatingSystem.IsMacOS() ? "Monaco" : "Cascadia Mono";
        FontSizeBox.Value = 12;
        ForegroundBox.Text = null; BackgroundBox.Text = null;
        CursorBox.SelectedIndex = 0;
    }
    private void CancelClicked(object? sender, RoutedEventArgs e) => Close();
    private void SaveClicked(object? sender, RoutedEventArgs e) => Change(() =>
    {
        CommitProfile();
        var shortcuts = ParsePairs(ShortcutsBox.Text);
        ShortcutMap.Resolve(shortcuts);
        var settings = _store.Settings with
        {
            Profiles = _profiles.ToList(), DefaultProfileId = _defaultId, Theme = ThemeBox.SelectedItem as string ?? "Dark",
            AlwaysShowTabBar = AlwaysShowTabBarBox.IsChecked == true, ShowStatusBar = ShowStatusBarBox.IsChecked == true,
            ScreenReplayEnabled = ReplayEnabledBox.IsChecked == true,
            ScriptingEnabled = ScriptingEnabledBox.IsChecked == true,
            ScriptingPort = (int)(ScriptingPortBox.Value ?? 1912),
            HotkeyWindowEnabled = HotkeyWindowBox.IsChecked == true,
            KeepSessionsAlive = KeepSessionsAliveBox.IsChecked == true, RestoreLayout = RestoreLayoutBox.IsChecked == true, CopyOnSelect = CopyOnSelectBox.IsChecked == true,
            DoubleClickSmartSelection = SmartSelectionBox.IsChecked == true,
            ConfirmClose = ConfirmCloseBox.IsChecked == true, Shortcuts = shortcuts,
        };
        _store.Save(settings);
        Result = settings;
        Close();
    });
}
