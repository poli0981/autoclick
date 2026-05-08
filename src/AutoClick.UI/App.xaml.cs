using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using AutoClick.Core.Interfaces;
using AutoClick.Services;
using AutoClick.UI.Resources;
using AutoClick.UI.Services;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using AutoClick.Win32;
using Velopack;

namespace AutoClick.UI;

public partial class App : Application
{
    private const string RestartingArg = "--restarting";

    private ServiceProvider _serviceProvider = null!;
    private NotifyIcon? _trayIcon;
    private SingleInstanceService? _singleInstance;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Velopack lifecycle — must run first for install/update hooks.
        // Velopack hook args (--veloapp-*) cause Velopack to call Environment.Exit before returning.
        VelopackApp.Build().Run();

        // Single-instance gate. Skip during Velopack hooks just in case (defensive).
        var isVelopackHook = e.Args.Any(a => a.StartsWith("--veloapp-", StringComparison.OrdinalIgnoreCase));
        var isRestarting = e.Args.Any(a => string.Equals(a, RestartingArg, StringComparison.OrdinalIgnoreCase));

        // LogService is instantiated up-front so the gate can log; the same instance is then
        // registered as the DI singleton so logs share the file/buffer.
        var log = new LogService();

        if (!isVelopackHook)
        {
            _singleInstance = new SingleInstanceService(log);
            if (!_singleInstance.TryAcquire(isRestarting))
            {
                _singleInstance.SendShowToExisting();
                _singleInstance.Dispose();
                _singleInstance = null;
                Environment.Exit(0);
                return;
            }
        }

        var services = new ServiceCollection();

        services.AddSingleton<ILogService>(log);
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGameDetector, GameDetectorService>();
        services.AddSingleton<IMemoryManager, MemoryManagerService>();
        services.AddSingleton<IClickEngine, ClickEngineService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<ISessionExportService, SessionExportService>();
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
        services.AddSingleton<DashboardViewModel>(sp =>
            new DashboardViewModel(sp.GetRequiredService<MainViewModel>()));

        _serviceProvider = services.BuildServiceProvider();

        var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();

        // On first launch, detect system theme
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var loaded = settingsService.Load();
        // Apply: if settings file didn't exist yet, default to system theme
        ApplyTheme(loaded.Theme);
        ApplyLanguage(settingsVm.Language);

        settingsVm.ThemeChanged += () => ApplyTheme(settingsVm.Theme);
        settingsVm.LanguageChanged += ApplyLanguage;

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

        settingsVm.ResetAppRequested += () => mainVm.ResetAppCommand.Execute(null);
        settingsVm.SessionExportRequested += filePath => mainVm.ExportSessionToFile(filePath);
        settingsVm.SessionImportRequested += filePath =>
        {
            mainVm.ImportSessionFromFile(filePath);
            settingsVm.RefreshAllBindings();
            ApplyTheme(settingsVm.Theme);
            ApplyLanguage(settingsVm.Language);
        };
        mainVm.SettingsReloaded += () =>
        {
            settingsVm.RefreshAllBindings();
            ApplyTheme(settingsVm.Theme);
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
            _serviceProvider.GetRequiredService<DashboardViewModel>(),
            _serviceProvider.GetRequiredService<HotkeyService>(),
            soundService);

        SetupTrayIcon(mainWindow);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Forward "show" requests from second-launch attempts to this window.
        _singleInstance?.StartIpcServer(() =>
            Dispatcher.Invoke(() => BringToFront(mainWindow)));

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

    private void ApplyTheme(AutoClick.Core.Enums.ThemeMode theme)
    {
        var path = theme switch
        {
            AutoClick.Core.Enums.ThemeMode.Light => "Themes/LightTheme.xaml",
            AutoClick.Core.Enums.ThemeMode.HighContrast => "Themes/HighContrastTheme.xaml",
            _ => "Themes/DarkTheme.xaml"
        };
        var dict = new ResourceDictionary { Source = new Uri(path, UriKind.Relative) };
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
        Icon? appIcon = null;
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath != null)
                appIcon = Icon.ExtractAssociatedIcon(exePath);
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

    private void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (exePath != null)
        {
            // Release the Mutex synchronously BEFORE spawning the new process so the
            // new instance can acquire it without racing OnExit (which fires async).
            _singleInstance?.Release();
            _singleInstance = null;

            Process.Start(new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Arguments = RestartingArg
            });
        }
        Current.Shutdown();
    }

    private static void BringToFront(Window mainWindow)
    {
        if (!mainWindow.IsVisible)
            mainWindow.Show();
        if (mainWindow.WindowState == WindowState.Minimized)
            mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();

        var handle = new WindowInteropHelper(mainWindow).Handle;
        if (handle != IntPtr.Zero)
            NativeMethods.SetForegroundWindow(handle);
    }

    public static void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (Current is App app)
            app._trayIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Release();
        _singleInstance = null;
        _trayIcon?.Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
