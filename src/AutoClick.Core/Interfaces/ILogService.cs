namespace AutoClick.Core.Interfaces;

public interface ILogService
{
    event Action<string>? LogReceived;
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
    void ExportLog(string filePath);
}
