using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WolfyDesktop.Services;

public class ThemeService : IThemeService
{
    public const string Dark = "Dark";
    public const string Light = "Light";
    public const string System = "System";

    private string _currentTheme = Dark;

    public string CurrentTheme => _currentTheme;
    
    public IReadOnlyList<string> AvailableThemes { get; } = new[] { Dark, Light, System };

    public event EventHandler<string>? ThemeChanged;

    public void ApplyTheme(FrameworkElement element, string theme)
    {
        if (!IsValidTheme(theme))
            return;

        _currentTheme = theme;

        switch (theme.ToUpperInvariant())
        {
            case "LIGHT":
                if (element is Microsoft.UI.Xaml.Controls.Grid grid1)
                {
                    grid1.Background = new SolidColorBrush(Colors.White);
                }
                element.RequestedTheme = ElementTheme.Light;
                break;

            case "DARK":
                if (element is Microsoft.UI.Xaml.Controls.Grid grid2)
                {
                    grid2.Background = new SolidColorBrush(Colors.Black);
                }
                element.RequestedTheme = ElementTheme.Dark;
                break;

            case "SYSTEM":
                if (element is Microsoft.UI.Xaml.Controls.Grid grid3)
                {
                    grid3.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
                }
                element.RequestedTheme = ElementTheme.Default;
                break;
        }

        ThemeChanged?.Invoke(this, theme);
    }

    public bool IsValidTheme(string theme)
    {
        return AvailableThemes.Contains(theme, StringComparer.OrdinalIgnoreCase);
    }
}
