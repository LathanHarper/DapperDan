namespace CodeCrafty.DapperDan.Platforms.Android;

internal static class KeyboardViewportInsetMath
{
    private const double MinimumKeyboardHeightFactor = 0.15;

    internal static int GetResidualBottomInsetPixels(
        int contentBottom,
        int visibleFrameBottom,
        int rootHeight,
        bool hasAuthoritativeImeVisibility)
    {
        if (rootHeight <= 0)
        {
            return 0;
        }

        var residualInset = Math.Max(0, contentBottom - visibleFrameBottom);
        var isKeyboardOverlap = hasAuthoritativeImeVisibility ||
            residualInset > rootHeight * MinimumKeyboardHeightFactor;

        return isKeyboardOverlap
            ? residualInset
            : 0;
    }
}
