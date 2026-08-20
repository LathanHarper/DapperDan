namespace CodeCrafty.DapperDan.Controls;

internal static partial class RichButtonSoundPlayer
{
    public static void PrimeDefaults()
    {
        PlatformPrime(
            TapViewBase.DefaultTouchSound,
            TapViewBase.DefaultLongTouchSound,
            TapViewBase.DefaultNegativeFeedbackSound);
    }

    public static void Prime(TapViewBase tapView)
    {
        PlatformPrime(
            tapView.TouchSound,
            tapView.LongTouchSound,
            tapView.NegativeFeedbackSound);
    }

    public static void Play(TapViewBase tapView, RichButtonFeedbackKind feedbackKind)
    {
        PlatformPlay(tapView, feedbackKind);
    }

    static partial void PlatformPrime(string touchSound, string longTouchSound, string negativeFeedbackSound);
    static partial void PlatformPlay(TapViewBase tapView, RichButtonFeedbackKind feedbackKind);
}
