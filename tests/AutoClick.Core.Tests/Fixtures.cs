namespace AutoClick.Core.Tests;

/// <summary>Files under tests/Fixtures (copied next to the test assembly).</summary>
internal static class Fixtures
{
    public static string Read(params string[] relativePath) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Fixtures", .. relativePath]));
}
