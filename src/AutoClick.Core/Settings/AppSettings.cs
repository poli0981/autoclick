using System.Text.Json.Serialization;
using AutoClick.Core.Hotkeys;
using AutoClick.Core.Models;

namespace AutoClick.Core.Settings;

/// <summary>
/// Application settings, schema version 2 (stored in settings.v2.json). Immutable: change a copy
/// with <c>with</c> and save it through the settings store.
/// </summary>
public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 2;

    /// <summary>UI languages with a translation (resx) in the app.</summary>
    public static readonly IReadOnlyList<string> SupportedLanguages = ["en", "vi", "ja", "ko", "zh"];

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public SettingsMode SettingsMode { get; init; } = SettingsMode.Global;
    public ClickTiming DefaultTiming { get; init; } = new();
    public int DefaultSequenceDelayMs { get; init; } = Limits.DefaultSequenceDelayMs;
    public int MaxGamesInQueue { get; init; } = 10;
    public PixelGuardOptions PixelGuard { get; init; } = new();

    public AppTheme Theme { get; init; } = AppTheme.System;
    public string Language { get; init; } = "en";
    public ExitBehavior ExitBehavior { get; init; } = ExitBehavior.MinimizeToTray;
    public bool ShowRealTimeLogs { get; init; } = true;
    public bool SoundNotifications { get; init; } = true;
    public bool ShowGameExitNotification { get; init; } = true;
    public bool MinimizeOnStartAll { get; init; }
    public bool AutoUpdate { get; init; } = true;
    public bool VerboseLogging { get; init; }

    public HotkeySettings Hotkeys { get; init; } = new();

    /// <summary>Relaunch elevated at startup (needed only for games that run as administrator).</summary>
    public bool AlwaysRunAsAdmin { get; init; }

    /// <summary>One-time notice for users upgrading from v1, which always required administrator rights.</summary>
    public bool ShowAdminModeNotice { get; init; }

    public WindowPlacement? MainWindow { get; init; }

    /// <summary>Returns a copy with every value valid; unknown or broken values fall back to defaults.</summary>
    public AppSettings Normalize()
    {
        var defaults = new AppSettings();
        return this with
        {
            SchemaVersion = CurrentSchemaVersion,
            SettingsMode = Enum.IsDefined(SettingsMode) ? SettingsMode : defaults.SettingsMode,
            DefaultTiming = (DefaultTiming ?? defaults.DefaultTiming).Normalize(),
            DefaultSequenceDelayMs = Limits.ClampSequenceDelay(DefaultSequenceDelayMs),
            MaxGamesInQueue = Math.Clamp(MaxGamesInQueue, Limits.MinGamesInQueue, Limits.MaxGamesInQueue),
            PixelGuard = (PixelGuard ?? defaults.PixelGuard).Normalize(),
            Theme = Enum.IsDefined(Theme) ? Theme : defaults.Theme,
            Language = NormalizeLanguage(Language),
            ExitBehavior = Enum.IsDefined(ExitBehavior) ? ExitBehavior : defaults.ExitBehavior,
            Hotkeys = (Hotkeys ?? defaults.Hotkeys).Normalize(),
            MainWindow = MainWindow?.IsUsable == true ? MainWindow : null,
        };
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var code = language.Trim().ToLowerInvariant();
        var neutral = code.Split('-')[0];
        return SupportedLanguages.Contains(code) ? code : SupportedLanguages.Contains(neutral) ? neutral : "en";
    }
}

/// <summary>Global hotkeys as text, e.g. "F6" or "Ctrl+Shift+F8" (WPF key names).</summary>
public sealed record HotkeySettings
{
    public string PauseResume { get; init; } = "F6";
    public string StopAll { get; init; } = "F7";
    public string StartAll { get; init; } = "F8";

    /// <summary>Replaces unparsable or reserved hotkeys with the defaults, in canonical form.</summary>
    public HotkeySettings Normalize()
    {
        var defaults = new HotkeySettings();
        return new HotkeySettings
        {
            PauseResume = NormalizeOne(PauseResume, defaults.PauseResume),
            StopAll = NormalizeOne(StopAll, defaults.StopAll),
            StartAll = NormalizeOne(StartAll, defaults.StartAll),
        };
    }

    private static string NormalizeOne(string? value, string fallback) =>
        HotkeyGesture.TryParse(value, out var gesture) && gesture.Validate() == HotkeyValidation.Valid
            ? gesture.ToString()
            : fallback;
}

/// <summary>Main window bounds in device-independent pixels.</summary>
public sealed record WindowPlacement
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public bool IsMaximized { get; init; }

    [JsonIgnore]
    public bool IsUsable =>
        double.IsFinite(Left) && double.IsFinite(Top)
        && Width is >= 400 and <= 20_000 && Height is >= 300 and <= 20_000;
}
