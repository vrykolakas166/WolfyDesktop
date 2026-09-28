using System;
using System.Collections.Generic;
using System.IO;

namespace WolfyDesktop.Services;

public interface IFileService
{
    string MusicsFolderPath { get; }
    string DefaultAudioFileName { get; }
    IReadOnlyList<string> SupportedExtensions { get; }
    
    bool FileExists(string path);
    string? GetDefaultMusicPath();
    void SetDefaultMusicPath(string fileName);
    IEnumerable<string> GetMusicFiles();
    void DeleteFile(string path);
    void EnsureMusicsFolderExists();
    long GetFileSize(string path);
    string FormatFileSize(long bytes);
}
