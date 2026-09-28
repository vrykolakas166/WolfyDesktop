using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class MusicLibraryTests
{
    private TempDirectory _temp = null!;
    private AppPaths _paths = null!;
    private SettingsStore _settings = null!;
    private MusicLibrary _library = null!;

    [SetUp]
    public void SetUp()
    {
        _temp = new TempDirectory();
        _paths = new AppPaths(_temp.Path);
        _settings = new SettingsStore(_paths, TimeSpan.FromMinutes(1));
        _library = new MusicLibrary(_paths, _settings);
    }

    [TearDown]
    public void TearDown() => _temp.Dispose();

    private string AddTrack(string fileName, int size = 16) => _temp.CreateFile(Path.Combine("Musics", fileName), size);

    [Test]
    public void GetTracks_WhenFolderMissing_ReturnsEmpty()
    {
        Assert.That(_library.GetTracks(), Is.Empty);
        Assert.That(Directory.Exists(_paths.MusicFolder), Is.False, "Reading must not create the folder.");
    }

    [Test]
    public void GetTracks_ListsSupportedFilesSortedByName()
    {
        AddTrack("b.MP3");
        AddTrack("a.flac");
        AddTrack("notes.txt");
        AddTrack("cover.jpg");

        var names = _library.GetTracks().Select(t => t.FileName);

        Assert.That(names, Is.EqualTo(new[] { "a.flac", "b.MP3" }));
    }

    [Test]
    public void GetTracks_MarksOnlyTheDefaultTrack()
    {
        AddTrack("a.mp3");
        AddTrack("b.mp3");
        _library.SetDefaultTrack("b.mp3");

        var tracks = _library.GetTracks();

        Assert.That(tracks.Single(t => t.IsDefault).FileName, Is.EqualTo("b.mp3"));
    }

    [Test]
    public void GetDefaultTrackPath_PrefersChosenTrack()
    {
        AddTrack(MusicLibrary.DownloadableTrackFileName);
        var chosen = AddTrack("chosen.mp3");
        _library.SetDefaultTrack("chosen.mp3");

        Assert.That(_library.GetDefaultTrackPath(), Is.EqualTo(chosen));
    }

    [Test]
    public void GetDefaultTrackPath_FallsBackToDownloadableTrack()
    {
        AddTrack("a.mp3");
        var downloadable = AddTrack(MusicLibrary.DownloadableTrackFileName);
        _settings.Update(s => s.DefaultMusicFile = "deleted.mp3");

        Assert.That(_library.GetDefaultTrackPath(), Is.EqualTo(downloadable));
    }

    [Test]
    public void GetDefaultTrackPath_FallsBackToFirstTrack()
    {
        var first = AddTrack("a.mp3");
        AddTrack("b.mp3");

        Assert.That(_library.GetDefaultTrackPath(), Is.EqualTo(first));
    }

    [Test]
    public void GetDefaultTrackPath_IgnoresPathsInSettings()
    {
        _temp.CreateFile("outside.mp3");
        _settings.Update(s => s.DefaultMusicFile = @"..\outside.mp3");

        Assert.That(_library.GetDefaultTrackPath(), Is.Null);
    }

    [Test]
    public async Task ImportAsync_CreatesFolderAndCopiesContent()
    {
        using var source = new MemoryStream([1, 2, 3]);

        var path = await _library.ImportAsync(source, "song.mp3");

        Assert.That(path, Is.EqualTo(Path.Combine(_paths.MusicFolder, "song.mp3")));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task ImportAsync_KeepsExistingFileAndPicksNewName()
    {
        var existing = AddTrack("song.mp3", size: 4);

        using var source = new MemoryStream([9]);
        var path = await _library.ImportAsync(source, "song.mp3");

        Assert.That(Path.GetFileName(path), Is.EqualTo("song_1.mp3"));
        Assert.That(new FileInfo(existing).Length, Is.EqualTo(4));
    }

    [Test]
    public async Task ImportAsync_RejectsUnsupportedExtension()
    {
        using var source = new MemoryStream([1]);

        await Assert.ThrowsAsync<NotSupportedException>(() => _library.ImportAsync(source, "virus.exe"));
    }

    [Test]
    public void Delete_RemovesFileAndClearsDefault()
    {
        var path = AddTrack("a.mp3");
        _library.SetDefaultTrack("a.mp3");

        _library.Delete(path);

        Assert.That(File.Exists(path), Is.False);
        Assert.That(_settings.Current.DefaultMusicFile, Is.Null);
    }

    [Test]
    public void Delete_RefusesFilesOutsideTheLibrary()
    {
        var outside = _temp.CreateFile("settings.json");

        Assert.Throws<InvalidOperationException>(() => _library.Delete(outside));
        Assert.That(File.Exists(outside), Is.True);
    }

    [Test]
    public async Task Changed_IsRaisedForEveryMutation()
    {
        var raised = 0;
        _library.Changed += (_, _) => raised++;

        using var source = new MemoryStream([1]);
        var path = await _library.ImportAsync(source, "a.mp3");
        _library.SetDefaultTrack("a.mp3");
        _library.Delete(path);

        Assert.That(raised, Is.EqualTo(3));
    }

    [Test]
    public void MusicTrack_FormatsSizeAndName()
    {
        var path = AddTrack("My Song.mp3", size: 3 * 1024 * 1024 / 2);

        var track = _library.GetTracks().Single();

        Assert.That(track.FullPath, Is.EqualTo(path));
        Assert.That(track.DisplayName, Is.EqualTo("My Song"));
        Assert.That(track.SizeText, Is.EqualTo("1.50 MB"));
    }
}
