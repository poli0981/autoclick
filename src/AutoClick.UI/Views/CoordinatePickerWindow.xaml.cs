using System.Windows;
using System.Windows.Controls;
using AutoClick.Core.Models;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.UI.Views;

public partial class CoordinatePickerWindow : Window
{
    private readonly IntPtr _targetWindow;
    public ClickPoint? SelectedPoint { get; private set; }

    public CoordinatePickerWindow(IntPtr targetWindow)
    {
        InitializeComponent();
        _targetWindow = targetWindow;
        MouseMove += OnMouseMove;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(this);

        // Update crosshairs
        HLine.X1 = 0;
        HLine.X2 = ActualWidth;
        HLine.Y1 = pos.Y;
        HLine.Y2 = pos.Y;

        VLine.X1 = pos.X;
        VLine.X2 = pos.X;
        VLine.Y1 = 0;
        VLine.Y2 = ActualHeight;

        // Convert screen position to target window client coords
        var screenPoint = PointToScreen(pos);
        var clientPoint = ScreenToClient(_targetWindow, (int)screenPoint.X, (int)screenPoint.Y);

        CoordText.Text = $"({clientPoint.X}, {clientPoint.Y})";

        // Position label near cursor
        double labelX = pos.X + 16;
        double labelY = pos.Y + 16;
        if (labelX + 120 > ActualWidth) labelX = pos.X - 120;
        if (labelY + 30 > ActualHeight) labelY = pos.Y - 30;
        Canvas.SetLeft(CoordLabel, labelX);
        Canvas.SetTop(CoordLabel, labelY);
    }

    private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        var screenPoint = PointToScreen(pos);
        var clientPoint = ScreenToClient(_targetWindow, (int)screenPoint.X, (int)screenPoint.Y);

        // Validate: coordinate should be within the target window's client area
        GetClientRect(_targetWindow, out RECT rect);
        if (clientPoint.X >= 0 && clientPoint.Y >= 0 &&
            clientPoint.X < rect.Width && clientPoint.Y < rect.Height)
        {
            SelectedPoint = new ClickPoint(clientPoint.X, clientPoint.Y);
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Selected coordinate is outside the game window. Please try again.",
                "Invalid Coordinate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            DialogResult = false;
        }
    }

    private static POINT ScreenToClient(IntPtr hWnd, int screenX, int screenY)
    {
        var pt = new POINT(screenX, screenY);
        // ClientToScreen is the inverse; we need ScreenToClient
        // Use GetWindowRect to calculate offset
        GetWindowRect(hWnd, out RECT windowRect);
        GetClientRect(hWnd, out RECT clientRect);

        // Calculate non-client area offset (title bar, borders)
        var clientOrigin = new POINT(0, 0);
        ClientToScreen(hWnd, ref clientOrigin);

        return new POINT(screenX - clientOrigin.X, screenY - clientOrigin.Y);
    }
}
