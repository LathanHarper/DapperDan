using Android.OS;
using Android.Views;
using CodeCrafty.DapperDan.Platforms.Android;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using ARect = Android.Graphics.Rect;
using AView = Android.Views.View;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

public partial class PanelBossBody_DefaultView
{
    private PanelBossViewportInsetLease? _viewportInsetLease;

    partial void InitializePlatformViewportInsetContract()
    {
        Loaded += OnViewportInsetViewLoaded;
        Unloaded += OnViewportInsetViewUnloaded;
    }

    partial void ApplyPlatformViewportInsetContract()
    {
        if (Handler?.PlatformView is not AView platformView)
        {
            ClearPlatformViewportInsetContract();
            ReportPlatformViewportBottomInset(0);
            return;
        }

        // OnHandlerChanged can run before Android attaches the native view.
        // Loaded is the point where RootView and ViewTreeObserver represent
        // the live window hierarchy rather than a temporary floating tree.
        if (!IsLoaded)
        {
            ClearPlatformViewportInsetContract();
            return;
        }

        var currentLease = _viewportInsetLease;
        if (currentLease?.MatchesCurrentRegistration(platformView) == true)
        {
            currentLease.QueueCurrentInsetReport();
            return;
        }

        ClearPlatformViewportInsetContract();

        var lease = PanelBossViewportInsetLease.TryAttach(this, platformView);
        if (lease is null)
        {
            // Native teardown won the attach race. The next Loaded or handler
            // application gets another chance to acquire a fresh lease.
            return;
        }

        _viewportInsetLease = lease;
        lease.QueueCurrentInsetReport();
    }

    partial void ClearPlatformViewportInsetContract()
    {
        // Retire the current generation before touching Android. A callback
        // already captured by ViewTreeObserver will then become a no-op.
        var lease = _viewportInsetLease;
        _viewportInsetLease = null;
        lease?.Detach();
    }

    private void OnViewportInsetViewLoaded(object? sender, EventArgs e) =>
        ApplyPlatformViewportInsetContract();

    private void OnViewportInsetViewUnloaded(object? sender, EventArgs e)
    {
        // Handler reconnect churn can deliver an old native Unloaded after
        // the replacement view is already loaded.
        if (IsLoaded)
        {
            return;
        }

        ClearPlatformViewportInsetContract();
        // IME overlap is transient. Stable system-bar clearance belongs to
        // the page layout and is intentionally preserved while it is cached.
        ReportPlatformViewportBottomInset(0);
    }

    private void RetirePlatformViewportInsetLease(PanelBossViewportInsetLease lease)
    {
        if (ReferenceEquals(_viewportInsetLease, lease))
        {
            _viewportInsetLease = null;
        }

        lease.Detach();
    }

    /// <summary>
    /// Owns one Android global-layout subscription for one loaded MAUI
    /// handler generation. Detaching the lease invalidates delayed callbacks
    /// before their native views can be released.
    /// </summary>
    private sealed class PanelBossViewportInsetLease : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly WeakReference<PanelBossBody_DefaultView> _ownerReference;
        private readonly WeakReference<AView> _contentViewReference;
        private readonly WeakReference<AView> _rootViewReference;
        private ViewTreeObserver? _registeredObserver;
        private bool _isActive;

        private PanelBossViewportInsetLease(
            PanelBossBody_DefaultView owner,
            AView contentView,
            AView rootView,
            ViewTreeObserver observer)
        {
            _ownerReference = new WeakReference<PanelBossBody_DefaultView>(owner);
            _contentViewReference = new WeakReference<AView>(contentView);
            _rootViewReference = new WeakReference<AView>(rootView);
            _registeredObserver = observer;
        }

        public static PanelBossViewportInsetLease? TryAttach(
            PanelBossBody_DefaultView owner,
            AView contentView)
        {
            if (!TryGetLiveRegistrationTarget(
                    contentView,
                    out var rootView,
                    out var observer))
            {
                return null;
            }

            var lease = new PanelBossViewportInsetLease(
                owner,
                contentView,
                rootView,
                observer);

            try
            {
                observer.AddOnGlobalLayoutListener(lease);
                lease._isActive = true;
                return lease;
            }
            catch (ObjectDisposedException)
            {
                lease._registeredObserver = null;
                return null;
            }
            catch (Java.Lang.IllegalStateException)
            {
                lease._registeredObserver = null;
                return null;
            }
        }

        public bool MatchesCurrentRegistration(AView contentView)
        {
            if (!_isActive ||
                _registeredObserver is not { } registeredObserver ||
                !_contentViewReference.TryGetTarget(out var registeredContentView) ||
                !ReferenceEquals(registeredContentView, contentView) ||
                !_rootViewReference.TryGetTarget(out var registeredRootView) ||
                !TryGetLiveRegistrationTarget(
                    contentView,
                    out var currentRootView,
                    out var currentObserver))
            {
                return false;
            }

            return ReferenceEquals(registeredRootView, currentRootView) &&
                ReferenceEquals(registeredObserver, currentObserver);
        }

        public void QueueCurrentInsetReport()
        {
            if (!_isActive || !_rootViewReference.TryGetTarget(out var rootView))
            {
                return;
            }

            try
            {
                // This instance method captures this lease generation. It
                // cannot accidentally dispatch through a replacement lease.
                rootView.Post(ReportCurrentInset);
            }
            catch (ObjectDisposedException)
            {
                RetireAfterNativeFailure();
            }
        }

        public void Detach()
        {
            if (!_isActive && _registeredObserver is null)
            {
                return;
            }

            _isActive = false;

            // Android may already hold this Java peer in a dispatch snapshot.
            // Do not dispose it eagerly; inactive callbacks are harmless and
            // normal Java/.NET ownership can collect it after dispatch.

            var registeredObserver = _registeredObserver;
            _registeredObserver = null;
            TryRemoveFromObserver(registeredObserver);

            // Observer identity can change during native attachment churn.
            // Try the view's current observer too, without depending on it.
            if (!_rootViewReference.TryGetTarget(out var rootView))
            {
                return;
            }

            try
            {
                var currentObserver = rootView.ViewTreeObserver;
                if (!ReferenceEquals(currentObserver, registeredObserver))
                {
                    TryRemoveFromObserver(currentObserver);
                }
            }
            catch (ObjectDisposedException)
            {
                // The lease is already inactive. Native teardown won the race.
            }
        }

        public void OnGlobalLayout() =>
            ReportCurrentInset();

        private void ReportCurrentInset()
        {
            if (!_isActive ||
                !_ownerReference.TryGetTarget(out var owner) ||
                !owner.IsLoaded ||
                !_contentViewReference.TryGetTarget(out var contentView) ||
                !ReferenceEquals(owner.Handler?.PlatformView, contentView) ||
                !_rootViewReference.TryGetTarget(out var rootView))
            {
                return;
            }

            double viewportBottomInset;
            double? platformBottomClearance = null;

            try
            {
                if (!contentView.IsAttachedToWindow || !rootView.IsAttachedToWindow)
                {
                    return;
                }

                viewportBottomInset = GetKeyboardBottomInsetDip(contentView, rootView);
                if (TryGetSystemBottomClearanceDip(rootView, out var bottomClearance))
                {
                    platformBottomClearance = bottomClearance;
                }
            }
            catch (ObjectDisposedException)
            {
                RetireAfterNativeFailure();
                return;
            }

            owner.ReportPlatformViewportBottomInset(viewportBottomInset);
            if (platformBottomClearance is { } bottomClearanceValue)
            {
                owner.ReportPlatformBottomClearance(bottomClearanceValue);
            }
        }

        private static bool TryGetLiveRegistrationTarget(
            AView contentView,
            [NotNullWhen(true)] out AView? rootView,
            [NotNullWhen(true)] out ViewTreeObserver? observer)
        {
            rootView = null;
            observer = null;

            try
            {
                if (!contentView.IsAttachedToWindow)
                {
                    return false;
                }

                var currentRootView = contentView.RootView ?? contentView;
                if (!currentRootView.IsAttachedToWindow)
                {
                    return false;
                }

                var currentObserver = currentRootView.ViewTreeObserver;
                if (currentObserver?.IsAlive != true)
                {
                    return false;
                }

                rootView = currentRootView;
                observer = currentObserver;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private void RetireAfterNativeFailure()
        {
            if (_ownerReference.TryGetTarget(out var owner))
            {
                owner.RetirePlatformViewportInsetLease(this);
                return;
            }

            Detach();
        }

        private void TryRemoveFromObserver(ViewTreeObserver? observer)
        {
            if (observer is null)
            {
                return;
            }

            try
            {
                if (observer.IsAlive)
                {
                    observer.RemoveOnGlobalLayoutListener(this);
                }
            }
            catch (ObjectDisposedException)
            {
                // The listener was deactivated before this best-effort remove.
            }
            catch (Java.Lang.IllegalStateException)
            {
                // IsAlive changed between the check and removal.
            }
        }

        private static double GetKeyboardBottomInsetDip(AView contentView, AView rootView)
        {
            var bottomPixels = GetKeyboardBottomInsetPixels(contentView, rootView);
            var density = rootView.Resources?.DisplayMetrics?.Density ?? 1f;
            return density > 0 ? bottomPixels / density : bottomPixels;
        }

        private static bool TryGetSystemBottomClearanceDip(AView view, out double bottomClearance)
        {
            bottomClearance = 0;

            if (!TryGetSystemBottomClearancePixels(view, out var bottomPixels))
            {
                return false;
            }

            var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
            bottomClearance = density > 0 ? bottomPixels / density : bottomPixels;
            return true;
        }

        private static int GetKeyboardBottomInsetPixels(AView contentView, AView rootView)
        {
            var insets = rootView.RootWindowInsets;
            var hasAuthoritativeImeVisibility = false;
            if (OperatingSystem.IsAndroidVersionAtLeast(30) &&
                insets is not null)
            {
                if (!IsModernImeVisible(insets))
                {
                    return 0;
                }

                hasAuthoritativeImeVisibility = true;
            }

            using var visibleFrame = new ARect();
            rootView.GetWindowVisibleDisplayFrame(visibleFrame);

            var rootHeight = rootView.Height;
            if (rootHeight <= 0 || contentView.Height <= 0)
            {
                return 0;
            }

            var contentLocation = new int[2];
            contentView.GetLocationOnScreen(contentLocation);
            var contentBottom = contentLocation[1] + contentView.Height;

            return KeyboardViewportInsetMath.GetResidualBottomInsetPixels(
                contentBottom,
                visibleFrame.Bottom,
                rootHeight,
                hasAuthoritativeImeVisibility);
        }

        [SupportedOSPlatform("android30.0")]
        private static bool IsModernImeVisible(WindowInsets insets) =>
            insets.IsVisible(WindowInsets.Type.Ime());

        private static bool TryGetSystemBottomClearancePixels(AView view, out int bottomPixels)
        {
            bottomPixels = 0;

            var insets = view.RootWindowInsets;
            if (insets is null)
            {
                return false;
            }

            bottomPixels = OperatingSystem.IsAndroidVersionAtLeast(30)
                ? GetModernSystemBottomClearancePixels(insets)
                : insets.StableInsetBottom;
            return true;
        }

        [SupportedOSPlatform("android30.0")]
        private static int GetModernSystemBottomClearancePixels(WindowInsets insets) =>
            insets.GetInsetsIgnoringVisibility(WindowInsets.Type.NavigationBars()).Bottom;
    }
}
