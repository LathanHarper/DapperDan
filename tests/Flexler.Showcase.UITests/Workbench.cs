using System.Diagnostics;
using System.Text.Json;

namespace Flexler.UITests;

internal abstract class Workbench : IDisposable
{
    private int _step;
    private readonly HashSet<string> _ownedFavorites = [];
    protected Workbench(string evidence) { Evidence = evidence; Directory.CreateDirectory(evidence); }
    protected string Evidence { get; }
    public bool Landscape { get; protected set; }
    public abstract void Click(UiTarget target);
    public abstract void Enter(UiTarget target, string text);
    public abstract void Select(UiTarget target, string text);
    public abstract string Text(UiTarget target);
    public abstract bool Enabled(UiTarget target);
    public abstract bool Present(UiTarget target);
    public abstract void SetCompact(bool compact);
    public abstract void Restart();
    public abstract string ClipboardText();
    protected abstract void SaveEvidence(string path);
    public abstract void Dispose();

    public void Wait(Func<bool> condition, string expectation, int seconds = 15)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            if (condition()) return;
            Thread.Sleep(120);
        }
        throw new TimeoutException($"Expected {expectation} within {seconds}s.");
    }
    public void Count(int count) => Wait(() => Text(UiTargets.Count) == $"{count} ITEMS", $"{count} recipe items");
    public void ExpectText(UiTarget target, string text) => Wait(() => Text(target).Contains(text, StringComparison.Ordinal), $"{target} to contain '{text}'");
    public void Snapshot(string name)
    {
        SaveEvidence(Path.Combine(Evidence, $"{++_step:D2}-{name}"));
        File.AppendAllText(Path.Combine(Evidence, "steps.jsonl"), JsonSerializer.Serialize(new { step = _step, name, utc = DateTimeOffset.UtcNow, Landscape }) + Environment.NewLine);
    }

    public void OwnFavorite(string name)
    {
        _ownedFavorites.Add(name);
        File.AppendAllText(Path.Combine(Evidence, "created-favorites.txt"), name + Environment.NewLine);
    }
    public void ForgetFavorite(string name)
    {
        _ownedFavorites.Remove(name);
        File.AppendAllText(Path.Combine(Evidence, "removed-favorites.txt"), name + Environment.NewLine);
    }
    public void CleanUpFavorites()
    {
        foreach (var name in _ownedFavorites.ToArray())
        {
            Restart();
            Click(UiTargets.Favorites);
            Select(UiTargets.Saved, name);
            Click(UiTargets.Delete);
            ExpectText(UiTargets.FavoritesStatus, "CONFIRM REMOVE");
            Click(UiTargets.Delete);
            ExpectText(UiTargets.FavoritesStatus, $"Removed “{name}”");
            ForgetFavorite(name);
        }
    }
}
