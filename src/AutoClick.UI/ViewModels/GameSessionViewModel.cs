using System.Collections.ObjectModel;
using System.Windows.Threading;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Hotkeys;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using AutoClick.Services.Engine;
using AutoClick.Services.Persistence;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using AutoClick.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.ViewModels;

/// <summary>Dependencies shared by every game card.</summary>
public sealed record GameSessionDependencies(
    ClickEngine Engine,
    SettingsStore Settings,
    ProfileStore Profiles,
    IWindowInspector Inspector,
    OverlayService Overlays,
    DialogService Dialogs,
    SoundService Sound,
    ILogger Logger);

/// <summary>One game in the queue: its click steps, timing, state and statistics.</summary>
public sealed partial class GameSessionViewModel : ObservableObject, IDisposable
{
    private readonly GameSessionDependencies _deps;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly Queue<(DateTime At, long Clicks)> _history = new();
    private ClickSession? _session;

    public GameSessionViewModel(WindowInfo window, GameSessionDependencies deps, ObservableCollection<GameProfile> profiles)
    {
        Window = window;
        _deps = deps;
        Profiles = profiles;
        var settings = deps.Settings.Current;
        Mode = settings.DefaultTiming.Mode;
        FixedInterval = settings.DefaultTiming.FixedIntervalSeconds;
        RandomMin = settings.DefaultTiming.RandomMinSeconds;
        RandomMax = settings.DefaultTiming.RandomMaxSeconds;
        SequenceDelayMs = settings.DefaultSequenceDelayMs;
        IsCustomMode = settings.SettingsMode == SettingsMode.Custom;
        Points.CollectionChanged += (_, _) => OnPointsChanged();
        deps.Overlays.HeatmapClosed += OnHeatmapClosed;
    }

    public static IReadOnlyList<Choice<ClickMode>> ModeOptions { get; } =
    [
        new(ClickMode.Random, "Timing_Random"),
        new(ClickMode.Fixed, "Timing_Fixed"),
    ];

    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    public WindowInfo Window { get; }

    public string ProcessName => Window.ProcessName;

    public string WindowTitle => Window.Title;

    public string ExecutablePath => Window.ExecutablePath;

    public bool IsElevated => Window.Elevation == ElevationState.Higher;

    public SessionStats Stats { get; } = new();

    public ObservableCollection<ClickPoint> Points { get; } = [];

    public ObservableCollection<GameProfile> Profiles { get; }

    /// <summary>Raised (on the UI thread) when the game blocks input because it runs as administrator.</summary>
    public event EventHandler? ElevationRequired;

    /// <summary>Raised after a profile was saved, imported (the profile) or deleted (null).</summary>
    public event EventHandler<GameProfile?>? ProfilesChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsRunning), nameof(IsPaused), nameof(IsActive), nameof(StateText))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand), nameof(ResumeCommand), nameof(StopCommand), nameof(AddPointCommand), nameof(AddRandomPointCommand), nameof(ClearPointsCommand), nameof(LoadProfileCommand), nameof(RemovePointCommand), nameof(ReorderPointCommand))]
    public partial SessionState State { get; set; } = SessionState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateText))]
    public partial bool IsWaitingForWindow { get; set; }

    /// <summary>Why the session stopped by itself (empty after a user stop).</summary>
    [ObservableProperty]
    public partial string StopMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedMode))]
    public partial ClickMode Mode { get; set; }

    [ObservableProperty]
    public partial double FixedInterval { get; set; }

    [ObservableProperty]
    public partial double RandomMin { get; set; }

    [ObservableProperty]
    public partial double RandomMax { get; set; }

    [ObservableProperty]
    public partial double SequenceDelayMs { get; set; }

    /// <summary>Per-game timing and profiles (Settings → click mode "Custom").</summary>
    [ObservableProperty]
    public partial bool IsCustomMode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadProfileCommand), nameof(ExportProfileCommand), nameof(DeleteProfileCommand))]
    public partial GameProfile? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial long Clicks { get; set; }

    [ObservableProperty]
    public partial long Skipped { get; set; }

    [ObservableProperty]
    public partial double ClicksPerMinute { get; set; }

    [ObservableProperty]
    public partial double LastIntervalSeconds { get; set; }

    public bool IsFixedMode => Mode == ClickMode.Fixed;

    public bool IsIdle => State is SessionState.Idle or SessionState.Stopped;

    public bool IsRunning => State == SessionState.Running;

    public bool IsPaused => State == SessionState.Paused;

    public bool IsActive => IsRunning || IsPaused;

    public bool HasPoints => Points.Count > 0;

    public bool IsHeatmapOpen => _deps.Overlays.IsHeatmapOpen(Id);

    public string StateText => State switch
    {
        SessionState.Running when IsWaitingForWindow => L.Get("State_WaitingMinimized"),
        SessionState.Running => L.Get("State_Running"),
        SessionState.Paused => L.Get("State_Paused"),
        SessionState.Stopped => L.Get("State_Stopped"),
        _ => L.Get("State_Idle"),
    };

    public bool CanStart => IsIdle && HasPoints;

    [RelayCommand(CanExecute = nameof(CanStart))]
    public void Start()
    {
        if (!CanStart)
        {
            return;
        }

        if (IsElevated)
        {
            // Windows would block every click (UIPI): ask for a restart as administrator instead.
            StopMessage = L.Get("Stop_ElevationRequired");
            ElevationRequired?.Invoke(this, EventArgs.Empty);
            return;
        }

        var settings = _deps.Settings.Current;
        var timing = IsCustomMode ? CurrentTiming() : settings.DefaultTiming;
        var delay = IsCustomMode ? (int)Math.Round(SequenceDelayMs) : settings.DefaultSequenceDelayMs;
        var info = new SessionStartInfo
        {
            SessionId = Id,
            Window = Window.Handle,
            ProcessId = Window.ProcessId,
            ProcessName = ProcessName,
            Points = Points.Select(p => p with { DelayAfterMs = delay }).ToList(),
            Timing = timing,
            PixelGuard = settings.PixelGuard,
        };

        StopMessage = string.Empty;
        _history.Clear();
        try
        {
            _session = _deps.Engine.Start(info, Stats);
        }
        catch (InvalidOperationException ex)
        {
            _deps.Logger.LogWarning(ex, "Could not start {Process}", ProcessName);
            return;
        }

        _session.StateChanged += OnSessionStateChanged;
        // The session is already running: read its state after subscribing.
        Apply(_session.State, _session.StopReason, _session.StopDetail, _session.IsWaitingForWindow);
        _deps.Sound.Start();
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    public void Pause()
    {
        _session?.Pause();
        _deps.Sound.Toggle();
    }

    [RelayCommand(CanExecute = nameof(IsPaused))]
    public void Resume()
    {
        _session?.Resume();
        _deps.Sound.Toggle();
    }

    [RelayCommand(CanExecute = nameof(IsActive))]
    public Task StopAsync() => StopWithReasonAsync(StopReason.User);

    public async Task StopWithReasonAsync(StopReason reason)
    {
        if (_session is { } session)
        {
            await session.StopAsync(reason);
            if (reason == StopReason.User)
            {
                _deps.Sound.Stop();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    public async Task AddPointAsync()
    {
        if (_deps.Inspector.GetStatus(Window.Handle, Window.ProcessId) != WindowStatus.Alive)
        {
            _deps.Dialogs.Toast(L.Get("Picker_Title"), L.Get("Picker_RestoreGame"), ToastKind.Warning);
            return;
        }

        WindowGeometry.BringToFront(Window.Handle);
        await Task.Delay(150);
        var point = await _deps.Overlays.PickPointAsync(Window.Handle, _deps.Settings.Current.PixelGuard.Enabled);
        if (point is null)
        {
            return;
        }

        if (!point.IsKeystroke && Points.Any(p => !p.IsKeystroke && p.X == point.X && p.Y == point.Y && p.ClickType == point.ClickType))
        {
            _deps.Sound.Error();
            _deps.Dialogs.Toast(L.Get("Picker_Title"), L.Format("Picker_Duplicate", point.X, point.Y), ToastKind.Warning);
            return;
        }

        Points.Add(point);
        _deps.Logger.LogInformation("Step {Index} added to {Process}: {Type} ({X}, {Y}) key {Key}", Points.Count, ProcessName, point.ClickType, point.X, point.Y, point.VirtualKeyCode);
        _deps.Sound.Success();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    public void AddRandomPoint()
    {
        if (!_deps.Inspector.TryGetClientSize(Window.Handle, out var width, out var height) || width < 10 || height < 10)
        {
            _deps.Sound.Error();
            _deps.Dialogs.Toast(L.Get("Game_RandomPoint"), L.Get("Game_RandomPointFailed"), ToastKind.Warning);
            return;
        }

        // Stay away from the edges, where window chrome-like game UI usually lives.
        int marginX = width / 10, marginY = height / 10;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var point = new ClickPoint { X = Random.Shared.Next(marginX, width - marginX), Y = Random.Shared.Next(marginY, height - marginY) };
            if (!Points.Any(p => !p.IsKeystroke && p.X == point.X && p.Y == point.Y))
            {
                Points.Add(point);
                _deps.Sound.Success();
                return;
            }
        }

        _deps.Dialogs.Toast(L.Get("Game_RandomPoint"), L.Get("Game_RandomPointFailed"), ToastKind.Warning);
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    public void RemovePoint(ClickPoint? point)
    {
        if (point is not null)
        {
            Points.Remove(point);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    public void ReorderPoint(ReorderRequest? request)
    {
        if (request is { From: >= 0, To: >= 0 } && request.From < Points.Count && request.To < Points.Count && request.From != request.To)
        {
            Points.Move(request.From, request.To);
        }
    }

    [RelayCommand]
    public void MovePointUp(ClickPoint? point) => MoveBy(point, -1);

    [RelayCommand]
    public void MovePointDown(ClickPoint? point) => MoveBy(point, +1);

    [RelayCommand(CanExecute = nameof(IsIdle))]
    public async Task ClearPointsAsync()
    {
        if (Points.Count > 0
            && await _deps.Dialogs.ConfirmAsync(L.Get("Game_ClearPoints"), L.Format("Game_ClearPointsConfirm", Points.Count, ProcessName), L.Get("Common_Clear")))
        {
            Points.Clear();
        }
    }

    [RelayCommand]
    public void ToggleHeatmap()
    {
        _deps.Overlays.ToggleHeatmap(Id, Window.Handle, Window.ProcessId, Stats);
        OnPropertyChanged(nameof(IsHeatmapOpen));
    }

    [RelayCommand]
    public void ResetStats()
    {
        Stats.Reset();
        _history.Clear();
        Refresh();
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        if (!HasPoints)
        {
            _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Get("Profile_NoSteps"), ToastKind.Warning);
            return;
        }

        var name = await _deps.Dialogs.PromptAsync(L.Get("Profile_Save"), L.Get("Profile_NamePrompt"), SelectedProfile?.Name ?? ProcessName);
        if (name is null)
        {
            return;
        }

        var existing = _deps.Profiles.FindByName(name);
        if (existing is not null && !await _deps.Dialogs.ConfirmAsync(L.Get("Profile_Save"), L.Format("Profile_Overwrite", name), L.Get("Common_Overwrite")))
        {
            return;
        }

        var delay = (int)Math.Round(SequenceDelayMs);
        var saved = _deps.Profiles.Save(new GameProfile
        {
            Id = existing?.Id ?? GameProfile.NewId(),
            CreatedAt = existing?.CreatedAt ?? default,
            Name = name,
            ClickPoints = Points.Select(p => p with { DelayAfterMs = delay }).ToList(),
            ClickSettings = CurrentTiming(),
            SequenceDelayMs = delay,
        });
        ProfilesChanged?.Invoke(this, saved);
        _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Format("Profile_Saved", saved.Name), ToastKind.Success);
    }

    [RelayCommand(CanExecute = nameof(CanLoadProfile))]
    public void LoadProfile()
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        Points.Clear();
        foreach (var point in profile.ClickPoints)
        {
            Points.Add(point);
        }

        ApplyTiming(profile.ClickSettings, profile.SequenceDelayMs);
        _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Format("Profile_Loaded", profile.Name, ProcessName), ToastKind.Success);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    public void ExportProfile()
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        var path = DialogService.PickSaveFile(L.Get("Filter_Profile"), profile.Name + ".autoclick");
        if (path is null)
        {
            return;
        }

        try
        {
            ProfileStore.Export(profile, path);
            _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Format("Profile_Exported", path), ToastKind.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _deps.Logger.LogWarning(ex, "Profile export failed");
            _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Format("Common_FileError", ex.Message), ToastKind.Error);
        }
    }

    [RelayCommand]
    public async Task ImportProfileAsync()
    {
        var path = DialogService.PickOpenFile(L.Get("Filter_Profile"));
        if (path is null)
        {
            return;
        }

        try
        {
            var profile = ProfileStore.ReadFile(path);
            if (_deps.Profiles.FindByName(profile.Name) is not null
                && !await _deps.Dialogs.ConfirmAsync(L.Get("Profile_Import"), L.Format("Profile_Overwrite", profile.Name), L.Get("Common_Overwrite")))
            {
                return;
            }

            var saved = _deps.Profiles.Import(profile);
            ProfilesChanged?.Invoke(this, saved);
            _deps.Dialogs.Toast(L.Get("Profile_Title"), L.Format("Profile_Imported", saved.Name), ToastKind.Success);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _deps.Logger.LogWarning(ex, "Profile import failed");
            await _deps.Dialogs.AlertAsync(L.Get("Profile_Import"), L.Format("Profile_ImportFailed", ex.Message));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    public async Task DeleteProfileAsync()
    {
        if (SelectedProfile is not { } profile
            || !await _deps.Dialogs.ConfirmAsync(L.Get("Profile_Delete"), L.Format("Profile_DeleteConfirm", profile.Name), L.Get("Common_Delete")))
        {
            return;
        }

        _deps.Profiles.Delete(profile.Id);
        SelectedProfile = null;
        ProfilesChanged?.Invoke(this, null);
    }

    /// <summary>Applies a game saved in a session file.</summary>
    public void ApplySavedGame(SavedGame game)
    {
        Points.Clear();
        foreach (var point in game.ClickPoints)
        {
            Points.Add(point);
        }

        ApplyTiming(game.Timing, game.SequenceDelayMs);
    }

    public SavedGame ToSavedGame()
    {
        var delay = (int)Math.Round(SequenceDelayMs);
        return new SavedGame
        {
            ProcessName = ProcessName,
            WindowTitle = WindowTitle,
            ExecutablePath = ExecutablePath,
            ClickPoints = Points.Select(p => p with { DelayAfterMs = delay }).ToList(),
            Timing = CurrentTiming(),
            SequenceDelayMs = delay,
            PixelGuard = _deps.Settings.Current.PixelGuard,
            IsCustomMode = IsCustomMode,
        };
    }

    /// <summary>Called once per second by the queue.</summary>
    public void Refresh()
    {
        Clicks = Stats.Clicks;
        Skipped = Stats.Skipped;
        LastIntervalSeconds = Math.Round(Stats.LastInterval.TotalSeconds, 1);

        // Clicks per minute over the last 60 seconds.
        var now = DateTime.UtcNow;
        _history.Enqueue((now, Stats.Clicks));
        while (now - _history.Peek().At > TimeSpan.FromSeconds(60))
        {
            _history.Dequeue();
        }

        var oldest = _history.Peek();
        var minutes = (now - oldest.At).TotalMinutes;
        ClicksPerMinute = minutes > 0.05 ? Math.Round((Stats.Clicks - oldest.Clicks) / minutes, 1) : 0;
    }

    /// <summary>Re-reads localized texts after a language change.</summary>
    public void RefreshTexts() => OnPropertyChanged(nameof(StateText));

    public void Dispose()
    {
        _deps.Overlays.HeatmapClosed -= OnHeatmapClosed;
        _deps.Overlays.CloseHeatmap(Id);
        if (_session is { } session)
        {
            session.StateChanged -= OnSessionStateChanged;
            _ = session.StopAsync(StopReason.User);
        }
    }

    public static string Describe(ClickPoint point) => point.ClickType switch
    {
        ClickType.Keystroke => L.Format("Step_Key", VirtualKeyNames.GetDisplayName(point.VirtualKeyCode)),
        ClickType.DoubleClick => L.Format("Step_Double", point.X, point.Y),
        ClickType.RightClick => L.Format("Step_Right", point.X, point.Y),
        _ => L.Format("Step_Left", point.X, point.Y),
    };

    private bool CanLoadProfile() => IsIdle && SelectedProfile is not null;

    private bool HasSelectedProfile() => SelectedProfile is not null;

    private ClickTiming CurrentTiming() =>
        new ClickTiming { Mode = Mode, FixedIntervalSeconds = FixedInterval, RandomMinSeconds = RandomMin, RandomMaxSeconds = RandomMax }.Normalize();

    private void ApplyTiming(ClickTiming timing, int sequenceDelayMs)
    {
        Mode = timing.Mode;
        FixedInterval = timing.FixedIntervalSeconds;
        RandomMin = timing.RandomMinSeconds;
        RandomMax = timing.RandomMaxSeconds;
        SequenceDelayMs = sequenceDelayMs;
    }

    private void MoveBy(ClickPoint? point, int delta)
    {
        if (point is not null)
        {
            var index = Points.IndexOf(point);
            ReorderPoint(new ReorderRequest(index, index + delta));
        }
    }

    private void OnPointsChanged()
    {
        OnPropertyChanged(nameof(HasPoints));
        OnPropertyChanged(nameof(CanStart));
        StartCommand.NotifyCanExecuteChanged();
    }

    private void OnHeatmapClosed(object? sender, string sessionId)
    {
        if (sessionId == Id)
        {
            OnPropertyChanged(nameof(IsHeatmapOpen));
        }
    }

    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e) =>
        _dispatcher.BeginInvoke(() => Apply(e.State, e.Reason, e.Detail, e.IsWaitingForWindow));

    private void Apply(SessionState state, StopReason reason, string? detail, bool waiting)
    {
        State = state;
        IsWaitingForWindow = waiting;
        OnPropertyChanged(nameof(CanStart));
        if (state != SessionState.Stopped)
        {
            return;
        }

        if (_session is { } session)
        {
            session.StateChanged -= OnSessionStateChanged;
            session.Dispose();
            _session = null;
        }

        StopMessage = reason switch
        {
            StopReason.WindowClosed => L.Get("Stop_WindowClosed"),
            StopReason.OutOfBounds => L.Format("Stop_OutOfBounds", detail),
            StopReason.ColorMismatch => L.Format("Stop_ColorMismatch", detail),
            StopReason.ElevationRequired => L.Get("Stop_ElevationRequired"),
            StopReason.TargetNotResponding => L.Get("Stop_TargetNotResponding"),
            StopReason.Error => L.Format("Stop_Error", detail),
            _ => string.Empty,
        };

        if (reason == StopReason.ElevationRequired)
        {
            ElevationRequired?.Invoke(this, EventArgs.Empty);
        }

        if (StopMessage.Length > 0)
        {
            _deps.Sound.Error();
        }

        Refresh();
    }
}
