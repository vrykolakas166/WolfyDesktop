using System;

namespace WolfyDesktop.Services;

public interface IClockService
{
    event EventHandler<ClockUpdateEventArgs>? ClockUpdated;
    
    bool IsRunning { get; }
    
    void Start();
    void Stop();
}
