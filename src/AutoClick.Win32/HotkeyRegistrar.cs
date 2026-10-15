using System.Runtime.InteropServices;
using AutoClick.Core.Hotkeys;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace AutoClick.Win32;

public enum HotkeyRegistrationResult
{
    Registered,

    /// <summary>Another application already owns this combination.</summary>
    InUse,

    /// <summary>The gesture is invalid or reserved (F12, Win-key combinations).</summary>
    NotAllowed,
    Failed,
}

/// <summary>RegisterHotKey/UnregisterHotKey for a window owned by the calling thread.</summary>
public static class HotkeyRegistrar
{
    public const int WmHotkey = 0x0312;
    private const int ErrorHotkeyAlreadyRegistered = 1409;

    public static HotkeyRegistrationResult Register(nint window, int id, HotkeyGesture gesture)
    {
        if (gesture.Validate() != HotkeyValidation.Valid)
        {
            return HotkeyRegistrationResult.NotAllowed;
        }

        // MOD_NOREPEAT: holding the key down triggers the action once.
        var modifiers = (HOT_KEY_MODIFIERS)gesture.Modifiers | HOT_KEY_MODIFIERS.MOD_NOREPEAT;
        if (PInvoke.RegisterHotKey((HWND)window, id, modifiers, (uint)gesture.VirtualKey))
        {
            return HotkeyRegistrationResult.Registered;
        }

        return Marshal.GetLastPInvokeError() == ErrorHotkeyAlreadyRegistered
            ? HotkeyRegistrationResult.InUse
            : HotkeyRegistrationResult.Failed;
    }

    public static void Unregister(nint window, int id) => PInvoke.UnregisterHotKey((HWND)window, id);
}
