using System.IO;
using System.Text.Json;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.Services;

public class ProfileService : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _profilesDir;

    public ProfileService()
    {
        _profilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoClick", "profiles");
        Directory.CreateDirectory(_profilesDir);
    }

    public List<GameProfile> GetAll()
    {
        var profiles = new List<GameProfile>();
        if (!Directory.Exists(_profilesDir)) return profiles;

        foreach (var file in Directory.GetFiles(_profilesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<GameProfile>(json, JsonOptions);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch
            {
                // Skip corrupt files
            }
        }

        return profiles.OrderBy(p => p.Name).ToList();
    }

    public GameProfile? GetByName(string name)
    {
        return GetAll().FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void Save(GameProfile profile)
    {
        profile.UpdatedAt = DateTime.Now;
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        var filePath = Path.Combine(_profilesDir, $"{profile.Id}.json");
        File.WriteAllText(filePath, json);
    }

    public void Delete(string id)
    {
        var filePath = Path.Combine(_profilesDir, $"{id}.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void Export(string filePath, GameProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public GameProfile Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var profile = JsonSerializer.Deserialize<GameProfile>(json, JsonOptions)
                      ?? throw new InvalidOperationException("Invalid profile file.");

        var existing = GetByName(profile.Name);
        if (existing != null)
        {
            profile.Id = existing.Id;
            profile.CreatedAt = existing.CreatedAt;
        }
        else
        {
            profile.Id = Guid.NewGuid().ToString("N")[..8];
            profile.CreatedAt = DateTime.Now;
        }

        profile.UpdatedAt = DateTime.Now;
        Save(profile);
        return profile;
    }
}
