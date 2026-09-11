using System.Text.Json;
using CodeCrafty.DapperDan.Controls;
using CodeCrafty.DapperDan.ViewModels;

namespace CodeCrafty.DapperDan.Views.BottomPanelCanary;

public partial class BottomPanelCanaryPage : ContentPage
{
    private readonly BottomPanelCanaryViewModel _viewModel;
    private readonly string _case;
    private CancellationTokenSource? _measurementCancellation;

    public BottomPanelCanaryPage(BottomPanelCanaryViewModel viewModel)
    {
        _case = Environment.GetEnvironmentVariable("DAPPERDAN_PANEL_CASE") ?? "baseline";
        if (_case is not ("baseline" or "safe-area-none"))
            throw new InvalidOperationException("Unknown bottom-panel canary case.");

        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // This is the single layout variable under test. Never resize the panel or buttons.
        if (_case == "safe-area-none")
            ActionPanel.SafeAreaEdges = SafeAreaEdges.None;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _measurementCancellation?.Cancel();
        _measurementCancellation?.Dispose();
        _measurementCancellation = new CancellationTokenSource();
        var cancellationToken = _measurementCancellation.Token;
        try
        {
            // Observe the settled native layout; these measurements never feed layout back.
            await Task.Delay(2000, cancellationToken);
            var measurement = Measure();
            _viewModel.Status =
                $"{_case}\nPanel: {measurement.Panel.Height:F1} pt; header: {measurement.Tongue.Height:F1} pt\n" +
                $"First button: {measurement.Buttons[0].Button.Height:F1} pt; border: {measurement.Buttons[0].Border.Height:F1} pt\n" +
                $"Window bottom inset: {measurement.IosInsets.Bottom:F1} pt";
            var json = JsonSerializer.Serialize(measurement, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "bottom-panel-canary.json"), json, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _viewModel.Status = $"Measurement failed: {exception.GetType().Name}";
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    protected override void OnDisappearing()
    {
        _measurementCancellation?.Cancel();
        base.OnDisappearing();
    }

    private Measurement Measure()
    {
        var insets = new Insets(0, 0, 0, 0);
#if IOS
        if (Handler?.PlatformView is UIKit.UIView pageView)
        {
            var native = pageView.Window?.SafeAreaInsets ?? pageView.SafeAreaInsets;
            insets = new Insets((double)native.Top, (double)native.Bottom, (double)native.Left, (double)native.Right);
        }
#endif
        return new Measurement(
            1, _case, BoundsOf(ActionPanel), BoundsOf(Tongue),
            [
                MeasureButton(AlphaButton, AlphaBorder),
                MeasureButton(BravoButton, BravoBorder),
                MeasureButton(CharlieButton, CharlieBorder),
                MeasureButton(DeltaButton, DeltaBorder),
                MeasureButton(MoreButton, MoreBorder)
            ],
            insets, ActionPanel.SafeAreaEdges.ToString());
    }

    private static ButtonMeasurement MeasureButton(RichButton button, Border border) =>
        new(button.AutomationId, BoundsOf(button), BoundsOf(border));

    private static LayoutBounds BoundsOf(VisualElement view)
    {
        if (!double.IsFinite(view.Width) || !double.IsFinite(view.Height) || view.Width <= 0 || view.Height <= 0)
            throw new InvalidOperationException("The canary has no positive native layout bounds.");
        return new LayoutBounds(view.X, view.Y, view.Width, view.Height);
    }

    private sealed record LayoutBounds(double X, double Y, double Width, double Height);
    private sealed record Insets(double Top, double Bottom, double Left, double Right);
    private sealed record ButtonMeasurement(string Id, LayoutBounds Button, LayoutBounds Border);
    private sealed record Measurement(int Schema, string Case, LayoutBounds Panel, LayoutBounds Tongue,
        ButtonMeasurement[] Buttons, Insets IosInsets, string PanelSafeAreaEdges);
}
