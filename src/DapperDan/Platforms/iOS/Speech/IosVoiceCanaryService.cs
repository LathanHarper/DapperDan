using AVFoundation;
using Microsoft.Maui.ApplicationModel;

namespace CodeCrafty.DapperDan.Speech;

internal sealed class IosVoiceCanaryService : IVoiceCanaryService, IDisposable
{
    private const string EnglishUsLanguage = "en-US";

    private readonly object _activeGate = new();
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private readonly AVSpeechSynthesizer _synthesizer = new();
    private readonly VoiceCanarySpeechDelegate _speechDelegate;
    private ActiveSpeech? _activeSpeech;
    private bool _disposed;

    public IosVoiceCanaryService()
    {
        _speechDelegate = new VoiceCanarySpeechDelegate(
            OnSpeechStarted,
            OnSpeechFinished,
            OnSpeechCanceled);
        _synthesizer.Delegate = _speechDelegate;
    }

    public bool IsSupported => true;

    public async Task<VoiceCanaryResult> SpeakAsync(
        VoiceCanaryScenario scenario,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _runGate.WaitAsync(cancellationToken);

        AVSpeechUtterance? utterance = null;
        ActiveSpeech? activeSpeech = null;

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var plan = VoiceCanaryPlan.For(scenario);
            VoiceCanaryResult? result = null;
            var completion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var matchingVoices = AVSpeechSynthesisVoice.GetSpeechVoices()
                    .Where(voice => string.Equals(
                        voice.Language,
                        EnglishUsLanguage,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var selectedVoice = SelectVoice(plan.Selection, matchingVoices);
                if (selectedVoice is null)
                {
                    throw new InvalidOperationException(
                        "An installed US English voice is unavailable.");
                }
                var beforeSpeech = CaptureSharedAudioSession();

                _synthesizer.UsesApplicationAudioSession =
                    plan.UsesApplicationAudioSession;

                utterance = new AVSpeechUtterance(text);
                utterance.Voice = selectedVoice;

                activeSpeech = new ActiveSpeech(utterance, completion, beforeSpeech);
                SetActiveSpeech(activeSpeech);

                result = CreateResult(
                    plan,
                    selectedVoice,
                    matchingVoices.Length,
                    activeSpeech);
                _synthesizer.SpeakUtterance(utterance);
            });

            using var cancellationRegistration =
                cancellationToken.Register(Stop);
            await completion.Task.WaitAsync(cancellationToken);
            await Task.Delay(100, cancellationToken);

            var afterSpeech = await MainThread.InvokeOnMainThreadAsync(
                CaptureSharedAudioSession);
            return result! with
            {
                AtSpeechStart = activeSpeech!.AtSpeechStart,
                AfterSpeech = afterSpeech,
            };
        }
        catch (OperationCanceledException)
        {
            await MainThread.InvokeOnMainThreadAsync(StopNativeSpeech);
            throw;
        }
        finally
        {
            ClearActiveSpeech(activeSpeech);
            utterance?.Dispose();
            _runGate.Release();
        }
    }

    public void Stop()
    {
        TakeActiveSpeech()?.Completion.TrySetCanceled();
        MainThread.BeginInvokeOnMainThread(StopNativeSpeech);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _synthesizer.Delegate = null;
        _speechDelegate.Dispose();
        _synthesizer.Dispose();
        _runGate.Dispose();
    }

    private static AVSpeechSynthesisVoice? SelectVoice(
        VoiceCanarySelection selection,
        IReadOnlyCollection<AVSpeechSynthesisVoice> matchingVoices) =>
        selection switch
        {
            VoiceCanarySelection.LanguageDefault =>
                AVSpeechSynthesisVoice.FromLanguage(EnglishUsLanguage),
            VoiceCanarySelection.RankedInstalled => matchingVoices
                .OrderByDescending(voice => (int)voice.Quality)
                .ThenBy(voice => voice.Name, StringComparer.Ordinal)
                .FirstOrDefault()
                ?? AVSpeechSynthesisVoice.FromLanguage(EnglishUsLanguage),
            _ => throw new ArgumentOutOfRangeException(
                nameof(selection),
                selection,
                null),
        };

    private static VoiceCanaryResult CreateResult(
        VoiceCanaryPlan plan,
        AVSpeechSynthesisVoice? voice,
        int matchingVoiceCount,
        ActiveSpeech activeSpeech)
    {
        return new VoiceCanaryResult(
            plan,
            plan.Selection == VoiceCanarySelection.RankedInstalled
                ? "quality-descending, name-ascending installed-voice ranking"
                : "AVSpeechSynthesisVoice.FromLanguage(\"en-US\")",
            voice!.Name,
            voice.Identifier,
            voice.Language,
            voice.Quality.ToString(),
            matchingVoiceCount,
            activeSpeech.BeforeSpeech,
            VoiceCanaryAudioSnapshot.Unavailable,
            VoiceCanaryAudioSnapshot.Unavailable);
    }

    private static VoiceCanaryAudioSnapshot CaptureSharedAudioSession()
    {
        var session = AVAudioSession.SharedInstance();
        var route = string.Join(
            ", ",
            session.CurrentRoute.Outputs.Select(output => output.PortType.ToString()));

        return new VoiceCanaryAudioSnapshot(
            session.Category?.ToString() ?? "none",
            session.Mode?.ToString() ?? "none",
            session.CategoryOptions.ToString(),
            session.SampleRate,
            (int)session.OutputNumberOfChannels,
            string.IsNullOrWhiteSpace(route) ? "no output route" : route);
    }

    private void OnSpeechStarted(AVSpeechUtterance utterance)
    {
        lock (_activeGate)
        {
            if (_activeSpeech?.Utterance.Handle == utterance.Handle)
            {
                _activeSpeech.AtSpeechStart = CaptureSharedAudioSession();
            }
        }
    }

    private void OnSpeechFinished(AVSpeechUtterance utterance) =>
        TakeActiveSpeech(utterance)?.Completion.TrySetResult();

    private void OnSpeechCanceled(AVSpeechUtterance utterance) =>
        TakeActiveSpeech(utterance)?.Completion.TrySetCanceled();

    private void SetActiveSpeech(ActiveSpeech activeSpeech)
    {
        lock (_activeGate)
        {
            _activeSpeech = activeSpeech;
        }
    }

    private void ClearActiveSpeech(ActiveSpeech? activeSpeech)
    {
        lock (_activeGate)
        {
            if (ReferenceEquals(_activeSpeech, activeSpeech))
            {
                _activeSpeech = null;
            }
        }
    }

    private ActiveSpeech? TakeActiveSpeech(AVSpeechUtterance? utterance = null)
    {
        lock (_activeGate)
        {
            if (_activeSpeech is null ||
                (utterance is not null &&
                    _activeSpeech.Utterance.Handle != utterance.Handle))
            {
                return null;
            }

            var activeSpeech = _activeSpeech;
            _activeSpeech = null;
            return activeSpeech;
        }
    }

    private void StopNativeSpeech() =>
        _synthesizer.StopSpeaking(AVSpeechBoundary.Immediate);

    private sealed class ActiveSpeech(
        AVSpeechUtterance utterance,
        TaskCompletionSource completion,
        VoiceCanaryAudioSnapshot beforeSpeech)
    {
        public AVSpeechUtterance Utterance { get; } = utterance;

        public TaskCompletionSource Completion { get; } = completion;

        public VoiceCanaryAudioSnapshot BeforeSpeech { get; } = beforeSpeech;

        public VoiceCanaryAudioSnapshot AtSpeechStart { get; set; } =
            VoiceCanaryAudioSnapshot.Unavailable;
    }

    private sealed class VoiceCanarySpeechDelegate(
        Action<AVSpeechUtterance> started,
        Action<AVSpeechUtterance> finished,
        Action<AVSpeechUtterance> canceled) : AVSpeechSynthesizerDelegate
    {
        public override void DidStartSpeechUtterance(
            AVSpeechSynthesizer synthesizer,
            AVSpeechUtterance utterance) => started(utterance);

        public override void DidFinishSpeechUtterance(
            AVSpeechSynthesizer synthesizer,
            AVSpeechUtterance utterance) => finished(utterance);

        public override void DidCancelSpeechUtterance(
            AVSpeechSynthesizer synthesizer,
            AVSpeechUtterance utterance) => canceled(utterance);
    }
}
