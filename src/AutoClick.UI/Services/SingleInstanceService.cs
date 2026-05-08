using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using AutoClick.Core.Interfaces;

namespace AutoClick.UI.Services;

/// <summary>
/// Single-instance gate using a named Mutex plus a named-pipe IPC channel for
/// forwarding "show" requests from a second-launch attempt to the running instance.
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    private const byte ShowCommand = (byte)'S';
    private const string MutexPrefix = @"Local\AutoClick.SingleInstance.";
    private const string PipePrefix = "AutoClick.IPC.";
    private const int PipeConnectTimeoutMs = 1000;
    private const int RestartingAcquireTimeoutMs = 3000;

    private readonly ILogService _log;
    private readonly string _userHash;
    private Mutex? _mutex;
    private bool _hasMutex;
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;

    public SingleInstanceService(ILogService log)
    {
        _log = log;
        _userHash = ComputeUserHash();
    }

    private static string ComputeUserHash()
    {
        var user = Environment.UserName ?? "default";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(user));
        return Convert.ToHexString(bytes).Substring(0, 16);
    }

    private string MutexName => MutexPrefix + _userHash;
    private string PipeName => PipePrefix + _userHash;

    /// <summary>
    /// Tries to acquire the single-instance Mutex.
    /// </summary>
    /// <param name="isRestarting">When true, waits up to 3 s for the previous instance to release
    /// the Mutex (used when this process was spawned by <c>RestartApplication</c>).</param>
    /// <returns>true if this is the first instance; false if another is already running.</returns>
    public bool TryAcquire(bool isRestarting)
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: false, MutexName, out _);
            var timeout = isRestarting
                ? TimeSpan.FromMilliseconds(RestartingAcquireTimeoutMs)
                : TimeSpan.Zero;

            try { _hasMutex = _mutex.WaitOne(timeout, exitContext: false); }
            catch (AbandonedMutexException)
            {
                // Previous holder died without releasing — OS hands ownership to us.
                _hasMutex = true;
            }

            if (_hasMutex)
            {
                _log.Info(isRestarting
                    ? "Single-instance mutex acquired after restart wait."
                    : "Single-instance mutex acquired.");
                return true;
            }

            _log.Info(isRestarting
                ? "Restart wait timed out; another AutoClick instance is still running."
                : "Another AutoClick instance is already running.");

            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        catch (Exception ex)
        {
            // Fail open — letting the app start is better UX than blocking on an unexpected error.
            _log.Error("Single-instance Mutex error (failing open)", ex);
            return true;
        }
    }

    /// <summary>
    /// Sends a SHOW command to the running instance via named pipe.
    /// </summary>
    public bool SendShowToExisting()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(PipeConnectTimeoutMs);
            client.WriteByte(ShowCommand);
            client.Flush();
            return true;
        }
        catch (TimeoutException)
        {
            _log.Warn("Could not reach existing AutoClick instance (pipe connect timeout).");
            return false;
        }
        catch (Exception ex)
        {
            _log.Warn($"Could not reach existing instance: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts a background pipe server that invokes <paramref name="onShowRequested"/> on each
    /// SHOW command. Loop is resilient to per-connection errors.
    /// </summary>
    public void StartIpcServer(Action onShowRequested)
    {
        _serverCts = new CancellationTokenSource();
        var token = _serverCts.Token;
        _serverTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) break;

                    int b = server.ReadByte();
                    if (b == ShowCommand)
                    {
                        try { onShowRequested(); }
                        catch (Exception ex) { _log.Warn($"IPC handler threw: {ex.Message}"); }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.Warn($"IPC server error (will restart loop): {ex.Message}");
                    try { await Task.Delay(500, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }, token);
    }

    public void Release()
    {
        try
        {
            _serverCts?.Cancel();
            _serverCts?.Dispose();
            _serverCts = null;
        }
        catch { /* ignore */ }

        if (_mutex != null)
        {
            try { if (_hasMutex) _mutex.ReleaseMutex(); }
            catch { /* abandoned/disposed — ignore */ }
            try { _mutex.Dispose(); }
            catch { /* ignore */ }
            _mutex = null;
            _hasMutex = false;
        }
    }

    public void Dispose() => Release();
}
