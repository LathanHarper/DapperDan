namespace CodeCrafty.DapperDan.Controls;

/// <summary>
/// Deterministic native-input tap surface with locally-owned recursive
/// RichVisualState propagation.
/// </summary>
public class RichButton : TapViewBase, INativeTapView
{
    public static readonly BindableProperty CascadeRichVisualStatesProperty =
        BindableProperty.Create(
            nameof(CascadeRichVisualStates),
            typeof(bool),
            typeof(RichButton),
            true);

    private readonly NativePrimaryTapBridge _nativePrimaryTapBridge;

    public RichButton()
    {
        _nativePrimaryTapBridge = new NativePrimaryTapBridge(
            this,
            BeginNativeTouchSequence,
            CancelNativeTouchSequence,
            ActivateNativeTouchSequenceAsync,
            ReportNativeTouchDown);

        global::CodeCrafty.DapperDan.PanelBossKit.PanelBoss.SetTouchPointBloom(this, true);
    }

    public event EventHandler NativeTouchDown;

    public bool CascadeRichVisualStates
    {
        get => (bool)GetValue(CascadeRichVisualStatesProperty);
        set => SetValue(CascadeRichVisualStatesProperty, value);
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEnabled) ||
            propertyName == nameof(IsBusy) ||
            propertyName == nameof(InputTransparent))
        {
            _nativePrimaryTapBridge?.SynchronizeAvailability();
        }
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        _nativePrimaryTapBridge.Disconnect();
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _nativePrimaryTapBridge.Connect(Handler);
    }

    protected override void ApplyRichVisualStateCore(string state)
    {
        VisualStateManager.GoToState(this, state);

        if (!CascadeRichVisualStates)
            return;

        if (Content is IVisualTreeElement content)
            CascadeRichVisualState(content, state);
    }

    private void ReportNativeTouchDown() =>
        NativeTouchDown?.Invoke(this, EventArgs.Empty);

    private static void CascadeRichVisualState(IVisualTreeElement element, string state)
    {
        if (element is VisualElement visualElement && !GetRichVisualStateOptOut(visualElement))
            VisualStateManager.GoToState(visualElement, state);

        if (element is BindableObject bindable && !GetCascadeRichState(bindable))
            return;

        foreach (var child in element.GetVisualChildren())
            CascadeRichVisualState(child, state);
    }
}
