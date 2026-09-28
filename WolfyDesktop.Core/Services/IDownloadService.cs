namespace WolfyDesktop.Core.Services;

public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)
{
    /// <summary>0–100, or null when the server did not send a content length.</summary>
    public double? Percent => TotalBytes > 0 ? 100.0 * BytesReceived / TotalBytes.Value : null;
}

public interface IDownloadService
{
    /// <summary>
    /// Downloads <paramref name="source"/> to <paramref name="destinationPath"/>. The file is
    /// only replaced once the download completes, so a failed or cancelled download
    /// never damages an existing copy.
    /// </summary>
    Task DownloadAsync(
        Uri source,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
