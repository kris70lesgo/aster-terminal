using Avalonia;
using Avalonia.Controls;
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
            CaptureChrome(window, "single-dark");
            for (var i = 2; i <= 3; i++)
            {
                Click(VisualButton(window, "NewTabButton"));
                await Until(() => tabs.ItemCount == i && Panes(window).Single().Terminal.IsRunning);
                all.Add(Panes(window).Single());
            }
            Assert.DoesNotContain("single", tabs.Classes);
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
}
