using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;
using CodeCrafty.DapperDan.PanelBossKit.Helpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CodeCrafty.DapperDan.PanelBossKit
{
    public interface IPanelBoss_View
    {
        PanelBoss PanelBossInstance { get; set; }
        Grid BottomInputPanelsArea { get; set; }
        RichObservableCollection<View> BottomInputPanels { get; set; }
        Grid BottomStatusPanelsArea { get; set; }
        RichObservableCollection<View> BottomStatusPanels { get; set; }
        Grid LeftSelectorPanelsArea { get; set; }
        RichObservableCollection<View> LeftSelectorPanels { get; set; }
        Grid RightSelectorPanelsArea { get; set; }
        RichObservableCollection<View> RightSelectorPanels { get; set; }
        Grid TopHeaderPanelsArea { get; set; }
        RichObservableCollection<View> TopHeaderPanels { get; set; }
        Grid TopHeaderPagePanelsArea { get; set; }
        RichObservableCollection<View> TopHeaderPagePanels { get; set; }
        Grid NonVisualControlsArea { get; set; }
        RichObservableCollection<View> NonVisualControls { get; set; }
        Grid FullScreenPopupPanelsArea { get; set; }
        RichObservableCollection<View> FullScreenPopupPanels { get; set; }
        Grid MenuPanelsArea { get; set; }
        RichObservableCollection<View> MenuPanels { get; set; }
        Grid ContentPanelsArea { get; set; }
        RichObservableCollection<View> ContentPanels { get; set; }
        LayoutOptions VerticalOptionsBinding { get; set; }
        IDispatcher Dispatcher { get; }
        void ApplyViewportBottomInset(double bottomInset);
        void ApplyPlatformBottomClearance(double bottomClearance);
    }
    //Constructor
    public partial class PanelBoss : BindableBase
    {
        public Guid guidID = Guid.NewGuid();

        public PanelBoss()
        {

        }

        // 🏄‍♂️ SmokyLayerAnimatedValue: Prism property for the animated brush
        private SolidColorBrush _smokyLayerAnimatedValue = new SolidColorBrush(Color.FromArgb("#00333333"));
        public SolidColorBrush SmokyLayerAnimatedValue
        {
            get => _smokyLayerAnimatedValue;
            set => SetProperty(ref _smokyLayerAnimatedValue, value);
        }


    }

    internal interface ISupportPanelBoss_Core
    {
        PanelBoss ActivePanelBoss { get; }
        IDispatcher Dispatcher { get; }
    }
    internal interface ISupportPanelBoss_All
    {

        PanelBoss ActivePanelBoss { get; }
        IDispatcher Dispatcher { get; }
        RichObservableCollection<View> BottomInputPanels { get; set; }
        RichObservableCollection<View> BottomStatusPanels { get; set; }
        RichObservableCollection<View> LeftSelectorPanels { get; set; }
        RichObservableCollection<View> RightSelectorPanels { get; set; }
        RichObservableCollection<View> TopHeaderPanels { get; set; }
        RichObservableCollection<View> TopHeaderPagePanels { get; set; }
        RichObservableCollection<View> FullScreenPopupPanels { get; set; }
        RichObservableCollection<View> MenuPanels { get; set; }
        RichObservableCollection<View> ContentPanels { get; set; }
    }

}
