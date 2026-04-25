using System.Collections.ObjectModel;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
using AutoClick.Services;
using AutoClick.UI.Resources;
using AutoClick.Win32;

namespace AutoClick.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IClickEngine _clickEngine;
    private readonly IGameDetector _gameDetector;
    private readonly ISettingsService _settingsService;
    private readonly ILogService _logService;
    private readonly IMemoryManager _memoryManager;
    private readonly IProfileService _profileService;
    private SoundService? _soundService;

    public ObservableCollection<GameSessionViewModel> GameSessions { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();
    public ObservableCollection<GameProfile> SavedProfiles { get; } = new();

    private GameSessionViewModel? _selectedSession;
    public GameSessionViewModel? SelectedSession
    {
        get => _selectedSession;
        set => SetProperty(ref _selectedSession, value);
    }

    private AppSettings _settings;
    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    private int _activeGameCount;
    public int ActiveGameCount { get => _activeGameCount; set => SetProperty(ref _activeGameCount, value); }

    private bool _showLogs = true;
    public bool ShowLogs { get => _showLogs; set => SetProperty(ref _showLogs, value); }

    // ── Session Statistics ──
    private DateTime? _sessionStartedAt;

    private long _totalClicks;
    public long TotalClicks { get => _totalClicks; set => SetProperty(ref _totalClicks, value); }

    private long _totalSkipped;
    public long TotalSkipped { get => _totalSkipped; set => SetProperty(ref _totalSkipped, value); }

    private string _sessionUptime = "00:00:00";
    public string SessionUptime { get => _sessionUptime; set => SetProperty(ref _sessionUptime, value); }

    private double _clicksPerMinute;
    public double ClicksPerMinute { get => _clicksPerMinute; set => SetProperty(ref _clicksPerMinute, value); }

    private long _peakClicksPerMinute;
    public long PeakClicksPerMinute { get => _peakClicksPerMinute; set => SetProperty(ref _peakClicksPerMinute, value); }

    public bool HasStats => _sessionStartedAt != null;

    public bool HasAnyRunning => GameSessions.Any(g =>
        g.Session.State == SessionState.Running || g.Session.State == SessionState.Paused);

    public bool HasNoRunning => !HasAnyRunning;

    /// <summary>
    /// Start All is only enabled if there are idle games AND all games have coordinates.
    /// </summary>
    public bool CanStartAll => GameSessions.Any(g => g.IsIdle) &&
                               GameSessions.All(g => g.HasCoordinate);

    public RelayCommand AddGameCommand { get; }
    public RelayCommand<object?> RemoveGameCommand { get; }
    public RelayCommand StartAllCommand { get; }
    public RelayCommand StopAllCommand { get; }
    public RelayCommand RemoveAllCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand ResetAppCommand { get; }
    public RelayCommand ToggleSchedulerCommand { get; }

    // ── Scheduler ──
    private bool _isSchedulerEnabled;
    public bool IsSchedulerEnabled { get => _isSchedulerEnabled; set => SetProperty(ref _isSchedulerEnabled, value); }

    public bool HasScheduler => _isSchedulerEnabled;

    private string _scheduledStartTimeText = "";
    public string ScheduledStartTimeText { get => _scheduledStartTimeText; set => SetProperty(ref _scheduledStartTimeText, value); }

    private string _scheduledStopTimeText = "";
    public string ScheduledStopTimeText { get => _scheduledStopTimeText; set => SetProperty(ref _scheduledStopTimeText, value); }

    private DateTime? _scheduledStartTime;
    private DateTime? _scheduledStopTime;
    private bool _schedulerStarted;

    private string _schedulerCountdown = "";
    public string SchedulerCountdown { get => _schedulerCountdown; set => SetProperty(ref _schedulerCountdown, value); }

    private string _scheduleButtonText = Strings.Schedule;
    public string ScheduleButtonText { get => _scheduleButtonText; set => SetProperty(ref _scheduleButtonText, value); }

    public event Action? RequestAddGame;

    public event Action? SettingsReloaded;

    /// <summary>
    /// Fired when a game process exits. Args: processName, clickCount, skippedClicks.
    /// Used by dashboard to archive stats of exited games.
    /// </summary>
    public event Action<string, long, long>? GameExited;

    private readonly ISessionExportService _sessionExportService;

    public MainViewModel(
        IClickEngine clickEngine,
        IGameDetector gameDetector,
        ISettingsService settingsService,
        ILogService logService,
        IMemoryManager memoryManager,
        IProfileService profileService,
        ISessionExportService sessionExportService)
    {
        _clickEngine = clickEngine;
        _gameDetector = gameDetector;
        _settingsService = settingsService;
        _logService = logService;
        _memoryManager = memoryManager;
        _profileService = profileService;
        _sessionExportService = sessionExportService;

        _settings = _settingsService.Load();
        _showLogs = _settings.ShowRealTimeLogs;

        AddGameCommand = new RelayCommand(OnAddGame, () => GameSessions.Count < _settings.MaxGamesInQueue);
        RemoveGameCommand = new RelayCommand<object?>(OnRemoveGame);
        StartAllCommand = new RelayCommand(OnStartAll, () => CanStartAll);
        StopAllCommand = new RelayCommand(OnStopAll, () => HasAnyRunning);
        RemoveAllCommand = new RelayCommand(OnRemoveAll, () => HasNoRunning && GameSessions.Count > 0);
        ResetAllCommand = new RelayCommand(OnResetAll);
        ResetAppCommand = new RelayCommand(OnResetApp);
        ToggleSchedulerCommand = new RelayCommand(OnToggleScheduler);

        _logService.LogReceived += entry =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(entry);
                if (LogEntries.Count > 500)
                    LogEntries.RemoveAt(0);
            });
        };

        // UI refresh + process monitor timer
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, _) =>
        {
            ActiveGameCount = GameSessions.Count(g => g.Session.State == SessionState.Running);
            OnPropertyChanged(nameof(HasAnyRunning));
            OnPropertyChanged(nameof(HasNoRunning));
            OnPropertyChanged(nameof(CanStartAll));
            RefreshSessionStats();
            CheckForExitedProcesses();
            CheckScheduler();
        };
        timer.Start();

        RefreshProfiles();
    }

    public void SetSoundService(SoundService sound) => _soundService = sound;

    public IGameDetector GameDetector => _gameDetector;

    public List<GameWindowInfo> GetAvailableWindows() => _gameDetector.GetRunningWindows();

    public void UpdateShowLogs(bool show) => ShowLogs = show;

    public bool IsGameAlreadyInQueue(int processId)
        => GameSessions.Any(g => g.Session.ProcessId == processId);

    /// <summary>
    /// Checks if any other game already uses the same coordinate.
    /// </summary>
    public string? CheckDuplicateCoordinate(IntPtr windowHandle, ClickPoint point, string? excludeSessionId = null)
    {
        foreach (var g in GameSessions)
        {
            if (excludeSessionId != null && g.Id == excludeSessionId) continue;
            if (g.Session.WindowHandle == windowHandle)
            {
                foreach (var existing in g.Session.ClickPoints)
                {
                    if (existing.X == point.X && existing.Y == point.Y)
                        return g.ProcessName;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Adds a picked coordinate to a game session's click sequence. Validates bounds + duplicates.
    /// </summary>
    public bool TryAddCoordinate(GameSessionViewModel sessionVm, ClickPoint point)
    {
        // Validate bounds
        if (!CoordinateHelper.IsCoordinateInBounds(sessionVm.Session.WindowHandle, point.X, point.Y))
        {
            _logService.Warn($"Coordinate ({point.X}, {point.Y}) is outside the window of \"{sessionVm.ProcessName}\". Rejected.");
            return false;
        }

        // Check duplicate within same session
        foreach (var existing in sessionVm.Session.ClickPoints)
        {
            if (existing.X == point.X && existing.Y == point.Y)
            {
                _logService.Warn($"Coordinate ({point.X}, {point.Y}) already in sequence for \"{sessionVm.ProcessName}\". Rejected.");
                return false;
            }
        }

        sessionVm.Session.ClickPoints.Add(point);
        sessionVm.UpdateCoordinateText();
        var idx = sessionVm.Session.ClickPoints.Count;
        _logService.Info($"Coordinate #{idx} ({point.X}, {point.Y}) added to \"{sessionVm.ProcessName}\" (picked)");
        OnPropertyChanged(nameof(CanStartAll));
        return true;
    }

    /// <summary>
    /// Clears all coordinates from a game session.
    /// </summary>
    public void ClearCoordinates(GameSessionViewModel sessionVm)
    {
        sessionVm.Session.ClickPoints.Clear();
        sessionVm.UpdateCoordinateText();
        _logService.Info($"All coordinates cleared for \"{sessionVm.ProcessName}\"");
        OnPropertyChanged(nameof(CanStartAll));
    }

    /// <summary>
    /// Generates a random coordinate within the game window, checks for duplicates, and assigns it.
    /// Retries up to 10 times to avoid duplicates.
    /// </summary>
    public bool TryAddRandomCoordinate(GameSessionViewModel sessionVm)
    {
        var hWnd = sessionVm.Session.WindowHandle;
        var (w, h) = CoordinateHelper.GetClientSize(hWnd);

        if (w <= 0 || h <= 0)
        {
            _logService.Warn($"Cannot generate random coordinate for \"{sessionVm.ProcessName}\": window size is invalid ({w}x{h}).");
            return false;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var point = CoordinateHelper.GenerateRandomCoordinate(hWnd);

            // Check duplicate within same session
            bool duplicate = sessionVm.Session.ClickPoints.Any(p => p.X == point.X && p.Y == point.Y);
            if (!duplicate)
            {
                sessionVm.Session.ClickPoints.Add(point);
                sessionVm.UpdateCoordinateText();
                var idx = sessionVm.Session.ClickPoints.Count;
                _logService.Info($"Random coordinate #{idx} ({point.X}, {point.Y}) added to \"{sessionVm.ProcessName}\" (window: {w}x{h})");
                OnPropertyChanged(nameof(CanStartAll));
                return true;
            }
        }

        _logService.Warn($"Failed to generate unique random coordinate for \"{sessionVm.ProcessName}\" after 10 attempts.");
        return false;
    }

    public GameSessionViewModel? TryAddGameSession(GameWindowInfo window)
    {
        if (IsGameAlreadyInQueue(window.ProcessId))
        {
            _logService.Warn($"Game \"{window.ProcessName}\" (PID: {window.ProcessId}) is already in the queue. Skipped.");
            return null;
        }

        if (GameSessions.Count >= _settings.MaxGamesInQueue)
        {
            _logService.Warn($"Queue is full ({_settings.MaxGamesInQueue} games max). Cannot add \"{window.ProcessName}\".");
            return null;
        }

        var session = new GameSession
        {
            ProcessName = window.ProcessName,
            WindowTitle = window.Title,
            ExecutablePath = window.ExecutablePath,
            ProcessId = window.ProcessId,
            WindowHandle = window.Handle,
            Profile = new ClickProfile
            {
                Mode = _settings.DefaultClickMode,
                FixedIntervalSeconds = _settings.DefaultFixedInterval,
                RandomMinSeconds = _settings.RandomMin,
                RandomMaxSeconds = _settings.RandomMax
            },
            EnablePixelColorGuard = _settings.EnablePixelColorGuard,
            ColorTolerance = _settings.ColorTolerance,
            ColorMismatchBehavior = _settings.ColorMismatchBehavior
        };

        var vm = new GameSessionViewModel(session, _clickEngine, _logService, _soundService);
        vm.IsCustomMode = _settings.SettingsMode == SettingsMode.Custom;
        GameSessions.Add(vm);
        _logService.Info($"Added game \"{window.ProcessName}\" (PID: {window.ProcessId}) to queue");
        return vm;
    }

    /// <summary>
    /// Reload settings from the model (called after settings are saved).
    /// Applies new values to existing sessions.
    /// </summary>
    public void ReloadSettingsToSessions()
    {
        var isCustom = _settings.SettingsMode == SettingsMode.Custom;

        if (!isCustom)
        {
            foreach (var g in GameSessions.Where(g => g.IsIdle))
            {
                g.Session.Profile.Mode = _settings.DefaultClickMode;
                g.Session.Profile.FixedIntervalSeconds = _settings.DefaultFixedInterval;
                g.Session.Profile.RandomMinSeconds = _settings.RandomMin;
                g.Session.Profile.RandomMaxSeconds = _settings.RandomMax;
            }
        }

        foreach (var g in GameSessions)
        {
            g.IsCustomMode = isCustom;

            // Sync pixel color guard settings to all sessions
            g.Session.EnablePixelColorGuard = _settings.EnablePixelColorGuard;
            g.Session.ColorTolerance = _settings.ColorTolerance;
            g.Session.ColorMismatchBehavior = _settings.ColorMismatchBehavior;
        }

        _showLogs = _settings.ShowRealTimeLogs;
        OnPropertyChanged(nameof(ShowLogs));
        _logService.Info("Settings saved and applied");
    }

    private void CheckForExitedProcesses()
    {
        var toRemove = GameSessions
            .Where(g => !_gameDetector.IsProcessAlive(g.Session.ProcessId) ||
                        !_gameDetector.IsWindowValid(g.Session.WindowHandle))
            .ToList();

        foreach (var vm in toRemove)
        {
            if (vm.IsRunning || vm.IsPaused)
                vm.Stop();

            // Archive stats before removing
            GameExited?.Invoke(vm.ProcessName, vm.Session.ClickCount, vm.Session.SkippedClicks);

            _logService.Info($"Game \"{vm.ProcessName}\" (PID: {vm.Session.ProcessId}) has exited. Total clicks: {vm.Session.ClickCount}. Auto-removed.");

            if (_settings.ShowGameExitNotification)
            {
                App.ShowBalloonTip(
                    Strings.GameExitNotificationTitle,
                    string.Format(Strings.GameExitNotificationMessage, vm.ProcessName, vm.Session.ClickCount, vm.Session.SkippedClicks),
                    ToolTipIcon.Warning);
            }

            GameSessions.Remove(vm);
        }
        if (toRemove.Count > 0)
        {
            _memoryManager.ForceCleanup();

            // Auto-stop when no games remain in queue
            if (GameSessions.Count == 0 && _sessionStartedAt != null)
            {
                _logService.Info(Strings.AutoStoppedQueueEmpty);
                App.ShowBalloonTip(
                    Strings.AppTitle,
                    Strings.AutoStoppedQueueEmpty,
                    System.Windows.Forms.ToolTipIcon.Info);
            }
        }
    }

    private void OnAddGame() => RequestAddGame?.Invoke();

    private void OnRemoveGame(object? param)
    {
        if (param is GameSessionViewModel vm)
        {
            if (vm.IsRunning || vm.IsPaused) vm.Stop();
            _logService.Info($"Removed game \"{vm.ProcessName}\" from queue. Total clicks: {vm.Session.ClickCount}");
            GameSessions.Remove(vm);
            _memoryManager.ForceCleanup();
        }
    }

    private void OnRemoveAll()
    {
        if (HasAnyRunning) return;
        foreach (var g in GameSessions.ToList())
            _logService.Info($"Removed \"{g.ProcessName}\". Total clicks: {g.Session.ClickCount}");
        GameSessions.Clear();
        _memoryManager.ForceCleanup();
        _logService.Info("All games removed from queue");
    }

    private void OnStartAll()
    {
        if (_sessionStartedAt == null)
            _sessionStartedAt = DateTime.Now;

        var startable = GameSessions.Where(g => g.IsIdle && g.HasCoordinate).ToList();
        foreach (var g in startable)
            g.Start();

        if (startable.Count > 0 && _settings.MinimizeOnStartAll
            && Application.Current?.MainWindow is { } mainWindow)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }
    }

    private void OnStopAll()
    {
        foreach (var g in GameSessions.Where(g => g.IsRunning || g.IsPaused).ToList())
            g.Stop();
    }

    private long _lastTotalClicks;
    private DateTime _lastStatsTime;
    private TimeSpan _frozenElapsed;
    private bool _statsFrozen;

    private void RefreshSessionStats()
    {
        long total = GameSessions.Sum(g => g.Session.ClickCount);
        TotalClicks = total;
        TotalSkipped = GameSessions.Sum(g => g.Session.SkippedClicks);

        if (_sessionStartedAt == null && ActiveGameCount > 0)
        {
            _sessionStartedAt = DateTime.Now;
            _lastStatsTime = DateTime.Now;
            _lastTotalClicks = 0;
            _statsFrozen = false;
        }

        if (_sessionStartedAt != null)
        {
            if (ActiveGameCount == 0 && !_statsFrozen)
            {
                _frozenElapsed = DateTime.Now - _sessionStartedAt.Value;
                _statsFrozen = true;
            }
            else if (ActiveGameCount > 0 && _statsFrozen)
            {
                _sessionStartedAt = DateTime.Now - _frozenElapsed;
                _lastStatsTime = DateTime.Now;
                _statsFrozen = false;
            }

            if (!_statsFrozen)
            {
                var elapsed = DateTime.Now - _sessionStartedAt.Value;
                SessionUptime = elapsed.ToString(@"hh\:mm\:ss");

                if (elapsed.TotalSeconds >= 5)
                {
                    ClicksPerMinute = Math.Round(total / elapsed.TotalMinutes, 1);
                }

                var intervalSeconds = (DateTime.Now - _lastStatsTime).TotalSeconds;
                if (intervalSeconds >= 1.5 && elapsed.TotalSeconds >= 5)
                {
                    var realtimeCpm = (total - _lastTotalClicks) / intervalSeconds * 60.0;
                    var realtimeLong = (long)Math.Round(realtimeCpm);
                    if (realtimeLong > PeakClicksPerMinute)
                        PeakClicksPerMinute = realtimeLong;

                    _lastTotalClicks = total;
                    _lastStatsTime = DateTime.Now;
                }
            }
        }

        OnPropertyChanged(nameof(HasStats));
    }

    private void OnResetAll()
    {
        if (MessageBox.Show(
            Strings.ConfirmReset, Strings.AppTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        OnStopAll();
        GameSessions.Clear();
        _memoryManager.ForceCleanup();
        _logService.Info("Application reset");
    }

    private void OnResetApp()
    {
        if (MessageBox.Show(
            Strings.ConfirmResetApp, Strings.AppTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        OnStopAll();
        GameSessions.Clear();
        LogEntries.Clear();

        // Reset settings to defaults
        var defaults = new AppSettings();
        _settings.DefaultClickMode = defaults.DefaultClickMode;
        _settings.DefaultFixedInterval = defaults.DefaultFixedInterval;
        _settings.RandomMin = defaults.RandomMin;
        _settings.RandomMax = defaults.RandomMax;
        _settings.MaxGamesInQueue = defaults.MaxGamesInQueue;
        _settings.ShowRealTimeLogs = defaults.ShowRealTimeLogs;
        _settings.DarkMode = defaults.DarkMode;
        _settings.Language = defaults.Language;
        _settings.ExitBehavior = defaults.ExitBehavior;
        _settings.AutoUpdate = defaults.AutoUpdate;
        _settings.SoundNotifications = defaults.SoundNotifications;
        _settings.ShowGameExitNotification = defaults.ShowGameExitNotification;
        _settings.MinimizeOnStartAll = defaults.MinimizeOnStartAll;
        _settings.SettingsMode = defaults.SettingsMode;
        _settings.EnablePixelColorGuard = defaults.EnablePixelColorGuard;
        _settings.ColorTolerance = defaults.ColorTolerance;
        _settings.ColorMismatchBehavior = defaults.ColorMismatchBehavior;
        _settings.Hotkeys = new HotkeySettings();
        _settingsService.Save(_settings);

        _showLogs = _settings.ShowRealTimeLogs;
        OnPropertyChanged(nameof(ShowLogs));
        OnPropertyChanged(nameof(Settings));

        // Notify settings VM to refresh all its bindings
        SettingsReloaded?.Invoke();

        // Reset session stats
        ResetSessionStats();

        _memoryManager.ForceCleanup();
        _logService.Info("Application reset to factory defaults");
    }

    // ── Profile Management ──

    public void RefreshProfiles()
    {
        SavedProfiles.Clear();
        foreach (var p in _profileService.GetAll())
            SavedProfiles.Add(p);
    }

    public void SaveProfileFromSession(GameSessionViewModel sessionVm, string profileName)
    {
        var existing = _profileService.GetByName(profileName);
        var profile = existing ?? new GameProfile();
        profile.Name = profileName;
        profile.ClickPoints = sessionVm.Session.ClickPoints
            .Select(p => new ClickPoint(p.X, p.Y, p.ClickType, p.DelayAfterMs) { ReferenceColor = p.ReferenceColor })
            .ToList();
        profile.ClickSettings = new ClickProfile
        {
            Mode = sessionVm.Session.Profile.Mode,
            FixedIntervalSeconds = sessionVm.Session.Profile.FixedIntervalSeconds,
            RandomMinSeconds = sessionVm.Session.Profile.RandomMinSeconds,
            RandomMaxSeconds = sessionVm.Session.Profile.RandomMaxSeconds
        };
        profile.SequenceDelayMs = sessionVm.SequenceDelayMs;
        _profileService.Save(profile);
        RefreshProfiles();
        _logService.Info(string.Format(Strings.ProfileSaved, profileName));
    }

    public void LoadProfileIntoSession(GameSessionViewModel sessionVm, GameProfile profile)
    {
        sessionVm.ApplyProfile(profile);
        OnPropertyChanged(nameof(CanStartAll));
        _logService.Info(string.Format(Strings.ProfileLoaded, profile.Name, sessionVm.ProcessName));
    }

    public void DeleteProfile(string profileId)
    {
        var profile = SavedProfiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return;
        _profileService.Delete(profileId);
        RefreshProfiles();
        _logService.Info(string.Format(Strings.ProfileDeleted, profile.Name));
    }

    public void ExportProfile(GameProfile profile, string filePath)
    {
        _profileService.Export(filePath, profile);
        _logService.Info(string.Format(Strings.ProfileExported, filePath));
    }

    public string? CheckImportDuplicateName(string filePath)
    {
        try
        {
            var json = System.IO.File.ReadAllText(filePath);
            var preview = System.Text.Json.JsonSerializer.Deserialize<GameProfile>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            if (preview?.Name != null)
            {
                var existing = SavedProfiles.FirstOrDefault(p =>
                    string.Equals(p.Name, preview.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    return preview.Name;
            }
        }
        catch { }
        return null;
    }

    public GameProfile? ImportProfile(string filePath)
    {
        try
        {
            var profile = _profileService.Import(filePath);
            RefreshProfiles();
            _logService.Info(string.Format(Strings.ProfileImported, profile.Name));
            return profile;
        }
        catch (Exception ex)
        {
            _logService.Error(Strings.ProfileImportError, ex);
            return null;
        }
    }

    // ── Scheduler Logic ──

    private void OnToggleScheduler()
    {
        if (_isSchedulerEnabled)
        {
            // Cancel
            IsSchedulerEnabled = false;
            _schedulerStarted = false;
            _scheduledStartTime = null;
            _scheduledStopTime = null;
            SchedulerCountdown = "";
            ScheduleButtonText = Strings.Schedule;
            OnPropertyChanged(nameof(HasScheduler));
            _logService.Info("Scheduler cancelled");
            return;
        }

        // Validate queue has games with coordinates
        if (!GameSessions.Any(g => g.HasCoordinate))
        {
            _logService.Warn(Strings.SchedulerNoGames);
            return;
        }

        // Validate start time (required)
        if (string.IsNullOrWhiteSpace(_scheduledStartTimeText))
        {
            _logService.Warn(Strings.SchedulerStartRequired);
            return;
        }

        var startError = ValidateTimeInput(_scheduledStartTimeText);
        if (startError != null)
        {
            _logService.Warn($"{Strings.StartTime} {startError}");
            return;
        }
        _scheduledStartTime = ParseValidTime(_scheduledStartTimeText);

        // Validate stop time (optional)
        if (!string.IsNullOrWhiteSpace(_scheduledStopTimeText))
        {
            var stopError = ValidateTimeInput(_scheduledStopTimeText);
            if (stopError != null)
            {
                _logService.Warn($"{Strings.StopTime} {stopError}");
                return;
            }
            _scheduledStopTime = ParseValidTime(_scheduledStopTimeText);

            // Ensure stop is after start (same-day comparison)
            if (_scheduledStopTime <= _scheduledStartTime)
            {
                _logService.Warn(Strings.SchedulerStopBeforeStart);
                return;
            }
        }
        else
        {
            _scheduledStopTime = null;
        }

        _schedulerStarted = false;
        IsSchedulerEnabled = true;
        ScheduleButtonText = Strings.CancelSchedule;
        OnPropertyChanged(nameof(HasScheduler));
        _logService.Info($"Scheduler armed: start at {_scheduledStartTime:HH:mm}, stop at {(_scheduledStopTime?.ToString("HH:mm") ?? "—")}");
    }

    private void CheckScheduler()
    {
        if (!_isSchedulerEnabled) return;

        var now = DateTime.Now;

        // Trigger start
        if (!_schedulerStarted && _scheduledStartTime != null && now >= _scheduledStartTime)
        {
            _schedulerStarted = true;
            _logService.Info(Strings.SchedulerStarted);
            OnStartAll();
        }

        // Trigger stop
        if (_schedulerStarted && _scheduledStopTime != null && now >= _scheduledStopTime)
        {
            _logService.Info(Strings.SchedulerStopped);
            OnStopAll();
            // One-shot: disable after stop
            IsSchedulerEnabled = false;
            _schedulerStarted = false;
            SchedulerCountdown = "";
            ScheduleButtonText = Strings.Schedule;
            OnPropertyChanged(nameof(HasScheduler));
            return;
        }

        // Update countdown display
        if (!_schedulerStarted && _scheduledStartTime != null)
        {
            var remaining = _scheduledStartTime.Value - now;
            SchedulerCountdown = remaining.TotalSeconds > 0
                ? string.Format(Strings.SchedulerStartsIn, remaining.ToString(@"hh\:mm\:ss"))
                : "";
        }
        else if (_schedulerStarted && _scheduledStopTime != null)
        {
            var remaining = _scheduledStopTime.Value - now;
            SchedulerCountdown = remaining.TotalSeconds > 0
                ? string.Format(Strings.SchedulerStopsIn, remaining.ToString(@"hh\:mm\:ss"))
                : "";
        }
    }

    /// <summary>
    /// Validates a time input string. Returns error message or null if valid.
    /// </summary>
    private string? ValidateTimeInput(string text)
    {
        var trimmed = text.Trim();

        // Check for non-numeric/colon characters
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{1,2}:\d{2}$"))
            return Strings.SchedulerInvalidFormat;

        var parts = trimmed.Split(':');
        if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
            return Strings.SchedulerInvalidFormat;

        // Validate hour range 0-23
        if (hours < 0 || hours > 23)
            return string.Format(Strings.SchedulerInvalidHour, trimmed);

        // Validate minute range 0-59
        if (minutes < 0 || minutes > 59)
            return string.Format(Strings.SchedulerInvalidMinute, trimmed);

        return null;
    }

    /// <summary>
    /// Parses a validated "HH:mm" string to DateTime (today or tomorrow if past).
    /// Call only after ValidateTimeInput returns null.
    /// </summary>
    private static DateTime ParseValidTime(string text)
    {
        var parts = text.Trim().Split(':');
        var time = new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0);
        var today = DateTime.Today.Add(time);
        return today > DateTime.Now ? today : today.AddDays(1);
    }

    /// <summary>
    /// Resets session statistics (called from dashboard reset too).
    /// </summary>
    public void ResetSessionStats()
    {
        _sessionStartedAt = null;
        TotalClicks = 0;
        TotalSkipped = 0;
        SessionUptime = "00:00:00";
        ClicksPerMinute = 0;
        PeakClicksPerMinute = 0;
        _lastTotalClicks = 0;
        _statsFrozen = false;
        OnPropertyChanged(nameof(HasStats));

        // Reset per-game stats
        foreach (var g in GameSessions)
            g.ResetStats();
    }

    public void SaveSettings()
    {
        _settingsService.Save(_settings);
        ReloadSettingsToSessions();
    }

    // ── Full Session Export / Import ──

    public SessionExport BuildSessionExport()
    {
        var export = new SessionExport
        {
            SchemaVersion = 1,
            ExportedAt = DateTime.UtcNow,
            AppVersion = typeof(MainViewModel).Assembly.GetName().Version?.ToString() ?? "",
            Settings = CloneSettings(_settings),
            Profiles = _profileService.GetAll(),
            Games = GameSessions.Select(vm => new SavedGameSession
            {
                ProcessName = vm.Session.ProcessName,
                WindowTitle = vm.Session.WindowTitle,
                ExecutablePath = vm.Session.ExecutablePath,
                ClickPoints = vm.Session.ClickPoints
                    .Select(p => new ClickPoint(p.X, p.Y, p.ClickType, p.DelayAfterMs) { ReferenceColor = p.ReferenceColor })
                    .ToList(),
                Profile = new ClickProfile
                {
                    Mode = vm.Session.Profile.Mode,
                    FixedIntervalSeconds = vm.Session.Profile.FixedIntervalSeconds,
                    RandomMinSeconds = vm.Session.Profile.RandomMinSeconds,
                    RandomMaxSeconds = vm.Session.Profile.RandomMaxSeconds
                },
                SequenceDelayMs = vm.SequenceDelayMs,
                EnablePixelColorGuard = vm.Session.EnablePixelColorGuard,
                ColorTolerance = vm.Session.ColorTolerance,
                ColorMismatchBehavior = vm.Session.ColorMismatchBehavior,
                IsCustomMode = vm.IsCustomMode
            }).ToList()
        };
        return export;
    }

    public void ExportSessionToFile(string filePath)
    {
        try
        {
            var payload = BuildSessionExport();
            _sessionExportService.Export(filePath, payload);
            _logService.Info($"Session exported to {filePath} ({payload.Games.Count} games, {payload.Profiles.Count} profiles)");
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to export session", ex);
        }
    }

    /// <summary>
    /// Imports a full session: replaces settings, restores profiles, and re-attaches saved games
    /// to currently running windows by ProcessName + WindowTitle. Refuses while any session is running.
    /// </summary>
    public void ImportSessionFromFile(string filePath)
    {
        if (HasAnyRunning)
        {
            _logService.Warn("Cannot import session while clicks are running. Stop all first.");
            return;
        }

        SessionExport import;
        try
        {
            import = _sessionExportService.Import(filePath);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to read session file", ex);
            return;
        }

        if (import.SchemaVersion != 1)
        {
            _logService.Warn($"Unsupported session schema version {import.SchemaVersion} (expected 1).");
            return;
        }

        // 1. Apply imported settings (mutate in place to keep _settings reference stable)
        ApplySettings(import.Settings);
        _settingsService.Save(_settings);

        // 2. Restore profiles (Save preserves IDs if name matches; otherwise creates new)
        foreach (var profile in import.Profiles)
        {
            var existing = _profileService.GetByName(profile.Name);
            if (existing != null)
            {
                profile.Id = existing.Id;
                profile.CreatedAt = existing.CreatedAt;
            }
            _profileService.Save(profile);
        }
        RefreshProfiles();

        // 3. Clear current queue and re-attach saved games to live windows
        GameSessions.Clear();
        var liveWindows = _gameDetector.GetRunningWindows();
        var attached = 0;
        var skipped = 0;
        foreach (var saved in import.Games)
        {
            var match = liveWindows.FirstOrDefault(w =>
                string.Equals(w.ProcessName, saved.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(w.Title, saved.WindowTitle, StringComparison.Ordinal));
            if (match == null)
            {
                _logService.Warn($"Imported game \"{saved.ProcessName}\" / \"{saved.WindowTitle}\" not found among running windows. Skipped.");
                skipped++;
                continue;
            }

            var session = new GameSession
            {
                ProcessName = match.ProcessName,
                WindowTitle = match.Title,
                ExecutablePath = match.ExecutablePath,
                ProcessId = match.ProcessId,
                WindowHandle = match.Handle,
                ClickPoints = new System.Collections.ObjectModel.ObservableCollection<ClickPoint>(
                    saved.ClickPoints.Select(p => new ClickPoint(p.X, p.Y, p.ClickType, p.DelayAfterMs) { ReferenceColor = p.ReferenceColor })),
                Profile = new ClickProfile
                {
                    Mode = saved.Profile.Mode,
                    FixedIntervalSeconds = saved.Profile.FixedIntervalSeconds,
                    RandomMinSeconds = saved.Profile.RandomMinSeconds,
                    RandomMaxSeconds = saved.Profile.RandomMaxSeconds
                },
                EnablePixelColorGuard = saved.EnablePixelColorGuard,
                ColorTolerance = saved.ColorTolerance,
                ColorMismatchBehavior = saved.ColorMismatchBehavior
            };

            var vm = new GameSessionViewModel(session, _clickEngine, _logService, _soundService);
            vm.IsCustomMode = saved.IsCustomMode;
            vm.SequenceDelayMs = saved.SequenceDelayMs;
            GameSessions.Add(vm);
            attached++;
        }

        SettingsReloaded?.Invoke();
        _logService.Info($"Session imported: {attached} attached, {skipped} skipped (no matching window).");
    }

    private static AppSettings CloneSettings(AppSettings src)
    {
        return new AppSettings
        {
            SettingsMode = src.SettingsMode,
            DefaultClickMode = src.DefaultClickMode,
            DefaultFixedInterval = src.DefaultFixedInterval,
            RandomMin = src.RandomMin,
            RandomMax = src.RandomMax,
            MaxGamesInQueue = src.MaxGamesInQueue,
            ShowRealTimeLogs = src.ShowRealTimeLogs,
            DarkMode = src.DarkMode,
            Language = src.Language,
            ExitBehavior = src.ExitBehavior,
            AutoUpdate = src.AutoUpdate,
            SoundNotifications = src.SoundNotifications,
            ShowGameExitNotification = src.ShowGameExitNotification,
            MinimizeOnStartAll = src.MinimizeOnStartAll,
            EnablePixelColorGuard = src.EnablePixelColorGuard,
            ColorTolerance = src.ColorTolerance,
            ColorMismatchBehavior = src.ColorMismatchBehavior,
            Hotkeys = new HotkeySettings
            {
                PauseResume = src.Hotkeys.PauseResume,
                StopAll = src.Hotkeys.StopAll,
                StartAll = src.Hotkeys.StartAll
            }
        };
    }

    private void ApplySettings(AppSettings src)
    {
        _settings.SettingsMode = src.SettingsMode;
        _settings.DefaultClickMode = src.DefaultClickMode;
        _settings.DefaultFixedInterval = src.DefaultFixedInterval;
        _settings.RandomMin = src.RandomMin;
        _settings.RandomMax = src.RandomMax;
        _settings.MaxGamesInQueue = src.MaxGamesInQueue;
        _settings.ShowRealTimeLogs = src.ShowRealTimeLogs;
        _settings.DarkMode = src.DarkMode;
        _settings.Language = src.Language;
        _settings.ExitBehavior = src.ExitBehavior;
        _settings.AutoUpdate = src.AutoUpdate;
        _settings.SoundNotifications = src.SoundNotifications;
        _settings.ShowGameExitNotification = src.ShowGameExitNotification;
        _settings.MinimizeOnStartAll = src.MinimizeOnStartAll;
        _settings.EnablePixelColorGuard = src.EnablePixelColorGuard;
        _settings.ColorTolerance = src.ColorTolerance;
        _settings.ColorMismatchBehavior = src.ColorMismatchBehavior;
        _settings.Hotkeys = new HotkeySettings
        {
            PauseResume = src.Hotkeys.PauseResume,
            StopAll = src.Hotkeys.StopAll,
            StartAll = src.Hotkeys.StartAll
        };
    }
}

public class RelayCommand<T> : System.Windows.Input.ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T)parameter!) ?? true;
    public void Execute(object? parameter) => _execute((T)parameter!);
}
