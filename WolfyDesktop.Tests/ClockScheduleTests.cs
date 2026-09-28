using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Tests;

public class ClockScheduleTests
{
    [TestCase(0, 1000)]
    [TestCase(1, 999)]
    [TestCase(500, 500)]
    [TestCase(999, 1)]
    public void DelayUntilNextSecond_LandsJustAfterTheNextBoundary(int millisecond, int expectedMilliseconds)
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, millisecond);

        var delay = ClockSchedule.DelayUntilNextSecond(now);

        Assert.That(delay, Is.EqualTo(TimeSpan.FromMilliseconds(expectedMilliseconds) + ClockSchedule.TickOffset));
    }

    [Test]
    public void DelayUntilNextSecond_TickFallsInsideTheNextSecond()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 41, 873);

        var tickTime = now + ClockSchedule.DelayUntilNextSecond(now);

        Assert.That(tickTime.Second, Is.EqualTo(42));
    }
}
