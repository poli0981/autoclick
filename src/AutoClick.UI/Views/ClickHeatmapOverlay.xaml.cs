using System.Windows;
using System.Windows.Controls;
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
/// </summary>
public partial class ClickHeatmapOverlay : Window
{
    private readonly GameSession _session;
    private readonly DispatcherTimer _timer;
    private bool _closed;

    public ClickHeatmapOverlay(GameSession session)
    {
        InitializeComponent();
        _session = session;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Refresh();
        _timer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _timer.Stop();
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
