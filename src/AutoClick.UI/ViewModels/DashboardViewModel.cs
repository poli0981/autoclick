using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AutoClick.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    private readonly DispatcherTimer _timer;

    // CPM history (circular buffer, max 150 points = 5 min at 2s intervals)
    private const int MaxHistory = 150;
    private readonly ObservableCollection<ObservablePoint> _cpmPoints = new();
    private int _historyIndex;
    private long _lastChartClicks;

    // Per-game timeline history
    private const int MaxTimelineHistory = 150;
    private readonly Dictionary<string, ObservableCollection<ObservablePoint>> _gameTimelinePoints = new();
    private readonly Dictionary<string, long> _gameLastClicks = new();
    private int _timelineIndex;

    // Archived stats for exited games (preserved until reset)
    private readonly List<ArchivedGameStats> _archivedGames = new();

    // ── CPM Line Chart ──

    public ISeries[] CpmSeries { get; }

    public Axis[] CpmXAxes { get; }
    public Axis[] CpmYAxes { get; }

    // ── Per-Game Breakdown ──

    private ISeries[] _gameBreakdownSeries = [];
    public ISeries[] GameBreakdownSeries
    {
        get => _gameBreakdownSeries;
        set => SetProperty(ref _gameBreakdownSeries, value);
    }

    private Axis[] _gameXAxes = [new Axis { MinLimit = 0 }];
    public Axis[] GameXAxes
    {
        get => _gameXAxes;
        set => SetProperty(ref _gameXAxes, value);
    }

    private Axis[] _gameYAxes = [new Axis()];
    public Axis[] GameYAxes
    {
        get => _gameYAxes;
        set => SetProperty(ref _gameYAxes, value);
    }

    private int _gameChartHeight = 80;
    public int GameChartHeight
    {
        get => _gameChartHeight;
        set => SetProperty(ref _gameChartHeight, value);
    }

    // ── Success/Skip Ratio Pie Chart ──

    private ISeries[] _ratioSeries = [];
    public ISeries[] RatioSeries
    {
        get => _ratioSeries;
        set => SetProperty(ref _ratioSeries, value);
    }

    private bool _hasRatioData;
    public bool HasRatioData
    {
        get => _hasRatioData;
        set => SetProperty(ref _hasRatioData, value);
    }

    // ── Per-Game CPM Timeline ──

    private ISeries[] _timelineSeries = [];
    public ISeries[] TimelineSeries
    {
        get => _timelineSeries;
        set => SetProperty(ref _timelineSeries, value);
    }

    public Axis[] TimelineXAxes { get; }
    public Axis[] TimelineYAxes { get; }

    // ── Summary Cards (forwarded from MainViewModel) ──

    public long TotalClicks => _mainVm.TotalClicks;
    public long TotalSkipped => _mainVm.TotalSkipped;
    public string SessionUptime => _mainVm.SessionUptime;
    public double ClicksPerMinute => _mainVm.ClicksPerMinute;
    public long PeakClicksPerMinute => _mainVm.PeakClicksPerMinute;
    public bool HasStats => _mainVm.HasStats;

    public bool HasAnyRunning => _mainVm.HasAnyRunning;

    // ── Commands ──

    public RelayCommand ExportStatsCommand { get; }
    public RelayCommand ResetDashboardCommand { get; }

    public DashboardViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;

        ExportStatsCommand = new RelayCommand(OnExportStats, () => HasStats);
        ResetDashboardCommand = new RelayCommand(OnResetDashboard, () => !HasAnyRunning);

        // Forward property changes from MainViewModel
        _mainVm.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(MainViewModel.TotalClicks): OnPropertyChanged(nameof(TotalClicks)); break;
                case nameof(MainViewModel.TotalSkipped): OnPropertyChanged(nameof(TotalSkipped)); break;
                case nameof(MainViewModel.SessionUptime): OnPropertyChanged(nameof(SessionUptime)); break;
                case nameof(MainViewModel.ClicksPerMinute): OnPropertyChanged(nameof(ClicksPerMinute)); break;
                case nameof(MainViewModel.PeakClicksPerMinute): OnPropertyChanged(nameof(PeakClicksPerMinute)); break;
                case nameof(MainViewModel.HasStats): OnPropertyChanged(nameof(HasStats)); break;
                case nameof(MainViewModel.HasAnyRunning): OnPropertyChanged(nameof(HasAnyRunning)); break;
            }
        };

        // Listen for game exits to archive stats
        _mainVm.GameExited += OnGameExited;

        // CPM line series
        var primaryColor = GetSkColor("PrimaryBrush", SKColors.CornflowerBlue);
        CpmSeries =
        [
            new LineSeries<ObservablePoint>
            {
                Values = _cpmPoints,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Stroke = new SolidColorPaint(primaryColor, 2),
                Fill = new SolidColorPaint(primaryColor.WithAlpha(40)),
                AnimationsSpeed = TimeSpan.FromMilliseconds(100)
            }
        ];

        var axisTextColor = GetSkColor("TextSecondaryBrush", SKColors.Gray);
        var separatorColor = GetSkColor("BorderBrush", new SKColor(51, 65, 85));

        CpmXAxes =
        [
            new Axis
            {
                Labeler = val => TimeSpan.FromSeconds(val * 2).ToString(@"m\:ss"),
                LabelsPaint = new SolidColorPaint(axisTextColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor),
                TextSize = 11
            }
        ];

        CpmYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(axisTextColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor),
                TextSize = 11
            }
        ];

        // Timeline axes
        TimelineXAxes =
        [
            new Axis
            {
                Labeler = val => TimeSpan.FromSeconds(val * 2).ToString(@"m\:ss"),
                LabelsPaint = new SolidColorPaint(axisTextColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor),
                TextSize = 11
            }
        ];

        TimelineYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                Name = Resources.Strings.ClicksPerMin,
                NamePaint = new SolidColorPaint(axisTextColor),
                LabelsPaint = new SolidColorPaint(axisTextColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor),
                TextSize = 11
            }
        ];

        // Timer for chart updates
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => UpdateCharts();
        _timer.Start();
    }

    private void OnGameExited(string processName, long clicks, long skipped)
    {
        _archivedGames.Add(new ArchivedGameStats
        {
            ProcessName = processName,
            ClickCount = clicks,
            SkippedClicks = skipped,
            ExitedAt = DateTime.Now
        });
    }

    private bool _wasRunning;

    private void UpdateCharts()
    {
        bool isRunning = _mainVm.HasAnyRunning;

        // Always update static charts (breakdown, ratio) so archived data shows
        UpdateGameBreakdown();
        UpdateRatioChart();

        // Only update live time-series charts while tasks are running
        if (isRunning)
        {
            // ── CPM History ──
            long currentTotal = _mainVm.TotalClicks;
            double instantCpm = (currentTotal - _lastChartClicks) / 2.0 * 60.0;
            _lastChartClicks = currentTotal;

            _cpmPoints.Add(new ObservablePoint(_historyIndex, Math.Round(instantCpm, 1)));
            _historyIndex++;

            if (_cpmPoints.Count > MaxHistory)
                _cpmPoints.RemoveAt(0);

            // Slide X axis window
            if (_historyIndex > MaxHistory)
            {
                CpmXAxes[0].MinLimit = _historyIndex - MaxHistory;
                CpmXAxes[0].MaxLimit = _historyIndex;
            }

            // ── Per-Game Timeline ──
            UpdateTimeline();

            _wasRunning = true;
        }
        else if (_wasRunning)
        {
            // Just stopped: sync _lastChartClicks so next start doesn't show a spike
            _lastChartClicks = _mainVm.TotalClicks;
            _wasRunning = false;
        }
    }

    private void UpdateGameBreakdown()
    {
        var sessions = _mainVm.GameSessions;

        // Combine active sessions + archived
        var allGames = new List<(string Name, long Clicks, long Skipped)>();

        foreach (var s in sessions)
            allGames.Add((s.ProcessName, s.Session.ClickCount, s.Session.SkippedClicks));

        foreach (var a in _archivedGames)
            allGames.Add(($"{a.ProcessName} ✕", a.ClickCount, a.SkippedClicks));

        if (allGames.Count == 0)
        {
            GameBreakdownSeries = [];
            return;
        }

        var successColor = GetSkColor("SuccessBrush", SKColors.LimeGreen);
        var skippedColor = GetSkColor("WarningBrush", SKColors.Orange);

        var successValues = new ObservableValue[allGames.Count];
        var skippedValues = new ObservableValue[allGames.Count];
        var labels = new string[allGames.Count];

        for (int i = 0; i < allGames.Count; i++)
        {
            successValues[i] = new ObservableValue(allGames[i].Clicks);
            skippedValues[i] = new ObservableValue(allGames[i].Skipped);
            labels[i] = allGames[i].Name;
        }

        GameBreakdownSeries =
        [
            new RowSeries<ObservableValue>
            {
                Values = successValues,
                Name = Resources.Strings.ClickSuccess,
                Stroke = null,
                Fill = new SolidColorPaint(successColor),
                MaxBarWidth = 25
            },
            new RowSeries<ObservableValue>
            {
                Values = skippedValues,
                Name = Resources.Strings.ClickSkipped,
                Stroke = null,
                Fill = new SolidColorPaint(skippedColor),
                MaxBarWidth = 25
            }
        ];

        var axisTextColor = GetSkColor("TextSecondaryBrush", SKColors.Gray);
        var separatorColor = GetSkColor("BorderBrush", new SKColor(51, 65, 85));

        GameYAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(axisTextColor),
                TextSize = 12,
                MinStep = 1,
                ForceStepToMin = true
            }
        ];

        GameXAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(axisTextColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor),
                TextSize = 11
            }
        ];

        GameChartHeight = Math.Max(80, allGames.Count * 60);
    }

    private void UpdateRatioChart()
    {
        long totalSuccess = _mainVm.TotalClicks + _archivedGames.Sum(a => a.ClickCount);
        long totalSkipped = _mainVm.TotalSkipped + _archivedGames.Sum(a => a.SkippedClicks);
        long total = totalSuccess + totalSkipped;

        if (total == 0)
        {
            HasRatioData = false;
            return;
        }

        HasRatioData = true;
        var successColor = GetSkColor("SuccessBrush", SKColors.LimeGreen);
        var skippedColor = GetSkColor("WarningBrush", SKColors.Orange);

        RatioSeries =
        [
            new PieSeries<double>
            {
                Values = [totalSuccess],
                Name = $"{Resources.Strings.ClickSuccess} ({totalSuccess})",
                Fill = new SolidColorPaint(successColor),
                MaxRadialColumnWidth = 30
            },
            new PieSeries<double>
            {
                Values = [totalSkipped],
                Name = $"{Resources.Strings.ClickSkipped} ({totalSkipped})",
                Fill = new SolidColorPaint(skippedColor),
                MaxRadialColumnWidth = 30
            }
        ];
    }

    private static readonly SKColor[] GameColors =
    [
        new(100, 181, 246), // blue
        new(129, 199, 132), // green
        new(255, 183, 77),  // orange
        new(206, 147, 216), // purple
        new(255, 138, 128), // red
        new(128, 222, 234), // cyan
        new(255, 213, 79),  // yellow
        new(176, 190, 197)  // grey
    ];

    private void UpdateTimeline()
    {
        var sessions = _mainVm.GameSessions;
        if (sessions.Count == 0 && _gameTimelinePoints.Count == 0)
        {
            TimelineSeries = [];
            return;
        }

        bool seriesChanged = false;

        // Track which games we've seen
        var activeIds = new HashSet<string>();

        foreach (var s in sessions)
        {
            activeIds.Add(s.Id);
            if (!_gameTimelinePoints.ContainsKey(s.Id))
            {
                _gameTimelinePoints[s.Id] = new ObservableCollection<ObservablePoint>();
                _gameLastClicks[s.Id] = 0;
                seriesChanged = true;
            }

            long currentClicks = s.Session.ClickCount;
            long delta = currentClicks - _gameLastClicks[s.Id];
            double cpm = delta / 2.0 * 60.0;
            _gameLastClicks[s.Id] = currentClicks;

            var points = _gameTimelinePoints[s.Id];
            points.Add(new ObservablePoint(_timelineIndex, Math.Round(cpm, 1)));
            if (points.Count > MaxTimelineHistory)
                points.RemoveAt(0);
        }

        _timelineIndex++;

        // Slide timeline X axis
        if (_timelineIndex > MaxTimelineHistory)
        {
            TimelineXAxes[0].MinLimit = _timelineIndex - MaxTimelineHistory;
            TimelineXAxes[0].MaxLimit = _timelineIndex;
        }

        // Rebuild series if games were added
        if (seriesChanged)
        {
            var newSeries = new List<ISeries>();
            int colorIdx = 0;
            foreach (var s in sessions)
            {
                var color = GameColors[colorIdx % GameColors.Length];
                newSeries.Add(new LineSeries<ObservablePoint>
                {
                    Values = _gameTimelinePoints[s.Id],
                    Name = s.ProcessName,
                    GeometrySize = 0,
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(color, 2),
                    Fill = new SolidColorPaint(color.WithAlpha(30)),
                    AnimationsSpeed = TimeSpan.FromMilliseconds(100)
                });
                colorIdx++;
            }
            TimelineSeries = newSeries.ToArray();
        }
    }

    private void OnExportStats()
    {
        var sessions = _mainVm.GameSessions;
        var statsData = new
        {
            ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Session = new
            {
                TotalClicks = _mainVm.TotalClicks + _archivedGames.Sum(a => a.ClickCount),
                TotalSkipped = _mainVm.TotalSkipped + _archivedGames.Sum(a => a.SkippedClicks),
                _mainVm.SessionUptime,
                ClicksPerMinute = Math.Round(_mainVm.ClicksPerMinute, 2),
                _mainVm.PeakClicksPerMinute
            },
            ActiveGames = sessions.Select(g => new
            {
                g.ProcessName,
                g.Session.ProcessId,
                ClickCount = g.Session.ClickCount,
                SkippedClicks = g.Session.SkippedClicks,
                Total = g.Session.ClickCount + g.Session.SkippedClicks,
                State = g.CurrentState.ToString(),
                ClickPoints = g.Session.ClickPoints.Select(p => new { p.X, p.Y, ClickType = p.ClickType.ToString() })
            }),
            ExitedGames = _archivedGames.Select(a => new
            {
                a.ProcessName,
                a.ClickCount,
                a.SkippedClicks,
                Total = a.ClickCount + a.SkippedClicks,
                ExitedAt = a.ExitedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
        };

        var dlg = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = $"AutoClick_Session_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dlg.ShowDialog() == true)
        {
            var json = JsonSerializer.Serialize(statsData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(dlg.FileName, json);
        }
    }

    private void OnResetDashboard()
    {
        if (MessageBox.Show(
            Resources.Strings.ConfirmResetStats,
            Resources.Strings.AppTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        // Reset CPM chart
        _cpmPoints.Clear();
        _historyIndex = 0;
        _lastChartClicks = 0;
        CpmXAxes[0].MinLimit = null;
        CpmXAxes[0].MaxLimit = null;

        // Reset game breakdown
        GameBreakdownSeries = [];
        _archivedGames.Clear();

        // Reset ratio
        RatioSeries = [];
        HasRatioData = false;

        // Reset timeline
        _gameTimelinePoints.Clear();
        _gameLastClicks.Clear();
        _timelineIndex = 0;
        TimelineSeries = [];
        TimelineXAxes[0].MinLimit = null;
        TimelineXAxes[0].MaxLimit = null;

        // Reset main stats
        _mainVm.ResetSessionStats();
    }

    private static SKColor GetSkColor(string resourceKey, SKColor fallback)
    {
        try
        {
            if (Application.Current?.Resources[resourceKey] is SolidColorBrush brush)
            {
                var c = brush.Color;
                return new SKColor(c.R, c.G, c.B, c.A);
            }
        }
        catch { }
        return fallback;
    }
}

public class ArchivedGameStats
{
    public string ProcessName { get; set; } = "";
    public long ClickCount { get; set; }
    public long SkippedClicks { get; set; }
    public DateTime ExitedAt { get; set; }
}
