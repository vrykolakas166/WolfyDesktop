using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WolfyDesktop.Services;

public class FileService : IFileService
{
    public string MusicsFolderPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics");
    
    public string DefaultAudioFileName { get; } = "lofi_rain.mp3";
    
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".mp3", ".m4a", ".wav", ".flac", ".aac", ".wma" };

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public string? GetDefaultMusicPath()
    {
        try
        {
            var settingsFile = Path.Combine(MusicsFolderPath, "settings.txt");
            if (File.Exists(settingsFile))
            {
                var defaultFileName = File.ReadAllText(settingsFile).Trim();
                var fullPath = Path.Combine(MusicsFolderPath, defaultFileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading default music path: {ex.Message}");
        }

        // Fallback to default audio file
        var defaultPath = Path.Combine(MusicsFolderPath, DefaultAudioFileName);
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    public void SetDefaultMusicPath(string fileName)
    {
        try
        {
            EnsureMusicsFolderExists();
            var settingsFile = Path.Combine(MusicsFolderPath, "settings.txt");
            File.WriteAllText(settingsFile, fileName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save default music: {ex.Message}");
        }
    }

    public IEnumerable<string> GetMusicFiles()
    {
        EnsureMusicsFolderExists();

        return Directory.GetFiles(MusicsFolderPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }

    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void EnsureMusicsFolderExists()
    {
        Directory.CreateDirectory(MusicsFolderPath);
    }

    public long GetFileSize(string path)
    {
        if (!File.Exists(path))
            return 0;

        return new FileInfo(path).Length;
    }

    public string FormatFileSize(long bytes)
    {
        var sizeInMB = bytes / (1024.0 * 1024.0);
        return $"{sizeInMB:F2} MB";
    }
}
