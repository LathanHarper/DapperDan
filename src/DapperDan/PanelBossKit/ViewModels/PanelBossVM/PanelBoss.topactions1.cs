
using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Top Actions1
    public partial class PanelBoss : BindableBase
    {
        private static async Task TopHeaderPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActiveBottomPanel = view.TopHeaderPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            if (previousActiveBottomPanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveBottomPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(panelToActivate, true);
                });

                var theResetOfThem = view.TopHeaderPanels.Where((iTM) => iTM != panelToActivate).ToList();
                foreach (var iTM in theResetOfThem)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(iTM, false);
                    });
                }
            }
            //else
            //{
            //    var dum = true;
            //}
        }
        private static async Task TopHeaderPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {

            // Ensure any UI updates are dispatched on the UI thread.
            await view.Dispatcher.DispatchAsync(() =>
            {

                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });

            var previousActiveBottomPanel = view.TopHeaderPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

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


        public async Task TopHeaderPanels_ActivatePanelByName(string panelNameToActivate)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToActivate == "CloseAll")
            {
                await TopHeaderPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToActivate = view.TopHeaderPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                if (panelToActivate != null)
                {
                    await TopHeaderPanel_ActivateInternalAsync(panelToActivate, view);
                }
            }
        }
        public async Task TopHeaderPanels_ActivatePanelByName(string panelNameToActivate, object itemBindingContext)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToActivate == "CloseAll")
            {
                await TopHeaderPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToActivate = view.TopHeaderPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                if (panelToActivate != null)
                {
                    await TopHeaderPanel_ActivateInternalAsync(panelToActivate, view);
                    panelToActivate.BindingContext = itemBindingContext;
                }
            }
        }
        public async Task TopHeaderPanels_TogglePanelByName(string panelNameToToggle)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToToggle == "CloseAll")
            {
                await TopHeaderPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.TopHeaderPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));
                var wasVisible = panelToToggle.IsVisible;// PanelBoss.GetPanelIsVisible(panelToToggle);

                if (panelToToggle != null)
                {
                    if (wasVisible)
                    {



                        await TopHeaderPanels_DeActivatePanelByName(panelNameToToggle);

                    }
                    else
                    {
                        await TopHeaderPanels_ActivatePanelByName(panelNameToToggle);

                    }
                }
            }
        }

        public async Task TopHeaderPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await TopHeaderPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }

        public async Task TopHeaderPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.TopHeaderPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await TopHeaderPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }

        public async Task TopHeaderPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await TopHeaderPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }


        public async Task TopHeaderPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.TopHeaderPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };

                var activeLeftPanel = view.TopHeaderPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activeLeftPanel != null)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activeLeftPanel, true);
                    });

                    var theResetOfThem = view.TopHeaderPanels.Except(view.TopHeaderPanels.TakeLast(1)).ToList();
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

        public async Task TopHeaderPanels_CloseAllAsync()
        {
            await TopHeaderPanels_CloseAllAsyncInternal();

        }

        private async Task TopHeaderPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var theseSelectorPanels = view.TopHeaderPanels;

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