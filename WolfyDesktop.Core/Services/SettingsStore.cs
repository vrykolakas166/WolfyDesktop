using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using WolfyDesktop.Core.Models;

namespace WolfyDesktop.Core.Services;

/// <summary>
/// JSON-backed settings. Saves are debounced so that dragging the volume slider
/// produces one write instead of dozens, and each write goes to a temp file first
/// so a crash mid-write never leaves a truncated settings file behind.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private static readonly TimeSpan DefaultSaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly string _filePath;
    private readonly TimeSpan _saveDelay;
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private CancellationTokenSource? _pendingSave;

    public SettingsStore(AppPaths paths)
        : this(paths, DefaultSaveDelay)
    {
    }

    public SettingsStore(AppPaths paths, TimeSpan saveDelay)
    {
        _filePath = paths.SettingsFile;
        _saveDelay = saveDelay;
    }

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer
                .DeserializeAsync(stream, SettingsJsonContext.Default.AppSettings, cancellationToken)
                .ConfigureAwait(false);

            lock (_gate)
            {
                Current = loaded ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to load settings, using defaults: {ex.Message}");
        }
    }

    public void Update(Action<AppSettings> change)
    {
        lock (_gate)
        {
            change(Current);
        }

        ScheduleSave();
    }

    public Task FlushAsync()
    {
        CancellationTokenSource? pending;
        lock (_gate)
        {
            pending = _pendingSave;
            _pendingSave = null;
        }

        if (pending is null)
        {
            return Task.CompletedTask;
        }

        pending.Cancel();
        return SaveAsync();
    }

    private void ScheduleSave()
    {
        var next = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_gate)
        {
            previous = _pendingSave;
            _pendingSave = next;
        }

        previous?.Cancel();
        _ = SaveAfterDelayAsync(next.Token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_saveDelay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer change or by FlushAsync.
            return;
        }

        await SaveAsync().ConfigureAwait(false);
    }

    private async Task SaveAsync()
    {
        byte[] json;
        lock (_gate)
        {
            json = JsonSerializer.SerializeToUtf8Bytes(Current, SettingsJsonContext.Default.AppSettings);
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var tempPath = _filePath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, json).ConfigureAwait(false);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
