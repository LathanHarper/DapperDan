#if WINDOWS
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiLinearGradientBrush = Microsoft.Maui.Controls.LinearGradientBrush;
using MauiRadialGradientBrush = Microsoft.Maui.Controls.RadialGradientBrush;
using WinColor = Windows.UI.Color;
using WinGrid = Microsoft.UI.Xaml.Controls.Grid;
using WinGradientStop = Microsoft.UI.Xaml.Media.GradientStop;
using WinLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WinRadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;
using XamlHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using XamlVisibility = Microsoft.UI.Xaml.Visibility;
using XamlVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

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

    protected override CrossFadeRadoDotsPlatformView CreatePlatformView() => new();

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

internal sealed class CrossFadeRadoDotsPlatformView : WinGrid
{
    private const float DotCellSize = 38f;
    private const uint DotSeed = 0x5241444Fu;

    private readonly WinGrid _fallback;
    private readonly CanvasControl _canvas;

    private CrossFade_RadoDots? _material;
    private GradientBrush? _inputBrushOne;
    private GradientBrush? _inputBrushTwo;
    private CanvasCommandList? _inputOne;
    private CanvasCommandList? _inputTwo;
    private BlendEffect? _multiply;
    private BlendEffect? _screen;
    private CanvasGeometry? _dotMask;
    private Windows.Foundation.Size _resourceSize;
    private bool _isDirty = true;
    private bool _isDisconnected;

    public CrossFadeRadoDotsPlatformView()
    {
        _fallback = new WinGrid
        {
            HorizontalAlignment = XamlHorizontalAlignment.Stretch,
            IsHitTestVisible = false,
            VerticalAlignment = XamlVerticalAlignment.Stretch
        };
        _canvas = new CanvasControl
        {
            ClearColor = WinColor.FromArgb(0, 0, 0, 0),
            HorizontalAlignment = XamlHorizontalAlignment.Stretch,
            IsHitTestVisible = false,
            VerticalAlignment = XamlVerticalAlignment.Stretch
        };

        Children.Add(_fallback);
        Children.Add(_canvas);
        _canvas.CreateResources += OnCreateResources;
        _canvas.Draw += OnDraw;
        SizeChanged += OnSizeChanged;
    }

    public void Connect(CrossFade_RadoDots material)
    {
        _material = material;
        _isDisconnected = false;
        UpdateBrushSubscriptions(material.InputBrushOne, material.InputBrushTwo);
        UpdateFallback(material.InputBrushOne);
        MarkDirty();
    }

    public void UpdateMaterial(CrossFade_RadoDots material)
    {
        _material = material;
        UpdateBrushSubscriptions(material.InputBrushOne, material.InputBrushTwo);
        UpdateFallback(material.InputBrushOne);
        MarkDirty();
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
        _canvas.CreateResources -= OnCreateResources;
        _canvas.Draw -= OnDraw;
        SizeChanged -= OnSizeChanged;

        DisposeDeviceResources();
        _canvas.RemoveFromVisualTree();
        Children.Remove(_canvas);
        Children.Remove(_fallback);
    }

    private void OnCreateResources(
        CanvasControl sender,
        CanvasCreateResourcesEventArgs args)
    {
        DisposeDeviceResources();
        _isDirty = true;
        sender.Opacity = 0;
        ShowFallback();
        sender.Invalidate();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        MarkDirty();
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_material is null
            || sender.ActualWidth <= 0
            || sender.ActualHeight <= 0)
        {
            return;
        }

        var size = new Windows.Foundation.Size(sender.ActualWidth, sender.ActualHeight);
        if (_isDirty || !SizeEquals(_resourceSize, size))
        {
            if (!TryBuildDeviceResources(sender.Device, size))
            {
                _canvas.Opacity = 0;
                ShowFallback();
                return;
            }
        }

        if (_multiply is null || _screen is null || _dotMask is null)
        {
            _canvas.Opacity = 0;
            ShowFallback();
            return;
        }

        _fallback.Visibility = XamlVisibility.Collapsed;
        _canvas.Opacity = 1;
        var drawingSession = args.DrawingSession;
        drawingSession.DrawImage(_multiply);
        var previousBlend = drawingSession.Blend;
        try
        {
            drawingSession.Blend = CanvasBlend.Copy;
            using (drawingSession.CreateLayer(1f, _dotMask))
            {
                drawingSession.DrawImage(_screen);
            }
        }
        finally
        {
            drawingSession.Blend = previousBlend;
        }
    }

    private bool TryBuildDeviceResources(
        CanvasDevice device,
        Windows.Foundation.Size size)
    {
        DisposeDeviceResources();

        try
        {
            _inputOne = CreateGradientImage(
                device,
                _material!.InputBrushOne,
                (float)size.Width,
                (float)size.Height);
            _inputTwo = CreateGradientImage(
                device,
                _material.InputBrushTwo,
                (float)size.Width,
                (float)size.Height);

            if (_inputOne is null || _inputTwo is null)
            {
                DisposeDeviceResources();
                return false;
            }

            _multiply = new BlendEffect
            {
                Background = _inputOne,
                Foreground = _inputTwo,
                Mode = BlendEffectMode.Multiply
            };
            _screen = new BlendEffect
            {
                Background = _inputOne,
                Foreground = _inputTwo,
                Mode = BlendEffectMode.Screen
            };
            _dotMask = CreateDotMask(
                device,
                (float)size.Width,
                (float)size.Height);

            _resourceSize = size;
            _isDirty = false;
            return true;
        }
        catch (Exception exception) when (!device.IsDeviceLost(exception.HResult))
        {
            System.Diagnostics.Debug.WriteLine(
                $"PrimoMaterial CrossFade_RadoDots fell back to InputBrushOne: {exception.Message}");
            DisposeDeviceResources();
            _resourceSize = size;
            _isDirty = false;
            return false;
        }
    }

    private static CanvasCommandList? CreateGradientImage(
        CanvasDevice device,
        GradientBrush brush,
        float width,
        float height)
    {
        using var nativeBrush = CreateNativeBrush(device, brush, width, height);
        if (nativeBrush is null)
        {
            return null;
        }

        var image = new CanvasCommandList(device);
        try
        {
            using var drawingSession = image.CreateDrawingSession();
            drawingSession.Clear(WinColor.FromArgb(0, 0, 0, 0));
            drawingSession.FillRectangle(0, 0, width, height, nativeBrush);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    private static ICanvasBrush? CreateNativeBrush(
        CanvasDevice device,
        GradientBrush brush,
        float width,
        float height)
    {
        var stops = CreateGradientStops(brush);
        if (stops.Length < 2)
        {
            return null;
        }

        switch (brush)
        {
            case MauiLinearGradientBrush linear:
                return new CanvasLinearGradientBrush(device, stops)
                {
                    StartPoint = new Vector2(
                        (float)linear.StartPoint.X * width,
                        (float)linear.StartPoint.Y * height),
                    EndPoint = new Vector2(
                        (float)linear.EndPoint.X * width,
                        (float)linear.EndPoint.Y * height)
                };

            case MauiRadialGradientBrush radial:
                return new CanvasRadialGradientBrush(device, stops)
                {
                    Center = new Vector2(
                        (float)radial.Center.X * width,
                        (float)radial.Center.Y * height),
                    OriginOffset = Vector2.Zero,
                    RadiusX = Math.Max(1f, (float)radial.Radius * width),
                    RadiusY = Math.Max(1f, (float)radial.Radius * height)
                };

            default:
                return null;
        }
    }

    private static CanvasGradientStop[] CreateGradientStops(GradientBrush brush)
    {
        var ordered = brush.GradientStops
            .Select(
                static (stop, index) => new
                {
                    Stop = stop,
                    Index = index,
                    Position = float.IsFinite(stop.Offset)
                        ? Math.Clamp(stop.Offset, 0f, 1f)
                        : 0f
                })
            .OrderBy(static entry => entry.Position)
            .ThenBy(static entry => entry.Index)
            .Select(
                static entry => new CanvasGradientStop
                {
                    Color = ToWindowsColor(entry.Stop.Color),
                    Position = entry.Position
                })
            .ToArray();

        if (ordered.Length != 1)
        {
            return ordered;
        }

        return
        [
            new CanvasGradientStop
            {
                Color = ordered[0].Color,
                Position = 0
            },
            new CanvasGradientStop
            {
                Color = ordered[0].Color,
                Position = 1
            }
        ];
    }

    private static CanvasGeometry CreateDotMask(
        CanvasDevice device,
        float width,
        float height)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(width / DotCellSize));
        var rows = Math.Max(1, (int)Math.Ceiling(height / DotCellSize));
        var shapes = new List<CanvasGeometry>(columns * rows);

        try
        {
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var hash = HashCell(column, row);
                    if (ToUnit(hash) > 0.72f)
                    {
                        continue;
                    }

                    var jitterX = (ToUnit(Hash(hash ^ 0x68BC21EBu)) - 0.5f)
                        * DotCellSize
                        * 0.42f;
                    var jitterY = (ToUnit(Hash(hash ^ 0x02E5BE93u)) - 0.5f)
                        * DotCellSize
                        * 0.42f;
                    var radius = 4.5f
                        + (ToUnit(Hash(hash ^ 0x967A889Bu)) * 8.5f);
                    var centerX = ((column + 0.5f) * DotCellSize) + jitterX;
                    var centerY = ((row + 0.5f) * DotCellSize) + jitterY;

                    shapes.Add(
                        CanvasGeometry.CreateCircle(
                            device,
                            centerX,
                            centerY,
                            radius));
                }
            }

            return CanvasGeometry.CreateGroup(
                device,
                shapes.ToArray(),
                CanvasFilledRegionDetermination.Winding);
        }
        finally
        {
            foreach (var shape in shapes)
            {
                shape.Dispose();
            }
        }
    }

    private void MarkDirty()
    {
        if (_isDisconnected)
        {
            return;
        }

        _isDirty = true;
        _canvas.Opacity = 0;
        ShowFallback();
        _canvas.Invalidate();
    }

    private void ShowFallback()
    {
        _fallback.Visibility = XamlVisibility.Visible;
    }

    private void UpdateFallback(GradientBrush brush)
    {
        _fallback.Background = brush switch
        {
            MauiLinearGradientBrush linear => CreateFallbackLinearBrush(linear),
            MauiRadialGradientBrush radial => CreateFallbackRadialBrush(radial),
            _ => null
        };
        _fallback.Visibility = XamlVisibility.Visible;
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

        if (DispatcherQueue.HasThreadAccess)
        {
            RefreshInvalidatedBrushes();
            return;
        }

        DispatcherQueue.TryEnqueue(RefreshInvalidatedBrushes);
    }

    private void OnGradientBrushPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs args) =>
        OnGradientBrushInvalidated(sender, args);

    private void RefreshInvalidatedBrushes()
    {
        if (_isDisconnected || _inputBrushOne is null)
        {
            return;
        }

        UpdateFallback(_inputBrushOne);
        MarkDirty();
    }

    private static WinLinearGradientBrush CreateFallbackLinearBrush(
        MauiLinearGradientBrush source)
    {
        var result = new WinLinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(
                source.StartPoint.X,
                source.StartPoint.Y),
            EndPoint = new Windows.Foundation.Point(
                source.EndPoint.X,
                source.EndPoint.Y)
        };
        CopyFallbackStops(source, result.GradientStops);
        return result;
    }

    private static WinRadialGradientBrush CreateFallbackRadialBrush(
        MauiRadialGradientBrush source)
    {
        var result = new WinRadialGradientBrush
        {
            Center = new Windows.Foundation.Point(
                source.Center.X,
                source.Center.Y),
            GradientOrigin = new Windows.Foundation.Point(
                source.Center.X,
                source.Center.Y),
            RadiusX = source.Radius,
            RadiusY = source.Radius
        };
        CopyFallbackStops(source, result.GradientStops);
        return result;
    }

    private static void CopyFallbackStops(
        GradientBrush source,
        IList<WinGradientStop> destination)
    {
        foreach (var entry in source.GradientStops
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
                     .ThenBy(static entry => entry.Index))
        {
            destination.Add(
                new WinGradientStop
                {
                    Color = ToWindowsColor(entry.Stop.Color),
                    Offset = entry.Offset
                });
        }
    }

    private void DisposeDeviceResources()
    {
        _screen?.Dispose();
        _screen = null;
        _multiply?.Dispose();
        _multiply = null;
        _dotMask?.Dispose();
        _dotMask = null;
        _inputTwo?.Dispose();
        _inputTwo = null;
        _inputOne?.Dispose();
        _inputOne = null;
    }

    private static bool SizeEquals(
        Windows.Foundation.Size left,
        Windows.Foundation.Size right) =>
        Math.Abs(left.Width - right.Width) < 0.5
        && Math.Abs(left.Height - right.Height) < 0.5;

    private static uint HashCell(int column, int row)
    {
        var value = DotSeed;
        value ^= unchecked((uint)column * 0x9E3779B9u);
        value ^= unchecked((uint)row * 0x85EBCA6Bu);
        return Hash(value);
    }

    private static uint Hash(uint value)
    {
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return value;
    }

    private static float ToUnit(uint value) =>
        (value & 0x00FFFFFFu) / 16777215f;

    private static WinColor ToWindowsColor(MauiColor color) =>
        WinColor.FromArgb(
            ToByte(color.Alpha),
            ToByte(color.Red),
            ToByte(color.Green),
            ToByte(color.Blue));

    private static byte ToByte(float channel) =>
        (byte)Math.Round(Math.Clamp(channel, 0f, 1f) * byte.MaxValue);
}
#endif
