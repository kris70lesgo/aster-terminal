using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Aster.App.Views;
using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed partial class DesktopTests
{
    private static Button VisualButton(Control owner, string name) => owner.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void CaptureChrome(Window window, string name)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/ui-design-candidate"));
        Directory.CreateDirectory(directory);
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(Path.Combine(directory, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    [AvaloniaFact]
    public async Task CompactCaptionTabActionsAndMinimumWidthKeepRealSessionsAlive()
    {
        var window = new MainWindow(new TerminalProfile("Visual acceptance", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath()));
        var all = new List<TerminalPane>(); window.Show();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            var first = Panes(window).Single(); all.Add(first);
            Assert.Equal(WindowDecorations.BorderOnly, window.WindowDecorations);
            var tabs = window.FindControl<TerminalTabs>("Tabs")!;
            Assert.Contains("single", tabs.Classes);
            Assert.Equal(32, tabs.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "WindowHeader").Bounds.Height);
            Assert.False(VisualButton(window, "NewTabButton").IsEffectivelyVisible);
            Assert.EndsWith("1", tabs.WindowShortcut);
            CaptureChrome(window, "single-dark");
            for (var i = 2; i <= 3; i++)
            {
                if (i == 2) SendKey(window, Key.T, OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control | RawInputModifiers.Shift);
                else Click(VisualButton(window, "NewTabButton"));
                await Until(() => tabs.ItemCount == i && Panes(window).Single().Terminal.IsRunning);
                all.Add(Panes(window).Single());
            }
            Assert.DoesNotContain("single", tabs.Classes);
            Assert.True(VisualButton(window, "NewTabButton").IsEffectivelyVisible);
            var ids = all.Select(p => p.Terminal.ProcessMetadata!.SessionId).ToArray();
            Assert.Equal(3, ids.Distinct().Count());
            window.Width = 620; window.Height = 400;
            await Until(() => tabs.Items.Cast<TabItem>().All(t => t.Bounds.Width >= 100 && t.Bounds.Width < 200));
            var viewport = tabs.GetVisualDescendants().OfType<ScrollViewer>().Single(v => v.Name == "HeaderViewport");
            Assert.InRange(tabs.Items.Cast<TabItem>().Sum(t => t.Bounds.Width), viewport.Bounds.Width - 1, viewport.Bounds.Width + 1);
            CaptureChrome(window, "tabs-dark-compact");
            Click(VisualButton(window, "Caption2")); Assert.Equal(WindowState.Maximized, window.WindowState);
            Click(VisualButton(window, "Caption2")); Assert.Equal(WindowState.Normal, window.WindowState);
            Click(VisualButton(window, "Caption1")); Assert.Equal(WindowState.Minimized, window.WindowState);
            window.WindowState = WindowState.Normal;
            Assert.Equal(ids, all.Select(p => p.Terminal.ProcessMetadata!.SessionId));
            Assert.All(all, p => Assert.True(p.Terminal.IsRunning));
            window.RequestedThemeVariant = ThemeVariant.Light;
            await Until(() => all.All(p => p.Terminal.Engine.Scheme.Background == 0xFFFAFAFC));
            CaptureChrome(window, "tabs-light-compact");
            for (var i = 4; i <= 8; i++)
            {
                Click(VisualButton(window, "NewTabButton"));
                await Until(() => tabs.ItemCount == i && Panes(window).Single().Terminal.IsRunning);
                all.Add(Panes(window).Single());
            }
            var selected = (TabItem)tabs.SelectedItem!;
            await Until(() => selected.TranslatePoint(default, viewport) is { } point && point.X >= -1 && point.X + selected.Bounds.Width <= viewport.Bounds.Width + 1);
            Assert.True(viewport.Extent.Width > viewport.Viewport.Width);
            CaptureChrome(window, "tabs-overflow");
            Click(VisualButton(window, "Caption0"));
            await Until(() => !window.IsVisible && all.All(p => !p.Terminal.IsRunning));
        }
        finally { window.Close(); await Until(() => !window.IsVisible && all.All(p => !p.Terminal.IsRunning)); }
    }

    [AvaloniaFact]
    public void SettingsToolbarCategoriesAndProfileSegmentsKeepFieldsAccessibleAtMinimumSize()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-visual-settings-" + Guid.NewGuid().ToString("N"));
        PreferencesWindow? window = null;
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = new TerminalProfile("Default", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", directory);
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id });
            window = new PreferencesWindow(store) { Width = 760, Height = 540, RequestedThemeVariant = ThemeVariant.Dark }; window.Show();
            var categories = window.FindControl<TabControl>("SettingsTabs")!;
            Assert.Equal(4, categories.ItemCount);
            foreach (var name in new[] { "GeneralCategory", "AppearanceCategory", "ProfilesCategory", "KeysCategory" })
            {
                categories.SelectedItem = window.FindControl<TabItem>(name);
                window.UpdateLayout();
                Assert.True(((TabItem)categories.SelectedItem!).IsEffectivelyVisible);
                CaptureChrome(window, "settings-" + name);
            }
            categories.SelectedItem = window.FindControl<TabItem>("ProfilesCategory");
            var segments = window.FindControl<TabControl>("ProfileTabs")!;
            foreach (var (name, field) in new[] { ("ProfileColorsTab", "ForegroundBox"), ("ProfileTextTab", "FontFamilyBox") })
            {
                segments.SelectedItem = window.FindControl<TabItem>(name); window.UpdateLayout();
                var editor = window.FindControl<TextBox>(field)!;
                Assert.True(editor.IsEffectivelyVisible); Assert.True(editor.Focus());
                var position = editor.TranslatePoint(default, window)!.Value;
                Assert.InRange(position.Y + editor.Bounds.Height, 0, window.Bounds.Height - 45);
            }
            window.RequestedThemeVariant = ThemeVariant.Light; window.UpdateLayout(); CaptureChrome(window, "settings-light-text");
            Click(VisualButton(window, "Caption0")); Assert.False(window.IsVisible); Assert.Null(window.Result);
        }
        finally { window?.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [AvaloniaFact]
    public async Task NativeScrollbarFollowsRealHistoryThumbDragAndAlternateScreenWithoutChangingShell()
    {
        var window = new MainWindow(new TerminalProfile("Scrollback", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath()));
        TerminalPane? pane = null; window.Show();
        try
        {
            await Until(() => Panes(window).Length == 1 && Panes(window)[0].Terminal.IsRunning);
            pane = Panes(window).Single(); pane.Terminal.Focus();
            var id = pane.Terminal.ProcessMetadata!.SessionId;
            window.KeyTextInput(OperatingSystem.IsWindows() ? "for /L %i in (1,1,160) do @echo HISTORY_%i" : "i=1; while [ $i -le 160 ]; do printf 'HISTORY_%s\\n' \"$i\"; i=$((i+1)); done");
            SendKey(window, Key.Enter);
            var bar = pane.GetVisualDescendants().OfType<TerminalScrollBar>().Single();
            await Until(() => Text(pane).Contains("HISTORY_160") && bar.Maximum > 50);
            Assert.Equal(TerminalScrollBar.GutterWidth, bar.Bounds.Width);
            Assert.Equal(pane.Terminal.Engine.HistoryCount, bar.Maximum);
            Assert.Equal(pane.Terminal.Engine.Rows, bar.ViewportSize);
            var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
            var point = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseMove(point - new Vector(0, 90)); window.MouseUp(point - new Vector(0, 90), MouseButton.Left);
            await Until(() => pane.Terminal.Engine.ScrollOffset > 0);
            Assert.Equal((int)Math.Round(bar.Maximum - bar.Value), pane.Terminal.Engine.ScrollOffset);
            bar.Value = 0;
            Assert.Equal(pane.Terminal.Engine.HistoryCount, pane.Terminal.Engine.ScrollOffset);
            pane.Terminal.ScrollToBottom();
            Assert.Equal(bar.Maximum, bar.Value);
            Assert.Equal(id, pane.Terminal.ProcessMetadata.SessionId);
            Assert.True(pane.Terminal.IsRunning);
            CaptureChrome(window, "scrollback-bottom");
            // Alternate screen is a renderer/engine regression; shell identity stays live.
            pane.Terminal.Engine.Feed("\u001b[?1049h");
            await Until(() => !bar.IsEnabled && bar.Maximum == 0);
            pane.Terminal.Engine.Feed("\u001b[?1049l");
            await Until(() => bar.IsEnabled && bar.Maximum > 50);
            Assert.Equal(id, pane.Terminal.ProcessMetadata.SessionId);
        }
        finally { window.Close(); await Until(() => !window.IsVisible && (pane is null || !pane.Terminal.IsRunning)); }
    }

    [AvaloniaFact]
    public void ReferenceAppearancePresetIsTransactionalAndFontPointsMatchLogicalCellMetrics()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aster-reference-preset-" + Guid.NewGuid().ToString("N"));
        PreferencesWindow? dialog = null;
        try
        {
            var store = new SettingsStore(Path.Combine(directory, "settings.json"));
            var profile = store.Settings.Profiles[0] with { FontSize = 20, FontFamily = "monospace", Foreground = "#EEEEEE", EnvironmentVariables = new() { ["KEEP_ME"] = "preserved" } };
            store.Save(store.Settings with { Profiles = [profile], DefaultProfileId = profile.Id });
            void Preset()
            {
                dialog = new PreferencesWindow(store); dialog.Show();
                dialog.FindControl<TabControl>("ProfileTabs")!.SelectedItem = dialog.FindControl<TabItem>("ProfileTextTab");
                dialog.UpdateLayout(); Click(VisualButton(dialog, "ReferenceAppearanceButton"));
                Assert.Equal(12, dialog.FindControl<NumericUpDown>("FontSizeBox")!.Value);
                Assert.Equal(20, store.Settings.Profiles[0].FontSize);
            }
            Preset(); dialog!.Close();
            Assert.Equal(20, new SettingsStore(store.Path).Settings.Profiles[0].FontSize);
            Preset(); Click(dialog!.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Save preferences")));
            var saved = new SettingsStore(store.Path).Settings.Profiles.Single();
            Assert.Equal(12, saved.FontSize); Assert.Null(saved.Foreground); Assert.Null(saved.Background);
            Assert.Equal(profile.Command, saved.Command); Assert.Equal("preserved", saved.EnvironmentVariables["KEEP_ME"]);
            Assert.Equal(OperatingSystem.IsMacOS() ? "Monaco" : "Cascadia Mono", saved.FontFamily);
            Assert.Equal(9, saved.ToControlProfile().FontSize);
            Assert.Equal(0xFFA7ABF2u, saved.ResolveColors(false).Table[12]);
            Assert.Equal(0xFFFFFFFFu, saved.ResolveColors(false).Cursor);
            Assert.Equal(0xFFB3D7FFu, saved.ResolveColors(false).SelectionBackground);
            var small = Aster.Terminal.TermControl.MeasureCell(saved.ToControlProfile());
            var large = Aster.Terminal.TermControl.MeasureCell((saved with { FontSize = 24 }).ToControlProfile());
            Assert.InRange(large.Width / small.Width, 1.9, 2.1);
            Assert.InRange(small.Height, 10, 20);
        }
        finally { dialog?.Close(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [AvaloniaFact]
    public async Task CaptionWindowNumbersNavigateAndRenumberRemainingLiveWindows()
    {
        var profile = new TerminalProfile("Window navigation", OperatingSystem.IsWindows() ? "cmd.exe /Q" : "exec /bin/sh", Path.GetTempPath());
        var first = new MainWindow(profile); var second = new MainWindow(profile);
        var panes = new List<TerminalPane>(); first.Show(); second.Show();
        try
        {
            await Until(() => Panes(first).Length == 1 && Panes(second).Length == 1 && Panes(first)[0].Terminal.IsRunning && Panes(second)[0].Terminal.IsRunning);
            panes.AddRange(Panes(first)); panes.AddRange(Panes(second));
            Assert.EndsWith("1", first.FindControl<TerminalTabs>("Tabs")!.WindowShortcut);
            Assert.EndsWith("2", second.FindControl<TerminalTabs>("Tabs")!.WindowShortcut);
            second.Hide();
            Assert.EndsWith("1", first.FindControl<TerminalTabs>("Tabs")!.WindowShortcut);
            second.Show(); second.Hide(); second.Show();
            Assert.EndsWith("2", second.FindControl<TerminalTabs>("Tabs")!.WindowShortcut);
            var modifiers = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta | RawInputModifiers.Alt : RawInputModifiers.Alt;
            SendKey(second, Key.D1, modifiers);
            Assert.True(first.IsActive);
            Assert.All(panes, p => Assert.True(p.Terminal.IsRunning));
            first.Close(); await Until(() => !first.IsVisible);
            Assert.EndsWith("1", second.FindControl<TerminalTabs>("Tabs")!.WindowShortcut);
        }
        finally { first.Close(); second.Close(); await Until(() => !first.IsVisible && !second.IsVisible && panes.All(p => !p.Terminal.IsRunning)); }
    }

}
