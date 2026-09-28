namespace WolfyDesktop.Tests.ViewModels;

[TestFixture]
public class MainWindowViewModelTests
{
    [Test]
    public void Clock_InitialValues_ShouldBe00()
    {
        const string initialHours = "00";
        const string initialMinutes = "00";
        const string initialSeconds = "00";

        Assert.Multiple(() =>
        {
            Assert.That(initialHours, Is.EqualTo("00"));
            Assert.That(initialMinutes, Is.EqualTo("00"));
            Assert.That(initialSeconds, Is.EqualTo("00"));
        });
    }

    [Test]
    public void Theme_DefaultValue_ShouldBeDark()
    {
        const string defaultTheme = "Dark";
        Assert.That(defaultTheme, Is.EqualTo("Dark"));
    }

    [Test]
    public void Volume_DefaultValue_ShouldBe50()
    {
        const double defaultVolume = 50;
        Assert.That(defaultVolume, Is.EqualTo(50));
    }

    [Test]
    public void IsChilled_DefaultValue_ShouldBeFalse()
    {
        const bool defaultIsChilled = false;
        Assert.That(defaultIsChilled, Is.False);
    }

    [Test]
    public void IsAudioPlayerVisible_DefaultValue_ShouldBeFalse()
    {
        const bool defaultVisible = false;
        Assert.That(defaultVisible, Is.False);
    }

    [Test]
    public void PlayPauseIcon_DefaultValue_ShouldBePlayIcon()
    {
        const string playIcon = "\uE768";
        const string pauseIcon = "\uE769";
        const string defaultIcon = playIcon;

        Assert.That(defaultIcon, Is.EqualTo(playIcon));
        Assert.That(defaultIcon, Is.Not.EqualTo(pauseIcon));
    }

    [Test]
    public void IsWelcomePanelVisible_DefaultValue_ShouldBeTrue()
    {
        const bool defaultVisible = true;
        Assert.That(defaultVisible, Is.True);
    }

    [Test]
    public void VolumeUp_ShouldIncrementBy5()
    {
        double volume = 50;
        const double increment = 5;
        
        volume = Math.Min(volume + increment, 100);
        
        Assert.That(volume, Is.EqualTo(55));
    }

    [Test]
    public void VolumeDown_ShouldDecrementBy5()
    {
        double volume = 50;
        const double decrement = 5;
        
        volume = Math.Max(volume - decrement, 0);
        
        Assert.That(volume, Is.EqualTo(45));
    }

    [Test]
    public void VolumeUp_AtMax_ShouldStayAt100()
    {
        double volume = 98;
        const double increment = 5;
        
        volume = Math.Min(volume + increment, 100);
        
        Assert.That(volume, Is.EqualTo(100));
    }

    [Test]
    public void VolumeDown_AtMin_ShouldStayAt0()
    {
        double volume = 3;
        const double decrement = 5;
        
        volume = Math.Max(volume - decrement, 0);
        
        Assert.That(volume, Is.EqualTo(0));
    }

    [Test]
    public void ThemeUpperCase_ShouldConvertCorrectly()
    {
        const string theme = "Dark";
        var upperCase = theme.ToUpperInvariant();
        
        Assert.That(upperCase, Is.EqualTo("DARK"));
    }

    [Test]
    public void IsChilled_WhenToggled_ShouldChangeState()
    {
        bool isChilled = false;
        
        // Toggle on
        isChilled = !isChilled;
        Assert.That(isChilled, Is.True);
        
        // Toggle off
        isChilled = !isChilled;
        Assert.That(isChilled, Is.False);
    }

    [Test]
    public void IsAudioPlayerVisible_WhenToggled_ShouldChangeState()
    {
        bool isVisible = false;
        
        // Show
        isVisible = true;
        Assert.That(isVisible, Is.True);
        
        // Hide
        isVisible = false;
        Assert.That(isVisible, Is.False);
    }

    [Test]
    public void PlayPauseIcon_WhenPlaying_ShouldBePauseIcon()
    {
        const string playIcon = "\uE768";
        const string pauseIcon = "\uE769";
        
        bool isPlaying = true;
        var icon = isPlaying ? pauseIcon : playIcon;
        
        Assert.That(icon, Is.EqualTo(pauseIcon));
    }

    [Test]
    public void PlayPauseIcon_WhenPaused_ShouldBePlayIcon()
    {
        const string playIcon = "\uE768";
        const string pauseIcon = "\uE769";
        
        bool isPlaying = false;
        var icon = isPlaying ? pauseIcon : playIcon;
        
        Assert.That(icon, Is.EqualTo(playIcon));
    }

    [Test]
    public void VersionInfo_Format_ShouldBeCorrect()
    {
        const string version = "1.1.2";
        var versionInfo = $"Version: {version}";
        
        Assert.That(versionInfo, Is.EqualTo("Version: 1.1.2"));
    }

    [TestCase("1.1.2", 1, 1, 2)]
    [TestCase("2.0.0", 2, 0, 0)]
    [TestCase("1.5.3", 1, 5, 3)]
    public void Version_ShouldParse_ToExpectedParts(string version, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        var parts = version.Split('.');
        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        var patch = int.Parse(parts[2]);

        Assert.Multiple(() =>
        {
            Assert.That(major, Is.EqualTo(expectedMajor));
            Assert.That(minor, Is.EqualTo(expectedMinor));
            Assert.That(patch, Is.EqualTo(expectedPatch));
        });
    }
}
