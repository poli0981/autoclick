using System;
using System.Threading;
using AutoClick.Core.Enums;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Win32;

public static class InputSimulator
{
    private static readonly Random Jitter = Random.Shared;

    /// <summary>
    /// Minimum delay between mouse events (ms).
    /// Needed for game UIs that require hover before click (choices, dialogs, buttons).
    /// </summary>
    private const int MouseEventDelayMs = 15;

    public static void SendClick(IntPtr hWnd, int x, int y, ClickType clickType = ClickType.LeftClick, bool addJitter = true)
    {
        int finalX = x;
        int finalY = y;

        if (addJitter)
        {
            finalX += Jitter.Next(-2, 3);
            finalY += Jitter.Next(-2, 3);
            if (finalX < 0) finalX = 0;
            if (finalY < 0) finalY = 0;
        }

        IntPtr lParam = MakeLParam(finalX, finalY);

        // Step 1: Move mouse to position (required for game UI hover detection)
        PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Thread.Sleep(MouseEventDelayMs);

        // Step 2: Perform the click
        switch (clickType)
        {
            case ClickType.LeftClick:
                PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                Thread.Sleep(MouseEventDelayMs);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickType.DoubleClick:
                PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                Thread.Sleep(MouseEventDelayMs);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                Thread.Sleep(MouseEventDelayMs);
                PostMessage(hWnd, WM_LBUTTONDBLCLK, (IntPtr)MK_LBUTTON, lParam);
                Thread.Sleep(MouseEventDelayMs);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickType.RightClick:
                PostMessage(hWnd, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
                Thread.Sleep(MouseEventDelayMs);
                PostMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
                break;
        }
    }
}
