using System.Globalization;
using System.Reflection;
using AutoClick.Services;
using AutoClick.Services.Logging;
using Serilog;
using Serilog.Core;

namespace AutoClick.UI.Startup;

internal static class AppLogging
{
    /// <summary>Daily log files kept for 14 days (10 MB each at most) plus the in-app log panel.</summary>
    public static Logger Create(AppPaths paths, LogBuffer buffer, LoggingLevelSwitch levelSwitch) =>
        new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Async(sink => sink.File(
                Path.Combine(paths.LogsDirectory, "autoclick-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Sink(buffer)
            .CreateLogger();
}

/// <summary>Product information read from the assembly.</summary>
public static class AppInfo
{
    /// <summary>"2.0.0" (build metadata after '+' removed).</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var assembly = typeof(AppInfo).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "0.0.0";
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
