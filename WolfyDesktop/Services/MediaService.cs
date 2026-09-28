using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace WolfyDesktop.Services;

public class MediaService : IMediaService
{
    private MediaPlayer? _mediaPlayer;
    private bool _isLooping = true;
    private bool _disposed;

    public MediaPlayer? MediaPlayer => _mediaPlayer;
    
    public bool IsPlaying => _mediaPlayer?.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
    
    public bool IsPaused => _mediaPlayer?.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
    
    public bool IsInitialized => _mediaPlayer != null;

    public double Volume
    {
        get => _mediaPlayer?.Volume ?? 0.5;
        set
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Clamp(value, 0.0, 1.0);
            }
        }
    }

    public bool IsLooping
    {
        get => _isLooping;
        set => _isLooping = value;
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler? MediaEnded;

    public async Task<bool> InitializeAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            // Dispose existing player if any
            Reset();

            var file = await StorageFile.GetFileFromPathAsync(filePath);
            _mediaPlayer = new MediaPlayer
            {
                Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file)
            };

            _mediaPlayer.MediaEnded += OnMediaEnded;
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Play()
    {
        _mediaPlayer?.Play();
    }

    public void Pause()
    {
        _mediaPlayer?.Pause();
    }

    public void Stop()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            // Reset position to beginning
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }
    }

    public void Reset()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.MediaEnded -= OnMediaEnded;
            _mediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        MediaEnded?.Invoke(this, EventArgs.Empty);
        
        if (_isLooping)
        {
            sender.Play();
        }
    }

    private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
        {
            IsPlaying = sender.PlaybackState == MediaPlaybackState.Playing,
            IsPaused = sender.PlaybackState == MediaPlaybackState.Paused,
            IsStopped = sender.PlaybackState == MediaPlaybackState.None
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Reset();
        _disposed = true;
    }
}
