using System;
using System.Threading;
using System.Threading.Tasks;

namespace WolfyDesktop.Services;

public interface IDownloadService
{
    event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    event EventHandler<string>? DownloadCompleted;
    event EventHandler<Exception>? DownloadFailed;
    
    bool IsDownloading { get; }
    
    Task<bool> DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
    void Cancel();
}
