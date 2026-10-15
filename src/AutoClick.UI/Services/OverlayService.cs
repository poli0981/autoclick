using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using AutoClick.UI.Overlays;

namespace AutoClick.UI.Services;

/// <summary>Owns the coordinate picker and the per-game heatmap overlays.</summary>
public sealed class OverlayService
{
    private readonly Dictionary<string, HeatmapOverlay> _heatmaps = [];
    private readonly IWindowInspector _inspector;
    private readonly IPixelReader _pixels;

    public OverlayService(UiTicker ticker, IWindowInspector inspector, IPixelReader pixels)
    {
        _inspector = inspector;
        _pixels = pixels;
        ticker.Tick += (_, _) =>
        {
            foreach (var overlay in _heatmaps.Values.ToList())
            {
                overlay.Refresh();
            }
        };
    }

    /// <summary>Raised when a heatmap closes by itself (e.g. its game window is gone).</summary>
    public event EventHandler<string>? HeatmapClosed;

    public bool IsHeatmapOpen(string sessionId) => _heatmaps.ContainsKey(sessionId);

    public void ToggleHeatmap(string sessionId, nint window, int processId, SessionStats stats)
    {
        if (_heatmaps.ContainsKey(sessionId))
        {
            CloseHeatmap(sessionId);
            return;
        }

        var overlay = new HeatmapOverlay(window, processId, stats, _inspector);
        overlay.Closed += (_, _) =>
        {
            if (_heatmaps.Remove(sessionId))
            {
                HeatmapClosed?.Invoke(this, sessionId);
            }
        };
        _heatmaps[sessionId] = overlay;
        overlay.Show();
    }

    public void CloseHeatmap(string sessionId)
    {
        if (_heatmaps.TryGetValue(sessionId, out var overlay))
        {
            overlay.Close();
        }
    }

    public void CloseAll()
    {
        foreach (var id in _heatmaps.Keys.ToList())
        {
            CloseHeatmap(id);
        }
    }

    /// <param name="window">The game window.</param>
    /// <param name="captureColor">Store the pixel color under the point (pixel color guard).</param>
    public Task<ClickPoint?> PickPointAsync(nint window, bool captureColor) =>
        CoordinatePickerOverlay.PickAsync(window, captureColor ? _pixels : null);
}
