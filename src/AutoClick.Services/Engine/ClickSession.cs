using System.Globalization;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Dpi;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using Microsoft.Extensions.Logging;

namespace AutoClick.Services.Engine;

/// <summary>One running click loop for one game window. Dispose it after it has stopped.</summary>
public sealed class ClickSession : IDisposable
{
    internal static readonly TimeSpan WindowPollInterval = TimeSpan.FromMilliseconds(500);
    internal static readonly TimeSpan PixelPollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan MismatchLogInterval = TimeSpan.FromSeconds(30);
    private const int MaxConsecutiveFailures = 5;
    private const int JitterPixels = 2;

    private readonly SessionStartInfo _info;
    private readonly IInputSender _input;
    private readonly IWindowInspector _inspector;
    private readonly IPixelReader _pixels;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly Random _random;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly AsyncGate _gate = new();
    private readonly Lock _stateLock = new();

    private SessionState _state = SessionState.Running;
    private StopReason _reason;
    private string? _detail;
    private bool _waitingForWindow;
    private StopReason? _requestedStop;
    private int _consecutiveFailures;
    private long _lastMismatchLog;
    private int _unloggedMismatches;

    internal ClickSession(SessionStartInfo info, SessionStats stats, IInputSender input, IWindowInspector inspector,
        IPixelReader pixels, TimeProvider time, ILogger logger, Random random)
    {
        _info = info with
        {
            Points = info.Points.Select(p => p.Normalize()).ToList(),
            Timing = info.Timing.Normalize(),
            PixelGuard = info.PixelGuard.Normalize(),
        };
        Stats = stats;
        _input = input;
        _inspector = inspector;
        _pixels = pixels;
        _time = time;
        _logger = logger;
        _random = random;
    }

    public string SessionId => _info.SessionId;

    public SessionStats Stats { get; }

    /// <summary>Completes when the loop has exited (stopped for any reason).</summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

    public SessionState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    public StopReason StopReason
    {
        get
        {
            lock (_stateLock)
            {
                return _reason;
            }
        }
    }

    /// <summary>The game is minimized and the session waits for it to be restored.</summary>
    public bool IsWaitingForWindow
    {
        get
        {
            lock (_stateLock)
            {
                return _waitingForWindow;
            }
        }
    }

    /// <summary>Detail of the stop reason (e.g. the out-of-bounds point), if any.</summary>
    public string? StopDetail
    {
        get
        {
            lock (_stateLock)
            {
                return _detail;
            }
        }
    }

    /// <summary>
    /// Raised on a worker thread whenever the state (or waiting flag) changes. The session
    /// starts immediately, so read the current properties after subscribing.
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    internal void Start()
    {
        _logger.LogInformation("Auto-click started for {Process} ({Points} steps)", _info.ProcessName, _info.Points.Count);
        // Not the session token: a stop requested before the loop starts would cancel the task
        // without running it, and the session would never report that it stopped.
        Completion = Task.Run(RunAsync, CancellationToken.None);
    }

    public void Pause()
    {
        if (!TransitionFrom(SessionState.Running, SessionState.Paused))
        {
            return;
        }

        _gate.Close();
        _logger.LogInformation("Auto-click paused for {Process}", _info.ProcessName);
    }

    public void Resume()
    {
        if (!TransitionFrom(SessionState.Paused, SessionState.Running))
        {
            return;
        }

        _gate.Open();
        _logger.LogInformation("Auto-click resumed for {Process}", _info.ProcessName);
    }

    /// <summary>Stops the loop and waits for it to exit.</summary>
    public async Task StopAsync(StopReason reason = StopReason.User)
    {
        lock (_stateLock)
        {
            if (_state == SessionState.Stopped)
            {
                return;
            }

            _requestedStop ??= reason;
        }

        await _cancellation.CancelAsync().ConfigureAwait(false);
        _gate.Open();
        await Completion.ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Completion.IsCompleted)
        {
            _cancellation.Dispose();
        }
    }

    private async Task RunAsync()
    {
        var token = _cancellation.Token;
        try
        {
            while (true)
            {
                await _gate.WaitAsync(token).ConfigureAwait(false);

                switch (_inspector.GetStatus(_info.Window, _info.ProcessId))
                {
                    case WindowStatus.Gone:
                        Finish(StopReason.WindowClosed, null);
                        return;
                    case WindowStatus.Minimized:
                        SetWaiting(true);
                        await Delay(WindowPollInterval, token).ConfigureAwait(false);
                        continue;
                }

                SetWaiting(false);
                if (!_inspector.TryGetClientSize(_info.Window, out var width, out var height))
                {
                    await Delay(WindowPollInterval, token).ConfigureAwait(false);
                    continue;
                }

                if (!await RunSequenceAsync(width, height, token).ConfigureAwait(false))
                {
                    return;
                }

                var interval = _info.Timing.NextInterval(_random);
                Stats.RecordInterval(interval);
                await Delay(interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            StopReason reason;
            lock (_stateLock)
            {
                reason = _requestedStop ?? StopReason.User;
            }

            Finish(reason, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Click loop for {Process} failed", _info.ProcessName);
            Finish(StopReason.Error, ex.Message);
        }
    }

    /// <summary>Runs every step once. Returns false when the session must stop.</summary>
    private async Task<bool> RunSequenceAsync(int width, int height, CancellationToken token)
    {
        var points = _info.Points;
        for (var i = 0; i < points.Count; i++)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            var point = points[i];

            if (point.IsKeystroke)
            {
                if (point.VirtualKeyCode != 0)
                {
                    var keyResult = await _input.KeystrokeAsync(_info.Window, point.VirtualKeyCode, token).ConfigureAwait(false);
                    if (!Handle(keyResult))
                    {
                        return false;
                    }

                    if (keyResult == InputResult.Ok)
                    {
                        Stats.RecordKeystroke();
                    }
                }
            }
            else
            {
                if (!DpiMath.IsInsideClient(point.X, point.Y, width, height))
                {
                    // Language-neutral detail: the UI puts it into a translated sentence.
                    Finish(StopReason.OutOfBounds, string.Create(CultureInfo.InvariantCulture, $"({point.X}, {point.Y}) / {width}×{height}"));
                    return false;
                }

                if (_info.PixelGuard.Enabled && point.HasReferenceColor)
                {
                    var guard = await CheckPixelAsync(point, token).ConfigureAwait(false);
                    if (guard == GuardOutcome.Stop)
                    {
                        return false;
                    }

                    if (guard == GuardOutcome.Skip)
                    {
                        Stats.RecordSkip();
                        continue;
                    }
                }

                var target = _info.Jitter
                    ? DpiMath.ClampToClient(point.X + _random.Next(-JitterPixels, JitterPixels + 1), point.Y + _random.Next(-JitterPixels, JitterPixels + 1), width, height)
                    : new PixelPoint(point.X, point.Y);
                var clickResult = await _input.ClickAsync(_info.Window, target.X, target.Y, point.ClickType, token).ConfigureAwait(false);
                if (!Handle(clickResult))
                {
                    return false;
                }

                if (clickResult == InputResult.Ok)
                {
                    Stats.RecordClick(point.X, point.Y);
                }
            }

            if (i < points.Count - 1 && point.DelayAfterMs > 0)
            {
                await Delay(TimeSpan.FromMilliseconds(point.DelayAfterMs), token).ConfigureAwait(false);
            }
        }

        return true;
    }

    private enum GuardOutcome
    {
        Click,
        Skip,
        Stop,
    }

    private async Task<GuardOutcome> CheckPixelAsync(ClickPoint point, CancellationToken token)
    {
        var guard = _info.PixelGuard;
        var sample = _pixels.Read(_info.Window, point.X, point.Y);
        if (sample.IsValid && ColorRef.Matches(sample.Color, point.ReferenceColor, guard.Tolerance))
        {
            return GuardOutcome.Click;
        }

        switch (guard.MismatchBehavior)
        {
            case ColorMismatchBehavior.StopSession:
                Finish(StopReason.ColorMismatch, string.Create(CultureInfo.InvariantCulture,
                    $"({point.X}, {point.Y}) {ColorRef.ToHex(point.ReferenceColor)} → {ColorRef.ToHex(sample.Color)}"));
                return GuardOutcome.Stop;

            case ColorMismatchBehavior.WaitUntilMatch:
                var timeout = TimeSpan.FromMilliseconds(guard.WaitTimeoutMs);
                var started = _time.GetTimestamp();
                while (true)
                {
                    if (!_gate.IsOpen)
                    {
                        // Paused while waiting: wait for resume, then give the pixel a full timeout again.
                        await _gate.WaitAsync(token).ConfigureAwait(false);
                        started = _time.GetTimestamp();
                    }

                    if (_time.GetElapsedTime(started) >= timeout)
                    {
                        LogMismatch(point, sample, waited: true);
                        return GuardOutcome.Skip;
                    }

                    await Delay(PixelPollInterval, token).ConfigureAwait(false);
                    if (_inspector.GetStatus(_info.Window, _info.ProcessId) == WindowStatus.Gone)
                    {
                        Finish(StopReason.WindowClosed, null);
                        return GuardOutcome.Stop;
                    }

                    sample = _pixels.Read(_info.Window, point.X, point.Y);
                    if (sample.IsValid && ColorRef.Matches(sample.Color, point.ReferenceColor, guard.Tolerance))
                    {
                        return GuardOutcome.Click;
                    }
                }

            default:
                LogMismatch(point, sample, waited: false);
                return GuardOutcome.Skip;
        }
    }

    /// <summary>Logs at most one mismatch line per interval, with a count of the ones in between.</summary>
    private void LogMismatch(ClickPoint point, PixelSample sample, bool waited)
    {
        var now = _time.GetTimestamp();
        if (_lastMismatchLog != 0 && _time.GetElapsedTime(_lastMismatchLog, now) < MismatchLogInterval)
        {
            _unloggedMismatches++;
            return;
        }

        _logger.LogInformation(
            "Color mismatch for {Process} at ({X}, {Y}): expected {Expected}, got {Actual}; point skipped{Waited} ({Suppressed} similar since last report)",
            _info.ProcessName, point.X, point.Y, ColorRef.ToHex(point.ReferenceColor), sample.IsValid ? ColorRef.ToHex(sample.Color) : "unreadable",
            waited ? " after waiting" : string.Empty, _unloggedMismatches);
        _lastMismatchLog = now;
        _unloggedMismatches = 0;
    }

    /// <summary>Handles a send result. Returns false when the session has to stop.</summary>
    private bool Handle(InputResult result)
    {
        switch (result)
        {
            case InputResult.Ok:
                _consecutiveFailures = 0;
                return true;
            case InputResult.AccessDenied:
                Finish(StopReason.ElevationRequired, null);
                return false;
            case InputResult.WindowGone:
                Finish(StopReason.WindowClosed, null);
                return false;
            default:
                if (++_consecutiveFailures >= MaxConsecutiveFailures)
                {
                    Finish(result == InputResult.TargetNotResponding ? StopReason.TargetNotResponding : StopReason.Error,
                        $"{_consecutiveFailures} × {result}");
                    return false;
                }

                _logger.LogWarning("Sending input to {Process} failed ({Result}); retrying", _info.ProcessName, result);
                return true;
        }
    }

    private Task Delay(TimeSpan delay, CancellationToken token) => Task.Delay(delay, _time, token);

    private void SetWaiting(bool waiting)
    {
        SessionStateChangedEventArgs? args = null;
        lock (_stateLock)
        {
            if (_waitingForWindow != waiting && _state != SessionState.Stopped)
            {
                _waitingForWindow = waiting;
                args = new SessionStateChangedEventArgs(_state, _reason, _detail, waiting);
            }
        }

        if (args is null)
        {
            return;
        }

        if (waiting)
        {
            _logger.LogInformation("{Process} is minimized; waiting until it is restored", _info.ProcessName);
        }
        else
        {
            _logger.LogInformation("{Process} is visible again; resuming clicks", _info.ProcessName);
        }

        StateChanged?.Invoke(this, args);
    }

    private bool TransitionFrom(SessionState from, SessionState to)
    {
        SessionStateChangedEventArgs args;
        lock (_stateLock)
        {
            if (_state != from)
            {
                return false;
            }

            _state = to;
            args = new SessionStateChangedEventArgs(to, _reason, _detail, _waitingForWindow);
        }

        StateChanged?.Invoke(this, args);
        return true;
    }

    private void Finish(StopReason reason, string? detail)
    {
        SessionStateChangedEventArgs args;
        lock (_stateLock)
        {
            if (_state == SessionState.Stopped)
            {
                return;
            }

            _state = SessionState.Stopped;
            _reason = reason;
            _detail = detail;
            _waitingForWindow = false;
            args = new SessionStateChangedEventArgs(_state, reason, detail, false);
        }

        if (reason is StopReason.User or StopReason.Scheduler or StopReason.AppExit)
        {
            _logger.LogInformation("Auto-click stopped for {Process} ({Reason}); {Clicks} clicks", _info.ProcessName, reason, Stats.Clicks);
        }
        else
        {
            _logger.LogWarning("Auto-click stopped for {Process}: {Reason} {Detail}; {Clicks} clicks", _info.ProcessName, reason, detail, Stats.Clicks);
        }

        StateChanged?.Invoke(this, args);
    }
}
