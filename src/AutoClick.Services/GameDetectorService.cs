using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
using AutoClick.Win32;

namespace AutoClick.Services;

public class GameDetectorService : IGameDetector
{
    public List<GameWindowInfo> GetRunningWindows()
    {
        return WindowHelper.GetVisibleWindows()
            .Where(w => !IsSystemWindow(w))
            .OrderBy(w => w.ProcessName)
            .ToList();
    }

    public bool IsProcessAlive(int processId) => WindowHelper.IsProcessRunning(processId);

    public bool IsWindowValid(IntPtr handle) => WindowHelper.IsWindowStillValid(handle);

    private static bool IsSystemWindow(GameWindowInfo w)
    {
        var ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer", "SearchHost", "ShellExperienceHost", "StartMenuExperienceHost",
            "TextInputHost", "SystemSettings", "ApplicationFrameHost", "LockApp",
            "ScreenClippingHost", "CompPkgSrv", "dwm"
        };
        return ignore.Contains(w.ProcessName);
    }
}
