using System.IO;
using System.Text.Json;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;

namespace AutoClick.Services;

public class SessionExportService : ISessionExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Export(string filePath, SessionExport payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public SessionExport Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SessionExport>(json, JsonOptions)
               ?? throw new InvalidOperationException("Invalid session file (deserialized to null).");
    }
}
