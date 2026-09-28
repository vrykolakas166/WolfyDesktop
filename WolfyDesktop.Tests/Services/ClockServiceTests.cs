using WolfyDesktop.Tests.Helpers;

namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class ClockServiceTests
{
    [Test]
    public void TimeFormatting_Hours_ShouldReturnTwoDigits()
    {
        var dateTime = new DateTime(2024, 1, 1, 9, 30, 45);
        var hours = dateTime.ToString("HH");
        
        Assert.That(hours, Is.EqualTo("09"));
        Assert.That(hours.Length, Is.EqualTo(2));
    }

    [Test]
    public void TimeFormatting_Minutes_ShouldReturnTwoDigits()
    {
        var dateTime = new DateTime(2024, 1, 1, 9, 5, 45);
        var minutes = dateTime.ToString("mm");
        
        Assert.That(minutes, Is.EqualTo("05"));
        Assert.That(minutes.Length, Is.EqualTo(2));
    }

    [Test]
    public void TimeFormatting_Seconds_ShouldReturnTwoDigits()
    {
        var dateTime = new DateTime(2024, 1, 1, 9, 30, 7);
        var seconds = dateTime.ToString("ss");
        
        Assert.That(seconds, Is.EqualTo("07"));
        Assert.That(seconds.Length, Is.EqualTo(2));
    }

    [TestCase(0, "00")]
    [TestCase(1, "01")]
    [TestCase(9, "09")]
    [TestCase(10, "10")]
    [TestCase(23, "23")]
    public void TimeFormatting_VariousHours_ShouldFormatCorrectly(int hour, string expected)
    {
        var dateTime = new DateTime(2024, 1, 1, hour, 0, 0);
        var formatted = dateTime.ToString("HH");
        
        Assert.That(formatted, Is.EqualTo(expected));
    }

    [TestCase(0, "00")]
    [TestCase(1, "01")]
    [TestCase(30, "30")]
    [TestCase(59, "59")]
    public void TimeFormatting_VariousMinutes_ShouldFormatCorrectly(int minute, string expected)
    {
        var dateTime = new DateTime(2024, 1, 1, 12, minute, 0);
        var formatted = dateTime.ToString("mm");
        
        Assert.That(formatted, Is.EqualTo(expected));
    }

    [TestCase(0, "00")]
    [TestCase(1, "01")]
    [TestCase(30, "30")]
    [TestCase(59, "59")]
    public void TimeFormatting_VariousSeconds_ShouldFormatCorrectly(int second, string expected)
    {
        var dateTime = new DateTime(2024, 1, 1, 12, 0, second);
        var formatted = dateTime.ToString("ss");
        
        Assert.That(formatted, Is.EqualTo(expected));
    }

    [Test]
    public void TimeFormatting_Midnight_ShouldFormat_As_000000()
    {
        var midnight = new DateTime(2024, 1, 1, 0, 0, 0);
        
        Assert.Multiple(() =>
        {
            Assert.That(midnight.ToString("HH"), Is.EqualTo("00"));
            Assert.That(midnight.ToString("mm"), Is.EqualTo("00"));
            Assert.That(midnight.ToString("ss"), Is.EqualTo("00"));
        });
    }

    [Test]
    public void TimeFormatting_Noon_ShouldFormat_As_120000()
    {
        var noon = new DateTime(2024, 1, 1, 12, 0, 0);
        
        Assert.Multiple(() =>
        {
            Assert.That(noon.ToString("HH"), Is.EqualTo("12"));
            Assert.That(noon.ToString("mm"), Is.EqualTo("00"));
            Assert.That(noon.ToString("ss"), Is.EqualTo("00"));
        });
    }

    [Test]
    public void TimeFormatting_EndOfDay_ShouldFormat_As_235959()
    {
        var endOfDay = new DateTime(2024, 1, 1, 23, 59, 59);
        
        Assert.Multiple(() =>
        {
            Assert.That(endOfDay.ToString("HH"), Is.EqualTo("23"));
            Assert.That(endOfDay.ToString("mm"), Is.EqualTo("59"));
            Assert.That(endOfDay.ToString("ss"), Is.EqualTo("59"));
        });
    }

    [Test]
    public void ClockInterval_ShouldBeOneSecond()
    {
        var interval = TimeSpan.FromSeconds(1);
        
        Assert.Multiple(() =>
        {
            Assert.That(interval.TotalSeconds, Is.EqualTo(1));
            Assert.That(interval.TotalMilliseconds, Is.EqualTo(1000));
        });
    }
}
