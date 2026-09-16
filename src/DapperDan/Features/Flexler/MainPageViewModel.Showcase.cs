using Prism.Commands;

namespace Flexler.ViewModels;

// The sole host-specific command; layout editing and recipes keep their production code.
public sealed partial class MainPageViewModel
{
    private readonly INavigationService? _showcaseNavigation;
    private AsyncDelegateCommand? _backToDapperCommand;
    private bool _isReturningToDapper;

    public AsyncDelegateCommand BackToDapperCommand =>
        _backToDapperCommand ??= new AsyncDelegateCommand(BackToDapperAsync,
            () => _showcaseNavigation is not null);

    public bool IsReturningToDapper
    {
        get => _isReturningToDapper;
        set => SetProperty(ref _isReturningToDapper, value);
    }

    private async Task BackToDapperAsync()
    {
        if (_showcaseNavigation is null) return;
        IsReturningToDapper = true;
        try
        {
            // A proof build can start here directly, without a preceding Dapper page.
            var result = await _showcaseNavigation.GoBackAsync();
            if (!result.Success)
                result = await _showcaseNavigation.NavigateAsync("/NavigationPage/DapperDanPage");
            if (!result.Success)
                ExportStatus = $"Could not return to Dapper: {result.Exception?.Message ?? "navigation unavailable"}";
        }
        finally
        {
            IsReturningToDapper = false;
        }
    }
}
