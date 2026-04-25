using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

/// <summary>
/// Single-file snapshot of the entire AutoClick state — settings, saved profiles,
/// and the current per-game queue. SchemaVersion bumps when the shape changes
/// in an incompatible way.
/// </summary>
public class SessionExport
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string AppVersion { get; set; } = string.Empty;
    public AppSettings Settings { get; set; } = new();
    public List<GameProfile> Profiles { get; set; } = new();
    public List<SavedGameSession> Games { get; set; } = new();
}

/// <summary>
/// Persistable snapshot of a queued game. Runtime-only fields (ProcessId,
/// WindowHandle, State, ClickCount, etc.) are intentionally excluded — on
/// import we re-attach by matching ProcessName + WindowTitle to a live window.
/// </summary>
public class SavedGameSession
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public List<ClickPoint> ClickPoints { get; set; } = new();
    public ClickProfile Profile { get; set; } = new();
    public int SequenceDelayMs { get; set; }
    public bool EnablePixelColorGuard { get; set; }
    public int ColorTolerance { get; set; } = 10;
    public ColorMismatchBehavior ColorMismatchBehavior { get; set; }
    public bool IsCustomMode { get; set; }
}
