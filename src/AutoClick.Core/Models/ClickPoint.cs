using System.Text.Json.Serialization;

namespace AutoClick.Core.Models;

/// <summary>
/// One step of a click sequence. Immutable: use <c>with</c> to change a copy, so no code path
/// can forget to copy a field (v1 lost the keystroke key when copying points).
/// </summary>
/// <remarks>The property names are the v1 JSON contract (profiles, sessions): do not rename.</remarks>
public sealed record ClickPoint
{
    /// <summary>COLORREF sentinel meaning "no reference color captured".</summary>
    public const uint NoColor = 0xFFFFFFFF;

    /// <summary>Client X coordinate in the target window's own coordinate space.</summary>
    public int X { get; init; }

    /// <summary>Client Y coordinate in the target window's own coordinate space.</summary>
    public int Y { get; init; }

    public ClickType ClickType { get; init; } = ClickType.LeftClick;

    /// <summary>Delay after this step, before the next step of the sequence.</summary>
    public int DelayAfterMs { get; init; }

    /// <summary>Reference pixel color (COLORREF 0x00BBGGRR) for the pixel color guard.</summary>
    public uint ReferenceColor { get; init; } = NoColor;

    /// <summary>Virtual-key code sent when <see cref="ClickType"/> is Keystroke.</summary>
    public int VirtualKeyCode { get; init; }

    /// <summary>Monitor DPI of the target window when the point was picked (null for v1 data).</summary>
    public int? CaptureDpi { get; init; }

    [JsonIgnore]
    public bool HasReferenceColor => ReferenceColor != NoColor;

    [JsonIgnore]
    public bool IsKeystroke => ClickType == ClickType.Keystroke;

    /// <summary>Returns a copy with every value inside its valid range.</summary>
    public ClickPoint Normalize() => this with
    {
        X = IsKeystroke ? 0 : Math.Clamp(X, 0, Limits.MaxCoordinate),
        Y = IsKeystroke ? 0 : Math.Clamp(Y, 0, Limits.MaxCoordinate),
        ClickType = Enum.IsDefined(ClickType) ? ClickType : ClickType.LeftClick,
        DelayAfterMs = Limits.ClampSequenceDelay(DelayAfterMs),
        VirtualKeyCode = IsKeystroke ? Math.Clamp(VirtualKeyCode, 0, 0xFE) : 0,
        CaptureDpi = CaptureDpi is > 0 and <= 1536 ? CaptureDpi : null,
    };
}
