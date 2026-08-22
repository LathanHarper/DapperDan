using CodeCrafty.DapperDan.Speech;

namespace CodeCrafty.DapperDan.Tests;

public sealed class VoiceCanaryScenarioTests
{
    [Theory]
    [InlineData(
        VoiceCanaryScenario.LanguageDefaultApplicationSession,
        VoiceCanarySelection.LanguageDefault,
        true)]
    [InlineData(
        VoiceCanaryScenario.RankedInstalledApplicationSession,
        VoiceCanarySelection.RankedInstalled,
        true)]
    [InlineData(
        VoiceCanaryScenario.LanguageDefaultSystemManagedSession,
        VoiceCanarySelection.LanguageDefault,
        false)]
    public void ScenarioPlanChangesOneExperimentalAxisAtATime(
        VoiceCanaryScenario scenario,
        VoiceCanarySelection expectedSelection,
        bool expectedApplicationSession)
    {
        var plan = VoiceCanaryPlan.For(scenario);

        Assert.Equal(expectedSelection, plan.Selection);
        Assert.Equal(expectedApplicationSession, plan.UsesApplicationAudioSession);
        Assert.False(string.IsNullOrWhiteSpace(plan.Label));
    }
}
