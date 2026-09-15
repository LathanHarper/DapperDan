

using Android.Media;

namespace Flexler.Controls;

internal static partial class RichButtonSoundPlayer
{
    private static readonly object _soundGate = new();
    private static readonly Dictionary<string, int> _soundIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> _streamIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<SoundPool> _soundPool = new(CreateSoundPool);

    static partial void PlatformPrime(string touchSound, string longTouchSound, string negativeFeedbackSound)
    {
        Load(touchSound);
        Load(longTouchSound);
        Load(negativeFeedbackSound);
    }

    static partial void PlatformPlay(TapViewBase tapView, RichButtonFeedbackKind feedbackKind)
    {
        var soundName = feedbackKind switch
        {
            RichButtonFeedbackKind.Bunk => tapView.NegativeFeedbackSound,
            RichButtonFeedbackKind.LongPress => tapView.LongTouchSound,
            _ => tapView.TouchSound
        };

        var soundId = Load(soundName);
        if (soundId == 0)
            return;

        var soundPool = _soundPool.Value;

        lock (_soundGate)
        {
            if (_streamIds.TryGetValue(soundName, out var streamId) && streamId != 0)
                soundPool.Stop(streamId);

            streamId = soundPool.Play(soundId, 1f, 1f, 1, 0, 1f);

            if (streamId == 0)
                _streamIds.Remove(soundName);
            else
                _streamIds[soundName] = streamId;
        }
    }

    private static SoundPool CreateSoundPool()
    {
        using var audioBuilder = new AudioAttributes.Builder();
        audioBuilder.SetUsage(AudioUsageKind.AssistanceSonification);
        audioBuilder.SetContentType(AudioContentType.Sonification);
        using var audioAttributes = audioBuilder.Build()
            ?? throw new InvalidOperationException("Android audio attributes are unavailable.");

        using var poolBuilder = new SoundPool.Builder();
        poolBuilder.SetAudioAttributes(audioAttributes);
        poolBuilder.SetMaxStreams(4);
        return poolBuilder.Build()
            ?? throw new InvalidOperationException("Android sound pool is unavailable.");
    }

    private static int Load(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            return 0;

        lock (_soundGate)
        {
            if (_soundIds.TryGetValue(soundName, out var cachedSoundId))
                return cachedSoundId;

            try
            {
                var assets = Android.App.Application.Context.Assets
                    ?? throw new InvalidOperationException("Android packaged assets are unavailable.");
                using var assetDescriptor = assets.OpenFd(soundName)
                    ?? throw new FileNotFoundException("The optional feedback sound is unavailable.", soundName);
                var soundId = _soundPool.Value.Load(
                    assetDescriptor.FileDescriptor,
                    assetDescriptor.StartOffset,
                    assetDescriptor.Length,
                    1);

                _soundIds[soundName] = soundId;
                return soundId;
            }
            catch
            {
                _soundIds[soundName] = 0;
                return 0;
            }
        }
    }
}
