namespace CodeCrafty.DapperDan.Speech;

public enum VoiceCanaryScenario
{
    LanguageDefaultApplicationSession,
    RankedInstalledApplicationSession,
    LanguageDefaultSystemManagedSession,
}

public enum VoiceCanarySelection
{
    LanguageDefault,
    RankedInstalled,
}

public sealed record VoiceCanaryPlan(
    string Label,
    VoiceCanarySelection Selection,
    bool UsesApplicationAudioSession)
{
    public static VoiceCanaryPlan For(VoiceCanaryScenario scenario) =>
        scenario switch
        {
            VoiceCanaryScenario.LanguageDefaultApplicationSession => new(
                "A · en-US default / app session",
                VoiceCanarySelection.LanguageDefault,
                UsesApplicationAudioSession: true),
            VoiceCanaryScenario.RankedInstalledApplicationSession => new(
                "B · ranked installed voice / app session",
                VoiceCanarySelection.RankedInstalled,
                UsesApplicationAudioSession: true),
            VoiceCanaryScenario.LanguageDefaultSystemManagedSession => new(
                "C · en-US default / system session",
                VoiceCanarySelection.LanguageDefault,
                UsesApplicationAudioSession: false),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };
}
