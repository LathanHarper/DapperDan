namespace Flexler.Controls;

public partial class MineralAtmosphereView : Grid
{
    private IDispatcherTimer? _frameTimer;
    private Size _pendingSize;

    public MineralAtmosphereView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        SizeChanged -= OnAtmosphereSizeChanged;
        SizeChanged += OnAtmosphereSizeChanged;

        _frameTimer ??= CreateFrameTimer();
        QueueParallaxFrame();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        SizeChanged -= OnAtmosphereSizeChanged;
        _frameTimer?.Stop();
    }

    private void OnAtmosphereSizeChanged(object? sender, EventArgs e) =>
        QueueParallaxFrame();

    private IDispatcherTimer CreateFrameTimer()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(16);
        timer.IsRepeating = false;
        timer.Tick += OnFrameTimerTick;
        return timer;
    }

    private void QueueParallaxFrame()
    {
        _pendingSize = new Size(Width, Height);

        if (_frameTimer is not null && !_frameTimer.IsRunning)
        {
            _frameTimer.Start();
        }
    }

    private void OnFrameTimerTick(object? sender, EventArgs e)
    {
        if (_pendingSize.Width <= 0 || _pendingSize.Height <= 0)
        {
            return;
        }

        var horizontalFactor = Math.Clamp((_pendingSize.Width - 720d) / 720d, -1d, 1d);
        var verticalFactor = Math.Clamp((_pendingSize.Height - 620d) / 620d, -1d, 1d);

        RawStoneLayer.TranslationX = horizontalFactor * 6d;
        RawStoneLayer.TranslationY = verticalFactor * 4d;

        PolishedStoneLayer.TranslationX = horizontalFactor * -11d;
        PolishedStoneLayer.TranslationY = verticalFactor * -7d;

        SpectralPlane.TranslationX = horizontalFactor * 16d;
        SpectralPlane.TranslationY = verticalFactor * 9d;
    }
}
