using System.Globalization;
using System.Text;

namespace AutoClick.Services.Persistence;

/// <summary>File writes that never leave a half-written file behind.</summary>
internal static class AtomicFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Writes to a temporary file, flushes it to disk, then renames it over the target.</summary>
    public static void WriteAllText(string path, string contents)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, Utf8NoBom))
        {
            writer.Write(contents);
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Moves an unreadable file aside (name.corrupt-yyyyMMdd-HHmmss) and returns the new path.</summary>
    public static string? MoveAside(string path, DateTimeOffset now)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var target = $"{path}.corrupt-{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
        File.Move(path, target, overwrite: true);
        return target;
    }
}
