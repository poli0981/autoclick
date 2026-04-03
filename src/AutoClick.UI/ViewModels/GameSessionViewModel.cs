using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
using AutoClick.UI.Resources;

namespace AutoClick.UI.ViewModels;

public class GameSessionViewModel : ViewModelBase
{
    private readonly IClickEngine _clickEngine;
    private readonly ILogService _log;
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

    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ResetStatsCommand { get; }

    public GameSessionViewModel(GameSession session, IClickEngine clickEngine, ILogService log)
    {
        _session = session;
        _clickEngine = clickEngine;
        _log = log;

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
    }

    public void Pause()
    {
        _clickEngine.Pause(_session);
        CanEditInterval = true;
        RefreshState();
    }

    public void Resume()
    {
        _clickEngine.Resume(_session);
        CanEditInterval = false;
        RefreshState();
    }

    public void Stop()
    {
        _clickEngine.Stop(_session);
        _cts?.Cancel();
        _uiTimer.Stop();
        CanEditInterval = true;
        _session.State = SessionState.Stopped;
        RefreshState();
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
