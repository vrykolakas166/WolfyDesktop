namespace WolfyDesktop.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public double Volume { get; set; } = 50.0;
    public string? DefaultMusicFile { get; set; }
    public bool IsFirstRun { get; set; } = true;
}
