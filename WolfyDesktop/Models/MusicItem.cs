
using Microsoft.UI.Xaml;

namespace WolfyDesktop.Models;

public class MusicItem
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string SizeText { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public Visibility IsDefaultVisibility => IsDefault ? Visibility.Visible : Visibility.Collapsed;
}
