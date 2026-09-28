using System;

namespace WolfyDesktop.Services;

public class PlaybackStateChangedEventArgs : EventArgs
{
    public bool IsPlaying { get; init; }
    public bool IsPaused { get; init; }
    public bool IsStopped { get; init; }
}
