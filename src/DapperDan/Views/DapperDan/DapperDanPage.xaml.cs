using CodeCrafty.DapperDan.ViewModels;

namespace CodeCrafty.DapperDan.Views.DapperDan;

public partial class DapperDanPage : ContentPage
{
    private readonly DapperDanViewModel _viewModel;

    public DapperDanPage(DapperDanViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
