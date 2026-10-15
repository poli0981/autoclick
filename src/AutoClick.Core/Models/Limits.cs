namespace AutoClick.Core.Models;

/// <summary>Valid ranges for user-editable values. Anything read from disk is clamped to these.</summary>
public static class Limits
{
    public const double MinIntervalSeconds = 1;
    public const double MaxIntervalSeconds = 3600;
    public const double DefaultIntervalSeconds = 2;

    public const int MinSequenceDelayMs = 0;
    public const int MaxSequenceDelayMs = 10_000;
    public const int DefaultSequenceDelayMs = 200;

    public const int MinGamesInQueue = 1;
    public const int MaxGamesInQueue = 50;

    public const int MinColorTolerance = 0;
    public const int MaxColorTolerance = 100;

    public const int MinColorWaitTimeoutMs = 100;
    public const int MaxColorWaitTimeoutMs = 600_000;

    /// <summary>Largest client coordinate accepted for a click point.</summary>
    public const int MaxCoordinate = 32_767;

    public static double ClampInterval(double seconds) =>
        double.IsFinite(seconds) ? Math.Clamp(seconds, MinIntervalSeconds, MaxIntervalSeconds) : DefaultIntervalSeconds;

    public static int ClampSequenceDelay(int milliseconds) =>
        Math.Clamp(milliseconds, MinSequenceDelayMs, MaxSequenceDelayMs);
}
