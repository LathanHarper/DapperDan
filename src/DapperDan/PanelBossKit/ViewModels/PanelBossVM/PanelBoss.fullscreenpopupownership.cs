using Microsoft.Maui.Controls;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCrafty.DapperDan.PanelBossKit
{
    public sealed class PanelBossFullScreenPopupPanelHandle
    {
        // Visual threshold, not a time guess: the Android frame fence runs only after the fade is materially visible.
        private const double MinimumPresentedOpacity = 0.5;
        private static readonly TimeSpan PresentationFailureTimeout = TimeSpan.FromSeconds(2);

        internal PanelBossFullScreenPopupPanelHandle(IPanelBoss_View owner, View panel)
        {
            Owner = owner;
            Panel = panel;
        }

        internal IPanelBoss_View Owner { get; }
        internal View Panel { get; }

        public bool IsRequestedVisible => PanelBoss.GetPanelIsVisible(Panel);
        public bool IsActuallyVisible => Panel.IsVisible;

        public async Task<bool> WaitForPresentationAsync()
        {
            if (HasPresentedOpacity())
            {
                return true;
            }

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void OnPanelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
            {
                if (HasPresentedOpacity())
                {
                    completion.TrySetResult(true);
                }
                else if (!IsRequestedVisible || !Panel.IsVisible)
                {
                    completion.TrySetResult(false);
                }
            }

            Panel.PropertyChanged += OnPanelPropertyChanged;
            try
            {
                if (HasPresentedOpacity())
                {
                    return true;
                }

                if (!IsRequestedVisible || !Panel.IsVisible)
                {
                    return false;
                }

                return await completion.Task.WaitAsync(PresentationFailureTimeout);
            }
            catch (TimeoutException)
            {
                return false;
            }
            finally
            {
                Panel.PropertyChanged -= OnPanelPropertyChanged;
            }
        }

        private bool HasPresentedOpacity() =>
            IsRequestedVisible &&
            Panel.IsVisible &&
            Panel.Opacity >= MinimumPresentedOpacity;
    }

    public partial class PanelBoss
    {
        public PanelBossFullScreenPopupPanelHandle FullScreenPopupPanels_CapturePanelByName(string panelName)
        {
            var owner = GetPanelBossStandardView();
            var panel = owner?.FullScreenPopupPanels?
                .SingleOrDefault(item => panelName == GetPanelName(item));

            return panel == null
                ? null
                : new PanelBossFullScreenPopupPanelHandle(owner, panel);
        }

        public async Task<PanelBossFullScreenPopupPanelHandle> FullScreenPopupPanels_ActivateOwnedPanelByName(
            string panelName,
            int? zIndex = null)
        {
            var handle = FullScreenPopupPanels_CapturePanelByName(panelName);
            if (handle == null)
            {
                return null;
            }

            if (zIndex.HasValue)
            {
                handle.Owner.FullScreenPopupPanelsArea.ZIndex = zIndex.Value;
            }

            await FullScreenPopupPanel_ActivateInternalAsync(handle.Panel, handle.Owner);
            return handle;
        }

        public async Task<bool> FullScreenPopupPanels_DeActivateOwnedPanelAsync(
            PanelBossFullScreenPopupPanelHandle handle)
        {
            if (handle?.Owner?.FullScreenPopupPanels?.Contains(handle.Panel) != true)
            {
                return false;
            }

            await FullScreenPopupPanel_DeActivateInternalAsync(handle.Panel, handle.Owner);
            return true;
        }

        public async Task<bool> FullScreenPopupPanels_HideOwnedPanelImmediatelyAsync(
            PanelBossFullScreenPopupPanelHandle handle)
        {
            if (handle?.Owner?.FullScreenPopupPanels?.Contains(handle.Panel) != true)
            {
                return false;
            }

            await handle.Owner.Dispatcher.DispatchAsync(() =>
            {
                SetPanelPriority(handle.Panel, 0);
                SetPanelIsVisible(handle.Panel, false);
                handle.Panel.IsVisible = false;
            });

            return true;
        }
    }
}
