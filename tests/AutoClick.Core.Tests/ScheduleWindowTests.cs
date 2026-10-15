using AutoClick.Core.Scheduling;

namespace AutoClick.Core.Tests;

public class ScheduleWindowTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 10, 30, 0, DateTimeKind.Local);

    [Theory]
    [InlineData("9:05", 9, 5)]
    [InlineData("09:05", 9, 5)]
    [InlineData(" 23:59 ", 23, 59)]
    [InlineData("0:00", 0, 0)]
    public void TryParseTime_Valid(string text, int hour, int minute)
    {
        Assert.Equal(ScheduleError.None, ScheduleWindow.TryParseTime(text, out var time));
        Assert.Equal(new TimeOnly(hour, minute), time);
    }

    [Theory]
    [InlineData(null, ScheduleError.Empty)]
    [InlineData("  ", ScheduleError.Empty)]
    [InlineData("9", ScheduleError.InvalidFormat)]
    [InlineData("9:5", ScheduleError.InvalidFormat)]
    [InlineData("123:00", ScheduleError.InvalidFormat)]
    [InlineData("-1:00", ScheduleError.InvalidFormat)]
    [InlineData("ab:cd", ScheduleError.InvalidFormat)]
    [InlineData("24:00", ScheduleError.InvalidHour)]
    [InlineData("12:60", ScheduleError.InvalidMinute)]
    public void TryParseTime_Invalid(string? text, ScheduleError expected)
    {
        Assert.Equal(expected, ScheduleWindow.TryParseTime(text, out _));
    }

    [Fact]
    public void Create_StartLaterToday_StartsToday()
    {
        var window = ScheduleWindow.Create(new TimeOnly(11, 0), null, Now);

        Assert.Equal(Now.Date.AddHours(11), window.StartAt);
        Assert.Null(window.StopAt);
    }

    [Fact]
    public void Create_StartAlreadyPassed_StartsTomorrow()
    {
        var window = ScheduleWindow.Create(new TimeOnly(10, 30), null, Now);

        Assert.Equal(Now.Date.AddDays(1).AddHours(10.5), window.StartAt);
    }

    [Fact]
    public void Create_StopAfterStart_SameDay()
    {
        var window = ScheduleWindow.Create(new TimeOnly(11, 0), new TimeOnly(13, 0), Now);

        Assert.Equal(Now.Date.AddHours(13), window.StopAt);
        Assert.False(window.StopsNextDay);
    }

    [Fact]
    public void Create_StopBeforeStart_StopsTheNextDay()
    {
        var window = ScheduleWindow.Create(new TimeOnly(23, 0), new TimeOnly(1, 0), Now);

        Assert.Equal(Now.Date.AddHours(23), window.StartAt);
        Assert.Equal(Now.Date.AddDays(1).AddHours(1), window.StopAt);
        Assert.True(window.StopsNextDay);
    }

    [Fact]
    public void Create_StartTomorrowAndStopBeforeIt_StopIsTheDayAfter()
    {
        // 09:00 has passed today, so the run starts tomorrow; 08:00 is then the day after.
        var window = ScheduleWindow.Create(new TimeOnly(9, 0), new TimeOnly(8, 0), Now);

        Assert.Equal(Now.Date.AddDays(1).AddHours(9), window.StartAt);
        Assert.Equal(Now.Date.AddDays(2).AddHours(8), window.StopAt);
    }
}
