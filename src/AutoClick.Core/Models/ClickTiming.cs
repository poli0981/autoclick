namespace AutoClick.Core.Models;

/// <summary>
/// Interval between two runs of a click sequence. JSON names match v1's ClickProfile
/// ("clickSettings" in profiles, "profile" in sessions).
/// </summary>
public sealed record ClickTiming
{
    public ClickMode Mode { get; init; } = ClickMode.Random;
    public double FixedIntervalSeconds { get; init; } = Limits.DefaultIntervalSeconds;
    public double RandomMinSeconds { get; init; } = 1;
    public double RandomMaxSeconds { get; init; } = 60;

    public ClickTiming Normalize()
    {
        var min = Limits.ClampInterval(RandomMinSeconds);
        var max = Limits.ClampInterval(RandomMaxSeconds);
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return this with
        {
            Mode = Enum.IsDefined(Mode) ? Mode : ClickMode.Random,
            FixedIntervalSeconds = Limits.ClampInterval(FixedIntervalSeconds),
            RandomMinSeconds = min,
            RandomMaxSeconds = max,
        };
    }

    /// <summary>The wait before the next sequence run. Call on a normalized instance.</summary>
    public TimeSpan NextInterval(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var seconds = Mode == ClickMode.Fixed
            ? FixedIntervalSeconds
            : RandomMinSeconds + (random.NextDouble() * (RandomMaxSeconds - RandomMinSeconds));
        return TimeSpan.FromSeconds(seconds);
    }
}
