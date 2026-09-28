using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Windows.Media.Playback;
using Windows.System;
using WolfyDesktop.Services;

namespace WolfyDesktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IClockService _clockService;
    private readonly IMediaService _videoMediaService;
    private readonly IMediaService _audioMediaService;
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;

    #region Observable Properties

    [ObservableProperty]
    private string _currentHours = "00";

    [ObservableProperty]
    private string _currentMinutes = "00";

    [ObservableProperty]
    private string _currentSeconds = "00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeUpperCase))]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _isChilled;

    [ObservableProperty]
    private bool _isAudioPlayerVisible;

    [ObservableProperty]
    private double _volume = 50;

    [ObservableProperty]
    private string _playPauseIcon = "\uE768"; // Play icon

    [ObservableProperty]
    private string _versionInfo = string.Empty;

    [ObservableProperty]
    private bool _isWelcomePanelVisible = true;

    #endregion

    #region Computed Properties

    public string ThemeUpperCase => SelectedTheme.ToUpperInvariant();

    public MediaPlayer? VideoMediaPlayer => _videoMediaService.MediaPlayer;

    public MediaPlayer? BackgroundAudioMediaPlayer => _audioMediaService.MediaPlayer;

    #endregion

    #region Events

    public event EventHandler? ExitRequested;
    public event EventHandler? FullScreenToggleRequested;
    public event EventHandler? PackageManagerRequested;
    public event EventHandler? UserActivityDetected;
    public event EventHandler<string>? ClockUpdated;

    #endregion

    public MainWindowViewModel(
        IClockService clockService,
        IMediaService videoMediaService,
        IMediaService backgroundMediaService,
        IThemeService themeService,
        ISettingsService settingsService,
        IFileService fileService)
    {
        _clockService = clockService;
        _videoMediaService = videoMediaService;
        _audioMediaService = backgroundMediaService;
        _themeService = themeService;
        _settingsService = settingsService;
        _fileService = fileService;

        // Subscribe to service events
        _clockService.ClockUpdated += OnClockUpdated;
        _audioMediaService.PlaybackStateChanged += OnAudioPlaybackStateChanged;

        // Load settings
        SelectedTheme = _settingsService.Theme;
        Volume = _settingsService.Volume;

        // Start clock
        _clockService.Start();

        // Update version info
        UpdateVersionInfo();
    }

    #region Event Handlers

    private void OnClockUpdated(object? sender, ClockUpdateEventArgs e)
    {
        CurrentHours = e.Hours;
        CurrentMinutes = e.Minutes;
        CurrentSeconds = e.Seconds;
        ClockUpdated?.Invoke(this, $"{e.Hours}:{e.Minutes}:{e.Seconds}");
    }

    private void OnAudioPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        PlayPauseIcon = !e.IsPlaying ? "\uE769" : "\uE768"; // Pause : Play
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedThemeChanged(string value)
    {
        _settingsService.Theme = value;
    }

    partial void OnVolumeChanged(double value)
    {
        _settingsService.Volume = value;
        _audioMediaService.Volume = value / 100.0;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void FocusMode()
    {
        FullScreenToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ChillModeAsync()
    {
        try
        {
            if (IsChilled)
            {
                // Stop video
                _videoMediaService.Reset();
                OnPropertyChanged(nameof(VideoMediaPlayer));

                // Pause audio (don't reset - user might want to keep playing)
                if (_audioMediaService.IsPlaying)
                {
                    _audioMediaService.Pause();
                }

                IsChilled = false;
            }
            else
            {
                // Load video
                var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "background.mp4");
                var videoInitialized = await _videoMediaService.InitializeAsync(videoPath);

                if (videoInitialized)
                {
                    _videoMediaService.Play();
                    OnPropertyChanged(nameof(VideoMediaPlayer));

                    // Load and play audio if available
                    var defaultMusicPath = _fileService.GetDefaultMusicPath();
                    if (!string.IsNullOrEmpty(defaultMusicPath) && _fileService.FileExists(defaultMusicPath))
                    {
                        if (!_audioMediaService.IsInitialized)
                        {
                            await InitializeBackgroundAudioAsync(defaultMusicPath);
                        }
                        else if (!_audioMediaService.IsPlaying)
                        {
                            _audioMediaService.Play();
                        }
                    }

                    IsChilled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ChillMode error: {ex.Message}");
            // Reset state on error
            IsChilled = false;
        }
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        OnUserActivity();

        var defaultMusicPath = _fileService.GetDefaultMusicPath();
        if (string.IsNullOrEmpty(defaultMusicPath) || !_fileService.FileExists(defaultMusicPath))
        {
            return;
        }

        if (!_audioMediaService.IsInitialized)
        {
            await InitializeBackgroundAudioAsync(defaultMusicPath);
        }
        else
        {
            if (_audioMediaService.IsPlaying)
            {
                _audioMediaService.Pause();
            }
            else
            {
                _audioMediaService.Play();
            }
        }
    }

    [RelayCommand]
    private void VolumeUp()
    {
        OnUserActivity();
        Volume = Math.Min(Volume + 5, 100);
    }

    [RelayCommand]
    private void VolumeDown()
    {
        OnUserActivity();
        Volume = Math.Max(Volume - 5, 0);
    }

    [RelayCommand]
    private void Exit()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CheckUpdate()
    {
        UpdateVersionInfo();
    }

    [RelayCommand]
    private void ShowPackageManager()
    {
        PackageManagerRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods

    public void OnUserActivity()
    {
        var defaultMusicPath = _fileService.GetDefaultMusicPath();
        if (!IsAudioPlayerVisible && !string.IsNullOrEmpty(defaultMusicPath) && _fileService.FileExists(defaultMusicPath))
        {
            IsAudioPlayerVisible = true;
        }
        UserActivityDetected?.Invoke(this, EventArgs.Empty);
    }

    public void HideAudioPlayer()
    {
        IsAudioPlayerVisible = false;
    }

    public void HideWelcomePanel()
    {
        IsWelcomePanelVisible = false;
    }

    public bool HasDefaultMusicFile()
    {
        var path = _fileService.GetDefaultMusicPath();
        return !string.IsNullOrEmpty(path) && _fileService.FileExists(path);
    }

    public void ApplyTheme(Microsoft.UI.Xaml.FrameworkElement element)
    {
        _themeService.ApplyTheme(element, SelectedTheme);
    }

    #endregion

    #region Private Methods

    private async Task InitializeBackgroundAudioAsync(string path)
    {
        var initialized = await _audioMediaService.InitializeAsync(path);
        if (initialized)
        {
            PlayPauseIcon = "\uE769";
            _audioMediaService.Volume = Volume / 100.0;
            _audioMediaService.Play();
            OnPropertyChanged(nameof(BackgroundAudioMediaPlayer));
        }
    }

    private void UpdateVersionInfo()
    {
        try
        {
            var exePath = Environment.ProcessPath!;
            var version = FileVersionInfo.GetVersionInfo(exePath).FileVersion;
            VersionInfo = $"Version: {version}";
        }
        catch
        {
            VersionInfo = "Version: Unknown";
        }
    }

    #endregion
}
