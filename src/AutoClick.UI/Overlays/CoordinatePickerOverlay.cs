using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.UI.Localization;
using AutoClick.Win32;

namespace AutoClick.UI.Overlays;

/// <summary>
/// Crosshair overlay placed exactly over the game's client area (on whichever monitor it is).
/// The picked point is read with GetCursorPos/ScreenToClient in the game's own DPI context, so
/// it is correct for DPI-unaware games on scaled monitors too.
/// </summary>
public sealed class CoordinatePickerOverlay : Window
{
    private readonly nint _target;
    private readonly IPixelReader? _pixels;
    private readonly TaskCompletionSource<ClickPoint?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Line _horizontal = CrosshairLine();
    private readonly Line _vertical = CrosshairLine();
    private readonly TextBlock _coordinates = new() { Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 12 };
    private readonly Border _coordinateLabel;
    private readonly TextBlock _instructions = new() { Foreground = Brushes.White, FontSize = 14, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
    private ClickType _type = ClickType.LeftClick;

    private CoordinatePickerOverlay(nint target, IPixelReader? pixels)
    {
        _target = target;
        _pixels = pixels;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        // Almost transparent but still hit-testable, so it receives the click.
        Background = new SolidColorBrush(Color.FromArgb(0x10, 0x3B, 0x82, 0xF6));
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Cursor = Cursors.Cross;
        Title = "AutoClick coordinate picker";

        _coordinateLabel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x29, 0x3B)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Child = _coordinates,
        };
        var canvas = new Canvas();
        canvas.Children.Add(_horizontal);
        canvas.Children.Add(_vertical);
        canvas.Children.Add(_coordinateLabel);

        var banner = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Padding = new Thickness(16, 8, 16, 8),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0xDD, 0x1E, 0x29, 0x3B)),
            Child = _instructions,
            IsHitTestVisible = false,
        };
        AutomationProperties.SetLiveSetting(_instructions, AutomationLiveSetting.Polite);

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(banner);
        Content = root;
        UpdateInstructions();

        MouseMove += OnMouseMove;
        MouseLeftButtonDown += OnMouseDown;
        PreviewKeyDown += OnKeyDown;
        Deactivated += (_, _) => Finish(null);
        Closed += (_, _) => _result.TrySetResult(null);
    }

    /// <summary>Shows the picker over <paramref name="target"/> and returns the picked point (null if cancelled).</summary>
    /// <param name="target">The game window.</param>
    /// <param name="pixels">When set, the pixel under the point is stored as the reference color.</param>
    public static Task<ClickPoint?> PickAsync(nint target, IPixelReader? pixels)
    {
        if (!WindowGeometry.TryGetPhysicalClientRect(target, out _))
        {
            return Task.FromResult<ClickPoint?>(null);
        }

        var overlay = new CoordinatePickerOverlay(target, pixels);
        overlay.Show();
        overlay.Activate();
        overlay.Focus();
        return overlay._result.Task;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PlaceOverTarget();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // WPF resized the window to the "suggested" rectangle; put it back over the game.
        PlaceOverTarget();
    }

    private void PlaceOverTarget()
    {
        if (WindowGeometry.TryGetPhysicalClientRect(_target, out var rect))
        {
            WindowGeometry.SetBounds(new WindowInteropHelper(this).Handle, rect, topmost: true);
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        _horizontal.X1 = 0;
        _horizontal.X2 = ActualWidth;
        _horizontal.Y1 = _horizontal.Y2 = position.Y;
        _vertical.X1 = _vertical.X2 = position.X;
        _vertical.Y1 = 0;
        _vertical.Y2 = ActualHeight;

        _coordinates.Text = WindowGeometry.TryGetCursorClientPoint(_target, out var point)
            ? $"({point.X}, {point.Y})  {TypeTag(_type)}"
            : TypeTag(_type);

        var labelX = position.X + 16 + 160 > ActualWidth ? position.X - 176 : position.X + 16;
        var labelY = position.Y + 16 + 30 > ActualHeight ? position.Y - 40 : position.Y + 16;
        Canvas.SetLeft(_coordinateLabel, Math.Max(0, labelX));
        Canvas.SetTop(_coordinateLabel, Math.Max(0, labelY));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_type == ClickType.Keystroke || !WindowGeometry.TryGetCursorClientPoint(_target, out var point))
        {
            return;
        }

        var color = ClickPoint.NoColor;
        if (_pixels is not null)
        {
            // Hide the overlay tint first so it can never leak into the reference color.
            Opacity = 0;
            var sample = _pixels.Read(_target, point.X, point.Y);
            color = sample.IsValid ? sample.Color : ClickPoint.NoColor;
        }

        Finish(new ClickPoint
        {
            X = point.X,
            Y = point.Y,
            ClickType = _type,
            ReferenceColor = color,
            CaptureDpi = (int)WindowGeometry.GetDpi(_target),
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        e.Handled = true;
        switch (key)
        {
            case Key.Escape:
                Finish(null);
                return;
            case Key.D1 or Key.NumPad1:
                SetType(ClickType.LeftClick);
                return;
            case Key.D2 or Key.NumPad2:
                SetType(ClickType.DoubleClick);
                return;
            case Key.D3 or Key.NumPad3:
                SetType(ClickType.RightClick);
                return;
            case Key.D4 or Key.NumPad4:
                SetType(ClickType.Keystroke);
                return;
        }

        if (_type != ClickType.Keystroke
            || key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey != 0)
        {
            Finish(new ClickPoint { ClickType = ClickType.Keystroke, VirtualKeyCode = virtualKey });
        }
    }

    private void SetType(ClickType type)
    {
        _type = type;
        UpdateInstructions();
    }

    private void UpdateInstructions()
    {
        var typeName = L.Get(_type switch
        {
            ClickType.DoubleClick => "ClickType_Double",
            ClickType.RightClick => "ClickType_Right",
            ClickType.Keystroke => "ClickType_Keystroke",
            _ => "ClickType_Left",
        });
        var instruction = _type == ClickType.Keystroke ? L.Get("Picker_InstructionKeystroke") : L.Get("Picker_Instruction");
        _instructions.Text = $"{instruction}\n[{typeName}]";
    }

    private void Finish(ClickPoint? point)
    {
        if (_result.TrySetResult(point))
        {
            Close();
        }
    }

    private static string TypeTag(ClickType type) => type switch
    {
        ClickType.DoubleClick => "[D]",
        ClickType.RightClick => "[R]",
        ClickType.Keystroke => "[K]",
        _ => "[L]",
    };

    private static Line CrosshairLine() => new()
    {
        Stroke = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)),
        StrokeThickness = 1,
        StrokeDashArray = [4, 4],
        IsHitTestVisible = false,
    };
}
