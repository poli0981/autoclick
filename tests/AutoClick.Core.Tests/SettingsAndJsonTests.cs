using AutoClick.Core.Models;
using AutoClick.Core.Serialization;
using AutoClick.Core.Settings;

namespace AutoClick.Core.Tests;

public class SettingsAndJsonTests
{
    [Fact]
    public void Settings_RoundTripThroughJson_IsUnchanged()
    {
        var settings = new AppSettings
        {
            SettingsMode = SettingsMode.Custom,
            DefaultTiming = new ClickTiming { Mode = ClickMode.Fixed, FixedIntervalSeconds = 7.5 },
            Theme = AppTheme.HighContrast,
            Language = "ko",
            PixelGuard = new PixelGuardOptions { Enabled = true, Tolerance = 3, MismatchBehavior = ColorMismatchBehavior.SkipPoint },
            Hotkeys = new HotkeySettings { PauseResume = "Ctrl+Alt+P" },
            AlwaysRunAsAdmin = true,
            MainWindow = new WindowPlacement { Left = 10, Top = 20, Width = 1200, Height = 800, IsMaximized = true },
        }.Normalize();

        var json = AppJson.SerializeSettings(settings);
        var restored = AppJson.DeserializeSettings(json)!.Normalize();

        Assert.Equal(settings, restored);
        Assert.Contains("\"theme\": \"HighContrast\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_MissingProperties_UseDefaults()
    {
        var settings = AppJson.DeserializeSettings("""{"schemaVersion": 2, "language": "vi"}""")!.Normalize();

        Assert.Equal("vi", settings.Language);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(new HotkeySettings(), settings.Hotkeys);
        Assert.False(settings.AlwaysRunAsAdmin);
    }

    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("VI", "vi")]
    [InlineData("zh-Hans", "zh")]
    [InlineData("ja-JP", "ja")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void NormalizeLanguage_MapsToSupportedLanguage(string? input, string expected)
    {
        Assert.Equal(expected, AppSettings.NormalizeLanguage(input));
    }

    [Fact]
    public void Normalize_DropsUnusableWindowPlacement()
    {
        var settings = new AppSettings { MainWindow = new WindowPlacement { Width = 10, Height = 10 } }.Normalize();

        Assert.Null(settings.MainWindow);
    }

    [Fact]
    public void V1Profile_Deserializes_WithKeystrokeKeyPreserved()
    {
        var profile = AppJson.DeserializeProfile(Fixtures.Read("v1", "profiles", "a1b2c3d4.json"))!.Normalize();

        Assert.Equal("a1b2c3d4", profile.Id);
        Assert.Equal("Fixture Profile", profile.Name);
        Assert.Equal(3, profile.ClickPoints.Count);
        Assert.Equal(ClickType.RightClick, profile.ClickPoints[1].ClickType);
        Assert.Equal(3355443u, profile.ClickPoints[1].ReferenceColor);
        Assert.Equal(ClickType.Keystroke, profile.ClickPoints[2].ClickType);
        Assert.Equal(32, profile.ClickPoints[2].VirtualKeyCode);
        Assert.Equal(ClickMode.Fixed, profile.ClickSettings.Mode);
        Assert.Equal(250, profile.SequenceDelayMs);
    }

    [Fact]
    public void Profile_Serializes_InTheV1Format()
    {
        var profile = AppJson.DeserializeProfile(Fixtures.Read("v1", "profiles", "a1b2c3d4.json"))!.Normalize();

        var json = AppJson.SerializeProfile(profile);

        // v1 reads profiles without a string-enum converter: enums must stay numeric.
        Assert.Contains("\"clickType\": 3", json, StringComparison.Ordinal);
        Assert.Contains("\"virtualKeyCode\": 32", json, StringComparison.Ordinal);
        Assert.Contains("\"mode\": 1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("isKeystroke", json, StringComparison.Ordinal);

        var restored = AppJson.DeserializeProfile(json)!.Normalize();
        Assert.Equal(profile.ClickPoints, restored.ClickPoints);
        Assert.Equal(profile with { ClickPoints = [] }, restored with { ClickPoints = [] });
    }

    [Theory]
    [InlineData("a1b2c3d4", true)]
    [InlineData("0123456789abcdef0123456789abcdef", true)]
    [InlineData("a1b2c3d", false)]
    [InlineData("A1B2C3D4", false)]
    [InlineData("..\\..\\evil", false)]
    [InlineData("a1b2/c3d4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidId_OnlyAcceptsShortLowercaseAlphanumerics(string? id, bool expected)
    {
        Assert.Equal(expected, GameProfile.IsValidId(id));
    }

    [Fact]
    public void ProfileNormalize_ReplacesUnsafeIdAndTrimsName()
    {
        var profile = new GameProfile { Id = "..\\..\\Startup\\evil", Name = "  " + new string('x', 150) }.Normalize();

        Assert.True(GameProfile.IsValidId(profile.Id));
        Assert.Equal(GameProfile.MaxNameLength, profile.Name.Length);
    }

    [Fact]
    public void ClickPointNormalize_KeystrokeHasNoCoordinates()
    {
        var point = new ClickPoint { X = 50, Y = 60, ClickType = ClickType.Keystroke, VirtualKeyCode = 0x41 }.Normalize();

        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
        Assert.Equal(0x41, point.VirtualKeyCode);
    }

    [Fact]
    public void ClickPointNormalize_ClampsValues()
    {
        var point = new ClickPoint { X = -5, Y = 99_999, DelayAfterMs = -1, VirtualKeyCode = 13, ClickType = (ClickType)42 }.Normalize();

        Assert.Equal(0, point.X);
        Assert.Equal(Limits.MaxCoordinate, point.Y);
        Assert.Equal(0, point.DelayAfterMs);
        Assert.Equal(ClickType.LeftClick, point.ClickType);
        Assert.Equal(0, point.VirtualKeyCode);
    }
}
