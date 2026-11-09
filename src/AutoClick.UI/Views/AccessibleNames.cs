using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using AutoClick.UI.Localization;

namespace AutoClick.UI.Views;

/// <summary>
/// Names for parts of WPF-UI templates that have only an icon, which screen readers would read as
/// just "button".
/// </summary>
public static class AccessibleNames
{
    /// <summary>
    /// On an InfoBar: the localization key of the name of its close button (the template part
    /// <c>CloseButton</c>). The name follows language changes.
    /// </summary>
    public static readonly DependencyProperty CloseButtonProperty = DependencyProperty.RegisterAttached(
        "CloseButton", typeof(string), typeof(AccessibleNames), new PropertyMetadata(null, OnCloseButtonChanged));

    public static string? GetCloseButton(DependencyObject element) => (string?)element.GetValue(CloseButtonProperty);

    public static void SetCloseButton(DependencyObject element, string? value) => element.SetValue(CloseButtonProperty, value);

    private static void OnCloseButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Control control)
        {
            control.Loaded -= OnLoaded;
            control.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var control = (Control)sender;
        // A closed InfoBar is collapsed and has no template yet.
        control.ApplyTemplate();
        if (GetCloseButton(control) is { } key && control.Template?.FindName("CloseButton", control) is UIElement button)
        {
            BindingOperations.SetBinding(button, AutomationProperties.NameProperty,
                new Binding($"[{key}]") { Source = LocalizationService.Instance, Mode = BindingMode.OneWay });
        }
    }
}
