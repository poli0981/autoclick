using static AutoClick.Win32.NativeMethods;

namespace AutoClick.UI.Views;

/// <summary>
/// Process-wide foreground-window watcher. Subscribers receive the new foreground
/// HWND whenever it changes. The underlying WinEvent hook is installed lazily on
/// first subscribe and torn down when the last subscriber leaves, so when no
/// heatmap overlays are open the app pays no syscall cost. Callbacks fire on
/// the thread that installed the hook (the UI thread) because we use
/// WINEVENT_OUTOFCONTEXT — subscribers can touch WPF state directly.
/// </summary>
internal static class ForegroundWatcher
{
    private static readonly object _gate = new();
    private static readonly List<Action<IntPtr>> _subscribers = new();
    private static IntPtr _hook;
    // Hold the delegate in a field so the GC doesn't collect it while the
    // unmanaged hook still holds a function pointer to it.
    private static WinEventDelegate? _callback;

    public static void Subscribe(Action<IntPtr> handler)
    {
        lock (_gate)
        {
            _subscribers.Add(handler);
            if (_hook == IntPtr.Zero)
            {
                _callback = OnForegroundChanged;
                _hook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);
            }
        }
    }

    public static void Unsubscribe(Action<IntPtr> handler)
    {
        lock (_gate)
        {
            _subscribers.Remove(handler);
            if (_subscribers.Count == 0 && _hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
                _callback = null;
            }
        }
    }

    private static void OnForegroundChanged(IntPtr hook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // EVENT_SYSTEM_FOREGROUND can fire for non-window objects (menus, popups);
        // ignore those — we only care about top-level window foreground swaps.
        if (idObject != OBJID_WINDOW) return;

        System.Diagnostics.Trace.WriteLine(
            $"[ForegroundWatcher] fg-changed hwnd=0x{hwnd.ToInt64():X} subs={_subscribers.Count}");

        Action<IntPtr>[] copy;
        lock (_gate) copy = _subscribers.ToArray();
        foreach (var handler in copy)
        {
            try { handler(hwnd); }
            catch { /* swallow per-subscriber failure so siblings still fire */ }
        }
    }
}
