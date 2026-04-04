using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
using AutoClick.Services;
using AutoClick.UI.Resources;

namespace AutoClick.UI.ViewModels;

public class GameSessionViewModel : ViewModelBase
{
    private readonly IClickEngine _clickEngine;
    private readonly ILogService _log;
    private readonly SoundService? _sound;
    private readonly GameSession _session;
    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _uiTimer;

    public GameSession Session => _session;
    public string Id => _session.Id;
    public string ProcessName => _session.ProcessName;
    public string WindowTitle => _session.WindowTitle;
    public string ExecutablePath => _session.ExecutablePath;

    private string _stateText = "Idle";
    public string StateText { get => _stateText; set => SetProperty(ref _stateText, value); }

    private SessionState _currentState = SessionState.Idle;
    public SessionState CurrentState { get => _currentState; set => SetProperty(ref _currentState, value); }

    private string _coordinateText = "-";
    public string CoordinateText { get => _coordinateText; set => SetProperty(ref _coordinateText, value); }

    private long _clickCount;
    public long ClickCount { get => _clickCount; set => SetProperty(ref _clickCount, value); }

    private double _lastInterval;
    public double LastInterval { get => _lastInterval; set => SetProperty(ref _lastInterval, value); }

    private double _fixedInterval = 2.0;
    public double FixedInterval
    {
        get => _fixedInterval;
        set
        {
            if (value < 1) value = 1;
            if (value > 60) value = 60;
            SetProperty(ref _fixedInterval, value);
            _session.Profile.FixedIntervalSeconds = value;
        }
    }

    private bool _canEditInterval = true;
    public bool CanEditInterval { get => _canEditInterval; set => SetProperty(ref _canEditInterval, value); }

    public bool IsIdle => _session.State == SessionState.Idle || _session.State == SessionState.Stopped;
    public bool IsRunning => _session.State == SessionState.Running;
    public bool IsPaused => _session.State == SessionState.Paused;
    public bool HasCoordinate => _session.ClickPoints.Count > 0;
    public int PointCount => _session.ClickPoints.Count;
    public bool HasMultiplePoints => _session.ClickPoints.Count > 1;

    /// <summary>
    /// Delay in ms between each click point in the sequence.
    /// Applied to all points uniformly. Editable while idle.
    /// </summary>
    private int _sequenceDelayMs = 200;
    public int SequenceDelayMs
    {
        get => _sequenceDelayMs;
        set
        {
            var clamped = Math.Clamp(value, 0, 10000);
            SetProperty(ref _sequenceDelayMs, clamped);
            // Apply to all points
            foreach (var pt in _session.ClickPoints)
                pt.DelayAfterMs = clamped;
        }
    }

    public int SelectedModeIndex
    {
        get => _session.Profile.Mode == ClickMode.Random ? 0 : 1;
        set { _session.Profile.Mode = value == 0 ? ClickMode.Random : ClickMode.Fixed; OnPropertyChanged(); }
    }

    public double PerGameRandomMin
    {
        get => _session.Profile.RandomMinSeconds;
        set { _session.Profile.RandomMinSeconds = Math.Clamp(value, 1, _session.Profile.RandomMaxSeconds - 1); OnPropertyChanged(); }
    }

    public double PerGameRandomMax
    {
        get => _session.Profile.RandomMaxSeconds;
        set { _session.Profile.RandomMaxSeconds = Math.Clamp(value, _session.Profile.RandomMinSeconds + 1, 60); OnPropertyChanged(); }
    }

    private bool _isCustomMode;
    public bool IsCustomMode { get => _isCustomMode; set => SetProperty(ref _isCustomMode, value); }

    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ResetStatsCommand { get; }

    public GameSessionViewModel(GameSession session, IClickEngine clickEngine, ILogService log, SoundService? sound = null)
    {
        _session = session;
        _clickEngine = clickEngine;
        _log = log;
        _sound = sound;

        StartCommand = new RelayCommand(Start, () => IsIdle && HasCoordinate);
        PauseCommand = new RelayCommand(Pause, () => IsRunning);
        ResumeCommand = new RelayCommand(Resume, () => IsPaused);
        StopCommand = new RelayCommand(Stop, () => IsRunning || IsPaused);
        ResetStatsCommand = new RelayCommand(ResetStats, () => IsIdle);

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _uiTimer.Tick += (_, _) => RefreshUI();

        UpdateCoordinateText();
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _session.State = SessionState.Running;
        CanEditInterval = false;
        _uiTimer.Start();
        _ = _clickEngine.StartAsync(_session, _cts.Token);
        RefreshState();
        _sound?.PlayStart();
    }

    public void Pause()
    {
        _clickEngine.Pause(_session);
        CanEditInterval = true;
        RefreshState();
        _sound?.PlayToggle();
    }

    public void Resume()
    {
        _clickEngine.Resume(_session);
        CanEditInterval = false;
        RefreshState();
        _sound?.PlayToggle();
    }

    public void Stop()
    {
        _clickEngine.Stop(_session);
        _cts?.Cancel();
        _uiTimer.Stop();
        CanEditInterval = true;
        _session.State = SessionState.Stopped;
        RefreshState();
        _sound?.PlayStop();
    }

    public void ResetStats()
    {
        _session.ClickCount = 0;
        _session.LastIntervalSeconds = 0;
        _session.StartedAt = null;
        ClickCount = 0;
        LastInterval = 0;
        _log.Info($"Stats reset for \"{ProcessName}\"");
    }

    public void UpdateCoordinateText()
    {
        CoordinateText = _session.ClickPoints.Count > 0
            ? string.Join(" → ", _session.ClickPoints.Select((p, i) => $"#{i + 1}{p}"))
            : "-";
        OnPropertyChanged(nameof(HasCoordinate));
        OnPropertyChanged(nameof(PointCount));
        OnPropertyChanged(nameof(HasMultiplePoints));
    }

    public void ApplyProfile(GameProfile profile)
    {
        _session.ClickPoints.Clear();
        foreach (var p in profile.ClickPoints)
            _session.ClickPoints.Add(new ClickPoint(p.X, p.Y, p.ClickType, p.DelayAfterMs));

        _session.Profile.Mode = profile.ClickSettings.Mode;
        _session.Profile.FixedIntervalSeconds = profile.ClickSettings.FixedIntervalSeconds;
        _session.Profile.RandomMinSeconds = profile.ClickSettings.RandomMinSeconds;
        _session.Profile.RandomMaxSeconds = profile.ClickSettings.RandomMaxSeconds;

        _fixedInterval = profile.ClickSettings.FixedIntervalSeconds;
        OnPropertyChanged(nameof(FixedInterval));
        OnPropertyChanged(nameof(SelectedModeIndex));
        OnPropertyChanged(nameof(PerGameRandomMin));
        OnPropertyChanged(nameof(PerGameRandomMax));

        _sequenceDelayMs = profile.SequenceDelayMs;
        OnPropertyChanged(nameof(SequenceDelayMs));

        UpdateCoordinateText();
    }

    private void RefreshUI()
    {
        ClickCount = _session.ClickCount;
        LastInterval = Math.Round(_session.LastIntervalSeconds, 2);
        RefreshState();
    }

    private void RefreshState()
    {
        CurrentState = _session.State;
        StateText = _session.State switch
        {
            SessionState.Running => Strings.Running,
            SessionState.Paused => Strings.Paused,
            SessionState.Stopped => Strings.Stopped,
            _ => Strings.Idle
        };
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(HasCoordinate));
    }
}
