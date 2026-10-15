namespace AutoClick.UI.Startup;

/// <summary>Command-line switches.</summary>
public sealed record StartupOptions
{
    /// <summary>CI check: build the DI graph, open every page in every theme and language, then exit.</summary>
    public const string SmokeTestArg = "--smoke-test";

    /// <summary>Started by AutoClick itself (restart, update, elevation): wait for the previous instance.</summary>
    public const string RestartingArg = "--restarting";

    /// <summary>Start hidden in the notification area.</summary>
    public const string MinimizedArg = "--minimized";

    public bool SmokeTest { get; init; }
    public bool Restarting { get; init; }
    public bool StartMinimized { get; init; }

    public static StartupOptions Parse(IEnumerable<string> args)
    {
        var set = new HashSet<string>(args, StringComparer.OrdinalIgnoreCase);
        return new StartupOptions
        {
            SmokeTest = set.Contains(SmokeTestArg),
            Restarting = set.Contains(RestartingArg),
            StartMinimized = set.Contains(MinimizedArg),
        };
    }
}
