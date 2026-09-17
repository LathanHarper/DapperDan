using Microsoft.Maui.Controls.Shapes;

namespace PrimoMaterial;

/// <summary>
/// A contentless material layer that composites two gradient brushes through
/// PrimoMaterial's fixed random-dot facet recipe.
/// </summary>
public sealed class CrossFade_RadoDots : View
{
    internal const string MaterialUpdateKey = "PrimoMaterial.Material";

    public static readonly BindableProperty InputBrushOneProperty = BindableProperty.Create(
        nameof(InputBrushOne),
        typeof(GradientBrush),
        typeof(CrossFade_RadoDots),
        validateValue: static (_, value) => value is GradientBrush,
        propertyChanged: OnInputBrushChanged,
        defaultValueCreator: static _ => CreateDefaultBaseBrush());

    public static readonly BindableProperty InputBrushTwoProperty = BindableProperty.Create(
        nameof(InputBrushTwo),
        typeof(GradientBrush),
        typeof(CrossFade_RadoDots),
        validateValue: static (_, value) => value is GradientBrush,
        propertyChanged: OnInputBrushChanged,
        defaultValueCreator: static _ => CreateDefaultFlashBrush());

    public CrossFade_RadoDots()
    {
        InputTransparent = true;
    }

    public GradientBrush InputBrushOne
    {
        get => (GradientBrush)GetValue(InputBrushOneProperty);
        set => SetValue(InputBrushOneProperty, value);
    }

    public GradientBrush InputBrushTwo
    {
        get => (GradientBrush)GetValue(InputBrushTwoProperty);
        set => SetValue(InputBrushTwoProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // A rounded clip on a layer means "crop to my arranged surface."
        // Keep its local bounds current while the desktop workbench resizes.
        if (width > 0
            && height > 0
            && Clip is RoundRectangleGeometry roundRectangle)
        {
            roundRectangle.Rect = new Rect(0, 0, width, height);
        }
    }

    private static void OnInputBrushChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var material = (CrossFade_RadoDots)bindable;
        material.Handler?.UpdateValue(MaterialUpdateKey);
    }

    private static LinearGradientBrush CreateDefaultBaseBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFE5ECEA"), 0.00f));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFC3D3D5"), 0.52f));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFD7DDF0"), 1.00f));
        return brush;
    }

    private static LinearGradientBrush CreateDefaultFlashBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 1),
            EndPoint = new Point(1, 0)
        };

        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FF63E6D0"), 0.00f));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FF55A7FF"), 0.54f));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFA482FF"), 1.00f));
        return brush;
    }
}
