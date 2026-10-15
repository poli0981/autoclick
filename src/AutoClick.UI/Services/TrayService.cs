using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AutoClick.UI.Localization;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace AutoClick.UI.Services;

/// <summary>Notification-area icon with a localized menu and balloon notifications.</summary>
public sealed class TrayService : IDisposable
{
    private TaskbarIcon? _icon;

    public event EventHandler? ShowRequested;

    public event EventHandler? StartAllRequested;

    public event EventHandler? StopAllRequested;

    public event EventHandler? ExitRequested;

    public void Create()
    {
        if (_icon is not null)
        {
            return;
        }

        var menu = new ContextMenu();
        menu.Items.Add(Item("Tray_Show", (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Queue_StartAll", (_, _) => StartAllRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(Item("Queue_StopAll", (_, _) => StopAllRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Tray_Exit", (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty)));

        _icon = new TaskbarIcon
        {
            ToolTipText = "AutoClick",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/app.ico")),
            ContextMenu = menu,
            NoLeftClickDelay = true,
        };
        _icon.TrayMouseDoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        _icon.TrayLeftMouseUp += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    public void Notify(string title, string message, bool warning = false) =>
        _icon?.ShowNotification(title, message, warning ? NotificationIcon.Warning : NotificationIcon.Info);

    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }

    private static MenuItem Item(string key, System.Windows.RoutedEventHandler onClick)
    {
        var item = new MenuItem();
        item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding($"[{key}]") { Source = LocalizationService.Instance });
        item.Click += onClick;
        return item;
    }
}
