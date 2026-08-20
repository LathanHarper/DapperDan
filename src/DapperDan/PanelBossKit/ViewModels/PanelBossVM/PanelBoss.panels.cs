using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;
using CodeCrafty.DapperDan.Semantics;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;







namespace CodeCrafty.DapperDan.PanelBossKit
{
    //Panels
    public partial class PanelBoss : BindableBase
    {
        private double BottomSafeHeight;
        public double BottomSafeAreaHeight
        {
            get => BottomSafeHeight;
            set => SetProperty(ref BottomSafeHeight, value);
        }
        private double _bottomSafeAreaHeight_InputPanelsFactor;
        public double BottomSafeAreaHeight_InputPanelsFactor
        {
            get => _bottomSafeAreaHeight_InputPanelsFactor;
            set => SetProperty(ref _bottomSafeAreaHeight_InputPanelsFactor, value, nameof(BottomSafeAreaHeight_InputPanelsFactor));
        }

        private double _bottomSafeAreaHeight_StatusPanelsFactor;
        public double BottomSafeAreaHeight_StatusPanelsFactor
        {
            get => _bottomSafeAreaHeight_StatusPanelsFactor;
            set => SetProperty(ref _bottomSafeAreaHeight_StatusPanelsFactor, value, nameof(BottomSafeAreaHeight_StatusPanelsFactor));
        }

        private double _viewportBottomInset;
        public double ViewportBottomInset
        {
            get => _viewportBottomInset;
            private set => SetProperty(ref _viewportBottomInset, value, nameof(ViewportBottomInset));
        }

        private double _platformBottomClearance = PanelMetrics.BottomDrawerClearance;
        public double PlatformBottomClearance
        {
            get => _platformBottomClearance;
            private set => SetProperty(ref _platformBottomClearance, value, nameof(PlatformBottomClearance));
        }


        private RichObservableCollection<View> _TopHeaderPagePanels;
        public RichObservableCollection<View> TopHeaderPagePanels { get => _TopHeaderPagePanels; set => SetProperty(ref _TopHeaderPagePanels, value); }

        private RichObservableCollection<View> _TopHeaderPanels;
        public RichObservableCollection<View> TopHeaderPanels { get => _TopHeaderPanels; set => SetProperty(ref _TopHeaderPanels, value); }

        private RichObservableCollection<View> _BottomStatusPanels;
        public RichObservableCollection<View> BottomStatusPanels { get => _BottomStatusPanels; set => SetProperty(ref _BottomStatusPanels, value); }

        private RichObservableCollection<View> _LeftSelectorPanels;
        public RichObservableCollection<View> LeftSelectorPanels { get => _LeftSelectorPanels; set => SetProperty(ref _LeftSelectorPanels, value); }

        private RichObservableCollection<View> _RightSelectorPanels;
        public RichObservableCollection<View> RightSelectorPanels { get => _RightSelectorPanels; set => SetProperty(ref _RightSelectorPanels, value); }

        private RichObservableCollection<View> _BottomInputPanels;
        public RichObservableCollection<View> BottomInputPanels { get => _BottomInputPanels; set => SetProperty(ref _BottomInputPanels, value); }

        private RichObservableCollection<View> _FullScreenPopupPanels;
        public RichObservableCollection<View> FullScreenPopupPanels { get => _FullScreenPopupPanels; set => SetProperty(ref _FullScreenPopupPanels, value); }

        private RichObservableCollection<View> _MenuPanels;
        public RichObservableCollection<View> MenuPanels { get => _MenuPanels; set => SetProperty(ref _MenuPanels, value); }

        private RichObservableCollection<View> _ContentPanels;
        public RichObservableCollection<View> ContentPanels { get => _ContentPanels; set => SetProperty(ref _ContentPanels, value); }

        // make this an interface so we can use it with different panels
        private WeakReference<IPanelBoss_View> panelBossStandardViewReference;

        public void SetPanelBossStandardView(IPanelBoss_View view)
        {
            panelBossStandardViewReference = new WeakReference<IPanelBoss_View>(view);
            ApplyViewportBottomInsetToView(view, ViewportBottomInset);
            ApplyPlatformBottomClearanceToView(view, PlatformBottomClearance);
        }

        public IPanelBoss_View GetPanelBossStandardView()
        {
            if (panelBossStandardViewReference != null &&
                panelBossStandardViewReference.TryGetTarget(out IPanelBoss_View view))
            {

                if (view == null)
                {
                    throw new Exception("Lost the PanelBoss's View reference");
                }
                return view;
            }
            return null;
        }

        public void SetViewportBottomInset(double bottomInset)
        {
            var sanitizedInset = SanitizeViewportBottomInset(bottomInset);

            if (Math.Abs(ViewportBottomInset - sanitizedInset) < 0.5)
            {
                return;
            }

            ViewportBottomInset = sanitizedInset;
            ApplyViewportBottomInsetToView(GetPanelBossStandardView(), sanitizedInset);
        }

        public void SetPlatformBottomClearance(double bottomClearance)
        {
            var sanitizedClearance = SanitizeViewportBottomInset(bottomClearance);

            if (Math.Abs(PlatformBottomClearance - sanitizedClearance) < 0.5)
            {
                return;
            }

            PlatformBottomClearance = sanitizedClearance;
            ApplyPlatformBottomClearanceToView(GetPanelBossStandardView(), sanitizedClearance);
        }

        private static double SanitizeViewportBottomInset(double bottomInset)
            => double.IsNaN(bottomInset) || double.IsInfinity(bottomInset) || bottomInset < 0
                ? 0
                : bottomInset;

        private static void ApplyViewportBottomInsetToView(IPanelBoss_View view, double bottomInset)
        {
            if (view is null)
            {
                return;
            }

            var sanitizedInset = SanitizeViewportBottomInset(bottomInset);
            view.Dispatcher.Dispatch(() => view.ApplyViewportBottomInset(sanitizedInset));
        }

        private static void ApplyPlatformBottomClearanceToView(IPanelBoss_View view, double bottomClearance)
        {
            if (view is null)
            {
                return;
            }

            var sanitizedClearance = SanitizeViewportBottomInset(bottomClearance);
            view.Dispatcher.Dispatch(() => view.ApplyPlatformBottomClearance(sanitizedClearance));
        }

        public bool PanelIsVisibleByName(string panelName)
        {

            var retVal = false;
            retVal =
             (from iTM in TopHeaderPagePanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in TopHeaderPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in BottomStatusPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in LeftSelectorPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in RightSelectorPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in BottomInputPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in FullScreenPopupPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in MenuPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any() ||
             (from iTM in ContentPanels where PanelBoss.GetPanelIsVisible(iTM) select iTM).Any();

            return retVal;
        }

        public void FilterPanelsByIdiom(RichObservableCollection<View> panelCollection, DeviceIdiom idiom)
        {
            var idiomString = idiom.ToString(); // "Phone", "Tablet", etc.
            var keepers = panelCollection
                .Where(panel =>
                {
                    var name = panel.GetType().Name;
                    if (name.Contains("Phone") && idiomString != "Phone")
                        return false;
                    if (name.Contains("Tablet") && idiomString != "Tablet")
                        return false;
                    return true;
                })
                .ToList();

            panelCollection.Clear();
            foreach (var panel in keepers)
                panelCollection.Add(panel);
        }
    }


}
