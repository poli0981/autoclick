using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Hotkeys;
using AutoClick.Core.Models;

namespace AutoClick.Win32.Tests;

public class WindowAndSystemTests
{
    [Fact]
    public void Elevation_OwnProcess_IsSameOrLower()
    {
        var elevation = new ElevationService();

        Assert.Equal(ElevationState.SameOrLower, elevation.GetRelativeElevation(Environment.ProcessId));
    }

    [Fact]
    public void Elevation_MissingProcess_IsUnknown()
    {
        Assert.Equal(ElevationState.Unknown, new ElevationService().GetRelativeElevation(int.MaxValue - 1));
    }

    [Fact]
    public void Inspector_InvalidHandle_IsGone()
    {
        var inspector = new Win32WindowInspector(new ElevationService());

        Assert.Equal(WindowStatus.Gone, inspector.GetStatus(0x12345678, Environment.ProcessId));
    }

    [Fact]
    public void Inspector_ReportsOwnWindowSizeAndStatus_AndNeverListsOwnWindows()
    {
        StaThread.Run(() =>
        {
            var window = CreateWindow(Colors.Black);
            try
            {
                var handle = new WindowInteropHelper(window).Handle;
                var inspector = new Win32WindowInspector(new ElevationService());

                Assert.Equal(WindowStatus.Alive, inspector.GetStatus(handle, Environment.ProcessId));
                Assert.Equal(WindowStatus.Gone, inspector.GetStatus(handle, Environment.ProcessId + 1));
                Assert.True(inspector.TryGetClientSize(handle, out var width, out var height));
                Assert.True(width > 0 && height > 0);
                Assert.DoesNotContain(inspector.GetCandidateWindows(), w => w.ProcessId == Environment.ProcessId);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PixelReader_ReadsTheColorOfAWpfWindow()
    {
        StaThread.Run(() =>
        {
            var window = CreateWindow(Color.FromRgb(0x33, 0x66, 0x99));
            try
            {
                StaThread.PumpFor(TimeSpan.FromMilliseconds(500));
                var handle = new WindowInteropHelper(window).Handle;

                var sample = new Win32PixelReader().Read(handle, 20, 20);

                Assert.SkipWhen(!sample.IsValid, "No readable desktop surface in this session.");
                Assert.True(ColorRef.Matches(sample.Color, 0x00996633u, 8), $"Read {ColorRef.ToHex(sample.Color)}.");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Hotkeys_RegisterReportsConflictsAndReservedKeys()
    {
        StaThread.Run(() =>
        {
            using var source = new HwndSource(new HwndSourceParameters("AutoClick hotkey test") { ParentWindow = new IntPtr(-3) });
            var gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x87); // F24

            var first = HotkeyRegistrar.Register(source.Handle, 1, gesture);
            Assert.SkipWhen(first == HotkeyRegistrationResult.InUse, "Ctrl+Alt+Shift+F24 is taken on this machine.");
            Assert.Equal(HotkeyRegistrationResult.Registered, first);
            try
            {
                Assert.Equal(HotkeyRegistrationResult.InUse, HotkeyRegistrar.Register(source.Handle, 2, gesture));
                Assert.Equal(HotkeyRegistrationResult.NotAllowed, HotkeyRegistrar.Register(source.Handle, 3, new HotkeyGesture(HotkeyModifiers.None, 0x7B)));
            }
            finally
            {
                HotkeyRegistrar.Unregister(source.Handle, 1);
            }
        });
    }

    private static Window CreateWindow(Color background)
    {
        var window = new Window
        {
            Width = 200,
            Height = 150,
            Left = 40,
            Top = 40,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            Background = new SolidColorBrush(background),
        };
        window.Show();
        return window;
    }
}
