using System.Windows.Interop;
using AutoClick.Core.Hotkeys;
using AutoClick.Core.Settings;
using AutoClick.Win32;
using Microsoft.Extensions.Logging;

namespace AutoClick.UI.Services;

public enum HotkeyAction
{
    PauseResume = 1,
    StopAll = 2,
    StartAll = 3,
}

/// <summary>
/// Global hotkeys on a message-only window owned by the UI thread. Registration results are
/// reported (a combination another app already uses is no longer silently ignored), and the
/// hotkeys are re-registered whenever the settings change.
/// </summary>
public sealed class HotkeyService(ILogger<HotkeyService> logger) : IDisposable
{
    private readonly Dictionary<HotkeyAction, HotkeyRegistrationResult> _results = [];
    private HwndSource? _window;
    private HotkeySettings? _applied;
    private bool _suspended;

    /// <summary>Raised on the UI thread.</summary>
    public event EventHandler<HotkeyAction>? Pressed;

    /// <summary>Raised after the hotkeys were registered again (see <see cref="Results"/>).</summary>
    public event EventHandler? RegistrationChanged;

    public IReadOnlyDictionary<HotkeyAction, HotkeyRegistrationResult> Results => _results;

    public IReadOnlyDictionary<HotkeyAction, HotkeyRegistrationResult> Apply(HotkeySettings settings)
    {
        _applied = settings;
        if (!_suspended)
        {
            RegisterAll();
        }

        return _results;
    }

    /// <summary>Releases every hotkey (while the user records a new one in Settings).</summary>
    public void Suspend()
    {
        _suspended = true;
        UnregisterAll();
    }

    public void Resume()
    {
        _suspended = false;
        RegisterAll();
    }

    public void Dispose()
    {
        UnregisterAll();
        _window?.Dispose();
        _window = null;
    }

    private void RegisterAll()
    {
        UnregisterAll();
        if (_applied is null)
        {
            return;
        }

        EnsureWindow();
        Register(HotkeyAction.PauseResume, _applied.PauseResume);
        Register(HotkeyAction.StopAll, _applied.StopAll);
        Register(HotkeyAction.StartAll, _applied.StartAll);
        RegistrationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Register(HotkeyAction action, string text)
    {
        if (!HotkeyGesture.TryParse(text, out var gesture))
        {
            _results[action] = HotkeyRegistrationResult.NotAllowed;
            return;
        }

        var result = HotkeyRegistrar.Register(_window!.Handle, (int)action, gesture);
        _results[action] = result;
        if (result != HotkeyRegistrationResult.Registered)
        {
            logger.LogWarning("Hotkey {Gesture} for {Action} could not be registered: {Result}", gesture, action, result);
        }
    }

    private void UnregisterAll()
    {
        if (_window is null)
        {
            return;
        }

        foreach (var action in Enum.GetValues<HotkeyAction>())
        {
            HotkeyRegistrar.Unregister(_window.Handle, (int)action);
        }

        _results.Clear();
    }

    private void EnsureWindow()
    {
        if (_window is not null)
        {
            return;
        }

        _window = new HwndSource(new HwndSourceParameters("AutoClick hotkeys") { ParentWindow = new IntPtr(-3) });
        _window.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == HotkeyRegistrar.WmHotkey && Enum.IsDefined((HotkeyAction)(int)wParam))
        {
            handled = true;
            Pressed?.Invoke(this, (HotkeyAction)(int)wParam);
        }

        return 0;
    }
}
