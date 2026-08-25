using CodeCrafty.DapperDan.PanelBossKit;

namespace CodeCrafty.DapperDan.ViewModels;

public sealed class BillboardCanaryViewModel
{
    public BillboardCanaryViewModel(PanelBoss activePanelBoss)
    {
        ActivePanelBoss = activePanelBoss;
    }

    public PanelBoss ActivePanelBoss { get; }
}
