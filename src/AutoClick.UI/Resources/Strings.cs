namespace AutoClick.UI.Resources;

public static class Strings
{
    public static string AppTitle => GetString("AppTitle", "AutoClick");
    public static string Main => GetString("Main", "Main");
    public static string Settings => GetString("Settings", "Settings");
    public static string About => GetString("About", "About");
    public static string Start => GetString("Start", "Start");
    public static string Pause => GetString("Pause", "Pause");
    public static string Resume => GetString("Resume", "Resume");
    public static string Stop => GetString("Stop", "Stop");
    public static string AddGame => GetString("AddGame", "Add Game");
    public static string RemoveGame => GetString("RemoveGame", "Remove");
    public static string SelectCoordinate => GetString("SelectCoordinate", "Select Coordinate");
    public static string GamesRunning => GetString("GamesRunning", "Games Running");
    public static string ClickMode => GetString("ClickMode", "Click Mode");
    public static string Random => GetString("Random", "Random");
    public static string Fixed => GetString("Fixed", "Fixed");
    public static string Interval => GetString("Interval", "Interval (s)");
    public static string ClickCount => GetString("ClickCount", "Click Count");
    public static string Status => GetString("Status", "Status");
    public static string Path => GetString("Path", "Path");
    public static string Coordinate => GetString("Coordinate", "Coordinate");
    public static string CurrentInterval => GetString("CurrentInterval", "Current Interval");
    public static string Basic => GetString("Basic", "Basic");
    public static string Advanced => GetString("Advanced", "Advanced");
    public static string MaxGames => GetString("MaxGames", "Max Games in Queue");
    public static string ShowLogs => GetString("ShowLogs", "Show Real-time Logs");
    public static string ExportLog => GetString("ExportLog", "Export Log");
    public static string ExportSettings => GetString("ExportSettings", "Export Settings");
    public static string ImportSettings => GetString("ImportSettings", "Import Settings");
    public static string ResetAll => GetString("ResetAll", "Reset All");
    public static string RemoveAll => GetString("RemoveAll", "Remove All");
    public static string RandomMin => GetString("RandomMin", "Random Min (s)");
    public static string RandomMax => GetString("RandomMax", "Random Max (s)");
    public static string ResetStats => GetString("ResetStats", "Reset");
    public static string AddPoint => GetString("AddPoint", "Add Point");
    public static string ClearPoints => GetString("ClearPoints", "Clear Points");
    public static string Points => GetString("Points", "points");
    public static string SequenceDelay => GetString("SequenceDelay", "Delay between points");
    public static string SessionStats => GetString("SessionStats", "Session Statistics");
    public static string TotalClicks => GetString("TotalClicks", "Total Clicks");
    public static string Uptime => GetString("Uptime", "Uptime");
    public static string ClicksPerMin => GetString("ClicksPerMin", "Clicks/min");
    public static string PeakCPM => GetString("PeakCPM", "Peak");
    public static string SoundNotifications => GetString("SoundNotifications", "Sound Notifications");
    public static string Language => GetString("Language", "Language");
    public static string Theme => GetString("Theme", "Theme");
    public static string Dark => GetString("Dark", "Dark");
    public static string Light => GetString("Light", "Light");
    public static string Auto => GetString("Auto", "Auto");
    public static string Hotkeys => GetString("Hotkeys", "Hotkeys");
    public static string Idle => GetString("Idle", "Idle");
    public static string Running => GetString("Running", "Running");
    public static string Paused => GetString("Paused", "Paused");
    public static string Stopped => GetString("Stopped", "Stopped");
    public static string Logs => GetString("Logs", "Logs");
    public static string NoGames => GetString("NoGames", "No games added yet. Click \"Add Game\" to begin.");
    public static string ConfirmReset => GetString("ConfirmReset", "This will stop all running games and reset the application. Continue?");
    public static string StartAll => GetString("StartAll", "Start All");
    public static string StopAll => GetString("StopAll", "Stop All");
    public static string PauseResume => GetString("PauseResume", "Pause/Resume");

    // Exit behavior
    public static string ExitBehaviorLabel => GetString("ExitBehavior", "On Exit");
    public static string MinimizeToTray => GetString("MinimizeToTray", "Minimize to Tray");
    public static string StopAndExit => GetString("StopAndExit", "Stop All & Exit");
    public static string ForceExitLabel => GetString("ForceExit", "Force Exit");

    // Warnings / Dialogs
    public static string ConfirmExitTitle => GetString("ConfirmExitTitle", "Confirm Exit");
    public static string ConfirmExitMsg => GetString("ConfirmExitMsg", "There are running tasks. Stop all and exit?");
    public static string LanguageChangeTitle => GetString("LanguageChangeTitle", "Language Changed");
    public static string LanguageChangeMsg => GetString("LanguageChangeMsg", "The application needs to restart to apply the new language. Restart now?");
    public static string CoordinateErrorTitle => GetString("CoordinateErrorTitle", "Coordinate Error");
    public static string CoordinateErrorMsg => GetString("CoordinateErrorMsg", "The coordinate is invalid or already in use. Please try again.");
    public static string RandomCoordinateErrorTitle => GetString("RandomCoordinateErrorTitle", "Random Coordinate");
    public static string RandomCoordinateErrorMsg => GetString("RandomCoordinateErrorMsg", "Could not generate a valid random coordinate. The game window may be too small or all coordinates are in use.");
    public static string ConfirmResetApp => GetString("ConfirmResetApp", "Reset the application to factory defaults? All settings will be restored to their initial values.");
    public static string OpenSettingsFile => GetString("OpenSettingsFile", "Open Settings File");
    public static string SaveSettings => GetString("SaveSettings", "Save Settings");

    // Documents
    public static string Documents => GetString("Documents", "Documents");
    public static string PrivacyPolicy => GetString("PrivacyPolicy", "Privacy Policy");
    public static string Disclaimer_Label => GetString("Disclaimer", "Disclaimer");
    public static string Acknowledgements => GetString("Acknowledgements", "Acknowledgements & Third-Party");
    public static string TermsOfService => GetString("TermsOfService", "Terms of Service");
    public static string EULA_Label => GetString("EULA", "End-User License Agreement");
    public static string SecurityPolicy => GetString("SecurityPolicy", "Security Policy");
    public static string ViewOnGitHub => GetString("ViewOnGitHub", "View on GitHub");
    public static string AntiCheatWarning => GetString("AntiCheatWarning", "Use with caution on games with anti-cheat. Kernel-level anti-cheat may result in account bans. The developer is not responsible.");
    public static string AIDisclosure => GetString("AIDisclosure", "This software was built with AI assistance. In-app translations are AI-generated and may not be fully accurate.");

    // Update
    public static string Update => GetString("Update", "Update");
    public static string AutoUpdate => GetString("AutoUpdate", "Auto-update on Startup");
    public static string CheckForUpdate => GetString("CheckForUpdate", "Check for Update");
    public static string DownloadUpdate => GetString("DownloadUpdate", "Download Update");
    public static string ApplyAndRestart => GetString("ApplyAndRestart", "Apply & Restart");
    public static string UpdateReady => GetString("UpdateReady", "Click 'Check for Update' to get started.");
    public static string UpdateDevMode => GetString("UpdateDevMode", "Running in development mode. Updates not available.");
    public static string UpdateChecking => GetString("UpdateChecking", "Checking for updates...");
    public static string UpdateUpToDate => GetString("UpdateUpToDate", "You are running the latest version.");
    public static string UpdateFoundMsg => GetString("UpdateFoundMsg", "Update available: {0} → {1}");
    public static string UpdateDownloading => GetString("UpdateDownloading", "Downloading update...");
    public static string UpdateDownloadComplete => GetString("UpdateDownloadComplete", "Download complete. Click 'Apply & Restart' to update.");
    public static string UpdateApplying => GetString("UpdateApplying", "Applying update and restarting...");
    public static string UpdateAvailableTitle => GetString("UpdateAvailableTitle", "Update Available");
    public static string UpdateAutoDownloadedMsg => GetString("UpdateAutoDownloadedMsg", "Version {0} has been downloaded. Restart now to apply?");
    public static string UpdateNoRelease => GetString("UpdateNoRelease", "No releases found on GitHub.");
    public static string UpdateRateLimited => GetString("UpdateRateLimited", "GitHub API rate limit exceeded. Please wait a few minutes.");
    public static string UpdateForbidden => GetString("UpdateForbidden", "Access denied. Check repository permissions.");
    public static string UpdateServerError => GetString("UpdateServerError", "GitHub server error. Please try again later.");
    public static string UpdateNetworkError => GetString("UpdateNetworkError", "Network error. Check your internet connection.");
    public static string UpdateChecksumFailed => GetString("UpdateChecksumFailed", "Download verification failed. Please try again.");
    public static string UpdateUnknownError => GetString("UpdateUnknownError", "An unknown error occurred during update.");

    // Settings Mode
    public static string SettingsMode => GetString("SettingsMode", "Settings Mode");
    public static string GlobalMode => GetString("GlobalMode", "Global (Same for all)");
    public static string CustomMode => GetString("CustomMode", "Custom (Per game)");
    public static string PerGameMode => GetString("PerGameMode", "Mode");
    public static string PerGameRandomMin => GetString("PerGameRandomMin", "Min (s)");
    public static string PerGameRandomMax => GetString("PerGameRandomMax", "Max (s)");

    // Profiles
    public static string Profile => GetString("Profile", "Profile");
    public static string SaveProfile => GetString("SaveProfile", "Save Profile");
    public static string LoadProfile => GetString("LoadProfile", "Load");
    public static string DeleteProfile => GetString("DeleteProfile", "Delete");
    public static string ExportProfile => GetString("ExportProfile", "Export");
    public static string ImportProfile => GetString("ImportProfile", "Import");
    public static string ProfileNamePrompt => GetString("ProfileNamePrompt", "Profile name:");
    public static string ProfileSaved => GetString("ProfileSaved", "Profile \"{0}\" saved.");
    public static string ProfileLoaded => GetString("ProfileLoaded", "Profile \"{0}\" loaded into \"{1}\".");
    public static string ProfileDeleted => GetString("ProfileDeleted", "Profile \"{0}\" deleted.");
    public static string ProfileExported => GetString("ProfileExported", "Profile exported to {0}.");
    public static string ProfileImported => GetString("ProfileImported", "Profile \"{0}\" imported.");
    public static string ProfileImportError => GetString("ProfileImportError", "Failed to import profile.");
    public static string ConfirmDeleteProfile => GetString("ConfirmDeleteProfile", "Delete profile \"{0}\"?");
    public static string ProfileNameEmpty => GetString("ProfileNameEmpty", "Profile name cannot be empty.");
    public static string ProfileNameDuplicate => GetString("ProfileNameDuplicate", "A profile named \"{0}\" exists. Overwrite?");
    public static string NoCoordinatesToSave => GetString("NoCoordinatesToSave", "Add coordinates before saving a profile.");
    public static string SelectProfile => GetString("SelectProfile", "Select a profile...");

    // Game exit notification
    public static string GameExitNotification => GetString("GameExitNotification", "Game Exit Notification");
    public static string GameExitNotificationTitle => GetString("GameExitNotificationTitle", "Game Exited");
    public static string GameExitNotificationMessage => GetString("GameExitNotificationMessage", "{0} has exited. Total clicks: {1}");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("AutoClick.UI.Resources.Strings", typeof(Strings).Assembly);

    private static string GetString(string name, string fallback)
    {
        try
        {
            return ResourceManager.GetString(name, System.Threading.Thread.CurrentThread.CurrentUICulture) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
