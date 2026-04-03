using System;
using AutoClick.Core.Enums;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Win32;

public static class InputSimulator
{
    private static readonly Random Jitter = Random.Shared;

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

        switch (clickType)
        {
            case ClickType.LeftClick:
                PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickType.DoubleClick:
                PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                PostMessage(hWnd, WM_LBUTTONDBLCLK, (IntPtr)MK_LBUTTON, lParam);
                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickType.RightClick:
                PostMessage(hWnd, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
                PostMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
                break;
        }
    }
}
