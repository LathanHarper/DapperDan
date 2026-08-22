namespace CodeCrafty.DapperDan.Speech;

public interface IVoiceCanaryService
{
    bool IsSupported { get; }

    Task<VoiceCanaryResult> SpeakAsync(
        VoiceCanaryScenario scenario,
        string text,
        CancellationToken cancellationToken = default);

    void Stop();
}

public sealed record VoiceCanaryResult(
    VoiceCanaryPlan Plan,
    string VoiceSource,
    string VoiceName,
    string VoiceIdentifier,
    string VoiceLanguage,
    string VoiceQuality,
    int MatchingLanguageVoiceCount,
    VoiceCanaryAudioSnapshot BeforeSpeech,
    VoiceCanaryAudioSnapshot AtSpeechStart,
    VoiceCanaryAudioSnapshot AfterSpeech)
{
    public string ToDisplayText()
    {
        var sessionOwner = Plan.UsesApplicationAudioSession
            ? "shared application audio session"
            : "separate Apple-managed speech session";

        return string.Join(
            Environment.NewLine,
            $"Voice source: {VoiceSource}",
            $"Voice: {VoiceName}",
            $"Identifier: {VoiceIdentifier}",
            $"Language / quality: {VoiceLanguage} / {VoiceQuality}",
            $"Installed en-US matches: {MatchingLanguageVoiceCount}",
            $"Speech owner: {sessionOwner}",
            $"Shared session before: {BeforeSpeech.ToDisplayText()}",
            $"Shared session at start: {AtSpeechStart.ToDisplayText()}",
            $"Shared session after: {AfterSpeech.ToDisplayText()}");
    }
}

public sealed record VoiceCanaryAudioSnapshot(
    string Category,
    string Mode,
    string CategoryOptions,
    double SampleRate,
    int OutputChannels,
    string OutputRoute)
{
    public static VoiceCanaryAudioSnapshot Unavailable { get; } = new(
        "unavailable",
        "unavailable",
        "unavailable",
        0,
        0,
        "unavailable");

    public string ToDisplayText() =>
        $"{Category} · {Mode} · {CategoryOptions} · " +
        $"{SampleRate:0} Hz · {OutputChannels} ch · {OutputRoute}";
}
