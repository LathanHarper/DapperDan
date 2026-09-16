#if IOS
using System.ComponentModel;
using CoreGraphics;
using CoreImage;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;
using CodeCrafty.DapperDan.Diagnostics;

namespace PrimoMaterial;

internal sealed class CrossFadeRadoDotsHandler
    : ViewHandler<CrossFade_RadoDots, CrossFadeRadoDotsPlatformView>
{
    private static readonly IPropertyMapper<CrossFade_RadoDots, CrossFadeRadoDotsHandler> MaterialMapper =
        new PropertyMapper<CrossFade_RadoDots, CrossFadeRadoDotsHandler>(ViewHandler.ViewMapper)
        {
            [CrossFade_RadoDots.MaterialUpdateKey] = MapMaterial
        };

    public CrossFadeRadoDotsHandler()
        : base(MaterialMapper)
    {
    }

    protected override CrossFadeRadoDotsPlatformView CreatePlatformView()
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialViewEnter);
        try
        {
            var platformView = new CrossFadeRadoDotsPlatformView();
            CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialViewReady);
            return platformView;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(CrashSource.GuardedSeam,
                CrashPoint.FlexlerIosMaterialViewEnter, exception, terminating: false);
            throw;
        }
    }

    protected override void ConnectHandler(CrossFadeRadoDotsPlatformView platformView)
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialConnectEnter);
        try
        {
            base.ConnectHandler(platformView);
            platformView.Connect(VirtualView);
            CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialConnectReady);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(CrashSource.GuardedSeam,
                CrashPoint.FlexlerIosMaterialConnectEnter, exception, terminating: false);
            throw;
        }
    }

    protected override void DisconnectHandler(CrossFadeRadoDotsPlatformView platformView)
    {
        platformView.Disconnect();
        base.DisconnectHandler(platformView);
    }

    private static void MapMaterial(
        CrossFadeRadoDotsHandler handler,
        CrossFade_RadoDots material)
    {
        handler.PlatformView?.UpdateMaterial(material);
    }
}

internal sealed class CrossFadeRadoDotsPlatformView : UIView
{
    private const float DotCellSize = 38f;
    private const uint DotSeed = 0x5241444Fu;
    private static readonly TimeSpan ResizeIdleDelay = TimeSpan.FromMilliseconds(90);

    private static readonly CIContext SharedImageContext = CreateSharedImageContext();

    private readonly UIImageView _imageView;

    private CrossFade_RadoDots? _material;
    private GradientBrush? _inputBrushOne;
    private GradientBrush? _inputBrushTwo;
    private GradientBrush? _failedInputBrushOne;
    private GradientBrush? _failedInputBrushTwo;
    private UIImage? _displayedImage;
    private UIImage? _cachedDotMask;
    private NSTimer? _resizeTimer;
    private CGSize _renderedSize;
    private CGSize _pendingRenderSize;
    private CGSize _cachedMaskLogicalSize;
    private double _renderedScale;
    private double _pendingRenderScale;
    private double _cachedMaskScale;
    private int _cachedMaskPixelWidth;
    private int _cachedMaskPixelHeight;
    private int _brushInvalidationPending;
    private bool _hasRendered;
    private bool _materialDirty = true;
    private bool _premiumUnavailable;
    private bool _isDisconnected = true;
    private bool _isDisposed;

    private static CIContext CreateSharedImageContext()
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialContextEnter);
        try
        {
            var context = CIContext.Create();
            CrashJournal.Checkpoint(CrashPoint.FlexlerIosMaterialContextReady);
            return context;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(CrashSource.GuardedSeam,
                CrashPoint.FlexlerIosMaterialContextEnter, exception, terminating: false);
            throw;
        }
    }

    public CrossFadeRadoDotsPlatformView()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;

        _imageView = new UIImageView
        {
            BackgroundColor = UIColor.Clear,
            ContentMode = UIViewContentMode.ScaleToFill,
            IsAccessibilityElement = false,
            Opaque = false,
            UserInteractionEnabled = false
        };

        AddSubview(_imageView);
    }

    public void Connect(CrossFade_RadoDots material)
    {
        _isDisconnected = false;
        UpdateMaterial(material);
    }

    public void UpdateMaterial(CrossFade_RadoDots material)
    {
        _material = material;
        UpdateBrushSubscriptions(
            material.InputBrushOne,
            material.InputBrushTwo);
        QueueMaterialRender();
    }

    public void Disconnect()
    {
        if (_isDisconnected)
        {
            return;
        }

        _isDisconnected = true;
        _material = null;
        CancelResizeRender();
        RemoveBrushSubscriptions();
        ClearCachedDotMask();
        _materialDirty = false;
        _hasRendered = false;
        _renderedSize = CGSize.Empty;
        _renderedScale = 0;
        ReplaceDisplayedImage(null);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        _imageView.Frame = Bounds;

        if (_isDisconnected || _material is null)
        {
            return;
        }

        var size = Bounds.Size;
        var scale = GetRenderScale();

        if (size.Width <= 0 || size.Height <= 0 || scale <= 0)
        {
            CancelResizeRender();
            ReplaceDisplayedImage(null);
            _hasRendered = false;
            _renderedSize = CGSize.Empty;
            _renderedScale = 0;
            return;
        }

        if (_materialDirty || !_hasRendered)
        {
            CancelResizeRender();
            RenderCurrentBounds();
            return;
        }

        if (SizeEquals(size, _renderedSize)
            && ScaleEquals(scale, _renderedScale))
        {
            return;
        }

        // Keep stretching the last valid image during interactive resizing.
        // Only the latest stable bounds earn another Core Image render.
        ScheduleResizeRender(size, scale);
    }

    public override void MovedToWindow()
    {
        base.MovedToWindow();

        if (Window is not null && !_isDisconnected && !_isDisposed)
        {
            QueueMaterialRender();
        }
        else if (Window is null)
        {
            CancelResizeRender();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
            Disconnect();
            CancelResizeRender();
            ClearCachedDotMask();
            _failedInputBrushOne = null;
            _failedInputBrushTwo = null;
            _imageView.RemoveFromSuperview();
            _imageView.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RenderMaterial(CGSize logicalSize, double scale)
    {
        UIImage? fallback = null;
        UIImage? premium = null;

        if (_inputBrushOne is null)
        {
            ReplaceDisplayedImage(null);
            return;
        }

        try
        {
            fallback = Microsoft.Maui.Controls.Platform.BrushExtensions
                .GetBackgroundImage(this, _inputBrushOne);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"PrimoMaterial CrossFade_RadoDots could not render its iOS InputBrushOne fallback: {exception.Message}");
            ReplaceDisplayedImage(null);
            return;
        }

        if (fallback is null)
        {
            ReplaceDisplayedImage(null);
            return;
        }

        try
        {
            if (_inputBrushTwo is null || _premiumUnavailable)
            {
                var next = fallback;
                fallback = null;
                ReplaceDisplayedImage(next);
                return;
            }

            try
            {
                using var inputTwo = Microsoft.Maui.Controls.Platform.BrushExtensions
                    .GetBackgroundImage(this, _inputBrushTwo);

                if (inputTwo is null)
                {
                    throw new InvalidOperationException(
                        "InputBrushTwo did not produce a native gradient image.");
                }

                premium = CreatePremiumImage(
                    fallback,
                    inputTwo,
                    logicalSize,
                    scale);

                if (premium is null)
                {
                    throw new InvalidOperationException(
                        "Core Image did not produce a material image.");
                }
            }
            catch (Exception exception)
            {
                LatchPremiumFailure(exception);
                var next = fallback;
                fallback = null;
                ReplaceDisplayedImage(next);
                return;
            }

            fallback.Dispose();
            fallback = null;

            var premiumImage = premium;
            premium = null;
            ReplaceDisplayedImage(premiumImage);
        }
        finally
        {
            premium?.Dispose();
            fallback?.Dispose();
        }
    }

    private UIImage? CreatePremiumImage(
        UIImage inputOne,
        UIImage inputTwo,
        CGSize logicalSize,
        double scale)
    {
        var inputOneImage = inputOne.CGImage;
        var inputTwoImage = inputTwo.CGImage;

        if (inputOneImage is null
            || inputTwoImage is null
            || inputOneImage.Width != inputTwoImage.Width
            || inputOneImage.Height != inputTwoImage.Height)
        {
            return null;
        }

        var pixelWidth = checked((int)inputOneImage.Width);
        var pixelHeight = checked((int)inputOneImage.Height);
        var extent = new CGRect(0, 0, pixelWidth, pixelHeight);

        using var inputOneCi = CIImage.FromCGImage(inputOneImage);
        using var inputTwoCi = CIImage.FromCGImage(inputTwoImage);
        using var screenFilter = new CIScreenBlendMode
        {
            InputImage = inputOneCi,
            BackgroundImage = inputTwoCi
        };
        using var multiplyFilter = new CIMultiplyBlendMode
        {
            InputImage = inputOneCi,
            BackgroundImage = inputTwoCi
        };
        using var screenOutput = screenFilter.OutputImage;
        using var multiplyOutput = multiplyFilter.OutputImage;

        if (screenOutput is null || multiplyOutput is null)
        {
            return null;
        }

        using var screen = screenOutput.ImageByCroppingToRect(extent);
        using var multiply = multiplyOutput.ImageByCroppingToRect(extent);
        var maskBitmap = GetOrCreateDotMask(
            pixelWidth,
            pixelHeight,
            logicalSize,
            scale);
        var maskCgImage = maskBitmap.CGImage;

        if (maskCgImage is null)
        {
            return null;
        }

        using var mask = CIImage.FromCGImage(maskCgImage);
        using var blurFilter = new CIGaussianBlur
        {
            InputImage = mask,
            Radius = (float)scale
        };
        using var blurredOutput = blurFilter.OutputImage;

        if (blurredOutput is null)
        {
            return null;
        }

        using var blurredMask = blurredOutput.ImageByCroppingToRect(extent);
        using var blendFilter = new CIBlendWithMask
        {
            InputImage = screen,
            BackgroundImage = multiply,
            MaskImage = blurredMask
        };
        using var blendOutput = blendFilter.OutputImage;

        if (blendOutput is null)
        {
            return null;
        }

        using var output = blendOutput.ImageByCroppingToRect(extent);
        using var outputImage = SharedImageContext.CreateCGImage(output, extent);

        return outputImage is null
            ? null
            : UIImage.FromImage(
                outputImage,
                (System.Runtime.InteropServices.NFloat)scale,
                UIImageOrientation.Up);
    }

    private UIImage GetOrCreateDotMask(
        int pixelWidth,
        int pixelHeight,
        CGSize logicalSize,
        double scale)
    {
        if (_cachedDotMask is not null
            && pixelWidth == _cachedMaskPixelWidth
            && pixelHeight == _cachedMaskPixelHeight
            && SizeEquals(logicalSize, _cachedMaskLogicalSize)
            && ScaleEquals(scale, _cachedMaskScale))
        {
            return _cachedDotMask;
        }

        var next = CreateDotMask(
            pixelWidth,
            pixelHeight,
            logicalSize,
            scale);
        var previous = _cachedDotMask;

        _cachedDotMask = next;
        _cachedMaskPixelWidth = pixelWidth;
        _cachedMaskPixelHeight = pixelHeight;
        _cachedMaskLogicalSize = logicalSize;
        _cachedMaskScale = scale;
        previous?.Dispose();

        return next;
    }

    private void ClearCachedDotMask()
    {
        var previous = _cachedDotMask;
        _cachedDotMask = null;
        _cachedMaskPixelWidth = 0;
        _cachedMaskPixelHeight = 0;
        _cachedMaskLogicalSize = CGSize.Empty;
        _cachedMaskScale = 0;
        previous?.Dispose();
    }

    private static UIImage CreateDotMask(
        int pixelWidth,
        int pixelHeight,
        CGSize logicalSize,
        double scale)
    {
        using var format = new UIGraphicsImageRendererFormat
        {
            Opaque = true,
            Scale = 1
        };
        using var renderer = new UIGraphicsImageRenderer(
            new CGSize(pixelWidth, pixelHeight),
            format);

        return renderer.CreateImage(
            context =>
            {
                var graphics = context.CGContext;
                graphics.SetAllowsAntialiasing(true);
                graphics.SetShouldAntialias(true);
                graphics.SetFillColor(UIColor.Black.CGColor);
                graphics.FillRect(new CGRect(0, 0, pixelWidth, pixelHeight));
                graphics.SetFillColor(UIColor.White.CGColor);

                var columns = Math.Max(
                    1,
                    (int)Math.Ceiling((double)logicalSize.Width / DotCellSize));
                var rows = Math.Max(
                    1,
                    (int)Math.Ceiling((double)logicalSize.Height / DotCellSize));

                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                    {
                        var hash = HashCell(column, row);

                        if (ToUnit(hash) > 0.72f)
                        {
                            continue;
                        }

                        var jitterX =
                            (ToUnit(Hash(hash ^ 0x68BC21EBu)) - 0.5f)
                            * DotCellSize
                            * 0.42f;
                        var jitterY =
                            (ToUnit(Hash(hash ^ 0x02E5BE93u)) - 0.5f)
                            * DotCellSize
                            * 0.42f;
                        var radius =
                            4.5f
                            + (ToUnit(Hash(hash ^ 0x967A889Bu)) * 8.5f);
                        var centerX =
                            (((column + 0.5f) * DotCellSize) + jitterX)
                            * scale;
                        var centerY =
                            (((row + 0.5f) * DotCellSize) + jitterY)
                            * scale;
                        var pixelRadius = radius * scale;

                        graphics.FillEllipseInRect(
                            new CGRect(
                                centerX - pixelRadius,
                                centerY - pixelRadius,
                                pixelRadius * 2,
                                pixelRadius * 2));
                    }
                }
            });
    }

    private void UpdateBrushSubscriptions(
        GradientBrush? inputBrushOne,
        GradientBrush? inputBrushTwo)
    {
        if (_premiumUnavailable
            && (!ReferenceEquals(_failedInputBrushOne, inputBrushOne)
                || !ReferenceEquals(_failedInputBrushTwo, inputBrushTwo)))
        {
            _premiumUnavailable = false;
            _failedInputBrushOne = null;
            _failedInputBrushTwo = null;
        }

        RemoveBrushSubscriptions();

        _inputBrushOne = inputBrushOne;
        _inputBrushTwo = inputBrushTwo;

        if (_inputBrushOne is not null)
        {
            AttachBrush(_inputBrushOne);
        }

        if (_inputBrushTwo is not null
            && !ReferenceEquals(_inputBrushTwo, _inputBrushOne))
        {
            AttachBrush(_inputBrushTwo);
        }
    }

    private void RemoveBrushSubscriptions()
    {
        if (_inputBrushOne is not null)
        {
            DetachBrush(_inputBrushOne);
        }

        if (_inputBrushTwo is not null
            && !ReferenceEquals(_inputBrushTwo, _inputBrushOne))
        {
            DetachBrush(_inputBrushTwo);
        }

        _inputBrushOne = null;
        _inputBrushTwo = null;
    }

    private void AttachBrush(GradientBrush brush)
    {
        brush.PropertyChanged += OnGradientBrushPropertyChanged;
        brush.InvalidateGradientBrushRequested += OnGradientBrushInvalidated;
    }

    private void DetachBrush(GradientBrush brush)
    {
        brush.PropertyChanged -= OnGradientBrushPropertyChanged;
        brush.InvalidateGradientBrushRequested -= OnGradientBrushInvalidated;
    }

    private void OnGradientBrushPropertyChanged(
        object? sender,
        PropertyChangedEventArgs args)
    {
        QueueBrushInvalidation();
    }

    private void OnGradientBrushInvalidated(object? sender, EventArgs args)
    {
        QueueBrushInvalidation();
    }

    private void QueueBrushInvalidation()
    {
        if (_isDisconnected || _isDisposed)
        {
            return;
        }

        if (System.Threading.Interlocked.Exchange(
                ref _brushInvalidationPending,
                1) != 0)
        {
            return;
        }

        BeginInvokeOnMainThread(
            () =>
            {
                System.Threading.Interlocked.Exchange(
                    ref _brushInvalidationPending,
                    0);

                if (!_isDisconnected && !_isDisposed)
                {
                    QueueMaterialRender();
                }
            });
    }

    private void QueueMaterialRender()
    {
        if (_isDisconnected || _isDisposed)
        {
            return;
        }

        _materialDirty = true;
        CancelResizeRender();
        SetNeedsLayout();
    }

    private void RenderCurrentBounds()
    {
        if (_isDisconnected || _isDisposed || _material is null)
        {
            return;
        }

        var size = Bounds.Size;
        var scale = GetRenderScale();

        if (size.Width <= 0 || size.Height <= 0 || scale <= 0)
        {
            ReplaceDisplayedImage(null);
            _hasRendered = false;
            _renderedSize = CGSize.Empty;
            _renderedScale = 0;
            return;
        }

        _materialDirty = false;
        _hasRendered = true;
        _renderedSize = size;
        _renderedScale = scale;
        RenderMaterial(size, scale);
    }

    private void ScheduleResizeRender(CGSize size, double scale)
    {
        if (_resizeTimer is not null
            && SizeEquals(size, _pendingRenderSize)
            && ScaleEquals(scale, _pendingRenderScale))
        {
            return;
        }

        CancelResizeRender();
        _pendingRenderSize = size;
        _pendingRenderScale = scale;
        _resizeTimer = NSTimer.CreateScheduledTimer(
            ResizeIdleDelay,
            OnResizeTimerElapsed);
    }

    private void OnResizeTimerElapsed(NSTimer timer)
    {
        if (!ReferenceEquals(_resizeTimer, timer))
        {
            return;
        }

        _resizeTimer = null;
        _pendingRenderSize = CGSize.Empty;
        _pendingRenderScale = 0;
        timer.Invalidate();
        timer.Dispose();

        if (!_isDisconnected && !_isDisposed)
        {
            RenderCurrentBounds();
        }
    }

    private void CancelResizeRender()
    {
        var timer = _resizeTimer;
        _resizeTimer = null;
        _pendingRenderSize = CGSize.Empty;
        _pendingRenderScale = 0;

        if (timer is not null)
        {
            timer.Invalidate();
            timer.Dispose();
        }
    }

    private double GetRenderScale()
    {
        var scale = (double)(Window?.Screen.Scale ?? UIScreen.MainScreen.Scale);
        return double.IsFinite(scale) && scale > 0
            ? scale
            : 1;
    }

    private void LatchPremiumFailure(Exception exception)
    {
        _premiumUnavailable = true;
        _failedInputBrushOne = _inputBrushOne;
        _failedInputBrushTwo = _inputBrushTwo;
        ClearCachedDotMask();
        System.Diagnostics.Debug.WriteLine(
            $"PrimoMaterial CrossFade_RadoDots disabled its iOS premium path until an input brush is replaced: {exception.Message}");
    }

    private void ReplaceDisplayedImage(UIImage? next)
    {
        if (ReferenceEquals(_displayedImage, next))
        {
            return;
        }

        var previous = _displayedImage;
        _displayedImage = next;
        _imageView.Image = next;
        previous?.Dispose();
    }

    private static bool SizeEquals(CGSize left, CGSize right) =>
        Math.Abs((double)(left.Width - right.Width)) < 0.01
        && Math.Abs((double)(left.Height - right.Height)) < 0.01;

    private static bool ScaleEquals(double left, double right) =>
        Math.Abs(left - right) < 0.001;

    private static uint HashCell(int column, int row)
    {
        var value = DotSeed;
        value ^= unchecked((uint)column * 0x9E3779B9u);
        value ^= unchecked((uint)row * 0x85EBCA6Bu);
        return Hash(value);
    }

    private static uint Hash(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }

    private static float ToUnit(uint value) =>
        (value & 0x00FFFFFFu) / 16777215f;
}
#endif
