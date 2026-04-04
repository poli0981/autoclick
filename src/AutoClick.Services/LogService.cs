using System.IO;
using AutoClick.Core.Interfaces;
using Serilog;
using Serilog.Core;

namespace AutoClick.Services;

public class LogService : ILogService
{
    private readonly Logger _fileLogger;
    private readonly List<string> _logBuffer = new();
    private readonly object _lock = new();

    public event Action<string>? LogReceived;

    public LogService()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoClick", "logs");
        Directory.CreateDirectory(logDir);

        _fileLogger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(logDir, "autoclick-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public void Info(string message)
    {
        var formatted = $"[{DateTime.Now:HH:mm:ss}] [INF] {message}";
        _fileLogger.Information(message);
        AddToBuffer(formatted);
    }

    public void Warn(string message)
    {
        var formatted = $"[{DateTime.Now:HH:mm:ss}] [WRN] {message}";
        _fileLogger.Warning(message);
        AddToBuffer(formatted);
    }

    public void Error(string message, Exception? ex = null)
    {
        var formatted = $"[{DateTime.Now:HH:mm:ss}] [ERR] {message}";
        if (ex != null)
            _fileLogger.Error(ex, message);
        else
            _fileLogger.Error(message);
        AddToBuffer(formatted);
    }

    public void ExportLog(string filePath)
    {
        lock (_lock)
        {
            File.WriteAllLines(filePath, _logBuffer);
        }
    }

    private void AddToBuffer(string entry)
    {
        lock (_lock)
        {
            _logBuffer.Add(entry);
            if (_logBuffer.Count > 10000)
                _logBuffer.RemoveRange(0, 5000);
        }
        LogReceived?.Invoke(entry);
    }
}
