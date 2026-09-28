using System;

namespace WolfyDesktop.Services;

public class DownloadProgressEventArgs : EventArgs
{
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }
    public double ProgressPercentage => TotalBytes.HasValue && TotalBytes.Value > 0 
        ? (double)BytesReceived / TotalBytes.Value * 100.0 
        : 0;
    public bool IsIndeterminate => !TotalBytes.HasValue || TotalBytes.Value <= 0;
}
