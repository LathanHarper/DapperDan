namespace CodeCrafty.DapperDan.Controls;

public partial class TouchPointBloom : ContentView
{
    public static readonly BindableProperty BloomTintProperty =
        BindableProperty.Create(
            nameof(BloomTint),
            typeof(Color),
            typeof(TouchPointBloom),
            Colors.White,
            propertyChanged: OnBloomTintChanged);

    private const string AnimationName = "TouchPointBloomAnimation";
    private const double BloomHeight = 72d;
    private const double BloomWidth = 96d;
    private const float BloomTintStrongAlpha = 112f / 255f;
    private const float BloomTintSoftAlpha = 36f / 255f;
    private const float BloomTintClearAlpha = 0f;
    private int _animationGeneration;

    public TouchPointBloom()
    {
        InitializeComponent();
        ApplyBloomTint(BloomTint);
        Unloaded += OnUnloaded;
    }

    public Color BloomTint
    {
        get => (Color)GetValue(BloomTintProperty);
        set => SetValue(BloomTintProperty, value);
    }

    public void ShowAt(Point position)
    {
        if (Parent is not AbsoluteLayout)
            return;

        AbsoluteLayout.SetLayoutBounds(
            this,
            new Rect(
                position.X - BloomWidth / 2d,
                position.Y - BloomHeight / 2d,
                BloomWidth,
                BloomHeight));

        var animationGeneration = ++_animationGeneration;
        this.AbortAnimation(AnimationName);

        IsVisible = true;
        Opacity = 0.72;
        Scale = 0.18;

        var animation = new Animation();
        animation.Add(
            0,
            1,
            new Animation(
                value => Scale = value,
                0.18,
                1.8,
                Easing.CubicOut));
        animation.Add(
            0.08,
            1,
            new Animation(
                value => Opacity = value,
                0.72,
                0,
                Easing.CubicOut));
        animation.Commit(
            this,
            AnimationName,
            16,
            320,
            Easing.Linear,
            (_, _) =>
            {
                if (animationGeneration != _animationGeneration)
                    return;

                Opacity = 0;
                IsVisible = false;
            });
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        _animationGeneration++;
        this.AbortAnimation(AnimationName);
        Opacity = 0;
        IsVisible = false;
    }

    private static void OnBloomTintChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is TouchPointBloom bloom && newValue is Color tint)
            bloom.ApplyBloomTint(tint);
    }

    private void ApplyBloomTint(Color tint)
    {
        if (BloomTintStrongStop is null ||
            BloomTintSoftStop is null ||
            BloomTintClearStop is null)
        {
            return;
        }

        BloomTintStrongStop.Color = WithAlpha(tint, BloomTintStrongAlpha);
        BloomTintSoftStop.Color = WithAlpha(tint, BloomTintSoftAlpha);
        BloomTintClearStop.Color = WithAlpha(tint, BloomTintClearAlpha);
    }

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.Red, color.Green, color.Blue, alpha);
}
