using AutoClick.Core.Hotkeys;

namespace AutoClick.Core.Tests;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("F6", HotkeyModifiers.None, 0x75)]
    [InlineData("Ctrl+F6", HotkeyModifiers.Control, 0x75)]
    [InlineData("Ctrl+Shift+F8", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x77)]
    [InlineData(" ctrl + alt + d1 ", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x31)]
    [InlineData("Control+Return", HotkeyModifiers.Control, 0x0D)]
    [InlineData("Alt+Enter", HotkeyModifiers.Alt, 0x0D)]
    [InlineData("Shift+PageUp", HotkeyModifiers.Shift, 0x21)]
    [InlineData("Shift+Prior", HotkeyModifiers.Shift, 0x21)]
    [InlineData("OemTilde", HotkeyModifiers.None, 0xC0)]
    [InlineData("NumPad5", HotkeyModifiers.None, 0x65)]
    [InlineData("1", HotkeyModifiers.None, 0x31)]
    [InlineData("Ctrl+VK7B", HotkeyModifiers.Control, 0x7B)]
    public void TryParse_ValidText_ReturnsGesture(string text, HotkeyModifiers modifiers, int virtualKey)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(new HotkeyGesture(modifiers, virtualKey), gesture);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Shift")]
    [InlineData("LeftCtrl")]
    [InlineData("F6+F7")]
    [InlineData("NotAKey")]
    [InlineData("12")]
    public void TryParse_InvalidText_Fails(string? text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out _));
    }

    [Theory]
    [InlineData("F6", HotkeyValidation.Valid)]
    [InlineData("F12", HotkeyValidation.Reserved)]
    [InlineData("Win+A", HotkeyValidation.UsesWindowsKey)]
    public void Validate_FlagsReservedCombinations(string text, HotkeyValidation expected)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(expected, gesture.Validate());
    }

    [Theory]
    [InlineData("ctrl+shift+f8", "Ctrl+Shift+F8")]
    [InlineData("Shift+Alt+Ctrl+A", "Ctrl+Alt+Shift+A")]
    [InlineData("Enter", "Return")]
    [InlineData("OemTilde", "Oem3")]
    [InlineData("1", "D1")]
    public void ToString_IsCanonicalWpfName(string text, string expected)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(expected, gesture.ToString());
    }

    [Fact]
    public void HasModifiers_IgnoresWindowsKey()
    {
        Assert.False(new HotkeyGesture(HotkeyModifiers.None, 0x75).HasModifiers);
        Assert.True(new HotkeyGesture(HotkeyModifiers.Alt, 0x75).HasModifiers);
        Assert.False(new HotkeyGesture(HotkeyModifiers.Windows, 0x75).HasModifiers);
    }

    [Fact]
    public void EveryKnownKeyName_RoundTripsThroughItsVirtualKey()
    {
        foreach (var (name, vk) in VirtualKeyNames.All)
        {
            Assert.True(VirtualKeyNames.TryGetVirtualKey(name, out var parsed), name);
            Assert.Equal(vk, parsed);
            Assert.NotNull(VirtualKeyNames.GetName(vk));
        }
    }
}
