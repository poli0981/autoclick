using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoClick.Core.Models;
using AutoClick.UI.Localization;
using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views;

/// <summary>
/// MultiBinding: [0] resource key, [1] <see cref="LocalizationService.Culture"/>. The second
/// binding only makes the text re-evaluate when the language changes.
/// </summary>
public sealed class LocKeyConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length > 0 && values[0] is string key ? L.Get(key) : string.Empty;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>MultiBinding: [0] a <see cref="ClickPoint"/>, [1] the culture (see <see cref="LocKeyConverter"/>).</summary>
public sealed class StepTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length > 0 && values[0] is ClickPoint point ? GameSessionViewModel.Describe(point) : string.Empty;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>COLORREF (0x00BBGGRR) to a frozen brush; transparent for "no color".</summary>
public sealed class ColorRefToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not uint color || color == ClickPoint.NoColor)
        {
            return Brushes.Transparent;
        }

        var brush = new SolidColorBrush(Color.FromRgb((byte)ColorRef.R(color), (byte)ColorRef.G(color), (byte)ColorRef.B(color)));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>COLORREF to "#RRGGBB".</summary>
public sealed class ColorRefToHexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is uint color ? ColorRef.ToHex(color) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when the value is false.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Collapsed for null or empty strings, visible otherwise.</summary>
public sealed class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null || (value is string text && text.Length == 0) || (value is int count && count == 0)
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true ↔ false (e.g. IsEnabled = !IsArmed).</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public static InvertBoolConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
}
