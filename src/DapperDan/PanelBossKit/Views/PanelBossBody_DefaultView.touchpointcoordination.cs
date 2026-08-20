using CodeCrafty.DapperDan.Controls;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

public partial class PanelBossBody_DefaultView
{
    private const int TouchPointBloomPresentationMilliseconds = 34;
    private readonly Dictionary<TapViewBase, int> _touchPointBloomOriginalPresentationMilliseconds = [];
    private readonly HashSet<TapViewBase> _touchPointBloomButtons = [];
    private bool _richButtonCoordinationEnabled;
    private Grid _touchPointBloomOverlayArea;

    internal void SetRichButtonCoordinationEnabled(bool isEnabled)
    {
        if (_richButtonCoordinationEnabled == isEnabled)
            return;

        _richButtonCoordinationEnabled = isEnabled;

        if (isEnabled)
        {
            Loaded += OnRichButtonCoordinationHostLoaded;
            Unloaded += OnRichButtonCoordinationHostUnloaded;

            if (IsLoaded)
                RegisterPanelBossBehaviors();

            return;
        }

        Loaded -= OnRichButtonCoordinationHostLoaded;
        Unloaded -= OnRichButtonCoordinationHostUnloaded;
        UnregisterPanelBossBehaviors();
    }

    private void OnRichButtonCoordinationHostLoaded(object sender, EventArgs e) =>
        RegisterPanelBossBehaviors();

    private void OnRichButtonCoordinationHostUnloaded(object sender, EventArgs e) =>
        UnregisterPanelBossBehaviors();

    private void RegisterPanelBossBehaviors()
    {
        RefreshStaticTapViewRosterFeatures();
        ReconcileTrackedTapViewRegistrations();
        RegisterRichStateControls();
    }

    internal void RefreshRichButtonCoordination()
    {
        if (!_richButtonCoordinationEnabled || !IsLoaded)
            return;

        RegisterPanelBossBehaviors();
    }

    private void UnregisterPanelBossBehaviors()
    {
        foreach (var button in _touchPointBloomButtons)
            UnregisterTouchPointBloomButton(button);

        _touchPointBloomButtons.Clear();
        _touchPointBloomOriginalPresentationMilliseconds.Clear();
        UnregisterRichStateControls();
        RemoveTouchPointBloomOverlayArea();
        ResetAppliedTapViewFeatures();
        ClearLegacyTapViewSources();
    }

    private void OnTouchPointBloomButtonTouching(object sender, RichButtonTapStartingEventArgs e)
    {
        if (sender is not TapViewBase button)
            return;

        Console.WriteLine(
            $"TOUCH_POINT_BLOOM|touching|id={button.AutomationId}|width={button.Width:0.##}|height={button.Height:0.##}|loaded={button.IsLoaded}");
        ShowTouchPointBloom(button, e);
    }

    private void OnTouchPointBloomButtonUnloaded(object sender, EventArgs e)
    {
        if (sender is not TapViewBase button)
            return;

        // Handler and rich-state churn can deliver an old native Unloaded
        // after the same virtual button is already live again.
        if (button.IsLoaded)
            return;

        UnregisterTouchPointBloomButton(button);
        _touchPointBloomButtons.Remove(button);
        ForgetLegacyTapViewFeature(button, TapViewFeatures.TouchPointBloom);
    }

    private void UnregisterTouchPointBloomButton(TapViewBase button)
    {
        button.Touching -= OnTouchPointBloomButtonTouching;
        button.Unloaded -= OnTouchPointBloomButtonUnloaded;

        if (_touchPointBloomOriginalPresentationMilliseconds.Remove(
                button,
                out var originalMilliseconds))
        {
            button.FeedbackPresentationMilliseconds = originalMilliseconds;
        }
    }

    private void EnsureTouchPointBloomOverlayArea()
    {
        if (_touchPointBloomOverlayArea is not null)
            return;

        _touchPointBloomOverlayArea = new Grid
        {
            AutomationId = "PanelBoss_TouchPointBloomOverlay",
            HorizontalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            IsClippedToBounds = false,
            VerticalOptions = LayoutOptions.Fill,
            ZIndex = 80
        };

        Grid.SetRow(_touchPointBloomOverlayArea, 1);
        Children.Add(_touchPointBloomOverlayArea);
    }

    private void RemoveTouchPointBloomOverlayArea()
    {
        if (_touchPointBloomOverlayArea is null)
            return;

        Children.Remove(_touchPointBloomOverlayArea);
        RemoveTouchPointBloom();
        _touchPointBloomOverlayArea = null;
    }
}
