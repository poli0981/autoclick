using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AutoClick.Core.Enums;

namespace AutoClick.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = value is SessionState s ? s : SessionState.Idle;
        return state switch
        {
            SessionState.Running => Application.Current.FindResource("SuccessBrush"),
            SessionState.Paused => Application.Current.FindResource("WarningBrush"),
            SessionState.Stopped => Application.Current.FindResource("DangerBrush"),
            _ => Application.Current.FindResource("TextSecondaryBrush")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
