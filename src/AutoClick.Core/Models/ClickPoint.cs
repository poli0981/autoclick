using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class ClickPoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public ClickType ClickType { get; set; } = ClickType.LeftClick;

    public ClickPoint() { }

    public ClickPoint(int x, int y, ClickType clickType = ClickType.LeftClick)
    {
        X = x;
        Y = y;
        ClickType = clickType;
    }

    public override string ToString() => $"({X}, {Y}) [{ClickType}]";
}
