using System.Net;
using System.Net.Http.Headers;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class DownloadServiceTests
{
    private static readonly Uri Source = new("https://example.test/file.mp3");

    private TempDirectory _temp = null!;

    [SetUp]
    public void SetUp() => _temp = new TempDirectory();

    [TearDown]
    public void TearDown() => _temp.Dispose();

    private static DownloadService CreateService(Func<HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)));

    private static HttpResponseMessage Ok(byte[] body, string mediaType = "audio/mpeg")
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    [Test]
    public async Task DownloadAsync_WritesFileAndReportsCompletion()
    {
        var body = new byte[300_000];
        Random.Shared.NextBytes(body);
        var destination = _temp.Combine("sub", "file.mp3");
        var reports = new List<DownloadProgress>();

        await CreateService(() => Ok(body)).DownloadAsync(Source, destination, new SyncProgress(reports.Add));

        Assert.That(File.ReadAllBytes(destination), Is.EqualTo(body));
        Assert.That(reports[^1].Percent, Is.EqualTo(100));
        Assert.That(File.Exists(destination + ".download"), Is.False);
    }

    [Test]
    public async Task DownloadAsync_RejectsHtmlAndKeepsExistingFile()
    {
        var destination = _temp.CreateFile("file.mp3", [7, 7, 7]);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CreateService(() => Ok("<html></html>"u8.ToArray(), "text/html")).DownloadAsync(Source, destination));

        Assert.That(File.ReadAllBytes(destination), Is.EqualTo(new byte[] { 7, 7, 7 }));
        Assert.That(File.Exists(destination + ".download"), Is.False);
    }

    [Test]
    public async Task DownloadAsync_ServerError_Throws()
    {
        var destination = _temp.Combine("file.mp3");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateService(() => new HttpResponseMessage(HttpStatusCode.NotFound)).DownloadAsync(Source, destination));

        Assert.That(File.Exists(destination), Is.False);
    }

    [Test]
    public async Task DownloadAsync_Cancelled_LeavesNoFiles()
    {
        var destination = _temp.Combine("file.mp3");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.CatchAsync<OperationCanceledException>(() =>
            CreateService(() => Ok(new byte[10])).DownloadAsync(Source, destination, cancellationToken: cts.Token));

        Assert.That(Directory.GetFiles(_temp.Path), Is.Empty);
    }

    [Test]
    public void DownloadProgress_PercentIsNullWithoutLength()
    {
        Assert.That(new DownloadProgress(10, null).Percent, Is.Null);
        Assert.That(new DownloadProgress(25, 100).Percent, Is.EqualTo(25));
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(respond());
        }
    }

    /// <summary>Unlike <see cref="Progress{T}"/>, reports inline so assertions see every value.</summary>
    private sealed class SyncProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    {
        public void Report(DownloadProgress value) => report(value);
    }
}
