using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AutoClick.Core.Abstractions;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace AutoClick.Win32;

public enum RelaunchResult
{
    Started,

    /// <summary>The user declined the UAC prompt.</summary>
    Cancelled,
    Failed,
}

/// <summary>
/// Compares integrity levels (Windows blocks input from a lower to a higher level, UIPI)
/// and relaunches AutoClick elevated when the user asks for it.
/// </summary>
public sealed class ElevationService : IElevationService
{
    private const int ErrorAccessDenied = 5;
    private const int ErrorCancelled = 1223;

    private readonly Lazy<int> _ownIntegrityLevel = new(GetOwnIntegrityLevel);
    private readonly Lazy<bool> _isElevated = new(GetOwnElevation);

    public bool IsCurrentProcessElevated => _isElevated.Value;

    public ElevationState GetRelativeElevation(int processId)
    {
        using var process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)processId);
        if (process.IsInvalid)
        {
            // Opening an elevated process with this right normally works; protected and
            // anti-cheat processes refuse it, which says nothing reliable about elevation.
            return ElevationState.Unknown;
        }

        if (!PInvoke.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var token))
        {
            // Reading the token of an elevated process is denied to a non-elevated caller.
            return Marshal.GetLastPInvokeError() == ErrorAccessDenied && !IsCurrentProcessElevated
                ? ElevationState.Higher
                : ElevationState.Unknown;
        }

        using (token)
        {
            var level = GetIntegrityLevel(token);
            if (level < 0)
            {
                return ElevationState.Unknown;
            }

            return level > _ownIntegrityLevel.Value ? ElevationState.Higher : ElevationState.SameOrLower;
        }
    }

    /// <summary>Starts <paramref name="executable"/> elevated ("Run as administrator").</summary>
    public static RelaunchResult RelaunchElevated(string executable, IEnumerable<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory,
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
            return process is null ? RelaunchResult.Failed : RelaunchResult.Started;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return RelaunchResult.Cancelled;
        }
        catch (Win32Exception)
        {
            return RelaunchResult.Failed;
        }
    }

    [SuppressMessage("Security", "S6640:Unsafe code blocks should not be used", Justification = "Reviewed: CsWin32 has no safe overload; the struct has the size passed to the call.")]
    private static bool GetOwnElevation()
    {
        using var token = OpenOwnToken();
        if (token is null)
        {
            return false;
        }

        unsafe
        {
            TOKEN_ELEVATION elevation;
            return PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevation, &elevation, (uint)sizeof(TOKEN_ELEVATION), out _)
                && elevation.TokenIsElevated != 0;
        }
    }

    private static int GetOwnIntegrityLevel()
    {
        using var token = OpenOwnToken();
        return token is null ? -1 : GetIntegrityLevel(token);
    }

    private static SafeFileHandle? OpenOwnToken()
    {
        using var self = new SafeFileHandle(PInvoke.GetCurrentProcess(), ownsHandle: false);
        return PInvoke.OpenProcessToken(self, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var token) ? token : null;
    }

    /// <summary>The integrity level RID (e.g. 0x2000 medium, 0x3000 high), or -1.</summary>
    [SuppressMessage("Security", "S6640:Unsafe code blocks should not be used", Justification = "Reviewed: CsWin32 has no safe overloads; the buffer has the size the first call reports and stays pinned while it is read.")]
    private static unsafe int GetIntegrityLevel(SafeHandle token)
    {
        PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, null, 0, out var size);
        if (size == 0)
        {
            return -1;
        }

        var buffer = new byte[size];
        fixed (byte* pointer = buffer)
        {
            if (!PInvoke.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pointer, size, out _))
            {
                return -1;
            }

            var label = (TOKEN_MANDATORY_LABEL*)pointer;
            var sid = label->Label.Sid;
            var count = *PInvoke.GetSidSubAuthorityCount(sid);
            return count == 0 ? -1 : (int)*PInvoke.GetSidSubAuthority(sid, (uint)(count - 1));
        }
    }
}
