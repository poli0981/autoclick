namespace AutoClick.Core.Dpi;

/// <summary>A point or size in whole pixels.</summary>
public readonly record struct PixelPoint(int X, int Y);

public readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// Pure coordinate math used by the overlays. Click points are stored in the game window's
/// own client coordinates (what PostMessage expects). For a DPI-unaware game on a scaled
/// monitor those are "logical" pixels, while the overlay is positioned in physical pixels.
/// </summary>
public static class DpiMath
{
    /// <summary>
    /// Scale between the game's own client coordinates and physical pixels, measured from the
    /// same client area seen in both coordinate spaces.
    /// </summary>
    public static (double X, double Y) ClientScale(int clientWidth, int clientHeight, PixelRect physicalClient)
    {
        if (clientWidth <= 0 || clientHeight <= 0 || physicalClient.IsEmpty)
        {
            return (1.0, 1.0);
        }

        return ((double)physicalClient.Width / clientWidth, (double)physicalClient.Height / clientHeight);
    }

    /// <summary>Offset (in physical pixels) of a game client point inside the client area.</summary>
    public static (double X, double Y) ToPhysicalOffset(int clientX, int clientY, (double X, double Y) scale) =>
        (clientX * scale.X, clientY * scale.Y);

    /// <summary>Physical pixels to WPF device-independent pixels for a window at <paramref name="dpi"/>.</summary>
    public static double ToDips(double physical, double dpi) => dpi > 0 ? physical * 96.0 / dpi : physical;

    /// <summary>Clamps a point to the client area [0, size - 1].</summary>
    public static PixelPoint ClampToClient(int x, int y, int clientWidth, int clientHeight) =>
        new(Math.Clamp(x, 0, Math.Max(0, clientWidth - 1)), Math.Clamp(y, 0, Math.Max(0, clientHeight - 1)));

    public static bool IsInsideClient(int x, int y, int clientWidth, int clientHeight) =>
        x >= 0 && y >= 0 && x < clientWidth && y < clientHeight;
}
