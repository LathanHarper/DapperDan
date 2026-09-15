

using Android.OS;
using Android.Views;
using Microsoft.Maui;
using System.Threading;
using AView = Android.Views.View;

namespace Flexler.Controls;

// Source-derived race carve from .NET MAUI Controls 10.0.20:
// commit 0d1705adc4a6b4ec531e316ec956755abbe059c5
// - GesturePlatformManager.Android.cs: native Touch lifecycle/focus cleanup
// - TapAndPanGestureDetector.cs: Android GestureDetector delegation
// - InnerGestureListener.cs: OnDown interest and single-tap completion
// One shared bridge hardcodes one primary single tap and deletes generic routing.
internal sealed class NativePrimaryTapBridge : IDisposable
{
    private const long MaximumVerticalFlickDurationMilliseconds = 500;
    private const float MinimumVerticalFlickDistanceDip = 44f;
    private const float MinimumVerticalFlickVelocityDipPerSecond = 350f;
    private const float VerticalFlickDominanceRatio = 1.2f;

    private static long _nextInstanceId;

    private readonly Func<Task> _activateNativeTouchSequenceAsync;
    private readonly Func<Func<Element, Point?>?, bool> _beginNativeTouchSequence;
    private readonly Action _cancelNativeTouchSequence;
    private readonly Func<bool>? _hasNativeVerticalFlickSubscribers;
    private readonly long _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly TapViewBase _owner;
    private readonly Action _reportNativeTouchDown;
    private readonly Action<NativeVerticalFlickDirection>? _reportNativeVerticalFlick;
    private DirectTouchGestureListener? _gestureListener;
    private GestureDetector? _gestureDetector;
    private AView? _platformView;
    private bool _isHoldingVerticalFlickTouch;
    private bool _platformViewWasClickable;
    private bool _platformViewWasFocusable;
    private bool _platformViewSoundEffectsWereEnabled;
    private long _verticalFlickDownTimeMilliseconds;
    private bool _verticalFlickWasReported;
    private float _verticalFlickStartX;
    private float _verticalFlickStartY;
    private int _connectionSequence;
    private long _suppressNativeClickUntilUptimeMilliseconds;
    private bool _directTouchClickInProgress;

    public NativePrimaryTapBridge(
        TapViewBase owner,
        Func<Func<Element, Point?>?, bool> beginNativeTouchSequence,
        Action cancelNativeTouchSequence,
        Func<Task> activateNativeTouchSequenceAsync,
        Action reportNativeTouchDown,
        Action<NativeVerticalFlickDirection>? reportNativeVerticalFlick = null,
        Func<bool>? hasNativeVerticalFlickSubscribers = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _beginNativeTouchSequence = beginNativeTouchSequence ??
            throw new ArgumentNullException(nameof(beginNativeTouchSequence));
        _cancelNativeTouchSequence = cancelNativeTouchSequence ??
            throw new ArgumentNullException(nameof(cancelNativeTouchSequence));
        _activateNativeTouchSequenceAsync = activateNativeTouchSequenceAsync ??
            throw new ArgumentNullException(nameof(activateNativeTouchSequenceAsync));
        _reportNativeTouchDown = reportNativeTouchDown ??
            throw new ArgumentNullException(nameof(reportNativeTouchDown));
        _reportNativeVerticalFlick = reportNativeVerticalFlick;
        _hasNativeVerticalFlickSubscribers = hasNativeVerticalFlickSubscribers;
    }

    public void Connect(IElementHandler? handler)
    {
        Disconnect();

        if (handler?.PlatformView is not AView platformView)
            return;

        _platformView = platformView;
        _platformViewWasClickable = platformView.Clickable;
        _platformViewWasFocusable = platformView.Focusable;
        _platformViewSoundEffectsWereEnabled = platformView.SoundEffectsEnabled;
        _gestureListener = new DirectTouchGestureListener(this);
        _gestureDetector = new GestureDetector(
            platformView.Context,
            _gestureListener);
        _gestureDetector.IsLongpressEnabled = false;

        platformView.Clickable = true;
        platformView.Focusable = true;
        platformView.SoundEffectsEnabled = false;
        platformView.Touch += OnPlatformViewTouched;
        platformView.Click += OnPlatformViewClicked;
        _connectionSequence++;
        SynchronizeAvailability();

        Console.WriteLine(
            $"DIRECT_TOUCH|bridge|stage=connected|control={_owner.GetType().Name}|id={_owner.AutomationId}|instance={_instanceId}|connection={_connectionSequence}");
    }

    public void Disconnect()
    {
        var hadNativeTouchBridge = _platformView is not null;

        if (_platformView is not null)
        {
            if (_isHoldingVerticalFlickTouch)
            {
                _platformView.Parent?.RequestDisallowInterceptTouchEvent(false);
            }

            _platformView.Touch -= OnPlatformViewTouched;
            _platformView.Click -= OnPlatformViewClicked;
            _platformView.Clickable = _platformViewWasClickable;
            _platformView.Focusable = _platformViewWasFocusable;
            _platformView.SoundEffectsEnabled = _platformViewSoundEffectsWereEnabled;
            _platformView.Enabled = _owner.IsEnabled;
        }

        _gestureDetector?.Dispose();
        _gestureListener?.Dispose();
        _gestureDetector = null;
        _gestureListener = null;
        _platformView = null;
        _isHoldingVerticalFlickTouch = false;
        _verticalFlickWasReported = false;
        _suppressNativeClickUntilUptimeMilliseconds = 0;
        _cancelNativeTouchSequence();

        if (hadNativeTouchBridge)
        {
            Console.WriteLine(
                $"DIRECT_TOUCH|bridge|stage=disconnected|control={_owner.GetType().Name}|id={_owner.AutomationId}|instance={_instanceId}|connection={_connectionSequence}");
        }
    }

    public void Dispose() => Disconnect();

    public void SynchronizeAvailability()
    {
        if (_platformView is not null)
        {
            // MAUI InputTransparent changes hit testing through a wrapper; it
            // does not remove Android's accessibility click action.
            _platformView.Enabled =
                _owner.IsEnabled && !_owner.IsBusy && !_owner.InputTransparent;
        }
    }

    private void OnPlatformViewTouched(object? sender, AView.TouchEventArgs e)
    {
        if (e.Event is null)
            return;

        if (e.Event.ActionMasked == MotionEventActions.Down)
        {
            _isHoldingVerticalFlickTouch = HasNativeVerticalFlickSubscribers();
            if (_isHoldingVerticalFlickTouch)
            {
                _verticalFlickStartX = e.Event.GetX();
                _verticalFlickStartY = e.Event.GetY();
                _verticalFlickDownTimeMilliseconds = e.Event.EventTime;
                _verticalFlickWasReported = false;
                _platformView?.Parent?.RequestDisallowInterceptTouchEvent(true);
            }
        }

        if (e.Event.ActionMasked == MotionEventActions.Cancel)
            _gestureListener?.Cancel("platform-cancel");
        else if (e.Event.ActionMasked == MotionEventActions.PointerDown)
            _gestureListener?.Cancel("pointer-down");

        var wasHandled = _gestureDetector?.OnTouchEvent(e.Event) == true;

        if (_isHoldingVerticalFlickTouch &&
            e.Event.ActionMasked == MotionEventActions.Up)
        {
            wasHandled =
                TryReportNativeVerticalFlickFromTerminalEvent(e.Event) ||
                wasHandled;
        }

        e.Handled = wasHandled;

        if (_isHoldingVerticalFlickTouch &&
            (e.Event.ActionMasked == MotionEventActions.Up ||
             e.Event.ActionMasked == MotionEventActions.Cancel ||
             e.Event.ActionMasked == MotionEventActions.PointerDown))
        {
            _platformView?.Parent?.RequestDisallowInterceptTouchEvent(false);
            _isHoldingVerticalFlickTouch = false;
            _verticalFlickWasReported = false;
        }
    }

    private async void OnPlatformViewClicked(object? sender, EventArgs e)
    {
        if (SystemClock.UptimeMillis() <= _suppressNativeClickUntilUptimeMilliseconds)
        {
            Console.WriteLine($"DIRECT_TOUCH|click-suppressed|id={_owner.AutomationId}");
            _suppressNativeClickUntilUptimeMilliseconds = 0;
            _cancelNativeTouchSequence();
            return;
        }

        Console.WriteLine($"DIRECT_TOUCH|click|id={_owner.AutomationId}");
        // Accessibility/native keyboard clicks have no pointer-down event.
        // Only that route may start a fresh sequence; a cancelled physical
        // sequence can never be silently restarted by a delayed completion.
        if (!_directTouchClickInProgress && !_beginNativeTouchSequence(null))
            return;
        await _activateNativeTouchSequenceAsync();
    }

    private void SuppressImmediateNativeClick()
    {
        _suppressNativeClickUntilUptimeMilliseconds = SystemClock.UptimeMillis() + 250;
    }

    private bool PerformDirectTouchClick()
    {
        _directTouchClickInProgress = true;
        try
        {
            return _platformView?.PerformClick() == true;
        }
        finally
        {
            _directTouchClickInProgress = false;
        }
    }

    private bool ReportNativeVerticalFlick(float velocityX, float velocityY)
    {
        if (_reportNativeVerticalFlick is null ||
            !HasNativeVerticalFlickSubscribers())
            return false;

        var absoluteVelocityX = Math.Abs(velocityX);
        var absoluteVelocityY = Math.Abs(velocityY);

        if (absoluteVelocityY == 0 || absoluteVelocityY <= absoluteVelocityX)
            return false;

        var direction = velocityY < 0
            ? NativeVerticalFlickDirection.Up
            : NativeVerticalFlickDirection.Down;

        return ReportNativeVerticalFlick(direction);
    }

    private bool TryReportNativeVerticalFlickFromTerminalEvent(MotionEvent e)
    {
        if (_verticalFlickWasReported)
        {
            return true;
        }

        var elapsedMilliseconds = Math.Max(
            1,
            e.EventTime - _verticalFlickDownTimeMilliseconds);
        var deltaX = e.GetX() - _verticalFlickStartX;
        var deltaY = e.GetY() - _verticalFlickStartY;
        var absoluteDeltaX = Math.Abs(deltaX);
        var absoluteDeltaY = Math.Abs(deltaY);
        var density = _platformView?.Resources?.DisplayMetrics?.Density ?? 1f;
        var minimumDistance = MinimumVerticalFlickDistanceDip * density;
        var minimumVelocity =
            MinimumVerticalFlickVelocityDipPerSecond * density;
        var velocityY = absoluteDeltaY * 1000f / elapsedMilliseconds;

        if (elapsedMilliseconds > MaximumVerticalFlickDurationMilliseconds ||
            absoluteDeltaY < minimumDistance ||
            absoluteDeltaY < absoluteDeltaX * VerticalFlickDominanceRatio ||
            velocityY < minimumVelocity)
        {
            return false;
        }

        _gestureListener?.Cancel("terminal-fling");
        var direction = deltaY < 0
            ? NativeVerticalFlickDirection.Up
            : NativeVerticalFlickDirection.Down;
        return ReportNativeVerticalFlick(direction);
    }

    private bool ReportNativeVerticalFlick(
        NativeVerticalFlickDirection direction)
    {
        if (_verticalFlickWasReported ||
            _reportNativeVerticalFlick is null ||
            !HasNativeVerticalFlickSubscribers())
        {
            return false;
        }

        _verticalFlickWasReported = true;

        Console.WriteLine(
            $"DIRECT_TOUCH|flick|platform=android|control={_owner.GetType().Name}|id={_owner.AutomationId}|direction={direction}");
        _reportNativeVerticalFlick(direction);
        return true;
    }

    private bool HasNativeVerticalFlickSubscribers() =>
        _hasNativeVerticalFlickSubscribers?.Invoke() == true;

    private Func<Element, Point?> CreateTouchPositionResolver(
        float localX,
        float localY)
    {
        var sourceView = _platformView;

        return relativeTo => ResolveTouchPosition(
            sourceView,
            relativeTo,
            localX,
            localY);
    }

    private static Point? ResolveTouchPosition(
        AView? sourceView,
        Element relativeTo,
        float localX,
        float localY)
    {
        if (sourceView is null ||
            relativeTo?.Handler?.PlatformView is not AView targetView)
        {
            return null;
        }

        var coordinates = new[] { localX, localY };

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            using var matrix = new Android.Graphics.Matrix();
            sourceView.TransformMatrixToGlobal(matrix);
            targetView.TransformMatrixToLocal(matrix);
            matrix.MapPoints(coordinates);
        }
        else
        {
            var sourceLocation = new int[2];
            var targetLocation = new int[2];
            sourceView.GetLocationInWindow(sourceLocation);
            targetView.GetLocationInWindow(targetLocation);
            coordinates[0] += sourceLocation[0] - targetLocation[0];
            coordinates[1] += sourceLocation[1] - targetLocation[1];
        }

        var density = targetView.Resources?.DisplayMetrics?.Density ?? 1f;
        return new Point(
            coordinates[0] / density,
            coordinates[1] / density);
    }

    private sealed class DirectTouchGestureListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly WeakReference<NativePrimaryTapBridge> _bridgeReference;
        private bool _cancelWasReported;
        private bool _touchSequenceStarted;

        public DirectTouchGestureListener(NativePrimaryTapBridge bridge)
        {
            _bridgeReference = new WeakReference<NativePrimaryTapBridge>(bridge);
        }

        public override bool OnDown(MotionEvent e)
        {
            if (!_bridgeReference.TryGetTarget(out var bridge))
                return false;

            _cancelWasReported = false;
            var started = bridge._beginNativeTouchSequence(
                bridge.CreateTouchPositionResolver(e.GetX(), e.GetY()));
            _touchSequenceStarted = started;
            bridge._reportNativeTouchDown();

            if (!started)
                bridge.SuppressImmediateNativeClick();

            Console.WriteLine(
                $"DIRECT_TOUCH|down|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}|started={started}|eventTime={e.EventTime}|downTime={e.DownTime}");
            return started;
        }

        public override bool OnSingleTapUp(MotionEvent e)
        {
            if (!_bridgeReference.TryGetTarget(out var bridge))
            {
                _touchSequenceStarted = false;
                return false;
            }

            Console.WriteLine(
                $"DIRECT_TOUCH|single-tap-up|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}|eventTime={e.EventTime}|downTime={e.DownTime}");
            _touchSequenceStarted = false;
            return bridge.PerformDirectTouchClick();
        }

        public override bool OnScroll(
            MotionEvent? e1,
            MotionEvent e2,
            float distanceX,
            float distanceY)
        {
            var shouldConsume =
                _bridgeReference.TryGetTarget(out var bridge) &&
                bridge.HasNativeVerticalFlickSubscribers();
            Cancel("scroll");
            // Keep ownership of the native gesture after the tap sequence is
            // cancelled so GestureDetector can still deliver the terminal
            // OnFling callback. Returning false here lets the view hierarchy
            // intercept the remainder of the swipe and strands the flick.
            return shouldConsume;
        }

        public override bool OnFling(
            MotionEvent? e1,
            MotionEvent e2,
            float velocityX,
            float velocityY)
        {
            Cancel("fling");

            if (!_bridgeReference.TryGetTarget(out var bridge))
                return false;

            return bridge.ReportNativeVerticalFlick(velocityX, velocityY);
        }

        public override void OnLongPress(MotionEvent e) => Cancel("long-press");

        public override void OnShowPress(MotionEvent e)
        {
        }

        public void Cancel(string reason)
        {
            if (!_bridgeReference.TryGetTarget(out var bridge))
                return;

            if (_touchSequenceStarted && !_cancelWasReported)
            {
                Console.WriteLine(
                    $"DIRECT_TOUCH|cancel|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}|reason={reason}");
                _cancelWasReported = true;
            }

            _touchSequenceStarted = false;
            bridge.SuppressImmediateNativeClick();
            bridge._cancelNativeTouchSequence();
        }
    }
}
