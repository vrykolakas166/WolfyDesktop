using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WolfyDesktop.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Models.AppSettings _settings = new();
    private bool _isLoaded;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WolfyDesktop");
        
        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
    }

    public string Theme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme != value)
            {
                _settings.Theme = value;
                _ = SaveAsync();
            }
        }
    }

    public double Volume
    {
        get => _settings.Volume;
        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 100.0);
            if (Math.Abs(_settings.Volume - clampedValue) > 0.01)
            {
                _settings.Volume = clampedValue;
                _ = SaveAsync();
            }
        }
    }

    public string? DefaultMusicFile
    {
        get => _settings.DefaultMusicFile;
        set
        {
            if (_settings.DefaultMusicFile != value)
            {
                _settings.DefaultMusicFile = value;
                _ = SaveAsync();
            }
        }
    }

    public bool IsFirstRun
    {
        get => _settings.IsFirstRun;
        set
        {
            if (_settings.IsFirstRun != value)
            {
                _settings.IsFirstRun = value;
                _ = SaveAsync();
            }
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public async Task LoadAsync()
    {
        if (_isLoaded)
            return;

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Models.AppSettings>(json);
                if (settings != null)
                {
                    _settings = settings;
                    _settings.IsFirstRun = false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            _settings = new Models.AppSettings();
        }

        _isLoaded = true;
    }
}
