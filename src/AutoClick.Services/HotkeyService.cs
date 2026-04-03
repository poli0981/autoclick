using System;
using System.Collections.Generic;
using System.Windows.Input;
using AutoClick.Core.Interfaces;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Services;

public class HotkeyService : IHotkeyService
{
    private readonly Dictionary<int, string> _registeredHotkeys = new();
    private IntPtr _windowHandle;
    private int _nextId = 9000;

    public event Action<string>? HotkeyPressed;

    public void Register(string id, string keyCombo, IntPtr windowHandle)
    {
        _windowHandle = windowHandle;

        ParseKeyCombo(keyCombo, out uint modifiers, out uint vk);

        int hotkeyId = _nextId++;
        if (RegisterHotKey(windowHandle, hotkeyId, modifiers | MOD_NOREPEAT, vk))
        {
            _registeredHotkeys[hotkeyId] = id;
        }
    }

    public void HandleHotkeyMessage(int hotkeyId)
    {
        if (_registeredHotkeys.TryGetValue(hotkeyId, out var id))
        {
            HotkeyPressed?.Invoke(id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var kvp in _registeredHotkeys)
        {
            UnregisterHotKey(_windowHandle, kvp.Key);
        }
        _registeredHotkeys.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
    }

    private static void ParseKeyCombo(string combo, out uint modifiers, out uint vk)
    {
        modifiers = MOD_NONE;
        vk = 0;

        var parts = combo.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL": modifiers |= MOD_CONTROL; break;
                case "ALT": modifiers |= MOD_ALT; break;
                case "SHIFT": modifiers |= MOD_SHIFT; break;
                case "WIN": modifiers |= MOD_WIN; break;
                default:
                    if (Enum.TryParse<Key>(part, true, out var key))
                    {
                        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                    }
                    break;
            }
        }
    }
}
