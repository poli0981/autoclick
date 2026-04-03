using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoClick.Core.Interfaces;
using Velopack;
using Velopack.Sources;

namespace AutoClick.UI.Services;

/// <summary>
/// Velopack-based update service with comprehensive GitHub API error handling.
/// Handles: 200 OK, 304 Not Modified, 403 Forbidden/Rate-Limited, 404 Not Found, 5xx Server Errors.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    /// <summary>
    /// Hardcoded repo URL — never read from settings to avoid stale/corrupt values.
    /// </summary>
    private const string GitHubRepoUrl = "https://github.com/poli0981/autoclick";

    private readonly ILogService _log;
    private UpdateManager? _updateManager;
    private UpdateInfo? _latestUpdate;

    public bool IsInstalled => _updateManager?.IsInstalled ?? false;
    public string CurrentVersion => _updateManager?.CurrentVersion?.ToString() ?? "dev";

    public UpdateService(ILogService log)
    {
        _log = log;
        InitializeManager();
    }

    private void InitializeManager()
    {
        try
        {
            var source = new GithubSource(
                GitHubRepoUrl,
                null, // access token (null for public repos)
                false // prerelease = false → stable only
            );
            _updateManager = new UpdateManager(source);
            _log.Info($"Update manager initialized for {GitHubRepoUrl} (installed: {IsInstalled}, current: {CurrentVersion})");
        }
        catch (Exception ex)
        {
            _log.Error("Failed to initialize update manager", ex);
            _updateManager = null;
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        if (_updateManager == null)
        {
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = UpdateErrorKind.NotInstalled,
                ErrorMessage = "Update manager not initialized.",
                CurrentVersion = "dev"
            };
        }

        if (!IsInstalled)
        {
            _log.Info("Update check skipped: app is not installed via Velopack (development mode).");
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = UpdateErrorKind.NotInstalled,
                ErrorMessage = "Application is running in development mode. Updates are only available for installed builds.",
                CurrentVersion = CurrentVersion
            };
        }

        try
        {
            _log.Info("Checking for updates...");
            _latestUpdate = await _updateManager.CheckForUpdatesAsync();

            if (_latestUpdate == null)
            {
                _log.Info($"No updates available. Current version: {CurrentVersion}");
                return new UpdateCheckResult
                {
                    Success = true,
                    UpdateAvailable = false,
                    CurrentVersion = CurrentVersion
                };
            }

            var newVer = _latestUpdate.TargetFullRelease.Version.ToString();
            _log.Info($"Update available: {CurrentVersion} → {newVer}");

            return new UpdateCheckResult
            {
                Success = true,
                UpdateAvailable = true,
                CurrentVersion = CurrentVersion,
                NewVersion = newVer
            };
        }
        catch (HttpRequestException httpEx)
        {
            return HandleHttpError(httpEx);
        }
        catch (Exception ex)
        {
            _log.Error("Update check failed", ex);
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = ClassifyException(ex),
                ErrorMessage = ex.Message,
                CurrentVersion = CurrentVersion
            };
        }
    }

    public async Task<UpdateCheckResult> DownloadUpdateAsync(Action<int>? progressCallback = null)
    {
        if (_updateManager == null || _latestUpdate == null)
        {
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = UpdateErrorKind.NoRelease,
                ErrorMessage = "No update available to download. Run CheckForUpdate first."
            };
        }

        try
        {
            _log.Info($"Downloading update {_latestUpdate.TargetFullRelease.Version}...");

            await _updateManager.DownloadUpdatesAsync(
                _latestUpdate,
                progress => progressCallback?.Invoke(progress)
            );

            _log.Info("Update downloaded successfully.");
            return new UpdateCheckResult
            {
                Success = true,
                UpdateAvailable = true,
                CurrentVersion = CurrentVersion,
                NewVersion = _latestUpdate.TargetFullRelease.Version.ToString()
            };
        }
        catch (HttpRequestException httpEx)
        {
            return HandleHttpError(httpEx);
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("Checksum"))
        {
            _log.Error("Download checksum verification failed. The update file may be corrupted.");
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = UpdateErrorKind.ChecksumFailed,
                ErrorMessage = "Download verification failed. Please try again."
            };
        }
        catch (Exception ex)
        {
            _log.Error("Update download failed", ex);
            return new UpdateCheckResult
            {
                Success = false,
                ErrorKind = ClassifyException(ex),
                ErrorMessage = ex.Message
            };
        }
    }

    public void ApplyUpdateAndRestart()
    {
        if (_updateManager == null || _latestUpdate == null) return;

        try
        {
            _log.Info($"Applying update {_latestUpdate.TargetFullRelease.Version} and restarting...");
            _updateManager.ApplyUpdatesAndRestart(_latestUpdate);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to apply update", ex);
        }
    }

    public void ApplyUpdateAndExit()
    {
        if (_updateManager == null || _latestUpdate == null) return;

        try
        {
            _log.Info($"Applying update {_latestUpdate.TargetFullRelease.Version} and exiting...");
            _updateManager.ApplyUpdatesAndExit(_latestUpdate);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to apply update", ex);
        }
    }

    /// <summary>
    /// Classify HTTP errors from the GitHub API into user-friendly categories.
    /// </summary>
    private UpdateCheckResult HandleHttpError(HttpRequestException httpEx)
    {
        var statusCode = httpEx.StatusCode;
        var (kind, msg) = statusCode switch
        {
            HttpStatusCode.NotFound => (
                UpdateErrorKind.NoRelease,
                "No releases found on GitHub. The repository may not have any published releases yet."
            ),
            HttpStatusCode.Forbidden => (
                UpdateErrorKind.RateLimited,
                "GitHub API rate limit exceeded. Please wait a few minutes and try again."
            ),
            (HttpStatusCode)429 => (
                UpdateErrorKind.RateLimited,
                "GitHub API rate limit exceeded (429). Please wait before retrying."
            ),
            HttpStatusCode.Unauthorized => (
                UpdateErrorKind.Forbidden,
                "Access denied. The repository may be private or the access token is invalid."
            ),
            >= HttpStatusCode.InternalServerError => (
                UpdateErrorKind.ServerError,
                $"GitHub server error ({(int)statusCode}). Please try again later."
            ),
            _ => (
                UpdateErrorKind.NetworkError,
                $"HTTP error {(int?)statusCode}: {httpEx.Message}"
            )
        };

        // Special check for rate limiting in 403 response
        if (statusCode == HttpStatusCode.Forbidden &&
            httpEx.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            kind = UpdateErrorKind.RateLimited;
            msg = "GitHub API rate limit exceeded. Anonymous requests are limited to 60/hour. Wait and retry.";
        }

        _log.Warn($"Update HTTP error [{(int?)statusCode}]: {msg}");
        return new UpdateCheckResult
        {
            Success = false,
            ErrorKind = kind,
            ErrorMessage = msg,
            CurrentVersion = CurrentVersion
        };
    }

    private static UpdateErrorKind ClassifyException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => UpdateErrorKind.NetworkError,
            TaskCanceledException => UpdateErrorKind.NetworkError,
            _ when ex.GetType().Name.Contains("Checksum") => UpdateErrorKind.ChecksumFailed,
            _ => UpdateErrorKind.Unknown
        };
    }
}
