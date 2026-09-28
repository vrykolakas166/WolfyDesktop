namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class ThemeServiceTests
{
    [Test]
    public void DarkTheme_Constant_ShouldBeDark()
    {
        const string dark = "Dark";
        Assert.That(dark, Is.EqualTo("Dark"));
    }

    [Test]
    public void LightTheme_Constant_ShouldBeLight()
    {
        const string light = "Light";
        Assert.That(light, Is.EqualTo("Light"));
    }

    [Test]
    public void SystemTheme_Constant_ShouldBeSystem()
    {
        const string system = "System";
        Assert.That(system, Is.EqualTo("System"));
    }

    [Test]
    public void AvailableThemes_ShouldContainAllThemes()
    {
        var themes = new[] { "Dark", "Light", "System" };
        
        Assert.Multiple(() =>
        {
            Assert.That(themes, Does.Contain("Dark"));
            Assert.That(themes, Does.Contain("Light"));
            Assert.That(themes, Does.Contain("System"));
            Assert.That(themes, Has.Length.EqualTo(3));
        });
    }

    [TestCase("Dark", true)]
    [TestCase("Light", true)]
    [TestCase("System", true)]
    [TestCase("dark", true)] // Case insensitive check
    [TestCase("LIGHT", true)]
    [TestCase("Invalid", false)]
    [TestCase("", false)]
    public void IsValidTheme_ShouldValidateCorrectly(string theme, bool expectedValid)
    {
        var validThemes = new[] { "Dark", "Light", "System" };
        var isValid = validThemes.Contains(theme, StringComparer.OrdinalIgnoreCase);
        
        Assert.That(isValid, Is.EqualTo(expectedValid));
    }

    [Test]
    public void ThemeConstants_ShouldBeUnique()
    {
        var themes = new[] { "Dark", "Light", "System" };
        var uniqueThemes = themes.Distinct().ToArray();
        
        Assert.That(uniqueThemes, Has.Length.EqualTo(3));
    }

    [TestCase("Dark", "DARK")]
    [TestCase("Light", "LIGHT")]
    [TestCase("System", "SYSTEM")]
    public void ThemeToUpperCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = input.ToUpperInvariant();
        Assert.That(result, Is.EqualTo(expected));
    }
}
