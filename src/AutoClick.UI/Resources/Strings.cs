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
    public static string ExportSession => GetString("ExportSession", "Export Session");
    public static string ImportSession => GetString("ImportSession", "Import Session");
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
    public static string MinimizeOnStartAll => GetString("MinimizeOnStartAll", "Minimize to tray on Start All");
    public static string DragToReorder => GetString("DragToReorder", "Drag to reorder");
    public static string WaitUntilMatch => GetString("WaitUntilMatch", "Wait until pixel matches");
    public static string ColorWaitTimeout => GetString("ColorWaitTimeout", "Wait timeout");
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

    // Dashboard
    public static string Dashboard => GetString("Dashboard", "Dashboard");
    public static string DashboardCpmChart => GetString("DashboardCpmChart", "Clicks Per Minute (Live)");
    public static string DashboardGameBreakdown => GetString("DashboardGameBreakdown", "Per-Game Breakdown");

    // Click count labels
    public static string ClickSuccess => GetString("ClickSuccess", "Success");
    public static string ClickSkipped => GetString("ClickSkipped", "Skipped");
    public static string ClickTotal => GetString("ClickTotal", "Total");

    // Pixel Color Guard
    public static string PixelColorGuard => GetString("PixelColorGuard", "Pixel Color Guard");
    public static string EnablePixelColorGuard => GetString("EnablePixelColorGuard", "Enable Pixel Color Guard");
    public static string ColorTolerance => GetString("ColorTolerance", "Color Tolerance (0-50)");
    public static string OnColorMismatch => GetString("OnColorMismatch", "On Color Mismatch");
    public static string SkipPoint => GetString("SkipPoint", "Skip Point");
    public static string StopSession => GetString("StopSession", "Stop Session");
    public static string ColorMismatchSkipped => GetString("ColorMismatchSkipped", "Color mismatch at ({0}, {1}): skipped.");
    public static string ColorMismatchStopped => GetString("ColorMismatchStopped", "Color mismatch at ({0}, {1}): expected {2}, got {3}. Stopped.");

    // Scheduler
    public static string Schedule => GetString("Schedule", "Schedule");
    public static string CancelSchedule => GetString("CancelSchedule", "Cancel");
    public static string StartTime => GetString("StartTime", "Start:");
    public static string StopTime => GetString("StopTime", "Stop:");
    public static string SchedulerStartsIn => GetString("SchedulerStartsIn", "Starts in: {0}");
    public static string SchedulerStopsIn => GetString("SchedulerStopsIn", "Stops in: {0}");
    public static string SchedulerStarted => GetString("SchedulerStarted", "Scheduler triggered start");
    public static string SchedulerStopped => GetString("SchedulerStopped", "Scheduler triggered stop");
    public static string SchedulerInvalidTime => GetString("SchedulerInvalidTime", "Invalid time format. Use HH:mm.");
    public static string SchedulerInvalidFormat => GetString("SchedulerInvalidFormat", "Invalid format. Enter time as HH:mm (e.g., 20:00).");
    public static string SchedulerInvalidHour => GetString("SchedulerInvalidHour", "\"{0}\": hour must be 00–23.");
    public static string SchedulerInvalidMinute => GetString("SchedulerInvalidMinute", "\"{0}\": minute must be 00–59.");
    public static string SchedulerStopBeforeStart => GetString("SchedulerStopBeforeStart", "Stop time must be after start time.");
    public static string SchedulerStartRequired => GetString("SchedulerStartRequired", "Start time is required.");
    public static string SchedulerNoGames => GetString("SchedulerNoGames", "Add games with coordinates before scheduling.");

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
    public static string GameExitNotificationMessage => GetString("GameExitNotificationMessage", "{0} has exited. Success: {1}, Skipped: {2}");

    // Click type per point
    public static string PickerInstruction => GetString("PickerInstruction", "Click to select. 1=Left 2=Double 3=Right. ESC=Cancel");
    public static string PickerCurrentType => GetString("PickerCurrentType", "Current: {0}");
    public static string ClickTypeLeft => GetString("ClickTypeLeft", "Left");
    public static string ClickTypeDouble => GetString("ClickTypeDouble", "Double");
    public static string ClickTypeRight => GetString("ClickTypeRight", "Right");

    // Dashboard extras
    public static string AutoStoppedQueueEmpty => GetString("AutoStoppedQueueEmpty", "All games have exited. Auto-stopped.");
    public static string DashboardSuccessRatio => GetString("DashboardSuccessRatio", "Success / Skip Ratio");
    public static string DashboardGameTimeline => GetString("DashboardGameTimeline", "Per-Game CPM Timeline");
    public static string ExportStats => GetString("ExportStats", "Export Stats");
    public static string ResetDashboard => GetString("ResetDashboard", "Reset All Stats");
    public static string ConfirmResetStats => GetString("ConfirmResetStats", "Reset all session statistics and dashboard data? This cannot be undone.");

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
