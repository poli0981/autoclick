using System.Collections.Concurrent;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using Microsoft.Extensions.Logging;

namespace AutoClick.Services.Engine;

/// <summary>Starts click sessions; at most one running session per session id.</summary>
public sealed class ClickEngine(
    IInputSender input,
    IWindowInspector inspector,
    IPixelReader pixels,
    TimeProvider timeProvider,
    ILogger<ClickEngine> logger,
    Func<Random>? randomFactory = null)
{
    private readonly ConcurrentDictionary<string, ClickSession> _sessions = new();
    private readonly Func<Random> _randomFactory = randomFactory ?? (() => Random.Shared);

    /// <exception cref="InvalidOperationException">A session with this id is still running.</exception>
    public ClickSession Start(SessionStartInfo info, SessionStats stats)
    {
        ArgumentNullException.ThrowIfNull(info);
        if (info.Points.Count == 0)
        {
            throw new ArgumentException("A session needs at least one step.", nameof(info));
        }

        var session = new ClickSession(info, stats, input, inspector, pixels, timeProvider, logger, _randomFactory());
        if (!_sessions.TryAdd(info.SessionId, session))
        {
            throw new InvalidOperationException($"Session {info.SessionId} is already running.");
        }

        session.StateChanged += OnStateChanged;
        session.Start();
        return session;
    }

    public bool IsRunning(string sessionId) => _sessions.ContainsKey(sessionId);

    /// <summary>Stops every running session (e.g. before exit or applying an update).</summary>
    public Task StopAllAsync(StopReason reason) =>
        Task.WhenAll(_sessions.Values.Select(s => s.StopAsync(reason)));

    private void OnStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        if (e.State == SessionState.Stopped && sender is ClickSession session)
        {
            session.StateChanged -= OnStateChanged;
            _sessions.TryRemove(new KeyValuePair<string, ClickSession>(session.SessionId, session));
        }
    }
}
