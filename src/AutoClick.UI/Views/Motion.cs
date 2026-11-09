using System.Windows;

namespace AutoClick.UI.Views;

/// <summary>
/// Windows' "Animation effects" setting (Settings → Accessibility → Visual effects). WPF-UI's page
/// transitions and LiveCharts' animations do not read it, so the views apply it themselves.
/// </summary>
public static class Motion
{
    static Motion() => SystemParameters.StaticPropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation))
        {
            Changed?.Invoke(null, EventArgs.Empty);
        }
    };

    /// <summary>Raised when the Windows setting changes.</summary>
    public static event EventHandler? Changed;

    /// <summary>False when the user turned animations off in Windows.</summary>
    public static bool IsEnabled => SystemParameters.ClientAreaAnimation;
}
