using System.Text;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class LegacyInstallMigratorTests
{
    private TempDirectory _temp = null!;
    private AppPaths _paths = null!;
    private SettingsStore _settings = null!;
    private LegacyInstallMigrator _migrator = null!;

    [SetUp]
    public void SetUp()
    {
        _temp = new TempDirectory();
        _paths = new AppPaths(_temp.Combine("data"));
        _settings = new SettingsStore(_paths, TimeSpan.FromMinutes(1));
        _migrator = new LegacyInstallMigrator(_paths, _settings);
    }

    [TearDown]
    public void TearDown() => _temp.Dispose();

    [Test]
    public void ImportMusicFrom_CopiesOnlyAudioFiles()
    {
        _temp.CreateFile(Path.Combine("old", "Musics", "a.mp3"));
        _temp.CreateFile(Path.Combine("old", "Musics", "b.wav"));
        _temp.CreateFile(Path.Combine("old", "Musics", "readme.txt"));

        var copied = _migrator.ImportMusicFrom(_temp.Combine("old", "Musics"));

        Assert.That(copied, Is.EqualTo(2));
        Assert.That(Directory.GetFiles(_paths.MusicFolder).Select(Path.GetFileName), Is.EquivalentTo(new[] { "a.mp3", "b.wav" }));
    }

    [Test]
    public void ImportMusicFrom_NeverOverwritesExistingTracks()
    {
        _temp.CreateFile(Path.Combine("old", "Musics", "a.mp3"), size: 10);
        var existing = _temp.CreateFile(Path.Combine("data", "Musics", "a.mp3"), size: 99);

        var copied = _migrator.ImportMusicFrom(_temp.Combine("old", "Musics"));

        Assert.That(copied, Is.Zero);
        Assert.That(new FileInfo(existing).Length, Is.EqualTo(99));
    }

    [Test]
    public void ImportMusicFrom_CarriesOverDefaultTrackChoice()
    {
        _temp.CreateFile(Path.Combine("old", "Musics", "fav.mp3"));
        _temp.CreateFile(Path.Combine("old", "Musics", "settings.txt"), Encoding.UTF8.GetBytes("fav.mp3\r\n"));

        _migrator.ImportMusicFrom(_temp.Combine("old", "Musics"));

        Assert.That(_settings.Current.DefaultMusicFile, Is.EqualTo("fav.mp3"));
    }

    [Test]
    public void ImportMusicFrom_KeepsNewerDefaultTrackChoice()
    {
        _settings.Update(s => s.DefaultMusicFile = "new.mp3");
        _temp.CreateFile(Path.Combine("old", "Musics", "fav.mp3"));
        _temp.CreateFile(Path.Combine("old", "Musics", "settings.txt"), Encoding.UTF8.GetBytes("fav.mp3"));

        _migrator.ImportMusicFrom(_temp.Combine("old", "Musics"));

        Assert.That(_settings.Current.DefaultMusicFile, Is.EqualTo("new.mp3"));
    }

    [Test]
    public void ImportMusicFrom_MissingFolder_DoesNothing()
    {
        var copied = _migrator.ImportMusicFrom(_temp.Combine("nope"));

        Assert.That(copied, Is.Zero);
        Assert.That(Directory.Exists(_paths.MusicFolder), Is.False);
    }

    [Test]
    public void ImportMusicFrom_SameFolderAsLibrary_DoesNothing()
    {
        _temp.CreateFile(Path.Combine("data", "Musics", "a.mp3"));

        Assert.That(_migrator.ImportMusicFrom(_paths.MusicFolder), Is.Zero);
    }
}
