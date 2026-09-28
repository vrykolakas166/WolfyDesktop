using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace WolfyDesktop.Services;

/// <summary>
/// Owns a single <see cref="MediaPlayer"/> and reports its state on the UI thread
/// (the player raises its own events on a background thread).
/// </summary>
public sealed class MediaPlayerService : IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    private double _volume = 1.0;
    private int _loadVersion;

    public MediaPlayerService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    /// <summary>Raised on the UI thread when <see cref="IsPlaying"/> changes.</summary>
    public event EventHandler? IsPlayingChanged;

    public MediaPlayer? Player { get; private set; }

    public string? SourcePath { get; private set; }

    public bool IsPlaying { get; private set; }

    /// <summary>0–1. Kept across loads.</summary>
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            Player?.Volume = _volume;
        }
    }

    /// <param name="path">Full path of the media file.</param>
    /// <param name="loop">Uses the player's built-in looping, which has no gap between repeats.</param>
    /// <param name="enableSystemMediaControls">Lets media keys and the Windows media overlay control playback.</param>
    public async Task LoadAsync(string path, bool loop, bool enableSystemMediaControls)
    {
        Unload();
        var load = ++_loadVersion;

        var file = await StorageFile.GetFileFromPathAsync(path);
        if (load != _loadVersion)
        {
            // A newer load or an unload happened while opening the file.
            return;
        }

        var player = new MediaPlayer
        {
            AutoPlay = false,
            IsLoopingEnabled = loop,
            Volume = _volume,
            Source = MediaSource.CreateFromStorageFile(file),
        };
        player.CommandManager.IsEnabled = enableSystemMediaControls;
        player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;

        Player = player;
        SourcePath = path;
    }

    public void Play()
    {
        if (Player is not null)
        {
            Player.Play();
            SetIsPlaying(true);
        }
    }

    public void Pause()
    {
        Player?.Pause();
        SetIsPlaying(false);
    }

    /// <summary>Stops playback and releases the file so it can be deleted or replaced.</summary>
    public void Unload()
    {
        _loadVersion++;
        if (Player is not { } player)
        {
            return;
        }

        player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
        player.Pause();
        player.Source = null;
        player.Dispose();

        Player = null;
        SourcePath = null;
        SetIsPlaying(false);
    }

    public void Dispose() => Unload();

    private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        // Also catches pauses we did not ask for, e.g. from media keys or an unplugged headset.
        _dispatcherQueue.TryEnqueue(() =>
        {
            var state = Player?.PlaybackSession.PlaybackState;
            if (state is MediaPlaybackState.Playing or MediaPlaybackState.Paused)
            {
                SetIsPlaying(state == MediaPlaybackState.Playing);
            }
        });
    }

    private void SetIsPlaying(bool value)
    {
        if (IsPlaying != value)
        {
            IsPlaying = value;
            IsPlayingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
