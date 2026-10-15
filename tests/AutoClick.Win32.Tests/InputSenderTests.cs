using System.Windows.Interop;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;

namespace AutoClick.Win32.Tests;

public class InputSenderTests
{
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;

    private sealed record Received(int Message, nint WParam, nint LParam);

    [Theory]
    [InlineData(ClickType.LeftClick, new[] { WmMouseMove, WmLButtonDown, WmLButtonUp })]
    [InlineData(ClickType.RightClick, new[] { WmMouseMove, WmRButtonDown, WmRButtonUp })]
    [InlineData(ClickType.DoubleClick, new[] { WmMouseMove, WmLButtonDown, WmLButtonUp, WmLButtonDblClk, WmLButtonUp })]
    public void ClickAsync_PostsTheWindowsMessageSequence(ClickType type, int[] expected)
    {
        StaThread.Run(() =>
        {
            using var target = new MessageTarget();
            var sender = new Win32InputSender(TimeProvider.System);

            var send = sender.ClickAsync(target.Handle, 300, 200, type, CancellationToken.None).AsTask();
            StaThread.PumpUntil(send, TimeSpan.FromSeconds(5));
            StaThread.PumpFor(TimeSpan.FromMilliseconds(50));

            Assert.Equal(InputResult.Ok, send.Result);
            Assert.Equal(expected, target.Messages.Select(m => m.Message));
            Assert.All(target.Messages, m => Assert.Equal(Win32InputSender.MakeLParam(300, 200), m.LParam));
        });
    }

    [Fact]
    public void KeystrokeAsync_SendsScanCodeAndTransitionFlags()
    {
        StaThread.Run(() =>
        {
            using var target = new MessageTarget();
            var sender = new Win32InputSender(TimeProvider.System);

            var send = sender.KeystrokeAsync(target.Handle, 0x41, CancellationToken.None).AsTask();
            StaThread.PumpUntil(send, TimeSpan.FromSeconds(5));
            StaThread.PumpFor(TimeSpan.FromMilliseconds(50));

            Assert.Equal(InputResult.Ok, send.Result);
            Assert.Equal([WmKeyDown, WmKeyUp], target.Messages.Select(m => m.Message));
            var down = (long)target.Messages[0].LParam;
            var up = (long)target.Messages[1].LParam;
            Assert.Equal(0x41, (int)target.Messages[0].WParam);
            Assert.Equal(1, down & 0xFFFF); // repeat count
            Assert.NotEqual(0, (down >> 16) & 0xFF); // scan code
            Assert.Equal(0, (down >> 31) & 1); // key down
            Assert.Equal(1, (up >> 31) & 1); // key up transition
            Assert.Equal(1, (up >> 30) & 1); // previous state: down
        });
    }

    [Fact]
    public void KeyLParams_MarkArrowKeysAsExtended()
    {
        var (down, _) = Win32InputSender.KeyLParams(0x25);

        Assert.Equal(1, ((long)down >> 24) & 1);
    }

    [Fact]
    public async Task KeystrokeAsync_RejectsInvalidKey()
    {
        var sender = new Win32InputSender(TimeProvider.System);

        Assert.Equal(InputResult.Failed, await sender.KeystrokeAsync(1, 0, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClickAsync_ClosedWindow_ReportsWindowGone()
    {
        nint closed = 0;
        StaThread.Run(() =>
        {
            using var target = new MessageTarget();
            closed = target.Handle;
        });

        var sender = new Win32InputSender(TimeProvider.System);

        Assert.Equal(InputResult.WindowGone, await sender.ClickAsync(closed, 1, 1, ClickType.LeftClick, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0, 0, 0x00000000)]
    [InlineData(1, 2, 0x00020001)]
    [InlineData(1920, 1080, 0x04380780)]
    [InlineData(32767, 32767, 0x7FFF7FFF)]
    public void MakeLParam_PacksClientCoordinates(int x, int y, int expected)
    {
        Assert.Equal((nint)expected, Win32InputSender.MakeLParam(x, y));
    }

    /// <summary>A message-only window that records the input messages it receives.</summary>
    private sealed class MessageTarget : IDisposable
    {
        private readonly HwndSource _source;

        public MessageTarget()
        {
            _source = new HwndSource(new HwndSourceParameters("AutoClick input test") { ParentWindow = new IntPtr(-3) });
            _source.AddHook(Hook);
        }

        public nint Handle => _source.Handle;

        public List<Received> Messages { get; } = [];

        private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg is WmMouseMove or (>= WmLButtonDown and <= WmRButtonUp) or WmKeyDown or WmKeyUp)
            {
                Messages.Add(new Received(msg, wParam, lParam));
            }

            return 0;
        }

        public void Dispose() => _source.Dispose();
    }
}
