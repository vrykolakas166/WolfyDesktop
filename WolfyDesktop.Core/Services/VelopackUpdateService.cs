using Velopack;
using Velopack.Sources;

namespace WolfyDesktop.Core.Services;

/// <summary>
/// Updates from GitHub Releases. Velopack downloads delta packages when it can,
/// so most updates are a few megabytes.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    public const string RepositoryUrl = "https://github.com/vrykolakas166/WolfyDesktop";

    private readonly UpdateManager _manager = new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
    private UpdateInfo? _update;

    public bool IsSupported => _manager.IsInstalled;

    public string CurrentVersion =>
        _manager.CurrentVersion?.ToString()
        ?? typeof(VelopackUpdateService).Assembly.GetName().Version?.ToString(3)
        ?? "unknown";

    public string? AvailableVersion => _update?.TargetFullRelease.Version.ToString();

    public bool IsReadyToApply { get; private set; }

    public async Task<bool> CheckAsync()
    {
        if (!IsSupported)
        {
            return false;
        }

        _update = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
        IsReadyToApply = false;
        return _update is not null;
    }

    public async Task DownloadAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var update = _update ?? throw new InvalidOperationException("No update available. Call CheckAsync first.");
        await _manager.DownloadUpdatesAsync(update, p => progress?.Report(p), cancellationToken).ConfigureAwait(false);
        IsReadyToApply = true;
    }

    public void ApplyAndRestart()
    {
        if (_update is not null && IsReadyToApply)
        {
            _manager.ApplyUpdatesAndRestart(_update.TargetFullRelease);
        }
    }
}
