using AutoClick.Core.Models;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Win32;

/// <summary>
/// Helper for coordinate validation and random coordinate generation
/// within a game window's client area.
/// </summary>
public static class CoordinateHelper
{
    /// <summary>
    /// Gets the client area rectangle of a window.
    /// Returns (width, height) of the client area.
    /// </summary>
    public static (int Width, int Height) GetClientSize(IntPtr hWnd)
    {
        GetClientRect(hWnd, out RECT rect);
        return (rect.Width, rect.Height);
    }

    /// <summary>
    /// Checks whether a coordinate is within the client area of the given window.
    /// </summary>
    public static bool IsCoordinateInBounds(IntPtr hWnd, int x, int y)
    {
        GetClientRect(hWnd, out RECT rect);
        return x >= 0 && y >= 0 && x < rect.Width && y < rect.Height;
    }

    /// <summary>
    /// Generates a random coordinate within the client area of the given window.
    /// Applies a margin to avoid edges (10% inset on each side).
    /// </summary>
    public static ClickPoint GenerateRandomCoordinate(IntPtr hWnd)
    {
        GetClientRect(hWnd, out RECT rect);

        int marginX = Math.Max(10, rect.Width / 10);
        int marginY = Math.Max(10, rect.Height / 10);

        int minX = marginX;
        int maxX = rect.Width - marginX;
        int minY = marginY;
        int maxY = rect.Height - marginY;

        // Safety: if window is too small, use full area
        if (maxX <= minX) { minX = 0; maxX = Math.Max(1, rect.Width); }
        if (maxY <= minY) { minY = 0; maxY = Math.Max(1, rect.Height); }

        int x = Random.Shared.Next(minX, maxX);
        int y = Random.Shared.Next(minY, maxY);

        return new ClickPoint(x, y);
    }
}
