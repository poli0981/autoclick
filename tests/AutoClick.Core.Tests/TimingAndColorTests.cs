using AutoClick.Core.Models;

namespace AutoClick.Core.Tests;

public class TimingAndColorTests
{
    [Fact]
    public void NextInterval_Fixed_ReturnsFixedInterval()
    {
        var timing = new ClickTiming { Mode = ClickMode.Fixed, FixedIntervalSeconds = 4.25 }.Normalize();

        Assert.Equal(TimeSpan.FromSeconds(4.25), timing.NextInterval(new Random(1)));
    }

    [Fact]
    public void NextInterval_Random_StaysWithinRange()
    {
        var timing = new ClickTiming { Mode = ClickMode.Random, RandomMinSeconds = 2, RandomMaxSeconds = 3 }.Normalize();
        var random = new Random(42);

        for (var i = 0; i < 1000; i++)
        {
            var seconds = timing.NextInterval(random).TotalSeconds;
            Assert.InRange(seconds, 2, 3);
        }
    }

    [Fact]
    public void Normalize_SwapsReversedRangeAndClamps()
    {
        var timing = new ClickTiming { RandomMinSeconds = 9000, RandomMaxSeconds = 0.1, FixedIntervalSeconds = double.NaN }.Normalize();

        Assert.Equal(Limits.MinIntervalSeconds, timing.RandomMinSeconds);
        Assert.Equal(Limits.MaxIntervalSeconds, timing.RandomMaxSeconds);
        Assert.Equal(Limits.DefaultIntervalSeconds, timing.FixedIntervalSeconds);
    }

    [Theory]
    [InlineData(0x00102030u, 0x00102030u, 0, true)]
    [InlineData(0x00102030u, 0x0010203Au, 10, true)]
    [InlineData(0x00102030u, 0x0010203Bu, 10, false)]
    [InlineData(0x00FF0000u, 0x00F50000u, 10, true)]
    [InlineData(ClickPoint.NoColor, ClickPoint.NoColor, 255, false)]
    public void ColorRef_Matches_PerChannelWithinTolerance(uint actual, uint expected, int tolerance, bool matches)
    {
        Assert.Equal(matches, ColorRef.Matches(actual, expected, tolerance));
    }

    [Fact]
    public void ColorRef_ToHex_UsesRgbOrder()
    {
        // COLORREF is 0x00BBGGRR.
        Assert.Equal("#332211", ColorRef.ToHex(0x00112233u));
        Assert.Equal("N/A", ColorRef.ToHex(ClickPoint.NoColor));
    }
}
