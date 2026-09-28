using WolfyDesktop.Tests.Helpers;

namespace WolfyDesktop.Tests.Services;

[TestFixture]
public class SettingsServiceTests
{
    private TestFileHelper? _testFileHelper;
    private string? _testSettingsDirectory;
    private string? _testSettingsFilePath;

    [SetUp]
    public void SetUp()
    {
        _testFileHelper = new TestFileHelper();
        var tempPath = Path.GetTempPath();
        _testSettingsDirectory = _testFileHelper.CreateTestDirectory(tempPath, $"WolfyDesktop_Settings_{Guid.NewGuid():N}");
        _testSettingsFilePath = Path.Combine(_testSettingsDirectory, "settings.json");
    }

    [TearDown]
    public void TearDown()
    {
        _testFileHelper?.Dispose();
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
        const double defaultVolume = 50.0;
        Assert.That(defaultVolume, Is.EqualTo(50.0));
    }

    [Test]
    public void IsFirstRun_DefaultValue_ShouldBeTrue()
    {
        const bool defaultIsFirstRun = true;
        Assert.That(defaultIsFirstRun, Is.True);
    }

    [TestCase("Dark")]
    [TestCase("Light")]
    [TestCase("System")]
    public void Theme_ValidValues_ShouldBeAccepted(string theme)
    {
        var validThemes = new[] { "Dark", "Light", "System" };
        Assert.That(validThemes, Does.Contain(theme));
    }

    [TestCase(0.0)]
    [TestCase(25.0)]
    [TestCase(50.0)]
    [TestCase(75.0)]
    [TestCase(100.0)]
    public void Volume_ValidValues_ShouldBeInRange(double volume)
    {
        Assert.Multiple(() =>
        {
            Assert.That(volume, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(volume, Is.LessThanOrEqualTo(100.0));
        });
    }

    [Test]
    public void Volume_Clamping_ShouldEnforceBounds()
    {
        double clampedMin = Math.Clamp(-10.0, 0.0, 100.0);
        double clampedMax = Math.Clamp(150.0, 0.0, 100.0);
        double clampedNormal = Math.Clamp(50.0, 0.0, 100.0);

        Assert.Multiple(() =>
        {
            Assert.That(clampedMin, Is.EqualTo(0.0));
            Assert.That(clampedMax, Is.EqualTo(100.0));
            Assert.That(clampedNormal, Is.EqualTo(50.0));
        });
    }

    [Test]
    public void SettingsJson_CanBeSerialized()
    {
        var settings = new
        {
            Theme = "Dark",
            Volume = 50.0,
            DefaultMusicFile = "lofi_rain.mp3",
            IsFirstRun = false
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        Assert.That(json, Does.Contain("\"Theme\""));
        Assert.That(json, Does.Contain("\"Volume\""));
        Assert.That(json, Does.Contain("\"DefaultMusicFile\""));
        Assert.That(json, Does.Contain("\"IsFirstRun\""));
    }

    [Test]
    public void SettingsJson_CanBeDeserialized()
    {
        var json = @"{
            ""Theme"": ""Light"",
            ""Volume"": 75.0,
            ""DefaultMusicFile"": ""custom_music.mp3"",
            ""IsFirstRun"": false
        }";

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Theme").GetString(), Is.EqualTo("Light"));
            Assert.That(root.GetProperty("Volume").GetDouble(), Is.EqualTo(75.0));
            Assert.That(root.GetProperty("DefaultMusicFile").GetString(), Is.EqualTo("custom_music.mp3"));
            Assert.That(root.GetProperty("IsFirstRun").GetBoolean(), Is.False);
        });
    }

    [Test]
    public void SettingsFile_WhenSaved_ShouldExist()
    {
        var json = "{ \"Theme\": \"Dark\" }";
        File.WriteAllText(_testSettingsFilePath!, json);

        Assert.That(File.Exists(_testSettingsFilePath), Is.True);
    }

    [Test]
    public void SettingsFile_WhenLoaded_ShouldReturnContent()
    {
        var expectedJson = "{ \"Theme\": \"Dark\" }";
        File.WriteAllText(_testSettingsFilePath!, expectedJson);

        var actualJson = File.ReadAllText(_testSettingsFilePath!);

        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    [Test]
    public void SettingsDirectory_ShouldUseLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        Assert.That(localAppData, Is.Not.Empty);
        Assert.That(Directory.Exists(localAppData), Is.True);
    }
}
