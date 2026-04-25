using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class AppSettings
{
    public SettingsMode SettingsMode { get; set; } = SettingsMode.Global;
    public ClickMode DefaultClickMode { get; set; } = ClickMode.Random;
    public double DefaultFixedInterval { get; set; } = 2.0;
    public double RandomMin { get; set; } = 1.0;
    public double RandomMax { get; set; } = 60.0;
    public int MaxGamesInQueue { get; set; } = 10;
    public bool ShowRealTimeLogs { get; set; } = true;
    public ThemeMode Theme { get; set; } = ThemeMode.Dark; // default follows system on first launch
    public string Language { get; set; } = "en";
    public ExitBehavior ExitBehavior { get; set; } = ExitBehavior.MinimizeToTray;
    public bool AutoUpdate { get; set; } = true;
    public bool SoundNotifications { get; set; } = true;
    public bool ShowGameExitNotification { get; set; } = true;
    public bool MinimizeOnStartAll { get; set; } = false;
    public bool EnablePixelColorGuard { get; set; } = false;
    public int ColorTolerance { get; set; } = 10;
    public ColorMismatchBehavior ColorMismatchBehavior { get; set; } = ColorMismatchBehavior.StopSession;
    /// <summary>
    /// Maximum time the click loop will wait for a pixel to match its reference color
    /// when ColorMismatchBehavior is WaitUntilMatch. After timeout, the point is skipped.
    /// </summary>
    public int ColorWaitTimeoutMs { get; set; } = 5000;
    public HotkeySettings Hotkeys { get; set; } = new();
}

public class HotkeySettings
{
    public string PauseResume { get; set; } = "F6";
    public string StopAll { get; set; } = "F7";
    public string StartAll { get; set; } = "F8";
}
