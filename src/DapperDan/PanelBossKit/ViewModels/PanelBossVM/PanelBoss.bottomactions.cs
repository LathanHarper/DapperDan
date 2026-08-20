using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Bottom Actions
    public partial class PanelBoss : BindableBase
    {

        private View _ActiveBottomInputPanel;
        public View ActiveBottomInputPanel
        {
            get { return _ActiveBottomInputPanel; }
            set { SetProperty(ref _ActiveBottomInputPanel, value); }
        }

        public void MonkeyWithTheBottomRows(int newBottomInputPanelsZIndex)
        {
            this.panelBossStandardViewReference.TryGetTarget(out var view);
            //Grid.SetRowSpan(view.FullScreenPopupPanelsArea, newBottomInputPanelsZIndex);
            view.FullScreenPopupPanelsArea.ZIndex = newBottomInputPanelsZIndex;
        }
        private static async Task BottomInputPanel_ActivateInternalAsync(View panelToActivate, IPanelBoss_View view)
        {
            var previousActiveBottomPanel = view.BottomInputPanels.SortedAndFilteredView?.Reverse().FirstOrDefault();

            if (previousActiveBottomPanel != null)
            {
                var nextHigherPriority = PanelBoss.GetPanelPriority(previousActiveBottomPanel) + 1;
                var theResetOfThem = view.BottomInputPanels.Where((iTM) => iTM != panelToActivate).ToList();
                // Ensure any UI updates are dispatched on the UI thread.
                await view.Dispatcher.DispatchAsync(() =>
                {
                    PanelBoss.SetPanelPriority(panelToActivate, nextHigherPriority);


                    var LO = view.VerticalOptionsBinding;

                    PanelBoss.SetPanelVerticalOptions(panelToActivate, LO);
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
        private static async Task BottomInputPanel_DeActivateInternalAsync(View panelToDeActivate, IPanelBoss_View view)
        {

            // Ensure any UI updates are dispatched on the UI thread.
            await view.Dispatcher.DispatchAsync(() =>
            {
                PanelBoss.SetPanelPriority(panelToDeActivate, 0);
                PanelBoss.SetPanelIsVisible(panelToDeActivate, false);
            });

            var previousActiveBottomPanel = view.BottomInputPanels.SortedAndFilteredView.Reverse().FirstOrDefault();

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


        public async Task BottomInputPanels_ActivatePanelByName(string panelNameToActivate, Int32? ZIndex = null)
        {
            try
            {
                //Convenience method to close by string instead of method name.
                if (panelNameToActivate == "CloseAll")
                {
                    await BottomInputPanels_CloseAllAsyncInternal();
                    return;
                }

                var view = GetPanelBossStandardView();
                if (view != null)
                {
                    //var list1 = view.BottomInputPanels.Select((iTM) => PanelBoss.GetPanelName(iTM));

                    var panelToActivate = view.BottomInputPanels.SingleOrDefault((iTM) => panelNameToActivate == PanelBoss.GetPanelName(iTM));
                    if (panelToActivate != null)
                    {
                        if (ZIndex != null)
                        {
                            view.BottomInputPanelsArea.ZIndex = ZIndex.Value;
                        }

                        await BottomInputPanel_ActivateInternalAsync(panelToActivate, view);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BottomInputPanels_ActivatePanelByName");
                Debug.WriteLine(ex);
            }
        }
        public async Task BottomInputPanels_TogglePanelByName(string panelNameToToggle, Int32? ZIndex = null)
        {

            //Convenience method to close by string instead of method name.
            if (panelNameToToggle == "CloseAll")
            {
                await BottomInputPanels_CloseAllAsyncInternal();
                return;
            }


            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToToggle = view.BottomInputPanels.SingleOrDefault((iTM) => panelNameToToggle == PanelBoss.GetPanelName(iTM));

                if (panelToToggle != null)
                {
                    var wasVisible = PanelBoss.GetPanelIsVisible(panelToToggle);

                    if (ZIndex != null)
                    {
                        view.BottomInputPanelsArea.ZIndex = ZIndex.Value;
                    }

                    if (wasVisible)
                    {
                        await BottomInputPanels_DeActivatePanelByName(panelNameToToggle);
                    }
                    else
                    {
                        await BottomInputPanels_ActivatePanelByName(panelNameToToggle);
                    }
                }
            }
        }

        public async Task BottomInputPanels_ActivatePanel(View panelToActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await BottomInputPanel_ActivateInternalAsync(panelToActivate, view);
            }
        }

        public async Task BottomInputPanels_DeActivatePanelByName(string panelNameToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var panelToDeActivate = view.BottomInputPanels.SingleOrDefault((iTM) => panelNameToDeActivate == PanelBoss.GetPanelName(iTM));
                await BottomInputPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }

        public async Task BottomInputPanels_DeActivatePanel(View panelToDeActivate)
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                await BottomInputPanel_DeActivateInternalAsync(panelToDeActivate, view);
            }
        }


        public async Task BottomInputPanels_OpenTheHighestPriorityPanel()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                view.BottomInputPanels.SortAndFilterDelegate = (RichObservableCollection<View> unsortedItems) =>
                {
                    var retVal = from iTM in unsortedItems
                                 where iTM.IsEnabled == true
                                 select iTM;
                    return new RichObservableCollection<View>(retVal);
                };

                var activeLeftPanel = view.BottomInputPanels.SortedAndFilteredView.Reverse().FirstOrDefault();
                if (activeLeftPanel != null)
                {
                    // Ensure any UI updates are dispatched on the UI thread.
                    await view.Dispatcher.DispatchAsync(() =>
                    {
                        PanelBoss.SetPanelIsVisible(activeLeftPanel, true);
                    });

                    var theResetOfThem = view.BottomInputPanels.Except(view.BottomInputPanels.TakeLast(1)).ToList();
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

        public async Task RestoreDefaultPanelChromeAsync(params string[] extensionPanelNames)
        {
            await MenuPanels_CloseAllAsync();
            await FullScreenPopupPanels_CloseAllAsync();
            var restoredContentPanel = await ContentPanels_RestoreDefaultPanelAsync(extensionPanelNames);
            if (restoredContentPanel)
            {
                await TopHeaderPanels_OpenTheHighestPriorityPanel();
            }

            await BottomInputPanels_RestoreDefaultPanelAsync(extensionPanelNames);
        }

        public async Task BottomInputPanels_RestoreDefaultPanelAsync(params string[] extensionPanelNames)
        {
            var view = GetPanelBossStandardView();
            if (view == null)
                return;

            var extensionNames = new HashSet<string>(
                extensionPanelNames ?? [],
                StringComparer.OrdinalIgnoreCase);

            var defaultPanel = view.BottomInputPanels
                .Where(panel => !extensionNames.Contains(PanelBoss.GetPanelName(panel)))
                .OrderByDescending(PanelBoss.GetPanelPriority)
                .FirstOrDefault();

            if (defaultPanel == null)
            {
                await BottomInputPanels_CloseAllAsyncInternal();
                return;
            }

            await view.Dispatcher.DispatchAsync(() =>
            {
                foreach (var panel in view.BottomInputPanels)
                {
                    var isDefaultPanel = panel == defaultPanel;
                    var panelName = PanelBoss.GetPanelName(panel);

                    if (!isDefaultPanel && extensionNames.Contains(panelName))
                    {
                        PanelBoss.SetPanelPriority(panel, 0);
                    }

                    if (PanelBoss.GetPanelIsVisible(panel) != isDefaultPanel)
                    {
                        PanelBoss.SetPanelIsVisible(panel, isDefaultPanel);
                    }
                }
            });
        }

        public async Task BottomInputPanels_CloseAllAsync()
        {
            await BottomInputPanels_CloseAllAsyncInternal();

        }

        private async Task BottomInputPanels_CloseAllAsyncInternal()
        {
            var view = GetPanelBossStandardView();
            if (view != null)
            {
                var theseSelectorPanels = view.BottomInputPanels;

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
