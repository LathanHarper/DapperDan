using CodeCrafty.DapperDan.PanelBossKit;
using Prism.Commands;
using Prism.Mvvm;

namespace CodeCrafty.DapperDan.ViewModels;

public sealed class BottomPanelCanaryViewModel : BindableBase
{
    private readonly PanelBoss _activePanelBoss;
    public PanelBoss ActivePanelBoss => _activePanelBoss;

    private string _status = "Waiting for native layout measurements.";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private readonly DelegateCommand<string> _selectActionCommand;
    public DelegateCommand<string> SelectActionCommand => _selectActionCommand;
    private void SelectAction(string action) => Status = $"Selected {action}.";

    public BottomPanelCanaryViewModel(PanelBoss activePanelBoss)
    {
        _activePanelBoss = activePanelBoss;
        _selectActionCommand = new DelegateCommand<string>(SelectAction);
    }
}
