using Android.OS;
using Android.Views;
using Microsoft.Maui;
using System.Threading;
using AView = Android.Views.View;

namespace CodeCrafty.DapperDan.Controls;

// Source-derived race carve from .NET MAUI Controls 10.0.20:
// commit 0d1705adc4a6b4ec531e316ec956755abbe059c5
// - GesturePlatformManager.Android.cs: native Touch lifecycle/focus cleanup
// - TapAndPanGestureDetector.cs: Android GestureDetector delegation
// - InnerGestureListener.cs: OnDown interest and single-tap completion
// One shared bridge hardcodes one primary single tap and deletes generic routing.
internal sealed class NativePrimaryTapBridge : IDisposable
{
    private static long _nextInstanceId;

    private readonly Func<Task> _activateNativeTouchSequenceAsync;
    private readonly Func<Func<Element, Point?>, bool> _beginNativeTouchSequence;
    private readonly Action _cancelNativeTouchSequence;
    private readonly long _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly TapViewBase _owner;
    private readonly Action _reportNativeTouchDown;
    private DirectTouchGestureListener _gestureListener;
    private GestureDetector _gestureDetector;
    private AView _platformView;
    private bool _platformViewWasClickable;
    private bool _platformViewWasFocusable;
    private bool _platformViewSoundEffectsWereEnabled;
    private int _connectionSequence;
    private long _suppressNativeClickUntilUptimeMilliseconds;

    public NativePrimaryTapBridge(
        TapViewBase owner,
        Func<Func<Element, Point?>, bool> beginNativeTouchSequence,
        Action cancelNativeTouchSequence,
        Func<Task> activateNativeTouchSequenceAsync,
        Action reportNativeTouchDown)
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
    }

    public void Connect(IElementHandler handler)
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

    private void OnPlatformViewTouched(object sender, AView.TouchEventArgs e)
    {
        if (e.Event is null)
            return;

        if (e.Event.ActionMasked == MotionEventActions.Cancel)
            _gestureListener?.Cancel("platform-cancel");
        else if (e.Event.ActionMasked == MotionEventActions.PointerDown)
            _gestureListener?.Cancel("pointer-down");

        e.Handled = _gestureDetector?.OnTouchEvent(e.Event) == true;
    }

    private async void OnPlatformViewClicked(object sender, EventArgs e)
    {
        if (SystemClock.UptimeMillis() <= _suppressNativeClickUntilUptimeMilliseconds)
        {
            Console.WriteLine($"DIRECT_TOUCH|click-suppressed|id={_owner.AutomationId}");
            _suppressNativeClickUntilUptimeMilliseconds = 0;
            _cancelNativeTouchSequence();
            return;
        }

        Console.WriteLine($"DIRECT_TOUCH|click|id={_owner.AutomationId}");
        await _activateNativeTouchSequenceAsync();
    }

    private void SuppressImmediateNativeClick()
    {
        _suppressNativeClickUntilUptimeMilliseconds = SystemClock.UptimeMillis() + 250;
    }

    private bool PerformDirectTouchClick() =>
        _platformView?.PerformClick() == true;

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
        AView sourceView,
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

    private sealed class DirectTouchGestureListener : Java.Lang.Object, GestureDetector.IOnGestureListener
    {
        private readonly WeakReference<NativePrimaryTapBridge> _bridgeReference;
        private bool _cancelWasReported;
        private bool _touchSequenceStarted;

        public DirectTouchGestureListener(NativePrimaryTapBridge bridge)
        {
            _bridgeReference = new WeakReference<NativePrimaryTapBridge>(bridge);
        }

        public bool OnDown(MotionEvent e)
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

        public bool OnSingleTapUp(MotionEvent e)
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

        public bool OnScroll(
            MotionEvent e1,
            MotionEvent e2,
            float distanceX,
            float distanceY)
        {
            Cancel("scroll");
            return false;
        }

        public bool OnFling(
            MotionEvent e1,
            MotionEvent e2,
            float velocityX,
            float velocityY)
        {
            Cancel("fling");
            return false;
        }

        public void OnLongPress(MotionEvent e) => Cancel("long-press");

        public void OnShowPress(MotionEvent e)
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
