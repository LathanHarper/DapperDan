using System.Windows.Input;
using Microsoft.Maui.Devices;

namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// Shared public contract for deterministic tap surfaces. Platform input,
/// visual-tree traversal, and handler ownership remain concrete concerns.
/// </summary>
public interface ITapViewBase
{
    string RichVisualState { get; set; }
    ICommand Command { get; set; }
    object CommandParameter { get; set; }
    bool IsBusy { get; set; }
    bool IsTapping { get; set; }
    int AutoResetIsBusyMilliseconds { get; set; }
    int FeedbackPresentationMilliseconds { get; set; }
    RichButtonFeedbackMode FeedbackMode { get; set; }
    HapticFeedbackType AcceptedHapticType { get; set; }
    HapticFeedbackType RejectedHapticType { get; set; }
    HapticFeedbackType LongPressHapticType { get; set; }
    string TouchSound { get; set; }
    string LongTouchSound { get; set; }
    string NegativeFeedbackSound { get; set; }

    event EventHandler<RichButtonTapStartingEventArgs> Touching;
    event EventHandler<RichButtonTapStartingEventArgs> TapStarting;
    event EventHandler<RichButtonTouchedEventArgs> Touched;
    event EventHandler<RichButtonFeedbackRequestedEventArgs> FeedbackRequested;
}
