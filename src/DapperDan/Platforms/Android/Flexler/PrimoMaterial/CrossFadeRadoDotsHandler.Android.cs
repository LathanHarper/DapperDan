#if ANDROID
using System.Runtime.Versioning;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Handlers;
using ACanvas = Android.Graphics.Canvas;
using AColor = Android.Graphics.Color;
using ALinearGradient = Android.Graphics.LinearGradient;
using APaint = Android.Graphics.Paint;
using ARadialGradient = Android.Graphics.RadialGradient;
using AShader = Android.Graphics.Shader;
using MauiLinearGradientBrush = Microsoft.Maui.Controls.LinearGradientBrush;
using MauiRadialGradientBrush = Microsoft.Maui.Controls.RadialGradientBrush;

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

    protected override CrossFadeRadoDotsPlatformView CreatePlatformView() =>
        new(Context);

    protected override void ConnectHandler(CrossFadeRadoDotsPlatformView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Connect(VirtualView);
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

internal sealed class CrossFadeRadoDotsPlatformView : global::Android.Views.View
{
    private const string AgslSource = """
        uniform shader inputOne;
        uniform shader inputTwo;
        uniform float uDensity;

        float hash(float2 point) {
            point = fract(point * float2(0.1031, 0.1030));
            point += dot(point, point.yx + 33.33);
            return fract((point.x + point.y) * point.x);
        }

        half4 main(float2 fragCoord) {
            float2 logical = fragCoord / max(uDensity, 0.001);
            float2 cell = floor(logical / 38.0);
            float2 local = logical - (cell * 38.0);
            float2 seed = float2(21057.0, 17487.0);
            float present = 1.0 - step(0.72, hash(cell + seed));
            float2 random = float2(
                hash(cell + seed + float2(17.0, 59.0)),
                hash(cell + seed + float2(83.0, 31.0)));
            float2 center =
                (0.5 + ((random * 2.0 - 1.0) * 0.21)) * 38.0;
            float radius = mix(
                4.5,
                13.0,
                hash(cell + seed + float2(47.0, 113.0)));
            half dotMask = half(
                present
                * (1.0 - smoothstep(
                    radius - 1.0,
                    radius + 1.0,
                    length(local - center))));
            half4 one = inputOne.eval(fragCoord);
            half4 two = inputTwo.eval(fragCoord);
            half3 colorOne = unpremul(one).rgb;
            half3 colorTwo = unpremul(two).rgb;
            half3 multiplyResult = colorOne * colorTwo;
            half3 screenResult =
                1.0 - ((1.0 - colorOne) * (1.0 - colorTwo));
            half3 blended = mix(multiplyResult, screenResult, dotMask);
            half alpha = one.a + two.a - (one.a * two.a);
            half3 rgb =
                (one.rgb * (1.0 - two.a))
                + (two.rgb * (1.0 - one.a))
                + (blended * (one.a * two.a));
            return half4(rgb, alpha);
        }
        """;

    private readonly APaint _paint;

    private CrossFade_RadoDots? _material;
    private GradientBrush? _inputBrushOne;
    private GradientBrush? _inputBrushTwo;
    private AShader? _inputOne;
    private AShader? _inputTwo;
    private AShader? _premium;
    private bool _premiumUnavailable;
    private bool _isDisconnected;

    public CrossFadeRadoDotsPlatformView(Context context)
        : base(context)
    {
        _paint = new APaint(PaintFlags.AntiAlias | PaintFlags.Dither);
        ImportantForAccessibility = ImportantForAccessibility.No;
        SetWillNotDraw(false);
    }

    public void Connect(CrossFade_RadoDots material)
    {
        _material = material;
        _premiumUnavailable = false;
        _isDisconnected = false;
        UpdateBrushSubscriptions(material.InputBrushOne, material.InputBrushTwo);
        RebuildShaders();
    }

    public void UpdateMaterial(CrossFade_RadoDots material)
    {
        _material = material;
        _premiumUnavailable = false;
        UpdateBrushSubscriptions(material.InputBrushOne, material.InputBrushTwo);
        RebuildShaders();
    }

    public void Disconnect()
    {
        if (_isDisconnected)
        {
            return;
        }

        _isDisconnected = true;
        _material = null;
        RemoveBrushSubscriptions();
        DisposeShaders();
    }

    protected override void OnSizeChanged(int width, int height, int oldWidth, int oldHeight)
    {
        base.OnSizeChanged(width, height, oldWidth, oldHeight);
        RebuildShaders();
    }

    protected override void OnDraw(ACanvas canvas)
    {
        base.OnDraw(canvas);

        if (_inputOne is null || Width <= 0 || Height <= 0)
        {
            return;
        }

        var shader = canvas.IsHardwareAccelerated && _premium is not null
            ? _premium
            : _inputOne;

        try
        {
            DrawShader(canvas, shader);
        }
        catch (Java.Lang.RuntimeException exception) when (ReferenceEquals(shader, _premium))
        {
            System.Diagnostics.Debug.WriteLine(
                $"PrimoMaterial CrossFade_RadoDots disabled its Android premium path: {exception.Message}");
            _premiumUnavailable = true;
            _premium?.Dispose();
            _premium = null;
            DrawShader(canvas, _inputOne);
        }
    }

    private void RebuildShaders()
    {
        if (_isDisconnected
            || _material is null
            || Width <= 0
            || Height <= 0)
        {
            return;
        }

        DisposeShaders();
        try
        {
            _inputOne = CreateNativeShader(_material.InputBrushOne, Width, Height);
            _inputTwo = CreateNativeShader(_material.InputBrushTwo, Width, Height);

            if (OperatingSystem.IsAndroidVersionAtLeast(33)
                && !_premiumUnavailable
                && _inputOne is not null
                && _inputTwo is not null)
            {
                _premium = CreatePremiumShader(
                    _inputOne,
                    _inputTwo,
                    Resources?.DisplayMetrics?.Density ?? 1f);
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"PrimoMaterial CrossFade_RadoDots is using its Android gradient fallback: {exception.Message}");
            _premiumUnavailable = true;
            _premium?.Dispose();
            _premium = null;
        }

        Invalidate();
    }

    [SupportedOSPlatform("android33.0")]
    private static AShader CreatePremiumShader(
        AShader inputOne,
        AShader inputTwo,
        float density)
    {
        var shader = new RuntimeShader(AgslSource);
        try
        {
            shader.SetInputShader("inputOne", inputOne);
            shader.SetInputShader("inputTwo", inputTwo);
            shader.SetFloatUniform("uDensity", Math.Max(density, 1f));
            return shader;
        }
        catch
        {
            shader.Dispose();
            throw;
        }
    }

    private static AShader CreateNativeShader(
        GradientBrush brush,
        int width,
        int height)
    {
        var (colors, offsets) = CreateStops(brush);

        return brush switch
        {
            MauiLinearGradientBrush linear => new ALinearGradient(
                (float)linear.StartPoint.X * width,
                (float)linear.StartPoint.Y * height,
                (float)linear.EndPoint.X * width,
                (float)linear.EndPoint.Y * height,
                colors,
                offsets,
                AShader.TileMode.Clamp!),

            MauiRadialGradientBrush radial => new ARadialGradient(
                (float)radial.Center.X * width,
                (float)radial.Center.Y * height,
                Math.Max(width, height) * Math.Max((float)radial.Radius, 0.001f),
                colors,
                offsets,
                AShader.TileMode.Clamp!),

            _ => new ALinearGradient(
                0,
                0,
                width,
                height,
                colors,
                offsets,
                AShader.TileMode.Clamp!)
        };
    }

    private static (int[] Colors, float[] Offsets) CreateStops(GradientBrush brush)
    {
        var stops = brush.GradientStops
            .Select(
                static (stop, index) => new
                {
                    Stop = stop,
                    Index = index,
                    Offset = float.IsFinite(stop.Offset)
                        ? Math.Clamp(stop.Offset, 0f, 1f)
                        : 0f
                })
            .OrderBy(static entry => entry.Offset)
            .ThenBy(static entry => entry.Index)
            .ToArray();

        if (stops.Length == 0)
        {
            return
            (
                [AColor.Transparent.ToArgb(), AColor.Transparent.ToArgb()],
                [0f, 1f]
            );
        }

        if (stops.Length == 1)
        {
            var color = ToAndroidColor(stops[0].Stop.Color);
            return ([color, color], [0f, 1f]);
        }

        return
        (
            stops.Select(static entry => ToAndroidColor(entry.Stop.Color)).ToArray(),
            stops.Select(static entry => entry.Offset).ToArray()
        );
    }

    private static int ToAndroidColor(Microsoft.Maui.Graphics.Color color) =>
        AColor.Argb(
                ToByte(color.Alpha),
                ToByte(color.Red),
                ToByte(color.Green),
                ToByte(color.Blue))
            .ToArgb();

    private static byte ToByte(float channel) =>
        (byte)Math.Round(Math.Clamp(channel, 0f, 1f) * byte.MaxValue);

    private void DrawShader(ACanvas canvas, AShader shader)
    {
        _paint.SetShader(shader);
        try
        {
            canvas.DrawRect(0, 0, Width, Height, _paint);
        }
        finally
        {
            _paint.SetShader(null);
        }
    }

    private void DisposeShaders()
    {
        _paint.SetShader(null);
        _premium?.Dispose();
        _premium = null;
        _inputTwo?.Dispose();
        _inputTwo = null;
        _inputOne?.Dispose();
        _inputOne = null;
    }

    private void UpdateBrushSubscriptions(
        GradientBrush inputBrushOne,
        GradientBrush inputBrushTwo)
    {
        RemoveBrushSubscriptions();

        _inputBrushOne = inputBrushOne;
        _inputBrushTwo = inputBrushTwo;
        _inputBrushOne.InvalidateGradientBrushRequested += OnGradientBrushInvalidated;
        _inputBrushOne.PropertyChanged += OnGradientBrushPropertyChanged;

        if (!ReferenceEquals(_inputBrushTwo, _inputBrushOne))
        {
            _inputBrushTwo.InvalidateGradientBrushRequested += OnGradientBrushInvalidated;
            _inputBrushTwo.PropertyChanged += OnGradientBrushPropertyChanged;
        }
    }

    private void RemoveBrushSubscriptions()
    {
        if (_inputBrushOne is not null)
        {
            _inputBrushOne.InvalidateGradientBrushRequested -= OnGradientBrushInvalidated;
            _inputBrushOne.PropertyChanged -= OnGradientBrushPropertyChanged;
        }

        if (_inputBrushTwo is not null
            && !ReferenceEquals(_inputBrushTwo, _inputBrushOne))
        {
            _inputBrushTwo.InvalidateGradientBrushRequested -= OnGradientBrushInvalidated;
            _inputBrushTwo.PropertyChanged -= OnGradientBrushPropertyChanged;
        }

        _inputBrushOne = null;
        _inputBrushTwo = null;
    }

    private void OnGradientBrushInvalidated(object? sender, EventArgs args)
    {
        if (_isDisconnected)
        {
            return;
        }

        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(RebuildShaders);
    }

    private void OnGradientBrushPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs args) =>
        OnGradientBrushInvalidated(sender, args);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RemoveBrushSubscriptions();
            DisposeShaders();
            _paint.Dispose();
        }

        base.Dispose(disposing);
    }
}
#endif
