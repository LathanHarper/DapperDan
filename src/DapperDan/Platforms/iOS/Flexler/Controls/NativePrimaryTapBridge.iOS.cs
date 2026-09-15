

using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using UIKit;

namespace Flexler.Controls;

// Source-derived race carve from .NET MAUI Controls 10.0.20:
// commit 0d1705adc4a6b4ec531e316ec956755abbe059c5
// - GesturePlatformManager.iOS.cs: native UITap setup, availability filter,
//   accessibility promotion, weak ownership, and cleanup
// - ResignFirstResponderTouchGestureRecognizer.iOS.cs: subclass and target token
// Accessibility activation also incorporates upstream MAUI commit 09980daa.
internal sealed class NativePrimaryTapBridge : IDisposable
{
    private static long _nextInstanceId;

    private readonly Func<Task> _activateNativeTouchSequenceAsync;
    private readonly Func<Func<Element, Point?>?, bool> _beginNativeTouchSequence;
    private readonly Action _cancelNativeTouchSequence;
    private readonly Func<bool>? _hasNativeVerticalFlickSubscribers;
    private readonly long _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly TapViewBase _owner;
    private readonly Action _reportNativeTouchDown;
    private readonly Action<NativeVerticalFlickDirection>? _reportNativeVerticalFlick;
    private bool _addedButtonAccessibilityTrait;
    private bool _addedNotEnabledAccessibilityTrait;
    private bool _originalAccessibilityRespondsToUserInteraction;
    private int _connectionSequence;
    private DirectTouchVerticalFlickGestureRecognizer? _nativeFlickDownRecognizer;
    private DirectTouchVerticalFlickGestureRecognizer? _nativeFlickUpRecognizer;
    private DirectTouchTapGestureRecognizer? _nativeTouchRecognizer;
    private NativePrimaryTapPlatformView? _platformView;

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

        if (handler?.PlatformView is not NativePrimaryTapPlatformView platformView)
            return;

        _platformView = platformView;
        _addedButtonAccessibilityTrait =
            (platformView.AccessibilityTraits & UIAccessibilityTrait.Button) == 0;
        _originalAccessibilityRespondsToUserInteraction =
            platformView.AccessibilityRespondsToUserInteraction;

        if (_addedButtonAccessibilityTrait)
            platformView.AccessibilityTraits |= UIAccessibilityTrait.Button;

        platformView.AccessibilityRespondsToUserInteraction = true;

        var weakBridge = new WeakReference<NativePrimaryTapBridge>(this);
        _nativeTouchRecognizer = new DirectTouchTapGestureRecognizer(weakBridge)
        {
            ShouldReceiveTouch = (_, touch) => ShouldReceiveTouch(weakBridge, touch)
        };

        if (_reportNativeVerticalFlick is not null)
        {
            _nativeFlickUpRecognizer = CreateNativeVerticalFlickRecognizer(
                weakBridge,
                NativeVerticalFlickDirection.Up,
                UISwipeGestureRecognizerDirection.Up);
            _nativeFlickDownRecognizer = CreateNativeVerticalFlickRecognizer(
                weakBridge,
                NativeVerticalFlickDirection.Down,
                UISwipeGestureRecognizerDirection.Down);
        }

        platformView.AccessibilityActivateCallback = () =>
            weakBridge.TryGetTarget(out var bridge) &&
            bridge.TryActivateAccessibilitySequence();
        platformView.AddGestureRecognizer(_nativeTouchRecognizer);

        if (_nativeFlickUpRecognizer is not null)
            platformView.AddGestureRecognizer(_nativeFlickUpRecognizer);

        if (_nativeFlickDownRecognizer is not null)
            platformView.AddGestureRecognizer(_nativeFlickDownRecognizer);

        _connectionSequence++;
        SynchronizeAvailability();

        Console.WriteLine(
            $"DIRECT_TOUCH|bridge|platform=ios|stage=connected|control={_owner.GetType().Name}|id={_owner.AutomationId}|instance={_instanceId}|connection={_connectionSequence}");
    }

    public void Disconnect()
    {
        var hadNativeTouchBridge = _platformView is not null;

        if (_platformView is not null)
            _platformView.AccessibilityActivateCallback = null;

        if (_nativeTouchRecognizer is not null)
        {
            _nativeTouchRecognizer.Cancel("bridge-disconnect");
            _platformView?.RemoveGestureRecognizer(_nativeTouchRecognizer);
            _nativeTouchRecognizer.DisconnectTargets();
            _nativeTouchRecognizer.Dispose();
        }

        DisconnectNativeVerticalFlickRecognizer(_nativeFlickUpRecognizer);
        DisconnectNativeVerticalFlickRecognizer(_nativeFlickDownRecognizer);

        if (_platformView is not null)
        {
            if (_addedButtonAccessibilityTrait)
                _platformView.AccessibilityTraits &= ~UIAccessibilityTrait.Button;

            if (_addedNotEnabledAccessibilityTrait)
            {
                _platformView.AccessibilityTraits &=
                    ~UIAccessibilityTrait.NotEnabled;
            }

            _platformView.AccessibilityRespondsToUserInteraction =
                _originalAccessibilityRespondsToUserInteraction;
        }

        _nativeTouchRecognizer = null;
        _nativeFlickUpRecognizer = null;
        _nativeFlickDownRecognizer = null;
        _platformView = null;
        _addedButtonAccessibilityTrait = false;
        _addedNotEnabledAccessibilityTrait = false;
        _cancelNativeTouchSequence();

        if (hadNativeTouchBridge)
        {
            Console.WriteLine(
                $"DIRECT_TOUCH|bridge|platform=ios|stage=disconnected|control={_owner.GetType().Name}|id={_owner.AutomationId}|instance={_instanceId}|connection={_connectionSequence}");
        }
    }

    public void Dispose() => Disconnect();

    public void SynchronizeAvailability()
    {
        if (_nativeTouchRecognizer is null || _platformView is null)
            return;

        var isAvailable =
            _owner.IsEnabled && !_owner.IsBusy && !_owner.InputTransparent;

        if (!isAvailable)
            _nativeTouchRecognizer.Cancel("availability");

        SynchronizeAccessibilityAvailability(isAvailable);
        _nativeTouchRecognizer.Enabled = isAvailable;

        if (_nativeFlickUpRecognizer is not null)
            _nativeFlickUpRecognizer.Enabled = isAvailable;

        if (_nativeFlickDownRecognizer is not null)
            _nativeFlickDownRecognizer.Enabled = isAvailable;
    }

    private DirectTouchVerticalFlickGestureRecognizer CreateNativeVerticalFlickRecognizer(
        WeakReference<NativePrimaryTapBridge> weakBridge,
        NativeVerticalFlickDirection direction,
        UISwipeGestureRecognizerDirection nativeDirection) =>
        new(weakBridge, direction, nativeDirection)
        {
            ShouldReceiveTouch = (_, touch) =>
                ShouldReceiveVerticalFlickTouch(weakBridge, touch)
        };

    private void DisconnectNativeVerticalFlickRecognizer(
        DirectTouchVerticalFlickGestureRecognizer? recognizer)
    {
        if (recognizer is null)
            return;

        _platformView?.RemoveGestureRecognizer(recognizer);
        recognizer.DisconnectTargets();
        recognizer.Dispose();
    }

    private void SynchronizeAccessibilityAvailability(bool isAvailable)
    {
        if (_platformView is not { } platformView)
            return;

        platformView.AccessibilityRespondsToUserInteraction = isAvailable;

        if (isAvailable)
        {
            if (_addedNotEnabledAccessibilityTrait)
            {
                platformView.AccessibilityTraits &=
                    ~UIAccessibilityTrait.NotEnabled;
                _addedNotEnabledAccessibilityTrait = false;
            }

            return;
        }

        if ((platformView.AccessibilityTraits & UIAccessibilityTrait.NotEnabled) != 0)
            return;

        platformView.AccessibilityTraits |= UIAccessibilityTrait.NotEnabled;
        _addedNotEnabledAccessibilityTrait = true;
    }

    private static bool ShouldReceiveTouch(
        WeakReference<NativePrimaryTapBridge> weakBridge,
        UITouch touch)
    {
        if (!weakBridge.TryGetTarget(out var bridge) ||
            bridge._platformView is not { } platformView ||
            !bridge._owner.IsEnabled ||
            bridge._owner.IsBusy ||
            bridge._owner.InputTransparent ||
            touch.View is not { } touchedView)
        {
            return false;
        }

        return touchedView == platformView ||
            touchedView.IsDescendantOfView(platformView);
    }

    private static bool ShouldReceiveVerticalFlickTouch(
        WeakReference<NativePrimaryTapBridge> weakBridge,
        UITouch touch) =>
        weakBridge.TryGetTarget(out var bridge) &&
        bridge.HasNativeVerticalFlickSubscribers() &&
        ShouldReceiveTouch(weakBridge, touch);

    private Func<Element, Point?> CreateTouchPositionResolver(
        CGPoint localTouchPoint)
    {
        var sourceView = _platformView;

        return relativeTo =>
        {
            if (sourceView is null ||
                relativeTo?.Handler?.PlatformView is not UIView targetView)
            {
                return null;
            }

            var targetPoint = sourceView.ConvertPointToView(
                localTouchPoint,
                targetView);
            return new Point(targetPoint.X, targetPoint.Y);
        };
    }

    private bool TryActivateAccessibilitySequence()
    {
        var started = _beginNativeTouchSequence(null);
        Console.WriteLine(
            $"DIRECT_TOUCH|accessibility-activate|platform=ios|control={_owner.GetType().Name}|id={_owner.AutomationId}|started={started}");

        if (!started)
            return false;

        CompleteNativeTouchSequence();
        return true;
    }

    private async void CompleteNativeTouchSequence()
    {
        await _activateNativeTouchSequenceAsync();
    }

    private void ReportNativeVerticalFlick(NativeVerticalFlickDirection direction)
    {
        _nativeTouchRecognizer?.Cancel(
            $"flick-{direction.ToString().ToLowerInvariant()}");
        _cancelNativeTouchSequence();

        if (_reportNativeVerticalFlick is null ||
            !HasNativeVerticalFlickSubscribers())
            return;

        Console.WriteLine(
            $"DIRECT_TOUCH|flick|platform=ios|control={_owner.GetType().Name}|id={_owner.AutomationId}|direction={direction}");
        _reportNativeVerticalFlick(direction);
    }

    private bool HasNativeVerticalFlickSubscribers() =>
        _hasNativeVerticalFlickSubscribers?.Invoke() == true;

    private sealed class DirectTouchVerticalFlickGestureRecognizer : UISwipeGestureRecognizer
    {
        private readonly WeakReference<NativePrimaryTapBridge> _bridgeReference;
        private readonly NativeVerticalFlickDirection _direction;
        private UIGestureRecognizer.Token? _targetToken;

        public DirectTouchVerticalFlickGestureRecognizer(
            WeakReference<NativePrimaryTapBridge> bridgeReference,
            NativeVerticalFlickDirection direction,
            UISwipeGestureRecognizerDirection nativeDirection)
        {
            _bridgeReference = bridgeReference;
            _direction = direction;
            Direction = nativeDirection;
            NumberOfTouchesRequired = 1;
            _targetToken = AddTarget(OnNativeVerticalFlickRecognized);
        }

        public void DisconnectTargets()
        {
            ShouldReceiveTouch = null;

            if (_targetToken is not null)
                RemoveTarget(_targetToken);

            _targetToken = null;
        }

        private void OnNativeVerticalFlickRecognized(NSObject sender)
        {
            if (State != UIGestureRecognizerState.Ended ||
                !_bridgeReference.TryGetTarget(out var bridge))
            {
                return;
            }

            bridge.ReportNativeVerticalFlick(_direction);
        }
    }

    private sealed class DirectTouchTapGestureRecognizer : UITapGestureRecognizer
    {
        private readonly WeakReference<NativePrimaryTapBridge> _bridgeReference;
        private bool _cancelWasReported;
        private bool _recognizedAtReset;
        private bool _touchSequenceStarted;
        private UIGestureRecognizer.Token? _targetToken;

        public DirectTouchTapGestureRecognizer(
            WeakReference<NativePrimaryTapBridge> bridgeReference)
        {
            _bridgeReference = bridgeReference;
            NumberOfTapsRequired = 1;
            NumberOfTouchesRequired = 1;
            ButtonMaskRequired = UIEventButtonMask.Primary;
            _targetToken = AddTarget(OnNativeTapRecognized);
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            var hadActiveSequence = _touchSequenceStarted || _recognizedAtReset;
            base.TouchesBegan(touches, evt);

            if (hadActiveSequence)
            {
                Cancel("pointer-down");
                FailIfPossible();
                return;
            }

            if (touches.Count != 1 ||
                State != UIGestureRecognizerState.Possible ||
                !_bridgeReference.TryGetTarget(out var bridge))
            {
                FailIfPossible();
                return;
            }

            _cancelWasReported = false;
            _recognizedAtReset = false;
            var touch = touches.AnyObject as UITouch;
            var positionResolver = touch is null || bridge._platformView is null
                ? null
                : bridge.CreateTouchPositionResolver(
                    touch.LocationInView(bridge._platformView));
            var started = bridge._beginNativeTouchSequence(positionResolver);
            _touchSequenceStarted = started;
            bridge._reportNativeTouchDown();

            Console.WriteLine(
                $"DIRECT_TOUCH|down|platform=ios|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}|started={started}|timestamp={touch?.Timestamp:0.000}");

            if (!started)
                State = UIGestureRecognizerState.Failed;
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);
            Cancel("platform-cancel");
        }

        public override void Reset()
        {
            if (_touchSequenceStarted)
            {
                if (State == UIGestureRecognizerState.Ended)
                    _recognizedAtReset = true;
                else
                    Cancel($"reset-{State.ToString().ToLowerInvariant()}");
            }

            base.Reset();
        }

        public void Cancel(string reason)
        {
            if ((_touchSequenceStarted || _recognizedAtReset) &&
                _bridgeReference.TryGetTarget(out var bridge))
            {
                if (!_cancelWasReported)
                {
                    Console.WriteLine(
                        $"DIRECT_TOUCH|cancel|platform=ios|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}|reason={reason}");
                    _cancelWasReported = true;
                }

                bridge._cancelNativeTouchSequence();
            }

            _touchSequenceStarted = false;
            _recognizedAtReset = false;
        }

        public void DisconnectTargets()
        {
            ShouldReceiveTouch = null;

            if (_targetToken is not null)
                RemoveTarget(_targetToken);

            _targetToken = null;
        }

        private void OnNativeTapRecognized(NSObject sender)
        {
            var recognized =
                State == UIGestureRecognizerState.Ended || _recognizedAtReset;

            if (!recognized ||
                (!_touchSequenceStarted && !_recognizedAtReset) ||
                !_bridgeReference.TryGetTarget(out var bridge))
            {
                return;
            }

            _touchSequenceStarted = false;
            _recognizedAtReset = false;
            Console.WriteLine(
                $"DIRECT_TOUCH|click|platform=ios|control={bridge._owner.GetType().Name}|id={bridge._owner.AutomationId}");
            bridge.CompleteNativeTouchSequence();
        }

        private void FailIfPossible()
        {
            if (State == UIGestureRecognizerState.Possible)
                State = UIGestureRecognizerState.Failed;
        }
    }
}
