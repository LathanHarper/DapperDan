
using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;




namespace CodeCrafty.DapperDan.PanelBossKit
{
    //ContentPanels
    public partial class PanelBoss : BindableBase
    {
        private static async Task ContentPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActiveContentPanel = view.ContentPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            if (previousActiveContentPanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveContentPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(panelToActivate, true);
                });

                var theResetOfThem = view.ContentPanels.Where((iTM) => iTM != panelToActivate).ToList();
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

        private static async Task ContentPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {
            // Ensure any UI updates are dispatched on the UI thread.
            await view.Dispatcher.DispatchAsync(() =>
            {
                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });

            var previousActiveContentPanel = view.ContentPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

            if (previousActiveContentPanel != null && previousActiveContentPanel != panelToDeActivate)
            {
                //var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveContentPanel) + 1;
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    // PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);
                    PanelBoss.SetPanelIsVisible(previousActiveContentPanel, true);
                });
            }
        }


        public async Task ContentPanels_ActivatePanelByName(string panelNameToActivate)
        {
            //Convenience method to close by string instead of method name.
            if (panelNameToActivate == "CloseAll")
            {
                await ContentPanels_CloseAllAsyncInternal();
                return;
            }

            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToActivate = view.ContentPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                if (panelToActivate != null)
                {
                    await ContentPanel_ActivateInternalAsync(panelToActivate, view);
                }
            }
        }

        public async Task ContentPanels_ToggleHeaderOwningPanelByName(string panelNameToToggle)
        {
            var view = GetPanelBossStandardView();
            if (view == null)
                return;

            var panelToToggle = view.ContentPanels.SingleOrDefault(
                panel => panelNameToToggle == PanelBoss.GetPanelName(panel));
            if (panelToToggle == null)
                return;

            if (PanelBoss.GetPanelIsVisible(panelToToggle))
            {
                await RestoreDefaultPanelChromeAsync(panelNameToToggle);
                return;
            }

            await TopHeaderPanels_CloseAllAsync();
            await ContentPanel_ActivateInternalAsync(panelToToggle, view);
        }

        public async Task<bool> ContentPanels_RestoreDefaultPanelAsync(params string[] extensionPanelNames)
        {
            var view = GetPanelBossStandardView();
            if (view == null)
                return false;

            var extensionNames = new HashSet<string>(
                extensionPanelNames ?? [],
                StringComparer.OrdinalIgnoreCase);
            var extensionPanels = view.ContentPanels
                .Where(panel => extensionNames.Contains(PanelBoss.GetPanelName(panel)))
                .ToList();
            var wasExtensionVisible = extensionPanels.Any(PanelBoss.GetPanelIsVisible);

            if (!wasExtensionVisible)
                return false;

            var defaultPanel = view.ContentPanels
                .Where(panel => !extensionNames.Contains(PanelBoss.GetPanelName(panel)))
                .OrderByDescending(PanelBoss.GetPanelPriority)
                .FirstOrDefault();

            if (defaultPanel == null)
            {
                await ContentPanels_CloseAllAsyncInternal();
                return true;
            }

            await view.Dispatcher.DispatchAsync(() =>
            {
                foreach (var panel in view.ContentPanels)
                {
                    var isDefaultPanel = panel == defaultPanel;

                    if (extensionPanels.Contains(panel))
                    {
                        PanelBoss.SetPanelPriority(panel, 0);
                    }

                    if (PanelBoss.GetPanelIsVisible(panel) != isDefaultPanel)
                    {
                        PanelBoss.SetPanelIsVisible(panel, isDefaultPanel);
                    }
                }
            });

            return true;
        }

        public async Task ContentPanels_ActivatePanelByName(string panelNameToActivate, object itemBindingContext)
        {
            //Convenience method to close by string instead of method name.
            if (panelNameToActivate == "CloseAll")
            {
                await ContentPanels_CloseAllAsyncInternal();
                return;
            }

            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToActivate = view.ContentPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                if (panelToActivate != null)
                {
                    await ContentPanel_ActivateInternalAsync(panelToActivate, view);
                    panelToActivate.BindingContext = itemBindingContext;
                }
            }
        }

        public async Task ContentPanels_TogglePanelByName(string panelNameToToggle)
        {
            //Convenience method to close by string instead of method name.
            if (panelNameToToggle == "CloseAll")
            {
                await ContentPanels_CloseAllAsyncInternal();
                return;
            }

            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.ContentPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));
                var wasVisible = panelToToggle.IsVisible;

                if (panelToToggle != null)
                {
                    if (wasVisible)
                    {
                        await ContentPanels_DeActivatePanelByName(panelNameToToggle);
                    }
                    else
                    {
                        await ContentPanels_ActivatePanelByName(panelNameToToggle);
                    }
                }
            }
        }

        public async Task ContentPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await ContentPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }

        public async Task ContentPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.ContentPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await ContentPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }

        public async Task ContentPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await ContentPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }


        public async Task ContentPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.ContentPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };

                var activeContentPanel = view.ContentPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activeContentPanel != null)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activeContentPanel, true);
                    });

                    var theResetOfThem = view.ContentPanels.Except(view.ContentPanels.TakeLast(1)).ToList();
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

        public async Task ContentPanels_CloseAllAsync()
        {
            await ContentPanels_CloseAllAsyncInternal();
        }

        private async Task ContentPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var theseContentPanels = view.ContentPanels;

                foreach (var iTM_Panel in theseContentPanels)
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
