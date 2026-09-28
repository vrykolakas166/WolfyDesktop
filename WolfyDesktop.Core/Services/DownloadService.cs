using System.Diagnostics;

namespace WolfyDesktop.Core.Services;

public sealed class DownloadService : IDownloadService
{
    /// <summary>Progress is reported at most this often so the UI is not flooded.</summary>
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    private readonly HttpClient _httpClient;

    public DownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DownloadAsync(
        Uri source,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        var tempPath = destinationPath + ".download";

        try
        {
            using var response = await _httpClient
                .GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentType?.MediaType is "text/html")
            {
                throw new InvalidDataException("The server returned a web page instead of the file. Try again later.");
            }

            var totalBytes = response.Content.Headers.ContentLength;
            await using (var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long received = 0;
                var sinceReport = Stopwatch.StartNew();
                int read;

                while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    received += read;

                    if (sinceReport.Elapsed >= ProgressInterval)
                    {
                        progress?.Report(new DownloadProgress(received, totalBytes));
                        sinceReport.Restart();
                    }
                }

                progress?.Report(new DownloadProgress(received, totalBytes));
            }

            File.Move(tempPath, destinationPath, overwrite: true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort; a stale .download file is harmless and overwritten next time.
        }
    }
}
