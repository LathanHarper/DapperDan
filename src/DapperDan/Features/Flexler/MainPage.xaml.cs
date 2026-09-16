using Flexler.PanelBossKit;
using Flexler.ViewModels;
using CodeCrafty.DapperDan.Diagnostics;

namespace Flexler.Views;

public partial class MainPage : ContentPage
{
    private bool? _appliedSidePanelLayout;
    private MainPageViewModel? _viewModel;

    public MainPage()
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerPageXamlEnter);
        try
        {
            InitializeComponent();
            CrashJournal.Checkpoint(CrashPoint.FlexlerPageXamlReady);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(CrashSource.GuardedSeam,
                CrashPoint.FlexlerNavigationFailed, exception, terminating: false);
            throw;
        }
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CrashJournal.Checkpoint(CrashPoint.FlexlerPageAppearing);
    }

    protected override void OnBindingContextChanged()
    {
        DetachMaterialEvents();
        base.OnBindingContextChanged();
        _viewModel = BindingContext as MainPageViewModel;
        AttachMaterialEvents();
        _appliedSidePanelLayout = null;
        UpdatePanelOrientation(Width, Height);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdatePanelOrientation(width, height);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        AttachMaterialEvents();
        CrashJournal.Checkpoint(CrashPoint.FlexlerPageLoaded);
    }

    private void OnUnloaded(object? sender, EventArgs e) => DetachMaterialEvents();

    private void AttachMaterialEvents()
    {
        if (_viewModel is null) return;
        _viewModel.ActivePanelBoss.PanelShown -= OnPanelShown;
        _viewModel.ActivePanelBoss.PanelShown += OnPanelShown;
    }

    private void DetachMaterialEvents()
    {
        if (_viewModel is not null)
            _viewModel.ActivePanelBoss.PanelShown -= OnPanelShown;
    }

    // Only choose the lane here. XAML's Auto rows and one-way followers own clearance.
    private async void UpdatePanelOrientation(double width, double height)
    {
        if (PanelBossRoot is null || width <= 0 || height <= 0) return;
        var useSidePanels = (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) && width > height;
        if (_appliedSidePanelLayout == useSidePanels) return;

        _appliedSidePanelLayout = useSidePanels;
        if (_viewModel is not null)
            await _viewModel.ActivePanelBoss.SetUseSidePanelLayoutAsync(useSidePanels);
        else
            PanelBossRoot.UseSidePanelLayout = useSidePanels;
    }

    private async void OnPanelShown(object? sender, View panel)
    {
        switch (PanelBoss.GetPanelName(panel))
        {
            case MainPageViewModel.ContainerControlsPanelName:
                await RevealMineralLayerAsync(
                    ReferenceEquals(panel, LandscapeContainerControlsPanel)
                        ? LandscapeContainerMaterialLayer
                        : ContainerMaterialLayer,
                    startTranslationX: 18,
                    finalTranslationX: -14,
                    finalOpacity: 0.24);
                break;
            case MainPageViewModel.InspectorPanelName:
                await RevealMineralLayerAsync(
                    ReferenceEquals(panel, LandscapeInspectorPanel)
                        ? LandscapeInspectorMaterialLayer
                        : InspectorMaterialLayer,
                    startTranslationX: -18,
                    finalTranslationX: 14,
                    finalOpacity: 0.26);
                break;
        }
    }

    private static async Task RevealMineralLayerAsync(
        VisualElement layer,
        double startTranslationX,
        double finalTranslationX,
        double finalOpacity)
    {
        layer.TranslationX = startTranslationX;
        layer.Opacity = finalOpacity * 0.45;
        await Task.WhenAll(
            layer.TranslateToAsync(finalTranslationX, 0, 360, Easing.CubicOut),
            layer.FadeToAsync(finalOpacity, 280, Easing.CubicOut));
    }
}
