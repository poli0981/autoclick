using System.Collections.Concurrent;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoClick.Services.Tests;

internal sealed record SentInput(string Kind, int X, int Y, ClickType Type, int VirtualKey);

internal sealed class FakeInputSender : IInputSender
{
    private readonly ConcurrentQueue<InputResult> _scripted = new();

    public ConcurrentQueue<SentInput> Sent { get; } = new();

    public InputResult DefaultResult { get; set; } = InputResult.Ok;

    public void Script(params InputResult[] results)
    {
        foreach (var result in results)
        {
            _scripted.Enqueue(result);
        }
    }

    public ValueTask<InputResult> ClickAsync(nint window, int x, int y, ClickType type, CancellationToken cancellationToken)
    {
        var result = Next();
        if (result == InputResult.Ok)
        {
            Sent.Enqueue(new SentInput("click", x, y, type, 0));
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<InputResult> KeystrokeAsync(nint window, int virtualKey, CancellationToken cancellationToken)
    {
        var result = Next();
        if (result == InputResult.Ok)
        {
            Sent.Enqueue(new SentInput("key", 0, 0, ClickType.Keystroke, virtualKey));
        }

        return ValueTask.FromResult(result);
    }

    private InputResult Next() => _scripted.TryDequeue(out var result) ? result : DefaultResult;
}

internal sealed class FakeWindowInspector : IWindowInspector
{
    public volatile WindowStatus Status = WindowStatus.Alive;

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public List<WindowInfo> Windows { get; } = [];

    public IReadOnlyList<WindowInfo> GetCandidateWindows() => Windows;

    public WindowStatus GetStatus(nint window, int processId) => Status;

    public bool TryGetClientSize(nint window, out int width, out int height)
    {
        width = Width;
        height = Height;
        return true;
    }
}

internal sealed class FakePixelReader : IPixelReader
{
    public volatile uint Color = 0x00123456;

    public PixelSample Read(nint window, int x, int y) => new(Color, true);
}

internal static class TestLog
{
    public static NullLogger<T> For<T>() => NullLogger<T>.Instance;
}

/// <summary>A temporary data directory deleted after the test.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AutoClickTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort.
        }
    }
}

internal static class Fixtures
{
    public static string PathOf(params string[] relative) =>
        System.IO.Path.Combine([AppContext.BaseDirectory, "Fixtures", .. relative]);
}
