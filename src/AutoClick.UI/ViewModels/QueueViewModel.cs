using System.Collections.ObjectModel;
using System.Text.Json;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Scheduling;
using AutoClick.Core.Sessions;
using AutoClick.Core.Settings;
using AutoClick.Services;
using AutoClick.Services.Engine;
using AutoClick.Services.Logging;
using AutoClick.Services.Persistence;
using AutoClick.Services.Scheduling;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.UI.Startup;
using AutoClick.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.ViewModels;

/// <summary>Final statistics of a game that left the queue (kept for the dashboard).</summary>
public sealed record GameSummary(string ProcessName, long Clicks, long Skipped);

/// <summary>The game queue: games, start/stop for all, the scheduler and the live log.</summary>
public sealed partial class QueueViewModel : ObservableObject
{
    private const int MaxLogLines = 500;

    private readonly GameSessionDependencies _deps;
    private readonly SettingsStore _settings;
    private readonly ProfileStore _profiles;
    private readonly IWindowInspector _inspector;
    private readonly DialogService _dialogs;
    private readonly NotificationService _notifications;
    private readonly ClickScheduler _scheduler;
    private readonly LogBuffer _logBuffer;
    private readonly AppPaths _paths;
    private readonly ShellLauncher _shell;
    private readonly TimeProvider _time;
    private readonly ILogger<QueueViewModel> _logger;
    private readonly List<GameSummary> _finished = [];

    public QueueViewModel(
        ClickEngine engine,
        SettingsStore settings,
        ProfileStore profiles,
        IWindowInspector inspector,
        OverlayService overlays,
        DialogService dialogs,
        SoundService sound,
        NotificationService notifications,
        ClickScheduler scheduler,
        LogBuffer logBuffer,
        AppPaths paths,
        ShellLauncher shell,
        UiTicker ticker,
        TimeProvider time,
        ILogger<QueueViewModel> logger,
        ILogger<GameSessionViewModel> gameLogger)
    {
        _settings = settings;
        _profiles = profiles;
        _inspector = inspector;
        _dialogs = dialogs;
        _notifications = notifications;
        _scheduler = scheduler;
        _logBuffer = logBuffer;
        _paths = paths;
        _shell = shell;
        _time = time;
        _logger = logger;
        _deps = new GameSessionDependencies(engine, settings, profiles, inspector, overlays, dialogs, sound, gameLogger);

        ShowLogs = settings.Current.ShowRealTimeLogs;
        ReloadProfiles();
        Games.CollectionChanged += (_, _) => OnGamesChanged();
        settings.Changed += (_, current) => OnSettingsChanged(current);
        scheduler.StartRequested += (_, _) => OnScheduledStart();
        scheduler.StopRequested += (_, _) => _ = StopAllAsync(StopReason.Scheduler);
        scheduler.Changed += (_, _) => UpdateScheduleStatus();
        ticker.Tick += (_, _) => OnTick();
        LocalizationService.Instance.LanguageChanged += (_, _) => RefreshTexts();
    }

    public ObservableCollection<GameSessionViewModel> Games { get; } = [];

    /// <summary>Saved profiles, shared by every game card.</summary>
    public ObservableCollection<GameProfile> Profiles { get; } = [];

    public ObservableCollection<LogLine> LogLines { get; } = [];

    /// <summary>Games that exited or were removed, for the dashboard.</summary>
    public IReadOnlyList<GameSummary> FinishedGames => _finished;

    /// <summary>A game was blocked because it runs as administrator (UI thread).</summary>
    public event EventHandler<GameSessionViewModel>? ElevationRequired;

    /// <summary>Start all asked to hide the window (setting "Minimize to tray on Start all").</summary>
    public event EventHandler? MinimizeRequested;

    /// <summary>Raised after the per-second refresh (the dashboard samples then).</summary>
    public event EventHandler? Refreshed;

    /// <summary>Raised when statistics were reset.</summary>
    public event EventHandler? StatsReset;

    [ObservableProperty]
    public partial bool ShowLogs { get; set; }

    [ObservableProperty]
    public partial string ScheduleStart { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScheduleStop { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScheduleStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScheduleMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial long TotalClicks { get; set; }

    [ObservableProperty]
    public partial long TotalSkipped { get; set; }

    [ObservableProperty]
    public partial int RunningCount { get; set; }

    [ObservableProperty]
    public partial int PausedCount { get; set; }

    /// <summary>Time with at least one session running, since the last statistics reset.</summary>
    [ObservableProperty]
    public partial TimeSpan ActiveTime { get; set; }

    public bool IsScheduleArmed => _scheduler.IsArmed;

    public bool HasGames => Games.Count > 0;

    public bool AnyActive => Games.Any(g => g.IsActive);

    public string GamesCountText => L.Format("Queue_GamesCount", Games.Count, _settings.Current.MaxGamesInQueue);

    public string StatusText => RunningCount + PausedCount == 0
        ? L.Get("Status_Idle")
        : L.Format("Status_Running", RunningCount, PausedCount);

    private bool CanAddGame => Games.Count < _settings.Current.MaxGamesInQueue;

    [RelayCommand]
    public async Task AddGameAsync()
    {
        if (!CanAddGame)
        {
            _dialogs.Toast(L.Get("Queue_AddGame"), L.Format("Queue_Full", _settings.Current.MaxGamesInQueue), ToastKind.Warning);
            return;
        }

        var window = await _dialogs.PickGameAsync(() => _inspector.GetCandidateWindows()
            .Where(w => Games.All(g => g.Window.Handle != w.Handle))
            .ToList());
        if (window is not null)
        {
            AddWindow(window);
        }
    }

    [RelayCommand]
    public void RemoveGame(GameSessionViewModel? game)
    {
        if (game is not null)
        {
            Remove(game);
        }
    }

    [RelayCommand]
    public async Task RemoveAllAsync()
    {
        if (Games.Count == 0
            || !await _dialogs.ConfirmAsync(L.Get("Queue_RemoveAll"), L.Format("Queue_RemoveAllConfirm", Games.Count), L.Get("Queue_RemoveAll")))
        {
            return;
        }

        foreach (var game in Games.ToList())
        {
            Remove(game);
        }
    }

    [RelayCommand]
    public void StartAll() => StartAllCore(fromScheduler: false);

    [RelayCommand]
    public Task StopAllAsync() => StopAllAsync(StopReason.User);

    /// <summary>
    /// Hotkey: pause every running game, or resume every paused one when none is running, so
    /// the games never end up half paused (v1 toggled each game separately).
    /// </summary>
    public void PauseResumeAll()
    {
        var running = Games.Where(g => g.IsRunning).ToList();
        if (running.Count > 0)
        {
            running.ForEach(g => g.Pause());
            _logger.LogInformation("Paused {Count} game(s)", running.Count);
            return;
        }

        var paused = Games.Where(g => g.IsPaused).ToList();
        paused.ForEach(g => g.Resume());
        if (paused.Count > 0)
        {
            _logger.LogInformation("Resumed {Count} game(s)", paused.Count);
        }
    }

    public async Task StopAllAsync(StopReason reason)
    {
        var active = Games.Where(g => g.IsActive).ToList();
        if (active.Count == 0)
        {
            return;
        }

        await Task.WhenAll(active.Select(g => g.StopWithReasonAsync(reason)));
        _logger.LogInformation("Stopped {Count} game(s) ({Reason})", active.Count, reason);
    }

    [RelayCommand]
    public void ArmSchedule()
    {
        if (!Games.Any(g => g.HasPoints))
        {
            ScheduleMessage = L.Get("Schedule_NoGames");
            return;
        }

        var error = _scheduler.Arm(ScheduleStart, ScheduleStop);
        ScheduleMessage = error switch
        {
            ScheduleError.None => string.Empty,
            ScheduleError.Empty => L.Get("Schedule_StartRequired"),
            ScheduleError.InvalidHour => L.Get("Schedule_InvalidHour"),
            ScheduleError.InvalidMinute => L.Get("Schedule_InvalidMinute"),
            ScheduleError.StopEqualsStart => L.Get("Schedule_StopEqualsStart"),
            _ => L.Get("Schedule_InvalidFormat"),
        };
    }

    [RelayCommand]
    public void CancelSchedule()
    {
        _scheduler.Cancel();
        ScheduleMessage = string.Empty;
    }

    [RelayCommand]
    public void ClearLog() => LogLines.Clear();

    [RelayCommand]
    public void ExportLog()
    {
        var path = DialogService.PickSaveFile(L.Get("Filter_Log"), $"autoclick-log-{_time.GetLocalNow():yyyyMMdd-HHmmss}.txt");
        if (path is null)
        {
            return;
        }

        try
        {
            File.WriteAllLines(path, _logBuffer.Snapshot().Select(line => line.ToString()));
            _dialogs.Toast(L.Get("Log_Export"), L.Format("Common_SavedTo", path), ToastKind.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.Toast(L.Get("Log_Export"), L.Format("Common_FileError", ex.Message), ToastKind.Error);
        }
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        Directory.CreateDirectory(_paths.LogsDirectory);
        _shell.TryOpen(_paths.LogsDirectory);
    }

    [RelayCommand]
    public void ExportSession()
    {
        var path = DialogService.PickSaveFile(L.Get("Filter_Session"), $"autoclick-{_time.GetLocalNow():yyyyMMdd-HHmm}.autoclick-session");
        if (path is null)
        {
            return;
        }

        try
        {
            SessionImport.Export(path, new SessionExport
            {
                ExportedAt = _time.GetLocalNow(),
                AppVersion = AppInfo.Version,
                Settings = _settings.Current,
                Profiles = _profiles.GetAll(),
                Games = Games.Select(g => g.ToSavedGame()).ToList(),
            });
            _dialogs.Toast(L.Get("Session_Export"), L.Format("Common_SavedTo", path), ToastKind.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Session export failed");
            _dialogs.Toast(L.Get("Session_Export"), L.Format("Common_FileError", ex.Message), ToastKind.Error);
        }
    }

    [RelayCommand]
    public async Task ImportSessionAsync()
    {
        var path = DialogService.PickOpenFile(L.Get("Filter_Session"));
        if (path is null)
        {
            return;
        }

        SessionImportPlan plan;
        try
        {
            var session = SessionImport.Read(path);
            var windows = _inspector.GetCandidateWindows().Where(w => Games.All(g => g.Window.Handle != w.Handle)).ToList();
            plan = SessionImport.Plan(session, _settings.Current, windows);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Session import failed");
            await _dialogs.AlertAsync(L.Get("Session_Import"), L.Format("Session_ImportFailed", ex.Message));
            return;
        }

        var summary = L.Format("Session_ImportSummary", plan.Source.Profiles.Count, plan.Matched.Count, plan.Unmatched.Count);
        if (plan.Unmatched.Count > 0)
        {
            summary += Environment.NewLine + Environment.NewLine + L.Format("Session_NotRunning", string.Join(", ", plan.Unmatched.Select(g => g.ProcessName)));
        }

        if (!await _dialogs.ConfirmAsync(L.Get("Session_Import"), summary, L.Get("Session_Import")))
        {
            return;
        }

        _settings.Update(_ => plan.Settings);
        foreach (var profile in plan.Source.Profiles)
        {
            _profiles.Import(profile);
        }

        ReloadProfiles();
        foreach (var match in plan.Matched)
        {
            if (!CanAddGame)
            {
                break;
            }

            AddWindow(match.Window).ApplySavedGame(match.Game);
        }

        _logger.LogInformation("Session imported from {Path}: {Matched} game(s) restored, {Unmatched} not running", path, plan.Matched.Count, plan.Unmatched.Count);
        _dialogs.Toast(L.Get("Session_Import"), L.Format("Session_Imported", plan.Matched.Count), ToastKind.Success);
    }

    /// <summary>Resets every game's statistics and the dashboard history.</summary>
    public void ResetAllStats()
    {
        foreach (var game in Games)
        {
            game.ResetStats();
        }

        _finished.Clear();
        ActiveTime = TimeSpan.Zero;
        UpdateTotals();
        StatsReset?.Invoke(this, EventArgs.Empty);
    }

    internal GameSessionViewModel AddWindow(WindowInfo window)
    {
        var game = new GameSessionViewModel(window, _deps, Profiles);
        game.ElevationRequired += OnGameElevationRequired;
        game.ProfilesChanged += OnProfilesChanged;
        game.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(GameSessionViewModel.State) or nameof(GameSessionViewModel.HasPoints))
            {
                UpdateTotals();
            }
        };
        Games.Add(game);
        _logger.LogInformation("Added {Process} ({Title}) to the queue", window.ProcessName, window.Title);
        return game;
    }

    private void Remove(GameSessionViewModel game)
    {
        game.ElevationRequired -= OnGameElevationRequired;
        game.ProfilesChanged -= OnProfilesChanged;
        game.Dispose();
        Games.Remove(game);
        if (game.Clicks + game.Skipped > 0)
        {
            _finished.Add(new GameSummary(game.ProcessName, game.Clicks, game.Skipped));
        }
    }

    private void StartAllCore(bool fromScheduler)
    {
        var startable = Games.Where(g => g.CanStart && !g.IsElevated).ToList();
        startable.ForEach(g => g.Start());
        foreach (var game in Games.Where(g => g.IsElevated && g.IsIdle && g.HasPoints))
        {
            game.Start(); // reports the elevation problem
        }

        if (startable.Count == 0)
        {
            if (!fromScheduler)
            {
                _dialogs.Toast(L.Get("Queue_StartAll"), L.Get("Queue_NothingToStart"), ToastKind.Warning);
            }

            return;
        }

        _logger.LogInformation("Started {Count} game(s){Source}", startable.Count, fromScheduler ? " (scheduled)" : string.Empty);
        if (_settings.Current.MinimizeOnStartAll)
        {
            MinimizeRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnScheduledStart()
    {
        StartAllCore(fromScheduler: true);
        _notifications.Show(L.Get("Schedule_Title"), L.Get("Schedule_Started"));
    }

    private void OnTick()
    {
        DrainLog();
        _scheduler.Tick();
        UpdateScheduleStatus();

        var anyWasActive = AnyActive;
        foreach (var game in Games.ToList())
        {
            if (_inspector.GetStatus(game.Window.Handle, game.Window.ProcessId) == WindowStatus.Gone)
            {
                OnGameExited(game);
            }
            else
            {
                game.Refresh();
            }
        }

        if (anyWasActive && Games.Count == 0)
        {
            _notifications.Show(L.Get("Notify_GameExitedTitle"), L.Get("Notify_QueueEmpty"));
        }

        if (RunningCount > 0)
        {
            ActiveTime += TimeSpan.FromSeconds(1);
        }

        UpdateTotals();
        Refreshed?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameExited(GameSessionViewModel game)
    {
        game.Refresh();
        _logger.LogInformation("{Process} exited (clicks {Clicks}, skipped {Skipped}); removed from the queue", game.ProcessName, game.Clicks, game.Skipped);
        if (_settings.Current.ShowGameExitNotification)
        {
            _notifications.Show(L.Get("Notify_GameExitedTitle"), L.Format("Notify_GameExited", game.ProcessName, game.Clicks, game.Skipped));
        }

        Remove(game);
    }

    private void DrainLog()
    {
        var lines = _logBuffer.TakePending();
        if (!ShowLogs || lines.Count == 0)
        {
            return;
        }

        foreach (var line in lines.Skip(Math.Max(0, lines.Count - MaxLogLines)))
        {
            LogLines.Add(line);
        }

        while (LogLines.Count > MaxLogLines)
        {
            LogLines.RemoveAt(0);
        }
    }

    private void UpdateTotals()
    {
        TotalClicks = Games.Sum(g => g.Clicks) + _finished.Sum(g => g.Clicks);
        TotalSkipped = Games.Sum(g => g.Skipped) + _finished.Sum(g => g.Skipped);
        RunningCount = Games.Count(g => g.IsRunning);
        PausedCount = Games.Count(g => g.IsPaused);
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(AnyActive));
    }

    private void UpdateScheduleStatus()
    {
        OnPropertyChanged(nameof(IsScheduleArmed));
        if (_scheduler.TimeUntilNextAction() is not { } remaining || _scheduler.Window is null)
        {
            ScheduleStatus = string.Empty;
            return;
        }

        var text = remaining.ToString(remaining.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", LocalizationService.Instance.Culture);
        ScheduleStatus = _scheduler.HasStarted ? L.Format("Schedule_StopsIn", text) : L.Format("Schedule_StartsIn", text);
    }

    private void OnGamesChanged()
    {
        OnPropertyChanged(nameof(HasGames));
        OnPropertyChanged(nameof(GamesCountText));
        UpdateTotals();
    }

    private void OnSettingsChanged(AppSettings current)
    {
        ShowLogs = current.ShowRealTimeLogs;
        var custom = current.SettingsMode == SettingsMode.Custom;
        foreach (var game in Games)
        {
            game.IsCustomMode = custom;
        }

        OnPropertyChanged(nameof(GamesCountText));
    }

    private void OnGameElevationRequired(object? sender, EventArgs e)
    {
        if (sender is GameSessionViewModel game)
        {
            ElevationRequired?.Invoke(this, game);
        }
    }

    private void OnProfilesChanged(object? sender, GameProfile? changed)
    {
        ReloadProfiles();
        if (sender is GameSessionViewModel game && changed is not null)
        {
            game.SelectedProfile = Profiles.FirstOrDefault(p => p.Id == changed.Id);
        }
    }

    private void ReloadProfiles()
    {
        var selected = Games.ToDictionary(g => g, g => g.SelectedProfile?.Id);
        Profiles.Clear();
        foreach (var profile in _profiles.GetAll())
        {
            Profiles.Add(profile);
        }

        foreach (var (game, id) in selected)
        {
            game.SelectedProfile = id is null ? null : Profiles.FirstOrDefault(p => p.Id == id);
        }
    }

    private void RefreshTexts()
    {
        foreach (var game in Games)
        {
            game.RefreshTexts();
        }

        OnPropertyChanged(nameof(GamesCountText));
        OnPropertyChanged(nameof(StatusText));
        UpdateScheduleStatus();
    }
}
