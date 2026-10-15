using System.Text.Json;
using System.Text.Json.Serialization;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using AutoClick.Core.Settings;

namespace AutoClick.Core.Serialization;

/// <summary>
/// JSON formats of the files AutoClick writes. All use camelCase names.
/// Settings and session files store enums as names; profile files store enums as numbers,
/// exactly like v1, so a v1 install can still read profiles saved by v2.
/// Reading accepts both names and numbers.
/// </summary>
/// <remarks>
/// Reflection-based on purpose: the source generator assigns every init-only property, so a
/// property missing from an older file would get default(T) instead of its declared default.
/// </remarks>
public static class AppJson
{
    private static readonly JsonSerializerOptions NamedEnums = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions NumericEnums = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string SerializeSettings(AppSettings settings) => JsonSerializer.Serialize(settings, NamedEnums);

    public static AppSettings? DeserializeSettings(string json) => JsonSerializer.Deserialize<AppSettings>(json, NamedEnums);

    public static string SerializeProfile(GameProfile profile) => JsonSerializer.Serialize(profile, NumericEnums);

    public static GameProfile? DeserializeProfile(string json) => JsonSerializer.Deserialize<GameProfile>(json, NamedEnums);

    public static GameProfile? DeserializeProfile(JsonElement element) => element.Deserialize<GameProfile>(NamedEnums);

    public static string SerializeSession(SessionExport session) => JsonSerializer.Serialize(session, NamedEnums);

    internal static SessionExport? DeserializeSession(string json) => JsonSerializer.Deserialize<SessionExport>(json, NamedEnums);

    internal static List<ClickPoint>? DeserializeClickPoints(JsonElement element) => element.Deserialize<List<ClickPoint>>(NamedEnums);

    internal static ClickTiming? DeserializeTiming(JsonElement element) => element.Deserialize<ClickTiming>(NamedEnums);
}
