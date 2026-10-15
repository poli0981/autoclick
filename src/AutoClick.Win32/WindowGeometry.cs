using System.Drawing;
using AutoClick.Core.Dpi;
using AutoClick.Win32.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AutoClick.Win32;

/// <summary>Window geometry helpers for the coordinate picker and the heatmap overlay.</summary>
public static class WindowGeometry
{
    /// <summary>Client area of <paramref name="window"/> on screen, in physical pixels.</summary>
    public static bool TryGetPhysicalClientRect(nint window, out PixelRect rect)
    {
        using var scope = DpiScope.PerMonitorV2();
        var hwnd = (HWND)window;
        var origin = new Point(0, 0);
        if (!PInvoke.GetClientRect(hwnd, out var client) || !PInvoke.ClientToScreen(hwnd, ref origin))
        {
            rect = default;
            return false;
        }

        rect = new PixelRect(origin.X, origin.Y, client.right - client.left, client.bottom - client.top);
        return !rect.IsEmpty;
    }

    /// <summary>
    /// Cursor position in <paramref name="window"/>'s own client coordinates (the space
    /// PostMessage clicks use), even for DPI-unaware games on scaled monitors.
    /// </summary>
    public static bool TryGetCursorClientPoint(nint window, out PixelPoint point)
    {
        using var scope = DpiScope.ForWindow(window);
        var hwnd = (HWND)window;
        if (PInvoke.GetCursorPos(out var cursor) && PInvoke.ScreenToClient(hwnd, ref cursor)
            && PInvoke.GetClientRect(hwnd, out var client)
            && DpiMath.IsInsideClient(cursor.X, cursor.Y, client.right - client.left, client.bottom - client.top))
        {
            point = new PixelPoint(cursor.X, cursor.Y);
            return true;
        }

        point = default;
        return false;
    }

    public static uint GetDpi(nint window) => PInvoke.GetDpiForWindow((HWND)window);

    /// <summary>True when <paramref name="window"/> or one of its owned dialogs is in the foreground.</summary>
    public static bool IsForeground(nint window)
    {
        var foreground = PInvoke.GetForegroundWindow();
        return foreground == (HWND)window
            || PInvoke.GetAncestor(foreground, GET_ANCESTOR_FLAGS.GA_ROOTOWNER) == (HWND)window;
    }

    public static bool IsMinimized(nint window) => PInvoke.IsIconic((HWND)window);

    /// <summary>Makes an overlay window click-through, hidden from Alt+Tab and never activated.</summary>
    public static void MakeClickThrough(nint window)
    {
        var style = WindowLong.Get(window, WindowLong.GwlExStyle);
        WindowLong.Set(window, WindowLong.GwlExStyle,
            style | WindowLong.WsExTransparent | WindowLong.WsExToolWindow | WindowLong.WsExNoActivate | WindowLong.WsExLayered);
    }

    /// <summary>Moves/resizes a window in physical pixels, optionally re-asserting top-most.</summary>
    public static void SetBounds(nint window, PixelRect rect, bool topmost)
    {
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
        if (!topmost)
        {
            flags |= SET_WINDOW_POS_FLAGS.SWP_NOZORDER;
        }

        PInvoke.SetWindowPos((HWND)window, topmost ? HWND.HWND_TOPMOST : HWND.Null, rect.Left, rect.Top, rect.Width, rect.Height, flags);
    }

    public static void Show(nint window, bool visible) =>
        PInvoke.ShowWindow((HWND)window, visible ? SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE : SHOW_WINDOW_CMD.SW_HIDE);

    public static void BringToFront(nint window) => PInvoke.SetForegroundWindow((HWND)window);
}
