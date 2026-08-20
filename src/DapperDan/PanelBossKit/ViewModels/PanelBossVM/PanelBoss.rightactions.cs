
using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Right Actions
    public partial class PanelBoss : BindableBase
    {
        private static async Task RightNotificationPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActiveLeftPanel = view.RightSelectorPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            if (previousActiveLeftPanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveLeftPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(panelToActivate, true);
                });

                var theResetOfThem = view.RightSelectorPanels.Where((iTM) => iTM != panelToActivate).ToList();
                foreach (var iTM in theResetOfThem)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(iTM, false);
                    });
                }
            }
        }
        private static async Task RightNotificationPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {

            // Ensure any UI updates are dispatched on the UI thread.
            await view.Dispatcher.DispatchAsync(() =>
            {

                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });

            var previousActiveLeftPanel = view.RightSelectorPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            if (previousActiveLeftPanel != null && previousActiveLeftPanel != panelToDeActivate)
            {
                //var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveLeftPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    // PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(previousActiveLeftPanel, true);
                });

            }

        }


        public async Task RightSelectorPanels_ActivatePanelByName(string panelNameToActivate)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToActivate == "CloseAll")
            {
                await RightSelectorPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToActivate = view.RightSelectorPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                if (panelToActivate != null)
                {
                    await RightNotificationPanel_ActivateInternalAsync(panelToActivate, view);
                }
            }
        }

        public async Task RightSelectorPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await RightNotificationPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }

        public async Task RightSelectorPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.RightSelectorPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await RightNotificationPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }

        public async Task RightSelectorPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await RightNotificationPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }


        public async Task RightSelectorPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.RightSelectorPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };

                var activeLeftPanel = view.RightSelectorPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activeLeftPanel != null)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activeLeftPanel, true);
                    });

                    var theResetOfThem = view.RightSelectorPanels.Except(view.RightSelectorPanels.TakeLast(1)).ToList();
                    foreach (var iTM in theResetOfThem)
                    {
                        // Ensure any UI updates are dispatched on the UI thread.
                        await view.Dispatcher.DispatchAsync(() =>
                        {
                            PanelBoss.SetPanelIsVisible(iTM, false);
                        });
                    }
                }
            }
        }

        public async Task RightSelectorPanels_CloseAllAsync()
        {
            await RightSelectorPanels_CloseAllAsyncInternal();

        }

        private async Task RightSelectorPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var theseSelectorPanels = view.RightSelectorPanels;

                foreach (var iTM_Panel in theseSelectorPanels)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
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
