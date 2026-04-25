using System.IO;
using System.Text.Json;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public string SettingsFilePath => _settingsPath;

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoClick");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            MigrateLegacyDarkMode(json, settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public void Export(string filePath, AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public AppSettings Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        MigrateLegacyDarkMode(json, settings);
        return settings;
    }

    /// <summary>
    /// Pre-v1.3 settings stored "darkMode": true|false. v1.3 introduces "theme":
    /// "Dark"|"Light"|"HighContrast". If the JSON has darkMode but no theme,
    /// derive theme from the legacy bool so users don't lose their preference.
    /// </summary>
    private static void MigrateLegacyDarkMode(string json, AppSettings settings)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var hasTheme = doc.RootElement.TryGetProperty("theme", out _);
            if (!hasTheme && doc.RootElement.TryGetProperty("darkMode", out var legacy)
                && legacy.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                settings.Theme = legacy.GetBoolean() ? ThemeMode.Dark : ThemeMode.Light;
            }
        }
        catch
        {
            // If the JSON is malformed enough that JsonDocument fails, the prior
            // Deserialize call would also have failed. Default to Theme=Dark.
        }
    }
}
