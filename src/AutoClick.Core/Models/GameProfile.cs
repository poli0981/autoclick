using System.Text.RegularExpressions;

namespace AutoClick.Core.Models;

/// <summary>A saved click sequence and timing that can be loaded into any game.</summary>
/// <remarks>Property names are the v1 profile JSON contract: do not rename.</remarks>
public sealed partial record GameProfile
{
    public string Id { get; init; } = NewId();
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<ClickPoint> ClickPoints { get; init; } = [];
    public ClickTiming ClickSettings { get; init; } = new();
    public int SequenceDelayMs { get; init; } = Limits.DefaultSequenceDelayMs;

    public const int MaxNameLength = 100;

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Profile ids become file names, so only short lowercase alphanumerics are accepted.
    /// Anything else (e.g. "..\..\x" from an imported file) is rejected.
    /// </summary>
    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    /// <summary>Clamps every value and replaces an invalid id with a new one.</summary>
    public GameProfile Normalize()
    {
        var name = (Name ?? string.Empty).Trim();
        return this with
        {
            Id = IsValidId(Id) ? Id : NewId(),
            Name = name.Length > MaxNameLength ? name[..MaxNameLength] : name,
            ClickPoints = (ClickPoints ?? []).Select(p => p.Normalize()).ToList(),
            ClickSettings = (ClickSettings ?? new ClickTiming()).Normalize(),
            SequenceDelayMs = Limits.ClampSequenceDelay(SequenceDelayMs),
        };
    }

    [GeneratedRegex("^[a-z0-9]{8,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
}
