using AutoClick.Core.Dpi;

namespace AutoClick.Core.Tests;

public class DpiMathTests
{
    public static TheoryData<int, int, int, int, double> AwarenessGrid => new()
    {
        // client size in the game's own space, physical size, expected scale
        { 1280, 720, 1280, 720, 1.0 },    // per-monitor aware game, any scaling
        { 1280, 720, 1600, 900, 1.25 },   // DPI-unaware game on a 125% monitor
        { 1280, 720, 1920, 1080, 1.5 },   // DPI-unaware game on a 150% monitor
        { 1280, 720, 2560, 1440, 2.0 },   // DPI-unaware game on a 200% monitor
        { 1920, 1080, 1920, 1080, 1.0 },  // system-aware game on its own monitor
    };

    [Theory]
    [MemberData(nameof(AwarenessGrid))]
    public void ClientScale_FollowsTheRatioOfBothSpaces(int clientW, int clientH, int physicalW, int physicalH, double expected)
    {
        var scale = DpiMath.ClientScale(clientW, clientH, new PixelRect(100, 50, physicalW, physicalH));

        Assert.Equal(expected, scale.X, 6);
        Assert.Equal(expected, scale.Y, 6);
    }

    [Fact]
    public void ClientScale_EmptyInput_IsIdentity()
    {
        Assert.Equal((1.0, 1.0), DpiMath.ClientScale(0, 720, new PixelRect(0, 0, 100, 100)));
        Assert.Equal((1.0, 1.0), DpiMath.ClientScale(100, 100, new PixelRect(0, 0, 0, 0)));
    }

    [Fact]
    public void ToPhysicalOffset_ScalesTheClientPoint()
    {
        Assert.Equal((150.0, 75.0), DpiMath.ToPhysicalOffset(100, 50, (1.5, 1.5)));
    }

    [Theory]
    [InlineData(150, 144, 100)]
    [InlineData(96, 96, 96)]
    [InlineData(200, 192, 100)]
    public void ToDips_ConvertsPhysicalPixels(double physical, double dpi, double expected)
    {
        Assert.Equal(expected, DpiMath.ToDips(physical, dpi), 6);
    }

    [Fact]
    public void ClampToClient_KeepsPointInside()
    {
        Assert.Equal(new PixelPoint(0, 719), DpiMath.ClampToClient(-3, 800, 1280, 720));
        Assert.Equal(new PixelPoint(1279, 0), DpiMath.ClampToClient(1282, -1, 1280, 720));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1279, 719, true)]
    [InlineData(1280, 10, false)]
    [InlineData(10, 720, false)]
    [InlineData(-1, 10, false)]
    public void IsInsideClient_UsesExclusiveUpperBound(int x, int y, bool expected)
    {
        Assert.Equal(expected, DpiMath.IsInsideClient(x, y, 1280, 720));
    }

    // A game on a second monitor, left of the primary one.
    private static readonly PixelRect GameArea = new(-1920, 100, 1280, 720);

    [Theory]
    [InlineData(-1500, 400, 10, 0, -1490, 400)]
    [InlineData(-1500, 400, 0, -1, -1500, 399)]
    [InlineData(-1915, 400, -10, 0, -1920, 400)]
    [InlineData(-645, 815, 10, 10, -641, 819)]
    public void MovePointer_MovesInsideTheArea(int x, int y, int dx, int dy, int expectedX, int expectedY)
    {
        Assert.Equal(new PixelPoint(expectedX, expectedY), DpiMath.MovePointer(new PixelPoint(x, y), GameArea, dx, dy));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-640, 400)]
    [InlineData(-1500, 820)]
    public void MovePointer_StartsFromTheCenterWhenOutside(int x, int y)
    {
        Assert.Equal(new PixelPoint(-1280, 460), DpiMath.MovePointer(new PixelPoint(x, y), GameArea, 10, 0));
    }

    [Fact]
    public void MovePointer_LeavesThePointerForAnEmptyArea()
    {
        Assert.Equal(new PixelPoint(5, 5), DpiMath.MovePointer(new PixelPoint(5, 5), new PixelRect(0, 0, 0, 0), 10, 0));
    }
}
