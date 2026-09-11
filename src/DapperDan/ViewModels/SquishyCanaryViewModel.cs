using CodeCrafty.DapperDan.PanelBossKit;
using Prism.Commands;
using Prism.Mvvm;

namespace CodeCrafty.DapperDan.ViewModels;

public sealed class SquishyCanaryViewModel : BindableBase
{
    #region ActivePanelBoss
    private readonly PanelBoss _activePanelBoss;
    public PanelBoss ActivePanelBoss => _activePanelBoss;
    #endregion

    #region TextSize
    private double _actionFontSize = 14;
    public double ActionFontSize
    {
        get => _actionFontSize;
        private set
        {
            if (SetProperty(ref _actionFontSize, value))
            {
                RaisePropertyChanged(nameof(ActionGlyphSize));
                RaisePropertyChanged(nameof(SampleFontSize));
                RaisePropertyChanged(nameof(TextMode));
            }
        }
    }
    public double ActionGlyphSize => ActionFontSize + 4;
    public double SampleFontSize => ActionFontSize + 2;
    public string TextMode => ActionFontSize > 14 ? "large" : "normal";

    private readonly DelegateCommand<string> _setTextSizeCommand;
    public DelegateCommand<string> SetTextSizeCommand => _setTextSizeCommand;
    private void SetTextSize(string size) => ActionFontSize = size == "Large" ? 24 : 14;
    #endregion

    #region Tongue
    private bool _isTongueVisible = true;
    public bool IsTongueVisible
    {
        get => _isTongueVisible;
        private set => SetProperty(ref _isTongueVisible, value);
    }

    private readonly DelegateCommand _toggleTongueCommand;
    public DelegateCommand ToggleTongueCommand => _toggleTongueCommand;
    private void ToggleTongue() => IsTongueVisible = !IsTongueVisible;
    #endregion

    #region Actions
    private string _status = "Natural chrome. Remaining space scrolls.";
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private readonly AsyncDelegateCommand<string> _selectActionCommand;
    public AsyncDelegateCommand<string> SelectActionCommand => _selectActionCommand;
    private async Task SelectActionAsync(string action)
    {
        if (action == "More")
        {
            await ActivePanelBoss.ContentPanels_ToggleHeaderOwningPanelByName("SquishyMorePanel");
        }
        else
        {
            await ActivePanelBoss.RestoreDefaultPanelChromeAsync("SquishyMorePanel");
            Status = $"Selected {action}. Natural chrome. Remaining space scrolls.";
        }
    }
    #endregion

    #region SampleRows
    private readonly string[] _sampleRows = Enumerable.Range(1, 16)
        .Select(index => $"Sample row {index:00}. This neutral content wraps naturally. The viewport should use every available point between its top and bottom markers.")
        .ToArray();
    public string[] SampleRows => _sampleRows;
    #endregion

    public SquishyCanaryViewModel(PanelBoss activePanelBoss)
    {
        _activePanelBoss = activePanelBoss;
        _setTextSizeCommand = new DelegateCommand<string>(SetTextSize);
        _toggleTongueCommand = new DelegateCommand(ToggleTongue);
        _selectActionCommand = new AsyncDelegateCommand<string>(SelectActionAsync);
    }
}
