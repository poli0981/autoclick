using System.Diagnostics.CodeAnalysis;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using AutoClick.Services.Engine;
using Microsoft.Extensions.Time.Testing;

namespace AutoClick.Services.Tests;

public class ClickEngineTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));
    private readonly FakeInputSender _input = new();
    private readonly FakeWindowInspector _window = new();
    private readonly FakePixelReader _pixels = new();

    [SuppressMessage("Security", "S2245:Pseudorandom number generators (PRNGs) should not be used in security contexts", Justification = "Seeded Random keeps the tests reproducible.")]
    private ClickEngine CreateEngine() =>
        new(_input, _window, _pixels, _time, TestLog.For<ClickEngine>(), () => new Random(7));

    private static SessionStartInfo Info(params ClickPoint[] points) => new()
    {
        SessionId = "s1",
        Window = 0x1000,
        ProcessId = 42,
        ProcessName = "game",
        Points = points,
        Timing = new ClickTiming { Mode = ClickMode.Fixed, FixedIntervalSeconds = 10 },
        Jitter = false,
    };

    /// <summary>Advances fake time in small steps until <paramref name="condition"/> holds.</summary>
    private async Task RunUntil(Func<bool> condition, TimeSpan? step = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition not reached in time.");
            _time.Advance(step ?? TimeSpan.FromMilliseconds(5));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition not reached in time.");
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task RunsEveryStep_ThenWaitsTheIntervalBeforeTheNextRun()
    {
        var stats = new SessionStats();
        using var session = CreateEngine().Start(Info(
            new ClickPoint { X = 10, Y = 20, DelayAfterMs = 100 },
            new ClickPoint { X = 30, Y = 40, ClickType = ClickType.RightClick }), stats);

        await RunUntil(() => _input.Sent.Count == 2);
        Assert.Equal([new SentInput("click", 10, 20, ClickType.LeftClick, 0), new SentInput("click", 30, 40, ClickType.RightClick, 0)], _input.Sent);

        _time.Advance(TimeSpan.FromSeconds(9));
        await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal(2, _input.Sent.Count);

        await RunUntil(() => _input.Sent.Count == 4, TimeSpan.FromMilliseconds(100));
        Assert.Equal(4, stats.Clicks);
        Assert.Equal(TimeSpan.FromSeconds(10), stats.LastInterval);

        await session.StopAsync();
        Assert.Equal(SessionState.Stopped, session.State);
        Assert.Equal(StopReason.User, session.StopReason);
    }

    [Fact]
    public async Task Keystroke_SendsItsKey_AndStepsWithoutAKeyAreSkipped()
    {
        var stats = new SessionStats();
        using var session = CreateEngine().Start(Info(
            new ClickPoint { ClickType = ClickType.Keystroke, VirtualKeyCode = 0x20 },
            new ClickPoint { ClickType = ClickType.Keystroke, VirtualKeyCode = 0 },
            new ClickPoint { X = 5, Y = 5 }), stats);

        await RunUntil(() => _input.Sent.Count == 2);

        Assert.Equal(new SentInput("key", 0, 0, ClickType.Keystroke, 0x20), _input.Sent.First());
        Assert.Equal(2, stats.Clicks);
        await session.StopAsync();
    }

    [Fact]
    public async Task MinimizedGame_WaitsInsteadOfStopping()
    {
        _window.Status = WindowStatus.Minimized;
        var events = new List<bool>();
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), new SessionStats());
        session.StateChanged += (_, e) =>
        {
            lock (events)
            {
                events.Add(e.IsWaitingForWindow);
            }
        };

        await RunUntil(() => session.IsWaitingForWindow, TimeSpan.FromMilliseconds(100));
        Assert.Empty(_input.Sent);
        Assert.Equal(SessionState.Running, session.State);

        _window.Status = WindowStatus.Alive;
        await RunUntil(() => _input.Sent.Count == 1, TimeSpan.FromMilliseconds(100));
        Assert.False(session.IsWaitingForWindow);
        lock (events)
        {
            Assert.Contains(false, events);
        }
        await session.StopAsync();
    }

    [Fact]
    public async Task ClosedGame_StopsWithWindowClosed()
    {
        _window.Status = WindowStatus.Gone;
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), new SessionStats());

        await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(StopReason.WindowClosed, session.StopReason);
    }

    [Fact]
    public async Task AccessDenied_StopsWithElevationRequired_AndCountsNothing()
    {
        _input.DefaultResult = InputResult.AccessDenied;
        var stats = new SessionStats();
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), stats);

        await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(StopReason.ElevationRequired, session.StopReason);
        Assert.Equal(0, stats.Clicks);
    }

    [Fact]
    public async Task TargetNotResponding_StopsAfterRepeatedFailures()
    {
        _input.DefaultResult = InputResult.TargetNotResponding;
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), new SessionStats());

        await RunUntil(() => session.State == SessionState.Stopped, TimeSpan.FromSeconds(5));

        Assert.Equal(StopReason.TargetNotResponding, session.StopReason);
    }

    [Fact]
    public async Task TransientFailures_AreRetried_OnlyARunOfFailuresStops()
    {
        _input.Script(InputResult.Failed, InputResult.Failed, InputResult.Failed, InputResult.Failed);
        var stats = new SessionStats();
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), stats);

        // Four failures, then a success that resets the count: still running.
        await RunUntil(() => stats.Clicks == 1, TimeSpan.FromSeconds(1));
        Assert.Equal(SessionState.Running, session.State);

        _input.DefaultResult = InputResult.Failed;
        await RunUntil(() => session.State == SessionState.Stopped, TimeSpan.FromSeconds(1));
        Assert.Equal(StopReason.Error, session.StopReason);
        Assert.Equal(1, stats.Clicks);
    }

    [Fact]
    public async Task PointOutsideTheWindow_StopsWithOutOfBounds()
    {
        _window.Width = 100;
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 150, Y = 10 }), new SessionStats());

        await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(StopReason.OutOfBounds, session.StopReason);
        Assert.Empty(_input.Sent);
    }

    [Fact]
    public async Task PixelGuard_SkipPoint_CountsSkipsAndDoesNotClick()
    {
        var stats = new SessionStats();
        var info = Info(new ClickPoint { X = 1, Y = 1, ReferenceColor = 0x00FFFFFE }) with
        {
            PixelGuard = new PixelGuardOptions { Enabled = true, Tolerance = 2, MismatchBehavior = ColorMismatchBehavior.SkipPoint },
        };
        using var session = CreateEngine().Start(info, stats);

        await RunUntil(() => stats.Skipped >= 1);

        Assert.Empty(_input.Sent);
        Assert.Equal(SessionState.Running, session.State);
        await session.StopAsync();
    }

    [Fact]
    public async Task PixelGuard_StopSession_StopsWithColorMismatch()
    {
        var info = Info(new ClickPoint { X = 1, Y = 1, ReferenceColor = 0x00FFFFFE }) with
        {
            PixelGuard = new PixelGuardOptions { Enabled = true, MismatchBehavior = ColorMismatchBehavior.StopSession },
        };
        using var session = CreateEngine().Start(info, new SessionStats());

        await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(StopReason.ColorMismatch, session.StopReason);
    }

    [Fact]
    public async Task PixelGuard_WaitUntilMatch_ClicksOnceThePixelMatches()
    {
        var stats = new SessionStats();
        var info = Info(new ClickPoint { X = 1, Y = 1, ReferenceColor = 0x00ABCDEF }) with
        {
            PixelGuard = new PixelGuardOptions { Enabled = true, MismatchBehavior = ColorMismatchBehavior.WaitUntilMatch, WaitTimeoutMs = 5000 },
        };
        using var session = CreateEngine().Start(info, stats);

        _time.Advance(TimeSpan.FromMilliseconds(500));
        await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Empty(_input.Sent);

        _pixels.Color = 0x00ABCDEF;
        await RunUntil(() => _input.Sent.Count == 1, TimeSpan.FromMilliseconds(50));
        Assert.Equal(0, stats.Skipped);
        await session.StopAsync();
    }

    [Fact]
    public async Task PixelGuard_WaitUntilMatch_SkipsAfterTheTimeout()
    {
        var stats = new SessionStats();
        var info = Info(new ClickPoint { X = 1, Y = 1, ReferenceColor = 0x00ABCDEF }) with
        {
            PixelGuard = new PixelGuardOptions { Enabled = true, MismatchBehavior = ColorMismatchBehavior.WaitUntilMatch, WaitTimeoutMs = 300 },
        };
        using var session = CreateEngine().Start(info, stats);

        await RunUntil(() => stats.Skipped == 1, TimeSpan.FromMilliseconds(50));

        Assert.Empty(_input.Sent);
        await session.StopAsync();
    }

    [Fact]
    public async Task PauseAndResume()
    {
        var stats = new SessionStats();
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }) with { Timing = new ClickTiming { Mode = ClickMode.Fixed, FixedIntervalSeconds = 1 } }, stats);
        await RunUntil(() => stats.Clicks == 1);

        session.Pause();
        Assert.Equal(SessionState.Paused, session.State);
        _time.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var whilePaused = stats.Clicks;
        _time.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(whilePaused, stats.Clicks);

        session.Resume();
        Assert.Equal(SessionState.Running, session.State);
        await RunUntil(() => stats.Clicks > whilePaused, TimeSpan.FromMilliseconds(100));
        await session.StopAsync();
    }

    [Fact]
    public async Task StoppingAPausedSession_Works()
    {
        using var session = CreateEngine().Start(Info(new ClickPoint { X = 1, Y = 1 }), new SessionStats());
        session.Pause();

        await session.StopAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(StopReason.User, session.StopReason);
        await session.StopAsync();
    }

    [Fact]
    public async Task StartingTheSameSessionTwice_IsRejected_UntilItStops()
    {
        var engine = CreateEngine();
        var info = Info(new ClickPoint { X = 1, Y = 1 });
        using var first = engine.Start(info, new SessionStats());

        Assert.Throws<InvalidOperationException>(() => engine.Start(info, new SessionStats()));

        await first.StopAsync();
        await WaitFor(() => !engine.IsRunning(info.SessionId));
        using var second = engine.Start(info, new SessionStats());
        await second.StopAsync();
    }

    [Fact]
    public async Task Jitter_KeepsClicksInsideTheClientArea()
    {
        var stats = new SessionStats();
        var info = Info(new ClickPoint { X = 0, Y = 0 }, new ClickPoint { X = 1279, Y = 719 }) with
        {
            Jitter = true,
            Timing = new ClickTiming { Mode = ClickMode.Fixed, FixedIntervalSeconds = 1 },
        };
        using var session = CreateEngine().Start(info, stats);

        await RunUntil(() => _input.Sent.Count >= 20, TimeSpan.FromMilliseconds(250));

        Assert.All(_input.Sent, s => Assert.InRange(s.X, 0, 1279));
        Assert.All(_input.Sent, s => Assert.InRange(s.Y, 0, 719));
        Assert.Equal([(0, 0), (1279, 719)], stats.GetHeatmap().Select(p => (p.X, p.Y)).Order());
        await session.StopAsync();
    }

    [Fact]
    public void Start_WithoutSteps_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateEngine().Start(Info(), new SessionStats()));
    }
}
