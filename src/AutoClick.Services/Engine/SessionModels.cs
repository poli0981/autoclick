using AutoClick.Core.Models;

namespace AutoClick.Services.Engine;

/// <summary>Everything a click session needs; a snapshot, so later edits do not affect a running session.</summary>
public sealed record SessionStartInfo
{
    public required string SessionId { get; init; }
    public required nint Window { get; init; }
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required IReadOnlyList<ClickPoint> Points { get; init; }
    public ClickTiming Timing { get; init; } = new();
    public PixelGuardOptions PixelGuard { get; init; } = new();

    /// <summary>Move each click by up to ±2 px so clicks do not land on the identical pixel.</summary>
    public bool Jitter { get; init; } = true;
}

public sealed class SessionStateChangedEventArgs(SessionState state, StopReason reason, string? detail, bool isWaitingForWindow) : EventArgs
{
    public SessionState State { get; } = state;

    public StopReason Reason { get; } = reason;

    /// <summary>Extra context for the reason (e.g. which point was out of bounds).</summary>
    public string? Detail { get; } = detail;

    /// <summary>The game is minimized; the session waits instead of clicking.</summary>
    public bool IsWaitingForWindow { get; } = isWaitingForWindow;
}
