using System.Diagnostics;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogService _logService;
    private readonly AppSettings _settings;
    private readonly Func<bool> _hasAnyRunning;

    public bool IsSettingsEnabled => !_hasAnyRunning();

    public int SelectedSettingsModeIndex
    {
        get => (int)_settings.SettingsMode;
        set
        {
            _settings.SettingsMode = (SettingsMode)Math.Clamp(value, 0, 1);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsGlobalMode));
            OnPropertyChanged(nameof(IsCustomMode));
            SettingsModeChanged?.Invoke();
        }
    }

    public bool IsGlobalMode => _settings.SettingsMode == SettingsMode.Global;
    public bool IsCustomMode => _settings.SettingsMode == SettingsMode.Custom;
    public bool IsGlobalParamsEnabled => IsGlobalMode && IsSettingsEnabled;

    public int SelectedClickModeIndex
    {
        get => _settings.DefaultClickMode == ClickMode.Random ? 0 : 1;
        set { _settings.DefaultClickMode = value == 0 ? ClickMode.Random : ClickMode.Fixed; OnPropertyChanged(); }
    }

    public double FixedInterval
    {
        get => _settings.DefaultFixedInterval;
        set { _settings.DefaultFixedInterval = Math.Clamp(value, 1, 60); OnPropertyChanged(); }
    }

    public double RandomMin
    {
        get => _settings.RandomMin;
        set { _settings.RandomMin = Math.Clamp(value, 1, _settings.RandomMax - 1); OnPropertyChanged(); }
    }

    public double RandomMax
    {
        get => _settings.RandomMax;
        set { _settings.RandomMax = Math.Clamp(value, _settings.RandomMin + 1, 60); OnPropertyChanged(); }
    }

    public int MaxGamesInQueue
    {
        get => _settings.MaxGamesInQueue;
        set { _settings.MaxGamesInQueue = Math.Clamp(value, 1, 50); OnPropertyChanged(); }
    }

    public bool ShowRealTimeLogs
    {
        get => _settings.ShowRealTimeLogs;
        set { _settings.ShowRealTimeLogs = value; OnPropertyChanged(); }
    }

    public bool AutoUpdate
    {
        get => _settings.AutoUpdate;
        set { _settings.AutoUpdate = value; OnPropertyChanged(); }
    }

    public bool SoundNotifications
    {
        get => _settings.SoundNotifications;
        set { _settings.SoundNotifications = value; OnPropertyChanged(); }
    }

    public bool ShowGameExitNotification
    {
        get => _settings.ShowGameExitNotification;
        set { _settings.ShowGameExitNotification = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Toggle: true = Dark, false = Light.
    /// </summary>
    public bool DarkMode
    {
        get => _settings.DarkMode;
        set
        {
            _settings.DarkMode = value;
            OnPropertyChanged();
            ThemeChanged?.Invoke();
        }
    }

    public string Language
    {
        get => _settings.Language;
        set
        {
            if (_settings.Language == value) return;
            _settings.Language = value;
            OnPropertyChanged();
            LanguageChanged?.Invoke(value);
            RestartRequested?.Invoke();
        }
    }

    public int SelectedExitBehaviorIndex
    {
        get => (int)_settings.ExitBehavior;
        set { _settings.ExitBehavior = (ExitBehavior)Math.Clamp(value, 0, 2); OnPropertyChanged(); }
    }

    public string HotkeyPauseResume
    {
        get => _settings.Hotkeys.PauseResume;
        set { _settings.Hotkeys.PauseResume = value; OnPropertyChanged(); }
    }

    public string HotkeyStopAll
    {
        get => _settings.Hotkeys.StopAll;
        set { _settings.Hotkeys.StopAll = value; OnPropertyChanged(); }
    }

    public string HotkeyStartAll
    {
        get => _settings.Hotkeys.StartAll;
        set { _settings.Hotkeys.StartAll = value; OnPropertyChanged(); }
    }

    // Hotkey capture
    private string? _capturingHotkeyFor;
    public string? CapturingHotkeyFor
    {
        get => _capturingHotkeyFor;
        set => SetProperty(ref _capturingHotkeyFor, value);
    }

    public bool IsCapturingPauseResume => _capturingHotkeyFor == "PauseResume";
    public bool IsCapturingStopAll => _capturingHotkeyFor == "StopAll";
    public bool IsCapturingStartAll => _capturingHotkeyFor == "StartAll";

    public RelayCommand SaveSettingsCommand { get; }
    public RelayCommand ExportLogCommand { get; }
    public RelayCommand ExportSettingsCommand { get; }
    public RelayCommand ImportSettingsCommand { get; }
    public RelayCommand<object?> CaptureHotkeyCommand { get; }
    public RelayCommand ResetAppCommand { get; }
    public RelayCommand OpenSettingsFileCommand { get; }

    public event Action? ThemeChanged;
    public event Action<string>? LanguageChanged;
    public event Action? SettingsImported;
    public event Action? ResetAppRequested;
    public event Action? SaveRequested;
    public event Action? RestartRequested;
    public event Action? SettingsModeChanged;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILogService logService,
        AppSettings settings,
        Func<bool> hasAnyRunning)
    {
        _settingsService = settingsService;
        _logService = logService;
        _settings = settings;
        _hasAnyRunning = hasAnyRunning;

        SaveSettingsCommand = new RelayCommand(OnSaveSettings);
        ExportLogCommand = new RelayCommand(OnExportLog);
        ExportSettingsCommand = new RelayCommand(OnExportSettings);
        ImportSettingsCommand = new RelayCommand(OnImportSettings);
        CaptureHotkeyCommand = new RelayCommand<object?>(OnStartCapture);
        ResetAppCommand = new RelayCommand(OnResetApp);
        OpenSettingsFileCommand = new RelayCommand(OnOpenSettingsFile);
    }

    public void RefreshRunningState()
    {
        OnPropertyChanged(nameof(IsSettingsEnabled));
        OnPropertyChanged(nameof(IsGlobalParamsEnabled));
    }

    /// <summary>
    /// Refreshes ALL property bindings. Called after ResetApp to update UI.
    /// </summary>
    public void RefreshAllBindings()
    {
        OnPropertyChanged(nameof(SelectedSettingsModeIndex));
        OnPropertyChanged(nameof(IsGlobalMode));
        OnPropertyChanged(nameof(IsCustomMode));
        OnPropertyChanged(nameof(IsGlobalParamsEnabled));
        OnPropertyChanged(nameof(SelectedClickModeIndex));
        OnPropertyChanged(nameof(FixedInterval));
        OnPropertyChanged(nameof(RandomMin));
        OnPropertyChanged(nameof(RandomMax));
        OnPropertyChanged(nameof(MaxGamesInQueue));
        OnPropertyChanged(nameof(ShowRealTimeLogs));
        OnPropertyChanged(nameof(AutoUpdate));
        OnPropertyChanged(nameof(SoundNotifications));
        OnPropertyChanged(nameof(ShowGameExitNotification));
        OnPropertyChanged(nameof(DarkMode));
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(SelectedExitBehaviorIndex));
        OnPropertyChanged(nameof(HotkeyPauseResume));
        OnPropertyChanged(nameof(HotkeyStopAll));
        OnPropertyChanged(nameof(HotkeyStartAll));
        OnPropertyChanged(nameof(IsSettingsEnabled));
    }

    public void HandleCapturedKey(string keyName)
    {
        if (_capturingHotkeyFor == null) return;
        switch (_capturingHotkeyFor)
        {
            case "PauseResume": HotkeyPauseResume = keyName; break;
            case "StopAll": HotkeyStopAll = keyName; break;
            case "StartAll": HotkeyStartAll = keyName; break;
        }
        _logService.Info($"Hotkey for \"{_capturingHotkeyFor}\" set to: {keyName}");
        CapturingHotkeyFor = null;
        OnPropertyChanged(nameof(IsCapturingPauseResume));
        OnPropertyChanged(nameof(IsCapturingStopAll));
        OnPropertyChanged(nameof(IsCapturingStartAll));
    }

    public void CancelCapture()
    {
        CapturingHotkeyFor = null;
        OnPropertyChanged(nameof(IsCapturingPauseResume));
        OnPropertyChanged(nameof(IsCapturingStopAll));
        OnPropertyChanged(nameof(IsCapturingStartAll));
    }

    private void OnStartCapture(object? param)
    {
        if (param is string target)
        {
            CapturingHotkeyFor = target;
            OnPropertyChanged(nameof(IsCapturingPauseResume));
            OnPropertyChanged(nameof(IsCapturingStopAll));
            OnPropertyChanged(nameof(IsCapturingStartAll));
        }
    }

    private void OnSaveSettings()
    {
        SaveRequested?.Invoke();
    }

    private void OnResetApp() => ResetAppRequested?.Invoke();

    private void OnOpenSettingsFile()
    {
        try
        {
            var path = _settingsService.SettingsFilePath;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to open settings file", ex);
        }
    }

    private void OnExportLog()
    {
        var dlg = new SaveFileDialog { Filter = "Log files (*.log)|*.log", FileName = $"autoclick-{DateTime.Now:yyyyMMdd-HHmmss}.log" };
        if (dlg.ShowDialog() == true) { _logService.ExportLog(dlg.FileName); _logService.Info($"Log exported to {dlg.FileName}"); }
    }

    private void OnExportSettings()
    {
        var dlg = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "autoclick-settings.json" };
        if (dlg.ShowDialog() == true) { _settingsService.Export(dlg.FileName, _settings); _logService.Info($"Settings exported to {dlg.FileName}"); }
    }

    private void OnImportSettings()
    {
        var dlg = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var imported = _settingsService.Import(dlg.FileName);
                _settings.DefaultClickMode = imported.DefaultClickMode;
                _settings.DefaultFixedInterval = imported.DefaultFixedInterval;
                _settings.RandomMin = imported.RandomMin;
                _settings.RandomMax = imported.RandomMax;
                _settings.MaxGamesInQueue = imported.MaxGamesInQueue;
                _settings.ShowRealTimeLogs = imported.ShowRealTimeLogs;
                _settings.DarkMode = imported.DarkMode;
                _settings.Language = imported.Language;
                _settings.Hotkeys = imported.Hotkeys;
                _settingsService.Save(_settings);
                RefreshAllBindings();
                ThemeChanged?.Invoke();
                SettingsImported?.Invoke();
                _logService.Info($"Settings imported from {dlg.FileName}");
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to import settings", ex);
            }
        }
    }
}
