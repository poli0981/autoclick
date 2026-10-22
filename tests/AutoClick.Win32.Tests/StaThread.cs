using System.Windows.Threading;

namespace AutoClick.Win32.Tests;

/// <summary>Runs test code on a dedicated STA thread with a WPF dispatcher (windows need one).</summary>
internal static class StaThread
{
    public static void Run(Action action)
    {
        // The task carries the outcome back; GetResult rethrows a failure with its original stack.
        var outcome = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                outcome.SetResult();
            }
            catch (Exception ex)
            {
                outcome.SetException(ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA test thread timed out.");
        outcome.Task.GetAwaiter().GetResult();
    }

    /// <summary>Pumps messages until <paramref name="task"/> completes (or the timeout elapses).</summary>
    public static void PumpUntil(Task task, TimeSpan timeout)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = timeout };
        timer.Tick += (_, _) => frame.Continue = false;
        timer.Start();
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        timer.Stop();
    }

    /// <summary>Pumps messages for a short while so posted messages get dispatched.</summary>
    public static void PumpFor(TimeSpan duration) => PumpUntil(Task.Delay(duration), duration + TimeSpan.FromSeconds(1));
}
