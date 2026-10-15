using System.Text.Json;
using AutoClick.Core.Models;
using AutoClick.Core.Settings;

namespace AutoClick.Core.Tests;

public class LegacySettingsReaderTests
{
    [Fact]
    public void FromJson_V1Settings_MapsEveryField()
    {
        var settings = LegacySettingsReader.FromJson(Fixtures.Read("v1", "settings.json"), isUpgrade: true);

        Assert.Equal(SettingsMode.Custom, settings.SettingsMode);
        Assert.Equal(ClickMode.Fixed, settings.DefaultTiming.Mode);
        Assert.Equal(3.5, settings.DefaultTiming.FixedIntervalSeconds);
        Assert.Equal(2, settings.DefaultTiming.RandomMinSeconds);
        Assert.Equal(30, settings.DefaultTiming.RandomMaxSeconds);
        Assert.Equal(7, settings.MaxGamesInQueue);
        Assert.False(settings.ShowRealTimeLogs);
        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.Equal("vi", settings.Language);
        Assert.Equal(ExitBehavior.StopAndExit, settings.ExitBehavior);
        Assert.False(settings.AutoUpdate);
        Assert.False(settings.SoundNotifications);
        Assert.True(settings.ShowGameExitNotification);
        Assert.True(settings.MinimizeOnStartAll);
        Assert.True(settings.PixelGuard.Enabled);
        Assert.Equal(15, settings.PixelGuard.Tolerance);
        Assert.Equal(ColorMismatchBehavior.WaitUntilMatch, settings.PixelGuard.MismatchBehavior);
        Assert.Equal(7000, settings.PixelGuard.WaitTimeoutMs);
        Assert.Equal("Ctrl+F6", settings.Hotkeys.PauseResume);
        Assert.Equal("F7", settings.Hotkeys.StopAll);
        Assert.Equal("Ctrl+Shift+F8", settings.Hotkeys.StartAll);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void FromJson_Upgrade_KeepsAdministratorModeAndShowsNotice()
    {
        var settings = LegacySettingsReader.FromJson(Fixtures.Read("v1", "settings.json"), isUpgrade: true);

        Assert.True(settings.AlwaysRunAsAdmin);
        Assert.True(settings.ShowAdminModeNotice);
    }

    [Fact]
    public void FromJson_NotAnUpgrade_DoesNotForceAdministratorMode()
    {
        var settings = LegacySettingsReader.FromJson(Fixtures.Read("v1", "settings.json"), isUpgrade: false);

        Assert.False(settings.AlwaysRunAsAdmin);
        Assert.False(settings.ShowAdminModeNotice);
    }

    [Theory]
    [InlineData("""{"darkMode": false}""", AppTheme.Light)]
    [InlineData("""{"darkMode": true}""", AppTheme.Dark)]
    [InlineData("""{}""", AppTheme.Dark)]
    [InlineData("""{"theme": "HighContrast", "darkMode": false}""", AppTheme.HighContrast)]
    [InlineData("""{"theme": 1}""", AppTheme.Light)]
    [InlineData("""{"theme": "Purple"}""", AppTheme.Dark)]
    public void FromJson_Theme_HandlesLegacyAndInvalidValues(string json, AppTheme expected)
    {
        Assert.Equal(expected, LegacySettingsReader.FromJson(json, isUpgrade: false).Theme);
    }

    [Fact]
    public void FromJson_LegacyDarkModeFile_MapsToLight()
    {
        var settings = LegacySettingsReader.FromJson(Fixtures.Read("v1", "settings-legacy-darkmode.json"), isUpgrade: true);

        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.Equal(ClickMode.Random, settings.DefaultTiming.Mode);
    }

    [Fact]
    public void FromJson_OutOfRangeValues_AreClampedAndRepaired()
    {
        const string json = """
            {
              "maxGamesInQueue": 999,
              "randomMin": 90,
              "randomMax": 5,
              "defaultFixedInterval": -3,
              "colorTolerance": 400,
              "colorWaitTimeoutMs": 1,
              "language": "fr",
              "hotkeys": null,
              "settingsMode": "Nope",
              "exitBehavior": 42
            }
            """;

        var settings = LegacySettingsReader.FromJson(json, isUpgrade: false);

        Assert.Equal(Limits.MaxGamesInQueue, settings.MaxGamesInQueue);
        Assert.Equal(5, settings.DefaultTiming.RandomMinSeconds);
        Assert.Equal(90, settings.DefaultTiming.RandomMaxSeconds);
        Assert.Equal(Limits.MinIntervalSeconds, settings.DefaultTiming.FixedIntervalSeconds);
        Assert.Equal(Limits.MaxColorTolerance, settings.PixelGuard.Tolerance);
        Assert.Equal(Limits.MinColorWaitTimeoutMs, settings.PixelGuard.WaitTimeoutMs);
        Assert.Equal("en", settings.Language);
        Assert.Equal(new HotkeySettings(), settings.Hotkeys);
        Assert.Equal(SettingsMode.Global, settings.SettingsMode);
        Assert.Equal(ExitBehavior.MinimizeToTray, settings.ExitBehavior);
    }

    [Fact]
    public void FromJson_InvalidHotkey_FallsBackToDefaultForThatAction()
    {
        const string json = """{"hotkeys": {"pauseResume": "F12", "stopAll": "Ctrl+", "startAll": "1"}}""";

        var hotkeys = LegacySettingsReader.FromJson(json, isUpgrade: false).Hotkeys;

        Assert.Equal("F6", hotkeys.PauseResume);
        Assert.Equal("F7", hotkeys.StopAll);
        Assert.Equal("D1", hotkeys.StartAll);
    }

    [Fact]
    public void FromJson_NotAnObject_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => LegacySettingsReader.FromJson("[1, 2]", isUpgrade: false));
    }
}
