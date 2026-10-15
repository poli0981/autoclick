using System.Net;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace AutoClick.Services.Updates;

public enum UpdateState
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    ReadyToApply,
    Failed,

    /// <summary>Running from a build that was not installed with Velopack (e.g. from the IDE).</summary>
    NotInstalled,
}

public enum UpdateError
{
    None,
    NoRelease,
    RateLimited,
    Forbidden,
    ServerError,
    Network,
    Unknown,
}

/// <summary>Velopack updates from the GitHub releases of the repository.</summary>
public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/poli0981/autoclick";

    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager? _manager;
    private UpdateInfo? _available;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        try
        {
            _manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update manager could not be created");
        }

        if (_manager?.UpdatePendingRestart is { } pending)
        {
            NewVersion = pending.Version.ToString();
            State = UpdateState.ReadyToApply;
        }
        else if (!IsInstalled)
        {
            State = UpdateState.NotInstalled;
        }
    }

    public bool IsInstalled => _manager?.IsInstalled == true;

    public string? CurrentVersion => _manager?.CurrentVersion?.ToString();

    public UpdateState State { get; private set; }

    public UpdateError Error { get; private set; }

    public string? NewVersion { get; private set; }

    /// <summary>Release notes (Markdown) of the available update.</summary>
    public string? ReleaseNotes => _available?.TargetFullRelease.NotesMarkdown;

    public int DownloadProgress { get; private set; }

    /// <summary>Raised on any thread whenever <see cref="State"/> or progress changes.</summary>
    public event EventHandler? Changed;

    public async Task<UpdateState> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null || !IsInstalled)
        {
            return Set(UpdateState.NotInstalled);
        }

        if (State is UpdateState.Checking or UpdateState.Downloading or UpdateState.ReadyToApply)
        {
            return State;
        }

        Set(UpdateState.Checking);
        try
        {
            _available = await _manager.CheckForUpdatesAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            if (_available is null)
            {
                _logger.LogInformation("No update available (current {Version})", CurrentVersion);
                return Set(UpdateState.UpToDate);
            }

            NewVersion = _available.TargetFullRelease.Version.ToString();
            _logger.LogInformation("Update available: {Current} -> {New}", CurrentVersion, NewVersion);
            return Set(UpdateState.Available);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Fail(ex, "Update check failed");
        }
    }

    public async Task<UpdateState> DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null || _available is null)
        {
            return State;
        }

        Set(UpdateState.Downloading);
        try
        {
            await _manager.DownloadUpdatesAsync(_available, progress =>
            {
                DownloadProgress = progress;
                Changed?.Invoke(this, EventArgs.Empty);
            }, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Update {Version} downloaded", NewVersion);
            return Set(UpdateState.ReadyToApply);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Fail(ex, "Update download failed");
        }
    }

    /// <summary>
    /// Exits the process, applies the update and restarts. Save state and stop sessions first:
    /// Velopack ends the process immediately.
    /// </summary>
    public void ApplyAndRestart(string[]? restartArguments = null)
    {
        var asset = _available?.TargetFullRelease ?? _manager?.UpdatePendingRestart;
        if (_manager is null || asset is null)
        {
            return;
        }

        _logger.LogInformation("Applying update {Version} and restarting", asset.Version);
        _manager.ApplyUpdatesAndRestart(asset, restartArguments);
    }

    /// <summary>Applies a downloaded update after AutoClick exits, without restarting it.</summary>
    public void ApplyOnExit()
    {
        var asset = _available?.TargetFullRelease ?? _manager?.UpdatePendingRestart;
        if (_manager is null || asset is null)
        {
            return;
        }

        _logger.LogInformation("Update {Version} will be applied on exit", asset.Version);
        _manager.WaitExitThenApplyUpdates(asset, silent: true, restart: false);
    }

    private UpdateState Fail(Exception ex, string message)
    {
        Error = Classify(ex);
        _logger.LogWarning(ex, "{Message} ({Error})", message, Error);
        return Set(UpdateState.Failed);
    }

    private UpdateState Set(UpdateState state)
    {
        State = state;
        if (state != UpdateState.Failed)
        {
            Error = UpdateError.None;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return state;
    }

    internal static UpdateError Classify(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } => UpdateError.NoRelease,
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests } => UpdateError.RateLimited,
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => UpdateError.Forbidden,
        HttpRequestException { StatusCode: >= HttpStatusCode.InternalServerError } => UpdateError.ServerError,
        HttpRequestException or TaskCanceledException or TimeoutException => UpdateError.Network,
        _ => UpdateError.Unknown,
    };
}
