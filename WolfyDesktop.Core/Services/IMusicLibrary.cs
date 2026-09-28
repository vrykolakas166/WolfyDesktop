using WolfyDesktop.Core.Models;

namespace WolfyDesktop.Core.Services;

public interface IMusicLibrary
{
    string FolderPath { get; }

    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>Raised after tracks are added or removed, or the default track changes.</summary>
    event EventHandler? Changed;

    IReadOnlyList<MusicTrack> GetTracks();

    /// <summary>
    /// The track to play: the user's chosen default, else the downloadable default
    /// track, else the first track in the library. Null when the library is empty.
    /// </summary>
    string? GetDefaultTrackPath();

    void SetDefaultTrack(string fileName);

    /// <summary>Copies audio into the library under a name that does not clash with existing files.</summary>
    Task<string> ImportAsync(Stream source, string fileName, CancellationToken cancellationToken = default);

    void Delete(string path);

    void EnsureFolderExists();

    /// <summary>Raises <see cref="Changed"/> after files were added to the folder by other code.</summary>
    void NotifyChanged();
}
