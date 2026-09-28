namespace WolfyDesktop.Core.Services;

/// <summary>
/// Locations of user data. Everything lives outside the install folder so that
/// installing, updating or uninstalling the app never touches it.
/// </summary>
public sealed class AppPaths
{
    public AppPaths()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WolfyDesktop"))
    {
    }

    public AppPaths(string dataRoot)
    {
        DataRoot = dataRoot;
        MusicFolder = Path.Combine(dataRoot, "Musics");
        SettingsFile = Path.Combine(dataRoot, "settings.json");
    }

    public string DataRoot { get; }

    public string MusicFolder { get; }

    public string SettingsFile { get; }
}
