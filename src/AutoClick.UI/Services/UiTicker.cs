using System.Windows.Threading;

namespace AutoClick.UI.Services;

/// <summary>
/// The single once-per-second UI clock (statistics, scheduler, window checks, charts). One
/// timer instead of v1's five independent ones, so all readers see the same moment.
/// </summary>
public sealed class UiTicker
{
    private readonly DispatcherTimer _timer;

    public UiTicker()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised on the UI thread about once per second.</summary>
    public event EventHandler? Tick;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
