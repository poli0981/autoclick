using System.ComponentModel;
using System.Diagnostics;
using AutoClick.Core.Abstractions;

namespace AutoClick.Win32;

/// <summary>Opens URLs, files and folders with their default handler.</summary>
public sealed class ShellLauncher(IElevationService elevation)
{
    // A full path: a bare "explorer.exe" is searched in AutoClick's own folder first, which
    // the user can write to, and would then run with administrator rights.
    private static string ExplorerPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

    /// <summary>
    /// When AutoClick runs elevated, the target is opened through Explorer so the browser or
    /// editor does not inherit administrator rights.
    /// </summary>
    public bool TryOpen(string target)
    {
        try
        {
            using var process = Process.Start(StartInfo(target, elevation.IsCurrentProcessElevated));
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    internal static ProcessStartInfo StartInfo(string target, bool elevated) =>
        elevated
            ? new ProcessStartInfo(ExplorerPath) { ArgumentList = { target }, UseShellExecute = false }
            : new ProcessStartInfo(target) { UseShellExecute = true };
}
