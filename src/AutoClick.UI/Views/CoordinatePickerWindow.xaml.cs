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

    private bool _captureNextKey;

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Mode toggles 1/2/3/4 are always treated as mode switches, not as keystroke captures.
        switch (e.Key)
        {
            case System.Windows.Input.Key.Escape:
                DialogResult = false;
                return;
            case System.Windows.Input.Key.D1:
            case System.Windows.Input.Key.NumPad1:
                _selectedClickType = ClickType.LeftClick;
                _captureNextKey = false;
                UpdateInstructionText();
                return;
            case System.Windows.Input.Key.D2:
            case System.Windows.Input.Key.NumPad2:
                _selectedClickType = ClickType.DoubleClick;
                _captureNextKey = false;
                UpdateInstructionText();
                return;
            case System.Windows.Input.Key.D3:
            case System.Windows.Input.Key.NumPad3:
                _selectedClickType = ClickType.RightClick;
                _captureNextKey = false;
                UpdateInstructionText();
                return;
            case System.Windows.Input.Key.D4:
            case System.Windows.Input.Key.NumPad4:
                _selectedClickType = ClickType.Keystroke;
                _captureNextKey = true;
                UpdateInstructionText();
                return;
        }

        // In Keystroke mode, the next key press is captured as the keystroke to send.
        if (_captureNextKey && _selectedClickType == ClickType.Keystroke)
        {
            // Skip pure modifier-only presses; user expects to capture an actual key.
            if (e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl ||
                e.Key == System.Windows.Input.Key.LeftAlt  || e.Key == System.Windows.Input.Key.RightAlt  ||
                e.Key == System.Windows.Input.Key.LeftShift|| e.Key == System.Windows.Input.Key.RightShift||
                e.Key == System.Windows.Input.Key.LWin     || e.Key == System.Windows.Input.Key.RWin)
            {
                return;
            }

            var vk = System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key);
            SelectedPoint = new ClickPoint(0, 0, ClickType.Keystroke)
            {
                VirtualKeyCode = vk
            };
            DialogResult = true;
            e.Handled = true;
        }
    }

    private void UpdateInstructionText()
    {
        var currentName = _selectedClickType switch
        {
            ClickType.DoubleClick => Strings.ClickTypeDouble,
            ClickType.RightClick => Strings.ClickTypeRight,
            ClickType.Keystroke => Strings.ClickTypeKeystroke,
            _ => Strings.ClickTypeLeft
        };

        InstructionText.Text = _selectedClickType == ClickType.Keystroke
            ? $"{Strings.PickerInstructionKeystroke}  [{currentName}]"
            : $"{Strings.PickerInstruction}  [{currentName}]";
    }

    private static string GetClickTypeTag(ClickType type) => type switch
    {
        ClickType.DoubleClick => "[D]",
        ClickType.RightClick => "[R]",
        ClickType.Keystroke => "[K]",
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
