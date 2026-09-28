using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.System;
using WolfyDesktop.Core.Models;
using WolfyDesktop.Core.Services;
using WolfyDesktop.Services;

namespace WolfyDesktop.ViewModels;

public enum StatusKind
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed partial class AudioManagerViewModel : ObservableObject
{
    private static readonly Uri DownloadableTrackUri =
        new("https://drive.usercontent.google.com/download?id=1jzex-vuQrpHUZXI_VhV34AyY97DkUc5c&export=download");

    /// <summary>The media player can take a moment to let go of a file after it stops.</summary>
    private static readonly TimeSpan FileReleaseDelay = TimeSpan.FromMilliseconds(300);

    private readonly IMusicLibrary _library;
    private readonly IDownloadService _downloads;
    private readonly BackgroundAudioService _audio;

    public AudioManagerViewModel(IMusicLibrary library, IDownloadService downloads, BackgroundAudioService audio)
    {
        _library = library;
        _downloads = downloads;
        _audio = audio;

        StatusText = string.Empty;
        DownloadMessage = string.Empty;
        DownloadButtonText = "Download Lofi Rain";
    }

    public ObservableCollection<MusicTrack> Tracks { get; } = [];

    public string FolderPath => _library.FolderPath;

    public IReadOnlyList<string> SupportedExtensions => _library.SupportedExtensions;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetDefaultCommand), nameof(DeleteCommand))]
    public partial MusicTrack? SelectedTrack { get; set; }

    [ObservableProperty]
    public partial StatusKind Status { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; }

    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadProgressIndeterminate { get; set; }

    [ObservableProperty]
    public partial double DownloadProgress { get; set; }

    [ObservableProperty]
    public partial StatusKind DownloadMessageKind { get; set; }

    [ObservableProperty]
    public partial string DownloadMessage { get; set; }

    [ObservableProperty]
    public partial string DownloadButtonText { get; set; }

    /// <summary>Reloads the track list and the library status.</summary>
    public void Refresh()
    {
        Tracks.Clear();
        foreach (var track in _library.GetTracks())
        {
            Tracks.Add(track);
        }

        SelectedTrack = null;

        var downloadablePath = Path.Combine(_library.FolderPath, MusicLibrary.DownloadableTrackFileName);
        var hasDownloadable = File.Exists(downloadablePath);
        DownloadButtonText = hasDownloadable ? "Download Lofi Rain again" : "Download Lofi Rain";

        if (hasDownloadable && !AudioFileInspector.LooksLikeMp3(downloadablePath))
        {
            SetStatus(StatusKind.Error, "Lofi Rain is damaged. Download it again.");
        }
        else if (Tracks.Count == 0)
        {
            SetStatus(StatusKind.Warning, "Your library is empty. Download Lofi Rain or add your own music.");
        }
        else
        {
            SetStatus(StatusKind.Success, Tracks.Count == 1 ? "1 track in your library" : $"{Tracks.Count} tracks in your library");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        var destination = Path.Combine(_library.FolderPath, MusicLibrary.DownloadableTrackFileName);
        var wasPlaying = _audio.Release(destination);

        IsDownloading = true;
        IsDownloadProgressIndeterminate = true;
        DownloadProgress = 0;
        SetDownloadMessage(StatusKind.Info, "Downloading…");

        var progress = new Progress<DownloadProgress>(p =>
        {
            IsDownloadProgressIndeterminate = p.Percent is null;
            DownloadProgress = p.Percent ?? 0;
            DownloadMessage = p.Percent is { } percent
                ? $"Downloading… {percent:F0}%"
                : $"Downloading… {p.BytesReceived / (1024.0 * 1024.0):F1} MB";
        });

        try
        {
            await _downloads.DownloadAsync(DownloadableTrackUri, destination, progress, cancellationToken);

            if (AudioFileInspector.LooksLikeMp3(destination))
            {
                SetDownloadMessage(StatusKind.Success, "Download complete.");
            }
            else
            {
                File.Delete(destination);
                SetDownloadMessage(StatusKind.Error, "The download wasn't a valid audio file. Try again later.");
            }
        }
        catch (OperationCanceledException)
        {
            SetDownloadMessage(StatusKind.Warning, "Download cancelled.");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            SetDownloadMessage(StatusKind.Error, $"Download failed: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
        }

        _library.NotifyChanged();
        Refresh();
        if (wasPlaying)
        {
            await _audio.PlayAsync();
        }
    }

    public async Task ImportAsync(IReadOnlyList<StorageFile> files)
    {
        var failed = new List<string>();
        foreach (var file in files)
        {
            try
            {
                await using var stream = await file.OpenStreamForReadAsync();
                await _library.ImportAsync(stream, file.Name);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                failed.Add(file.Name);
            }
        }

        Refresh();
        if (failed.Count > 0)
        {
            SetStatus(StatusKind.Error, $"Couldn't add: {string.Join(", ", failed)}");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SetDefault()
    {
        if (SelectedTrack is { } track)
        {
            _library.SetDefaultTrack(track.FileName);
            Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedTrack is not { } track)
        {
            return;
        }

        var wasPlaying = _audio.Release(track.FullPath);
        try
        {
            try
            {
                _library.Delete(track.FullPath);
            }
            catch (IOException)
            {
                await Task.Delay(FileReleaseDelay);
                _library.Delete(track.FullPath);
            }

            Refresh();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Refresh();
            SetStatus(StatusKind.Error, $"Couldn't remove {track.DisplayName}: {ex.Message}");
        }

        if (wasPlaying)
        {
            await _audio.PlayAsync();
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        _library.EnsureFolderExists();
        await Launcher.LaunchFolderPathAsync(_library.FolderPath);
    }

    private bool HasSelection() => SelectedTrack is not null;

    private void SetStatus(StatusKind kind, string text)
    {
        Status = kind;
        StatusText = text;
    }

    private void SetDownloadMessage(StatusKind kind, string text)
    {
        DownloadMessageKind = kind;
        DownloadMessage = text;
    }
}
