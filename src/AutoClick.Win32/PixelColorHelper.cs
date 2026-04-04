using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Win32;

/// <summary>
/// Helper for reading pixel colors and comparing them with tolerance.
/// Uses GetDC/GetPixel Win32 API to read from window client area.
/// </summary>
public static class PixelColorHelper
{
    private const uint CLR_INVALID = 0xFFFFFFFF;

    /// <summary>
    /// Reads the pixel color at client coordinates (x, y) from the specified window.
    /// Returns COLORREF (0x00BBGGRR) or CLR_INVALID on failure.
    /// </summary>
    public static uint ReadPixelColor(IntPtr hWnd, int x, int y)
    {
        var hdc = GetDC(hWnd);
        if (hdc == IntPtr.Zero) return CLR_INVALID;
        try
        {
            return GetPixel(hdc, x, y);
        }
        finally
        {
            ReleaseDC(hWnd, hdc);
        }
    }

    /// <summary>
    /// Compares two COLORREF values with per-channel RGB tolerance.
    /// Returns true if all channels are within tolerance.
    /// </summary>
    public static bool IsColorMatch(uint color1, uint color2, int tolerance)
    {
        if (color1 == CLR_INVALID || color2 == CLR_INVALID) return false;

        int r1 = (int)(color1 & 0xFF), g1 = (int)((color1 >> 8) & 0xFF), b1 = (int)((color1 >> 16) & 0xFF);
        int r2 = (int)(color2 & 0xFF), g2 = (int)((color2 >> 8) & 0xFF), b2 = (int)((color2 >> 16) & 0xFF);

        return Math.Abs(r1 - r2) <= tolerance &&
               Math.Abs(g1 - g2) <= tolerance &&
               Math.Abs(b1 - b2) <= tolerance;
    }

    /// <summary>
    /// Converts a COLORREF (0x00BBGGRR) to a hex string "#RRGGBB".
    /// </summary>
    public static string ColorToHex(uint colorRef)
    {
        if (colorRef == CLR_INVALID) return "N/A";
        int r = (int)(colorRef & 0xFF);
        int g = (int)((colorRef >> 8) & 0xFF);
        int b = (int)((colorRef >> 16) & 0xFF);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
