using System;
using Microsoft.UI.Dispatching;

namespace WolfyDesktop.Services;

public class ClockService : IClockService
{
    private readonly DispatcherQueue _dispatcherQueue;
    private DispatcherQueueTimer? _timer;

    public event EventHandler<ClockUpdateEventArgs>? ClockUpdated;

    public bool IsRunning => _timer?.IsRunning ?? false;

    public ClockService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public void Start()
    {
        if (_timer != null && _timer.IsRunning)
            return;

        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
        
        // Fire immediately on start
        RaiseClockUpdated();
    }

    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        RaiseClockUpdated();
    }

    private void RaiseClockUpdated()
    {
        var now = DateTime.Now;
        ClockUpdated?.Invoke(this, new ClockUpdateEventArgs
        {
            Hours = now.ToString("HH"),
            Minutes = now.ToString("mm"),
            Seconds = now.ToString("ss")
        });
    }
}
