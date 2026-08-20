namespace CodeCrafty.DapperDan.PanelBossKit;

public partial class PanelBoss
{
    public static readonly BindableProperty RichButtonCoordinationEnabledProperty =
        BindableProperty.CreateAttached(
            "RichButtonCoordinationEnabled",
            typeof(bool),
            typeof(PanelBoss),
            false,
            propertyChanged: OnRichButtonCoordinationEnabledChanged);

    public static readonly BindableProperty IWantRichStateProperty =
        BindableProperty.CreateAttached(
            "IWantRichState",
            typeof(bool),
            typeof(PanelBoss),
            false,
            propertyChanged: OnTapViewFeatureChanged);

    public static readonly BindableProperty TouchPointBloomProperty =
        BindableProperty.CreateAttached(
            "TouchPointBloom",
            typeof(bool),
            typeof(PanelBoss),
            false,
            propertyChanged: OnTapViewFeatureChanged);

    public static bool GetRichButtonCoordinationEnabled(BindableObject view) =>
        (bool)view.GetValue(RichButtonCoordinationEnabledProperty);

    public static void SetRichButtonCoordinationEnabled(BindableObject view, bool value) =>
        view.SetValue(RichButtonCoordinationEnabledProperty, value);

    public static bool GetIWantRichState(BindableObject view) =>
        (bool)view.GetValue(IWantRichStateProperty);

    public static void SetIWantRichState(BindableObject view, bool value) =>
        view.SetValue(IWantRichStateProperty, value);

    public static bool GetTouchPointBloom(BindableObject view) =>
        (bool)view.GetValue(TouchPointBloomProperty);

    public static void SetTouchPointBloom(BindableObject view, bool value) =>
        view.SetValue(TouchPointBloomProperty, value);

    private static void OnRichButtonCoordinationEnabledChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is Views.PanelBossBody_DefaultView host && newValue is bool isEnabled)
            host.SetRichButtonCoordinationEnabled(isEnabled);
    }

    private static void OnTapViewFeatureChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is CodeCrafty.DapperDan.Controls.TapViewBase tapView)
            Views.TapViewRegistrationBridge.OnFeatureChanged(tapView);
    }
}
