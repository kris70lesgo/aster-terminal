using Aster.Sessions;
using Aster.Sessions.Persistence;

namespace Aster.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "aster-settings-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void ProfileThemeEnvironmentAndLayoutRoundTrip()
    {
        var store = new SettingsStore(FilePath);
        var profile = store.Settings.Profiles[0] with { FontSize = 19, FontFamily = "monospace", EnvironmentVariables = new() { ["ASTER_TEST"] = "hello=world" } };
        var settings = store.Settings with
        {
            Profiles = [profile], DefaultProfileId = profile.Id, Theme = "Light",
            Layout = [new("Saved tab", new() { Direction = "Rows", Ratio = .3, Children = [new() { ProfileId = profile.Id }, new() { ProfileId = profile.Id }] })]
        };
        store.Save(settings);
        var loaded = new SettingsStore(FilePath).Settings;
        Assert.Equal("Light", loaded.Theme); Assert.Equal(19, loaded.Profiles[0].FontSize);
        Assert.Equal("hello=world", loaded.Profiles[0].EnvironmentVariables["ASTER_TEST"]);
        Assert.Equal(.3, loaded.Layout[0].Root.Ratio);
    }
    [Fact]
    public void DamagedSettingsRecoverBackupAndPreserveOriginal()
    {
        var store = new SettingsStore(FilePath); store.Save(store.Settings); store.Save(store.Settings with { Theme = "Light" });
        File.WriteAllText(FilePath, "broken JSON");
        var recovered = new SettingsStore(FilePath);
        Assert.Equal("Dark", recovered.Settings.Theme); Assert.NotNull(recovered.LoadWarning);
        Assert.Equal("broken JSON", File.ReadAllText(FilePath));
        recovered.Save(recovered.Settings);
        Assert.Single(Directory.GetFiles(_directory, "*.damaged-*"));
        Assert.Null(new SettingsStore(FilePath).LoadWarning);
    }
    [Fact]
    public void InvalidSettingsNeverReplaceSavedConfiguration()
    {
        var store = new SettingsStore(FilePath); store.Save(store.Settings);
        var before = File.ReadAllText(FilePath);
        Assert.Throws<ArgumentException>(() => store.Save(store.Settings with { Theme = "bad" }));
        Assert.Throws<ArgumentException>(() => store.Save(store.Settings with { Profiles = [store.Settings.Profiles[0] with { FontSize = double.NaN }] }));
        Assert.Equal(before, File.ReadAllText(FilePath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }
    [Fact]
    public void NullProfilesAndDeepLayoutsAreRejectedWithRecoverableErrors()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{\"version\":1,\"profiles\":[null]}");
        var store = new SettingsStore(FilePath); Assert.NotNull(store.LoadWarning);
        var node = new PaneLayout { ProfileId = store.Settings.DefaultProfileId };
        for (var i = 0; i < 15; i++) node = new() { Direction = "Columns", Children = [node, new() { ProfileId = store.Settings.DefaultProfileId }] };
        Assert.Throws<ArgumentException>(() => store.Save(store.Settings with { Layout = [new(null, node)] }));
    }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
