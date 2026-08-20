using CodeCrafty.DapperDan.Controls;
using CodeCrafty.DapperDan.Semantics;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

public partial class PanelBossBody_DefaultView
{
    public static readonly BindableProperty TouchPointBloomTintProperty =
        BindableProperty.Create(
            nameof(TouchPointBloomTint),
            typeof(Color),
            typeof(PanelBossBody_DefaultView),
            defaultValueCreator: _ => SurfPalette.TouchFeedbackSignal,
            propertyChanged: OnTouchPointBloomTintChanged);

    private AbsoluteLayout _touchPointBloomCanvas;
    private TouchPointBloom _sharedTouchPointBloom;

    public Color TouchPointBloomTint
    {
        get => (Color)GetValue(TouchPointBloomTintProperty);
        set => SetValue(TouchPointBloomTintProperty, value);
    }

    private void EnsureTouchPointBloom()
    {
        if (_sharedTouchPointBloom is not null)
            return;

        EnsureTouchPointBloomOverlayArea();

        _touchPointBloomCanvas = new AbsoluteLayout
        {
            AutomationId = "PanelBoss_TouchPointBloomCanvas",
            HorizontalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            IsClippedToBounds = false,
            VerticalOptions = LayoutOptions.Fill
        };
        _sharedTouchPointBloom = new TouchPointBloom
        {
            BloomTint = TouchPointBloomTint
        };
        _touchPointBloomCanvas.Children.Add(_sharedTouchPointBloom);
        _touchPointBloomOverlayArea.Children.Add(_touchPointBloomCanvas);

        Console.WriteLine("TOUCH_POINT_BLOOM|created|shared-one-shot");
    }

    private void ShowTouchPointBloom(
        TapViewBase button,
        RichButtonTapStartingEventArgs e)
    {
        EnsureTouchPointBloom();

        var position = e.GetPosition(_touchPointBloomCanvas);

        if (position is null)
        {
            Console.WriteLine(
                $"TOUCH_POINT_BLOOM|position-unavailable|id={button.AutomationId}");
            return;
        }

        Console.WriteLine(
            $"TOUCH_POINT_BLOOM|show|id={button.AutomationId}|x={position.Value.X:0.##}|y={position.Value.Y:0.##}|canvasWidth={_touchPointBloomCanvas.Width:0.##}|canvasHeight={_touchPointBloomCanvas.Height:0.##}");
        _sharedTouchPointBloom.ShowAt(position.Value);
    }

    private void RemoveTouchPointBloom()
    {
        _sharedTouchPointBloom = null;
        _touchPointBloomCanvas = null;
    }

    private static void OnTouchPointBloomTintChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is PanelBossBody_DefaultView host &&
            newValue is Color tint &&
            host._sharedTouchPointBloom is not null)
        {
            host._sharedTouchPointBloom.BloomTint = tint;
        }
    }
}
