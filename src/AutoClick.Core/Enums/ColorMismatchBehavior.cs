namespace AutoClick.Core.Enums;

public enum ColorMismatchBehavior
{
    SkipPoint = 0,
    StopSession = 1,
    /// <summary>
    /// Block the click loop on this point, polling every 50ms until the pixel
    /// matches the reference color or ColorWaitTimeoutMs elapses (then skips).
    /// </summary>
    WaitUntilMatch = 2
}
