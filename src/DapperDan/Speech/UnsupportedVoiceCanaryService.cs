namespace CodeCrafty.DapperDan.Speech;

internal sealed class UnsupportedVoiceCanaryService : IVoiceCanaryService
{
    public bool IsSupported => false;

    public Task<VoiceCanaryResult> SpeakAsync(
        VoiceCanaryScenario scenario,
        string text,
        CancellationToken cancellationToken = default) =>
        Task.FromException<VoiceCanaryResult>(
            new PlatformNotSupportedException(
                "The native voice canary runs only on iOS."));

    public void Stop()
    {
    }
}
