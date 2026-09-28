using System.Threading.Tasks;

namespace WolfyDesktop.Services;

public interface ISettingsService
{
    string Theme { get; set; }
    double Volume { get; set; }
    string? DefaultMusicFile { get; set; }
    bool IsFirstRun { get; set; }
    
    Task SaveAsync();
    Task LoadAsync();
}
