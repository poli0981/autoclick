using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.HiDpi;

namespace AutoClick.Win32;

/// <summary>
/// Temporarily runs the calling thread in a given DPI awareness context.
/// </summary>
/// <remarks>
/// Click points live in the game window's own coordinate space. For a DPI-unaware game on a
/// scaled monitor that space is "logical", so every API call that reads or produces game
/// coordinates (client size, cursor position, screen-to-client) must run in the game's own
/// awareness context. The click engine moves between threads, so the scope is per call.
/// </remarks>
public readonly struct DpiScope : IDisposable
{
    private readonly nint _previous;

    private DpiScope(nint previous) => _previous = previous;

    /// <summary>Switches to the awareness of <paramref name="window"/> (no-op for invalid windows).</summary>
    public static DpiScope ForWindow(nint window)
    {
        var context = PInvoke.GetWindowDpiAwarenessContext((HWND)window);
        return context.IsNull ? default : new DpiScope(PInvoke.SetThreadDpiAwarenessContext(context));
    }

    /// <summary>DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 (a pseudo-handle, not in the CsWin32 metadata).</summary>
    private static readonly DPI_AWARENESS_CONTEXT PerMonitorAwareV2 = (DPI_AWARENESS_CONTEXT)new IntPtr(-4);

    /// <summary>Switches to per-monitor v2 awareness: every API then works in physical pixels.</summary>
    public static DpiScope PerMonitorV2() => new(PInvoke.SetThreadDpiAwarenessContext(PerMonitorAwareV2));

    public void Dispose()
    {
        if (_previous != 0)
        {
            PInvoke.SetThreadDpiAwarenessContext((DPI_AWARENESS_CONTEXT)_previous);
        }
    }
}
