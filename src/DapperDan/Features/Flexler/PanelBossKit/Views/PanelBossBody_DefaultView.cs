using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Flexler.PanelBossKit.Views;

public sealed class PanelBossBody_DefaultView : Grid
{
    public static readonly BindableProperty PanelBossInstanceProperty = BindableProperty.Create(
        nameof(PanelBossInstance),
        typeof(PanelBoss),
        typeof(PanelBossBody_DefaultView),
        null,
        propertyChanged: OnPanelBossInstanceChanged);

    public static readonly BindableProperty UseSidePanelLayoutProperty = BindableProperty.Create(
        nameof(UseSidePanelLayout),
        typeof(bool),
        typeof(PanelBossBody_DefaultView),
        false,
        propertyChanged: OnUseSidePanelLayoutChanged);

    private readonly Grid _bottomInputPanelsArea;
    private readonly Grid _contentPanelsArea;
    private readonly Grid _leftSelectorPanelsArea;
    private readonly Grid _rightSelectorPanelsArea;
    private readonly Grid _topHeaderPanelsArea;

    public PanelBossBody_DefaultView()
    {
        SafeAreaEdges = SafeAreaEdges.None;
        _contentPanelsArea = CreateArea(LayoutOptions.Fill, LayoutOptions.Fill, 1);
        _topHeaderPanelsArea = CreateArea(LayoutOptions.Fill, LayoutOptions.Start, 20);
        _bottomInputPanelsArea = CreateArea(LayoutOptions.Fill, LayoutOptions.End, 30);
        _leftSelectorPanelsArea = CreateArea(LayoutOptions.Start, LayoutOptions.Fill, 20);
        _rightSelectorPanelsArea = CreateArea(LayoutOptions.End, LayoutOptions.Fill, 30);

        // Chrome is measured for its content; only the workspace takes the
        // available viewport. A default star row would stretch an auto-sized
        // drawer over the entire page before its clearance can follow it.
        _topHeaderPanelsArea.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _bottomInputPanelsArea.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _leftSelectorPanelsArea.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _rightSelectorPanelsArea.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Children.Add(_contentPanelsArea);
        Children.Add(_topHeaderPanelsArea);
        Children.Add(_bottomInputPanelsArea);
        Children.Add(_leftSelectorPanelsArea);
        Children.Add(_rightSelectorPanelsArea);

        ContentPanels.CollectionChanged += OnContentPanelsChanged;
        TopHeaderPanels.CollectionChanged += OnTopHeaderPanelsChanged;
        BottomInputPanels.CollectionChanged += OnBottomInputPanelsChanged;
        LeftSelectorPanels.CollectionChanged += OnLeftSelectorPanelsChanged;
        RightSelectorPanels.CollectionChanged += OnRightSelectorPanelsChanged;

        ApplyPanelAreaLayout();
    }

    public ObservableCollection<View> ContentPanels { get; } = [];

    public ObservableCollection<View> TopHeaderPanels { get; } = [];

    public ObservableCollection<View> BottomInputPanels { get; } = [];

    public ObservableCollection<View> LeftSelectorPanels { get; } = [];

    public ObservableCollection<View> RightSelectorPanels { get; } = [];

    public PanelBoss? PanelBossInstance
    {
        get => (PanelBoss?)GetValue(PanelBossInstanceProperty);
        set => SetValue(PanelBossInstanceProperty, value);
    }

    public bool UseSidePanelLayout
    {
        get => (bool)GetValue(UseSidePanelLayoutProperty);
        set => SetValue(UseSidePanelLayoutProperty, value);
    }

    private static Grid CreateArea(
        LayoutOptions horizontalOptions,
        LayoutOptions verticalOptions,
        int zIndex) =>
        new()
        {
            BackgroundColor = Colors.Transparent,
            CascadeInputTransparent = false,
            HorizontalOptions = horizontalOptions,
            InputTransparent = false,
            SafeAreaEdges = SafeAreaEdges.None,
            VerticalOptions = verticalOptions,
            ZIndex = zIndex
        };

    private static void OnPanelBossInstanceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PanelBossBody_DefaultView host && newValue is PanelBoss panelBoss)
        {
            panelBoss.Attach(host);
            panelBoss.SetUseSidePanelLayout(host.UseSidePanelLayout);
        }
    }

    private static void OnUseSidePanelLayoutChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is not PanelBossBody_DefaultView host || newValue is not bool useSidePanels)
        {
            return;
        }

        host.PanelBossInstance?.SetUseSidePanelLayout(useSidePanels);
        host.ApplyPanelAreaLayout();
    }

    private void OnContentPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SynchronizeArea(_contentPanelsArea, ContentPanels);

    private void OnTopHeaderPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SynchronizeArea(_topHeaderPanelsArea, TopHeaderPanels);

    private void OnBottomInputPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SynchronizeArea(_bottomInputPanelsArea, BottomInputPanels);

    private void OnLeftSelectorPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SynchronizeArea(_leftSelectorPanelsArea, LeftSelectorPanels);

    private void OnRightSelectorPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        SynchronizeArea(_rightSelectorPanelsArea, RightSelectorPanels);

    private void ApplyPanelAreaLayout()
    {
        _topHeaderPanelsArea.IsVisible = !UseSidePanelLayout;
        _topHeaderPanelsArea.InputTransparent = UseSidePanelLayout;
        _bottomInputPanelsArea.IsVisible = !UseSidePanelLayout;
        _bottomInputPanelsArea.InputTransparent = UseSidePanelLayout;

        _leftSelectorPanelsArea.IsVisible = UseSidePanelLayout;
        _leftSelectorPanelsArea.InputTransparent = !UseSidePanelLayout;
        _rightSelectorPanelsArea.IsVisible = UseSidePanelLayout;
        _rightSelectorPanelsArea.InputTransparent = !UseSidePanelLayout;
    }

    private static void SynchronizeArea(Grid area, IEnumerable<View> panels)
    {
        area.Children.Clear();

        foreach (var panel in panels)
        {
            panel.IsVisible = PanelBoss.GetPanelIsVisible(panel);
            panel.InputTransparent = !panel.IsVisible;
            area.Children.Add(panel);
        }
    }
}
