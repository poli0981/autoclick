using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class ClickPoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public ClickType ClickType { get; set; } = ClickType.LeftClick;

    /// <summary>
    /// Delay in milliseconds AFTER this point is clicked, before moving to the next point.
    /// 0 = no extra delay (the main profile interval still applies after the full sequence).
    /// </summary>
    public int DelayAfterMs { get; set; }

    public ClickPoint() { }

    public ClickPoint(int x, int y, ClickType clickType = ClickType.LeftClick, int delayAfterMs = 0)
    {
        X = x;
        Y = y;
        ClickType = clickType;
        DelayAfterMs = delayAfterMs;
    }

    public override string ToString()
    {
        var suffix = DelayAfterMs > 0 ? $" +{DelayAfterMs}ms" : "";
        return $"({X}, {Y}){suffix}";
    }
}
