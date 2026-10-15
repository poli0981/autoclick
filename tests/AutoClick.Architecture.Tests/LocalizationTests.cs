using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AutoClick.Architecture.Tests;

/// <summary>
/// Every UI string comes from Strings.resx, every language has every string with the same
/// placeholders, and no unused strings pile up (v1 had 13 unused keys and ~25 untranslated texts).
/// </summary>
public sealed partial class LocalizationTests
{
    private static readonly string ResourcesDirectory = Path.Combine(RepositoryFiles.UiProject, "Resources");

    private static readonly IReadOnlyDictionary<string, string> Neutral = ReadResx(Path.Combine(ResourcesDirectory, "Strings.resx"));

    /// <summary>Product and brand names that are not translated.</summary>
    private static readonly HashSet<string> UntranslatedTexts =
    [
        "AutoClick", "GitHub", "GitHub Sponsors", "Discord", "X", "YouTube", "Bluesky", "Mastodon", "Steam",
        "Patreon", "Ko-fi", "HH:mm", "…", "LICENSE.txt", "THIRD-PARTY-NOTICES.txt",
    ];

    public static TheoryData<string> Languages => ["vi", "ja", "ko", "zh"];

    [Theory]
    [MemberData(nameof(Languages))]
    public void Translation_HasEveryKeyWithTheSamePlaceholders(string language)
    {
        var translated = ReadResx(Path.Combine(ResourcesDirectory, $"Strings.{language}.resx"));

        Assert.Empty(Neutral.Keys.Except(translated.Keys));
        Assert.Empty(translated.Keys.Except(Neutral.Keys));
        foreach (var (key, text) in Neutral)
        {
            Assert.False(string.IsNullOrWhiteSpace(translated[key]), $"{language}: {key} is empty");
            Assert.True(Placeholders(text).SetEquals(Placeholders(translated[key])), $"{language}: {key} has different placeholders");
        }
    }

    [Fact]
    public void EveryKeyUsedByTheUi_Exists()
    {
        // Any string literal shaped like one of our keys ("Group_Name" with a known group) counts,
        // so a typo in a key passed indirectly (switch expressions, tray menu items) is caught too.
        var groups = Neutral.Keys.Select(Group).ToHashSet(StringComparer.Ordinal);
        var missing = UsedKeys().Where(key => groups.Contains(Group(key)) && !Neutral.ContainsKey(key)).Order().ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void EveryKeyInTheResources_IsUsed()
    {
        var used = UsedKeys();
        var unused = Neutral.Keys.Where(key => !used.Contains(key)).Order().ToList();
        Assert.Empty(unused);
    }

    [Fact]
    public void Xaml_HasNoHardCodedText()
    {
        var hardCoded = new List<string>();
        foreach (var file in RepositoryFiles.SourceFiles(RepositoryFiles.UiProject, ".xaml"))
        {
            foreach (Match match in TextAttribute().Matches(File.ReadAllText(file)))
            {
                var value = match.Groups["value"].Value;
                if (!value.StartsWith('{') && !UntranslatedTexts.Contains(value) && value.Any(char.IsLetter))
                {
                    hardCoded.Add($"{Path.GetFileName(file)}: {match.Groups["name"].Value}=\"{value}\"");
                }
            }
        }

        Assert.Empty(hardCoded);
    }

    [Fact]
    public void LegacyDiscordInvite_IsGone()
    {
        // The "General" server was retired; only the games server (kDM9GMu5vm) is linked.
        var retiredInvite = "discord.gg/" + "2aNR3aVt";
        var offenders = RepositoryFiles.SourceFiles(RepositoryFiles.Root, ".cs", ".xaml", ".md", ".yml", ".yaml", ".json", ".resx", ".txt")
            .Where(file => File.ReadAllText(file).Contains(retiredInvite, StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(RepositoryFiles.Root, file))
            .ToList();
        Assert.Empty(offenders);
    }

    private static HashSet<string> UsedKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in RepositoryFiles.SourceFiles(RepositoryFiles.UiProject, ".cs", ".xaml"))
        {
            var text = File.ReadAllText(file);
            foreach (var pattern in new[] { KeyLiteral(), XamlKey() })
            {
                foreach (Match match in pattern.Matches(text))
                {
                    keys.Add(match.Groups["key"].Value);
                }
            }
        }

        return keys;
    }

    private static string Group(string key) => key[..key.IndexOf('_', StringComparison.Ordinal)];

    private static HashSet<string> Placeholders(string text) =>
        Placeholder().Matches(text).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

    private static Dictionary<string, string> ReadResx(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(e => (string)e.Attribute("name")!, e => (string?)e.Element("value") ?? string.Empty, StringComparer.Ordinal);

    /// <summary>A string literal shaped like a key: <c>"Group_Name"</c>.</summary>
    [GeneratedRegex(@"""(?<key>[A-Z][A-Za-z]*_[A-Za-z0-9_]+)""")]
    private static partial Regex KeyLiteral();

    [GeneratedRegex(@"\{l:Loc (?<key>[A-Za-z0-9_]+)\}")]
    private static partial Regex XamlKey();

    [GeneratedRegex(@"\{\d+(?:[,:][^}]*)?\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"\s(?<name>Text|Content|Header|ToolTip|Title|PlaceholderText|Message|Description|AutomationProperties\.Name)=""(?<value>[^""]*)""")]
    private static partial Regex TextAttribute();
}
