using CodeCrafty.DapperDan.ViewModels;

namespace CodeCrafty.DapperDan.Views.RotationCanary;

public partial class RotationCanaryPage : ContentPage
{
    private readonly RotationCanaryViewModel _viewModel;

    public RotationCanaryPage(RotationCanaryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        _viewModel.CancelCrossFade();
        base.OnDisappearing();
    }
}
