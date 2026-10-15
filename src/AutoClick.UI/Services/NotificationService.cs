using System.Windows;

namespace AutoClick.UI.Services;

/// <summary>Shows a toast inside the window, or a notification-area balloon when it is hidden.</summary>
public sealed class NotificationService(DialogService dialogs, TrayService tray)
{
    public void Show(string title, string message, ToastKind kind = ToastKind.Info)
    {
        var window = Application.Current?.MainWindow;
        if (window is { IsVisible: true } && window.WindowState != WindowState.Minimized)
        {
            dialogs.Toast(title, message, kind);
        }
        else
        {
            tray.Notify(title, message, kind is ToastKind.Warning or ToastKind.Error);
        }
    }
}
