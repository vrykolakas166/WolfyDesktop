namespace WolfyDesktop.Core.Services;

public static class AudioFileInspector
{
    private const int MinimumSize = 1024;

    /// <summary>
    /// Cheap sanity check for a downloaded MP3: big enough, and starts with an ID3 tag
    /// or an MPEG frame header. Catches the common failure where a web page (for
    /// example a quota or sign-in page) was saved in place of the audio.
    /// </summary>
    public static bool LooksLikeMp3(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length < MinimumSize)
            {
                return false;
            }

            Span<byte> header = stackalloc byte[3];
            stream.ReadExactly(header);

            var hasId3Tag = header[0] == 'I' && header[1] == 'D' && header[2] == '3';
            var hasFrameSync = header[0] == 0xFF && (header[1] & 0xE0) == 0xE0;
            return hasId3Tag || hasFrameSync;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
