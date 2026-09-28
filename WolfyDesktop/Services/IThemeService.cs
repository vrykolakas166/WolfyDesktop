using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace WolfyDesktop.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    IReadOnlyList<string> AvailableThemes { get; }
    
    event EventHandler<string>? ThemeChanged;
    
    void ApplyTheme(FrameworkElement element, string theme);
    bool IsValidTheme(string theme);
}
