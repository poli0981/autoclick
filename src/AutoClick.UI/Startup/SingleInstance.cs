using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.Startup;

/// <summary>
/// One AutoClick per user session: a named mutex, plus a named pipe the second launch uses to
/// ask the running instance to show itself.
/// </summary>
/// <remarks>
/// Both objects get an explicit ACL for the current user. Without it, an instance running as
/// administrator creates them with an admin-only default DACL, a normal second launch cannot
/// open them and (in v1) started a second copy.
/// </remarks>
public sealed class SingleInstance : IDisposable
{
    private const byte ShowCommand = (byte)'S';

    private readonly ILogger _logger;
    private readonly Mutex _mutex;
    private CancellationTokenSource? _serverCancellation;
    private bool _released;

    private SingleInstance(Mutex mutex, ILogger logger)
    {
        _mutex = mutex;
        _logger = logger;
    }

    /// <summary>Same names as v1, so v1 and v2 never run side by side.</summary>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Not security related: the hash only reproduces v1's object names.")]
    [SuppressMessage("Security", "S4790:Weak hashing algorithms should not be used", Justification = "Not security related: the hash only reproduces v1's object names.")]
    private static string UserKey { get; } =
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(Environment.UserName ?? "default")))[..16];

    private static string MutexName => @"Local\AutoClick.SingleInstance." + UserKey;

    private static string PipeName => "AutoClick.IPC." + UserKey;

    /// <summary>
    /// Returns the instance lock, or null when another AutoClick is running.
    /// <paramref name="wait"/> covers restarts, where the previous process is still exiting.
    /// </summary>
    public static SingleInstance? TryAcquire(TimeSpan wait, ILogger logger)
    {
        Mutex mutex;
        try
        {
            var security = new MutexSecurity();
            security.AddAccessRule(new MutexAccessRule(CurrentUser, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
            mutex = MutexAcl.Create(false, MutexName, out _, security);
        }
        catch (UnauthorizedAccessException)
        {
            // Created by an instance we cannot open (e.g. an elevated v1): it is running.
            logger.LogInformation("Another AutoClick instance owns the single-instance lock");
            return null;
        }

        try
        {
            if (mutex.WaitOne(wait))
            {
                return new SingleInstance(mutex, logger);
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous owner died without releasing it: the OS hands it to us.
            return new SingleInstance(mutex, logger);
        }

        mutex.Dispose();
        return null;
    }

    /// <summary>Asks the running instance to show its window.</summary>
    public static void SignalExisting(ILogger logger)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1500);
            client.WriteByte(ShowCommand);
            client.Flush();
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not reach the running AutoClick instance");
        }
    }

    /// <summary>Listens for "show" requests from later launches.</summary>
    public void StartListening(Action onShowRequested)
    {
        _serverCancellation = new CancellationTokenSource();
        var token = _serverCancellation.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var security = new PipeSecurity();
                    security.AddAccessRule(new PipeAccessRule(CurrentUser, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
                    await using var server = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    if (server.ReadByte() == ShowCommand)
                    {
                        onShowRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Single-instance pipe error; retrying");
                    try
                    {
                        await Task.Delay(500, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }, token);
    }

    /// <summary>Releases the lock now (before starting a replacement process).</summary>
    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        _serverCancellation?.Cancel();
        _serverCancellation?.Dispose();
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Not owned by this thread any more.
        }

        _mutex.Dispose();
    }

    public void Dispose() => Release();

    private static SecurityIdentifier CurrentUser => WindowsIdentity.GetCurrent().User
        ?? throw new InvalidOperationException("The current user has no SID.");
}
