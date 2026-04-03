using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class AppSettings
{
    public ClickMode DefaultClickMode { get; set; } = ClickMode.Random;
    public double DefaultFixedInterval { get; set; } = 2.0;
    public double RandomMin { get; set; } = 1.0;
    public double RandomMax { get; set; } = 60.0;
    public int MaxGamesInQueue { get; set; } = 10;
    public bool ShowRealTimeLogs { get; set; } = true;
    public bool DarkMode { get; set; } = true; // true=dark, false=light; default follows system at first launch
    public string Language { get; set; } = "en";
    public ExitBehavior ExitBehavior { get; set; } = ExitBehavior.MinimizeToTray;
    public bool AutoUpdate { get; set; } = true;
    public HotkeySettings Hotkeys { get; set; } = new();
}

public class HotkeySettings
{
    public string PauseResume { get; set; } = "F6";
    public string StopAll { get; set; } = "F7";
    public string StartAll { get; set; } = "F8";
}
