using System.Windows;
using System.Windows.Threading;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.UI.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI;

public partial class App
{
    private readonly IServiceProvider _services;
    private readonly ILogger<App> _logger;
    private readonly bool _smokeTest;

    public App(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetRequiredService<ILogger<App>>();
        _smokeTest = services.GetRequiredService<StartupOptions>().SmokeTest;

        // v1 had no global handlers: a crash left nothing in the log.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (_smokeTest)
        {
            _ = _services.GetRequiredService<SmokeTest>().RunAsync();
            return;
        }

        _services.GetRequiredService<AppCoordinator>().Start();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled exception on the UI thread");
        if (_smokeTest)
        {
            Shutdown(1);
            return;
        }

        // Keep running: one failed command should not end the click sessions of every game.
        e.Handled = true;
        try
        {
            _services.GetRequiredService<NotificationService>().Show(L.Get("Error_Title"), L.Format("Error_Unexpected", e.Exception.Message), ToastKind.Error);
        }
        catch (Exception notifyError)
        {
            _logger.LogError(notifyError, "Could not show the error notification");
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception; AutoClick is terminating");
        Serilog.Log.CloseAndFlush();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
