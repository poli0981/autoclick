using System.Text.Json;
using AutoClick.Core.Models;
using AutoClick.Core.Serialization;
using AutoClick.Core.Sessions;

namespace AutoClick.Core.Tests;

public class SessionTests
{
    [Fact]
    public void Parse_V1Session_MigratesSettingsProfilesAndGames()
    {
        var session = SessionFile.Parse(Fixtures.Read("v1", "session.autoclick-session"));

        Assert.Equal(SessionExport.CurrentSchemaVersion, session.SchemaVersion);
        Assert.Equal("1.3.3.0", session.AppVersion);

        Assert.Equal(AppTheme.HighContrast, session.Settings.Theme);
        Assert.Equal("ja", session.Settings.Language);
        Assert.False(session.Settings.AlwaysRunAsAdmin);

        Assert.Equal(2, session.Profiles.Count);
        var keystroke = session.Profiles[0].ClickPoints[1];
        Assert.Equal(ClickType.Keystroke, keystroke.ClickType);
        Assert.Equal(32, keystroke.VirtualKeyCode);

        var game = Assert.Single(session.Games);
        Assert.Equal("notepad", game.ProcessName);
        Assert.Equal(ClickType.RightClick, game.ClickPoints[0].ClickType);
        Assert.Equal(3, game.Timing.RandomMinSeconds);
        Assert.True(game.PixelGuard.Enabled);
        Assert.Equal(12, game.PixelGuard.Tolerance);
        Assert.Equal(ColorMismatchBehavior.SkipPoint, game.PixelGuard.MismatchBehavior);
        Assert.Equal(4000, game.PixelGuard.WaitTimeoutMs);
        Assert.True(game.IsCustomMode);
    }

    [Fact]
    public void Parse_V1Session_ReplacesPathTraversalProfileIds()
    {
        var session = SessionFile.Parse(Fixtures.Read("v1", "session.autoclick-session"));

        Assert.All(session.Profiles, p => Assert.True(GameProfile.IsValidId(p.Id), p.Id));
        Assert.Equal("a1b2c3d4", session.Profiles[0].Id);
    }

    [Fact]
    public void V2Session_RoundTrips()
    {
        var original = SessionFile.Parse(Fixtures.Read("v1", "session.autoclick-session")) with
        {
            ExportedAt = new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
            AppVersion = "2.0.0",
        };

        var json = AppJson.SerializeSession(original);
        var restored = SessionFile.Parse(json);

        Assert.Contains("\"schemaVersion\": 2", json, StringComparison.Ordinal);
        Assert.Equal(original.Settings, restored.Settings);
        Assert.Equal(original.Profiles.Count, restored.Profiles.Count);
        Assert.Equal(original.Profiles[0].ClickPoints, restored.Profiles[0].ClickPoints);
        Assert.Equal(original.Games[0].ClickPoints, restored.Games[0].ClickPoints);
        Assert.Equal(original.Games[0].PixelGuard, restored.Games[0].PixelGuard);
    }

    [Fact]
    public void Parse_NewerSchema_IsRejected()
    {
        Assert.Throws<NotSupportedException>(() => SessionFile.Parse("""{"schemaVersion": 3}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Parse_Malformed_Throws(string json)
    {
        Assert.ThrowsAny<JsonException>(() => SessionFile.Parse(json));
    }

    [Fact]
    public void Stats_CountConcurrentClicksExactly()
    {
        var stats = new SessionStats();

        Parallel.For(0, 10_000, i => stats.RecordClick(i % 10, 0));

        Assert.Equal(10_000, stats.Clicks);
        Assert.Equal(10, stats.GetHeatmap().Count);
        Assert.All(stats.GetHeatmap(), p => Assert.Equal(1000, p.Count));
    }

    [Fact]
    public void Stats_ResetClearsEverythingAndBumpsVersion()
    {
        var stats = new SessionStats();
        stats.RecordClick(1, 2);
        stats.RecordSkip();
        stats.RecordInterval(TimeSpan.FromSeconds(3));
        var version = stats.HeatmapVersion;

        stats.Reset();

        Assert.Equal(0, stats.Clicks);
        Assert.Equal(0, stats.Skipped);
        Assert.Equal(TimeSpan.Zero, stats.LastInterval);
        Assert.Empty(stats.GetHeatmap());
        Assert.NotEqual(version, stats.HeatmapVersion);
    }
}
