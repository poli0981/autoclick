using System.Windows.Input;
using AutoClick.Core.Hotkeys;
using AutoClick.UI.Localization;

namespace AutoClick.UI.Views.Dialogs;

/// <summary>Records one key combination for a global hotkey.</summary>
public partial class HotkeyCaptureView
{
    private static readonly (HotkeyModifiers Flag, string Name)[] ModifierNames =
    [
        (HotkeyModifiers.Control, "Ctrl"),
        (HotkeyModifiers.Alt, "Alt"),
        (HotkeyModifiers.Shift, "Shift"),
        (HotkeyModifiers.Windows, "Win"),
    ];

    public HotkeyCaptureView()
    {
        InitializeComponent();
    }

    /// <summary>The recorded, valid combination (null until one is pressed).</summary>
    public HotkeyGesture? Gesture { get; private set; }

    /// <summary>Raised whenever <see cref="Gesture"/> changes.</summary>
    public event EventHandler? GestureChanged;

    /// <summary>Handles a key press from the dialog. Escape and Enter are left to the dialog.</summary>
    public void HandleKey(KeyEventArgs e)
    {
        var key = e.Key switch
        {
            Key.System => e.SystemKey,
            Key.ImeProcessed => e.ImeProcessedKey,
            _ => e.Key,
        };

        var modifiers = ToModifiers(Keyboard.Modifiers);
        if (modifiers == HotkeyModifiers.None && key is Key.Escape or Key.Enter or Key.Tab)
        {
            return;
        }

        e.Handled = true;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            var held = ModifierNames.Where(m => modifiers.HasFlag(m.Flag)).Select(m => m.Name + "+");
            GestureText.Text = string.Concat(held) + "…";
            return;
        }

        var gesture = new HotkeyGesture(modifiers, KeyInterop.VirtualKeyFromKey(key));
        GestureText.Text = gesture.ToString();
        var validation = gesture.Validate();
        ValidationText.Text = validation switch
        {
            HotkeyValidation.Valid when !gesture.HasModifiers => L.Get("Hotkey_NoModifierHint"),
            HotkeyValidation.Valid => string.Empty,
            HotkeyValidation.Reserved => L.Get("Hotkey_Reserved"),
            HotkeyValidation.UsesWindowsKey => L.Get("Hotkey_WindowsKey"),
            _ => L.Get("Hotkey_Invalid"),
        };

        Gesture = validation == HotkeyValidation.Valid ? gesture : null;
        GestureChanged?.Invoke(this, EventArgs.Empty);
    }

    private static HotkeyModifiers ToModifiers(ModifierKeys keys)
    {
        var modifiers = HotkeyModifiers.None;
        if (keys.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (keys.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (keys.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (keys.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }
}
