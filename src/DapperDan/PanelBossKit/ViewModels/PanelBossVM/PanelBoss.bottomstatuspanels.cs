
using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Bottom Actions
    public partial class PanelBoss : BindableBase
    {
        private static async Task BottomStatusPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActiveBottomPanel = view.BottomStatusPanels.SortedAndFilteredView?.Reverse().FirstOrDefault();

            if (previousActiveBottomPanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveBottomPanel) + 1;
                var theResetOfThem = view.BottomStatusPanels.Where((iTM) => iTM != panelToActivate).ToList();
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(panelToActivate, true);
                    foreach (var iTM in theResetOfThem)
                    {
                        // Ensure any UI updates are dispatched on the UI thread.
                        PanelBoss.SetPanelIsVisible(iTM, false);
                    }
                });
            }
            //else
            //{
            //    var dum = true;
            //}
        }
        private static async Task BottomStatusPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {

            // Ensure any UI updates are dispatched on the UI thread.
            await view.Dispatcher.DispatchAsync(() =>
            {
                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });

            var previousActiveBottomPanel = view.BottomStatusPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            //Dont just make it visible again!
            if (previousActiveBottomPanel != null & previousActiveBottomPanel != panelToDeActivate)
            {
                //var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveBottomPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    // PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(previousActiveBottomPanel, true);
                });
            }
        }


        public async Task BottomStatusPanels_ActivatePanelByName(string panelNameToActivate, Int32? ZIndex = null, int delay = 0)
        {
            try
            {
                await Task.Delay(delay);
                //Convenience method to close by string instead of method name.
                if (panelNameToActivate == "CloseAll")
                {
                    await BottomStatusPanels_CloseAllAsyncInternal();
                    return;
                }

                var view = GetPanelBossStandardView();
                if (view != null)
                {
                    //var list1 = view.BottomStatusPanels.Select((iTM) => PanelBoss.GetPanelName(iTM));

                    var panelToActivate = view.BottomStatusPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                    if (panelToActivate != null)
                    {
                        if (ZIndex != null)
                        {
                            view.BottomStatusPanelsArea.ZIndex = ZIndex.Value;
                        }

                        await BottomStatusPanel_ActivateInternalAsync(panelToActivate, view);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BottomStatusPanels_ActivatePanelByName");
                Debug.WriteLine(ex);
            }
        }
        public async Task BottomStatusPanels_TogglePanelByName(string panelNameToToggle, Int32? ZIndex = null)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToToggle == "CloseAll")
            {
                await BottomStatusPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.BottomStatusPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));
                var wasVisible = panelToToggle?.IsVisible ?? false;// PanelBoss.GetPanelIsVisible(panelToToggle);

                if (panelToToggle != null)
                {
                    if (ZIndex != null)
                    {
                        view.BottomStatusPanelsArea.ZIndex = ZIndex.Value;
                    }

                    if (wasVisible)
                    {
                        await BottomStatusPanels_DeActivatePanelByName(panelNameToToggle);
                    }
                    else
                    {
                        await BottomStatusPanels_ActivatePanelByName(panelNameToToggle);
                    }
                }
            }
        }

        public async Task BottomStatusPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await BottomStatusPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }

        public async Task BottomStatusPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.BottomStatusPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await BottomStatusPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }

        public async Task BottomStatusPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await BottomStatusPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }


        public async Task BottomStatusPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.BottomStatusPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };

                var activeLeftPanel = view.BottomStatusPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activeLeftPanel != null)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activeLeftPanel, true);
                    });

                    var theResetOfThem = view.BottomStatusPanels.Except(view.BottomStatusPanels.TakeLast(1)).ToList();
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

        public async Task BottomStatusPanels_CloseAllAsync()
        {
            await BottomStatusPanels_CloseAllAsyncInternal();

        }

        private async Task BottomStatusPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var theseSelectorPanels = view.BottomStatusPanels;

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