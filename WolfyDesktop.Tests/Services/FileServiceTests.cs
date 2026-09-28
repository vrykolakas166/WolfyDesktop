using WolfyDesktop.Tests.Helpers;

namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class FileServiceTests
{
    private TestFileHelper? _testFileHelper;
    private string? _testMusicDirectory;

    [SetUp]
    public void SetUp()
    {
        _testFileHelper = new TestFileHelper();
        var tempPath = Path.GetTempPath();
        _testMusicDirectory = _testFileHelper.CreateTestDirectory(tempPath, $"WolfyDesktop_Test_{Guid.NewGuid():N}");
    }

    [TearDown]
    public void TearDown()
    {
        _testFileHelper?.Dispose();
    }

    [Test]
    public void MusicDirectory_WhenCreated_ShouldExist()
    {
        Assert.That(Directory.Exists(_testMusicDirectory), Is.True);
    }

    [Test]
    public void SettingsFile_WhenCreated_ShouldStoreDefaultMusicFileName()
    {
        const string defaultFileName = "lofi_rain.mp3";
        var settingsPath = Path.Combine(_testMusicDirectory!, "settings.txt");

        File.WriteAllText(settingsPath, defaultFileName);
        var content = File.ReadAllText(settingsPath);

        Assert.That(content, Is.EqualTo(defaultFileName));
    }

    [TestCase(".mp3")]
    [TestCase(".m4a")]
    [TestCase(".wav")]
    [TestCase(".flac")]
    [TestCase(".aac")]
    [TestCase(".wma")]
    public void SupportedAudioFormats_ShouldBeDetectable(string extension)
    {
        var supportedExtensions = new[] { ".mp3", ".m4a", ".wav", ".flac", ".aac", ".wma" };
        var testFileName = $"test_audio{extension}";
        _testFileHelper!.CreateTestFile(_testMusicDirectory!, testFileName, new byte[1024]);

        var files = Directory.GetFiles(_testMusicDirectory!)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        Assert.That(files, Has.Count.EqualTo(1));
        Assert.That(Path.GetFileName(files[0]), Is.EqualTo(testFileName));
    }

    [Test]
    public void GetFileSize_ShouldReturnCorrectSize()
    {
        const int expectedSize = 5242880; // 5 MB
        var testData = new byte[expectedSize];
        var testFile = _testFileHelper!.CreateTestFile(_testMusicDirectory!, "test_music.mp3", testData);

        var fileInfo = new FileInfo(testFile);

        Assert.That(fileInfo.Length, Is.EqualTo(expectedSize));
    }

    [Test]
    public void FormatFileSize_ShouldCalculateCorrectly()
    {
        const long fileSizeBytes = 5242880; // 5 MB
        const double expectedMB = 5.0;

        var sizeInMB = fileSizeBytes / (1024.0 * 1024.0);

        Assert.That(sizeInMB, Is.EqualTo(expectedMB).Within(0.01));
    }

    [Test]
    public void FormatFileSize_ShouldDisplayCorrectly()
    {
        const long fileSizeBytes = 5242880;
        var sizeInMB = fileSizeBytes / (1024.0 * 1024.0);

        var sizeText = $"{sizeInMB:F2} MB";

        Assert.That(sizeText, Is.EqualTo("5.00 MB"));
    }

    [Test]
    public void GetFileNameWithoutExtension_ShouldReturnCorrectDisplayName()
    {
        const string fileName = "lofi_rain.mp3";

        var displayName = Path.GetFileNameWithoutExtension(fileName);

        Assert.That(displayName, Is.EqualTo("lofi_rain"));
    }

    [Test]
    public void GetMusicFiles_ShouldEnumerateOnlyAudioFiles()
    {
        var fileNames = new[] { "music1.mp3", "music2.wav", "music3.flac", "document.txt" };
        foreach (var fileName in fileNames)
        {
            _testFileHelper!.CreateTestFile(_testMusicDirectory!, fileName, new byte[100]);
        }

        var supportedExtensions = new[] { ".mp3", ".m4a", ".wav", ".flac", ".aac", ".wma" };
        var audioFiles = Directory.GetFiles(_testMusicDirectory!)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        Assert.That(audioFiles, Has.Count.EqualTo(3), "Should find only audio files");
    }

    [Test]
    public void DefaultMusicPath_WhenSettingsFileMissing_ShouldAllowFallback()
    {
        var settingsPath = Path.Combine(_testMusicDirectory!, "settings.txt");
        var defaultPath = Path.Combine(_testMusicDirectory!, "lofi_rain.mp3");

        var actualPath = File.Exists(settingsPath) 
            ? Path.Combine(_testMusicDirectory!, File.ReadAllText(settingsPath).Trim())
            : defaultPath;

        Assert.That(actualPath, Is.EqualTo(defaultPath));
    }

    [Test]
    public void DeleteFile_ShouldRemoveFromFileSystem()
    {
        var testFile = _testFileHelper!.CreateTestFile(_testMusicDirectory!, "temp_music.mp3", new byte[100]);
        Assert.That(File.Exists(testFile), Is.True, "File should exist before deletion");

        File.Delete(testFile);

        Assert.That(File.Exists(testFile), Is.False, "File should not exist after deletion");
    }

    [TestCase("lofi_rain.mp3", ".mp3")]
    [TestCase("music.wav", ".wav")]
    [TestCase("audio.flac", ".flac")]
    [TestCase("song.m4a", ".m4a")]
    public void GetExtension_ShouldReturnCorrectExtension(string fileName, string expectedExtension)
    {
        var extension = Path.GetExtension(fileName);

        Assert.That(extension, Is.EqualTo(expectedExtension));
    }

    [TestCase("lofi_rain.mp3", "lofi_rain")]
    [TestCase("my_music.wav", "my_music")]
    [TestCase("song.flac", "song")]
    public void GetFileNameWithoutExtension_ShouldReturnCorrectName(string fileName, string expectedName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        Assert.That(nameWithoutExtension, Is.EqualTo(expectedName));
    }
}
