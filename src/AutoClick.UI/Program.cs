using AutoClick.Services;
using AutoClick.Services.Logging;
using AutoClick.Services.Persistence;
using AutoClick.UI.Startup;
using AutoClick.Win32;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Velopack;

namespace AutoClick.UI;

/// <summary>
/// Startup order matters: Velopack hooks → logging → settings (and v1 migration) →
/// elevation relaunch → single instance → DI host → WPF.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Install/update/uninstall hooks run here and exit the process; must come first.
        VelopackApp.Build().Run();

        var options = StartupOptions.Parse(args);
        // The smoke test never touches the user's settings, profiles or logs.
        var paths = options.SmokeTest ? new AppPaths(Path.Combine(Path.GetTempPath(), "AutoClick-smoke")) : new AppPaths();
        var logBuffer = new LogBuffer();
        var levelSwitch = new LoggingLevelSwitch();
        Log.Logger = AppLogging.Create(paths, logBuffer, levelSwitch);

        using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
        var logger = loggerFactory.CreateLogger("Startup");
        SingleInstance? instance = null;
        try
        {
            logger.LogInformation("AutoClick {Version} starting ({Args})", AppInfo.Version, string.Join(' ', args));
            var settings = new SettingsStore(paths, TimeProvider.System, loggerFactory.CreateLogger<SettingsStore>());
            var loadResult = settings.Load();
            levelSwitch.MinimumLevel = settings.Current.VerboseLogging ? LogEventLevel.Debug : LogEventLevel.Information;

            var elevation = new ElevationService();
            var elevationDeclined = false;
            if (!options.SmokeTest && settings.Current.AlwaysRunAsAdmin && !elevation.IsCurrentProcessElevated)
            {
                switch (ElevationService.RelaunchElevated(Environment.ProcessPath!, [.. args, StartupOptions.RestartingArg]))
                {
                    case RelaunchResult.Started:
                        logger.LogInformation("Relaunched as administrator");
                        return 0;
                    case RelaunchResult.Cancelled:
                        // Do not loop: continue without administrator rights and tell the user.
                        logger.LogWarning("Administrator relaunch was cancelled; continuing without administrator rights");
                        elevationDeclined = true;
                        break;
                    default:
                        logger.LogWarning("Administrator relaunch failed; continuing without administrator rights");
                        elevationDeclined = true;
                        break;
                }
            }

            if (!options.SmokeTest)
            {
                var wait = options.Restarting ? TimeSpan.FromSeconds(10) : TimeSpan.Zero;
                instance = SingleInstance.TryAcquire(wait, logger);
                if (instance is null)
                {
                    SingleInstance.SignalExisting(logger);
                    return 0;
                }
            }

            using var host = AppHost.Build(args, new AppHostContext(paths, settings, loadResult, logBuffer, levelSwitch, options, instance, elevationDeclined));
            host.Start();

            var app = new App(host.Services);
            app.InitializeComponent();
            var exitCode = app.Run();

            host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            logger.LogInformation("AutoClick exited with code {ExitCode}", exitCode);
            return exitCode;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "AutoClick failed to start");
            return 1;
        }
        finally
        {
            instance?.Dispose();
            Log.CloseAndFlush();
        }
    }
}
