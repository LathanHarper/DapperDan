using System.Text.Json;
using System.Text.Json.Serialization;
using Flexler.Models;

namespace Flexler.Services;

public sealed class FileFavoriteRecipeStore(string directory) : IFavoriteRecipeStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _directory = Path.GetFullPath(directory);

    public async Task<IReadOnlyList<FavoriteRecipe>> LoadAsync()
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync()).Favorites; }
        finally { _gate.Release(); }
    }

    public Task AddAsync(FavoriteRecipe favorite)
    {
        favorite.Validate();
        return ChangeAsync(favorites =>
        {
            if (favorites.Any(saved => saved.Id == favorite.Id
                || string.Equals(saved.Name, favorite.Name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("That favorite name is already used. Choose a different name.");
            favorites.Insert(0, favorite);
        });
    }

    public Task RemoveAsync(Guid id) => ChangeAsync(favorites => favorites.RemoveAll(saved => saved.Id == id));

    private async Task ChangeAsync(Action<List<FavoriteRecipe>> change)
    {
        await _gate.WaitAsync();
        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(_directory);
            // A second app instance must not silently overwrite this instance's edits.
            using var writeLock = new FileStream(Path.Combine(_directory, "favorites.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var document = await ReadAsync(); // Never replace an unreadable or newer-version library.
            change(document.Favorites);
            temporaryPath = Path.Combine(_directory, $"favorites-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(temporaryPath,
                JsonSerializer.Serialize(document, FavoriteJsonContext.Default.FavoriteDocument));
            File.Move(temporaryPath, Path.Combine(_directory, "favorites.json"), overwrite: true);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            _gate.Release();
        }
    }

    private async Task<FavoriteDocument> ReadAsync()
    {
        var path = Path.Combine(_directory, "favorites.json");
        string json;
        try { json = await File.ReadAllTextAsync(path); }
        catch (FileNotFoundException) { return new(1, []); }
        catch (DirectoryNotFoundException) { return new(1, []); }
        var document = JsonSerializer.Deserialize(json, FavoriteJsonContext.Default.FavoriteDocument);
        if (document is null || document.Version != 1 || document.Favorites is null)
            throw new InvalidDataException("Unsupported favorite library.");
        var ids = new HashSet<Guid>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var favorite in document.Favorites)
        {
            if (favorite is null) throw new InvalidDataException("Invalid favorite.");
            favorite.Validate();
            if (!ids.Add(favorite.Id) || !names.Add(favorite.Name))
                throw new InvalidDataException("Duplicate favorite.");
        }
        return document;
    }

}

internal sealed record FavoriteDocument(int Version, List<FavoriteRecipe> Favorites);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FavoriteDocument))]
internal partial class FavoriteJsonContext : JsonSerializerContext;
