using System.Collections.Concurrent;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace AutoClick.Services.Logging;

public enum LogLineLevel
{
    Debug,
    Information,
    Warning,
    Error,
}

public readonly record struct LogLine(DateTimeOffset Timestamp, LogLineLevel Level, string Message)
{
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Timestamp:HH:mm:ss} [{Level.ToString()[..3].ToUpperInvariant()}] {Message}");
}

/// <summary>
/// Serilog sink that keeps recent log lines for the in-app log panel. Writers never block:
/// the UI drains new lines in batches with <see cref="TakePending"/>.
/// </summary>
public sealed class LogBuffer : ILogEventSink
{
    public const int HistoryCapacity = 2000;
    private const int PendingCapacity = 5000;

    // ":lj" renders string values without quotes: AutoClick 2.0.0 rather than AutoClick "2.0.0".
    private static readonly MessageTemplateTextFormatter Formatter = new("{Message:lj}", CultureInfo.InvariantCulture);

    private readonly ConcurrentQueue<LogLine> _pending = new();
    private readonly Queue<LogLine> _history = new(HistoryCapacity);
    private readonly Lock _historyLock = new();

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        Formatter.Format(logEvent, writer);
        var message = writer.ToString();
        if (logEvent.Exception is { } exception)
        {
            message = $"{message} ({exception.GetType().Name}: {exception.Message})";
        }

        var line = new LogLine(logEvent.Timestamp, Map(logEvent.Level), message);

        // A UI that stopped draining (e.g. minimized for hours) must not grow memory forever.
        if (_pending.Count < PendingCapacity)
        {
            _pending.Enqueue(line);
        }

        lock (_historyLock)
        {
            if (_history.Count == HistoryCapacity)
            {
                _history.Dequeue();
            }

            _history.Enqueue(line);
        }
    }

    /// <summary>Lines logged since the previous call.</summary>
    public IReadOnlyList<LogLine> TakePending()
    {
        var lines = new List<LogLine>();
        while (_pending.TryDequeue(out var line))
        {
            lines.Add(line);
        }

        return lines;
    }

    /// <summary>The most recent lines (for "Export log").</summary>
    public IReadOnlyList<LogLine> Snapshot()
    {
        lock (_historyLock)
        {
            return _history.ToList();
        }
    }

    private static LogLineLevel Map(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose or LogEventLevel.Debug => LogLineLevel.Debug,
        LogEventLevel.Information => LogLineLevel.Information,
        LogEventLevel.Warning => LogLineLevel.Warning,
        _ => LogLineLevel.Error,
    };
}
