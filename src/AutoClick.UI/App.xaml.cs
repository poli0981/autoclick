using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using AutoClick.Core.Interfaces;
using AutoClick.Services;
using AutoClick.UI.Resources;
using AutoClick.UI.Services;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using Velopack;

namespace AutoClick.UI;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;
    private NotifyIcon? _trayIcon;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Velopack lifecycle — must run first for install/update hooks
        VelopackApp.Build().Run();

        var services = new ServiceCollection();

        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGameDetector, GameDetectorService>();
        services.AddSingleton<IMemoryManager, MemoryManagerService>();
        services.AddSingleton<IClickEngine, ClickEngineService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<IHotkeyService>(sp => sp.GetRequiredService<HotkeyService>());

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>(sp =>
        {
            var mainVm = sp.GetRequiredService<MainViewModel>();
            return new SettingsViewModel(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ILogService>(),
                mainVm.Settings,
                () => mainVm.HasAnyRunning);
        });
        services.AddSingleton<AboutViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();

        // On first launch, detect system theme
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var loaded = settingsService.Load();
        // Apply: if settings file didn't exist yet, default to system theme
        ApplyTheme(loaded.DarkMode);
        ApplyLanguage(settingsVm.Language);

        settingsVm.ThemeChanged += () => ApplyTheme(settingsVm.DarkMode);
        settingsVm.LanguageChanged += ApplyLanguage;

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

        settingsVm.ResetAppRequested += () => mainVm.ResetAppCommand.Execute(null);
        mainVm.SettingsReloaded += () =>
        {
            settingsVm.RefreshAllBindings();
            ApplyTheme(settingsVm.DarkMode);
            ApplyLanguage(settingsVm.Language);
        };

        // Language change → save & prompt restart
        settingsVm.RestartRequested += () =>
        {
            mainVm.SaveSettings();
            var result = MessageBox.Show(
                Strings.LanguageChangeMsg,
                Strings.LanguageChangeTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                mainVm.StopAllCommand.Execute(null);
                RestartApplication();
            }
        };

        var settingsRefreshTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        settingsRefreshTimer.Tick += (_, _) => settingsVm.RefreshRunningState();
        settingsRefreshTimer.Start();

        var soundService = new SoundService(mainVm.Settings);
        mainVm.SetSoundService(soundService);

        var mainWindow = new MainWindow();
        mainWindow.Initialize(mainVm, settingsVm,
            _serviceProvider.GetRequiredService<AboutViewModel>(),
            _serviceProvider.GetRequiredService<HotkeyService>(),
            soundService);

        SetupTrayIcon(mainWindow);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Initialize update service + about VM
        var logService = _serviceProvider.GetRequiredService<ILogService>();
        var updateService = new UpdateService(logService);
        var aboutVm = _serviceProvider.GetRequiredService<AboutViewModel>();
        aboutVm.InitializeUpdate(updateService, logService);

        logService.Info("AutoClick started");

        // Auto-update on startup (fire-and-forget, non-blocking)
        if (mainVm.Settings.AutoUpdate)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await aboutVm.UpdateVm!.AutoCheckOnStartupAsync();
                }
                catch (Exception ex)
                {
                    logService.Warn($"Auto-update check failed: {ex.Message}");
                }
            });
        }
    }

    private void ApplyTheme(bool dark)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri(dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
        };
        var merged = Resources.MergedDictionaries;
        if (merged.Count > 0) merged[0] = dict;
        else merged.Insert(0, dict);
    }

    private static void ApplyLanguage(string culture)
    {
        try { Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture); }
        catch { Thread.CurrentThread.CurrentUICulture = new CultureInfo("en"); }
    }

    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch { return true; }
    }

    private void SetupTrayIcon(Window mainWindow)
    {
        // Use the embedded app icon for tray; fall back to system default
        System.Drawing.Icon? appIcon = null;
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath != null)
                appIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch { /* ignore */ }

        _trayIcon = new NotifyIcon
        {
            Text = "AutoClick — Auto-click utility for games",
            Icon = appIcon ?? SystemIcons.Application,
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        });
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon.Visible = false;
            if (mainWindow is MainWindow mw)
                mw.ForceClose();
            else
                Current.Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };
    }

    private static void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (exePath != null)
        {
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
        }
        Current.Shutdown();
    }

    public static void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (Current is App app)
            app._trayIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
