using CodeCrafty.DapperDan.ViewModels;
using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan.Views.DapperDan;

public partial class DapperDanPage : ContentPage
{
    private readonly DapperDanViewModel _viewModel;

    public DapperDanPage(DapperDanViewModel viewModel)
    {
        CrashJournal.Checkpoint(CrashPoint.PageConstructorEnter);

        try
        {
            CrashJournal.Checkpoint(CrashPoint.PageXamlEnter);
            InitializeComponent();
            CrashJournal.Checkpoint(CrashPoint.PageXamlReady);
            _viewModel = viewModel;
            BindingContext = viewModel;
            CrashJournal.Checkpoint(CrashPoint.PageBindingReady);
            Loaded += OnPageLoaded;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.PageConstructorEnter,
                exception,
                terminating: true);
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CrashJournal.Checkpoint(CrashPoint.PageAppearingEnter);

        try
        {
            await _viewModel.InitializeAsync();
            CrashJournal.Checkpoint(CrashPoint.PageAppearingReady);
            Dispatcher.Dispatch(() =>
                CrashJournal.Checkpoint(CrashPoint.FirstResponsiveDispatch));
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.PageAppearingEnter,
                exception,
                terminating: true);
            throw;
        }
    }

    private static void OnPageLoaded(object? sender, EventArgs eventArgs)
        => CrashJournal.Checkpoint(CrashPoint.PageLoaded);
}
