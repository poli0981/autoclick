using System;
using System.IO;
using System.Text.Json;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            MigrateIfNeeded(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Fix stale values left by older versions in the user's settings file.
    /// </summary>
    private void MigrateIfNeeded(AppSettings settings)
    {
        var dirty = false;
        var defaults = new AppSettings();

        // v1.0.0 → v1.0.1: repo URL was wrong
        if (settings.GitHubRepo is "autoclick/autoclick" or "")
        {
            settings.GitHubRepo = defaults.GitHubRepo;
            dirty = true;
        }

        if (dirty)
            Save(settings);
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
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }
}
