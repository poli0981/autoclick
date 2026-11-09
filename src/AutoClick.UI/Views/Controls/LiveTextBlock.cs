using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AutoClick.UI.Views.Controls;

/// <summary>
/// A text block whose changes screen readers announce. In WPF,
/// <see cref="AutomationProperties.LiveSettingProperty"/> only marks a live region: nothing is read
/// until the element raises <see cref="AutomationEvents.LiveRegionChanged"/>, which a plain
/// <see cref="TextBlock"/> never does. The announcement waits until the text has been laid out (so
/// a message that appears together with its text is read), and comes at most once a second with
/// the latest text (so a download percentage is not read for every percent).
/// </summary>
public sealed class LiveTextBlock : TextBlock
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LayoutDelay = TimeSpan.FromMilliseconds(100);

    private readonly DispatcherTimer _timer;
    // Environment.TickCount64 of the last announcement (0: none yet, long before any tick count).
    private long _lastAnnouncement;

    static LiveTextBlock() =>
        // TextBlock.OnPropertyChanged is sealed; overriding the metadata adds a callback to TextBlock's own.
        TextProperty.OverrideMetadata(typeof(LiveTextBlock), new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public LiveTextBlock()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Announce();
        };
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var block = (LiveTextBlock)d;
        if (block._timer.IsEnabled
            || AutomationProperties.GetLiveSetting(block) == AutomationLiveSetting.Off
            || !AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
        {
            return;
        }

        var sinceLast = TimeSpan.FromMilliseconds(Environment.TickCount64 - block._lastAnnouncement);
        var wait = MinimumInterval - sinceLast;
        block._timer.Interval = wait > LayoutDelay ? wait : LayoutDelay;
        block._timer.Start();
    }

    private void Announce()
    {
        if (!IsVisible || string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        _lastAnnouncement = Environment.TickCount64;
        UIElementAutomationPeer.CreatePeerForElement(this)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
