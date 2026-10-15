using System.Globalization;

namespace AutoClick.Core.Scheduling;

public enum ScheduleError
{
    None,
    Empty,
    InvalidFormat,
    InvalidHour,
    InvalidMinute,

    /// <summary>The stop time equals the start time.</summary>
    StopEqualsStart,
}

/// <summary>
/// One scheduled run: start at the next occurrence of a time of day, optionally stop at the
/// next occurrence of another time after the start. A stop time at or before the start time
/// therefore means "the next day" (e.g. 23:00 → 01:00).
/// </summary>
public sealed record ScheduleWindow(DateTime StartAt, DateTime? StopAt)
{
    public bool StopsNextDay => StopAt is { } stop && stop.Date > StartAt.Date;

    public static ScheduleWindow Create(TimeOnly start, TimeOnly? stop, DateTime now)
    {
        var startAt = now.Date + start.ToTimeSpan();
        if (startAt <= now)
        {
            startAt = startAt.AddDays(1);
        }

        DateTime? stopAt = null;
        if (stop is { } stopTime)
        {
            var candidate = startAt.Date + stopTime.ToTimeSpan();
            if (candidate <= startAt)
            {
                candidate = candidate.AddDays(1);
            }

            stopAt = candidate;
        }

        return new ScheduleWindow(startAt, stopAt);
    }

    /// <summary>Parses "H:mm" or "HH:mm" (24-hour clock).</summary>
    public static ScheduleError TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return ScheduleError.Empty;
        }

        var parts = text.Trim().Split(':');
        if (parts.Length != 2 || parts[0].Length is < 1 or > 2 || parts[1].Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
        {
            return ScheduleError.InvalidFormat;
        }

        if (hours > 23)
        {
            return ScheduleError.InvalidHour;
        }

        if (minutes > 59)
        {
            return ScheduleError.InvalidMinute;
        }

        time = new TimeOnly(hours, minutes);
        return ScheduleError.None;
    }
}
