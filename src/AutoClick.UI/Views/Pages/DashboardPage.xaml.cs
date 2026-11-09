using AutoClick.UI.ViewModels;
using LiveChartsCore.Kernel.Sketches;

namespace AutoClick.UI.Views.Pages;

public partial class DashboardPage
{
    private readonly (IChartView Chart, TimeSpan Speed)[] _charts;

    public DashboardPage(DashboardViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        // The charts animate their updates unless Windows' "Animation effects" are off.
        _charts = [.. new IChartView[] { CpmChart, RatioChart, BreakdownChart, TimelineChart }.Select(chart => (chart, chart.AnimationsSpeed))];
        ApplyMotion();
        Motion.Changed += (_, _) => Dispatcher.Invoke(ApplyMotion);
    }

    private void ApplyMotion()
    {
        foreach (var (chart, speed) in _charts)
        {
            chart.AnimationsSpeed = Motion.IsEnabled ? speed : TimeSpan.Zero;
        }
    }
}
