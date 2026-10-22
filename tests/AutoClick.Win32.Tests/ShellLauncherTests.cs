using System.IO;
using AutoClick.Core.Abstractions;

namespace AutoClick.Win32.Tests;

public class ShellLauncherTests
{
    [Fact]
    public void StartInfo_Elevated_StartsExplorerByItsFullPath()
    {
        var startInfo = ShellLauncher.StartInfo("https://example.com", elevated: true);

        // A bare "explorer.exe" would be searched in AutoClick's own, user-writable folder first.
        Assert.True(Path.IsPathFullyQualified(startInfo.FileName));
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.Windows), Path.GetDirectoryName(startInfo.FileName), ignoreCase: true);
        Assert.Equal("explorer.exe", Path.GetFileName(startInfo.FileName), ignoreCase: true);
        Assert.True(File.Exists(startInfo.FileName));
        Assert.Equal("https://example.com", Assert.Single(startInfo.ArgumentList));
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void TryOpen_MissingFile_ReturnsFalse()
    {
        // Not elevated: the default handler reports the missing file without showing any UI.
        var launcher = new ShellLauncher(new NotElevated());

        Assert.False(launcher.TryOpen(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.autoclick-missing")));
    }

    private sealed class NotElevated : IElevationService
    {
        public bool IsCurrentProcessElevated => false;

        public ElevationState GetRelativeElevation(int processId) => ElevationState.SameOrLower;
    }
}
