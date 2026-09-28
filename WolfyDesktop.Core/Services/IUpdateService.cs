namespace WolfyDesktop.Core.Services;

public interface IUpdateService
{
    /// <summary>False when running from a build folder instead of an installed copy.</summary>
    bool IsSupported { get; }

    string CurrentVersion { get; }

    /// <summary>Version found by the last successful <see cref="CheckAsync"/>, if any.</summary>
    string? AvailableVersion { get; }

    bool IsReadyToApply { get; }

    /// <summary>Returns true when a newer version is available.</summary>
    Task<bool> CheckAsync();

    Task DownloadAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Exits, installs the downloaded update and relaunches the app.</summary>
    void ApplyAndRestart();
}
