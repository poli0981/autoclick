namespace AutoClick.Core.Models;

/// <summary>Checks the pixel under a point against its reference color before clicking.</summary>
public sealed record PixelGuardOptions
{
    public bool Enabled { get; init; }

    /// <summary>Allowed difference per RGB channel.</summary>
    public int Tolerance { get; init; } = 10;

    public ColorMismatchBehavior MismatchBehavior { get; init; } = ColorMismatchBehavior.StopSession;

    /// <summary>How long <see cref="ColorMismatchBehavior.WaitUntilMatch"/> waits before skipping.</summary>
    public int WaitTimeoutMs { get; init; } = 5000;

    public PixelGuardOptions Normalize() => this with
    {
        Tolerance = Math.Clamp(Tolerance, Limits.MinColorTolerance, Limits.MaxColorTolerance),
        MismatchBehavior = Enum.IsDefined(MismatchBehavior) ? MismatchBehavior : ColorMismatchBehavior.StopSession,
        WaitTimeoutMs = Math.Clamp(WaitTimeoutMs, Limits.MinColorWaitTimeoutMs, Limits.MaxColorWaitTimeoutMs),
    };
}

/// <summary>Helpers for Win32 COLORREF values (0x00BBGGRR).</summary>
public static class ColorRef
{
    public static bool Matches(uint actual, uint expected, int tolerance)
    {
        if (actual == ClickPoint.NoColor || expected == ClickPoint.NoColor)
        {
            return false;
        }

        return Math.Abs(R(actual) - R(expected)) <= tolerance
            && Math.Abs(G(actual) - G(expected)) <= tolerance
            && Math.Abs(B(actual) - B(expected)) <= tolerance;
    }

    /// <summary>"#RRGGBB", or "N/A" for <see cref="ClickPoint.NoColor"/>.</summary>
    public static string ToHex(uint color) =>
        color == ClickPoint.NoColor ? "N/A" : $"#{R(color):X2}{G(color):X2}{B(color):X2}";

    public static int R(uint color) => (int)(color & 0xFF);

    public static int G(uint color) => (int)((color >> 8) & 0xFF);

    public static int B(uint color) => (int)((color >> 16) & 0xFF);
}
