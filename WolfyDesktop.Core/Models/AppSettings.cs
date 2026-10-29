namespace WolfyDesktop.Core.Models;

/// <summary>
/// User preferences persisted to <c>settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    /// <summary>Background audio volume, 0–100.</summary>
    public double Volume { get; set; } = 50;

    /// <summary>Opacity of the play/pause and volume controls when shown, 0–100. At 0 they never show.</summary>
    public double ControlsOpacity { get; set; } = 100;

    /// <summary>File name (not path) of the preferred track inside the music folder.</summary>
    public string? DefaultMusicFile { get; set; }

    /// <summary>Full screen (the default) or windowed.</summary>
    public bool IsFullScreen { get; set; } = true;

    /// <summary>Last bounds of the window when windowed; null until it has been windowed once.</summary>
    public WindowBounds? WindowBounds { get; set; }

    /// <summary>True once music from the old Inno Setup install has been imported.</summary>
    public bool LegacyDataMigrated { get; set; }
}
