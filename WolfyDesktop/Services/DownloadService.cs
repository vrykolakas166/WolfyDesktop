using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WolfyDesktop.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _internalCts;
    
    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    public event EventHandler<string>? DownloadCompleted;
    public event EventHandler<Exception>? DownloadFailed;
    
    public bool IsDownloading { get; private set; }

    public DownloadService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<bool> DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (IsDownloading)
            return false;

        IsDownloading = true;
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _internalCts.Token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(_internalCts.Token);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, _internalCts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _internalCts.Token);
                totalBytesRead += bytesRead;

                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    BytesReceived = totalBytesRead,
                    TotalBytes = totalBytes
                });
            }

            DownloadCompleted?.Invoke(this, destinationPath);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Clean up partial download
            if (File.Exists(destinationPath))
            {
                try { File.Delete(destinationPath); } catch { }
            }
            throw;
        }
        catch (Exception ex)
        {
            DownloadFailed?.Invoke(this, ex);
            return false;
        }
        finally
        {
            IsDownloading = false;
            _internalCts?.Dispose();
            _internalCts = null;
        }
    }

    public void Cancel()
    {
        _internalCts?.Cancel();
    }
}
