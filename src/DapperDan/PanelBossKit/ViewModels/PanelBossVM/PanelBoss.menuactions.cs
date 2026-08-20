using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Bottom Actions
    public partial class PanelBoss : BindableBase
    {

        private static async Task MenuPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActivePanel = view.MenuPanels.SortedAndFilteredView?.Reverse().FirstOrDefault();
            if (previousActivePanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActivePanel) + 1;
                var theResetOfThem = view.MenuPanels.Where((iTM) => iTM != panelToActivate).ToList();
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(panelToActivate, true);
                    foreach (var iTM in theResetOfThem)
                    {
                        PanelBoss.SetPanelIsVisible(iTM, false);
                    }
                });
            }
        }
        private static async Task MenuPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {
            await view.Dispatcher.DispatchAsync(() =>
            {
                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });
            var previousActivePanel = view.MenuPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
            if (previousActivePanel != null & previousActivePanel != panelToDeActivate)
            {
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelIsVisible(previousActivePanel, true);
                });
            }
        }
        public async Task MenuPanels_ActivatePanelByName(string panelNameToActivate, Int32? ZIndex = null)
        {
            try
            {
                if (panelNameToActivate == "CloseAll")
                {
                    await MenuPanels_CloseAllAsyncInternal();
                    return;
                }
                var view = GetPanelBossStandardView();
                if (view != null)
                {
                    var panelToActivate = view.MenuPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                    if (panelToActivate != null)
                    {
                        if (ZIndex != null)
                        {
                            view.MenuPanelsArea.ZIndex = ZIndex.Value;
                        }
                        await MenuPanel_ActivateInternalAsync(panelToActivate, view);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MenuPanels_ActivatePanelByName");
                Debug.WriteLine(ex);
            }
        }
        public async Task MenuPanels_TogglePanelByName(string panelNameToToggle, Int32? ZIndex = null)
        {
            if (panelNameToToggle == "CloseAll")
            {
                await MenuPanels_CloseAllAsyncInternal();
                return;
            }
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.MenuPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));
                var wasVisible = panelToToggle?.IsVisible ?? false;
                if (panelToToggle != null)
                {
                    if (ZIndex != null)
                    {
                        view.MenuPanelsArea.ZIndex = ZIndex.Value;
                    }
                    if (wasVisible)
                    {
                        await MenuPanels_DeActivatePanelByName(panelNameToToggle);
                    }
                    else
                    {
                        await MenuPanels_ActivatePanelByName(panelNameToToggle);
                    }
                }
            }
        }
        public async Task MenuPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await MenuPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }
        public async Task MenuPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.MenuPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await MenuPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }
        public async Task MenuPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await MenuPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }
        public async Task MenuPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.MenuPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };
                var activePanel = view.MenuPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activePanel != null)
                {
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activePanel, true);
                    });
                    var theResetOfThem = view.MenuPanels.Except(view.MenuPanels.TakeLast(1)).ToList();
                    foreach (var iTM in theResetOfThem)
                    {
                        await view.Dispatcher.DispatchAsync(() =>
                        {
                            PanelBoss.SetPanelIsVisible(iTM, false);
                        });
                    }
                }
            }
        }
        public async Task MenuPanels_CloseAllAsync()
        {
            await MenuPanels_CloseAllAsyncInternal();
        }
        private async Task MenuPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var thesePanels = view.MenuPanels;
                foreach (var iTM_Panel in thesePanels)
                {
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        var wasVisible = PanelBoss.GetPanelIsVisible(iTM_Panel);
                        if (wasVisible)
                        {
                            PanelBoss.SetPanelIsVisible(iTM_Panel, false);
                        }
                    });
                }
            }
        }
    }




}