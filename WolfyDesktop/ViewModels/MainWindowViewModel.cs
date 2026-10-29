using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Media.Playback;
using WolfyDesktop.Core.Models;
using WolfyDesktop.Core.Services;
using WolfyDesktop.Services;

namespace WolfyDesktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    /// <summary>Gives startup some breathing room before touching the network.</summary>
    private static readonly TimeSpan StartupUpdateCheckDelay = TimeSpan.FromSeconds(10);

    private readonly ClockService _clock;
    private readonly MediaPlayerService _video;
    private readonly BackgroundAudioService _audio;
    private readonly ISettingsStore _settings;
    private readonly IUpdateService _updates;
    private bool _isUpdating;
    private bool _isChilled;

    public MainWindowViewModel(
        ClockService clock,
        MediaPlayerService video,
        BackgroundAudioService audio,
        ISettingsStore settings,
        IUpdateService updates)
    {
        _clock = clock;
        _video = video;
        _audio = audio;
        _settings = settings;
        _updates = updates;

        SelectedTheme = settings.Current.Theme;
        Volume = settings.Current.Volume;
        ControlsOpacity = settings.Current.ControlsOpacity;
        VersionText = $"Version {updates.CurrentVersion}";
        UpdateStatusText = updates.IsSupported ? string.Empty : "Updates are available in the installed app.";

        _audio.IsPlayingChanged += OnAudioIsPlayingChanged;
        _audio.HasTrackChanged += OnAudioHasTrackChanged;
        _clock.Tick += OnClockTick;
        _clock.Start();
    }

    [ObservableProperty]
    public partial DateTime CurrentTime { get; set; }

    [ObservableProperty]
    public partial AppTheme SelectedTheme { get; set; }

    /// <summary>0–100.</summary>
    [ObservableProperty]
    public partial double Volume { get; set; }

    /// <summary>The chill-mode video, or null when chill mode is off.</summary>
    [ObservableProperty]
    public partial MediaPlayer? VideoPlayer { get; set; }

    [ObservableProperty]
    public partial string UpdateStatusText { get; set; }

    /// <summary>Opacity of the play/pause and volume controls when shown, 0–100. At 0 they never show.</summary>
    [ObservableProperty]
    public partial double ControlsOpacity { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadingUpdate { get; set; }

    [ObservableProperty]
    public partial double UpdateDownloadProgress { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestartToUpdateCommand))]
    public partial bool IsUpdateReady { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateBannerOpen { get; set; }

    public string VersionText { get; }

    public bool IsUpdateSupported => _updates.IsSupported;

    public bool IsAudioPlaying => _audio.IsPlaying;

    public bool HasAudioTrack => _audio.HasTrack;

    /// <summary>Whether the window opens full screen, as it was last left.</summary>
    public bool StartsFullScreen => _settings.Current.IsFullScreen;

    /// <summary>Where the window was last windowed, or null if it never was.</summary>
    public WindowBounds? SavedWindowBounds => _settings.Current.WindowBounds;

    /// <summary>Remembers the window state for the next launch.</summary>
    /// <param name="isFullScreen">Whether the window is full screen now.</param>
    /// <param name="windowedBounds">Null keeps the saved bounds, e.g. while full screen.</param>
    public void SaveWindowState(bool isFullScreen, WindowBounds? windowedBounds)
    {
        var current = _settings.Current;
        var bounds = windowedBounds ?? current.WindowBounds;
        if (current.IsFullScreen != isFullScreen || current.WindowBounds != bounds)
        {
            _settings.Update(s =>
            {
                s.IsFullScreen = isFullScreen;
                s.WindowBounds = bounds;
            });
        }
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        if (_settings.Current.Theme != value)
        {
            _settings.Update(s => s.Theme = value);
        }
    }

    partial void OnVolumeChanged(double value)
    {
        var clamped = Math.Clamp(value, 0, 100);
        if (clamped != value)
        {
            Volume = clamped;
            return;
        }

        _audio.Volume = value;
        if (_settings.Current.Volume != value)
        {
            _settings.Update(s => s.Volume = value);
        }
    }

    partial void OnControlsOpacityChanged(double value)
    {
        var clamped = Math.Clamp(value, 0, 100);
        if (clamped != value)
        {
            ControlsOpacity = clamped;
            return;
        }

        if (_settings.Current.ControlsOpacity != value)
        {
            _settings.Update(s => s.ControlsOpacity = value);
        }
    }

    [RelayCommand]
    private async Task ToggleChillModeAsync()
    {
        if (_isChilled)
        {
            // Detach the video from the view before the player is disposed.
            VideoPlayer = null;
            _video.Unload();
            _audio.Pause();
            _isChilled = false;
            return;
        }

        var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "background.mp4");
        if (!File.Exists(videoPath))
        {
            return;
        }

        await _video.LoadAsync(videoPath, loop: true, enableSystemMediaControls: false);
        VideoPlayer = _video.Player;
        _video.Play();
        _isChilled = true;

        await _audio.PlayAsync();
    }

    [RelayCommand]
    private Task TogglePlayPauseAsync() => _audio.TogglePlayPauseAsync();

    [RelayCommand]
    private void VolumeUp() => Volume = Math.Min(Volume + 5, 100);

    [RelayCommand]
    private void VolumeDown() => Volume = Math.Max(Volume - 5, 0);

    /// <summary>Stops decoding the video while nobody can see it.</summary>
    public void OnWindowVisibilityChanged(bool isVisible)
    {
        if (!_isChilled)
        {
            return;
        }

        if (isVisible)
        {
            _video.Play();
        }
        else
        {
            _video.Pause();
        }
    }

    /// <summary>Checks once, shortly after startup, and downloads any update quietly.</summary>
    public async Task CheckForUpdatesInBackgroundAsync()
    {
        if (!_updates.IsSupported)
        {
            return;
        }

        await Task.Delay(StartupUpdateCheckDelay);
        await RunUpdateAsync(userInitiated: false);
    }

    [RelayCommand]
    private Task CheckForUpdatesAsync() => RunUpdateAsync(userInitiated: true);

    [RelayCommand(CanExecute = nameof(IsUpdateReady))]
    private async Task RestartToUpdateAsync()
    {
        await _settings.FlushAsync();
        _updates.ApplyAndRestart();
    }

    private async Task RunUpdateAsync(bool userInitiated)
    {
        if (IsUpdateReady)
        {
            IsUpdateBannerOpen |= userInitiated;
            return;
        }

        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            UpdateStatusText = "Checking for updates…";
            if (!await _updates.CheckAsync())
            {
                UpdateStatusText = "You're on the latest version.";
                return;
            }

            UpdateStatusText = $"Downloading version {_updates.AvailableVersion}…";
            UpdateDownloadProgress = 0;
            IsDownloadingUpdate = true;
            await _updates.DownloadAsync(new Progress<int>(percent => UpdateDownloadProgress = percent));

            UpdateStatusText = $"Version {_updates.AvailableVersion} is ready. Restart to install it.";
            IsUpdateReady = true;
            IsUpdateBannerOpen = true;
        }
        catch (Exception ex)
        {
            // Usually just offline. Stay quiet unless the user asked.
            Debug.WriteLine($"Update failed: {ex}");
            UpdateStatusText = userInitiated ? $"Couldn't check for updates: {ex.Message}" : string.Empty;
        }
        finally
        {
            IsDownloadingUpdate = false;
            _isUpdating = false;
        }
    }

    private void OnClockTick(object? sender, DateTime now) => CurrentTime = now;

    private void OnAudioIsPlayingChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(IsAudioPlaying));

    private void OnAudioHasTrackChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(HasAudioTrack));

    public void Dispose()
    {
        _clock.Tick -= OnClockTick;
        _clock.Stop();
        _audio.IsPlayingChanged -= OnAudioIsPlayingChanged;
        _audio.HasTrackChanged -= OnAudioHasTrackChanged;
        VideoPlayer = null;
        _video.Dispose();
    }
}
