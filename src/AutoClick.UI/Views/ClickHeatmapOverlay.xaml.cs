using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Threading;
using AutoClick.Core.Models;
using AutoClick.UI.Resources;
using static AutoClick.Win32.NativeMethods;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace AutoClick.UI.Views;

/// <summary>
/// Transparent always-on-top overlay aligned to a target game window's client area,
/// rendering a blue→red gradient circle at each clicked coordinate sized + colored
/// by relative frequency. Click-through (IsHitTestVisible=False).
///
/// Foreground-aware: the overlay is only visible when its target game window is
/// the system foreground window. This solves two problems at once:
///   1. With multiple games queued, only the active game's heatmap shows —
///      overlays don't pile up on top of each other.
///   2. When the user Alt-Tabs back into the game, the overlay is re-shown and
///      its z-order re-asserted to HWND_TOPMOST immediately (responding to the
///      WinEvent rather than waiting for the 200ms refresh tick), which is what
///      makes it stick on top of windowed games that also use topmost.
/// </summary>
public partial class ClickHeatmapOverlay : Window
{
    private readonly GameSession _session;
    private readonly DispatcherTimer _timer;
    private readonly Action<IntPtr> _foregroundHandler;
    private bool _closed;

    public ClickHeatmapOverlay(GameSession session)
    {
        InitializeComponent();
        _session = session;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => Refresh();
        _foregroundHandler = OnForegroundChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Promote to true OS-level click-through. WPF's IsHitTestVisible=False
        // only suppresses hit-testing inside the WPF tree; without WS_EX_TRANSPARENT
        // the OS still routes mouse input to this HWND and the underlying game
        // window never sees the click. WS_EX_NOACTIVATE prevents focus theft on
        // click-through; WS_EX_TOOLWINDOW removes the overlay from Alt-Tab.
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE,
            ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Refresh();
        _timer.Start();

        // Subscribe to foreground changes and apply the current state once.
        ForegroundWatcher.Subscribe(_foregroundHandler);
        UpdateVisibilityForForeground(GetForegroundWindow());
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _timer.Stop();
        ForegroundWatcher.Unsubscribe(_foregroundHandler);
    }

    private void OnForegroundChanged(IntPtr foregroundHwnd)
    {
        // The hook callback fires on the UI thread (WINEVENT_OUTOFCONTEXT) so we
        // can touch WPF state directly, but Dispatcher.BeginInvoke is cheap
        // insurance against the rare case where Windows delivers the event off
        // the install thread (e.g. during message pump re-entry).
        if (Dispatcher.CheckAccess()) UpdateVisibilityForForeground(foregroundHwnd);
        else Dispatcher.BeginInvoke(() => UpdateVisibilityForForeground(foregroundHwnd));
    }

    private void UpdateVisibilityForForeground(IntPtr foregroundHwnd)
    {
        if (_closed) return;

        var overlayHwnd = new WindowInteropHelper(this).Handle;
        if (overlayHwnd == IntPtr.Zero) return;

        // Match the foreground window directly OR via its top-level ancestor —
        // games that pop modal sub-windows (settings menus, dialogs) own those
        // sub-windows, so the foreground HWND is the child but the root is our
        // tracked game window.
        bool isOurGame = foregroundHwnd == _session.WindowHandle
            || GetAncestor(foregroundHwnd, GA_ROOT) == _session.WindowHandle;

        // Diagnostic: flows to the debugger output / DebugView so the heatmap
        // visibility decisions can be inspected without instrumenting the log
        // service. Cheap; cost-free in Release if listeners aren't attached.
        System.Diagnostics.Trace.WriteLine(
            $"[Heatmap {_session.ProcessName}] fg=0x{foregroundHwnd.ToInt64():X} " +
            $"game=0x{_session.WindowHandle.ToInt64():X} match={isOurGame}");

        // Toggle visibility through Win32 ShowWindow rather than WPF's Visibility
        // property: AllowsTransparency=True windows are known to misbehave on
        // Visibility cycle (the layered surface can stay hidden after Visible is
        // re-asserted). ShowWindow drives the HWND directly and is reliable.
        if (isOurGame)
        {
            ShowWindow(overlayHwnd, SW_SHOWNOACTIVATE);
            // Re-assert TOPMOST at the exact moment the game becomes foreground —
            // many games ship with their own WS_EX_TOPMOST flag, so we need to
            // jump above them right when the z-order swap happens.
            SetWindowPos(overlayHwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            ShowWindow(overlayHwnd, SW_HIDE);
        }
    }

    private void Refresh()
    {
        if (_closed) return;

        // If the target window is gone, close the overlay.
        if (!IsWindow(_session.WindowHandle))
        {
            Close();
            return;
        }

        // Align the overlay to the target window's client area in screen coordinates.
        if (!GetClientRect(_session.WindowHandle, out RECT clientRect))
        {
            Close();
            return;
        }
        var origin = new POINT(0, 0);
        ClientToScreen(_session.WindowHandle, ref origin);

        // Convert physical pixels to WPF DIPs (96 DPI baseline).
        var src = PresentationSource.FromVisual(this);
        double dpiScaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiScaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        Left = origin.X / dpiScaleX;
        Top = origin.Y / dpiScaleY;
        Width = Math.Max(1, clientRect.Width / dpiScaleX);
        Height = Math.Max(1, clientRect.Height / dpiScaleY);

        // Re-assert topmost z-order + re-check foreground match every tick.
        // The WinEvent hook fires the same logic instantly on foreground swap,
        // but polling is a cheap belt-and-suspenders fallback in case the hook
        // misses an event (Windows occasionally drops EVENT_SYSTEM_FOREGROUND
        // under heavy GPU load or when the game alt-tabs through compositor
        // changes). Idempotent — ShowWindow on already-correct state is a noop.
        UpdateVisibilityForForeground(GetForegroundWindow());

        // Snapshot the heatmap (ConcurrentDictionary).
        var snapshot = _session.ClickHeatmap.ToArray();
        if (snapshot.Length == 0)
        {
            MarkerCanvas.Children.Clear();
            LabelText.Text = $"{Strings.HeatmapTitle}: 0";
            return;
        }

        int maxCount = snapshot.Max(kv => kv.Value);
        long total = snapshot.Sum(kv => (long)kv.Value);
        LabelText.Text = $"{Strings.HeatmapTitle}: {total} ({snapshot.Length} pts, max {maxCount})";

        MarkerCanvas.Children.Clear();
        foreach (var (key, count) in snapshot)
        {
            // Normalized intensity 0..1
            double t = maxCount <= 1 ? 1.0 : (double)count / maxCount;
            // Blue (cool) → Red (hot) gradient via HSV-like blending.
            var color = LerpColor(Color.FromArgb(180, 30, 100, 255),
                                  Color.FromArgb(220, 255, 60, 30), t);

            // Diameter: 12..40 px scaled by intensity.
            double diameter = 12 + 28 * t;

            var ellipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Fill = new SolidColorBrush(color),
                IsHitTestVisible = false
            };
            // Center the ellipse on (X, Y), converting from physical pixels to DIPs.
            double xDip = key.X / dpiScaleX;
            double yDip = key.Y / dpiScaleY;
            Canvas.SetLeft(ellipse, xDip - diameter / 2);
            Canvas.SetTop(ellipse, yDip - diameter / 2);
            MarkerCanvas.Children.Add(ellipse);
        }
    }

    private static Color LerpColor(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte ca = (byte)(a.A + (b.A - a.A) * t);
        byte cr = (byte)(a.R + (b.R - a.R) * t);
        byte cg = (byte)(a.G + (b.G - a.G) * t);
        byte cb = (byte)(a.B + (b.B - a.B) * t);
        return Color.FromArgb(ca, cr, cg, cb);
    }
}
