namespace AutoClick.Core.Enums;

public enum ClickType
{
    LeftClick = 0,
    DoubleClick = 1,
    RightClick = 2,
    /// <summary>
    /// Send a virtual-key keystroke (WM_KEYDOWN/WM_KEYUP) to the window instead of a mouse click.
    /// X/Y are ignored; ClickPoint.VirtualKeyCode holds the VK_* code.
    /// </summary>
    Keystroke = 3
}
