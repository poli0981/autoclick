namespace AutoClick.Services;

/// <summary>
/// Where AutoClick keeps its data: %LocalAppData%\AutoClick (the same folder as v1, which is
/// also Velopack's install root, so uninstalling removes the data too).
/// </summary>
public sealed class AppPaths
{
    public AppPaths(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoClick");
    }

    public string DataDirectory { get; }

    /// <summary>v2 settings. v1's settings.json is left untouched so a downgrade keeps working.</summary>
    public string SettingsFile => Path.Combine(DataDirectory, "settings.v2.json");

    public string LegacySettingsFile => Path.Combine(DataDirectory, "settings.json");

    /// <summary>Profiles stay in the v1 folder and format, readable by both versions.</summary>
    public string ProfilesDirectory => Path.Combine(DataDirectory, "profiles");

    public string LogsDirectory => Path.Combine(DataDirectory, "logs");

    public string BackupDirectory => Path.Combine(DataDirectory, "backup");

    /// <summary>
    /// True when v1 left data behind. v1 wrote settings.json only on close or Save, so saved
    /// profiles alone also count.
    /// </summary>
    public bool HasLegacyData =>
        File.Exists(LegacySettingsFile)
        || (Directory.Exists(ProfilesDirectory) && Directory.EnumerateFiles(ProfilesDirectory, "*.json").Any());
}
