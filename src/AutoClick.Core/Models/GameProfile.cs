using System;
using System.Collections.Generic;

namespace AutoClick.Core.Models;

public class GameProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<ClickPoint> ClickPoints { get; set; } = new();
    public ClickProfile ClickSettings { get; set; } = new();
    public int SequenceDelayMs { get; set; }
}
