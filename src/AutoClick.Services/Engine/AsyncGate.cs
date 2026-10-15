namespace AutoClick.Services.Engine;

/// <summary>An awaitable open/closed gate (closed while a session is paused).</summary>
internal sealed class AsyncGate
{
    private readonly Lock _lock = new();
    private TaskCompletionSource _opened = CreateOpened();

    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                return _opened.Task.IsCompleted;
            }
        }
    }

    public void Open()
    {
        lock (_lock)
        {
            _opened.TrySetResult();
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            if (_opened.Task.IsCompleted)
            {
                _opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        Task opened;
        lock (_lock)
        {
            opened = _opened.Task;
        }

        return opened.IsCompleted ? Task.CompletedTask : opened.WaitAsync(cancellationToken);
    }

    private static TaskCompletionSource CreateOpened()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
