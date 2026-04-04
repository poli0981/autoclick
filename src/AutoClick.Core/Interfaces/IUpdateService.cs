namespace AutoClick.Core.Interfaces;

/// <summary>
/// Represents the result of an update check with comprehensive status information.
/// </summary>
public class UpdateCheckResult
{
    public bool Success { get; init; }
    public bool UpdateAvailable { get; init; }
    public string? NewVersion { get; init; }
    public string? CurrentVersion { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdateErrorKind ErrorKind { get; init; } = UpdateErrorKind.None;
}

public enum UpdateErrorKind
{
    None,
    NotInstalled,
    NoRelease,
    RateLimited,
    Forbidden,
    ServerError,
    NetworkError,
    ChecksumFailed,
    Unknown
}

public interface IUpdateService
{
    /// <summary>True if the app was installed via Velopack (not running from IDE/debug).</summary>
    bool IsInstalled { get; }

    /// <summary>Currently installed version string.</summary>
    string CurrentVersion { get; }

    /// <summary>Check GitHub for updates with full status code handling.</summary>
    Task<UpdateCheckResult> CheckForUpdateAsync();

    /// <summary>Download the available update. Returns progress 0-100 via callback.</summary>
    Task<UpdateCheckResult> DownloadUpdateAsync(Action<int>? progressCallback = null);

    /// <summary>Apply the downloaded update and restart the application.</summary>
    void ApplyUpdateAndRestart();

    /// <summary>Apply the downloaded update without restarting.</summary>
    void ApplyUpdateAndExit();
}
