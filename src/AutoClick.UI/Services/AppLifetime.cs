using System.Windows;
using AutoClick.Core.Models;
using AutoClick.Services.Engine;
using AutoClick.Services.Updates;
using AutoClick.UI.Startup;
using AutoClick.Win32;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AutoClick.UI.Services;

/// <summary>Exit, restart as administrator and apply-update flows, done in a safe order.</summary>
public sealed class AppLifetime(AppHostContext context, ClickEngine engine, UpdateService updates, ILogger<AppLifetime> logger)
{
    /// <summary>True once an exit is in progress (the main window then closes instead of hiding).</summary>
    public bool IsExiting { get; private set; }

    /// <summary>Raised before the app shuts down (the main window saves its placement).</summary>
    public event EventHandler? Exiting;

    public async Task ExitAsync()
    {
        if (IsExiting)
        {
            return;
        }

        IsExiting = true;
        await StopEverythingAsync().ConfigureAwait(true);
        if (updates.State == UpdateState.ReadyToApply)
        {
            // A downloaded update is installed after AutoClick has exited.
            updates.ApplyOnExit();
        }

        Application.Current.Shutdown();
    }

    /// <summary>Restarts elevated. Returns false (and keeps running) when the user declines UAC.</summary>
    public async Task<bool> RestartAsAdministratorAsync()
    {
        // The single-instance lock stays held: the new process waits for it (--restarting), and
        // if the user cancels the UAC prompt this instance simply continues.
        var result = ElevationService.RelaunchElevated(Environment.ProcessPath!, [StartupOptions.RestartingArg]);
        if (result != RelaunchResult.Started)
        {
            logger.LogWarning("Restart as administrator: {Result}", result);
            return false;
        }

        IsExiting = true;
        await StopEverythingAsync().ConfigureAwait(true);
        Application.Current.Shutdown();
        return true;
    }

    /// <summary>Stops sessions, flushes logs and lets Velopack apply the update and restart.</summary>
    public async Task ApplyUpdateAndRestartAsync()
    {
        IsExiting = true;
        await StopEverythingAsync().ConfigureAwait(true);
        context.Instance?.Release();
        await Log.CloseAndFlushAsync().ConfigureAwait(true);
        updates.ApplyAndRestart([StartupOptions.RestartingArg]);
    }

    private async Task StopEverythingAsync()
    {
        Exiting?.Invoke(this, EventArgs.Empty);
        await engine.StopAllAsync(StopReason.AppExit).ConfigureAwait(true);
    }
}
