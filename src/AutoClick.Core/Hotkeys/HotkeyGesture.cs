namespace AutoClick.Core.Hotkeys;

/// <summary>Modifier flags; values equal the Win32 MOD_* constants used by RegisterHotKey.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Windows = 0x8,
}

public enum HotkeyValidation
{
    Valid,

    /// <summary>The string could not be parsed or names no main key.</summary>
    Invalid,

    /// <summary>F12 is reserved by Windows for debuggers.</summary>
    Reserved,

    /// <summary>Win-key combinations are reserved for the operating system.</summary>
    UsesWindowsKey,
}

/// <summary>
/// A global hotkey such as "Ctrl+Shift+F8". Strings use WPF key names, the format v1 stored.
/// </summary>
public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, int VirtualKey)
{
    private const int VkF12 = 0x7B;

    /// <summary>A hotkey without modifiers also stops the key from reaching the game.</summary>
    public bool HasModifiers => (Modifiers & ~HotkeyModifiers.Windows) != HotkeyModifiers.None;

    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        var virtualKey = 0;
        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (raw.ToUpperInvariant())
            {
                case "CTRL" or "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    continue;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    continue;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    continue;
                case "WIN" or "WINDOWS":
                    modifiers |= HotkeyModifiers.Windows;
                    continue;
            }

            if (virtualKey != 0 || !TryParseKey(raw, out virtualKey) || IsModifierKey(virtualKey))
            {
                return false;
            }
        }

        if (virtualKey == 0)
        {
            return false;
        }

        gesture = new HotkeyGesture(modifiers, virtualKey);
        return true;
    }

    public HotkeyValidation Validate()
    {
        if (VirtualKey is <= 0 or > 0xFE || IsModifierKey(VirtualKey))
        {
            return HotkeyValidation.Invalid;
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            return HotkeyValidation.UsesWindowsKey;
        }

        return VirtualKey == VkF12 ? HotkeyValidation.Reserved : HotkeyValidation.Valid;
    }

    /// <summary>Canonical text, e.g. "Ctrl+Alt+Shift+F8" (readable by v1 as well).</summary>
    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(VirtualKeyNames.GetDisplayName(VirtualKey));
        return string.Join('+', parts);
    }

    private static bool TryParseKey(string name, out int virtualKey)
    {
        // A bare digit means the top-row key ("1" → D1). v1 parsed it as a numeric enum value
        // and silently registered the wrong key.
        if (name.Length == 1 && char.IsAsciiDigit(name[0]))
        {
            virtualKey = name[0];
            return true;
        }

        if (name.StartsWith("VK", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out virtualKey))
        {
            return virtualKey is > 0 and <= 0xFE;
        }

        return VirtualKeyNames.TryGetVirtualKey(name, out virtualKey);
    }

    private static bool IsModifierKey(int vk) =>
        vk is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or (>= 0xA0 and <= 0xA5);
}
