namespace AutoClick.Core.Models;

public class GameWindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int ProcessId { get; set; }

    public override string ToString() => $"{ProcessName} - {Title}";
}
