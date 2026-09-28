namespace WolfyDesktop.Core.Models;

/// <summary>
/// An audio file in the user's music library.
/// </summary>
public sealed record MusicTrack(string FullPath, long SizeBytes, bool IsDefault)
{
    public string FileName => Path.GetFileName(FullPath);

    public string DisplayName => Path.GetFileNameWithoutExtension(FullPath);

    public string SizeText => $"{SizeBytes / (1024.0 * 1024.0):F2} MB";
}
