using System.Net;
using AutoClick.Core.Scheduling;
using AutoClick.Services.Logging;
using AutoClick.Services.Scheduling;
using AutoClick.Services.Updates;
using Microsoft.Extensions.Time.Testing;
using Serilog.Events;
using Serilog.Parsing;

namespace AutoClick.Services.Tests;

public class SchedulerAndLoggingTests
{
    private static (ClickScheduler Scheduler, FakeTimeProvider Time) CreateScheduler()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        return (new ClickScheduler(time, TestLog.For<ClickScheduler>()), time);
    }

    [Fact]
    public void Scheduler_FiresStartAndStopOnce()
    {
        var (scheduler, time) = CreateScheduler();
        var starts = 0;
        var stops = 0;
        scheduler.StartRequested += (_, _) => starts++;
        scheduler.StopRequested += (_, _) => stops++;

        Assert.Equal(ScheduleError.None, scheduler.Arm("10:05", "10:07"));
        scheduler.Tick();
        Assert.Equal(TimeSpan.FromMinutes(5), scheduler.TimeUntilNextAction());

        time.Advance(TimeSpan.FromMinutes(5));
        scheduler.Tick();
        scheduler.Tick();
        Assert.Equal(1, starts);
        Assert.True(scheduler.HasStarted);
        Assert.Equal(TimeSpan.FromMinutes(2), scheduler.TimeUntilNextAction());

        time.Advance(TimeSpan.FromMinutes(2));
        scheduler.Tick();
        Assert.Equal(1, stops);
        Assert.False(scheduler.IsArmed);
    }

    [Fact]
    public void Scheduler_WithoutStop_DisarmsAfterStarting()
    {
        var (scheduler, time) = CreateScheduler();
        scheduler.Arm("10:01", null);

        time.Advance(TimeSpan.FromMinutes(1));
        scheduler.Tick();

        Assert.False(scheduler.IsArmed);
    }

    [Fact]
    public void Scheduler_MissedWhileAsleep_FiresOnTheNextTick()
    {
        var (scheduler, time) = CreateScheduler();
        var starts = 0;
        scheduler.StartRequested += (_, _) => starts++;
        scheduler.Arm("10:05", null);

        time.Advance(TimeSpan.FromHours(3));
        scheduler.Tick();

        Assert.Equal(1, starts);
    }

    [Theory]
    [InlineData("25:00", null, ScheduleError.InvalidHour)]
    [InlineData("10:00", "10:00", ScheduleError.StopEqualsStart)]
    [InlineData("", null, ScheduleError.Empty)]
    [InlineData("10:00", "9:7", ScheduleError.InvalidFormat)]
    public void Scheduler_RejectsInvalidInput(string start, string? stop, ScheduleError expected)
    {
        var (scheduler, _) = CreateScheduler();

        Assert.Equal(expected, scheduler.Arm(start, stop));
        Assert.False(scheduler.IsArmed);
    }

    [Fact]
    public void LogBuffer_DrainsPendingAndKeepsBoundedHistory()
    {
        var buffer = new LogBuffer();
        for (var i = 0; i < LogBuffer.HistoryCapacity + 10; i++)
        {
            buffer.Emit(Event(LogEventLevel.Warning, $"line {i}"));
        }

        var pending = buffer.TakePending();

        Assert.Equal(LogBuffer.HistoryCapacity + 10, pending.Count);
        Assert.Empty(buffer.TakePending());
        Assert.Equal(LogBuffer.HistoryCapacity, buffer.Snapshot().Count);
        Assert.Equal("line 10", buffer.Snapshot()[0].Message);
        Assert.Equal(LogLineLevel.Warning, pending[0].Level);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, UpdateError.NoRelease)]
    [InlineData(HttpStatusCode.Forbidden, UpdateError.RateLimited)]
    [InlineData(HttpStatusCode.TooManyRequests, UpdateError.RateLimited)]
    [InlineData(HttpStatusCode.Unauthorized, UpdateError.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway, UpdateError.ServerError)]
    [InlineData(HttpStatusCode.BadRequest, UpdateError.Network)]
    public void Update_ClassifiesHttpErrors(HttpStatusCode status, UpdateError expected)
    {
        Assert.Equal(expected, UpdateService.Classify(new HttpRequestException("x", null, status)));
    }

    [Fact]
    public void Update_ClassifiesOtherErrors()
    {
        Assert.Equal(UpdateError.Network, UpdateService.Classify(new TaskCanceledException()));
        Assert.Equal(UpdateError.Unknown, UpdateService.Classify(new InvalidOperationException()));
    }

    private static LogEvent Event(LogEventLevel level, string text) =>
        new(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse(text), []);
}
