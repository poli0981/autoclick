using AutoClick.Core.Models;

namespace AutoClick.Core.Abstractions;

/// <summary>Outcome of posting input to a window.</summary>
public enum InputResult
{
    Ok,

    /// <summary>Blocked by User Interface Privilege Isolation: the target runs elevated.</summary>
    AccessDenied,

    /// <summary>The window no longer exists.</summary>
    WindowGone,

    /// <summary>The target's message queue is full (it is not processing messages).</summary>
    TargetNotResponding,

    Failed,
}

/// <summary>Sends mouse and keyboard input to a window without moving the real cursor.</summary>
public interface IInputSender
{
    /// <summary>Moves to and clicks the client point (x, y) of <paramref name="window"/>.</summary>
    ValueTask<InputResult> ClickAsync(nint window, int x, int y, ClickType type, CancellationToken cancellationToken);

    ValueTask<InputResult> KeystrokeAsync(nint window, int virtualKey, CancellationToken cancellationToken);
}

public enum WindowStatus
{
    Alive,
    Minimized,

    /// <summary>Closed, or the handle now belongs to another process.</summary>
    Gone,
}

/// <summary>How a process's privileges compare to AutoClick's own.</summary>
public enum ElevationState
{
    Unknown,
    SameOrLower,

    /// <summary>Higher integrity level: input from AutoClick will be blocked.</summary>
    Higher,
}

/// <summary>A top-level window the user can pick as a click target.</summary>
public sealed record WindowInfo(
    nint Handle,
    int ProcessId,
    string ProcessName,
    string Title,
    string ExecutablePath,
    ElevationState Elevation)
{
    public override string ToString() => $"{ProcessName} - {Title}";
}

public interface IWindowInspector
{
    /// <summary>Visible, titled top-level windows of other processes, sorted by process name.</summary>
    IReadOnlyList<WindowInfo> GetCandidateWindows();

    WindowStatus GetStatus(nint window, int processId);

    /// <summary>Client area size in the window's own coordinate space (the space click points use).</summary>
    bool TryGetClientSize(nint window, out int width, out int height);
}

/// <summary>A pixel read from a window; <see cref="IsValid"/> is false when it could not be read.</summary>
public readonly record struct PixelSample(uint Color, bool IsValid)
{
    public static PixelSample Invalid => new(ClickPoint.NoColor, false);
}

public interface IPixelReader
{
    /// <summary>Reads the pixel at client (x, y) of <paramref name="window"/>.</summary>
    PixelSample Read(nint window, int x, int y);
}

public interface IElevationService
{
    bool IsCurrentProcessElevated { get; }

    ElevationState GetRelativeElevation(int processId);
}
