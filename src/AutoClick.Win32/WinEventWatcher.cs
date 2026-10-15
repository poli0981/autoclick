using Windows.Win32;
using Windows.Win32.UI.Accessibility;

namespace AutoClick.Win32;

/// <summary>
/// Out-of-context WinEvent hook. Callbacks arrive on the thread that created the watcher
/// (it must pump messages, e.g. the WPF UI thread).
/// </summary>
public sealed class WinEventWatcher : IDisposable
{
    private const uint WinEventOutOfContext = 0x0000;
    private const int ObjIdWindow = 0;

    // The unmanaged hook calls through this delegate: keep it referenced while hooked.
    private readonly WINEVENTPROC _callback;
    private readonly UnhookWinEventSafeHandle _hook;

    private WinEventWatcher(uint eventId, uint processId, Action<nint> onEvent, nint onlyWindow)
    {
        _callback = (_, _, hwnd, idObject, _, _, _) =>
        {
            if (idObject != ObjIdWindow || (onlyWindow != 0 && (nint)hwnd != onlyWindow))
            {
                return;
            }

            onEvent((nint)hwnd);
        };

        _hook = PInvoke.SetWinEventHook(eventId, eventId, null, _callback, processId, 0, WinEventOutOfContext);
    }

    public bool IsActive => !_hook.IsInvalid;

    /// <summary>Raised whenever any window becomes the foreground window.</summary>
    public static WinEventWatcher Foreground(Action<nint> onForegroundChanged) =>
        new(PInvoke.EVENT_SYSTEM_FOREGROUND, 0, onForegroundChanged, 0);

    /// <summary>Raised when <paramref name="window"/> (owned by <paramref name="processId"/>) moves or resizes.</summary>
    public static WinEventWatcher LocationChange(nint window, int processId, Action onMoved) =>
        new(PInvoke.EVENT_OBJECT_LOCATIONCHANGE, (uint)processId, _ => onMoved(), window);

    public void Dispose()
    {
        _hook.Dispose();
        GC.KeepAlive(_callback);
    }
}
