namespace AutoClick.Core.Interfaces;

public interface IHotkeyService : IDisposable
{
    event Action<string>? HotkeyPressed;
    void Register(string id, string keyCombo, IntPtr windowHandle);
    void UnregisterAll();
}
