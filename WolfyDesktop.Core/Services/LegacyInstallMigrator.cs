using System.Diagnostics;
using Microsoft.Win32;

namespace WolfyDesktop.Core.Services;

/// <summary>
/// One-time move from the Inno Setup installer used up to v1.1.2, which kept the
/// music library inside the install folder. Copies that music into the user data
/// folder, then removes the old install so two copies do not linger.
/// </summary>
public sealed class LegacyInstallMigrator
{
    private const string InnoUninstallKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{E2AE1AC1-FDFF-44A3-9FC2-D2E9AC844326}_is1";

    /// <summary>The old app stored its default track name in this file inside the music folder.</summary>
    private const string LegacyDefaultTrackFile = "settings.txt";

    private readonly AppPaths _paths;
    private readonly ISettingsStore _settings;

    public LegacyInstallMigrator(AppPaths paths, ISettingsStore settings)
    {
        _paths = paths;
        _settings = settings;
    }

    /// <param name="allowUninstall">
    /// Only true for an installed copy; a developer build must never uninstall the real app.
    /// </param>
    public void Run(bool allowUninstall)
    {
        if (_settings.Current.LegacyDataMigrated)
        {
            return;
        }

        var legacy = FindInnoInstall();
        if (legacy is not null)
        {
            ImportMusicFrom(Path.Combine(legacy.InstallLocation, "Musics"));
        }

        ImportMusicFrom(Path.Combine(AppContext.BaseDirectory, "Musics"));

        if (legacy is null)
        {
            _settings.Update(s => s.LegacyDataMigrated = true);
        }
        else if (allowUninstall && TryUninstall(legacy))
        {
            _settings.Update(s => s.LegacyDataMigrated = true);
        }
    }

    /// <summary>Copies audio files that are not already in the library. Returns how many were copied.</summary>
    public int ImportMusicFrom(string legacyMusicFolder)
    {
        if (!Directory.Exists(legacyMusicFolder)
            || string.Equals(Path.GetFullPath(legacyMusicFolder), Path.GetFullPath(_paths.MusicFolder), StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        Directory.CreateDirectory(_paths.MusicFolder);
        var copied = 0;

        foreach (var source in Directory.EnumerateFiles(legacyMusicFolder).Where(MusicLibrary.IsSupported))
        {
            var destination = Path.Combine(_paths.MusicFolder, Path.GetFileName(source));
            if (File.Exists(destination))
            {
                continue;
            }

            try
            {
                File.Copy(source, destination);
                copied++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Could not import '{source}': {ex.Message}");
            }
        }

        ImportDefaultTrackChoice(legacyMusicFolder);
        return copied;
    }

    private void ImportDefaultTrackChoice(string legacyMusicFolder)
    {
        var markerPath = Path.Combine(legacyMusicFolder, LegacyDefaultTrackFile);
        if (_settings.Current.DefaultMusicFile is not null || !File.Exists(markerPath))
        {
            return;
        }

        try
        {
            var fileName = Path.GetFileName(File.ReadAllText(markerPath).Trim());
            if (fileName.Length > 0 && File.Exists(Path.Combine(_paths.MusicFolder, fileName)))
            {
                _settings.Update(s => s.DefaultMusicFile = fileName);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not read legacy default track: {ex.Message}");
        }
    }

    private static LegacyInstall? FindInnoInstall()
    {
        // The old installer ran per-user, but check machine-wide too in case it was elevated.
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(InnoUninstallKey);
            if (key?.GetValue("InstallLocation") is string location
                && key.GetValue("UninstallString") is string uninstallString)
            {
                return new LegacyInstall(location.TrimEnd(Path.DirectorySeparatorChar), uninstallString.Trim('"'));
            }
        }

        return null;
    }

    private static bool TryUninstall(LegacyInstall legacy)
    {
        if (!File.Exists(legacy.Uninstaller))
        {
            return true;
        }

        try
        {
            // The Inno uninstaller relaunches itself from %TEMP%, so there is nothing useful to wait for.
            Process.Start(new ProcessStartInfo(legacy.Uninstaller, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART")
            {
                UseShellExecute = false,
            });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            Debug.WriteLine($"Could not remove old install: {ex.Message}");
            return false;
        }
    }

    private sealed record LegacyInstall(string InstallLocation, string Uninstaller);
}
