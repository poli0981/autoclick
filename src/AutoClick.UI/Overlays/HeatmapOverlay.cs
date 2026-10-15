using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Dpi;
using AutoClick.Core.Sessions;
using AutoClick.UI.Localization;
using AutoClick.Win32;

namespace AutoClick.UI.Overlays;

/// <summary>
/// Click-through, top-most heatmap over a game's client area. It is shown only while that game
/// (or one of its dialogs) is the foreground window, follows the game when it moves (WinEvent
/// location hook, with the once-per-second refresh as a fallback) and redraws only when the
/// clicks, the position or the DPI change.
/// </summary>
public sealed class HeatmapOverlay : Window
{
    private readonly nint _target;
    private readonly int _processId;
    private readonly SessionStats _stats;
    private readonly IWindowInspector _inspector;
    private readonly HeatmapLayer _layer = new();
    private readonly TextBlock _legend = new() { Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 11 };
    private WinEventWatcher? _foregroundWatcher;
    private WinEventWatcher? _locationWatcher;
    private PixelRect _bounds;
    private int _drawnVersion = -1;

    // Show() makes the window visible; OnContentRendered then hides it unless the game is in front.
    private bool _visible = true;
    private bool _repositionQueued;

    public HeatmapOverlay(nint target, int processId, SessionStats stats, IWindowInspector inspector)
    {
        _target = target;
        _processId = processId;
        _stats = stats;
        _inspector = inspector;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        ResizeMode = ResizeMode.NoResize;
        Title = "AutoClick heatmap";

        var legend = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 12, 12, 0),
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x29, 0x3B)),
            Child = _legend,
        };
        var root = new Grid();
        root.Children.Add(_layer);
        root.Children.Add(legend);
        Content = root;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        WindowGeometry.MakeClickThrough(hwnd);
        _foregroundWatcher = WinEventWatcher.Foreground(_ => UpdateVisibility());
        _locationWatcher = WinEventWatcher.LocationChange(_target, _processId, QueueReposition);
        Reposition(force: true);
        Redraw(force: true);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UpdateVisibility();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Reposition(force: true);
        Redraw(force: true);
    }

    protected override void OnClosed(EventArgs e)
    {
        _foregroundWatcher?.Dispose();
        _locationWatcher?.Dispose();
        base.OnClosed(e);
    }

    /// <summary>Once-per-second fallback: position, visibility and new clicks.</summary>
    public void Refresh()
    {
        if (_inspector.GetStatus(_target, _processId) == WindowStatus.Gone)
        {
            Close();
            return;
        }

        Reposition(force: false);
        UpdateVisibility();
        Redraw(force: false);
    }

    private void QueueReposition()
    {
        // Location events arrive in bursts while dragging: handle at most one per frame.
        if (_repositionQueued)
        {
            return;
        }

        _repositionQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _repositionQueued = false;
            Reposition(force: false);
        }, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void Reposition(bool force)
    {
        if (WindowGeometry.IsMinimized(_target) || !WindowGeometry.TryGetPhysicalClientRect(_target, out var rect))
        {
            SetVisible(false);
            return;
        }

        if (!force && rect == _bounds)
        {
            return;
        }

        _bounds = rect;
        WindowGeometry.SetBounds(new WindowInteropHelper(this).Handle, rect, topmost: _visible);
        Redraw(force: true);
    }

    private void UpdateVisibility() =>
        SetVisible(WindowGeometry.IsForeground(_target) && !WindowGeometry.IsMinimized(_target));

    private void SetVisible(bool visible)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0 || visible == _visible)
        {
            return;
        }

        _visible = visible;
        WindowGeometry.Show(hwnd, visible);
        if (visible)
        {
            // Re-assert top-most once when the game comes to the front (many games are top-most too).
            WindowGeometry.SetBounds(hwnd, _bounds, topmost: true);
        }
    }

    private void Redraw(bool force)
    {
        var version = _stats.HeatmapVersion;
        if (!force && version == _drawnVersion)
        {
            return;
        }

        _drawnVersion = version;
        var points = _stats.GetHeatmap();
        var total = points.Sum(p => (long)p.Count);
        var max = points.Count == 0 ? 0 : points.Max(p => p.Count);
        _legend.Text = L.Format("Heatmap_Legend", total, points.Count, max);

        if (points.Count == 0 || _bounds.IsEmpty || !_inspector.TryGetClientSize(_target, out var width, out var height))
        {
            _layer.SetPoints([]);
            return;
        }

        var scale = DpiMath.ClientScale(width, height, _bounds);
        var dpi = VisualTreeHelper.GetDpi(this);
        var markers = points.Select(p =>
        {
            var (x, y) = DpiMath.ToPhysicalOffset(p.X, p.Y, scale);
            var intensity = max <= 1 ? 1.0 : (double)p.Count / max;
            return new HeatmapLayer.Marker(new Point(DpiMath.ToDips(x, dpi.PixelsPerInchX), DpiMath.ToDips(y, dpi.PixelsPerInchY)), intensity);
        }).ToList();
        _layer.SetPoints(markers);
    }
}

/// <summary>Draws the heatmap markers in one pass (no element per marker).</summary>
public sealed class HeatmapLayer : FrameworkElement
{
    private static readonly Brush[] Palette = BuildPalette();
    private IReadOnlyList<Marker> _markers = [];

    public HeatmapLayer() => IsHitTestVisible = false;

    public readonly record struct Marker(Point Center, double Intensity);

    public void SetPoints(IReadOnlyList<Marker> markers)
    {
        _markers = markers;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        foreach (var marker in _markers)
        {
            var t = Math.Clamp(marker.Intensity, 0, 1);
            var radius = (12 + (28 * t)) / 2;
            drawingContext.DrawEllipse(Palette[(int)Math.Round(t * (Palette.Length - 1))], null, marker.Center, radius, radius);
        }
    }

    /// <summary>Blue (cool) to red (hot), frozen once.</summary>
    private static Brush[] BuildPalette()
    {
        var cool = Color.FromArgb(180, 30, 100, 255);
        var hot = Color.FromArgb(220, 255, 60, 30);
        var palette = new Brush[16];
        for (var i = 0; i < palette.Length; i++)
        {
            var t = i / (double)(palette.Length - 1);
            var brush = new SolidColorBrush(Color.FromArgb(
                (byte)(cool.A + ((hot.A - cool.A) * t)),
                (byte)(cool.R + ((hot.R - cool.R) * t)),
                (byte)(cool.G + ((hot.G - cool.G) * t)),
                (byte)(cool.B + ((hot.B - cool.B) * t))));
            brush.Freeze();
            palette[i] = brush;
        }

        return palette;
    }
}
