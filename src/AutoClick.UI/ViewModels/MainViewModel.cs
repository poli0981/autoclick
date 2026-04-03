using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
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

    public ObservableCollection<GameSessionViewModel> GameSessions { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

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
    public RelayCommand FreeRamCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand ResetAppCommand { get; }

    public event Action? RequestAddGame;

    public event Action? SettingsReloaded;

    public MainViewModel(
        IClickEngine clickEngine,
        IGameDetector gameDetector,
        ISettingsService settingsService,
        ILogService logService,
        IMemoryManager memoryManager)
    {
        _clickEngine = clickEngine;
        _gameDetector = gameDetector;
        _settingsService = settingsService;
        _logService = logService;
        _memoryManager = memoryManager;

        _settings = _settingsService.Load();
        _showLogs = _settings.ShowRealTimeLogs;

        AddGameCommand = new RelayCommand(OnAddGame, () => GameSessions.Count < _settings.MaxGamesInQueue);
        RemoveGameCommand = new RelayCommand<object?>(OnRemoveGame);
        StartAllCommand = new RelayCommand(OnStartAll, () => CanStartAll);
        StopAllCommand = new RelayCommand(OnStopAll, () => HasAnyRunning);
        RemoveAllCommand = new RelayCommand(OnRemoveAll, () => HasNoRunning && GameSessions.Count > 0);
        FreeRamCommand = new RelayCommand(OnFreeRam);
        ResetAllCommand = new RelayCommand(OnResetAll);
        ResetAppCommand = new RelayCommand(OnResetApp);

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
            CheckForExitedProcesses();
        };
        timer.Start();
    }

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
    /// Assigns a picked coordinate to a game session. Validates bounds + duplicates + logs.
    /// </summary>
    public bool TrySetCoordinate(GameSessionViewModel sessionVm, ClickPoint point)
    {
        // Validate bounds
        if (!CoordinateHelper.IsCoordinateInBounds(sessionVm.Session.WindowHandle, point.X, point.Y))
        {
            _logService.Warn($"Coordinate ({point.X}, {point.Y}) is outside the window of \"{sessionVm.ProcessName}\". Rejected.");
            return false;
        }

        // Check duplicate
        var conflict = CheckDuplicateCoordinate(sessionVm.Session.WindowHandle, point, sessionVm.Id);
        if (conflict != null)
        {
            _logService.Warn($"Coordinate ({point.X}, {point.Y}) already used by \"{conflict}\". Rejected for \"{sessionVm.ProcessName}\".");
            return false;
        }

        sessionVm.Session.ClickPoints.Clear();
        sessionVm.Session.ClickPoints.Add(point);
        sessionVm.UpdateCoordinateText();
        _logService.Info($"Coordinate ({point.X}, {point.Y}) set for \"{sessionVm.ProcessName}\" (picked)");
        OnPropertyChanged(nameof(CanStartAll));
        return true;
    }

    /// <summary>
    /// Generates a random coordinate within the game window, checks for duplicates, and assigns it.
    /// Retries up to 10 times to avoid duplicates.
    /// </summary>
    public bool TrySetRandomCoordinate(GameSessionViewModel sessionVm)
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

            var conflict = CheckDuplicateCoordinate(hWnd, point, sessionVm.Id);
            if (conflict == null)
            {
                sessionVm.Session.ClickPoints.Clear();
                sessionVm.Session.ClickPoints.Add(point);
                sessionVm.UpdateCoordinateText();
                _logService.Info($"Random coordinate ({point.X}, {point.Y}) set for \"{sessionVm.ProcessName}\" (window: {w}x{h})");
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
            }
        };

        var vm = new GameSessionViewModel(session, _clickEngine, _logService);
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
        foreach (var g in GameSessions.Where(g => g.IsIdle))
        {
            g.Session.Profile.Mode = _settings.DefaultClickMode;
            g.Session.Profile.FixedIntervalSeconds = _settings.DefaultFixedInterval;
            g.Session.Profile.RandomMinSeconds = _settings.RandomMin;
            g.Session.Profile.RandomMaxSeconds = _settings.RandomMax;
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
            _logService.Info($"Game \"{vm.ProcessName}\" (PID: {vm.Session.ProcessId}) has exited. Total clicks: {vm.Session.ClickCount}. Auto-removed.");
            GameSessions.Remove(vm);
        }
        if (toRemove.Count > 0)
            _memoryManager.ForceCleanup();
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
        foreach (var g in GameSessions.Where(g => g.IsIdle && g.HasCoordinate).ToList())
            g.Start();
    }

    private void OnStopAll()
    {
        foreach (var g in GameSessions.Where(g => g.IsRunning || g.IsPaused).ToList())
            g.Stop();
    }

    private void OnFreeRam()
    {
        _memoryManager.ForceCleanup();
        _logService.Info("Manual RAM cleanup performed");
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
        _settings.Hotkeys = new HotkeySettings();
        _settingsService.Save(_settings);

        _showLogs = _settings.ShowRealTimeLogs;
        OnPropertyChanged(nameof(ShowLogs));
        OnPropertyChanged(nameof(Settings));

        // Notify settings VM to refresh all its bindings
        SettingsReloaded?.Invoke();

        _memoryManager.ForceCleanup();
        _logService.Info("Application reset to factory defaults");
    }

    public void SaveSettings()
    {
        _settingsService.Save(_settings);
        ReloadSettingsToSessions();
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
