using System.Text;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class AudioFileInspectorTests
{
    private TempDirectory _temp = null!;

    [SetUp]
    public void SetUp() => _temp = new TempDirectory();

    [TearDown]
    public void TearDown() => _temp.Dispose();

    private static byte[] WithHeader(params byte[] header)
    {
        var content = new byte[4096];
        header.CopyTo(content, 0);
        return content;
    }

    [Test]
    public void LooksLikeMp3_AcceptsId3Tag()
    {
        var path = _temp.CreateFile("a.mp3", WithHeader((byte)'I', (byte)'D', (byte)'3'));

        Assert.That(AudioFileInspector.LooksLikeMp3(path), Is.True);
    }

    [Test]
    public void LooksLikeMp3_AcceptsBareMpegFrame()
    {
        var path = _temp.CreateFile("a.mp3", WithHeader(0xFF, 0xFB, 0x90));

        Assert.That(AudioFileInspector.LooksLikeMp3(path), Is.True);
    }

    [Test]
    public void LooksLikeMp3_RejectsHtmlPage()
    {
        var html = Encoding.UTF8.GetBytes("<!DOCTYPE html><html>" + new string(' ', 2000) + "</html>");
        var path = _temp.CreateFile("a.mp3", html);

        Assert.That(AudioFileInspector.LooksLikeMp3(path), Is.False);
    }

    [Test]
    public void LooksLikeMp3_RejectsTinyFile()
    {
        var path = _temp.CreateFile("a.mp3", [(byte)'I', (byte)'D', (byte)'3']);

        Assert.That(AudioFileInspector.LooksLikeMp3(path), Is.False);
    }

    [Test]
    public void LooksLikeMp3_ReturnsFalseForMissingFile()
    {
        Assert.That(AudioFileInspector.LooksLikeMp3(_temp.Combine("missing.mp3")), Is.False);
    }
}
