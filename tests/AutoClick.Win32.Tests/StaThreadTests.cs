namespace AutoClick.Win32.Tests;

public class StaThreadTests
{
    [Fact]
    public void Run_TestCodeThrows_RethrowsTheSameException()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => StaThread.Run(() => throw new InvalidOperationException("boom")));

        Assert.Equal("boom", failure.Message);
    }
}
