using System.Text.Json;
using AutoClick.Core.Models;

namespace AutoClick.Core.Settings;

/// <summary>
/// Reads v1 settings (settings.json, or the "settings" object of a v1 session file) into
/// <see cref="AppSettings"/> v2. Field by field and forgiving: a missing or invalid value
/// keeps the default instead of discarding the whole file.
/// </summary>
public static class LegacySettingsReader
{
    /// <param name="json">Contents of a v1 settings.json.</param>
    /// <param name="isUpgrade">
    /// True when migrating an existing v1 installation. v1 always ran as administrator, so
    /// upgraders keep that behaviour (and get a one-time notice explaining how to turn it off).
    /// </param>
    public static AppSettings FromJson(string json, bool isUpgrade)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        return FromElement(document.RootElement, isUpgrade);
    }

    public static AppSettings FromElement(JsonElement root, bool isUpgrade)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("v1 settings must be a JSON object.");
        }

        var defaults = new AppSettings();
        var timing = new ClickTiming
        {
            Mode = GetEnum(root, "defaultClickMode", defaults.DefaultTiming.Mode),
            FixedIntervalSeconds = GetDouble(root, "defaultFixedInterval", defaults.DefaultTiming.FixedIntervalSeconds),
            RandomMinSeconds = GetDouble(root, "randomMin", defaults.DefaultTiming.RandomMinSeconds),
            RandomMaxSeconds = GetDouble(root, "randomMax", defaults.DefaultTiming.RandomMaxSeconds),
        };

        var pixelGuard = new PixelGuardOptions
        {
            Enabled = GetBool(root, "enablePixelColorGuard", defaults.PixelGuard.Enabled),
            Tolerance = GetInt(root, "colorTolerance", defaults.PixelGuard.Tolerance),
            MismatchBehavior = GetEnum(root, "colorMismatchBehavior", defaults.PixelGuard.MismatchBehavior),
            WaitTimeoutMs = GetInt(root, "colorWaitTimeoutMs", defaults.PixelGuard.WaitTimeoutMs),
        };

        var hotkeys = defaults.Hotkeys;
        if (root.TryGetProperty("hotkeys", out var hk) && hk.ValueKind == JsonValueKind.Object)
        {
            hotkeys = new HotkeySettings
            {
                PauseResume = GetString(hk, "pauseResume", defaults.Hotkeys.PauseResume),
                StopAll = GetString(hk, "stopAll", defaults.Hotkeys.StopAll),
                StartAll = GetString(hk, "startAll", defaults.Hotkeys.StartAll),
            };
        }

        var settings = new AppSettings
        {
            SettingsMode = GetEnum(root, "settingsMode", defaults.SettingsMode),
            DefaultTiming = timing,
            MaxGamesInQueue = GetInt(root, "maxGamesInQueue", defaults.MaxGamesInQueue),
            PixelGuard = pixelGuard,
            Theme = ReadTheme(root),
            Language = GetString(root, "language", defaults.Language),
            ExitBehavior = GetEnum(root, "exitBehavior", defaults.ExitBehavior),
            ShowRealTimeLogs = GetBool(root, "showRealTimeLogs", defaults.ShowRealTimeLogs),
            SoundNotifications = GetBool(root, "soundNotifications", defaults.SoundNotifications),
            ShowGameExitNotification = GetBool(root, "showGameExitNotification", defaults.ShowGameExitNotification),
            MinimizeOnStartAll = GetBool(root, "minimizeOnStartAll", defaults.MinimizeOnStartAll),
            AutoUpdate = GetBool(root, "autoUpdate", defaults.AutoUpdate),
            Hotkeys = hotkeys,
            AlwaysRunAsAdmin = isUpgrade,
            ShowAdminModeNotice = isUpgrade,
        };

        return settings.Normalize();
    }

    /// <summary>
    /// v1.3 stored "theme"; earlier versions stored "darkMode": true/false. v1 defaulted to Dark,
    /// so a file with neither keeps Dark.
    /// </summary>
    private static AppTheme ReadTheme(JsonElement root)
    {
        if (root.TryGetProperty("theme", out _))
        {
            return GetEnum(root, "theme", AppTheme.Dark);
        }

        if (root.TryGetProperty("darkMode", out var darkMode) && darkMode.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return darkMode.GetBoolean() ? AppTheme.Dark : AppTheme.Light;
        }

        return AppTheme.Dark;
    }

    internal static TEnum GetEnum<TEnum>(JsonElement obj, string name, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!obj.TryGetProperty(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.String
            && Enum.TryParse<TEnum>(value.GetString(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
            && !int.TryParse(value.GetString(), out _))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            var candidate = (TEnum)Enum.ToObject(typeof(TEnum), number);
            if (Enum.IsDefined(candidate))
            {
                return candidate;
            }
        }

        return fallback;
    }

    internal static double GetDouble(JsonElement obj, string name, double fallback) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && double.IsFinite(d)
            ? d
            : fallback;

    internal static int GetInt(JsonElement obj, string name, int fallback)
    {
        if (!obj.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }

        if (v.TryGetInt32(out var i))
        {
            return i;
        }

        return v.TryGetDouble(out var d) && double.IsFinite(d) ? (int)Math.Clamp(Math.Round(d), int.MinValue, int.MaxValue) : fallback;
    }

    internal static bool GetBool(JsonElement obj, string name, bool fallback) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? v.GetBoolean()
            : fallback;

    internal static string GetString(JsonElement obj, string name, string fallback) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!
            : fallback;
}
