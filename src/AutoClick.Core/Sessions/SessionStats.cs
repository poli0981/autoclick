using System.Collections.Concurrent;

namespace AutoClick.Core.Sessions;

/// <summary>A clicked client point and how often it was clicked.</summary>
public readonly record struct HeatPoint(int X, int Y, int Count);

/// <summary>
/// Counters for one game session. Written by the click engine's worker, read by the UI:
/// every member is thread-safe.
/// </summary>
public sealed class SessionStats
{
    private readonly ConcurrentDictionary<(int X, int Y), int> _heatmap = new();
    private long _clicks;
    private long _skipped;
    private long _lastIntervalTicks;
    private int _heatmapVersion;

    /// <summary>Successful clicks and keystrokes.</summary>
    public long Clicks => Interlocked.Read(ref _clicks);

    /// <summary>Points skipped by the pixel color guard.</summary>
    public long Skipped => Interlocked.Read(ref _skipped);

    public long Total => Clicks + Skipped;

    public TimeSpan LastInterval => TimeSpan.FromTicks(Interlocked.Read(ref _lastIntervalTicks));

    /// <summary>Changes whenever the heatmap changes, so readers can skip redundant redraws.</summary>
    public int HeatmapVersion => Volatile.Read(ref _heatmapVersion);

    /// <summary>Records a click at the original point (before jitter).</summary>
    public void RecordClick(int x, int y)
    {
        Interlocked.Increment(ref _clicks);
        _heatmap.AddOrUpdate((x, y), 1, (_, count) => count + 1);
        Interlocked.Increment(ref _heatmapVersion);
    }

    public void RecordKeystroke() => Interlocked.Increment(ref _clicks);

    public void RecordSkip() => Interlocked.Increment(ref _skipped);

    public void RecordInterval(TimeSpan interval) => Interlocked.Exchange(ref _lastIntervalTicks, interval.Ticks);

    public IReadOnlyList<HeatPoint> GetHeatmap() =>
        _heatmap.Select(kv => new HeatPoint(kv.Key.X, kv.Key.Y, kv.Value)).ToList();

    public void Reset()
    {
        Interlocked.Exchange(ref _clicks, 0);
        Interlocked.Exchange(ref _skipped, 0);
        Interlocked.Exchange(ref _lastIntervalTicks, 0);
        _heatmap.Clear();
        Interlocked.Increment(ref _heatmapVersion);
    }
}
