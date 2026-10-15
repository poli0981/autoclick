using System.Text.Json;
using AutoClick.Core.Models;
using AutoClick.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace AutoClick.Services.Persistence;

/// <summary>
/// Saved profiles: one file per profile, profiles\{id}.json, in the v1 format. The id is
/// validated before it is used as a file name, and the file name is the id that counts.
/// </summary>
public sealed class ProfileStore(AppPaths paths, TimeProvider timeProvider, ILogger<ProfileStore> logger)
{
    public IReadOnlyList<GameProfile> GetAll()
    {
        if (!Directory.Exists(paths.ProfilesDirectory))
        {
            return [];
        }

        var profiles = new List<GameProfile>();
        foreach (var file in Directory.EnumerateFiles(paths.ProfilesDirectory, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (!GameProfile.IsValidId(id))
            {
                logger.LogWarning("Ignoring profile file with an unexpected name: {File}", Path.GetFileName(file));
                continue;
            }

            try
            {
                if (AppJson.DeserializeProfile(File.ReadAllText(file)) is { } profile)
                {
                    profiles.Add((profile with { Id = id }).Normalize());
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Skipping unreadable profile {File}", Path.GetFileName(file));
            }
        }

        return profiles.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public GameProfile? FindByName(string name) =>
        GetAll().FirstOrDefault(p => string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Saves (creates or replaces) a profile and returns what was written.</summary>
    public GameProfile Save(GameProfile profile)
    {
        var now = timeProvider.GetLocalNow();
        var normalized = profile.Normalize();
        var saved = normalized with
        {
            CreatedAt = normalized.CreatedAt == default ? now : normalized.CreatedAt,
            UpdatedAt = now,
        };

        AtomicFile.WriteAllText(PathFor(saved.Id), AppJson.SerializeProfile(saved));
        return saved;
    }

    public bool Delete(string id)
    {
        if (!GameProfile.IsValidId(id))
        {
            return false;
        }

        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>Writes a profile to a .autoclick file (same format v1 used).</summary>
    public static void Export(GameProfile profile, string path) =>
        AtomicFile.WriteAllText(path, AppJson.SerializeProfile(profile.Normalize()));

    /// <summary>Reads a .autoclick file without saving it.</summary>
    /// <exception cref="JsonException">Not a profile file.</exception>
    public static GameProfile ReadFile(string path) =>
        (AppJson.DeserializeProfile(File.ReadAllText(path)) ?? throw new JsonException("Empty profile file.")).Normalize();

    /// <summary>
    /// Saves an imported profile. A profile with the same name is replaced (keeping its id and
    /// creation date); otherwise it gets a new id, whatever id the file claimed.
    /// </summary>
    public GameProfile Import(GameProfile imported)
    {
        var existing = FindByName(imported.Name);
        return Save(imported with
        {
            Id = existing?.Id ?? GameProfile.NewId(),
            CreatedAt = existing?.CreatedAt ?? default,
        });
    }

    private string PathFor(string id)
    {
        if (!GameProfile.IsValidId(id))
        {
            throw new ArgumentException($"Invalid profile id '{id}'.", nameof(id));
        }

        return Path.Combine(paths.ProfilesDirectory, id + ".json");
    }
}
