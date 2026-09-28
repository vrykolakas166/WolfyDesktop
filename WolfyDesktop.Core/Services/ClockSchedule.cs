namespace WolfyDesktop.Core.Services;

public static class ClockSchedule
{
    /// <summary>
    /// Timers fire slightly late or early; aiming just past the boundary keeps each
    /// tick inside the second it is meant to display.
    /// </summary>
    public static readonly TimeSpan TickOffset = TimeSpan.FromMilliseconds(15);

    /// <summary>
    /// Delay from <paramref name="now"/> until just after the next whole second.
    /// A fixed one-second interval drifts and can skip or repeat a displayed second.
    /// </summary>
    public static TimeSpan DelayUntilNextSecond(DateTime now)
    {
        var remainder = TimeSpan.TicksPerSecond - (now.Ticks % TimeSpan.TicksPerSecond);
        return TimeSpan.FromTicks(remainder) + TickOffset;
    }
}
