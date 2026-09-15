using Microsoft.Maui;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Flexler.Controls;

/// <summary>
/// Windows two-phase native input bridge. WinUI owns capture,
/// click recognition, keyboard focus and Invoke; RichButton owns command and busy.
/// </summary>
internal sealed class NativePrimaryTapBridge : IDisposable
{
    private readonly TapViewBase _owner;
    private readonly Func<Func<Element, Point?>, bool> _beginNativeTouchSequence;
    private readonly Action _cancelNativeTouchSequence;
    private readonly Func<Task> _activateNativeTouchSequenceAsync;
    private readonly Action _reportNativeTouchDown;
    private NativePrimaryTapPlatformView? _platformView;
    private bool _sequenceStarted;
    private bool _physicalActivationPending;

    public NativePrimaryTapBridge(
        TapViewBase owner,
        Func<Func<Element, Point?>, bool> beginNativeTouchSequence,
        Action cancelNativeTouchSequence,
        Func<Task> activateNativeTouchSequenceAsync,
        Action reportNativeTouchDown,
        Action<NativeVerticalFlickDirection>? reportNativeVerticalFlick = null,
        Func<bool>? hasNativeVerticalFlickSubscribers = null)
    {
        _owner = owner;
        _beginNativeTouchSequence = beginNativeTouchSequence;
        _cancelNativeTouchSequence = cancelNativeTouchSequence;
        _activateNativeTouchSequenceAsync = activateNativeTouchSequenceAsync;
        _reportNativeTouchDown = reportNativeTouchDown;
    }

    public void Connect(IElementHandler? handler)
    {
        Disconnect();
        if (handler?.PlatformView is not NativePrimaryTapPlatformView platformView)
            return;

        _platformView = platformView;
        platformView.NativePointerPressed = OnPointerPressed;
        platformView.NativePointerCompleted = CompletePhysicalSequence;
        platformView.NativePointerCancelled = CompletePhysicalSequence;
        platformView.NativeKeyPressed = OnKeyDown;
        platformView.NativeKeyCompleted = OnKeyUp;
        platformView.LostFocus += OnLostFocus;
        platformView.Click += OnClick;
        SynchronizeAvailability();
    }

    public void Disconnect()
    {
        if (_platformView is { } platformView)
        {
            platformView.NativePointerPressed = null;
            platformView.NativePointerCompleted = null;
            platformView.NativePointerCancelled = null;
            platformView.NativeKeyPressed = null;
            platformView.NativeKeyCompleted = null;
            platformView.LostFocus -= OnLostFocus;
            platformView.Click -= OnClick;
        }

        _platformView = null;
        _physicalActivationPending = false;
        CancelSequence();
    }

    public void Dispose() => Disconnect();

    public void SynchronizeAvailability()
    {
        var available = _owner.IsEnabled && !_owner.IsBusy && !_owner.InputTransparent;
        if (_platformView is { } platformView)
            platformView.ApplyAvailability(_owner);
        if (!available)
            CancelSequence();
    }

    private bool BeginSequence(Func<Element, Point?>? resolvePosition = null)
    {
        if (_sequenceStarted)
            return true;
        if (!_owner.IsEnabled || _owner.IsBusy || _owner.InputTransparent)
            return false;

        _sequenceStarted = _beginNativeTouchSequence(resolvePosition ?? (_ => null));
        if (_sequenceStarted)
            _reportNativeTouchDown();
        return _sequenceStarted;
    }

    private void CancelSequence()
    {
        _sequenceStarted = false;
        _cancelNativeTouchSequence();
    }

    private void OnPointerPressed(PointerRoutedEventArgs args)
    {
        if (_platformView is not { } view)
            return;
        var point = args.GetCurrentPoint(view);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        _physicalActivationPending = true;
        BeginSequence(relativeTo => relativeTo.Handler?.PlatformView is UIElement relativeView
            ? ToMauiPoint(args.GetCurrentPoint(relativeView).Position)
            : null);
    }

    private void OnKeyDown(KeyRoutedEventArgs args)
    {
        if (args.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            _physicalActivationPending = true;
            BeginSequence();
        }
        else if (args.Key == VirtualKey.Escape)
            CancelSequence();
    }

    private void OnKeyUp(KeyRoutedEventArgs args)
    {
        if (args.Key is VirtualKey.Enter or VirtualKey.Space)
            CompletePhysicalSequence();
    }

    private void CompletePhysicalSequence()
    {
        CancelSequence();
        _physicalActivationPending = false;
    }

    private void OnLostFocus(object sender, RoutedEventArgs args) => CompletePhysicalSequence();

    private async void OnClick(object sender, RoutedEventArgs args)
    {
        // UI Automation Invoke has no preceding pointer/key event, so it begins
        // a fresh sequence here. Physical activation consumes the down snapshot.
        if ((_physicalActivationPending && !_sequenceStarted) || !BeginSequence())
            return;
        _sequenceStarted = false;
        await _activateNativeTouchSequenceAsync();
    }

    private static Point ToMauiPoint(global::Windows.Foundation.Point point) => new(point.X, point.Y);
}
