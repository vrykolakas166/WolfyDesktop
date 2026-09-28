using Microsoft.UI.Dispatching;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Services;

/// <summary>
/// Ticks on the UI thread once per second, re-aligned to the wall clock on every
/// tick so the displayed seconds never drift, skip or repeat.
/// </summary>
public sealed class ClockService
{
    private readonly DispatcherQueueTimer _timer;

    public ClockService(DispatcherQueue dispatcherQueue)
    {
        _timer = dispatcherQueue.CreateTimer();
        _timer.IsRepeating = false;
        _timer.Tick += (_, _) => RaiseAndScheduleNext();
    }

    public event EventHandler<DateTime>? Tick;

    public void Start()
    {
        if (!_timer.IsRunning)
        {
            RaiseAndScheduleNext();
        }
    }

    public void Stop() => _timer.Stop();

    private void RaiseAndScheduleNext()
    {
        Tick?.Invoke(this, DateTime.Now);
        _timer.Interval = ClockSchedule.DelayUntilNextSecond(DateTime.Now);
        _timer.Start();
    }
}
