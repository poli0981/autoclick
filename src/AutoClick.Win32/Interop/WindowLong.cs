using System.Runtime.InteropServices;

namespace AutoClick.Win32.Interop;

/// <summary>
/// GetWindowLongPtr/SetWindowLongPtr are macros on 32-bit Windows, so CsWin32 does not
/// generate them. AutoClick only ships for x64, where both are real exports.
/// </summary>
internal static partial class WindowLong
{
    public const int GwlExStyle = -20;

    public const nint WsExTransparent = 0x00000020;
    public const nint WsExToolWindow = 0x00000080;
    public const nint WsExLayered = 0x00080000;
    public const nint WsExNoActivate = 0x08000000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static partial nint Get(nint hWnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    public static partial nint Set(nint hWnd, int index, nint value);
}
