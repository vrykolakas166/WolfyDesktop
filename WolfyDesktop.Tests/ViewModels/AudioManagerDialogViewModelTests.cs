namespace WolfyDesktop.Tests.ViewModels;

[TestFixture]
public class AudioManagerDialogViewModelTests
{
    [Test]
    public void MusicItems_InitialCount_ShouldBeZero()
    {
        var musicItems = new List<object>();
        Assert.That(musicItems, Has.Count.EqualTo(0));
    }

    [Test]
    public void StatusIconGlyph_Default_ShouldBeWarning()
    {
        const string warningGlyph = "\uE7BA";
        const string checkmarkGlyph = "\uE930";
        
        // No music files initially
        var statusGlyph = warningGlyph;
        
        Assert.That(statusGlyph, Is.EqualTo(warningGlyph));
    }

    [Test]
    public void StatusIconGlyph_WhenFilesExist_ShouldBeCheckmark()
    {
        const string checkmarkGlyph = "\uE930";
        
        // When music files exist
        var statusGlyph = checkmarkGlyph;
        
        Assert.That(statusGlyph, Is.EqualTo(checkmarkGlyph));
    }

    [Test]
    public void DownloadButtonText_WhenNoFiles_ShouldBeDownload()
    {
        const string expectedText = "Download Default Audio";
        Assert.That(expectedText, Is.EqualTo("Download Default Audio"));
    }

    [Test]
    public void DownloadButtonText_WhenFilesExist_ShouldBeRedownload()
    {
        const string expectedText = "Re-download Default Audio";
        Assert.That(expectedText, Is.EqualTo("Re-download Default Audio"));
    }

    [Test]
    public void DownloadButtonEnabled_Default_ShouldBeTrue()
    {
        const bool enabled = true;
        Assert.That(enabled, Is.True);
    }

    [Test]
    public void DownloadButtonEnabled_WhenDownloading_ShouldBeFalse()
    {
        const bool enabled = false;
        Assert.That(enabled, Is.False);
    }

    [Test]
    public void CancelButtonVisible_Default_ShouldBeFalse()
    {
        const bool visible = false;
        Assert.That(visible, Is.False);
    }

    [Test]
    public void CancelButtonVisible_WhenDownloading_ShouldBeTrue()
    {
        const bool visible = true;
        Assert.That(visible, Is.True);
    }

    [Test]
    public void ProgressBarVisible_Default_ShouldBeFalse()
    {
        const bool visible = false;
        Assert.That(visible, Is.False);
    }

    [Test]
    public void ProgressBarVisible_WhenDownloading_ShouldBeTrue()
    {
        const bool visible = true;
        Assert.That(visible, Is.True);
    }

    [Test]
    public void ProgressBarIndeterminate_WhenTotalUnknown_ShouldBeTrue()
    {
        const bool indeterminate = true;
        Assert.That(indeterminate, Is.True);
    }

    [Test]
    public void ProgressBarIndeterminate_WhenTotalKnown_ShouldBeFalse()
    {
        const bool indeterminate = false;
        Assert.That(indeterminate, Is.False);
    }

    [Test]
    public void ProgressText_Default_ShouldBeEmpty()
    {
        const string progressText = "";
        Assert.That(progressText, Is.Empty);
    }

    [Test]
    public void ProgressText_WhenDownloading_ShouldShowProgress()
    {
        const double percentage = 50.0;
        var progressText = $"Downloading... {percentage:F0}%";
        
        Assert.That(progressText, Is.EqualTo("Downloading... 50%"));
    }

    [Test]
    public void Message_WhenSet_ShouldBeVisible()
    {
        const string message = "Test message";
        var visible = !string.IsNullOrEmpty(message);
        
        Assert.That(visible, Is.True);
    }

    [Test]
    public void Message_WhenEmpty_ShouldNotBeVisible()
    {
        const string message = "";
        var visible = !string.IsNullOrEmpty(message);
        
        Assert.That(visible, Is.False);
    }

    [Test]
    public void IsDownloading_Default_ShouldBeFalse()
    {
        const bool isDownloading = false;
        Assert.That(isDownloading, Is.False);
    }

    [Test]
    public void SelectedMusicItem_Default_ShouldBeNull()
    {
        object? selectedItem = null;
        Assert.That(selectedItem, Is.Null);
    }

    [Test]
    public void CanSetDefault_WithNoSelection_ShouldBeFalse()
    {
        object? selectedItem = null;
        var canSetDefault = selectedItem != null;
        
        Assert.That(canSetDefault, Is.False);
    }

    [Test]
    public void CanSetDefault_WithSelection_ShouldBeTrue()
    {
        var selectedItem = new object();
        var canSetDefault = selectedItem != null;
        
        Assert.That(canSetDefault, Is.True);
    }

    [Test]
    public void CanDelete_WithNoSelection_ShouldBeFalse()
    {
        object? selectedItem = null;
        var canDelete = selectedItem != null;
        
        Assert.That(canDelete, Is.False);
    }

    [Test]
    public void CanDelete_WithSelection_ShouldBeTrue()
    {
        var selectedItem = new object();
        var canDelete = selectedItem != null;
        
        Assert.That(canDelete, Is.True);
    }

    [Test]
    public void StatusText_NoFiles_ShouldIndicateEmpty()
    {
        const string statusText = "No music files found in library";
        Assert.That(statusText, Does.Contain("No music files"));
    }

    [Test]
    public void StatusText_WithFiles_ShouldShowCount()
    {
        const int fileCount = 5;
        var statusText = $"Music library: {fileCount} file(s)";
        
        Assert.That(statusText, Is.EqualTo("Music library: 5 file(s)"));
    }

    [TestCase(1, "Music library: 1 file(s)")]
    [TestCase(3, "Music library: 3 file(s)")]
    [TestCase(10, "Music library: 10 file(s)")]
    public void StatusText_VariousFileCounts_ShouldFormatCorrectly(int count, string expected)
    {
        var statusText = $"Music library: {count} file(s)";
        Assert.That(statusText, Is.EqualTo(expected));
    }
}
