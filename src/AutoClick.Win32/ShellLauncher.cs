using System.ComponentModel;
using System.Diagnostics;
using AutoClick.Core.Abstractions;

namespace AutoClick.Win32;

/// <summary>Opens URLs, files and folders with their default handler.</summary>
public sealed class ShellLauncher(IElevationService elevation)
{
    /// <summary>
    /// When AutoClick runs elevated, the target is opened through Explorer so the browser or
    /// editor does not inherit administrator rights.
    /// </summary>
    public bool TryOpen(string target)
    {
        try
        {
            var startInfo = elevation.IsCurrentProcessElevated
                ? new ProcessStartInfo("explorer.exe") { ArgumentList = { target }, UseShellExecute = false }
                : new ProcessStartInfo(target) { UseShellExecute = true };
            using var process = Process.Start(startInfo);
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
