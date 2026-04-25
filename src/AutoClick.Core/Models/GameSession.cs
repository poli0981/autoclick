using System.Collections.ObjectModel;
using AutoClick.Core.Enums;

namespace AutoClick.Core.Models;

public class GameSession
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public IntPtr WindowHandle { get; set; }
    public SessionState State { get; set; } = SessionState.Idle;
    public ObservableCollection<ClickPoint> ClickPoints { get; set; } = new();
    public ClickProfile Profile { get; set; } = new();
    public long ClickCount { get; set; }
    public long SkippedClicks { get; set; }
    public double LastIntervalSeconds { get; set; }
    public DateTime? StartedAt { get; set; }

    // Pixel Color Guard (populated from AppSettings when session is created)
    public bool EnablePixelColorGuard { get; set; }
    public int ColorTolerance { get; set; } = 10;
    public ColorMismatchBehavior ColorMismatchBehavior { get; set; }
    public int ColorWaitTimeoutMs { get; set; } = 5000;

    public override string ToString() => $"{ProcessName} ({State})";
}