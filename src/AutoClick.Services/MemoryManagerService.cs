using System;
using System.Diagnostics;
using AutoClick.Core.Interfaces;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.Services;

public class MemoryManagerService : IMemoryManager
{
    public long GetCurrentMemoryUsageMb()
    {
        // Use PrivateMemorySize64 to match Task Manager's "Memory (Private Working Set)"
        using var proc = Process.GetCurrentProcess();
        return proc.PrivateMemorySize64 / (1024 * 1024);
    }

    public void ForceCleanup()
    {
        GC.Collect(2, GCCollectionMode.Aggressive, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Aggressive, true);

        SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
    }

    public bool IsOverLimit(long limitMb)
    {
        return GetCurrentMemoryUsageMb() > limitMb;
    }
}
