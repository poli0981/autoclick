using System.Windows;
using AutoClick.Core.Models;
using Wpf.Ui.Appearance;

namespace AutoClick.UI.Services;

/// <summary>Applies the Light/Dark/High contrast/System theme (WPF-UI Fluent resources).</summary>
public sealed class ThemeService
{
    private Window? _watchedWindow;

    /// <summary>Raised after the theme resources changed (charts re-read their colors).</summary>
    public event EventHandler? ThemeChanged;

    public AppTheme Current { get; private set; } = AppTheme.System;

    /// <param name="theme">The theme to apply.</param>
    /// <param name="window">Main window; with <see cref="AppTheme.System"/> it follows Windows theme changes.</param>
    public void Apply(AppTheme theme, Window? window)
    {
        Current = theme;
        if (_watchedWindow is not null)
        {
            SystemThemeWatcher.UnWatch(_watchedWindow);
            _watchedWindow = null;
        }

        if (theme == AppTheme.System)
        {
            ApplicationThemeManager.ApplySystemTheme(true);
            if (window is not null && new System.Windows.Interop.WindowInteropHelper(window).Handle != 0)
            {
                SystemThemeWatcher.Watch(window);
                _watchedWindow = window;
            }
        }
        else
        {
            var wpfTheme = theme switch
            {
                AppTheme.Light => ApplicationTheme.Light,
                AppTheme.HighContrast => ApplicationTheme.HighContrast,
                _ => ApplicationTheme.Dark,
            };
            ApplicationThemeManager.Apply(wpfTheme);
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
