using System.Collections.Concurrent;
using AutoClick.Core.Enums;
using AutoClick.Core.Interfaces;
using AutoClick.Core.Models;
using AutoClick.Win32;

namespace AutoClick.Services;

public class ClickEngineService : IClickEngine
{
    private readonly ConcurrentDictionary<string, ManualResetEventSlim> _pauseEvents = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
    private readonly ILogService _log;

    public ClickEngineService(ILogService log)
    {
        _log = log;
    }

    public Task StartAsync(GameSession session, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokens[session.Id] = cts;

        var pauseEvent = new ManualResetEventSlim(true);
        _pauseEvents[session.Id] = pauseEvent;

        session.State = SessionState.Running;
        session.StartedAt = DateTime.Now;
        session.ClickCount = 0;

        _log.Info($"Auto-click of \"{session.ProcessName}\" is started");

        return Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    pauseEvent.Wait(cts.Token);

                    if (cts.Token.IsCancellationRequested)
                        break;

                    if (!WindowHelper.IsWindowStillValid(session.WindowHandle) ||
                        !WindowHelper.IsProcessRunning(session.ProcessId))
                    {
                        _log.Warn($"Window for \"{session.ProcessName}\" is no longer valid. Stopping.");
                        session.State = SessionState.Stopped;
                        break;
                    }

                    // Execute click sequence: each point in order with optional per-point delay
                    var points = session.ClickPoints;
                    bool outOfBounds = false;
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (cts.Token.IsCancellationRequested) break;

                        var point = points[i];

                        if (!CoordinateHelper.IsCoordinateInBounds(session.WindowHandle, point.X, point.Y))
                        {
                            _log.Warn($"Coordinate ({point.X}, {point.Y}) is out of bounds for \"{session.ProcessName}\". Window may have been resized. Stopping.");
                            outOfBounds = true;
                            break;
                        }

                        InputSimulator.SendClick(session.WindowHandle, point.X, point.Y, point.ClickType);
                        session.ClickCount++;

                        // Per-point delay (between points in a sequence)
                        if (point.DelayAfterMs > 0 && i < points.Count - 1)
                        {
                            await Task.Delay(point.DelayAfterMs, cts.Token);
                        }
                    }

                    if (outOfBounds)
                    {
                        session.State = SessionState.Stopped;
                        break;
                    }

                    // Main interval (between full sequence cycles)
                    double interval = session.Profile.GetInterval();
                    session.LastIntervalSeconds = interval;

                    await Task.Delay(TimeSpan.FromSeconds(interval), cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _log.Error($"Error in click loop for \"{session.ProcessName}\"", ex);
            }
            finally
            {
                session.State = SessionState.Stopped;
                _log.Info($"Auto-click of \"{session.ProcessName}\" is stopped. Total clicks: {session.ClickCount}");
                Cleanup(session.Id);
            }
        }, cts.Token);
    }

    public void Pause(GameSession session)
    {
        if (_pauseEvents.TryGetValue(session.Id, out var evt))
        {
            evt.Reset();
            session.State = SessionState.Paused;
            _log.Info($"Auto-click of \"{session.ProcessName}\" is paused");
        }
    }

    public void Resume(GameSession session)
    {
        if (_pauseEvents.TryGetValue(session.Id, out var evt))
        {
            evt.Set();
            session.State = SessionState.Running;
            _log.Info($"Auto-click of \"{session.ProcessName}\" is resumed");
        }
    }

    public void Stop(GameSession session)
    {
        if (_cancellationTokens.TryGetValue(session.Id, out var cts))
        {
            cts.Cancel();
        }
        if (_pauseEvents.TryGetValue(session.Id, out var evt))
        {
            evt.Set(); // Unblock if paused so loop can exit
        }
    }

    private void Cleanup(string sessionId)
    {
        if (_cancellationTokens.TryRemove(sessionId, out var cts))
            cts.Dispose();
        if (_pauseEvents.TryRemove(sessionId, out var evt))
            evt.Dispose();
    }
}
