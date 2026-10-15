using System.Text.Json;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Hotkeys;
using AutoClick.Core.Models;
using AutoClick.Core.Settings;
using AutoClick.Services;
using AutoClick.Services.Persistence;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.ViewModels;

/// <summary>
/// Settings page. Every change is saved immediately (no "Save" button); values are clamped by
/// <see cref="AppSettings.Normalize"/> and read back, so the page always shows what is stored.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _settings;
    private readonly HotkeyService _hotkeys;
    private readonly DialogService _dialogs;
    private readonly AppLifetime _lifetime;
    private readonly ShellLauncher _shell;
    private readonly AppPaths _paths;
    private readonly ILogger<SettingsViewModel> _logger;
    private bool _loading;

    public SettingsViewModel(
        SettingsStore settings,
        HotkeyService hotkeys,
        DialogService dialogs,
        AppLifetime lifetime,
        ShellLauncher shell,
        AppPaths paths,
        IElevationService elevation,
        ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _hotkeys = hotkeys;
        _dialogs = dialogs;
        _lifetime = lifetime;
        _shell = shell;
        _paths = paths;
        _logger = logger;
        IsElevated = elevation.IsCurrentProcessElevated;
        Load(settings.Current);
        settings.Changed += (_, current) => Load(current);
        hotkeys.RegistrationChanged += (_, _) => UpdateHotkeyStatus();
        LocalizationService.Instance.LanguageChanged += (_, _) => UpdateHotkeyStatus();
    }

    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en", "English"),
        new("vi", "Tiếng Việt"),
        new("ja", "日本語"),
        new("ko", "한국어"),
        new("zh", "中文（简体）"),
    ];

    public static IReadOnlyList<Choice> Themes { get; } =
    [
        new(AppTheme.System, "Theme_System"),
        new(AppTheme.Light, "Theme_Light"),
        new(AppTheme.Dark, "Theme_Dark"),
        new(AppTheme.HighContrast, "Theme_HighContrast"),
    ];

    public static IReadOnlyList<Choice> ExitBehaviors { get; } =
    [
        new(ExitBehavior.MinimizeToTray, "Exit_MinimizeToTray"),
        new(ExitBehavior.StopAndExit, "Exit_StopAndExit"),
        new(ExitBehavior.ForceExit, "Exit_ForceExit"),
    ];

    public static IReadOnlyList<Choice> SettingsModes { get; } =
    [
        new(SettingsMode.Global, "Mode_Global"),
        new(SettingsMode.Custom, "Mode_Custom"),
    ];

    public static IReadOnlyList<Choice> TimingModes => GameSessionViewModel.ModeOptions;

    public static IReadOnlyList<Choice> MismatchBehaviors { get; } =
    [
        new(ColorMismatchBehavior.SkipPoint, "Guard_Skip"),
        new(ColorMismatchBehavior.StopSession, "Guard_Stop"),
        new(ColorMismatchBehavior.WaitUntilMatch, "Guard_Wait"),
    ];

    public bool IsElevated { get; }

    public string DataFolder => _paths.DataDirectory;

    [ObservableProperty]
    public partial string Language { get; set; } = "en";

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    [ObservableProperty]
    public partial ExitBehavior ExitBehavior { get; set; }

    [ObservableProperty]
    public partial bool MinimizeOnStartAll { get; set; }

    [ObservableProperty]
    public partial bool SoundNotifications { get; set; }

    [ObservableProperty]
    public partial bool ShowGameExitNotification { get; set; }

    [ObservableProperty]
    public partial bool ShowRealTimeLogs { get; set; }

    [ObservableProperty]
    public partial bool AutoUpdate { get; set; }

    [ObservableProperty]
    public partial bool VerboseLogging { get; set; }

    [ObservableProperty]
    public partial bool AlwaysRunAsAdmin { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomMode))]
    public partial SettingsMode SettingsMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedTiming))]
    public partial ClickMode TimingMode { get; set; }

    [ObservableProperty]
    public partial double FixedInterval { get; set; }

    [ObservableProperty]
    public partial double RandomMin { get; set; }

    [ObservableProperty]
    public partial double RandomMax { get; set; }

    [ObservableProperty]
    public partial double SequenceDelayMs { get; set; }

    [ObservableProperty]
    public partial double MaxGamesInQueue { get; set; }

    [ObservableProperty]
    public partial bool PixelGuardEnabled { get; set; }

    [ObservableProperty]
    public partial double ColorTolerance { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaitBehavior))]
    public partial ColorMismatchBehavior MismatchBehavior { get; set; }

    [ObservableProperty]
    public partial double ColorWaitTimeoutMs { get; set; }

    [ObservableProperty]
    public partial string PauseResumeHotkey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StopAllHotkey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartAllHotkey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PauseResumeStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StopAllStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartAllStatus { get; set; } = string.Empty;

    public bool IsCustomMode => SettingsMode == SettingsMode.Custom;

    public bool IsFixedTiming => TimingMode == ClickMode.Fixed;

    public bool IsWaitBehavior => MismatchBehavior == ColorMismatchBehavior.WaitUntilMatch;

    public static double MinInterval => Limits.MinIntervalSeconds;

    public static double MaxInterval => Limits.MaxIntervalSeconds;

    public static double MaxSequenceDelay => Limits.MaxSequenceDelayMs;

    public static double MinGames => Limits.MinGamesInQueue;

    public static double MaxGames => Limits.MaxGamesInQueue;

    public static double MaxTolerance => Limits.MaxColorTolerance;

    public static double MinWaitTimeout => Limits.MinColorWaitTimeoutMs;

    public static double MaxWaitTimeout => Limits.MaxColorWaitTimeoutMs;

    [RelayCommand]
    public async Task RecordHotkeyAsync(HotkeyAction action)
    {
        _hotkeys.Suspend();
        try
        {
            var gesture = await _dialogs.CaptureHotkeyAsync(L.Get(ActionKey(action)));
            if (gesture is not { } captured)
            {
                return;
            }

            var text = captured.ToString();
            var current = _settings.Current.Hotkeys;
            var clash = Enum.GetValues<HotkeyAction>()
                .Where(other => other != action)
                .FirstOrDefault(other => string.Equals(Get(current, other), text, StringComparison.OrdinalIgnoreCase));
            if (clash != default)
            {
                await _dialogs.AlertAsync(L.Get("Hotkey_Title"), L.Format("Hotkey_Duplicate", text, L.Get(ActionKey(clash))));
                return;
            }

            _settings.Update(s => s with { Hotkeys = Set(s.Hotkeys, action, text) });
            _logger.LogInformation("Hotkey for {Action} set to {Gesture}", action, text);
        }
        finally
        {
            _hotkeys.Resume();
        }
    }

    [RelayCommand]
    public void ResetHotkeys() => _settings.Update(s => s with { Hotkeys = new HotkeySettings() });

    [RelayCommand]
    public void ExportSettings()
    {
        var path = DialogService.PickSaveFile(L.Get("Filter_Settings"), "autoclick-settings.json");
        if (path is null)
        {
            return;
        }

        try
        {
            _settings.Export(path);
            _dialogs.Toast(L.Get("Settings_Export"), L.Format("Common_SavedTo", path), ToastKind.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.Toast(L.Get("Settings_Export"), L.Format("Common_FileError", ex.Message), ToastKind.Error);
        }
    }

    [RelayCommand]
    public async Task ImportSettingsAsync()
    {
        var path = DialogService.PickOpenFile(L.Get("Filter_Settings"));
        if (path is null)
        {
            return;
        }

        try
        {
            _settings.Import(path);
            _dialogs.Toast(L.Get("Settings_Import"), L.Get("Settings_Imported"), ToastKind.Success);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Settings import failed");
            await _dialogs.AlertAsync(L.Get("Settings_Import"), L.Format("Settings_ImportFailed", ex.Message));
        }
    }

    [RelayCommand]
    public async Task ResetSettingsAsync()
    {
        if (await _dialogs.ConfirmAsync(L.Get("Settings_Reset"), L.Get("Settings_ResetConfirm"), L.Get("Settings_Reset")))
        {
            _settings.Reset();
            _logger.LogInformation("Settings reset to defaults");
        }
    }

    [RelayCommand]
    public void OpenDataFolder()
    {
        Directory.CreateDirectory(_paths.DataDirectory);
        _shell.TryOpen(_paths.DataDirectory);
    }

    [RelayCommand]
    public async Task RestartAsAdministratorAsync()
    {
        if (!await _lifetime.RestartAsAdministratorAsync())
        {
            _dialogs.Toast(L.Get("Admin_Restart"), L.Get("Admin_RestartCancelled"), ToastKind.Warning);
        }
    }

    partial void OnLanguageChanged(string value) => Save(s => s with { Language = value });

    partial void OnThemeChanged(AppTheme value) => Save(s => s with { Theme = value });

    partial void OnExitBehaviorChanged(ExitBehavior value) => Save(s => s with { ExitBehavior = value });

    partial void OnMinimizeOnStartAllChanged(bool value) => Save(s => s with { MinimizeOnStartAll = value });

    partial void OnSoundNotificationsChanged(bool value) => Save(s => s with { SoundNotifications = value });

    partial void OnShowGameExitNotificationChanged(bool value) => Save(s => s with { ShowGameExitNotification = value });

    partial void OnShowRealTimeLogsChanged(bool value) => Save(s => s with { ShowRealTimeLogs = value });

    partial void OnAutoUpdateChanged(bool value) => Save(s => s with { AutoUpdate = value });

    partial void OnVerboseLoggingChanged(bool value) => Save(s => s with { VerboseLogging = value });

    partial void OnAlwaysRunAsAdminChanged(bool value) => Save(s => s with { AlwaysRunAsAdmin = value, ShowAdminModeNotice = false });

    partial void OnSettingsModeChanged(SettingsMode value) => Save(s => s with { SettingsMode = value });

    partial void OnTimingModeChanged(ClickMode value) => Save(s => s with { DefaultTiming = s.DefaultTiming with { Mode = value } });

    partial void OnFixedIntervalChanged(double value) => Save(s => s with { DefaultTiming = s.DefaultTiming with { FixedIntervalSeconds = value } });

    partial void OnRandomMinChanged(double value) => Save(s => s with { DefaultTiming = s.DefaultTiming with { RandomMinSeconds = value } });

    partial void OnRandomMaxChanged(double value) => Save(s => s with { DefaultTiming = s.DefaultTiming with { RandomMaxSeconds = value } });

    partial void OnSequenceDelayMsChanged(double value) => Save(s => s with { DefaultSequenceDelayMs = (int)Math.Round(value) });

    partial void OnMaxGamesInQueueChanged(double value) => Save(s => s with { MaxGamesInQueue = (int)Math.Round(value) });

    partial void OnPixelGuardEnabledChanged(bool value) => Save(s => s with { PixelGuard = s.PixelGuard with { Enabled = value } });

    partial void OnColorToleranceChanged(double value) => Save(s => s with { PixelGuard = s.PixelGuard with { Tolerance = (int)Math.Round(value) } });

    partial void OnMismatchBehaviorChanged(ColorMismatchBehavior value) => Save(s => s with { PixelGuard = s.PixelGuard with { MismatchBehavior = value } });

    partial void OnColorWaitTimeoutMsChanged(double value) => Save(s => s with { PixelGuard = s.PixelGuard with { WaitTimeoutMs = (int)Math.Round(value) } });

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (_loading)
        {
            return;
        }

        var saved = _settings.Update(change);
        // Show the stored (clamped) values, e.g. a minimum above the maximum is swapped.
        Load(saved);
    }

    private void Load(AppSettings settings)
    {
        _loading = true;
        try
        {
            Language = settings.Language;
            Theme = settings.Theme;
            ExitBehavior = settings.ExitBehavior;
            MinimizeOnStartAll = settings.MinimizeOnStartAll;
            SoundNotifications = settings.SoundNotifications;
            ShowGameExitNotification = settings.ShowGameExitNotification;
            ShowRealTimeLogs = settings.ShowRealTimeLogs;
            AutoUpdate = settings.AutoUpdate;
            VerboseLogging = settings.VerboseLogging;
            AlwaysRunAsAdmin = settings.AlwaysRunAsAdmin;
            SettingsMode = settings.SettingsMode;
            TimingMode = settings.DefaultTiming.Mode;
            FixedInterval = settings.DefaultTiming.FixedIntervalSeconds;
            RandomMin = settings.DefaultTiming.RandomMinSeconds;
            RandomMax = settings.DefaultTiming.RandomMaxSeconds;
            SequenceDelayMs = settings.DefaultSequenceDelayMs;
            MaxGamesInQueue = settings.MaxGamesInQueue;
            PixelGuardEnabled = settings.PixelGuard.Enabled;
            ColorTolerance = settings.PixelGuard.Tolerance;
            MismatchBehavior = settings.PixelGuard.MismatchBehavior;
            ColorWaitTimeoutMs = settings.PixelGuard.WaitTimeoutMs;
            PauseResumeHotkey = settings.Hotkeys.PauseResume;
            StopAllHotkey = settings.Hotkeys.StopAll;
            StartAllHotkey = settings.Hotkeys.StartAll;
        }
        finally
        {
            _loading = false;
        }
    }

    private void UpdateHotkeyStatus()
    {
        PauseResumeStatus = StatusText(HotkeyAction.PauseResume, PauseResumeHotkey);
        StopAllStatus = StatusText(HotkeyAction.StopAll, StopAllHotkey);
        StartAllStatus = StatusText(HotkeyAction.StartAll, StartAllHotkey);
    }

    private string StatusText(HotkeyAction action, string hotkey)
    {
        if (!_hotkeys.Results.TryGetValue(action, out var result))
        {
            return string.Empty;
        }

        return result switch
        {
            HotkeyRegistrationResult.InUse => L.Get("Hotkey_InUse"),
            HotkeyRegistrationResult.NotAllowed => L.Get("Hotkey_NotAllowed"),
            HotkeyRegistrationResult.Failed => L.Get("Hotkey_Failed"),
            _ when HotkeyGesture.TryParse(hotkey, out var gesture) && !gesture.HasModifiers => L.Get("Hotkey_NoModifierHint"),
            _ => string.Empty,
        };
    }

    private static string ActionKey(HotkeyAction action) => action switch
    {
        HotkeyAction.PauseResume => "Hotkey_PauseResume",
        HotkeyAction.StopAll => "Hotkey_StopAll",
        _ => "Hotkey_StartAll",
    };

    private static string Get(HotkeySettings hotkeys, HotkeyAction action) => action switch
    {
        HotkeyAction.PauseResume => hotkeys.PauseResume,
        HotkeyAction.StopAll => hotkeys.StopAll,
        _ => hotkeys.StartAll,
    };

    private static HotkeySettings Set(HotkeySettings hotkeys, HotkeyAction action, string value) => action switch
    {
        HotkeyAction.PauseResume => hotkeys with { PauseResume = value },
        HotkeyAction.StopAll => hotkeys with { StopAll = value },
        _ => hotkeys with { StartAll = value },
    };
}
