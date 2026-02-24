using Avalonia;
using Avalonia.Styling;
using EasySave.Persistence;

namespace EasySave.GUI.Services;

/// <summary>
/// Manages theme switching (Light / Dark / System).
/// </summary>
public class ThemeService
{
    /// <summary>
    /// Applies the given theme preference to the running application.
    /// </summary>
    public void ApplyTheme(ThemePreference preference)
    {
        if (Avalonia.Application.Current is null)
            return;

        Avalonia.Application.Current.RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default // System
        };
    }
}
