using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AutoClick.UI.ViewModels;

namespace AutoClick.UI.Views;

/// <summary>
/// Drag-and-drop reordering for a ListBox. The drag starts only after the mouse moved past the
/// system drag threshold (v1 started dragging on mouse down) and never from a button.
/// </summary>
public static class ListReorder
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(ListReorder), new PropertyMetadata(null, OnCommandChanged));

    private static readonly DependencyProperty DragStartProperty = DependencyProperty.RegisterAttached(
        "DragStart", typeof(Point?), typeof(ListReorder), new PropertyMetadata(null));

    private const string DataFormat = "AutoClick.ReorderIndex";

    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list)
        {
            return;
        }

        list.PreviewMouseLeftButtonDown -= OnMouseDown;
        list.PreviewMouseMove -= OnMouseMove;
        list.Drop -= OnDrop;
        list.DragOver -= OnDragOver;
        if (e.NewValue is null)
        {
            return;
        }

        list.AllowDrop = true;
        list.PreviewMouseLeftButtonDown += OnMouseDown;
        list.PreviewMouseMove += OnMouseMove;
        list.Drop += OnDrop;
        list.DragOver += OnDragOver;
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var list = (ListBox)sender;
        var fromButton = FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) is not null;
        list.SetValue(DragStartProperty, fromButton ? null : e.GetPosition(list));
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        var list = (ListBox)sender;
        if (e.LeftButton != MouseButtonState.Pressed || list.GetValue(DragStartProperty) is not Point start)
        {
            return;
        }

        var delta = e.GetPosition(list) - start;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        list.SetValue(DragStartProperty, null);
        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item is null)
        {
            return;
        }

        var index = list.ItemContainerGenerator.IndexFromContainer(item);
        if (index >= 0)
        {
            DragDrop.DoDragDrop(item, new DataObject(DataFormat, index), DragDropEffects.Move);
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        var list = (ListBox)sender;
        if (e.Data.GetData(DataFormat) is not int from)
        {
            return;
        }

        var target = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        var to = target is null ? list.Items.Count - 1 : list.ItemContainerGenerator.IndexFromContainer(target);
        var request = new ReorderRequest(from, to);
        if (to >= 0 && to != from && GetCommand(list) is { } command && command.CanExecute(request))
        {
            command.Execute(request);
        }

        e.Handled = true;
    }

    internal static T? FindAncestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match)
            {
                return match;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return null;
    }
}

/// <summary>Keeps a ListBox scrolled to the newest item while the user is at the bottom.</summary>
public static class AutoScroll
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(AutoScroll), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list || e.NewValue is not true)
        {
            return;
        }

        ((INotifyCollectionChanged)list.Items).CollectionChanged += (_, args) =>
        {
            if (args.Action != NotifyCollectionChangedAction.Add || list.Items.Count == 0)
            {
                return;
            }

            var viewer = FindScrollViewer(list);
            if (viewer is null)
            {
                list.ScrollIntoView(list.Items[^1]);
            }
            else if (viewer.VerticalOffset >= viewer.ScrollableHeight - 2)
            {
                // Follow new lines only when already at the end: reading older lines is not interrupted.
                list.Dispatcher.InvokeAsync(viewer.ScrollToEnd, DispatcherPriority.Background);
            }
        };
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer)
            {
                return viewer;
            }

            if (FindScrollViewer(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
