using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WolfyDesktop.ViewModels;

namespace WolfyDesktop.Views;

/// <summary>
/// Helpers for x:Bind function bindings, keeping brushes and glyphs out of view models.
/// </summary>
public static class StatusVisuals
{
    public static string Glyph(StatusKind kind) => kind switch
    {
        StatusKind.Success => "", // Completed
        StatusKind.Error => "", // ErrorBadge
        StatusKind.Warning => "", // Warning
        _ => "", // Info
    };

    public static Brush Foreground(StatusKind kind) => (Brush)Application.Current.Resources[kind switch
    {
        StatusKind.Success => "SystemFillColorSuccessBrush",
        StatusKind.Error => "SystemFillColorCriticalBrush",
        StatusKind.Warning => "SystemFillColorCautionBrush",
        _ => "TextFillColorPrimaryBrush",
    }];

    public static Visibility VisibleIfNotEmpty(string? text) =>
        string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility CollapsedIf(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}
