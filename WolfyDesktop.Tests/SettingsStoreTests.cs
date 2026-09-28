using WolfyDesktop.Core.Models;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class SettingsStoreTests
{
    private TempDirectory _temp = null!;
    private AppPaths _paths = null!;

    [SetUp]
    public void SetUp()
    {
        _temp = new TempDirectory();
        _paths = new AppPaths(_temp.Path);
    }

    [TearDown]
    public void TearDown() => _temp.Dispose();

    [Test]
    public async Task LoadAsync_WithoutFile_UsesDefaults()
    {
        var store = new SettingsStore(_paths);

        await store.LoadAsync();

        Assert.That(store.Current.Theme, Is.EqualTo(AppTheme.Dark));
        Assert.That(store.Current.Volume, Is.EqualTo(50));
        Assert.That(store.Current.DefaultMusicFile, Is.Null);
    }

    [Test]
    public async Task LoadAsync_ReadsFileWrittenByVersion112()
    {
        // Format written by the reflection-based serializer in v1.1.2, including a removed property.
        File.WriteAllText(_paths.SettingsFile, """
            {
              "Theme": "Light",
              "Volume": 35.5,
              "DefaultMusicFile": "rain.mp3",
              "IsFirstRun": false
            }
            """);
        var store = new SettingsStore(_paths);

        await store.LoadAsync();

        Assert.That(store.Current.Theme, Is.EqualTo(AppTheme.Light));
        Assert.That(store.Current.Volume, Is.EqualTo(35.5));
        Assert.That(store.Current.DefaultMusicFile, Is.EqualTo("rain.mp3"));
    }

    [Test]
    public async Task LoadAsync_WithCorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(_paths.SettingsFile, "{ not json");
        var store = new SettingsStore(_paths);

        await store.LoadAsync();

        Assert.That(store.Current.Theme, Is.EqualTo(AppTheme.Dark));
    }

    [Test]
    public async Task FlushAsync_WritesPendingChangeThatReloads()
    {
        var store = new SettingsStore(_paths, TimeSpan.FromMinutes(1));
        store.Update(s =>
        {
            s.Theme = AppTheme.System;
            s.Volume = 80;
        });

        await store.FlushAsync();
        var reloaded = new SettingsStore(_paths);
        await reloaded.LoadAsync();

        Assert.That(reloaded.Current.Theme, Is.EqualTo(AppTheme.System));
        Assert.That(reloaded.Current.Volume, Is.EqualTo(80));
    }

    [Test]
    public void Update_DoesNotWriteBeforeTheDebounceDelay()
    {
        var store = new SettingsStore(_paths, TimeSpan.FromMinutes(1));

        store.Update(s => s.Volume = 10);

        Assert.That(File.Exists(_paths.SettingsFile), Is.False);
    }

    [Test]
    public async Task Update_RapidChanges_SaveOnlyTheLastValue()
    {
        var store = new SettingsStore(_paths, TimeSpan.FromMilliseconds(50));

        for (var volume = 0; volume <= 100; volume++)
        {
            var value = volume;
            store.Update(s => s.Volume = value);
        }

        await Task.Delay(500);
        var reloaded = new SettingsStore(_paths);
        await reloaded.LoadAsync();

        Assert.That(reloaded.Current.Volume, Is.EqualTo(100));
        Assert.That(File.Exists(_paths.SettingsFile + ".tmp"), Is.False);
    }

    [Test]
    public async Task FlushAsync_WithNothingPending_DoesNotCreateFile()
    {
        var store = new SettingsStore(_paths);

        await store.FlushAsync();

        Assert.That(File.Exists(_paths.SettingsFile), Is.False);
    }
}
