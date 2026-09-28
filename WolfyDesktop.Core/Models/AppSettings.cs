namespace WolfyDesktop.Core.Models;

/// <summary>
/// User preferences persisted to <c>settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    /// <summary>Background audio volume, 0–100.</summary>
    public double Volume { get; set; } = 50;

    /// <summary>File name (not path) of the preferred track inside the music folder.</summary>
    public string? DefaultMusicFile { get; set; }

    /// <summary>True once music from the old Inno Setup install has been imported.</summary>
    public bool LegacyDataMigrated { get; set; }
}
