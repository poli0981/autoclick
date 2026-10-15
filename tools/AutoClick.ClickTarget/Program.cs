namespace AutoClick.ClickTarget;

/// <summary>
/// Test target for AutoClick end-to-end checks. It logs every mouse button and keyboard
/// message it receives (client coordinates, virtual-key and scan codes).
///
/// Switches:
///   --dpi unaware|system|pmv2   DPI awareness of this process (default: pmv2), to check
///                               coordinate mapping for games that are not DPI aware.
///   --log &lt;path&gt;                append one tab-separated line per received message.
///   --title &lt;text&gt;              window title (default: "AutoClick ClickTarget").
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var dpi = GetArg(args, "--dpi") ?? "pmv2";
        var mode = dpi.ToLowerInvariant() switch
        {
            "unaware" => HighDpiMode.DpiUnaware,
            "system" => HighDpiMode.SystemAware,
            _ => HighDpiMode.PerMonitorV2,
        };

        Application.SetHighDpiMode(mode);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var title = GetArg(args, "--title") ?? "AutoClick ClickTarget";
        using var form = new TargetForm($"{title} [{mode}]", GetArg(args, "--log"));
        Application.Run(form);
    }

    private static string? GetArg(string[] args, string name)
    {
        var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
