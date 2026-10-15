using AutoClick.Core.Models;
using AutoClick.Core.Settings;

namespace AutoClick.Core.Sessions;

/// <summary>
/// Everything AutoClick needs to restore a setup on another machine: settings, saved
/// profiles and the game queue (.autoclick-session files). Schema 2; schema 1 files from v1
/// are read by <see cref="SessionFile"/>.
/// </summary>
public sealed record SessionExport
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAt { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public AppSettings Settings { get; init; } = new();
    public IReadOnlyList<GameProfile> Profiles { get; init; } = [];
    public IReadOnlyList<SavedGame> Games { get; init; } = [];

    public SessionExport Normalize() => this with
    {
        SchemaVersion = CurrentSchemaVersion,
        Settings = (Settings ?? new AppSettings()).Normalize(),
        Profiles = (Profiles ?? []).Select(p => p.Normalize()).ToList(),
        Games = (Games ?? []).Select(g => g.Normalize()).ToList(),
    };
}

/// <summary>
/// A queued game without its runtime state. On import it is matched to a running window by
/// process name and window title.
/// </summary>
public sealed record SavedGame
{
    public string ProcessName { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public IReadOnlyList<ClickPoint> ClickPoints { get; init; } = [];
    public ClickTiming Timing { get; init; } = new();
    public int SequenceDelayMs { get; init; } = Limits.DefaultSequenceDelayMs;
    public PixelGuardOptions PixelGuard { get; init; } = new();
    public bool IsCustomMode { get; init; }

    public SavedGame Normalize() => this with
    {
        ProcessName = ProcessName ?? string.Empty,
        WindowTitle = WindowTitle ?? string.Empty,
        ExecutablePath = ExecutablePath ?? string.Empty,
        ClickPoints = (ClickPoints ?? []).Select(p => p.Normalize()).ToList(),
        Timing = (Timing ?? new ClickTiming()).Normalize(),
        SequenceDelayMs = Limits.ClampSequenceDelay(SequenceDelayMs),
        PixelGuard = (PixelGuard ?? new PixelGuardOptions()).Normalize(),
    };
}
