using AutoClick.Core.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Xps;

namespace AutoClick.Win32;

/// <summary>Reads a pixel from a window's client area for the pixel color guard.</summary>
/// <remarks>
/// GetPixel on the window DC is cheap and works for GDI-rendered windows even when they are
/// covered. Many DirectX/flip-model games return black through it, so a black or failed read
/// is retried with PrintWindow(PW_RENDERFULLCONTENT), which asks DWM for the real content.
/// </remarks>
public sealed class Win32PixelReader : IPixelReader
{
    /// <summary>Not in the CsWin32 metadata enum: render DirectComposition/DirectX content too.</summary>
    private const uint PwRenderFullContent = 0x2;

    public PixelSample Read(nint window, int x, int y)
    {
        var hwnd = (HWND)window;
        using var scope = DpiScope.ForWindow(window);

        var direct = ReadFromWindowDc(hwnd, x, y);
        if (direct.IsValid && direct.Color != 0)
        {
            return direct;
        }

        var rendered = ReadWithPrintWindow(hwnd, x, y);
        return rendered.IsValid ? rendered : direct;
    }

    private static PixelSample ReadFromWindowDc(HWND hwnd, int x, int y)
    {
        var dc = PInvoke.GetDC(hwnd);
        if (dc.IsNull)
        {
            return PixelSample.Invalid;
        }

        try
        {
            var color = PInvoke.GetPixel(dc, x, y);
            return color.Value == PInvoke.CLR_INVALID ? PixelSample.Invalid : new PixelSample(color.Value, true);
        }
        finally
        {
            _ = PInvoke.ReleaseDC(hwnd, dc);
        }
    }

    private static PixelSample ReadWithPrintWindow(HWND hwnd, int x, int y)
    {
        if (!PInvoke.GetClientRect(hwnd, out var client))
        {
            return PixelSample.Invalid;
        }

        var width = client.right - client.left;
        var height = client.bottom - client.top;
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return PixelSample.Invalid;
        }

        var windowDc = PInvoke.GetDC(hwnd);
        if (windowDc.IsNull)
        {
            return PixelSample.Invalid;
        }

        var memoryDc = PInvoke.CreateCompatibleDC(windowDc);
        var bitmap = PInvoke.CreateCompatibleBitmap(windowDc, width, height);
        var previous = PInvoke.SelectObject(memoryDc, bitmap);
        try
        {
            var flags = (PRINT_WINDOW_FLAGS)((uint)PRINT_WINDOW_FLAGS.PW_CLIENTONLY | PwRenderFullContent);
            if (!PInvoke.PrintWindow(hwnd, memoryDc, flags))
            {
                return PixelSample.Invalid;
            }

            var color = PInvoke.GetPixel(memoryDc, x, y);
            return color.Value == PInvoke.CLR_INVALID ? PixelSample.Invalid : new PixelSample(color.Value, true);
        }
        finally
        {
            PInvoke.SelectObject(memoryDc, previous);
            PInvoke.DeleteObject(bitmap);
            PInvoke.DeleteDC(memoryDc);
            _ = PInvoke.ReleaseDC(hwnd, windowDc);
        }
    }
}
