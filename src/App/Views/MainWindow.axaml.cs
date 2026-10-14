using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Aster.App.Services;
using Aster.Sessions;
using Aster.Sessions.Persistence;
using Devolutions.Terminal.Core;
using Devolutions.Terminal.Settings;
using AppSettings = Aster.Sessions.Persistence.AppSettings;

namespace Aster.App.Views;

public partial class MainWindow : Window
{
    private sealed class Workspace
    {
        public TabItem Tab { get; } = new();
        public Grid Root { get; } = new();
        public TextBlock Label { get; } = new() { MaxWidth = 170, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        public List<TerminalPane> Panes { get; } = [];
        public TerminalPane? Active { get; set; }
        public string? CustomTitle { get; set; }
        public (TerminalPane Pane, Grid Parent, int Row, int Column)? Zoom { get; set; }
    }
    private readonly List<Workspace> _workspaces = [];
    private readonly SemaphoreSlim _layoutMutation = new(1, 1);
    private readonly SettingsStore? _store;
    private AppSettings _settings;
    private Dictionary<string, KeyGesture> _shortcuts = [];
    private bool _closing, _closed;
    private PreferencesWindow? _preferences;
    private Workspace? Current => _workspaces.FirstOrDefault(w => w.Tab == Tabs.SelectedItem);
    private TerminalPane? Active => Current?.Active;
    private bool Light => ActualThemeVariant == ThemeVariant.Light;
    private TerminalProfile DefaultProfile => _settings.Profiles.FirstOrDefault(p => p.Id == _settings.DefaultProfileId) ?? _settings.Profiles[0];

    public MainWindow() : this(new SettingsStore()) { }
    public MainWindow(SettingsStore store) : this(store.Settings, store) { }
    public MainWindow(TerminalProfile profile) : this(new AppSettings
    {
        Profiles = [profile], DefaultProfileId = profile.Id, RestoreLayout = false, ConfirmClose = false
    }, null) { }
    private MainWindow(AppSettings settings, SettingsStore? store)
    {
        _settings = settings; _store = store;
        InitializeComponent();
        MainMenu.IsVisible = !OperatingSystem.IsMacOS();
        Width = Math.Clamp(settings.WindowWidth, 620, 2400); Height = Math.Clamp(settings.WindowHeight, 320, 1800);
        ApplySettings(settings);
        Opened += (_, _) => Observe(ChangeLayoutAsync(InitializeSessionsAsync));
        Closing += OnClosing;
        ActualThemeVariantChanged += (_, _) => ApplyPaneAppearance();
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);
        Activated += (_, _) => UpdateApplicationMenu();
        Closed += (_, _) =>
        {
            if (!OperatingSystem.IsMacOS() || Application.Current is not { } app) return;
            var other = (app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.OfType<MainWindow>().LastOrDefault(w => w != this && !w._closing);
            if (other is not null) other.UpdateApplicationMenu();
            else NativeMenu.SetMenu(app, null);
        };
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        try { _shortcuts = ShortcutMap.Resolve(settings.Shortcuts); }
        catch (ArgumentException error) { _shortcuts = ShortcutMap.Resolve(new Dictionary<string, string>()); ShowError(error.Message); }
        RequestedThemeVariant = settings.Theme switch { "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark, _ => ThemeVariant.Default };
        ProfilesMenu.Items.Clear();
        foreach (var profile in settings.Profiles)
        {
            var item = new MenuItem { Header = profile.Name };
            item.Click += (_, _) => Observe(NewTabAsync(profile));
            ProfilesMenu.Items.Add(item);
        }
        ApplyPaneAppearance();
        RefreshChrome();
        BuildNativeMenu();
    }
    private void ApplyPaneAppearance()
    {
        foreach (var pane in _workspaces.SelectMany(w => w.Panes))
        {
            pane.ApplyAppearance(_settings.Profiles.FirstOrDefault(p => p.Id == pane.Profile.Id) ?? pane.Profile, Light, _settings.CopyOnSelect);
            pane.SetReplayEnabled(_settings.ScreenReplayEnabled);
        }
    }

    private async Task InitializeSessionsAsync()
    {
        if (_settings.RestoreLayout && _settings.Layout.Count > 0)
        {
            foreach (var saved in _settings.Layout)
            {
                var workspace = AddWorkspace(saved.Title);
                workspace.Root.Children.Add(RestoreNode(workspace, saved.Root));
                Activate(workspace, workspace.Panes[0]);
                Tabs.SelectedItem = workspace.Tab;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
                foreach (var pane in workspace.Panes) await pane.StartAsync();
            }
            Tabs.SelectedIndex = 0;
        }
        else await NewTabCoreAsync(DefaultProfile);
        if (_store?.LoadWarning is { } warning) ShowError(warning);
    }

    private Workspace AddWorkspace(string? title = null)
    {
        var workspace = new Workspace { CustomTitle = title };
        var header = new DockPanel { LastChildFill = true };
        var close = new Button { Content = "×", MinHeight = 18, MinWidth = 18, Padding = new Thickness(2, 0), Background = Avalonia.Media.Brushes.Transparent };
        ToolTip.SetTip(close, "Close tab");
        Avalonia.Automation.AutomationProperties.SetName(close, "Close tab");
        close.Click += (_, e) => { e.Handled = true; Observe(ChangeLayoutAsync(() => CloseTabCoreAsync(workspace))); };
        DockPanel.SetDock(close, Dock.Right); header.Children.Add(close); header.Children.Add(workspace.Label);
        workspace.Tab.Header = header;
        workspace.Tab.Content = workspace.Root;
        _workspaces.Add(workspace); Tabs.Items.Add(workspace.Tab);
        RefreshChrome();
        return workspace;
    }

    private TerminalPane CreatePane(Workspace workspace, TerminalProfile profile)
    {
        var pane = new TerminalPane(profile, Light, _settings.CopyOnSelect, _settings.ScreenReplayEnabled);
        workspace.Panes.Add(pane);
        pane.Activated += (_, _) => Activate(workspace, pane);
        pane.TitleChanged += (_, _) => { if (workspace.Active == pane) UpdateTitle(workspace); };
        void UpdateDimensions()
        {
            if (Current == workspace && workspace.Active == pane)
                WindowStatus.Text = $"{pane.Profile.Name} · {workspace.Panes.Count} pane(s) · {pane.Terminal.Engine.Columns} × {pane.Terminal.Engine.Rows}";
        }
        pane.Terminal.ViewportChanged += (_, _) => UpdateDimensions();
        pane.Terminal.SizeChanged += (_, _) => Dispatcher.UIThread.Post(UpdateDimensions, DispatcherPriority.Background);
        return pane;
    }
    private void Activate(Workspace workspace, TerminalPane pane)
    {
        workspace.Active = pane;
        foreach (var candidate in workspace.Panes) candidate.SetActive(candidate == pane);
        UpdateTitle(workspace);
        WindowStatus.Text = $"{pane.Profile.Name} · {workspace.Panes.Count} pane(s) · {pane.Terminal.Engine.Columns} × {pane.Terminal.Engine.Rows}";
        if (SearchBar.IsVisible) { pane.Terminal.Search.Clear(); pane.Terminal.SetSearchHighlights([]); SearchStatus.Text = "Press Enter to search this pane"; }
    }
    private void UpdateTitle(Workspace workspace)
    {
        workspace.Label.Text = workspace.CustomTitle ?? workspace.Active?.SessionTitle ?? "Terminal";
        Avalonia.Automation.AutomationProperties.SetName(workspace.Tab, workspace.Label.Text);
        if (Current == workspace) Title = workspace.Label.Text;
    }

    private Task NewTabAsync(TerminalProfile? profile = null) => ChangeLayoutAsync(() => NewTabCoreAsync(profile ?? DefaultProfile));
    private Task SplitAsync(bool below) => ChangeLayoutAsync(() => SplitCoreAsync(below));
    private Task ClosePaneAsync() => ChangeLayoutAsync(ClosePaneCoreAsync);
    private async Task ChangeLayoutAsync(Func<Task> change)
    {
        await _layoutMutation.WaitAsync();
        try { if (!_closing) await change(); }
        finally { _layoutMutation.Release(); }
    }
    private async Task NewTabCoreAsync(TerminalProfile profile)
    {
        var workspace = AddWorkspace();
        var pane = CreatePane(workspace, profile);
        workspace.Root.Children.Add(pane); Tabs.SelectedItem = workspace.Tab;
        Activate(workspace, pane);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await pane.StartAsync();
    }
    private static Grid SplitGrid(bool below, double ratio = .5)
    {
        var split = new Grid();
        var definitions = FormattableString.Invariant($"{ratio}*,1,{1 - ratio}*");
        if (below) split.RowDefinitions = new RowDefinitions(definitions);
        else split.ColumnDefinitions = new ColumnDefinitions(definitions);
        var divider = new GridSplitter
        {
            Background = Avalonia.Media.Brush.Parse("#555761"),
            ResizeDirection = below ? GridResizeDirection.Rows : GridResizeDirection.Columns,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        // Keep the visible line thin while offering a wider pointer target.
        divider.Template = new Avalonia.Controls.Templates.FuncControlTemplate<GridSplitter>((_, _) => new Border
        {
            Background = Avalonia.Media.Brushes.Transparent,
            Child = new Border
            {
                Background = Avalonia.Media.Brush.Parse("#555761"),
                Width = below ? double.NaN : 1, Height = below ? 1 : double.NaN,
                HorizontalAlignment = below ? Avalonia.Layout.HorizontalAlignment.Stretch : Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = below ? Avalonia.Layout.VerticalAlignment.Center : Avalonia.Layout.VerticalAlignment.Stretch
            }
        });
        if (below) { divider.Height = 7; divider.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center; }
        else { divider.Width = 7; divider.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center; }
        if (below) Grid.SetRow(divider, 1); else Grid.SetColumn(divider, 1);
        split.Children.Add(divider);
        return split;
    }
    private async Task SplitCoreAsync(bool below)
    {
        if (Current is not { } workspace || Active is not { } old) return;
        Unzoom(workspace);
        if (old.Parent is not Grid parent) return;
        var pane = CreatePane(workspace, old.Profile);
        var split = SplitGrid(below);
        var row = Grid.GetRow(old); var col = Grid.GetColumn(old);
        parent.Children.Remove(old); Grid.SetRow(split, row); Grid.SetColumn(split, col); parent.Children.Add(split);
        Grid.SetRow(old, 0); Grid.SetColumn(old, 0); split.Children.Add(old);
        if (below) Grid.SetRow(pane, 2); else Grid.SetColumn(pane, 2);
        split.Children.Add(pane);
        Activate(workspace, pane);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await pane.StartAsync();
    }
    private async Task<bool> ConfirmCloseAsync(IEnumerable<TerminalPane> panes)
    {
        var count = panes.Count(p => p.Terminal.IsRunning);
        return !_settings.ConfirmClose || count == 0 ||
            await DialogWindow.Confirm(this, "Close running sessions", $"Close {count} running shell session(s)? Their processes will end.");
    }
    private async Task ClosePaneCoreAsync()
    {
        if (Current is not { } workspace || Active is not { } pane || !await ConfirmCloseAsync([pane])) return;
        Unzoom(workspace);
        await pane.CloseAsync(); workspace.Panes.Remove(pane);
        if (workspace.Panes.Count == 0) { RemoveWorkspace(workspace); return; }
        if (pane.Parent is Grid split && split.Parent is Grid parent)
        {
            var remaining = split.Children.First(c => c != pane && c is not GridSplitter);
            var row = Grid.GetRow(split); var column = Grid.GetColumn(split);
            split.Children.Remove(remaining); parent.Children.Remove(split);
            Grid.SetRow(remaining, row); Grid.SetColumn(remaining, column); parent.Children.Add(remaining);
        }
        Activate(workspace, workspace.Panes[^1]); workspace.Active?.Terminal.Focus();
    }
    private async Task CloseTabCoreAsync(Workspace workspace)
    {
        if (!await ConfirmCloseAsync(workspace.Panes)) return;
        Unzoom(workspace);
        foreach (var pane in workspace.Panes) await pane.CloseAsync();
        RemoveWorkspace(workspace);
    }
    private void RemoveWorkspace(Workspace workspace)
    {
        _workspaces.Remove(workspace); Tabs.Items.Remove(workspace.Tab);
        RefreshChrome();
        if (_workspaces.Count == 0) { Close(); return; }
        Tabs.SelectedItem = _workspaces[^1].Tab; Current?.Active?.Terminal.Focus();
    }

    public List<TabLayout> CaptureLayout()
    {
        foreach (var w in _workspaces) Unzoom(w);
        return _workspaces.Select(w => new TabLayout(w.CustomTitle, CaptureNode(w.Root.Children[0]))).ToList();
    }
    private static PaneLayout CaptureNode(Control control)
    {
        if (control is TerminalPane pane) return new() { ProfileId = pane.Profile.Id };
        var grid = (Grid)control;
        var below = grid.RowDefinitions.Count > 0;
        var children = grid.Children.Where(c => c is not GridSplitter).OrderBy(c => below ? Grid.GetRow(c) : Grid.GetColumn(c)).ToArray();
        var first = below ? grid.RowDefinitions[0].Height.Value : grid.ColumnDefinitions[0].Width.Value;
        var last = below ? grid.RowDefinitions[2].Height.Value : grid.ColumnDefinitions[2].Width.Value;
        return new() { Direction = below ? "Rows" : "Columns", Ratio = Math.Clamp(first / Math.Max(.001, first + last), .05, .95), Children = children.Select(CaptureNode).ToList() };
    }
    private Control RestoreNode(Workspace workspace, PaneLayout node)
    {
        if (node.Children.Count == 0)
            return CreatePane(workspace, _settings.Profiles.FirstOrDefault(p => p.Id == node.ProfileId) ?? DefaultProfile);
        var below = node.Direction == "Rows"; var grid = SplitGrid(below, node.Ratio);
        var first = RestoreNode(workspace, node.Children[0]); var second = RestoreNode(workspace, node.Children[1]);
        if (below) Grid.SetRow(second, 2); else Grid.SetColumn(second, 2);
        grid.Children.Add(first); grid.Children.Add(second);
        return grid;
    }

    private void ZoomPane()
    {
        if (Current is not { } workspace || Active is not { } pane) return;
        if (workspace.Zoom is not null) { Unzoom(workspace); pane.Terminal.Focus(); return; }
        if (pane.Parent is not Grid parent || parent == workspace.Root) return;
        workspace.Zoom = (pane, parent, Grid.GetRow(pane), Grid.GetColumn(pane));
        parent.Children.Remove(pane); workspace.Root.Children[0].IsVisible = false;
        Grid.SetRow(pane, 0); Grid.SetColumn(pane, 0); workspace.Root.Children.Add(pane);
        pane.Terminal.Focus();
    }
    private static void Unzoom(Workspace workspace)
    {
        if (workspace.Zoom is not { } zoom) return;
        workspace.Root.Children.Remove(zoom.Pane); workspace.Root.Children[0].IsVisible = true;
        Grid.SetRow(zoom.Pane, zoom.Row); Grid.SetColumn(zoom.Pane, zoom.Column); zoom.Parent.Children.Add(zoom.Pane);
        workspace.Zoom = null;
    }
    private void CyclePane(int step)
    {
        if (Current is not { } w || w.Active is null) return;
        Unzoom(w);
        var index = (w.Panes.IndexOf(w.Active) + step + w.Panes.Count) % w.Panes.Count;
        Activate(w, w.Panes[index]); w.Active!.Terminal.Focus();
    }
    private void CycleTab(int step) { if (_workspaces.Count > 0) Tabs.SelectedIndex = (Tabs.SelectedIndex + step + _workspaces.Count) % _workspaces.Count; }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closed) return;
        e.Cancel = true; if (_closing) return;
        _closing = true;
        if (!await ConfirmCloseAsync(_workspaces.SelectMany(w => w.Panes))) { _closing = false; return; }
        await _layoutMutation.WaitAsync();
        try
        {
            if (_store is not null && _store.LoadWarning is null)
                try { _store.Save(_store.Settings with { Layout = CaptureLayout(), WindowWidth = Width, WindowHeight = Height }); }
                catch (Exception error) { Console.Error.WriteLine("Settings save failed: " + error.Message); }
            foreach (var pane in _workspaces.SelectMany(w => w.Panes).ToArray()) await pane.CloseAsync();
            _closed = true; Close();
        }
        catch (Exception error) { _closing = false; ShowError($"Session cleanup failed: {error.Message}"); }
        finally { _layoutMutation.Release(); }
    }

    private void OnShortcut(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && SearchBar.IsVisible) { HideSearch(); e.Handled = true; return; }
        var action = _shortcuts.FirstOrDefault(pair => pair.Value.Matches(e)).Key;
        if (action is null) return;
        e.Handled = true;
        switch (action)
        {
            case "NewTab": Observe(NewTabAsync()); break;
            case "NewWindow": NewWindow(); break;
            case "ClosePane": Observe(ClosePaneAsync()); break;
            case "SplitRight": Observe(SplitAsync(false)); break;
            case "SplitBelow": Observe(SplitAsync(true)); break;
            case "Find": ShowSearch(); break;
            case "Copy": if (Active is { } copy) Observe(copy.Terminal.CopyAsync()); break;
            case "Paste": if (Active is { } paste) Observe(paste.Terminal.PasteAsync()); break;
            case "Preferences": Observe(PreferencesAsync()); break;
            case "NextTab": CycleTab(1); break;
            case "PreviousTab": CycleTab(-1); break;
            case "NextPane": CyclePane(1); break;
            case "PreviousPane": CyclePane(-1); break;
            case "ZoomPane": ZoomPane(); break;
            case "LargerFont": Active?.Terminal.AdjustFontSize(1); break;
            case "SmallerFont": Active?.Terminal.AdjustFontSize(-1); break;
            case "ResetFont": Active?.Terminal.ResetFontSize(); break;
            case "PreviousMark": Active?.Terminal.ScrollToMark(ScrollToMarkDirection.Previous); break;
            case "NextMark": Active?.Terminal.ScrollToMark(ScrollToMarkDirection.Next); break;
            case "OpenQuickly": Observe(OpenQuicklyAsync()); break;
            case "Replay": ReplayClicked(null, new RoutedEventArgs()); break;
        }
    }

    private void NewWindow()
    {
        var window = _store is null ? new MainWindow(DefaultProfile) : new MainWindow(_store);
        // New windows start a fresh tab, not a duplicate of the saved startup layout.
        window._settings = window._settings with { Layout = [] };
        window.Show();
    }
    private async Task PreferencesAsync()
    {
        if (_store is null) { ShowError("Preferences are unavailable in an isolated test session."); return; }
        if (_preferences is not null) { _preferences.Activate(); return; }
        _preferences = new PreferencesWindow(_store) { RequestedThemeVariant = RequestedThemeVariant };
        try
        {
            await _preferences.ShowDialog(this);
            if (_preferences.Result is { } settings)
            {
                ApplySettings(settings);
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                    foreach (var window in lifetime.Windows.OfType<MainWindow>().Where(w => w != this && w._store?.Path == _store.Path)) window.ApplySettings(settings);
            }
        }
        finally { _preferences = null; Active?.Terminal.Focus(); }
    }
    private async Task OpenQuicklyAsync()
    {
        var choices = _workspaces.SelectMany(w => w.Panes.Select(p => (Caption: $"{p.SessionTitle} · {p.Profile.Name} · {p.Terminal.Engine.WorkingDirectory}", Workspace: w, Pane: p))).ToList();
        var index = await DialogWindow.Choose(this, "Open quickly", choices.Select(c => c.Caption).ToArray());
        if (index is >= 0)
        {
            var choice = choices[index.Value]; Tabs.SelectedItem = choice.Workspace.Tab; Unzoom(choice.Workspace);
            Activate(choice.Workspace, choice.Pane); choice.Pane.Terminal.Focus();
        }
    }
    private async Task RenameAsync()
    {
        if (Current is not { } workspace) return;
        var title = await DialogWindow.Prompt(this, "Rename tab", workspace.Label.Text ?? "");
        if (!string.IsNullOrWhiteSpace(title)) { workspace.CustomTitle = title[..Math.Min(80, title.Length)]; UpdateTitle(workspace); }
    }
    private async Task SaveBufferAsync()
    {
        if (Active is not { } pane) return;
        var file = await StorageProvider.SaveFilePickerAsync(new() { Title = "Save scrollback", SuggestedFileName = "terminal.txt", DefaultExtension = "txt" });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync(); using var writer = new StreamWriter(stream);
        await writer.WriteAsync(TerminalBufferExport.ToPlainText(pane.Terminal.Engine.CreateSnapshot(true).Buffer));
    }
    private void SetTheme(string theme)
    {
        var settings = _settings with { Theme = theme };
        if (_store is not null) _store.Save(settings);
        ApplySettings(settings);
    }
    private async void Observe(Task task) { try { await task; } catch (Exception error) { ShowError(error.Message); } }
    private void ShowSearch() { SearchBar.IsVisible = true; SearchBox.Focus(); }
    private void HideSearch() { SearchBar.IsVisible = false; Active?.Terminal.Search.Clear(); Active?.Terminal.SetSearchHighlights([]); Active?.Terminal.Focus(); }
    private void Search(bool previous)
    {
        if (Active is not { } pane) return;
        if (string.IsNullOrWhiteSpace(SearchBox.Text)) { pane.Terminal.Search.Clear(); pane.Terminal.SetSearchHighlights([]); SearchStatus.Text = ""; return; }
        var found = pane.Terminal.Find(SearchBox.Text, previous);
        SearchStatus.Text = found ? $"{pane.Terminal.Search.CurrentIndex + 1} / {pane.Terminal.Search.Matches.Count}" : "No matches";
    }
    private void TabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Current is { Active: { } pane } workspace)
            Dispatcher.UIThread.Post(() => { if (Current != workspace || _closing) return; Activate(workspace, pane); pane.Terminal.Focus(); });
    }
    private void SearchKeyDown(object? sender, KeyEventArgs e) { if (e.Key == Key.Enter) { Search(e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; } }
    private void NewTabClicked(object? sender, RoutedEventArgs e) => Observe(NewTabAsync());
    private void NewWindowClicked(object? sender, RoutedEventArgs e) => NewWindow();
    private void SplitRightClicked(object? sender, RoutedEventArgs e) => Observe(SplitAsync(false));
    private void SplitBelowClicked(object? sender, RoutedEventArgs e) => Observe(SplitAsync(true));
    private void ClosePaneClicked(object? sender, RoutedEventArgs e) => Observe(ClosePaneAsync());
    private void CloseTabClicked(object? sender, RoutedEventArgs e) { if (Current is { } w) Observe(ChangeLayoutAsync(() => CloseTabCoreAsync(w))); }
    private void FindClicked(object? sender, RoutedEventArgs e) => ShowSearch();
    private void CopyClicked(object? sender, RoutedEventArgs e) { if (Active is { } pane) Observe(pane.Terminal.CopyAsync()); }
    private void PasteClicked(object? sender, RoutedEventArgs e) { if (Active is { } pane) Observe(pane.Terminal.PasteAsync()); }
    private void SelectAllClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.SelectAll();
    private void ClearBufferClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.ClearBuffer();
    private void PreferencesClicked(object? sender, RoutedEventArgs e) => Observe(PreferencesAsync());
    private void PreviousClicked(object? sender, RoutedEventArgs e) => Search(true);
    private void NextClicked(object? sender, RoutedEventArgs e) => Search(false);
    private void DoneClicked(object? sender, RoutedEventArgs e) => HideSearch();
    private void ZoomPaneClicked(object? sender, RoutedEventArgs e) => ZoomPane();
    private void NextPaneClicked(object? sender, RoutedEventArgs e) => CyclePane(1);
    private void NextTabClicked(object? sender, RoutedEventArgs e) => CycleTab(1);
    private void RenameClicked(object? sender, RoutedEventArgs e) => Observe(RenameAsync());
    private void OpenQuicklyClicked(object? sender, RoutedEventArgs e) => Observe(OpenQuicklyAsync());
    private void AddMarkClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.AddMark();
    private void PreviousMarkClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.ScrollToMark(ScrollToMarkDirection.Previous);
    private void NextMarkClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.ScrollToMark(ScrollToMarkDirection.Next);
    private void LargerFontClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.AdjustFontSize(1);
    private void SmallerFontClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.AdjustFontSize(-1);
    private void ResetFontClicked(object? sender, RoutedEventArgs e) => Active?.Terminal.ResetFontSize();
    private void FullScreenClicked(object? sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    private void DarkThemeClicked(object? sender, RoutedEventArgs e) { try { SetTheme("Dark"); } catch (Exception error) { ShowError(error.Message); } }
    private void LightThemeClicked(object? sender, RoutedEventArgs e) { try { SetTheme("Light"); } catch (Exception error) { ShowError(error.Message); } }
    private void SaveBufferClicked(object? sender, RoutedEventArgs e) => Observe(SaveBufferAsync());
    private void QuitClicked(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            foreach (var window in lifetime.Windows.OfType<MainWindow>().ToArray()) window.Close();
        else Close();
    }
    private void AboutClicked(object? sender, RoutedEventArgs e) =>
        Observe(DialogWindow.Confirm(this, "About Aster Terminal", "Aster Terminal 0.3.0\nIndependent iTerm2 migration to Avalonia.\nGPL-3.0-or-later. No warranty.\nTerminal component: Devolutions (MIT).\nSee LICENSE and bundled notices.", "Done"));

    private void ShowError(string message) { AlertText.Text = message; AlertBar.IsVisible = true; }
    private void DismissAlertClicked(object? sender, RoutedEventArgs e) => AlertBar.IsVisible = false;
    private void RefreshChrome()
    {
        Tabs.Classes.Set("single", !_settings.AlwaysShowTabBar && _workspaces.Count <= 1);
        StatusBar.IsVisible = _settings.ShowStatusBar;
        StatusBarMenuItem.IsChecked = _settings.ShowStatusBar;
        TabBarMenuItem.IsChecked = _settings.AlwaysShowTabBar;
    }
    private void SaveChrome(AppSettings settings)
    {
        _store?.Save(settings);
        _settings = settings; RefreshChrome(); BuildNativeMenu();
    }
    private void StatusBarClicked(object? sender, RoutedEventArgs e) { try { SaveChrome(_settings with { ShowStatusBar = !_settings.ShowStatusBar }); } catch (Exception error) { ShowError(error.Message); } }
    private void TabBarClicked(object? sender, RoutedEventArgs e) { try { SaveChrome(_settings with { AlwaysShowTabBar = !_settings.AlwaysShowTabBar }); } catch (Exception error) { ShowError(error.Message); } }

    private void BuildNativeMenu()
    {
        if (!OperatingSystem.IsMacOS()) return;
        NativeMenu.SetMenu(this, NativeMenuBridge.FromMenu(MainMenu, _shortcuts));
        UpdateApplicationMenu();
    }
    private void UpdateApplicationMenu()
    {
        if (!OperatingSystem.IsMacOS() || Application.Current is not { } app) return;
        var menu = new NativeMenu();
        var about = new NativeMenuItem("About Aster Terminal"); about.Click += (_, _) => AboutClicked(null, new RoutedEventArgs()); menu.Add(about);
        var preferences = new NativeMenuItem("Settings…") { Gesture = _shortcuts.GetValueOrDefault("Preferences") };
        preferences.Click += (_, _) => Observe(PreferencesAsync()); menu.Add(preferences);
        NativeMenu.SetMenu(app, menu);
    }
    private void ReplayClicked(object? sender, RoutedEventArgs e)
    {
        if (Active is { } pane) { pane.CaptureReplay(); Observe(new ReplayWindow(pane.Replay, pane.Profile).ShowDialog(this)); }
    }
}
