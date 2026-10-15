using System.Text.Json;
using AutoClick.Core.Models;
using AutoClick.Core.Serialization;
using AutoClick.Core.Settings;

namespace AutoClick.Core.Sessions;

/// <summary>Reads .autoclick-session files of any supported schema (v1 or v2).</summary>
public static class SessionFile
{
    /// <exception cref="JsonException">The file is not valid JSON or has the wrong shape.</exception>
    /// <exception cref="NotSupportedException">The file was written by a newer AutoClick.</exception>
    public static SessionExport Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A session file must contain a JSON object.");
        }

        var schema = LegacySettingsReader.GetInt(root, "schemaVersion", 1);
        return schema switch
        {
            1 => FromV1(root),
            SessionExport.CurrentSchemaVersion =>
                (AppJson.DeserializeSession(json) ?? throw new JsonException("Empty session file.")).Normalize(),
            _ => throw new NotSupportedException($"Session schema version {schema} is not supported by this version of AutoClick."),
        };
    }

    /// <summary>v1 sessions: settings in the v1 layout, per-game pixel guard fields flattened.</summary>
    private static SessionExport FromV1(JsonElement root)
    {
        var settings = root.TryGetProperty("settings", out var settingsElement) && settingsElement.ValueKind == JsonValueKind.Object
            ? LegacySettingsReader.FromElement(settingsElement, isUpgrade: false)
            : new AppSettings();

        var profiles = new List<GameProfile>();
        if (root.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in profilesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && AppJson.DeserializeProfile(item) is { } profile)
                {
                    profiles.Add(profile);
                }
            }
        }

        var games = new List<SavedGame>();
        if (root.TryGetProperty("games", out var gamesElement) && gamesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in gamesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    games.Add(ReadV1Game(item, settings.PixelGuard.WaitTimeoutMs));
                }
            }
        }

        return new SessionExport
        {
            ExportedAt = root.TryGetProperty("exportedAt", out var at) && at.TryGetDateTimeOffset(out var exportedAt) ? exportedAt : default,
            AppVersion = LegacySettingsReader.GetString(root, "appVersion", string.Empty),
            Settings = settings,
            Profiles = profiles,
            Games = games,
        }.Normalize();
    }

    private static SavedGame ReadV1Game(JsonElement game, int waitTimeoutMs)
    {
        var points = game.TryGetProperty("clickPoints", out var pointsElement) && pointsElement.ValueKind == JsonValueKind.Array
            ? AppJson.DeserializeClickPoints(pointsElement) ?? []
            : [];

        var timing = game.TryGetProperty("profile", out var timingElement) && timingElement.ValueKind == JsonValueKind.Object
            ? AppJson.DeserializeTiming(timingElement) ?? new ClickTiming()
            : new ClickTiming();

        return new SavedGame
        {
            ProcessName = LegacySettingsReader.GetString(game, "processName", string.Empty),
            WindowTitle = LegacySettingsReader.GetString(game, "windowTitle", string.Empty),
            ExecutablePath = LegacySettingsReader.GetString(game, "executablePath", string.Empty),
            ClickPoints = points,
            Timing = timing,
            SequenceDelayMs = LegacySettingsReader.GetInt(game, "sequenceDelayMs", Limits.DefaultSequenceDelayMs),
            PixelGuard = new PixelGuardOptions
            {
                Enabled = LegacySettingsReader.GetBool(game, "enablePixelColorGuard", false),
                Tolerance = LegacySettingsReader.GetInt(game, "colorTolerance", 10),
                MismatchBehavior = LegacySettingsReader.GetEnum(game, "colorMismatchBehavior", ColorMismatchBehavior.StopSession),
                WaitTimeoutMs = waitTimeoutMs,
            },
            IsCustomMode = LegacySettingsReader.GetBool(game, "isCustomMode", false),
        };
    }
}
