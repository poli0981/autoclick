using AutoClick.Core.Abstractions;
using AutoClick.Core.Serialization;
using AutoClick.Core.Sessions;
using AutoClick.Core.Settings;

namespace AutoClick.Services.Persistence;

/// <summary>A saved game matched to a running window.</summary>
public sealed record GameMatch(SavedGame Game, WindowInfo Window);

/// <summary>What importing a session file will change.</summary>
public sealed record SessionImportPlan(
    AppSettings Settings,
    IReadOnlyList<GameMatch> Matched,
    IReadOnlyList<SavedGame> Unmatched,
    SessionExport Source);

/// <summary>Exports and imports .autoclick-session files.</summary>
public static class SessionImport
{
    public static void Export(string path, SessionExport session) =>
        AtomicFile.WriteAllText(path, AppJson.SerializeSession(session.Normalize()));

    /// <exception cref="System.Text.Json.JsonException">Not a session file.</exception>
    /// <exception cref="NotSupportedException">Written by a newer AutoClick.</exception>
    public static SessionExport Read(string path) => SessionFile.Parse(File.ReadAllText(path));

    /// <summary>
    /// Matches saved games to running windows (same process name, then same title when there
    /// are several) and keeps this machine's administrator and window settings.
    /// </summary>
    public static SessionImportPlan Plan(SessionExport session, AppSettings current, IReadOnlyList<WindowInfo> windows)
    {
        var settings = session.Settings with
        {
            AlwaysRunAsAdmin = current.AlwaysRunAsAdmin,
            ShowAdminModeNotice = current.ShowAdminModeNotice,
            MainWindow = current.MainWindow,
        };

        var available = windows.ToList();
        var matched = new List<GameMatch>();
        var unmatched = new List<SavedGame>();
        foreach (var game in session.Games)
        {
            var candidates = available
                .Where(w => string.Equals(w.ProcessName, game.ProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var window = candidates.FirstOrDefault(w => string.Equals(w.Title, game.WindowTitle, StringComparison.Ordinal))
                ?? (candidates.Count == 1 ? candidates[0] : null);

            if (window is null)
            {
                unmatched.Add(game);
                continue;
            }

            available.Remove(window);
            matched.Add(new GameMatch(game, window));
        }

        return new SessionImportPlan(settings.Normalize(), matched, unmatched, session);
    }
}
