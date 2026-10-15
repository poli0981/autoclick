namespace AutoClick.Core.Models;

// Numeric values are part of the v1 file formats (profiles store enums as numbers): never renumber.

public enum ClickType
{
    LeftClick = 0,
    DoubleClick = 1,
    RightClick = 2,

    /// <summary>Send a key press (WM_KEYDOWN/WM_KEYUP) instead of a mouse click; X/Y are ignored.</summary>
    Keystroke = 3,
}

public enum ClickMode
{
    Random = 0,
    Fixed = 1,
}

public enum ColorMismatchBehavior
{
    SkipPoint = 0,
    StopSession = 1,

    /// <summary>Poll until the pixel matches or the wait timeout elapses (then skip the point).</summary>
    WaitUntilMatch = 2,
}

public enum ExitBehavior
{
    MinimizeToTray = 0,
    StopAndExit = 1,
    ForceExit = 2,
}

public enum SettingsMode
{
    /// <summary>Every game uses the global click timing from Settings.</summary>
    Global = 0,

    /// <summary>Each game has its own timing and can save/load profiles.</summary>
    Custom = 1,
}

/// <summary>Named AppTheme (not ThemeMode) to avoid clashing with System.Windows.ThemeMode.</summary>
public enum AppTheme
{
    Dark = 0,
    Light = 1,
    HighContrast = 2,

    /// <summary>Follow the Windows app theme (and high contrast) setting.</summary>
    System = 3,
}

public enum SessionState
{
    Idle,
    Running,
    Paused,
    Stopped,
}

/// <summary>Why a click session stopped.</summary>
public enum StopReason
{
    None,
    User,
    WindowClosed,
    OutOfBounds,
    ColorMismatch,

    /// <summary>The game runs with higher privileges; Windows blocks input from AutoClick (UIPI).</summary>
    ElevationRequired,

    /// <summary>The game's message queue is full (the game is not responding).</summary>
    TargetNotResponding,
    Scheduler,
    AppExit,
    Error,
}
