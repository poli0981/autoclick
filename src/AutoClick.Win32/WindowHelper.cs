using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AutoClick.Core.Models;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Win32;

public static class WindowHelper
{
    public static List<GameWindowInfo> GetVisibleWindows()
    {
        var windows = new List<GameWindowInfo>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return true;

            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            GetWindowThreadProcessId(hWnd, out uint pid);

            try
            {
                var proc = Process.GetProcessById((int)pid);
                string? exePath = null;
                try { exePath = proc.MainModule?.FileName; } catch { }

                windows.Add(new GameWindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = proc.ProcessName,
                    ExecutablePath = exePath ?? string.Empty,
                    ProcessId = (int)pid
                });
            }
            catch
            {
                // Process may have exited
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static bool IsProcessRunning(int processId)
    {
        try
        {
            var proc = Process.GetProcessById(processId);
            return !proc.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsWindowStillValid(IntPtr handle) => IsWindow(handle);

    public static RECT GetWindowClientRect(IntPtr hWnd)
    {
        GetClientRect(hWnd, out RECT rect);
        return rect;
    }
}
