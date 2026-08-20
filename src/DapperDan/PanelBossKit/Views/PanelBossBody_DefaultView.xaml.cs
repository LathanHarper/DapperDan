using System.Collections.Specialized;
using Microsoft.Maui.Devices;
using CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers;
using CodeCrafty.DapperDan.Semantics;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

/// <summary>
/// Default portable PanelBoss body.
/// Grid-based view implementing IPanelBoss_View with all 9 zone slots.
/// Pages use this as their primary content layout - chrome is tattooed on,
/// panels that need orchestration go into zone collections.
/// </summary>
public partial class PanelBossBody_DefaultView : Grid, IPanelBoss_View
{
    private readonly Dictionary<RowDefinition, GridLength> _panelClearanceHeights = [];
    public double SavedBottomSafeAreaHeight_InputPanelsFactor { get; set; }
    public double SavedBottomSafeAreaHeight_StatusPanelsFactor { get; set; }
    private Thickness _basePanelBossBodyPadding;
    private bool _hasCapturedBodyBackdropColor;
    private bool _hasCapturedPanelBossBodyPadding;
    private bool _isStatusBarDefaultViewEnabled;
    private double _lastPlatformBottomClearance = PanelMetrics.BottomDrawerClearance;
    private double _lastPlatformViewportBottomInset;
    private Page _registeredAppearingPage;

    public PanelBossBody_DefaultView()
    {
        InitializeComponent();
        InitializePlatformViewportInsetContract();
        ApplyPanelAreaSafeAreaContract();
    }

    public void ApplyStatusBarDefaultView(Color statusBarColor)
    {
        _isStatusBarDefaultViewEnabled = true;

        if (!_hasCapturedBodyBackdropColor)
        {
            BodyBackdropArea.BackgroundColor = BackgroundColor ?? Colors.Transparent;
            _hasCapturedBodyBackdropColor = true;
        }

        BackgroundColor = statusBarColor;
        LandscapeStatusStripeView.Color = statusBarColor;

        SafeAreaEdges = new SafeAreaEdges(
            left: SafeAreaRegions.None,
            top: SafeAreaRegions.Container,
            right: SafeAreaRegions.None,
            bottom: SafeAreaRegions.None);

        UpdateLandscapeStatusStripeVisibility(Width, Height);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        UpdateLandscapeStatusStripeVisibility(width, height);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        ApplyHostSafeAreaContract();
        RegisterHostPageAppearing();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ApplyHostSafeAreaContract();
        RegisterHostPageAppearing();
        ApplyPlatformViewportInsetContract();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        ClearPlatformViewportInsetContract();
        base.OnHandlerChanging(args);
    }

    private void RegisterHostPageAppearing()
    {
        var hostPage = FindHostPage();

        if (ReferenceEquals(_registeredAppearingPage, hostPage))
        {
            return;
        }

        if (_registeredAppearingPage is not null)
        {
            _registeredAppearingPage.Appearing -= OnHostPageAppearing;
        }

        _registeredAppearingPage = hostPage;

        if (_registeredAppearingPage is not null)
        {
            _registeredAppearingPage.Appearing += OnHostPageAppearing;
        }
    }

    private Page FindHostPage()
    {
        Element current = this;

        while (current is not null)
        {
            if (current is Page page)
            {
                return page;
            }

            current = current.Parent;
        }

        return null;
    }

    private void OnHostPageAppearing(object sender, EventArgs e)
    {
        ApplyHostSafeAreaContract();
        RegisterAsPanelBossStandardView();
    }

    private void ApplyHostSafeAreaContract()
    {
        if (FindHostPage() is not ContentPage hostPage)
        {
            return;
        }

        hostPage.SafeAreaEdges = SafeAreaEdges.None;
    }

    private void ApplyPanelAreaSafeAreaContract()
    {
        SafeAreaEdges = SafeAreaEdges.None;
        BodyBackdropArea.SafeAreaEdges = SafeAreaEdges.None;
        NonVisualControlsArea.SafeAreaEdges = SafeAreaEdges.None;
        TopHeaderPagePanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        TopHeaderPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        LeftSelectorPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        RightSelectorPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        MenuPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        BottomInputPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        BottomStatusPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        FullScreenPopupPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
        ContentPanelsArea.SafeAreaEdges = SafeAreaEdges.None;
    }

    private void RegisterAsPanelBossStandardView()
    {
        PanelBossInstance?.SetPanelBossStandardView(this);
        RefreshPanelClearances();
    }

    internal static void RefreshPanelClearancesFor(View panel)
    {
        Element current = panel;

        while (current is not null)
        {
            if (current is PanelBossBody_DefaultView panelBossView)
            {
                panelBossView.RefreshPanelClearances();
                return;
            }

            current = current.Parent;
        }
    }

    private void RefreshPanelClearances()
    {
        foreach (var contentGrid in ContentPanels.OfType<Grid>())
        {
            foreach (var rowDefinition in contentGrid.RowDefinitions)
            {
                var panelName = PanelBoss.GetPanelClearanceFor(rowDefinition);

                if (string.IsNullOrWhiteSpace(panelName))
                {
                    continue;
                }

                if (!_panelClearanceHeights.TryGetValue(rowDefinition, out var openHeight))
                {
                    openHeight = rowDefinition.Height;
                    _panelClearanceHeights[rowDefinition] = openHeight;
                }

                var panel = FindPanel(panelName);
                rowDefinition.Height = panel is not null && PanelBoss.GetPanelIsVisible(panel)
                    ? openHeight
                    : new GridLength(0);
            }
        }

        InvalidateMeasure();
    }

    private View FindPanel(string panelName)
    {
        return EnumeratePanels().FirstOrDefault(panel =>
            string.Equals(PanelBoss.GetPanelName(panel), panelName, StringComparison.Ordinal));
    }

    private IEnumerable<View> EnumeratePanels()
    {
        return NonVisualControls
            .Concat(TopHeaderPagePanels)
            .Concat(TopHeaderPanels)
            .Concat(BottomStatusPanels)
            .Concat(LeftSelectorPanels)
            .Concat(RightSelectorPanels)
            .Concat(BottomInputPanels)
            .Concat(FullScreenPopupPanels)
            .Concat(MenuPanels)
            .Concat(ContentPanels);
    }

    partial void InitializePlatformViewportInsetContract();
    partial void ApplyPlatformViewportInsetContract();
    partial void ClearPlatformViewportInsetContract();

    internal void ReportPlatformViewportBottomInset(double bottomInset)
    {
        var sanitizedInset = SanitizeViewportBottomInset(bottomInset);
        _lastPlatformViewportBottomInset = sanitizedInset;
        PanelBossInstance?.SetViewportBottomInset(sanitizedInset);
    }

    internal void ReportPlatformBottomClearance(double bottomClearance)
    {
        var sanitizedClearance = SanitizeViewportBottomInset(bottomClearance);
        _lastPlatformBottomClearance = sanitizedClearance;
        PanelBossInstance?.SetPlatformBottomClearance(sanitizedClearance);
    }

    public void ApplyViewportBottomInset(double bottomInset)
    {
        if (!_hasCapturedPanelBossBodyPadding)
        {
            _basePanelBossBodyPadding = Padding;
            _hasCapturedPanelBossBodyPadding = true;
        }

        var sanitizedInset = SanitizeViewportBottomInset(bottomInset);
        Padding = new Thickness(
            _basePanelBossBodyPadding.Left,
            _basePanelBossBodyPadding.Top,
            _basePanelBossBodyPadding.Right,
            _basePanelBossBodyPadding.Bottom + sanitizedInset);

        InvalidateMeasure();
    }

    public void ApplyPlatformBottomClearance(double bottomClearance)
    {
        var sanitizedClearance = SanitizeViewportBottomInset(bottomClearance);

        if (BottomInputPanelsArea.RowDefinitions.Count > 1)
        {
            BottomInputPanelsArea.RowDefinitions[1].Height = new GridLength(sanitizedClearance);
        }

        BottomInputPanelsPlatformClearance.HeightRequest = sanitizedClearance;
        BottomInputPanelsArea.InvalidateMeasure();
    }

    private static double SanitizeViewportBottomInset(double bottomInset)
        => double.IsNaN(bottomInset) || double.IsInfinity(bottomInset) || bottomInset < 0
            ? 0
            : bottomInset;

    private void UpdateLandscapeStatusStripeVisibility(double width, double height)
    {
        LandscapeStatusStripeView.IsVisible =
            _isStatusBarDefaultViewEnabled &&
            DeviceInfo.Current.Platform == DevicePlatform.iOS &&
            width > height &&
            width > 0 &&
            height > 0;
    }

    #region VerticalOptionsBinding

    public static readonly BindableProperty VerticalOptionsBindingProperty =
        BindableProperty.Create(
            nameof(VerticalOptionsBinding),
            typeof(LayoutOptions),
            typeof(PanelBossBody_DefaultView),
            LayoutOptions.Fill,
            propertyChanged: OnVerticalOptionsChanged);

    public LayoutOptions VerticalOptionsBinding
    {
        get => (LayoutOptions)GetValue(VerticalOptionsBindingProperty);
        set => SetValue(VerticalOptionsBindingProperty, value);
    }

    private static void OnVerticalOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PanelBossBody_DefaultView view && newValue is LayoutOptions newOptions)
            view.BottomInputPanelsArea.VerticalOptions = newOptions;
    }

    #endregion

    #region Zone Collection BindableProperties

    public static readonly BindableProperty NonVisualControlsProperty = BindableProperty.Create(nameof(NonVisualControls), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnNonVisualControlsPropertyChanged);
    public static readonly BindableProperty TopHeaderPagePanelsProperty = BindableProperty.Create(nameof(TopHeaderPagePanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnTopHeaderPagePanelsPropertyChanged);
    public static readonly BindableProperty TopHeaderPanelsProperty = BindableProperty.Create(nameof(TopHeaderPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnTopHeaderPanelsPropertyChanged);
    public static readonly BindableProperty BottomStatusPanelsProperty = BindableProperty.Create(nameof(BottomStatusPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnBottomStatusPanelsPropertyChanged);
    public static readonly BindableProperty LeftSelectorPanelsProperty = BindableProperty.Create(nameof(LeftSelectorPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnLeftSelectorPanelsPropertyChanged);
    public static readonly BindableProperty RightSelectorPanelsProperty = BindableProperty.Create(nameof(RightSelectorPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnRightSelectorPanelsPropertyChanged);
    public static readonly BindableProperty BottomInputPanelsProperty = BindableProperty.Create(nameof(BottomInputPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnBottomInputPanelsPropertyChanged);
    public static readonly BindableProperty FullScreenPopupPanelsProperty = BindableProperty.Create(nameof(FullScreenPopupPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnFullScreenPopupPanelsPropertyChanged);
    public static readonly BindableProperty MenuPanelsProperty = BindableProperty.Create(nameof(MenuPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnMenuPanelsPropertyChanged);
    public static readonly BindableProperty ContentPanelsProperty = BindableProperty.Create(nameof(ContentPanels), typeof(RichObservableCollection<View>), typeof(PanelBossBody_DefaultView), defaultValueCreator: _ => new RichObservableCollection<View>(), propertyChanged: OnContentPanelsPropertyChanged);

    public RichObservableCollection<View> NonVisualControls { get => (RichObservableCollection<View>)GetValue(NonVisualControlsProperty); set => SetValue(NonVisualControlsProperty, value); }
    public RichObservableCollection<View> TopHeaderPagePanels { get => (RichObservableCollection<View>)GetValue(TopHeaderPagePanelsProperty); set => SetValue(TopHeaderPagePanelsProperty, value); }
    public RichObservableCollection<View> TopHeaderPanels { get => (RichObservableCollection<View>)GetValue(TopHeaderPanelsProperty); set => SetValue(TopHeaderPanelsProperty, value); }
    public RichObservableCollection<View> BottomStatusPanels { get => (RichObservableCollection<View>)GetValue(BottomStatusPanelsProperty); set => SetValue(BottomStatusPanelsProperty, value); }
    public RichObservableCollection<View> LeftSelectorPanels { get => (RichObservableCollection<View>)GetValue(LeftSelectorPanelsProperty); set => SetValue(LeftSelectorPanelsProperty, value); }
    public RichObservableCollection<View> RightSelectorPanels { get => (RichObservableCollection<View>)GetValue(RightSelectorPanelsProperty); set => SetValue(RightSelectorPanelsProperty, value); }
    public RichObservableCollection<View> BottomInputPanels { get => (RichObservableCollection<View>)GetValue(BottomInputPanelsProperty); set => SetValue(BottomInputPanelsProperty, value); }
    public RichObservableCollection<View> FullScreenPopupPanels { get => (RichObservableCollection<View>)GetValue(FullScreenPopupPanelsProperty); set => SetValue(FullScreenPopupPanelsProperty, value); }
    public RichObservableCollection<View> MenuPanels { get => (RichObservableCollection<View>)GetValue(MenuPanelsProperty); set => SetValue(MenuPanelsProperty, value); }
    public RichObservableCollection<View> ContentPanels { get => (RichObservableCollection<View>)GetValue(ContentPanelsProperty); set => SetValue(ContentPanelsProperty, value); }

    #endregion

    #region PanelBossInstance

    public static readonly BindableProperty PanelBossInstanceProperty =
        BindableProperty.Create(nameof(PanelBossInstance), typeof(PanelBoss), typeof(PanelBossBody_DefaultView), null, propertyChanged: OnPanelBossInstanceChanged);

    public PanelBoss PanelBossInstance
    {
        get => (PanelBoss)GetValue(PanelBossInstanceProperty);
        set => SetValue(PanelBossInstanceProperty, value);
    }

    private static void OnPanelBossInstanceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PanelBossBody_DefaultView control || newValue is not PanelBoss)
            return;

        control.RegisterAsPanelBossStandardView();
        control.PanelBossInstance?.SetViewportBottomInset(control._lastPlatformViewportBottomInset);
        control.PanelBossInstance?.SetPlatformBottomClearance(control._lastPlatformBottomClearance);

        // Flush any panels already in collections into their zone Grids
        foreach (var v in control.NonVisualControls) control.NonVisualControlsArea.Add(v);
        foreach (var v in control.TopHeaderPagePanels) control.TopHeaderPagePanelsArea.Add(v);
        foreach (var v in control.TopHeaderPanels) control.TopHeaderPanelsArea.Add(v);
        foreach (var v in control.BottomStatusPanels) control.BottomStatusPanelsArea.Add(v);
        foreach (var v in control.LeftSelectorPanels) control.LeftSelectorPanelsArea.Add(v);
        foreach (var v in control.RightSelectorPanels) control.RightSelectorPanelsArea.Add(v);
        foreach (var v in control.BottomInputPanels) control.BottomInputPanelsArea.Add(v);
        foreach (var v in control.FullScreenPopupPanels) control.FullScreenPopupPanelsArea.Add(v);
        foreach (var v in control.MenuPanels) control.MenuPanelsArea.Add(v);
        foreach (var v in control.ContentPanels) control.ContentPanelsArea.Add(v);
        control.RefreshPanelClearances();
    }

    #endregion

    #region DefaultInvisiblePanels

    public static readonly BindableProperty DefaultInvisiblePanelsProperty =
        BindableProperty.Create(
            nameof(DefaultInvisiblePanels),
            typeof(string),
            typeof(PanelBossBody_DefaultView),
            default(string),
            propertyChanged: OnDefaultInvisiblePanelsChanged);

    public string DefaultInvisiblePanels
    {
        get => (string)GetValue(DefaultInvisiblePanelsProperty);
        set => SetValue(DefaultInvisiblePanelsProperty, value);
    }

    private static void OnDefaultInvisiblePanelsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PanelBossBody_DefaultView view && view.PanelBossInstance is not null)
        {
            // Future: push CSV of panel names to hide by default
        }
    }

    #endregion

    #region Zone Collection PropertyChanged Handlers

    private static void OnNonVisualControlsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.NonVisualControls_CollectionChanged, c => c.NonVisualControlsArea); }
    private static void OnTopHeaderPagePanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.TopHeaderPagePanels_CollectionChanged, c => c.TopHeaderPagePanelsArea); }
    private static void OnTopHeaderPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.TopHeaderPanels_CollectionChanged, c => c.TopHeaderPanelsArea); }
    private static void OnBottomStatusPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.BottomStatusPanels_CollectionChanged, c => c.BottomStatusPanelsArea); }
    private static void OnLeftSelectorPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.LeftSelectorPanels_CollectionChanged, c => c.LeftSelectorPanelsArea); }
    private static void OnRightSelectorPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.RightSelectorPanels_CollectionChanged, c => c.RightSelectorPanelsArea); }
    private static void OnBottomInputPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.BottomInputPanels_CollectionChanged, c => c.BottomInputPanelsArea); }
    private static void OnFullScreenPopupPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.FullScreenPopupPanels_CollectionChanged, c => c.FullScreenPopupPanelsArea); }
    private static void OnMenuPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.MenuPanels_CollectionChanged, c => c.MenuPanelsArea); }
    private static void OnContentPanelsPropertyChanged(BindableObject bindable, object oldValue, object newValue) { WireZone<PanelBossBody_DefaultView>(bindable, oldValue, newValue, c => c.ContentPanels_CollectionChanged, c => c.ContentPanelsArea); }

    private static void WireZone<T>(
        BindableObject bindable,
        object oldValue,
        object newValue,
        Func<T, NotifyCollectionChangedEventHandler> handlerSelector,
        Func<T, Grid> areaSelector) where T : class
    {
        if (bindable is not T control) return;
        var handler = handlerSelector(control);

        if (oldValue is RichObservableCollection<View> oldCollection)
            oldCollection.CollectionChanged -= handler;

        if (newValue is RichObservableCollection<View> newCollection)
        {
            newCollection.CollectionChanged += handler;
            var area = areaSelector(control);
            foreach (var view in newCollection)
                area.Children.Add(view);
        }

        if (control is PanelBossBody_DefaultView panelBossView)
        {
            panelBossView.RefreshPanelClearances();
        }
    }

    #endregion

    #region CollectionChanged Handlers

    private void NonVisualControls_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, NonVisualControlsArea);
    private void TopHeaderPagePanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, TopHeaderPagePanelsArea);
    private void TopHeaderPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, TopHeaderPanelsArea);
    private void BottomStatusPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, BottomStatusPanelsArea);
    private void LeftSelectorPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, LeftSelectorPanelsArea);
    private void RightSelectorPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, RightSelectorPanelsArea);
    private void FullScreenPopupPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, FullScreenPopupPanelsArea);
    private void MenuPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, MenuPanelsArea);
    private void ContentPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => AddNewItemsToAreaAndRefreshPanelClearances(e, ContentPanelsArea);

    // BottomInput preserves Grid.Row/Column/Span properties
    private void BottomInputPanels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var view in e.NewItems.Cast<View>())
            {
                if (view is BindableObject bindable)
                {
                    var row = Grid.GetRow(bindable);
                    var column = Grid.GetColumn(bindable);
                    var rowSpan = Grid.GetRowSpan(bindable);
                    var columnSpan = Grid.GetColumnSpan(bindable);

                    BottomInputPanelsArea.Add(view);

                    Grid.SetRow(view, row);
                    Grid.SetColumn(view, column);
                    Grid.SetRowSpan(view, rowSpan);
                    Grid.SetColumnSpan(view, columnSpan);
                }
            }
        }

        RefreshPanelClearances();
    }

    private static void AddNewItemsToArea(NotifyCollectionChangedEventArgs e, Grid area)
    {
        if (e.NewItems is null) return;
        foreach (var view in e.NewItems.Cast<View>())
            area.Children.Add(view);
    }

    private void AddNewItemsToAreaAndRefreshPanelClearances(NotifyCollectionChangedEventArgs e, Grid area)
    {
        AddNewItemsToArea(e, area);
        RefreshPanelClearances();
    }

    #endregion

    #region Explicit IPanelBoss_View Grid Area Accessors

    Grid IPanelBoss_View.NonVisualControlsArea { get => NonVisualControlsArea; set { } }
    Grid IPanelBoss_View.TopHeaderPagePanelsArea { get => TopHeaderPagePanelsArea; set { } }
    Grid IPanelBoss_View.TopHeaderPanelsArea { get => TopHeaderPanelsArea; set { } }
    Grid IPanelBoss_View.BottomStatusPanelsArea { get => BottomStatusPanelsArea; set { } }
    Grid IPanelBoss_View.LeftSelectorPanelsArea { get => LeftSelectorPanelsArea; set { } }
    Grid IPanelBoss_View.RightSelectorPanelsArea { get => RightSelectorPanelsArea; set { } }
    Grid IPanelBoss_View.BottomInputPanelsArea { get => BottomInputPanelsArea; set { } }
    Grid IPanelBoss_View.FullScreenPopupPanelsArea { get => FullScreenPopupPanelsArea; set { } }
    Grid IPanelBoss_View.MenuPanelsArea { get => MenuPanelsArea; set { } }
    Grid IPanelBoss_View.ContentPanelsArea { get => ContentPanelsArea; set { } }

    #endregion
}
