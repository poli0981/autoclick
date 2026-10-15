using AutoClick.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace AutoClick.Services.Scheduling;

/// <summary>
/// One-shot "start all at HH:mm, optionally stop at HH:mm". Driven by <see cref="Tick"/> (the
/// UI calls it every second) and compared against the wall clock, so a missed timer during
/// sleep or hibernation still fires as soon as the machine wakes.
/// </summary>
public sealed class ClickScheduler(TimeProvider timeProvider, ILogger<ClickScheduler> logger)
{
    public ScheduleWindow? Window { get; private set; }

    public bool IsArmed => Window is not null;

    /// <summary>True once the start time has passed and StartRequested was raised.</summary>
    public bool HasStarted { get; private set; }

    public event EventHandler? StartRequested;

    public event EventHandler? StopRequested;

    /// <summary>Raised when the schedule is armed, fired or cancelled.</summary>
    public event EventHandler? Changed;

    public ScheduleError Arm(string startText, string? stopText)
    {
        var startError = ScheduleWindow.TryParseTime(startText, out var start);
        if (startError != ScheduleError.None)
        {
            return startError;
        }

        TimeOnly? stop = null;
        if (!string.IsNullOrWhiteSpace(stopText))
        {
            var stopError = ScheduleWindow.TryParseTime(stopText, out var stopTime);
            if (stopError != ScheduleError.None)
            {
                return stopError;
            }

            if (stopTime == start)
            {
                return ScheduleError.StopEqualsStart;
            }

            stop = stopTime;
        }

        Window = ScheduleWindow.Create(start, stop, timeProvider.GetLocalNow().DateTime);
        HasStarted = false;
        logger.LogInformation("Schedule armed: start {Start:g}, stop {Stop:g}", Window.StartAt, Window.StopAt);
        Changed?.Invoke(this, EventArgs.Empty);
        return ScheduleError.None;
    }

    public void Cancel()
    {
        if (Window is null)
        {
            return;
        }

        Window = null;
        HasStarted = false;
        logger.LogInformation("Schedule cancelled");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Time left until the next scheduled action, or null when nothing is pending.</summary>
    public TimeSpan? TimeUntilNextAction()
    {
        if (Window is null)
        {
            return null;
        }

        var now = timeProvider.GetLocalNow().DateTime;
        var next = HasStarted ? Window.StopAt : Window.StartAt;
        return next is { } at ? (at > now ? at - now : TimeSpan.Zero) : null;
    }

    /// <summary>Fires due actions. Call periodically (about once per second).</summary>
    public void Tick()
    {
        var window = Window;
        if (window is null)
        {
            return;
        }

        var now = timeProvider.GetLocalNow().DateTime;
        if (!HasStarted && now >= window.StartAt)
        {
            HasStarted = true;
            logger.LogInformation("Scheduled start reached");
            StartRequested?.Invoke(this, EventArgs.Empty);
            if (window.StopAt is null)
            {
                Window = null;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        if (HasStarted && window.StopAt is { } stopAt && now >= stopAt)
        {
            Window = null;
            HasStarted = false;
            logger.LogInformation("Scheduled stop reached");
            StopRequested?.Invoke(this, EventArgs.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
