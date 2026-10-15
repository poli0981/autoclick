namespace AutoClick.Architecture.Tests;

/// <summary>Locates the repository checkout the tests were built from.</summary>
internal static class RepositoryFiles
{
    public static string Root { get; } = FindRoot();

    public static string UiProject => Path.Combine(Root, "src", "AutoClick.UI");

    /// <summary>Source files of a project, without build output.</summary>
    public static IEnumerable<string> SourceFiles(string directory, params string[] extensions) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(path));

    /// <summary>Build output, tool folders (.git, .vs, .claude worktrees, ...) and generated files.</summary>
    private static bool IsBuildOutput(string path)
    {
        var parts = Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => p is "bin" or "obj" or "node_modules" or "artifacts" or "TestResults" or "publish" or "Releases"
            || (p.StartsWith('.') && p is not ".github" and not ".config"));
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoClick.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("AutoClick.slnx not found above " + AppContext.BaseDirectory);
    }
}
