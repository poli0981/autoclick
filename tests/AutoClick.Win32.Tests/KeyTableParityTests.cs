using System.Windows.Input;
using AutoClick.Core.Hotkeys;

namespace AutoClick.Win32.Tests;

/// <summary>
/// v1 stored hotkeys as WPF <see cref="Key"/> names. Core keeps its own table to avoid a WPF
/// dependency; these tests pin it to WPF so both stay identical.
/// </summary>
public class KeyTableParityTests
{
    [Fact]
    public void EveryWpfKeyName_MapsToTheSameVirtualKey()
    {
        foreach (var name in Enum.GetNames<Key>())
        {
            var expected = KeyInterop.VirtualKeyFromKey(Enum.Parse<Key>(name));
            if (expected == 0)
            {
                continue;
            }

            Assert.True(VirtualKeyNames.TryGetVirtualKey(name, out var actual), $"Missing key name '{name}'.");
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void EveryNameWeWrite_IsReadBackByWpfAsTheSameKey()
    {
        foreach (var (_, vk) in VirtualKeyNames.All)
        {
            var name = VirtualKeyNames.GetName(vk);
            Assert.NotNull(name);
            Assert.True(Enum.TryParse<Key>(name, out var key), $"WPF cannot parse '{name}'.");
            Assert.Equal(vk, KeyInterop.VirtualKeyFromKey(key));
        }
    }

    [Theory]
    [InlineData(Key.F6, ModifierKeys.None, "F6")]
    [InlineData(Key.F8, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+F8")]
    [InlineData(Key.D1, ModifierKeys.Alt, "Alt+D1")]
    [InlineData(Key.Enter, ModifierKeys.Control, "Ctrl+Return")]
    public void V1CaptureFormat_ParsesToTheSameGesture(Key key, ModifierKeys modifiers, string v1Text)
    {
        Assert.True(HotkeyGesture.TryParse(v1Text, out var gesture));
        Assert.Equal(KeyInterop.VirtualKeyFromKey(key), gesture.VirtualKey);
        Assert.Equal((int)modifiers, (int)gesture.Modifiers);
    }
}
