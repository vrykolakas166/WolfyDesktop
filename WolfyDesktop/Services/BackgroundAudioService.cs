using System.Diagnostics;
using Microsoft.UI.Dispatching;
using WolfyDesktop.Core.Services;

namespace WolfyDesktop.Services;

/// <summary>
/// Plays the library's default track on a loop and follows library changes: picking a
/// new default switches tracks straight away, and files can be released before deletion.
/// </summary>
public sealed class BackgroundAudioService
{
    private readonly MediaPlayerService _player;
    private readonly IMusicLibrary _library;
    private readonly DispatcherQueue _dispatcherQueue;

    public BackgroundAudioService(
        MediaPlayerService player,
        IMusicLibrary library,
        ISettingsStore settings,
        DispatcherQueue dispatcherQueue)
    {
        _player = player;
        _library = library;
        _dispatcherQueue = dispatcherQueue;

        _player.Volume = settings.Current.Volume / 100.0;
        _player.IsPlayingChanged += (_, _) => IsPlayingChanged?.Invoke(this, EventArgs.Empty);
        _library.Changed += OnLibraryChanged;
        HasTrack = _library.GetDefaultTrackPath() is not null;
    }

    public event EventHandler? IsPlayingChanged;

    public event EventHandler? HasTrackChanged;

    public bool IsPlaying => _player.IsPlaying;

    /// <summary>Whether there is anything to play. Cached so it is cheap to read on every mouse move.</summary>
    public bool HasTrack { get; private set; }

    /// <summary>0–100.</summary>
    public double Volume
    {
        get => _player.Volume * 100.0;
        set => _player.Volume = value / 100.0;
    }

    public async Task PlayAsync()
    {
        if (_library.GetDefaultTrackPath() is not { } path)
        {
            return;
        }

        if (!IsSameFile(_player.SourcePath, path))
        {
            await _player.LoadAsync(path, loop: true, enableSystemMediaControls: true);
        }

        _player.Play();
    }

    public void Pause() => _player.Pause();

    public Task TogglePlayPauseAsync()
    {
        if (IsPlaying)
        {
            Pause();
            return Task.CompletedTask;
        }

        return PlayAsync();
    }

    /// <summary>
    /// Stops using <paramref name="path"/> so it can be deleted or overwritten.
    /// Returns true if it was playing, so the caller can resume afterwards.
    /// </summary>
    public bool Release(string path)
    {
        if (!IsSameFile(_player.SourcePath, path))
        {
            return false;
        }

        var wasPlaying = IsPlaying;
        _player.Unload();
        return wasPlaying;
    }

    private async void OnLibraryChanged(object? sender, EventArgs e)
    {
        if (!_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => OnLibraryChanged(sender, e));
            return;
        }

        try
        {
            var defaultPath = _library.GetDefaultTrackPath();
            SetHasTrack(defaultPath is not null);

            if (_player.SourcePath is null || IsSameFile(_player.SourcePath, defaultPath))
            {
                return;
            }

            var wasPlaying = IsPlaying;
            _player.Unload();
            if (wasPlaying)
            {
                await PlayAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not switch background track: {ex.Message}");
        }
    }

    private void SetHasTrack(bool value)
    {
        if (HasTrack != value)
        {
            HasTrack = value;
            HasTrackChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsSameFile(string? a, string? b) =>
        a is not null && b is not null
        && string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
