using AVFoundation;
using Foundation;

namespace Flexler.Controls;

internal static partial class RichButtonSoundPlayer
{
    private static readonly Dictionary<string, AVAudioPlayer> Players = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim LoadGate = new(1, 1);
    private static bool _audioSessionConfigured;

    static partial void PlatformPrime(string touchSound, string longTouchSound, string negativeFeedbackSound)
    {
        _ = LoadAsync(touchSound);
        _ = LoadAsync(longTouchSound);
        _ = LoadAsync(negativeFeedbackSound);
    }

    static partial void PlatformPlay(TapViewBase tapView, RichButtonFeedbackKind feedbackKind)
    {
        var soundName = feedbackKind switch
        {
            RichButtonFeedbackKind.Bunk => tapView.NegativeFeedbackSound,
            RichButtonFeedbackKind.LongPress => tapView.LongTouchSound,
            _ => tapView.TouchSound,
        };
        _ = PlayAsync(soundName);
    }

    private static async Task PlayAsync(string soundName)
    {
        try
        {
            if (await LoadAsync(soundName) is not { } player)
                return;
            player.Stop();
            player.CurrentTime = 0;
            player.Play();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"RichButton audio unavailable: {exception.Message}");
        }
    }

    private static async Task<AVAudioPlayer?> LoadAsync(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            return null;

        await LoadGate.WaitAsync();
        try
        {
            if (Players.TryGetValue(soundName, out var player))
                return player;

            if (!_audioSessionConfigured)
            {
                AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Ambient,
                    AVAudioSessionCategoryOptions.MixWithOthers);
                _audioSessionConfigured = true;
            }

            // Read packaged audio asynchronously; never block the UI on file I/O.
            using var source = await FileSystem.OpenAppPackageFileAsync(soundName);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer);
            using var audioData = NSData.FromArray(buffer.ToArray());
            player = AVAudioPlayer.FromData(audioData);
            if (player is null)
                return null;
            player.PrepareToPlay();
            Players[soundName] = player;
            return player;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"RichButton audio unavailable: {exception.Message}");
            return null;
        }
        finally
        {
            LoadGate.Release();
        }
    }
}
