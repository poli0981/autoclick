using System;
using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class ClickProfile
{
    public ClickMode Mode { get; set; } = ClickMode.Random;
    public double FixedIntervalSeconds { get; set; } = 2.0;
    public double RandomMinSeconds { get; set; } = 1.0;
    public double RandomMaxSeconds { get; set; } = 60.0;

    public double GetInterval()
    {
        return Mode switch
        {
            ClickMode.Fixed => FixedIntervalSeconds,
            ClickMode.Random => Random.Shared.NextDouble() * (RandomMaxSeconds - RandomMinSeconds) + RandomMinSeconds,
            _ => FixedIntervalSeconds
        };
    }
}
