namespace WolfyDesktop.Tests.Models;

[TestFixture]
public class MusicItemTests
{
    [Test]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var musicItem = new TestMusicItem();

        Assert.Multiple(() =>
        {
            Assert.That(musicItem.FileName, Is.EqualTo(string.Empty));
            Assert.That(musicItem.DisplayName, Is.EqualTo(string.Empty));
            Assert.That(musicItem.FullPath, Is.EqualTo(string.Empty));
            Assert.That(musicItem.FileSize, Is.EqualTo(0));
            Assert.That(musicItem.SizeText, Is.EqualTo(string.Empty));
            Assert.That(musicItem.IsDefault, Is.False);
        });
    }

    [Test]
    public void FileName_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();
        const string expectedFileName = "test_music.mp3";

        musicItem.FileName = expectedFileName;

        Assert.That(musicItem.FileName, Is.EqualTo(expectedFileName));
    }

    [Test]
    public void DisplayName_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();
        const string expectedDisplayName = "Test Music";

        musicItem.DisplayName = expectedDisplayName;

        Assert.That(musicItem.DisplayName, Is.EqualTo(expectedDisplayName));
    }

    [Test]
    public void FullPath_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();
        const string expectedPath = @"C:\Music\test_music.mp3";

        musicItem.FullPath = expectedPath;

        Assert.That(musicItem.FullPath, Is.EqualTo(expectedPath));
    }

    [Test]
    public void FileSize_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();
        const long expectedSize = 1024000;

        musicItem.FileSize = expectedSize;

        Assert.That(musicItem.FileSize, Is.EqualTo(expectedSize));
    }

    [Test]
    public void SizeText_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();
        const string expectedSizeText = "1.00 MB";

        musicItem.SizeText = expectedSizeText;

        Assert.That(musicItem.SizeText, Is.EqualTo(expectedSizeText));
    }

    [Test]
    public void IsDefault_CanBeSetAndRetrieved()
    {
        var musicItem = new TestMusicItem();

        musicItem.IsDefault = true;

        Assert.That(musicItem.IsDefault, Is.True);
    }

    [Test]
    public void MusicItem_CanBeInitializedWithObjectInitializer()
    {
        var musicItem = new TestMusicItem
        {
            FileName = "lofi_rain.mp3",
            DisplayName = "Lofi Rain",
            FullPath = @"C:\Music\lofi_rain.mp3",
            FileSize = 5242880,
            SizeText = "5.00 MB",
            IsDefault = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(musicItem.FileName, Is.EqualTo("lofi_rain.mp3"));
            Assert.That(musicItem.DisplayName, Is.EqualTo("Lofi Rain"));
            Assert.That(musicItem.FullPath, Is.EqualTo(@"C:\Music\lofi_rain.mp3"));
            Assert.That(musicItem.FileSize, Is.EqualTo(5242880));
            Assert.That(musicItem.SizeText, Is.EqualTo("5.00 MB"));
            Assert.That(musicItem.IsDefault, Is.True);
        });
    }

    [TestCase(0L, "0 bytes")]
    [TestCase(1024L, "1 KB")]
    [TestCase(1048576L, "1 MB")]
    [TestCase(5242880L, "5 MB")]
    public void FileSize_VariousValues_CanBeStoredAndRetrieved(long fileSize, string description)
    {
        var musicItem = new TestMusicItem();
        
        musicItem.FileSize = fileSize;
        
        Assert.That(musicItem.FileSize, Is.EqualTo(fileSize), $"Failed for {description}");
    }
}

/// <summary>
/// Test version of MusicItem without WinUI dependencies
/// </summary>
public class TestMusicItem
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string SizeText { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
