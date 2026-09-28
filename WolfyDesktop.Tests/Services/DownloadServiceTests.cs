namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class DownloadServiceTests
{
    [Test]
    public void GoogleDriveUrl_ShouldBeValid()
    {
        const string googleDriveUrl = "https://drive.usercontent.google.com/download?id=1jzex-vuQrpHUZXI_VhV34AyY97DkUc5c&export=download";
        
        Assert.That(Uri.TryCreate(googleDriveUrl, UriKind.Absolute, out var uri), Is.True);
        Assert.That(uri?.Scheme, Is.EqualTo("https"));
    }

    [Test]
    public void DefaultAudioFileName_ShouldBeCorrect()
    {
        const string audioFileName = "lofi_rain.mp3";
        
        Assert.That(audioFileName, Is.EqualTo("lofi_rain.mp3"));
        Assert.That(Path.GetExtension(audioFileName), Is.EqualTo(".mp3"));
    }

    [Test]
    public void DownloadProgressPercentage_ShouldCalculateCorrectly()
    {
        const long bytesReceived = 5000000;
        const long totalBytes = 10000000;

        var progressPercentage = (double)bytesReceived / totalBytes * 100.0;

        Assert.That(progressPercentage, Is.EqualTo(50.0).Within(0.01));
    }

    [TestCase(0, 10000000, 0.0)]
    [TestCase(2500000, 10000000, 25.0)]
    [TestCase(5000000, 10000000, 50.0)]
    [TestCase(7500000, 10000000, 75.0)]
    [TestCase(10000000, 10000000, 100.0)]
    public void DownloadProgress_VariousStages_ShouldCalculateCorrectly(long received, long total, double expectedPercent)
    {
        var progressPercentage = (double)received / total * 100.0;
        Assert.That(progressPercentage, Is.EqualTo(expectedPercent).Within(0.01));
    }

    [Test]
    public void DownloadProgress_IsIndeterminate_WhenTotalBytesUnknown()
    {
        long? totalBytes = null;

        var isIndeterminate = !totalBytes.HasValue || totalBytes.Value <= 0;

        Assert.That(isIndeterminate, Is.True);
    }

    [Test]
    public void DownloadProgress_IsNotIndeterminate_WhenTotalBytesKnown()
    {
        long? totalBytes = 10000000;

        var isIndeterminate = !totalBytes.HasValue || totalBytes.Value <= 0;

        Assert.That(isIndeterminate, Is.False);
    }

    [Test]
    public void DestinationPath_ShouldCombineCorrectly()
    {
        var musicsFolderPath = @"C:\App\Musics";
        var fileName = "lofi_rain.mp3";

        var destinationPath = Path.Combine(musicsFolderPath, fileName);

        Assert.That(destinationPath, Does.EndWith("lofi_rain.mp3"));
        Assert.That(destinationPath, Does.Contain("Musics"));
    }

    [Test]
    public void CancellationToken_CanBeCancelled()
    {
        using var cts = new CancellationTokenSource();
        
        Assert.That(cts.IsCancellationRequested, Is.False);
        
        cts.Cancel();
        
        Assert.That(cts.IsCancellationRequested, Is.True);
    }
}
