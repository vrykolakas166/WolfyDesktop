using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace WolfyDesktop.Services;

public interface IMediaService : IDisposable
{
    MediaPlayer? MediaPlayer { get; }
    bool IsPlaying { get; }
    bool IsPaused { get; }
    bool IsInitialized { get; }
    double Volume { get; set; }
    bool IsLooping { get; set; }
    
    event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    event EventHandler? MediaEnded;
    
    Task<bool> InitializeAsync(string filePath);
    void Play();
    void Pause();
    void Stop();
    void Reset();
}
