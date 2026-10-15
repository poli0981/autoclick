using System.Text.Json;
using AutoClick.Core.Serialization;
using AutoClick.Core.Settings;
using Microsoft.Extensions.Logging;

namespace AutoClick.Services.Persistence;

public enum SettingsLoadResult
{
    /// <summary>settings.v2.json was read.</summary>
    Loaded,

    /// <summary>First run of v2 after v1: settings migrated from v1's data.</summary>
    Migrated,

    /// <summary>No settings yet: defaults.</summary>
    Created,

    /// <summary>settings.v2.json was unreadable; it was moved aside and defaults were used.</summary>
    RecoveredFromCorruption,
}

/// <summary>Loads, migrates and saves <see cref="AppSettings"/>. Every save is atomic.</summary>
public sealed class SettingsStore(AppPaths paths, TimeProvider timeProvider, ILogger<SettingsStore> logger)
{
    private readonly Lock _gate = new();
    private AppSettings _current = new AppSettings().Normalize();

    public AppSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <summary>Raised after every change (on the thread that made it).</summary>
    public event EventHandler<AppSettings>? Changed;

    /// <summary>Path of a file the corrupt settings were moved to during <see cref="Load"/>.</summary>
    public string? CorruptBackupPath { get; private set; }

    public SettingsLoadResult Load()
    {
        if (File.Exists(paths.SettingsFile))
        {
            try
            {
                var loaded = AppJson.DeserializeSettings(File.ReadAllText(paths.SettingsFile))
                    ?? throw new JsonException("Settings file is empty.");
                SetCurrent(loaded.Normalize());
                return SettingsLoadResult.Loaded;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                CorruptBackupPath = TryMoveAside(paths.SettingsFile);
                logger.LogWarning(ex, "Settings file was unreadable and was moved to {Backup}; using defaults", CorruptBackupPath);
                SetCurrent(new AppSettings().Normalize());
                TrySave();
                return SettingsLoadResult.RecoveredFromCorruption;
            }
        }

        if (paths.HasLegacyData)
        {
            SetCurrent(MigrateFromV1());
            TrySave();
            return SettingsLoadResult.Migrated;
        }

        SetCurrent(new AppSettings().Normalize());
        TrySave();
        return SettingsLoadResult.Created;
    }

    /// <summary>Applies <paramref name="change"/> to the current settings and saves them.</summary>
    public AppSettings Update(Func<AppSettings, AppSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        AppSettings updated;
        lock (_gate)
        {
            updated = change(_current).Normalize();
            if (updated == _current)
            {
                return updated;
            }

            _current = updated;
        }

        TrySave();
        Changed?.Invoke(this, updated);
        return updated;
    }

    /// <summary>Writes settings to a file the user picked (format: settings v2).</summary>
    public void Export(string path) => AtomicFile.WriteAllText(path, AppJson.SerializeSettings(Current));

    /// <summary>
    /// Imports a v1 or v2 settings file. Machine-specific values (administrator mode, window
    /// position, notices) are kept from the current settings.
    /// </summary>
    /// <exception cref="JsonException">The file is not a settings file.</exception>
    public AppSettings Import(string path)
    {
        var imported = ParseAnyVersion(File.ReadAllText(path));
        return Update(current => imported with
        {
            AlwaysRunAsAdmin = current.AlwaysRunAsAdmin,
            ShowAdminModeNotice = current.ShowAdminModeNotice,
            MainWindow = current.MainWindow,
        });
    }

    /// <summary>Restores defaults, keeping only the machine-specific values.</summary>
    public AppSettings Reset() => Update(current => new AppSettings
    {
        AlwaysRunAsAdmin = current.AlwaysRunAsAdmin,
        MainWindow = current.MainWindow,
    });

    internal static AppSettings ParseAnyVersion(string json)
    {
        using (var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("A settings file must contain a JSON object.");
            }

            if (!document.RootElement.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var schema) || schema < AppSettings.CurrentSchemaVersion)
            {
                return LegacySettingsReader.FromElement(document.RootElement, isUpgrade: false);
            }
        }

        return (AppJson.DeserializeSettings(json) ?? throw new JsonException("Settings file is empty.")).Normalize();
    }

    private AppSettings MigrateFromV1()
    {
        BackUpLegacyData();
        if (File.Exists(paths.LegacySettingsFile))
        {
            try
            {
                var migrated = LegacySettingsReader.FromJson(File.ReadAllText(paths.LegacySettingsFile), isUpgrade: true);
                logger.LogInformation("Migrated v1 settings to settings v2");
                return migrated;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "v1 settings could not be read; starting from defaults");
            }
        }

        // v1 data without a readable settings file: still an upgrade (v1 always ran elevated).
        return new AppSettings { AlwaysRunAsAdmin = true, ShowAdminModeNotice = true }.Normalize();
    }

    private void BackUpLegacyData()
    {
        try
        {
            if (File.Exists(paths.LegacySettingsFile))
            {
                Directory.CreateDirectory(paths.BackupDirectory);
                File.Copy(paths.LegacySettingsFile, Path.Combine(paths.BackupDirectory, "settings.v1.json"), overwrite: false);
            }
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "v1 settings backup skipped");
        }
    }

    private void SetCurrent(AppSettings settings)
    {
        lock (_gate)
        {
            _current = settings;
        }
    }

    private void TrySave()
    {
        try
        {
            AtomicFile.WriteAllText(paths.SettingsFile, AppJson.SerializeSettings(Current));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not save settings to {Path}", paths.SettingsFile);
        }
    }

    private string? TryMoveAside(string path)
    {
        try
        {
            return AtomicFile.MoveAside(path, timeProvider.GetLocalNow());
        }
        catch (IOException)
        {
            return null;
        }
    }
}
