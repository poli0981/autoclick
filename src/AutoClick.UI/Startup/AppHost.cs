using AutoClick.Core.Abstractions;
using AutoClick.Services;
using AutoClick.Services.Engine;
using AutoClick.Services.Logging;
using AutoClick.Services.Persistence;
using AutoClick.Services.Scheduling;
using AutoClick.Services.Updates;
using AutoClick.UI.Services;
using AutoClick.UI.ViewModels;
using AutoClick.UI.Views;
using AutoClick.UI.Views.Pages;
using AutoClick.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Wpf.Ui;
using ThemeService = AutoClick.UI.Services.ThemeService;

namespace AutoClick.UI.Startup;

/// <summary>Objects created before the host (settings are needed to decide about elevation).</summary>
public sealed record AppHostContext(
    AppPaths Paths,
    SettingsStore Settings,
    SettingsLoadResult SettingsLoadResult,
    LogBuffer LogBuffer,
    LoggingLevelSwitch LevelSwitch,
    StartupOptions Options,
    SingleInstance? Instance,
    bool ElevationDeclined);

internal static class AppHost
{
    public static IHost Build(string[] args, AppHostContext context)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ApplicationName = "AutoClick",
            // After a "run as administrator" relaunch the working directory is System32.
            ContentRootPath = AppContext.BaseDirectory,
            // No console or Event Log providers, no appsettings.json lookups.
            DisableDefaults = true,
        });

        builder.Services.AddSerilog(Log.Logger, dispose: false);
        var services = builder.Services;

        services.AddSingleton(context);
        services.AddSingleton(context.Paths);
        services.AddSingleton(context.Settings);
        services.AddSingleton(context.LogBuffer);
        services.AddSingleton(context.Options);
        services.AddSingleton(TimeProvider.System);

        // Platform + engine
        services.AddSingleton<IElevationService, ElevationService>();
        services.AddSingleton<IWindowInspector, Win32WindowInspector>();
        services.AddSingleton<IInputSender, Win32InputSender>();
        services.AddSingleton<IPixelReader, Win32PixelReader>();
        services.AddSingleton<ShellLauncher>();
        services.AddSingleton(sp => new ClickEngine(
            sp.GetRequiredService<IInputSender>(), sp.GetRequiredService<IWindowInspector>(), sp.GetRequiredService<IPixelReader>(),
            sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ClickEngine>>()));
        services.AddSingleton<ProfileStore>();
        services.AddSingleton<ClickScheduler>();
        services.AddSingleton<UpdateService>();

        // UI services
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<UiTicker>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<SoundService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<TrayService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<OverlayService>();
        services.AddSingleton<AppLifetime>();
        services.AddSingleton<AppCoordinator>();
        services.AddSingleton<SmokeTest>();

        // View models and views (NavigationView resolves pages from the container)
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<QueueViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<QueuePage>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<AboutPage>();

        // Fail at startup (and in the smoke test) when a registration is missing.
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));
        return builder.Build();
    }
}
