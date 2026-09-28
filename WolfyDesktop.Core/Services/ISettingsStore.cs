using WolfyDesktop.Core.Models;

namespace WolfyDesktop.Core.Services;

public interface ISettingsStore
{
    /// <summary>Current settings. Read freely; change them through <see cref="Update"/>.</summary>
    AppSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies a change and schedules a debounced save.</summary>
    void Update(Action<AppSettings> change);

    /// <summary>Writes any pending change immediately.</summary>
    Task FlushAsync();
}
