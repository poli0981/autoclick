using System.Runtime.InteropServices;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace AutoClick.Win32;

/// <summary>
/// Posts mouse and keyboard messages to a window (PostMessage), so the real cursor and the
/// focused window are left alone. Every step reports failures instead of ignoring them.
/// </summary>
public sealed class Win32InputSender(TimeProvider timeProvider) : IInputSender
{
    /// <summary>Pause between messages; many game UIs need a hover before a click registers.</summary>
    internal static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(15);

    private const int ErrorAccessDenied = 5;
    private const int ErrorInvalidWindowHandle = 1400;
    private const int ErrorNotEnoughQuota = 1816;

    public async ValueTask<InputResult> ClickAsync(nint window, int x, int y, ClickType type, CancellationToken cancellationToken)
    {
        var hwnd = (HWND)window;
        var position = MakeLParam(x, y);

        var result = Post(hwnd, PInvoke.WM_MOUSEMOVE, 0, position);
        if (result != InputResult.Ok)
        {
            return result;
        }

        await Task.Delay(StepDelay, timeProvider, cancellationToken).ConfigureAwait(false);

        return type switch
        {
            ClickType.RightClick => await PressAsync(hwnd, PInvoke.WM_RBUTTONDOWN, PInvoke.WM_RBUTTONUP, (nuint)MODIFIERKEYS_FLAGS.MK_RBUTTON, position, cancellationToken).ConfigureAwait(false),
            ClickType.DoubleClick => await DoubleClickAsync(hwnd, position, cancellationToken).ConfigureAwait(false),
            _ => await PressAsync(hwnd, PInvoke.WM_LBUTTONDOWN, PInvoke.WM_LBUTTONUP, (nuint)MODIFIERKEYS_FLAGS.MK_LBUTTON, position, cancellationToken).ConfigureAwait(false),
        };
    }

    public async ValueTask<InputResult> KeystrokeAsync(nint window, int virtualKey, CancellationToken cancellationToken)
    {
        if (virtualKey is <= 0 or > 0xFE)
        {
            return InputResult.Failed;
        }

        var hwnd = (HWND)window;
        var (down, up) = KeyLParams(virtualKey);
        var result = Post(hwnd, PInvoke.WM_KEYDOWN, (nuint)virtualKey, down);
        if (result != InputResult.Ok)
        {
            return result;
        }

        await Task.Delay(StepDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        return Post(hwnd, PInvoke.WM_KEYUP, (nuint)virtualKey, up);
    }

    private async ValueTask<InputResult> PressAsync(HWND hwnd, uint downMessage, uint upMessage, nuint buttonFlag, nint position, CancellationToken cancellationToken)
    {
        var result = Post(hwnd, downMessage, buttonFlag, position);
        if (result != InputResult.Ok)
        {
            return result;
        }

        await Task.Delay(StepDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        return Post(hwnd, upMessage, 0, position);
    }

    /// <summary>The sequence Windows itself produces: down, up, double-click, up.</summary>
    private async ValueTask<InputResult> DoubleClickAsync(HWND hwnd, nint position, CancellationToken cancellationToken)
    {
        var flag = (nuint)MODIFIERKEYS_FLAGS.MK_LBUTTON;
        var result = await PressAsync(hwnd, PInvoke.WM_LBUTTONDOWN, PInvoke.WM_LBUTTONUP, flag, position, cancellationToken).ConfigureAwait(false);
        if (result != InputResult.Ok)
        {
            return result;
        }

        await Task.Delay(StepDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        return await PressAsync(hwnd, PInvoke.WM_LBUTTONDBLCLK, PInvoke.WM_LBUTTONUP, flag, position, cancellationToken).ConfigureAwait(false);
    }

    private static InputResult Post(HWND hwnd, uint message, nuint wParam, nint lParam)
    {
        if (PInvoke.PostMessage(hwnd, message, (WPARAM)wParam, (LPARAM)lParam))
        {
            return InputResult.Ok;
        }

        return Marshal.GetLastPInvokeError() switch
        {
            // UIPI: a lower-integrity process cannot post input to an elevated window.
            ErrorAccessDenied => InputResult.AccessDenied,
            ErrorInvalidWindowHandle => InputResult.WindowGone,
            ErrorNotEnoughQuota => InputResult.TargetNotResponding,
            _ => InputResult.Failed,
        };
    }

    /// <summary>MAKELPARAM(x, y) for client coordinates.</summary>
    internal static nint MakeLParam(int x, int y) => ((y & 0xFFFF) << 16) | (x & 0xFFFF);

    /// <summary>WM_KEYDOWN/WM_KEYUP lParam with repeat count, scan code and extended-key flag.</summary>
    internal static (nint Down, nint Up) KeyLParams(int virtualKey)
    {
        var scanCode = PInvoke.MapVirtualKey((uint)virtualKey, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC) & 0xFF;
        var extended = IsExtendedKey(virtualKey) ? 1u << 24 : 0u;
        var down = 1u | (scanCode << 16) | extended;
        var up = down | (1u << 30) | (1u << 31);
        return ((nint)(int)down, (nint)(int)up);
    }

    private static bool IsExtendedKey(int vk) => vk is
        0x21 or 0x22 or 0x23 or 0x24 // PageUp, PageDown, End, Home
        or 0x25 or 0x26 or 0x27 or 0x28 // arrows
        or 0x2C or 0x2D or 0x2E // PrintScreen, Insert, Delete
        or 0x5B or 0x5C or 0x5D // Windows keys, Apps
        or 0x6F // Divide
        or 0x90 // NumLock
        or 0xA3 or 0xA5; // right Ctrl, right Alt
}
