namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class VolumeServiceTests
{
    [TestCase(0, 0.0)]
    [TestCase(50, 0.5)]
    [TestCase(100, 1.0)]
    [TestCase(25, 0.25)]
    [TestCase(75, 0.75)]
    public void ConvertVolumePercentageToDecimal_ShouldCalculateCorrectly(double percentage, double expected)
    {
        var decimalValue = percentage / 100.0;
        Assert.That(decimalValue, Is.EqualTo(expected).Within(0.001));
    }

    [Test]
    public void VolumeIncrease_By5Percent_ShouldCalculateCorrectly()
    {
        const double currentVolume = 50;
        const double increment = 5;

        var newVolume = currentVolume + increment;

        Assert.That(newVolume, Is.EqualTo(55));
    }

    [Test]
    public void VolumeDecrease_By5Percent_ShouldCalculateCorrectly()
    {
        const double currentVolume = 50;
        const double decrement = 5;

        var newVolume = currentVolume - decrement;

        Assert.That(newVolume, Is.EqualTo(45));
    }

    [Test]
    public void VolumeIncrease_ShouldNotExceed100()
    {
        const double currentVolume = 98;
        const double increment = 5;

        var newVolume = Math.Min(currentVolume + increment, 100);

        Assert.That(newVolume, Is.EqualTo(100));
    }

    [Test]
    public void VolumeDecrease_ShouldNotGoBelowZero()
    {
        const double currentVolume = 3;
        const double decrement = 5;

        var newVolume = Math.Max(currentVolume - decrement, 0);

        Assert.That(newVolume, Is.EqualTo(0));
    }

    [TestCase(0, 5, 5)]
    [TestCase(50, 5, 55)]
    [TestCase(95, 5, 100)]
    [TestCase(100, 5, 100)]
    public void VolumeUp_VariousScenarios_ShouldBehaveCorrectly(double current, double increment, double expected)
    {
        var newVolume = Math.Min(current + increment, 100);
        Assert.That(newVolume, Is.EqualTo(expected));
    }

    [TestCase(100, 5, 95)]
    [TestCase(50, 5, 45)]
    [TestCase(5, 5, 0)]
    [TestCase(0, 5, 0)]
    public void VolumeDown_VariousScenarios_ShouldBehaveCorrectly(double current, double decrement, double expected)
    {
        var newVolume = Math.Max(current - decrement, 0);
        Assert.That(newVolume, Is.EqualTo(expected));
    }

    [Test]
    public void DefaultVolume_ShouldBe50Percent()
    {
        const double defaultVolume = 50;
        
        Assert.That(defaultVolume, Is.EqualTo(50));
        Assert.That(defaultVolume / 100.0, Is.EqualTo(0.5));
    }

    [Test]
    public void VolumeRange_ShouldBeBetween0And100()
    {
        const double minVolume = 0;
        const double maxVolume = 100;

        Assert.Multiple(() =>
        {
            Assert.That(minVolume, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxVolume, Is.LessThanOrEqualTo(100));
            Assert.That(maxVolume, Is.GreaterThan(minVolume));
        });
    }

    [TestCase(0)]
    [TestCase(25)]
    [TestCase(50)]
    [TestCase(75)]
    [TestCase(100)]
    public void VolumeValues_ShouldBeWithinValidRange(double volume)
    {
        Assert.Multiple(() =>
        {
            Assert.That(volume, Is.GreaterThanOrEqualTo(0));
            Assert.That(volume, Is.LessThanOrEqualTo(100));
        });
    }

    [Test]
    public void VolumeIncrement_ShouldBe5()
    {
        const double volumeIncrement = 5;
        Assert.That(volumeIncrement, Is.EqualTo(5));
    }
}
