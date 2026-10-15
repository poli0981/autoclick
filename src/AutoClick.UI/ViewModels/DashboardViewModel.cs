using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using AutoClick.UI.Localization;
using AutoClick.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Wpf.Ui.Abstractions.Controls;

namespace AutoClick.UI.ViewModels;

/// <summary>
/// Live statistics. Samples are collected every two seconds in plain lists; the charts are
/// fed only while the page is visible (v1 rebuilt every chart every two seconds, even hidden).
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject, INavigationAware
{
    private const int HistoryLength = 150; // five minutes at one sample every two seconds
    private const int SampleEveryTicks = 2;

    private readonly QueueViewModel _queue;
    private readonly DialogService _dialogs;
    private readonly TimeProvider _time;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly List<(int Index, double Cpm)> _history = [];
    private readonly Dictionary<string, List<(int Index, double Cpm)>> _gameHistory = [];
    private readonly ObservableCollection<ObservablePoint> _cpmPoints = [];
    private readonly ObservableValue _successValue = new(0);
    private readonly ObservableValue _skippedValue = new(0);
    private int _sampleIndex;
    private int _ticks;
    private bool _isVisible;
    private bool _recording;

    public DashboardViewModel(QueueViewModel queue, DialogService dialogs, ThemeService theme, TimeProvider time, ILogger<DashboardViewModel> logger)
    {
        _queue = queue;
        _dialogs = dialogs;
        _time = time;
        _logger = logger;

        CpmSeries = [new LineSeries<ObservablePoint> { Values = _cpmPoints, GeometrySize = 0, LineSmoothness = 0.3 }];
        RatioSeries =
        [
            new PieSeries<ObservableValue> { Values = [_successValue], InnerRadius = 50, MaxRadialColumnWidth = 30 },
            new PieSeries<ObservableValue> { Values = [_skippedValue], InnerRadius = 50, MaxRadialColumnWidth = 30 },
        ];
        CpmXAxes = [new Axis { Labeler = FormatSampleTime, TextSize = 11 }];
        CpmYAxes = [new Axis { MinLimit = 0, TextSize = 11 }];
        TimelineXAxes = [new Axis { Labeler = FormatSampleTime, TextSize = 11 }];
        TimelineYAxes = [new Axis { MinLimit = 0, TextSize = 11 }];
        BreakdownXAxes = [new Axis { TextSize = 11, LabelsRotation = 0 }];
        BreakdownYAxes = [new Axis { MinLimit = 0, TextSize = 11 }];
        ApplyThemeAndTexts();

        theme.ThemeChanged += (_, _) => ApplyThemeAndTexts();
        LocalizationService.Instance.LanguageChanged += (_, _) => ApplyThemeAndTexts();
    }

    public ISeries[] CpmSeries { get; }

    public ISeries[] RatioSeries { get; }

    public ICartesianAxis[] CpmXAxes { get; }

    public ICartesianAxis[] CpmYAxes { get; }

    public ICartesianAxis[] TimelineXAxes { get; }

    public ICartesianAxis[] TimelineYAxes { get; }

    public ICartesianAxis[] BreakdownXAxes { get; }

    public ICartesianAxis[] BreakdownYAxes { get; }

    [ObservableProperty]
    public partial ISeries[] BreakdownSeries { get; set; } = [];

    [ObservableProperty]
    public partial ISeries[] TimelineSeries { get; set; } = [];

    [ObservableProperty]
    public partial SolidColorPaint LegendTextPaint { get; set; } = new(SKColors.Gray);

    [ObservableProperty]
    public partial long TotalClicks { get; set; }

    [ObservableProperty]
    public partial long TotalSkipped { get; set; }

    [ObservableProperty]
    public partial double ClicksPerMinute { get; set; }

    [ObservableProperty]
    public partial double PeakClicksPerMinute { get; set; }

    [ObservableProperty]
    public partial string ActiveTime { get; set; } = "0:00:00";

    [ObservableProperty]
    public partial int RunningGames { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportStatsCommand))]
    public partial bool HasStats { get; set; }

    /// <summary>
    /// Starts sampling the queue. Called at startup, so the history and the peak also cover the
    /// time before the dashboard is first opened.
    /// </summary>
    public void StartRecording()
    {
        if (_recording)
        {
            return;
        }

        _recording = true;
        _queue.Refreshed += (_, _) => OnQueueRefreshed();
        _queue.StatsReset += (_, _) => ClearHistory();
    }

    public Task OnNavigatedToAsync()
    {
        _isVisible = true;
        RebuildCharts();
        UpdateSummary();
        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        _isVisible = false;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasStats))]
    public void ExportStats()
    {
        var path = DialogService.PickSaveFile(L.Get("Filter_Csv"), $"autoclick-stats-{_time.GetLocalNow():yyyyMMdd-HHmm}.csv");
        if (path is null)
        {
            return;
        }

        var csv = new StringBuilder("game,status,clicks,skipped,clicks_per_minute\n");
        foreach (var game in _queue.Games)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"{Csv(game.ProcessName)},{game.State},{game.Clicks},{game.Skipped},{game.ClicksPerMinute}");
        }

        foreach (var game in _queue.FinishedGames)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"{Csv(game.ProcessName)},Exited,{game.Clicks},{game.Skipped},");
        }

        try
        {
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            _dialogs.Toast(L.Get("Dashboard_Export"), L.Format("Common_SavedTo", path), ToastKind.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Statistics export failed");
            _dialogs.Toast(L.Get("Dashboard_Export"), L.Format("Common_FileError", ex.Message), ToastKind.Error);
        }
    }

    [RelayCommand]
    public async Task ResetStatsAsync()
    {
        if (await _dialogs.ConfirmAsync(L.Get("Dashboard_Reset"), L.Get("Dashboard_ResetConfirm"), L.Get("Dashboard_Reset")))
        {
            _queue.ResetAllStats();
        }
    }

    private void OnQueueRefreshed()
    {
        if (++_ticks % SampleEveryTicks == 0)
        {
            Sample();
        }

        if (_isVisible)
        {
            UpdateSummary();
        }
    }

    private void Sample()
    {
        var active = _queue.Games.Where(g => g.IsActive).ToList();
        if (active.Count == 0 && _history.Count == 0)
        {
            return;
        }

        var index = _sampleIndex++;
        var total = Math.Round(active.Sum(g => g.ClicksPerMinute), 1);
        PeakClicksPerMinute = Math.Max(PeakClicksPerMinute, total);
        Append(_history, index, total);

        var liveIds = _queue.Games.Select(g => g.Id).ToHashSet();
        foreach (var gone in _gameHistory.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            _gameHistory.Remove(gone);
        }

        foreach (var game in _queue.Games)
        {
            if (!_gameHistory.TryGetValue(game.Id, out var points))
            {
                _gameHistory[game.Id] = points = [];
            }

            Append(points, index, game.ClicksPerMinute);
        }

        if (!_isVisible)
        {
            return;
        }

        _cpmPoints.Add(new ObservablePoint(index, total));
        if (_cpmPoints.Count > HistoryLength)
        {
            _cpmPoints.RemoveAt(0);
        }

        // Timeline and breakdown are rebuilt only when games came or went.
        if (TimelineSeries.Length != _queue.Games.Count)
        {
            RebuildTimeline();
        }
        else
        {
            for (var i = 0; i < _queue.Games.Count; i++)
            {
                if (TimelineSeries[i].Values is ObservableCollection<ObservablePoint> values)
                {
                    values.Add(new ObservablePoint(index, _queue.Games[i].ClicksPerMinute));
                    if (values.Count > HistoryLength)
                    {
                        values.RemoveAt(0);
                    }
                }
            }
        }
    }

    private void UpdateSummary()
    {
        TotalClicks = _queue.TotalClicks;
        TotalSkipped = _queue.TotalSkipped;
        RunningGames = _queue.RunningCount;
        ClicksPerMinute = Math.Round(_queue.Games.Where(g => g.IsActive).Sum(g => g.ClicksPerMinute), 1);
        ActiveTime = _queue.ActiveTime.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        HasStats = TotalClicks + TotalSkipped > 0;
        _successValue.Value = TotalClicks;
        _skippedValue.Value = TotalSkipped;
        RebuildBreakdown();
    }

    private void RebuildCharts()
    {
        _cpmPoints.Clear();
        foreach (var (index, cpm) in _history)
        {
            _cpmPoints.Add(new ObservablePoint(index, cpm));
        }

        RebuildTimeline();
        RebuildBreakdown();
    }

    private void RebuildTimeline()
    {
        var palette = SeriesPalette();
        TimelineSeries = _queue.Games.Select((game, i) =>
        {
            var color = palette[i % palette.Length];
            var values = new ObservableCollection<ObservablePoint>(
                (_gameHistory.TryGetValue(game.Id, out var points) ? points : []).Select(p => new ObservablePoint(p.Index, p.Cpm)));
            return (ISeries)new LineSeries<ObservablePoint>
            {
                Name = game.ProcessName,
                Values = values,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
            };
        }).ToArray();
    }

    private void RebuildBreakdown()
    {
        var rows = _queue.Games.Select(g => (Name: g.ProcessName, g.Clicks, g.Skipped))
            .Concat(_queue.FinishedGames.Select(g => (Name: g.ProcessName, g.Clicks, g.Skipped)))
            .ToList();
        var labels = rows.Select(r => r.Name).ToArray();
        var axis = (Axis)BreakdownXAxes[0];
        if (BreakdownSeries.Length == 2
            && axis.Labels is { } current && current.SequenceEqual(labels)
            && BreakdownSeries[0].Values is long[] success && success.SequenceEqual(rows.Select(r => r.Clicks))
            && BreakdownSeries[1].Values is long[] skipped && skipped.SequenceEqual(rows.Select(r => r.Skipped)))
        {
            return;
        }

        axis.Labels = labels;
        var (successColor, skippedColor) = StatusColors();
        BreakdownSeries =
        [
            new ColumnSeries<long> { Name = L.Get("Stats_Success"), Values = rows.Select(r => r.Clicks).ToArray(), Fill = new SolidColorPaint(successColor) },
            new ColumnSeries<long> { Name = L.Get("Stats_Skipped"), Values = rows.Select(r => r.Skipped).ToArray(), Fill = new SolidColorPaint(skippedColor) },
        ];
    }

    private void ClearHistory()
    {
        _history.Clear();
        _gameHistory.Clear();
        _cpmPoints.Clear();
        _sampleIndex = 0;
        PeakClicksPerMinute = 0;
        if (_isVisible)
        {
            RebuildCharts();
            UpdateSummary();
        }
    }

    private void ApplyThemeAndTexts()
    {
        var text = ResourceColor("TextFillColorSecondaryBrush", new SKColor(0x9E, 0x9E, 0x9E));
        var separator = ResourceColor("ControlStrokeColorDefaultBrush", new SKColor(0x40, 0x40, 0x40));
        var accent = ResourceColor("AccentFillColorDefaultBrush", SKColors.CornflowerBlue);

        foreach (var axis in CpmXAxes.Concat(CpmYAxes).Concat(TimelineXAxes).Concat(TimelineYAxes).Concat(BreakdownXAxes).Concat(BreakdownYAxes).OfType<Axis>())
        {
            axis.LabelsPaint = new SolidColorPaint(text);
            axis.SeparatorsPaint = new SolidColorPaint(separator.WithAlpha(90));
            axis.NamePaint = new SolidColorPaint(text);
        }

        ((Axis)CpmYAxes[0]).Name = L.Get("Stats_ClicksPerMinute");
        ((Axis)TimelineYAxes[0]).Name = L.Get("Stats_ClicksPerMinute");
        ((Axis)CpmYAxes[0]).NameTextSize = 12;
        ((Axis)TimelineYAxes[0]).NameTextSize = 12;

        var cpm = (LineSeries<ObservablePoint>)CpmSeries[0];
        cpm.Name = L.Get("Stats_ClicksPerMinute");
        cpm.Stroke = new SolidColorPaint(accent, 2);
        cpm.Fill = new SolidColorPaint(accent.WithAlpha(40));

        var (successColor, skippedColor) = StatusColors();
        var success = (PieSeries<ObservableValue>)RatioSeries[0];
        success.Name = L.Get("Stats_Success");
        success.Fill = new SolidColorPaint(successColor);
        var skipped = (PieSeries<ObservableValue>)RatioSeries[1];
        skipped.Name = L.Get("Stats_Skipped");
        skipped.Fill = new SolidColorPaint(skippedColor);

        LegendTextPaint = new SolidColorPaint(text);
        BreakdownSeries = [];
        if (_isVisible)
        {
            RebuildTimeline();
            RebuildBreakdown();
        }
    }

    private static (SKColor Success, SKColor Skipped) StatusColors() =>
        (ResourceColor("SystemFillColorSuccessBrush", new SKColor(0x6C, 0xCB, 0x5F)),
         ResourceColor("SystemFillColorCautionBrush", new SKColor(0xFC, 0xE1, 0x00)));

    private static SKColor[] SeriesPalette() =>
    [
        ResourceColor("AccentFillColorDefaultBrush", SKColors.CornflowerBlue),
        new SKColor(0x6C, 0xCB, 0x5F),
        new SKColor(0xFF, 0x99, 0x33),
        new SKColor(0xE0, 0x5C, 0xB5),
        new SKColor(0x4F, 0xC3, 0xF7),
        new SKColor(0xFF, 0xD5, 0x4F),
        new SKColor(0x9C, 0x8C, 0xFF),
        new SKColor(0xF4, 0x43, 0x36),
    ];

    private static SKColor ResourceColor(string key, SKColor fallback) =>
        Application.Current?.TryFindResource(key) is SolidColorBrush { Color: var c } ? new SKColor(c.R, c.G, c.B, c.A) : fallback;

    private static string FormatSampleTime(double index) =>
        TimeSpan.FromSeconds(Math.Max(0, index) * SampleEveryTicks).ToString(@"m\:ss", CultureInfo.InvariantCulture);

    private static void Append(List<(int Index, double Cpm)> list, int index, double cpm)
    {
        list.Add((index, cpm));
        if (list.Count > HistoryLength)
        {
            list.RemoveAt(0);
        }
    }

    private static string Csv(string value) =>
        value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
