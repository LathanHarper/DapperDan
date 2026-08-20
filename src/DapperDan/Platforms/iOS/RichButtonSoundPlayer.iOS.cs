using AVFoundation;
using Foundation;

namespace CodeCrafty.DapperDan.Controls;

internal static partial class RichButtonSoundPlayer
{
    private static readonly object _soundGate = new();
    private static readonly Dictionary<string, AVAudioPlayer> _players = new(StringComparer.OrdinalIgnoreCase);
    private static bool _audioSessionConfigured;

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

        var player = Load(soundName);
        if (player == null)
            return;

        lock (_soundGate)
        {
            player.Stop();
            player.CurrentTime = 0;
            player.PrepareToPlay();
            player.Play();
        }
    }

    private static AVAudioPlayer Load(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            return null;

        lock (_soundGate)
        {
            if (_players.TryGetValue(soundName, out var cachedPlayer))
                return cachedPlayer;

            try
            {
                ConfigureAudioSession();

                var path = GetCachedSoundPath(soundName);
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                var player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(path));
                player.PrepareToPlay();
                _players[soundName] = player;
                return player;
            }
            catch
            {
                return null;
            }
        }
    }

    private static void ConfigureAudioSession()
    {
        if (_audioSessionConfigured)
            return;

        AVAudioSession.SharedInstance().SetCategory(
            AVAudioSessionCategory.Ambient,
            AVAudioSessionCategoryOptions.MixWithOthers);

        _audioSessionConfigured = true;
    }

    private static string GetCachedSoundPath(string soundName)
    {
        var cachePath = Path.Combine(FileSystem.CacheDirectory, soundName);
        if (File.Exists(cachePath))
            return cachePath;

        using var source = FileSystem.OpenAppPackageFileAsync(soundName).GetAwaiter().GetResult();
        using var target = File.Create(cachePath);
        source.CopyTo(target);
        return cachePath;
    }
}
