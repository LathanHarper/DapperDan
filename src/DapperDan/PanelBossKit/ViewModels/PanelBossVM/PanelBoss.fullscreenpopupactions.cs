using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Bottom Actions
    public partial class PanelBoss : BindableBase
    {

        private static async Task FullScreenPopupPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActivePanel = view.FullScreenPopupPanels.SortedAndFilteredView?.Reverse().FirstOrDefault();
            if (previousActivePanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActivePanel) + 1;
                var theResetOfThem = view.FullScreenPopupPanels.Where((iTM) => iTM != panelToActivate).ToList();
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
        private static async Task FullScreenPopupPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {
            if (panelToDeActivate is null || view is null)
            {
                return;
            }

            await view.Dispatcher.DispatchAsync(() =>
            {
                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });
            var previousActivePanel = (from iTM in view.FullScreenPopupPanels.SortedAndFilteredView
                                       where PanelBoss.GetPanelIsVisible(iTM) == true
                                       select iTM).Reverse().FirstOrDefault();


            if (previousActivePanel != null & previousActivePanel != panelToDeActivate)
            {
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelIsVisible(previousActivePanel, true);
                });
            }
        }
        public async Task FullScreenPopupPanels_ActivatePanelByName(string panelNameToActivate, Int32? ZIndex = null)
        {
            try
            {
                if (panelNameToActivate == "CloseAll")
                {
                    await FullScreenPopupPanels_CloseAllAsyncInternal();
                    return;
                }
                var view = GetPanelBossStandardView();
                if (view != null)
                {
                    var panelToActivate = view.FullScreenPopupPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                    if (panelToActivate != null)
                    {
                        if (ZIndex != null)
                        {
                            view.FullScreenPopupPanelsArea.ZIndex = ZIndex.Value;
                        }
                        await FullScreenPopupPanel_ActivateInternalAsync(panelToActivate, view);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FullScreenPopupPanels_ActivatePanelByName");
                Debug.WriteLine(ex);
            }
        }
        public async Task FullScreenPopupPanels_TogglePanelByName(string panelNameToToggle, Int32? ZIndex = null)
        {
            if (panelNameToToggle == "CloseAll")
            {
                await FullScreenPopupPanels_CloseAllAsyncInternal();
                return;
            }
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.FullScreenPopupPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));
                var wasVisible = panelToToggle?.IsVisible ?? false;
                if (panelToToggle != null)
                {
                    if (ZIndex != null)
                    {
                        view.FullScreenPopupPanelsArea.ZIndex = ZIndex.Value;
                    }
                    if (wasVisible)
                    {
                        await FullScreenPopupPanels_DeActivatePanelByName(panelNameToToggle);
                    }
                    else
                    {
                        await FullScreenPopupPanels_ActivatePanelByName(panelNameToToggle);
                    }
                }
            }
        }
        public async Task FullScreenPopupPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await FullScreenPopupPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }
        public async Task FullScreenPopupPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.FullScreenPopupPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                if (panelToDeActivate != null)
                {
                    await FullScreenPopupPanel_DeActivateInternalAsync(panelToDeActivate, view);
                }
            }
        }
        public async Task FullScreenPopupPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await FullScreenPopupPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }
        public async Task FullScreenPopupPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.FullScreenPopupPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };
                var activePanel = view.FullScreenPopupPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activePanel != null)
                {
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activePanel, true);
                    });
                    var theResetOfThem = view.FullScreenPopupPanels.Except(view.FullScreenPopupPanels.TakeLast(1)).ToList();
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
        public async Task FullScreenPopupPanels_CloseAllAsync()
        {
            await FullScreenPopupPanels_CloseAllAsyncInternal();
        }
        private async Task FullScreenPopupPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var thesePanels = view.FullScreenPopupPanels;
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
