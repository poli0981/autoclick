using System.Windows;
using System.Windows.Interop;
using AutoClick.Core.Models;
using AutoClick.Core.Settings;
using AutoClick.Services.Persistence;
using AutoClick.Services.Updates;
using AutoClick.UI.Localization;
using AutoClick.UI.Startup;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using AutoClick.Win32;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace AutoClick.UI.Services;

/// <summary>
/// Connects the window, tray icon, global hotkeys, settings and updates at startup, and
/// handles closing (minimize to tray, stop and exit, or exit).
/// </summary>
public sealed class AppCoordinator(
    AppHostContext context,
    SettingsStore settings,
    ThemeService theme,
    HotkeyService hotkeys,
    TrayService tray,
    UiTicker ticker,
    QueueViewModel queue,
    DashboardViewModel dashboard,
    AppLifetime lifetime,
    UpdateService updates,
    NotificationService notifications,
    DialogService dialogs,
    OverlayService overlays,
    MainWindow window,
    ILogger<AppCoordinator> logger)
{
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);
    private HotkeySettings? _appliedHotkeys;
    private bool _trayHintShown;

    public void Start()
    {
        var current = settings.Current;
        LocalizationService.Instance.SetLanguage(current.Language);
        Application.Current.MainWindow = window;
        RestorePlacement(current.MainWindow);
        new WindowInteropHelper(window).EnsureHandle();
        theme.Apply(current.Theme, window);

        tray.Create();
        tray.ShowRequested += (_, _) => ShowWindow();
        tray.StartAllRequested += (_, _) => queue.StartAll();
        tray.StopAllRequested += (_, _) => _ = queue.StopAllAsync();
        tray.ExitRequested += (_, _) => _ = lifetime.ExitAsync();

        hotkeys.Pressed += OnHotkeyPressed;
        hotkeys.RegistrationChanged += (_, _) => ReportHotkeyProblems();
        ApplyHotkeys(current.Hotkeys);

        settings.Changed += OnSettingsChanged;
        queue.MinimizeRequested += (_, _) => HideToTray();
        window.CloseRequested += (_, _) => _ = OnCloseRequestedAsync();
        lifetime.Exiting += (_, _) => OnExiting();
        context.Instance?.StartListening(() => window.Dispatcher.BeginInvoke(ShowWindow));
        dashboard.StartRecording();
        ticker.Start();

        if (!context.Options.StartMinimized)
        {
            window.Show();
        }

        _ = UpdateLoopAsync();
        logger.LogInformation("AutoClick {Version} ready (settings {Load}, administrator {Elevated})",
            AppInfo.Version, context.SettingsLoadResult, window.DataContext is MainWindowViewModel { IsElevated: true });
    }

    private void ShowWindow()
    {
        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    private void HideToTray()
    {
        SavePlacement();
        window.Hide();
        if (!_trayHintShown)
        {
            _trayHintShown = true;
            tray.Notify("AutoClick", L.Get("Tray_StillRunning"));
        }
    }

    private async Task OnCloseRequestedAsync()
    {
        switch (settings.Current.ExitBehavior)
        {
            case ExitBehavior.MinimizeToTray:
                HideToTray();
                break;
            case ExitBehavior.StopAndExit:
                if (queue.AnyActive
                    && !await dialogs.ConfirmAsync(L.Get("Exit_ConfirmTitle"), L.Get("Exit_ConfirmMessage"), L.Get("Exit_StopAndExit")))
                {
                    return;
                }

                await lifetime.ExitAsync();
                break;
            default:
                await lifetime.ExitAsync();
                break;
        }
    }

    private void OnExiting()
    {
        SavePlacement();
        ticker.Stop();
        overlays.CloseAll();
        hotkeys.Dispose();
        tray.Dispose();
    }

    private void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.PauseResume:
                queue.PauseResumeAll();
                break;
            case HotkeyAction.StopAll:
                _ = queue.StopAllAsync();
                break;
            case HotkeyAction.StartAll:
                queue.StartAll();
                break;
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings current)
    {
        LocalizationService.Instance.SetLanguage(current.Language);
        if (current.Theme != theme.Current)
        {
            theme.Apply(current.Theme, window);
        }

        ApplyHotkeys(current.Hotkeys);
        context.LevelSwitch.MinimumLevel = current.VerboseLogging ? LogEventLevel.Debug : LogEventLevel.Information;
    }

    /// <summary>Re-registers only when the hotkeys changed (each registration reports conflicts).</summary>
    private void ApplyHotkeys(HotkeySettings value)
    {
        if (value != _appliedHotkeys)
        {
            _appliedHotkeys = value;
            hotkeys.Apply(value);
        }
    }

    private void ReportHotkeyProblems()
    {
        var failed = hotkeys.Results
            .Where(r => r.Value != HotkeyRegistrationResult.Registered)
            .Select(r => r.Key switch
            {
                HotkeyAction.PauseResume => settings.Current.Hotkeys.PauseResume,
                HotkeyAction.StopAll => settings.Current.Hotkeys.StopAll,
                _ => settings.Current.Hotkeys.StartAll,
            })
            .ToList();
        if (failed.Count > 0)
        {
            notifications.Show(L.Get("Hotkey_Title"), L.Format("Hotkey_RegisterFailed", string.Join(", ", failed)), ToastKind.Warning);
        }
    }

    private async Task UpdateLoopAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            using var timer = new PeriodicTimer(UpdateCheckInterval);
            do
            {
                if (settings.Current.AutoUpdate && updates.State is UpdateState.Idle or UpdateState.UpToDate or UpdateState.Failed)
                {
                    if (await updates.CheckAsync() == UpdateState.Available
                        && await updates.DownloadAsync() == UpdateState.ReadyToApply)
                    {
                        notifications.Show(L.Get("Notice_UpdateTitle"), L.Format("Notice_UpdateReady", updates.NewVersion), ToastKind.Success);
                    }
                }
            }
            while (await timer.WaitForNextTickAsync());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Automatic update check stopped");
        }
    }

    private void RestorePlacement(WindowPlacement? placement)
    {
        if (placement is null)
        {
            return;
        }

        var bounds = new Rect(placement.Left, placement.Top, placement.Width, placement.Height);
        var desktop = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var visible = Rect.Intersect(bounds, desktop);
        if (visible.IsEmpty || visible.Width < 200 || visible.Height < 100)
        {
            // The monitor it was on is gone: keep the default centered position.
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = bounds.Left;
        window.Top = bounds.Top;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        if (placement.IsMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    private void SavePlacement()
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
            : window.RestoreBounds;
        if (bounds.IsEmpty || !window.IsLoaded)
        {
            return;
        }

        settings.Update(s => s with
        {
            MainWindow = new WindowPlacement
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                IsMaximized = window.WindowState == WindowState.Maximized,
            },
        });
    }
}
