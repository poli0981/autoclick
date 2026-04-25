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

    /// <summary>
    /// Reference pixel color (COLORREF 0x00BBGGRR) captured when coordinate was picked.
    /// 0xFFFFFFFF means no color was captured (sentinel value).
    /// </summary>
    public uint ReferenceColor { get; set; } = 0xFFFFFFFF;
    public bool HasReferenceColor => ReferenceColor != 0xFFFFFFFF;

    /// <summary>
    /// Virtual-key code (VK_*) sent when ClickType is Keystroke. Ignored otherwise.
    /// 0 means no key set.
    /// </summary>
    public int VirtualKeyCode { get; set; }

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
