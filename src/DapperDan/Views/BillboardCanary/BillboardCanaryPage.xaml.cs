using CodeCrafty.DapperDan.ViewModels;

namespace CodeCrafty.DapperDan.Views.BillboardCanary;

public partial class BillboardCanaryPage : ContentPage
{
    private const double DesignWidth = 960;
    private const double DesignHeight = 540;
    private const double FaceX = 528;
    private const double FaceY = 231;
    private const double FaceWidth = 318;
    private const double FaceHeight = 96;
    private const double FaceRotation = -0.4;
    private const double FaceRotationX = 1.2;
    private const double FaceRotationY = -3.5;

    private CancellationTokenSource? _loopCancellation;

    public BillboardCanaryPage(BillboardCanaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartFaceLoop();
    }

    protected override void OnDisappearing()
    {
        StopFaceLoop();
        base.OnDisappearing();
    }

    private void OnSceneViewportSizeChanged(object? sender, EventArgs e)
    {
        if (!double.IsFinite(SceneViewport.Width) || SceneViewport.Width <= 0)
        {
            return;
        }

        var scale = Math.Min(SceneViewport.Width / DesignWidth, 1);
        SceneViewport.HeightRequest = DesignHeight * scale;

        SignPresenter.WidthRequest = FaceWidth * scale;
        SignPresenter.HeightRequest = FaceHeight * scale;
        SignPresenter.TranslationX = FaceX * scale;
        SignPresenter.TranslationY = FaceY * scale;
        SignPresenter.Rotation = FaceRotation;
        SignPresenter.RotationX = FaceRotationX;
        SignPresenter.RotationY = FaceRotationY;

        CanaryStatus.Text =
            $"viewport={SceneViewport.Width:0.#}×{SceneViewport.HeightRequest:0.#} " +
            $"face={SignPresenter.WidthRequest:0.#}×{SignPresenter.HeightRequest:0.#} " +
            $"at {SignPresenter.TranslationX:0.#},{SignPresenter.TranslationY:0.#} " +
            $"rot={FaceRotation:0.0}/{FaceRotationX:0.0}/{FaceRotationY:0.0}";
    }

    private void StartFaceLoop()
    {
        StopFaceLoop();
        _loopCancellation = new CancellationTokenSource();
        _ = RunFaceLoopAsync(_loopCancellation.Token);
    }

    private void StopFaceLoop()
    {
        var cancellation = Interlocked.Exchange(ref _loopCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        FrontFace.CancelAnimations();
        cancellation.Dispose();
    }

    private async Task RunFaceLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token);
                await FrontFace.FadeToAsync(0, 260, Easing.Linear);
                await Task.Delay(TimeSpan.FromSeconds(2), token);
                await FrontFace.FadeToAsync(1, 260, Easing.Linear);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Page navigation owns this intentionally endless public canary loop.
        }
    }
}
