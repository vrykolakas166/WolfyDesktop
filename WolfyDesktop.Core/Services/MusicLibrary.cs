using WolfyDesktop.Core.Models;

namespace WolfyDesktop.Core.Services;

public sealed class MusicLibrary : IMusicLibrary
{
    /// <summary>File name of the track offered for download in the audio manager.</summary>
    public const string DownloadableTrackFileName = "lofi_rain.mp3";

    private static readonly string[] Extensions = [".mp3", ".m4a", ".wav", ".flac", ".aac", ".wma"];

    private readonly ISettingsStore _settings;

    public MusicLibrary(AppPaths paths, ISettingsStore settings)
    {
        FolderPath = paths.MusicFolder;
        _settings = settings;
    }

    public event EventHandler? Changed;

    public string FolderPath { get; }

    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public static bool IsSupported(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MusicTrack> GetTracks()
    {
        var defaultPath = GetDefaultTrackPath();
        return EnumerateAudioFiles()
            .Select(path => new MusicTrack(
                path,
                new FileInfo(path).Length,
                string.Equals(path, defaultPath, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public string? GetDefaultTrackPath()
    {
        if (_settings.Current.DefaultMusicFile is { Length: > 0 } configured)
        {
            // Settings only ever hold a bare file name; never follow a path out of the library.
            var configuredPath = Path.Combine(FolderPath, Path.GetFileName(configured));
            if (File.Exists(configuredPath))
            {
                return configuredPath;
            }
        }

        var downloadablePath = Path.Combine(FolderPath, DownloadableTrackFileName);
        if (File.Exists(downloadablePath))
        {
            return downloadablePath;
        }

        return EnumerateAudioFiles().FirstOrDefault();
    }

    public void SetDefaultTrack(string fileName)
    {
        _settings.Update(s => s.DefaultMusicFile = Path.GetFileName(fileName));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<string> ImportAsync(Stream source, string fileName, CancellationToken cancellationToken = default)
    {
        if (!IsSupported(fileName))
        {
            throw new NotSupportedException($"'{Path.GetExtension(fileName)}' files are not supported.");
        }

        EnsureFolderExists();
        var destination = GetAvailablePath(Path.GetFileName(fileName));

        await using (var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return destination;
    }

    public void Delete(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!string.Equals(Path.GetDirectoryName(fullPath), Path.GetFullPath(FolderPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only files inside the music library can be deleted.");
        }

        File.Delete(fullPath);

        if (string.Equals(_settings.Current.DefaultMusicFile, Path.GetFileName(fullPath), StringComparison.OrdinalIgnoreCase))
        {
            _settings.Update(s => s.DefaultMusicFile = null);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void EnsureFolderExists() => Directory.CreateDirectory(FolderPath);

    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private IEnumerable<string> EnumerateAudioFiles()
    {
        if (!Directory.Exists(FolderPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(FolderPath)
            .Where(IsSupported)
            .Order(StringComparer.OrdinalIgnoreCase);
    }

    private string GetAvailablePath(string fileName)
    {
        var candidate = Path.Combine(FolderPath, fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var counter = 1; File.Exists(candidate); counter++)
        {
            candidate = Path.Combine(FolderPath, $"{name}_{counter}{extension}");
        }

        return candidate;
    }
}
