using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Prism.Commands;

namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// Deterministic command, busy, and feedback stringer shared by tap surfaces.
/// Concrete controls own their input bridge and visual-state propagation shape.
/// </summary>
public abstract class TapViewBase : ContentView, ITapViewBase
{
    public const string WaitingForTouchState = "WaitingForTouch";
    public const string IsProcessingState = "IsProcessing";
    public const string DefaultTouchSound = "rich_touch.wav";
    public const string DefaultLongTouchSound = "rich_long_touch.wav";
    public const string DefaultNegativeFeedbackSound = "rich_negative_feedback.wav";

    public static readonly BindableProperty RichVisualStateProperty =
        BindableProperty.Create(
            nameof(RichVisualState),
            typeof(string),
            typeof(TapViewBase),
            WaitingForTouchState,
            propertyChanged: OnRichVisualStateChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(TapViewBase),
            null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(TapViewBase),
            null);

    public static readonly BindableProperty IsBusyProperty =
        BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(TapViewBase),
            false,
            BindingMode.TwoWay,
            propertyChanged: OnIsBusyChanged);

    public static readonly BindableProperty IsTappingProperty =
        BindableProperty.Create(
            nameof(IsTapping),
            typeof(bool),
            typeof(TapViewBase),
            false,
            BindingMode.TwoWay);

    public static readonly BindableProperty AutoResetIsBusyMillisecondsProperty =
        BindableProperty.Create(
            nameof(AutoResetIsBusyMilliseconds),
            typeof(int),
            typeof(TapViewBase),
            0,
            validateValue: (_, value) => value is int milliseconds && milliseconds >= 0);

    public static readonly BindableProperty FeedbackPresentationMillisecondsProperty =
        BindableProperty.Create(
            nameof(FeedbackPresentationMilliseconds),
            typeof(int),
            typeof(TapViewBase),
            0,
            validateValue: (_, value) => value is int milliseconds && milliseconds >= 0);

    public static readonly BindableProperty FeedbackModeProperty =
        BindableProperty.Create(
            nameof(FeedbackMode),
            typeof(RichButtonFeedbackMode),
            typeof(TapViewBase),
            RichButtonFeedbackMode.HapticAndSound);

    public static readonly BindableProperty AcceptedHapticTypeProperty =
        BindableProperty.Create(
            nameof(AcceptedHapticType),
            typeof(HapticFeedbackType),
            typeof(TapViewBase),
            HapticFeedbackType.Click);

    public static readonly BindableProperty RejectedHapticTypeProperty =
        BindableProperty.Create(
            nameof(RejectedHapticType),
            typeof(HapticFeedbackType),
            typeof(TapViewBase),
            HapticFeedbackType.LongPress);

    public static readonly BindableProperty LongPressHapticTypeProperty =
        BindableProperty.Create(
            nameof(LongPressHapticType),
            typeof(HapticFeedbackType),
            typeof(TapViewBase),
            HapticFeedbackType.LongPress);

    public static readonly BindableProperty TouchSoundProperty =
        BindableProperty.Create(
            nameof(TouchSound),
            typeof(string),
            typeof(TapViewBase),
            DefaultTouchSound,
            propertyChanged: OnSoundChanged);

    public static readonly BindableProperty LongTouchSoundProperty =
        BindableProperty.Create(
            nameof(LongTouchSound),
            typeof(string),
            typeof(TapViewBase),
            DefaultLongTouchSound,
            propertyChanged: OnSoundChanged);

    public static readonly BindableProperty NegativeFeedbackSoundProperty =
        BindableProperty.Create(
            nameof(NegativeFeedbackSound),
            typeof(string),
            typeof(TapViewBase),
            DefaultNegativeFeedbackSound,
            propertyChanged: OnSoundChanged);

    public static readonly BindableProperty CascadeRichStateProperty =
        BindableProperty.CreateAttached(
            "CascadeRichState",
            typeof(bool),
            typeof(TapViewBase),
            true);

    public static readonly BindableProperty RichVisualStateOptOutProperty =
        BindableProperty.CreateAttached(
            "RichVisualStateOptOut",
            typeof(bool),
            typeof(TapViewBase),
            false);

    private ICommand _activeNativeTouchCommand;
    private object _activeNativeTouchCommandParameter;
    private RichButtonTapStartingEventArgs _activeNativeTouchStartingArgs;
    private CancellationTokenSource _autoResetCancellationTokenSource;
    private DateTimeOffset _autoResetBusyDeadlineUtc;
    private bool _nativeTouchSequenceActive;
    private bool _ownsBusy;

    protected TapViewBase()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateInputEnabled();
    }

    public event EventHandler<RichButtonTapStartingEventArgs> Touching;
    public event EventHandler<RichButtonTapStartingEventArgs> TapStarting;
    public event EventHandler<RichButtonTouchedEventArgs> Touched;
    public event EventHandler<RichButtonFeedbackRequestedEventArgs> FeedbackRequested;

    public string RichVisualState
    {
        get => (string)GetValue(RichVisualStateProperty);
        set => SetValue(RichVisualStateProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public bool IsTapping
    {
        get => (bool)GetValue(IsTappingProperty);
        set => SetValue(IsTappingProperty, value);
    }

    public int AutoResetIsBusyMilliseconds
    {
        get => (int)GetValue(AutoResetIsBusyMillisecondsProperty);
        set => SetValue(AutoResetIsBusyMillisecondsProperty, value);
    }

    public int FeedbackPresentationMilliseconds
    {
        get => (int)GetValue(FeedbackPresentationMillisecondsProperty);
        set => SetValue(FeedbackPresentationMillisecondsProperty, value);
    }

    public RichButtonFeedbackMode FeedbackMode
    {
        get => (RichButtonFeedbackMode)GetValue(FeedbackModeProperty);
        set => SetValue(FeedbackModeProperty, value);
    }

    public HapticFeedbackType AcceptedHapticType
    {
        get => (HapticFeedbackType)GetValue(AcceptedHapticTypeProperty);
        set => SetValue(AcceptedHapticTypeProperty, value);
    }

    public HapticFeedbackType RejectedHapticType
    {
        get => (HapticFeedbackType)GetValue(RejectedHapticTypeProperty);
        set => SetValue(RejectedHapticTypeProperty, value);
    }

    public HapticFeedbackType LongPressHapticType
    {
        get => (HapticFeedbackType)GetValue(LongPressHapticTypeProperty);
        set => SetValue(LongPressHapticTypeProperty, value);
    }

    public string TouchSound
    {
        get => (string)GetValue(TouchSoundProperty);
        set => SetValue(TouchSoundProperty, value);
    }

    public string LongTouchSound
    {
        get => (string)GetValue(LongTouchSoundProperty);
        set => SetValue(LongTouchSoundProperty, value);
    }

    public string NegativeFeedbackSound
    {
        get => (string)GetValue(NegativeFeedbackSoundProperty);
        set => SetValue(NegativeFeedbackSoundProperty, value);
    }

    public static bool GetCascadeRichState(BindableObject bindable) =>
        (bool)bindable.GetValue(CascadeRichStateProperty);

    public static void SetCascadeRichState(BindableObject bindable, bool value) =>
        bindable.SetValue(CascadeRichStateProperty, value);

    public static bool GetRichVisualStateOptOut(BindableObject bindable) =>
        (bool)bindable.GetValue(RichVisualStateOptOutProperty);

    public static void SetRichVisualStateOptOut(BindableObject bindable, bool value) =>
        bindable.SetValue(RichVisualStateOptOutProperty, value);

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(Content))
            ApplyRichVisualState(RichVisualState);

        if (propertyName == nameof(IsEnabled))
        {
            UpdateInputEnabled();

            if (!IsEnabled)
                CancelNativeTouchSequence();
        }

        if (propertyName == nameof(InputTransparent) && InputTransparent)
            CancelNativeTouchSequence();
    }

    /// <summary>
    /// Applies the current rich state using the concrete control's ownership
    /// policy. RichButton cascades locally; Noice-style controls apply to self.
    /// </summary>
    protected abstract void ApplyRichVisualStateCore(string state);

    /// <summary>
    /// Runs a completed MAUI tap. Command is captured before Touching while the
    /// command parameter remains live through Touched, CanExecute, and Execute.
    /// </summary>
    protected async Task ActivateTapAsync()
    {
        ReleaseExpiredAutoResetBusyIfNeeded();

        if (IsBusy)
            return;

        var command = Command;
        var touchingArgs = new RichButtonTapStartingEventArgs(CommandParameter);
        Touching?.Invoke(this, touchingArgs);
        TapStarting?.Invoke(this, touchingArgs);

        if (touchingArgs.Cancel)
        {
            RequestFeedback(RichButtonFeedbackKind.Bunk);
            return;
        }

        await RunAcceptedTouchPipelineAsync(
            command,
            touchingArgs,
            commandParameterSnapshot: null,
            useCommandParameterSnapshot: false);
    }

    /// <summary>
    /// Begins a native two-phase touch and snapshots command plus parameter at
    /// pointer DOWN so later property churn cannot retarget the activation.
    /// </summary>
    protected bool BeginNativeTouchSequence()
        => BeginNativeTouchSequence(positionResolver: null);

    /// <summary>
    /// Begins a native two-phase touch and exposes a platform-owned coordinate
    /// converter for observers that draw feedback outside this control.
    /// </summary>
    protected bool BeginNativeTouchSequence(
        Func<Element, Point?> positionResolver)
    {
        ReleaseExpiredAutoResetBusyIfNeeded();

        if (!IsEnabled || InputTransparent || IsBusy || _nativeTouchSequenceActive)
            return false;

        var command = Command;
        var commandParameter = CommandParameter;
        var touchingArgs = new RichButtonTapStartingEventArgs(
            commandParameter,
            positionResolver);
        Touching?.Invoke(this, touchingArgs);
        TapStarting?.Invoke(this, touchingArgs);

        if (touchingArgs.Cancel)
        {
            RequestFeedback(RichButtonFeedbackKind.Bunk);
            return false;
        }

        _activeNativeTouchCommand = command;
        _activeNativeTouchCommandParameter = commandParameter;
        _activeNativeTouchStartingArgs = touchingArgs;
        _nativeTouchSequenceActive = true;
        return true;
    }

    protected void CancelNativeTouchSequence()
    {
        _activeNativeTouchCommand = null;
        _activeNativeTouchCommandParameter = null;
        _activeNativeTouchStartingArgs = null;
        _nativeTouchSequenceActive = false;
    }

    protected async Task ActivateNativeTouchSequenceAsync()
    {
        if (!_nativeTouchSequenceActive && !BeginNativeTouchSequence())
            return;

        var command = _activeNativeTouchCommand;
        var commandParameter = _activeNativeTouchCommandParameter;
        var touchingArgs = _activeNativeTouchStartingArgs;
        CancelNativeTouchSequence();

        if (!IsEnabled || InputTransparent || IsBusy || touchingArgs is null)
            return;

        await ActivateTapAsync(command, commandParameter, touchingArgs);
    }

    /// <summary>
    /// Runs a native activation that was already accepted at pointer DOWN.
    /// This overload deliberately consumes the snapshotted parameter.
    /// </summary>
    protected Task ActivateTapAsync(
        ICommand command,
        object commandParameterSnapshot,
        RichButtonTapStartingEventArgs touchingArgs) =>
        RunAcceptedTouchPipelineAsync(
            command,
            touchingArgs,
            commandParameterSnapshot,
            useCommandParameterSnapshot: true);

    private static void OnRichVisualStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TapViewBase tapView && newValue is string state && !string.IsNullOrWhiteSpace(state))
            tapView.ApplyRichVisualState(state);
    }

    private static void OnIsBusyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not TapViewBase tapView)
            return;

        tapView.UpdateInputEnabled();

        if (newValue is not bool isBusy)
            return;

        if (!isBusy)
        {
            tapView.CancelAutoResetTimer();
            tapView._ownsBusy = false;
            tapView.IsTapping = false;
        }
        else
        {
            tapView.CancelNativeTouchSequence();
        }

        tapView.RichVisualState = isBusy ? IsProcessingState : WaitingForTouchState;
    }

    private static void OnSoundChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TapViewBase tapView)
            RichButtonSoundPlayer.Prime(tapView);
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        RichButtonSoundPlayer.Prime(this);
        ApplyRichVisualState(RichVisualState);
        UpdateInputEnabled();
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        // Handler reconnect churn can deliver an old native Unloaded after the
        // same virtual tap view is already live again. Do not tear down its new
        // touch sequence or debounce ownership from that stale notification.
        if (IsLoaded)
            return;

        CancelNativeTouchSequence();

        // Debounce-owned buttons can unload during panel transitions.
        // If we only cancel the timer here, an unbound IsBusy latch can survive the unload/reload cycle.
        if (_ownsBusy && AutoResetIsBusyMilliseconds > 0)
        {
            CompletePipeline(clearBusy: true);
            return;
        }

        CancelAutoResetTimer();
    }

    private void ApplyRichVisualState(string state)
    {
        if (!string.IsNullOrWhiteSpace(state))
            ApplyRichVisualStateCore(state);
    }

    private void UpdateInputEnabled()
    {
        InputTransparent = IsBusy || !IsEnabled;
    }

    private async Task RunAcceptedTouchPipelineAsync(
        ICommand command,
        RichButtonTapStartingEventArgs touchingArgs,
        object commandParameterSnapshot,
        bool useCommandParameterSnapshot)
    {
        var touchedCommandParameter = useCommandParameterSnapshot
            ? commandParameterSnapshot
            : CommandParameter;
        var touchedArgs = new RichButtonTouchedEventArgs(touchedCommandParameter);

        Touched?.Invoke(this, touchedArgs);

        if (touchedArgs.Cancel)
        {
            RequestFeedback(RichButtonFeedbackKind.Bunk);
            return;
        }

        if (command == null)
            return;

        var canExecuteCommandParameter = useCommandParameterSnapshot
            ? commandParameterSnapshot
            : CommandParameter;

        if (!command.CanExecute(canExecuteCommandParameter))
        {
            RequestFeedback(RichButtonFeedbackKind.Bunk);
            return;
        }

        var autoReset = AutoResetIsBusyMilliseconds > 0;

        RequestFeedback(touchingArgs.FeedbackKind);
        StartBusyState();

        if (autoReset)
            StartAutoResetTimer();

        await Task.Yield();

        if (FeedbackPresentationMilliseconds > 0)
            await Task.Delay(FeedbackPresentationMilliseconds);

        var commandFaulted = false;

        try
        {
            var executeCommandParameter = useCommandParameterSnapshot
                ? commandParameterSnapshot
                : CommandParameter;
            await ExecuteCommandAsync(command, executeCommandParameter);
        }
        catch (Exception exception)
        {
            // Chokepoint guard: native bridges invoke this pipeline from async
            // void handlers, so an escaping command exception would kill the app.
            commandFaulted = true;
            await LogCommandExceptionAsync(exception);
        }
        finally
        {
            if (commandFaulted)
                CompletePipeline(clearBusy: true);
            else if (!autoReset)
                CompletePipeline(clearBusy: false);
        }
    }

    private async Task LogCommandExceptionAsync(Exception exception)
    {
        Console.WriteLine($"DIRECT_TOUCH|command-exception|id={AutomationId}|{exception}");

        try
        {
            await RichButtonDiagnostics.ReportCommandExceptionAsync(this, exception);
        }
        catch
        {
            // Diagnostics must never take the tap pipeline down with them.
        }
    }

    private void StartBusyState()
    {
        _ownsBusy = true;
        IsBusy = true;
        IsTapping = true;
        RichVisualState = IsProcessingState;
    }

    private void CompletePipeline(bool clearBusy)
    {
        IsTapping = false;

        if (_ownsBusy)
        {
            CancelAutoResetTimer();
            _ownsBusy = false;

            if (clearBusy)
                IsBusy = false;
        }
        else
        {
            RichVisualState = WaitingForTouchState;
        }

        if (!IsBusy)
            RichVisualState = WaitingForTouchState;
    }

    private void StartAutoResetTimer()
    {
        CancelAutoResetTimer();

        var milliseconds = AutoResetIsBusyMilliseconds;

        if (milliseconds <= 0)
            return;

        _autoResetBusyDeadlineUtc = DateTimeOffset.UtcNow.AddMilliseconds(milliseconds);
        _autoResetCancellationTokenSource = new CancellationTokenSource();
        _ = AutoResetIsBusyAsync(milliseconds, _autoResetCancellationTokenSource.Token);
    }

    private void CancelAutoResetTimer()
    {
        _autoResetBusyDeadlineUtc = default;

        var cancellationTokenSource = _autoResetCancellationTokenSource;
        _autoResetCancellationTokenSource = null;

        if (cancellationTokenSource == null)
            return;

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    private async Task AutoResetIsBusyAsync(int milliseconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(milliseconds, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
            return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!_ownsBusy || !IsBusy)
                return;

            _autoResetCancellationTokenSource?.Dispose();
            _autoResetCancellationTokenSource = null;
            _autoResetBusyDeadlineUtc = default;
            _ownsBusy = false;
            IsTapping = false;
            IsBusy = false;
        });
    }

    private void ReleaseExpiredAutoResetBusyIfNeeded()
    {
        if (!_ownsBusy || !IsBusy)
            return;

        if (AutoResetIsBusyMilliseconds <= 0 || _autoResetBusyDeadlineUtc == default)
            return;

        if (DateTimeOffset.UtcNow < _autoResetBusyDeadlineUtc)
            return;

        CompletePipeline(clearBusy: true);
    }

    private static async Task ExecuteCommandAsync(ICommand command, object commandParameter)
    {
        if (command is IAsyncCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(commandParameter);
            return;
        }

        command.Execute(commandParameter);
    }

    private void RequestFeedback(RichButtonFeedbackKind feedbackKind)
    {
        var feedbackArgs = new RichButtonFeedbackRequestedEventArgs(feedbackKind);
        FeedbackRequested?.Invoke(this, feedbackArgs);

        if (feedbackArgs.Handled || feedbackKind == RichButtonFeedbackKind.None || FeedbackMode == RichButtonFeedbackMode.None)
            return;

        if (ShouldPlayHaptic() && HapticFeedback.Default.IsSupported)
            HapticFeedback.Default.Perform(GetHapticType(feedbackKind));

        if (ShouldPlaySound())
            RichButtonSoundPlayer.Play(this, feedbackKind);
    }

    private bool ShouldPlayHaptic()
    {
        return FeedbackMode == RichButtonFeedbackMode.Haptic
            || FeedbackMode == RichButtonFeedbackMode.HapticAndSound;
    }

    private bool ShouldPlaySound()
    {
        return FeedbackMode == RichButtonFeedbackMode.Sound
            || FeedbackMode == RichButtonFeedbackMode.HapticAndSound;
    }

    private HapticFeedbackType GetHapticType(RichButtonFeedbackKind feedbackKind)
    {
        return feedbackKind switch
        {
            RichButtonFeedbackKind.Bunk => RejectedHapticType,
            RichButtonFeedbackKind.LongPress => LongPressHapticType,
            _ => AcceptedHapticType
        };
    }
}

public enum RichButtonFeedbackMode
{
    None,
    Haptic,
    Sound,
    HapticAndSound
}

public enum RichButtonFeedbackKind
{
    None,
    Go,
    Bunk,
    LongPress
}

public sealed class RichButtonTapStartingEventArgs : EventArgs
{
    private readonly Func<Element, Point?> _positionResolver;

    public RichButtonTapStartingEventArgs(object commandParameter)
        : this(commandParameter, positionResolver: null)
    {
    }

    public RichButtonTapStartingEventArgs(
        object commandParameter,
        Func<Element, Point?> positionResolver)
    {
        CommandParameter = commandParameter;
        _positionResolver = positionResolver;
    }

    public object CommandParameter { get; }
    public bool Cancel { get; set; }
    public RichButtonFeedbackKind FeedbackKind { get; set; } = RichButtonFeedbackKind.Go;

    public Point? GetPosition(Element relativeTo) =>
        relativeTo is null ? null : _positionResolver?.Invoke(relativeTo);
}

public sealed class RichButtonTouchedEventArgs : EventArgs
{
    public RichButtonTouchedEventArgs(object commandParameter)
    {
        CommandParameter = commandParameter;
    }

    public object CommandParameter { get; }
    public bool Cancel { get; set; }
}

public sealed class RichButtonFeedbackRequestedEventArgs : EventArgs
{
    public RichButtonFeedbackRequestedEventArgs(RichButtonFeedbackKind feedbackKind)
    {
        FeedbackKind = feedbackKind;
    }

    public RichButtonFeedbackKind FeedbackKind { get; }
    public bool Handled { get; set; }
}
