using System.ComponentModel;
using System.Text.Json;
using CodeCrafty.DapperDan.PanelBossKit;
using CodeCrafty.DapperDan.ViewModels;

namespace CodeCrafty.DapperDan.Views.SquishyCanary;

public partial class SquishyCanaryPage : ContentPage
{
    private readonly SquishyCanaryViewModel _viewModel;
    private CancellationTokenSource? _captureCancellation;
    private bool _isObserving;
    private int _captureSequence;

    public SquishyCanaryPage(SquishyCanaryViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_isObserving)
        {
            _isObserving = true;
            foreach (var view in ObservedViews())
                view.SizeChanged += OnGeometryChanged;
            _viewModel.PropertyChanged += OnSampleChanged;
        }
        QueueCapture();
    }

    protected override void OnDisappearing()
    {
        _isObserving = false;
        foreach (var view in ObservedViews())
            view.SizeChanged -= OnGeometryChanged;
        _viewModel.PropertyChanged -= OnSampleChanged;
        _captureCancellation?.Cancel();
        base.OnDisappearing();
    }

    private VisualElement[] ObservedViews() =>
        [PanelBossRoot, HeaderPanel, BodyViewport, BodyScroll, MoreHeader, MoreViewport, MoreScroll, ActionPanel, Tongue];

    private void OnGeometryChanged(object? sender, EventArgs args) => QueueCapture();
    private void OnSampleChanged(object? sender, PropertyChangedEventArgs args) => QueueCapture();

    private void QueueCapture()
    {
        if (!_isObserving)
            return;

        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        _captureCancellation = new CancellationTokenSource();
        _ = CaptureAfterLayoutAsync(_captureCancellation.Token);
    }

    private async Task CaptureAfterLayoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            // A diagnostic debounce, not a layout dependency. No measurement feeds sizing.
            await Task.Delay(400, cancellationToken);
            var moreVisible = PanelBoss.GetPanelIsVisible(MorePanel);
            var viewport = moreVisible ? MoreViewport : BodyViewport;
            var scroll = moreVisible ? MoreScroll : BodyScroll;
            var header = moreVisible ? MoreHeader : HeaderPanel;
            if (viewport.Height <= 0 || ActionPanel.Height <= 0)
                return;

            var measurement = new
            {
                Schema = 1,
                Scenario = "squishy",
                Sequence = ++_captureSequence,
                CapturedUtc = DateTimeOffset.UtcNow,
                Platform = DeviceInfo.Current.Platform.ToString(),
                _viewModel.TextMode,
                _viewModel.ActionFontSize,
                TongueVisible = _viewModel.IsTongueVisible,
                MoreVisible = moreVisible,
                Host = new LayoutFrame(0, 0, PanelBossRoot.Width, PanelBossRoot.Height),
                NativeInsets = ReadNativeInsets(),
                Header = InHost(header),
                Viewport = InHost(viewport),
                Scroll = InHost(scroll),
                Actions = InHost(ActionPanel),
                Tongue = _viewModel.IsTongueVisible ? InHost(Tongue) : null,
                _viewModel.ActivePanelBoss.PlatformBottomClearance,
                Buttons = new[]
                {
                    ButtonEvidence(AlphaButton, AlphaBorder, AlphaGlyph, AlphaText),
                    ButtonEvidence(BravoButton, BravoBorder, BravoGlyph, BravoText),
                    ButtonEvidence(CharlieButton, CharlieBorder, CharlieGlyph, CharlieText),
                    ButtonEvidence(DeltaButton, DeltaBorder, DeltaGlyph, DeltaText),
                    ButtonEvidence(MoreButton, MoreBorder, MoreGlyph, MoreText)
                }
            };
            var json = JsonSerializer.Serialize(measurement,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            var path = Path.Combine(FileSystem.AppDataDirectory, "squishy-canary.json");
            await File.WriteAllTextAsync(path + ".pending", json, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(path + ".pending", path, overwrite: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Squishy canary observation failed: {exception}");
        }
    }

    private object ButtonEvidence(VisualElement button, VisualElement border, VisualElement glyph, VisualElement text) =>
        new { Id = button.AutomationId, Button = InHost(button), Border = InHost(border), Glyph = InHost(glyph), Text = InHost(text) };

    private LayoutFrame InHost(VisualElement element)
    {
        var x = 0d;
        var y = 0d;
        Element? current = element;
        while (current is not null && !ReferenceEquals(current, PanelBossRoot))
        {
            if (current is VisualElement visual)
            {
                x += visual.X;
                y += visual.Y;
            }
            current = current.Parent;
        }
        if (current is null || !double.IsFinite(element.Width) || !double.IsFinite(element.Height))
            throw new InvalidOperationException("Observed element is not arranged inside this host.");
        return new LayoutFrame(x, y, element.Width, element.Height);
    }

    private Insets? ReadNativeInsets()
    {
#if IOS
        if (Handler?.PlatformView is UIKit.UIView pageView)
        {
            var inset = pageView.Window?.SafeAreaInsets ?? pageView.SafeAreaInsets;
            return new Insets((double)inset.Left, (double)inset.Top, (double)inset.Right, (double)inset.Bottom);
        }
#elif ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(30) && Handler?.PlatformView is global::Android.Views.View pageView)
        {
            var native = pageView.RootView?.RootWindowInsets;
            var density = pageView.Resources?.DisplayMetrics?.Density ?? 1;
            if (native is not null && density > 0)
            {
                var inset = native.GetInsetsIgnoringVisibility(
                    global::Android.Views.WindowInsets.Type.SystemBars() | global::Android.Views.WindowInsets.Type.DisplayCutout());
                return new Insets(inset.Left / density, inset.Top / density, inset.Right / density, inset.Bottom / density);
            }
        }
#endif
        return null;
    }

    private sealed record Insets(double Left, double Top, double Right, double Bottom);
    private sealed record LayoutFrame(double X, double Y, double Width, double Height);
}
