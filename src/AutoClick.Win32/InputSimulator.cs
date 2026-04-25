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

    /// <summary>
    /// Sends a single keystroke (WM_KEYDOWN + WM_KEYUP) to the target window via PostMessage.
    /// Note: many DirectInput / anti-cheat games ignore PostMessage keystrokes — use for
    /// windowed games and standard Win32 controls.
    /// </summary>
    public static void SendKeystroke(IntPtr hWnd, int virtualKeyCode)
    {
        if (virtualKeyCode <= 0) return;

        // lParam encoding for WM_KEYDOWN: bits 0-15 = repeat count, 16-23 = scan code,
        // 24 = extended, 29 = context, 30 = previous state, 31 = transition.
        // For a synthetic single press, repeat=1 and the rest 0 is the conventional value.
        var downLParam = (IntPtr)0x00000001;
        // For WM_KEYUP: previous state = 1 (bit 30), transition = 1 (bit 31).
        var upLParam = unchecked((IntPtr)0xC0000001);

        PostMessage(hWnd, WM_KEYDOWN, (IntPtr)virtualKeyCode, downLParam);
        Thread.Sleep(MouseEventDelayMs);
        PostMessage(hWnd, WM_KEYUP, (IntPtr)virtualKeyCode, upLParam);
    }
}
