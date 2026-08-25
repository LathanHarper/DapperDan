using Prism.Commands;
using CodeCrafty.DapperDan.Models;

namespace CodeCrafty.DapperDan.ViewModels;

public partial class DapperDanViewModel
{
    private AsyncDelegateCommand? _addKeikiCommand;
    private AsyncDelegateCommand? _asyncSpecimenCommand;
    private AsyncDelegateCommand? _clearKeikiCommand;
    private AsyncDelegateCommand<string>? _executePanelDemoCommand;
    private AsyncDelegateCommand? _loadKeikiCommand;
    private AsyncDelegateCommand? _openRotationCanaryCommand;
    private AsyncDelegateCommand<string>? _runVoiceCanaryCommand;
    private AsyncDelegateCommand<DapperDanPageAction>? _selectPageActionCommand;
    private DelegateCommand? _stopVoiceCanaryCommand;

    public AsyncDelegateCommand AddKeikiCommand =>
        _addKeikiCommand ??= new AsyncDelegateCommand(AddKeikiAsync);

    public AsyncDelegateCommand AsyncSpecimenCommand =>
        _asyncSpecimenCommand ??= new AsyncDelegateCommand(RunAsyncSpecimenAsync);

    public AsyncDelegateCommand ClearKeikiCommand =>
        _clearKeikiCommand ??= new AsyncDelegateCommand(ClearKeikiAsync);

    public AsyncDelegateCommand<string> ExecutePanelDemoCommand =>
        _executePanelDemoCommand ??= new AsyncDelegateCommand<string>(ExecutePanelDemoAsync);

    public AsyncDelegateCommand LoadKeikiCommand =>
        _loadKeikiCommand ??= new AsyncDelegateCommand(LoadKeikiAsync);

    public AsyncDelegateCommand OpenRotationCanaryCommand =>
        _openRotationCanaryCommand ??= new AsyncDelegateCommand(OpenRotationCanaryAsync);

    public AsyncDelegateCommand<string> RunVoiceCanaryCommand =>
        _runVoiceCanaryCommand ??=
            new AsyncDelegateCommand<string>(RunVoiceCanaryAsync);

    public AsyncDelegateCommand<DapperDanPageAction> SelectPageActionCommand =>
        _selectPageActionCommand ??= new AsyncDelegateCommand<DapperDanPageAction>(SelectPageActionAsync);

    public DelegateCommand StopVoiceCanaryCommand =>
        _stopVoiceCanaryCommand ??= new DelegateCommand(StopVoiceCanary);
}
