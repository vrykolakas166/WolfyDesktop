using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using WolfyDesktop.Models;
using WolfyDesktop.Services;

namespace WolfyDesktop.ViewModels;

public partial class AudioManagerDialogViewModel : ObservableObject
{
    private const string GOOGLE_DRIVE_FILE = "https://drive.usercontent.google.com/download?id=1jzex-vuQrpHUZXI_VhV34AyY97DkUc5c&export=download";
    private const string AUDIO_FILENAME = "lofi_rain.mp3";

    private readonly IFileService _fileService;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cancellationTokenSource;

    #region Observable Properties

    public ObservableCollection<MusicItem> MusicItems { get; } = [];

    [ObservableProperty]
    private MusicItem? _selectedMusicItem;

    [ObservableProperty]
    private string _statusIconGlyph = "\uE7BA"; // Warning

    [ObservableProperty]
    private Brush _statusIconForeground = new SolidColorBrush(Colors.Orange);

    [ObservableProperty]
    private string _statusText = "Checking status...";

    [ObservableProperty]
    private string _downloadButtonText = "Download Default Audio";

    [ObservableProperty]
    private bool _downloadButtonEnabled = true;

    [ObservableProperty]
    private bool _cancelButtonVisible;

    [ObservableProperty]
    private bool _progressBarVisible;

    [ObservableProperty]
    private bool _progressBarIndeterminate = true;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private Brush _progressTextForeground = new SolidColorBrush(Colors.White);

    [ObservableProperty]
    private bool _progressTextVisible;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _messageVisible;

    [ObservableProperty]
    private bool _isDownloading;

    #endregion

    #region Events

    public event EventHandler? DownloadCompleted;
    public event EventHandler<Exception>? DownloadFailed;

    #endregion

    public AudioManagerDialogViewModel(
        IFileService fileService,
        IDownloadService downloadService,
        ISettingsService settingsService)
    {
        _fileService = fileService;
        _downloadService = downloadService;
        _settingsService = settingsService;

        _downloadService.ProgressChanged += OnDownloadProgressChanged;
    }

    #region Initialization

    public async Task InitializeAsync()
    {
        await LoadMusicListAsync();
        await UpdateStatusAsync();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (IsDownloading)
            return;

        IsDownloading = true;
        _cancellationTokenSource = new CancellationTokenSource();

        DownloadButtonEnabled = false;
        CancelButtonVisible = true;
        ProgressBarVisible = true;
        ProgressBarIndeterminate = true;
        ProgressTextVisible = true;
        ProgressText = "Downloading...";
        ProgressTextForeground = new SolidColorBrush(Colors.White);

        try
        {
            _fileService.EnsureMusicsFolderExists();
            var destinationPath = Path.Combine(_fileService.MusicsFolderPath, AUDIO_FILENAME);

            var success = await _downloadService.DownloadFileAsync(
                GOOGLE_DRIVE_FILE,
                destinationPath,
                _cancellationTokenSource.Token);

            if (success)
            {
                StatusIconGlyph = "\uE930"; // Checkmark
                StatusIconForeground = new SolidColorBrush(Colors.Green);
                StatusText = $"Audio file downloaded successfully!\nLocation: {destinationPath}";
                DownloadButtonText = "Re-download Audio";
                ProgressTextForeground = new SolidColorBrush(Colors.Green);
                ProgressText = "Download complete!";

                await LoadMusicListAsync();
                DownloadCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Download canceled";
            ProgressTextForeground = new SolidColorBrush(Colors.Orange);
        }
        catch (Exception ex)
        {
            ProgressText = $"Error: {ex.Message}";
            ProgressTextForeground = new SolidColorBrush(Colors.Red);
            DownloadFailed?.Invoke(this, ex);
        }
        finally
        {
            IsDownloading = false;
            DownloadButtonEnabled = true;
            CancelButtonVisible = false;
            ProgressBarIndeterminate = false;
            ProgressBarVisible = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            await UpdateStatusAsync();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void CancelDownload()
    {
        if (IsDownloading)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainHandle);

            foreach (var ext in _fileService.SupportedExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                _fileService.EnsureMusicsFolderExists();

                foreach (var file in files)
                {
                    var destPath = Path.Combine(_fileService.MusicsFolderPath, file.Name);

                    // Handle duplicate file names
                    if (File.Exists(destPath))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file.Name);
                        var extension = Path.GetExtension(file.Name);
                        var counter = 1;

                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(_fileService.MusicsFolderPath, $"{fileName}_{counter}{extension}");
                            counter++;
                        }
                    }

                    await file.CopyAsync(
                        await StorageFolder.GetFolderFromPathAsync(_fileService.MusicsFolderPath),
                        Path.GetFileName(destPath),
                        NameCollisionOption.ReplaceExisting);
                }

                await LoadMusicListAsync();
                await UpdateStatusAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error browsing files: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSetDefault))]
    private async Task SetDefaultAsync()
    {
        if (SelectedMusicItem == null)
            return;

        _fileService.SetDefaultMusicPath(SelectedMusicItem.FileName);
        _settingsService.DefaultMusicFile = SelectedMusicItem.FileName;

        await LoadMusicListAsync();
    }

    private bool CanSetDefault() => SelectedMusicItem != null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (SelectedMusicItem == null)
            return;

        try
        {
            _fileService.DeleteFile(SelectedMusicItem.FullPath);
            await LoadMusicListAsync();
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
        }
    }

    private bool CanDelete() => SelectedMusicItem != null;

    [RelayCommand]
    private void OpenFolder()
    {
        _fileService.EnsureMusicsFolderExists();
        System.Diagnostics.Process.Start("explorer.exe", _fileService.MusicsFolderPath);
    }

    #endregion

    #region Public Methods

    public void SetMessage(string? message)
    {
        Message = message;
        MessageVisible = !string.IsNullOrEmpty(message);
    }

    public async Task LoadMusicListAsync()
    {
        MusicItems.Clear();

        var defaultMusic = _fileService.GetDefaultMusicPath();
        var musicFiles = _fileService.GetMusicFiles();

        foreach (var file in musicFiles)
        {
            var fileSize = _fileService.GetFileSize(file);

            MusicItems.Add(new MusicItem
            {
                FileName = Path.GetFileName(file),
                DisplayName = Path.GetFileNameWithoutExtension(file),
                FullPath = file,
                FileSize = fileSize,
                SizeText = _fileService.FormatFileSize(fileSize),
                IsDefault = file.Equals(defaultMusic, StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    public async Task UpdateStatusAsync()
    {
        if (MusicItems.Count == 0)
        {
            StatusIconGlyph = "\uE7BA"; // Warning
            StatusIconForeground = new SolidColorBrush(Colors.Orange);
            StatusText = "No music files found in library";
            DownloadButtonText = "Download Default Audio";
            return;
        }

        var defaultAudioPath = Path.Combine(_fileService.MusicsFolderPath, AUDIO_FILENAME);
        bool defaultExists = _fileService.FileExists(defaultAudioPath);

        if (defaultExists)
        {
            var isValid = await IsAudioFileValidAsync(defaultAudioPath);

            if (isValid)
            {
                StatusIconGlyph = "\uE930"; // Checkmark
                StatusIconForeground = new SolidColorBrush(Colors.Green);
                StatusText = $"Music library: {MusicItems.Count} file(s)";
                DownloadButtonText = "Re-download Default Audio";
            }
            else
            {
                StatusIconGlyph = "\uE7BA"; // Warning
                StatusIconForeground = new SolidColorBrush(Colors.Red);
                StatusText = "Default audio is corrupted";
                DownloadButtonText = "Re-download Default Audio";
            }
        }
        else
        {
            StatusIconGlyph = "\uE930"; // Checkmark
            StatusIconForeground = new SolidColorBrush(Colors.Green);
            StatusText = $"Music library: {MusicItems.Count} file(s)";
            DownloadButtonText = "Download Default Audio";
        }
    }

    #endregion

    #region Private Methods

    private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
    {
        ProgressBarIndeterminate = e.IsIndeterminate;
        if (!e.IsIndeterminate)
        {
            ProgressValue = e.ProgressPercentage;
            ProgressText = $"Downloading... {e.ProgressPercentage:F0}%";
        }
    }

    private static async Task<bool> IsAudioFileValidAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 1024) // Less than 1KB
            {
                return false;
            }

            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);

            using var testPlayer = new Windows.Media.Playback.MediaPlayer
            {
                Source = mediaSource
            };

            await Task.Delay(100);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Property Changed

    partial void OnSelectedMusicItemChanged(MusicItem? value)
    {
        SetDefaultCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    #endregion
}
