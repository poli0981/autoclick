using System.Windows;
using System.Windows.Controls;
using AutoClick.Core.Enums;
using AutoClick.Core.Models;
using AutoClick.UI.Resources;
using AutoClick.Win32;
using static AutoClick.Win32.NativeMethods;

namespace AutoClick.UI.Views;

public partial class CoordinatePickerWindow : Window
{
    private readonly IntPtr _targetWindow;
    private ClickType _selectedClickType = ClickType.LeftClick;
    public ClickPoint? SelectedPoint { get; private set; }

    public CoordinatePickerWindow(IntPtr targetWindow)
    {
        InitializeComponent();
        _targetWindow = targetWindow;
        MouseMove += OnMouseMove;
        UpdateInstructionText();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Activate();
        Focus();
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

        var typeTag = GetClickTypeTag(_selectedClickType);
        CoordText.Text = $"({clientPoint.X}, {clientPoint.Y}) {typeTag}";

        // Position label near cursor
        double labelX = pos.X + 16;
        double labelY = pos.Y + 16;
        if (labelX + 140 > ActualWidth) labelX = pos.X - 140;
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
            var refColor = PixelColorHelper.ReadPixelColor(_targetWindow, clientPoint.X, clientPoint.Y);
            SelectedPoint = new ClickPoint(clientPoint.X, clientPoint.Y, _selectedClickType)
            {
                ReferenceColor = refColor
            };
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
        switch (e.Key)
        {
            case System.Windows.Input.Key.Escape:
                DialogResult = false;
                break;
            case System.Windows.Input.Key.D1:
            case System.Windows.Input.Key.NumPad1:
                _selectedClickType = ClickType.LeftClick;
                UpdateInstructionText();
                break;
            case System.Windows.Input.Key.D2:
            case System.Windows.Input.Key.NumPad2:
                _selectedClickType = ClickType.DoubleClick;
                UpdateInstructionText();
                break;
            case System.Windows.Input.Key.D3:
            case System.Windows.Input.Key.NumPad3:
                _selectedClickType = ClickType.RightClick;
                UpdateInstructionText();
                break;
        }
    }

    private void UpdateInstructionText()
    {
        var currentName = _selectedClickType switch
        {
            ClickType.DoubleClick => Strings.ClickTypeDouble,
            ClickType.RightClick => Strings.ClickTypeRight,
            _ => Strings.ClickTypeLeft
        };
        InstructionText.Text = $"{Strings.PickerInstruction}  [{currentName}]";
    }

    private static string GetClickTypeTag(ClickType type) => type switch
    {
        ClickType.DoubleClick => "[D]",
        ClickType.RightClick => "[R]",
        _ => "[L]"
    };

    private static POINT ScreenToClient(IntPtr hWnd, int screenX, int screenY)
    {
        var pt = new POINT(screenX, screenY);
        GetWindowRect(hWnd, out RECT windowRect);
        GetClientRect(hWnd, out RECT clientRect);

        var clientOrigin = new POINT(0, 0);
        ClientToScreen(hWnd, ref clientOrigin);

        return new POINT(screenX - clientOrigin.X, screenY - clientOrigin.Y);
    }
}
