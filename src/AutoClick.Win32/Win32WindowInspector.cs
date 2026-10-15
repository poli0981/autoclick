using AutoClick.Core.Abstractions;
using AutoClick.Win32.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AutoClick.Win32;

/// <summary>Finds game windows and checks whether a target window is still usable.</summary>
public sealed class Win32WindowInspector(IElevationService elevation) : IWindowInspector
{
    /// <summary>Shell and system processes that are never click targets.</summary>
    private static readonly HashSet<string> IgnoredProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "SearchHost", "ShellExperienceHost", "StartMenuExperienceHost", "TextInputHost",
        "ApplicationFrameHost", "LockApp", "ScreenClippingHost", "CompPkgSrv", "dwm", "ShellHost",
    };

    public IReadOnlyList<WindowInfo> GetCandidateWindows()
    {
        var handles = new List<HWND>();
        PInvoke.EnumWindows((hwnd, _) =>
        {
            handles.Add(hwnd);
            return true;
        }, default);

        var ownProcessId = (uint)Environment.ProcessId;
        var windows = new List<WindowInfo>();
        foreach (var hwnd in handles)
        {
            if (!IsCandidate(hwnd))
            {
                continue;
            }

            PInvoke.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0 || processId == ownProcessId)
            {
                continue;
            }

            var title = GetTitle(hwnd);
            if (title.Length == 0)
            {
                continue;
            }

            var path = GetProcessImagePath(processId);
            var name = path.Length > 0 ? Path.GetFileNameWithoutExtension(path) : $"pid {processId}";
            if (IgnoredProcesses.Contains(name))
            {
                continue;
            }

            windows.Add(new WindowInfo((nint)hwnd, (int)processId, name, title, path, elevation.GetRelativeElevation((int)processId)));
        }

        return windows
            .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public WindowStatus GetStatus(nint window, int processId)
    {
        var hwnd = (HWND)window;
        if (!PInvoke.IsWindow(hwnd))
        {
            return WindowStatus.Gone;
        }

        // A handle can be recycled for another window after the game closes.
        PInvoke.GetWindowThreadProcessId(hwnd, out var owner);
        if (owner != (uint)processId)
        {
            return WindowStatus.Gone;
        }

        return PInvoke.IsIconic(hwnd) ? WindowStatus.Minimized : WindowStatus.Alive;
    }

    public bool TryGetClientSize(nint window, out int width, out int height)
    {
        using var scope = DpiScope.ForWindow(window);
        if (PInvoke.GetClientRect((HWND)window, out var rect))
        {
            width = rect.right - rect.left;
            height = rect.bottom - rect.top;
            return width > 0 && height > 0;
        }

        width = height = 0;
        return false;
    }

    private static bool IsCandidate(HWND hwnd)
    {
        if (!PInvoke.IsWindowVisible(hwnd) || PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER) != HWND.Null)
        {
            return false;
        }

        if ((WindowLong.Get(hwnd, WindowLong.GwlExStyle) & WindowLong.WsExToolWindow) != 0)
        {
            return false;
        }

        // UWP apps and windows on other virtual desktops are "visible" but cloaked.
        Span<byte> cloaked = stackalloc byte[4];
        var hr = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, cloaked);
        return hr.Failed || BitConverter.ToInt32(cloaked) == 0;
    }

    private static string GetTitle(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        Span<char> buffer = length < 512 ? stackalloc char[length + 1] : new char[length + 1];
        var copied = PInvoke.GetWindowText(hwnd, buffer);
        return new string(buffer[..copied]).Trim();
    }

    /// <summary>
    /// Works for elevated processes too (unlike Process.MainModule), because it only needs
    /// PROCESS_QUERY_LIMITED_INFORMATION.
    /// </summary>
    internal static string GetProcessImagePath(uint processId)
    {
        using var process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process.IsInvalid)
        {
            return string.Empty;
        }

        Span<char> buffer = stackalloc char[1024];
        var size = (uint)buffer.Length;
        return PInvoke.QueryFullProcessImageName(process, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, ref size)
            ? new string(buffer[..(int)size])
            : string.Empty;
    }
}
