using System.Windows;
using System.Windows.Threading;
using AutoClick.Core.Models;
using Wpf.Ui.Appearance;

namespace AutoClick.UI.Services;

/// <summary>Applies the Light/Dark/High contrast/System theme (WPF-UI Fluent resources).</summary>
public sealed class ThemeService
{
    private Window? _watchedWindow;
    private bool _changeQueued;

    public ThemeService()
    {
        // WPF-UI raises this for every theme it applies: ours, and SystemThemeWatcher's when the
        // theme follows Windows (several times for one switch in Windows).
        ApplicationThemeManager.Changed += (_, _) => QueueThemeChanged();
    }

    /// <summary>
    /// Raised after the theme resources changed, including a change made by Windows (charts re-read
    /// their colors). A burst of changes raises it once, after the work already queued on the UI thread.
    /// </summary>
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
    }

    private void QueueThemeChanged()
    {
        if (_changeQueued)
        {
            return;
        }

        _changeQueued = true;
        _ = Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _changeQueued = false;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }, DispatcherPriority.Background);
    }
}
