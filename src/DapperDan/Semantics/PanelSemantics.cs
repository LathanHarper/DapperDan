namespace CodeCrafty.DapperDan.Semantics;

/// <summary>
/// Platform chrome values owned by the CodeCrafty.DapperDan PanelBoss host.
/// Android begins with navigation-bar clearance until the live inset observer reports it.
/// </summary>
public static class PanelMetrics
{
#if ANDROID
    public const double BottomDrawerClearance = 24d;
#else
    public const double BottomDrawerClearance = 0d;
#endif
}

/// <summary>
/// Original CodeCrafty.DapperDan colors used by portable interaction feedback.
/// </summary>
public static class SurfPalette
{
    public static readonly Color TouchFeedbackSignal = Color.FromArgb("#00A9A5");
}

/// <summary>
/// The transition names understood by PanelBoss attached properties.
/// </summary>
public static class PanelMotion
{
    public const string None = "None";
    public const string FadeInDownward = "FadeInDownward";
    public const string FadeOutUpward = "FadeOutUpward";
    public const string SlideInDownward = "SlideInDownward";
    public const string SlideInToLeft = "SlideInToLeft";
    public const string SlideInToRight = "SlideInToRight";
    public const string SlideInUpward = "SlideInUpward";
    public const string SlideOutDownward = "SlideOutDownward";
    public const string SlideOutToLeft = "SlideOutToLeft";
    public const string SlideOutToRight = "SlideOutToRight";
    public const string SlideOutUpward = "SlideOutUpward";
}
