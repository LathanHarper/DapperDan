using Flexler.Models;
using Flexler.PanelBossKit;
using Flexler.Services;
using Flexler.ViewModels;
using Microsoft.Maui.Layouts;

namespace Flexler.Tests;

public sealed class FavoriteRecipeTests
{
    [Fact]
    public async Task Favorite_restores_exact_export_after_new_store_and_viewmodel_and_remains_independent()
    {
        using var folder = new FavoriteTestFolder();
        var original = Create(new FileFavoriteRecipeStore(folder.Path));
        original.Direction = FlexDirection.ColumnReverse;
        original.Wrap = FlexWrap.Reverse;
        original.AlignContent = FlexAlignContent.SpaceAround;
        original.AlignItems = FlexAlignItems.End;
        original.JustifyContent = FlexJustify.SpaceBetween;
        original.PreviewPadding = 7.25;
        original.PreviewMinimumHeight = -1;
        original.Items[0].Text = "Aloha & <Flex> {ready}\n海";
        original.Items[0].Grow = 2;
        original.Items[0].Shrink = 0.5f;
        original.Items[0].Order = -3;
        original.Items[0].AlignSelf = FlexAlignSelf.Center;
        original.Items[1].BasisOption = original.BasisOptions[2];
        original.Items[2].BasisOption = original.BasisOptions[5];
        var expectedXaml = original.RecipeXaml;
        await original.OpenFavoritesCommand.Execute();
        original.FavoriteName = "  Island flow  ";
        await original.SaveFavoriteCommand.Execute();
        Assert.Equal("Island flow", Assert.Single(original.Favorites).Name);
        original.Items[0].Text = "Unsaved edit";
        await original.ResetCommand.Execute();
        Assert.Single(original.Favorites);
        Assert.Equal(18, original.PreviewPadding);
        Assert.Equal(0, original.PreviewMinimumHeight);

        var reopened = Create(new FileFavoriteRecipeStore(folder.Path));
        await reopened.OpenFavoritesCommand.Execute();
        Assert.Single(reopened.Favorites);
        await reopened.LoadFavoriteCommand.Execute();
        Assert.Equal(expectedXaml, reopened.RecipeXaml);
        Assert.False(reopened.IsRecipeOpen);
        Assert.Null(reopened.SelectedItem);
        reopened.Items[0].Text = "Another edit";
        await reopened.OpenFavoritesCommand.Execute();
        await reopened.LoadFavoriteCommand.Execute();
        Assert.Equal(expectedXaml, reopened.RecipeXaml);
        reopened.AddItemCommand.Execute();
        Assert.Equal(6, reopened.Items.Select(item => item.Id).Distinct().Count());
    }

    [Fact]
    public async Task Empty_recipe_can_be_saved_and_reloaded()
    {
        var recipe = Create(new TestFavoriteStore());
        recipe.Items.Clear();
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "Empty start";
        await recipe.SaveFavoriteCommand.Execute();
        recipe.AddItemCommand.Execute();
        await recipe.LoadFavoriteCommand.Execute();
        Assert.Empty(recipe.Items);
        recipe.AddItemCommand.Execute();
        Assert.Equal(1, Assert.Single(recipe.Items).Id);
    }

    [Fact]
    public async Task Save_in_flight_keeps_new_edits_and_serializes_favorite_actions()
    {
        var store = new TestFavoriteStore { AddCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var recipe = Create(store);
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "Snapshot";
        recipe.IsFavoriteBusy = true; // Physical RichButton's TwoWay write.
        var save = recipe.SaveFavoriteCommand.Execute();
        Assert.True(recipe.IsFavoriteBusy);
        Assert.False(recipe.SaveFavoriteCommand.CanExecute());
        Assert.False(recipe.LoadFavoriteCommand.CanExecute());
        Assert.False(recipe.DeleteFavoriteCommand.CanExecute());
        recipe.Items[0].Text = "New unsaved work";
        recipe.FavoriteName = "Next name";
        store.AddCompletion.SetResult();
        await save;
        Assert.False(recipe.IsFavoriteBusy);
        Assert.Equal("Next name", recipe.FavoriteName);
        Assert.Contains("newer edits", recipe.FavoritesStatus);
        Assert.Equal("One", Assert.Single(recipe.Favorites).Recipe.Items[0].Text);
    }

    [Fact]
    public async Task Unreadable_library_is_not_replaced_and_can_be_retried()
    {
        using var folder = new FavoriteTestFolder();
        var path = System.IO.Path.Combine(folder.Path, "favorites.json");
        const string broken = "{ interrupted";
        await File.WriteAllTextAsync(path, broken);
        var store = new FileFavoriteRecipeStore(folder.Path);
        var recipe = Create(store);
        var current = recipe.RecipeXaml;
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "New favorite";
        Assert.False(recipe.SaveFavoriteCommand.CanExecute());
        Assert.Contains("unavailable", recipe.FavoritesStatus);
        Assert.Equal(current, recipe.RecipeXaml);
        var good = new TestFavoriteStore();
        var source = Create(good);
        await source.OpenFavoritesCommand.Execute();
        source.FavoriteName = "Source";
        await source.SaveFavoriteCommand.Execute();
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => store.AddAsync(good.Saved[0]));
        Assert.Equal(broken, await File.ReadAllTextAsync(path));
        await File.WriteAllTextAsync(path, "{\"Version\":1,\"Favorites\":[]}");
        await recipe.OpenFavoritesCommand.Execute();
        Assert.True(recipe.SaveFavoriteCommand.CanExecute());
    }

    [Theory]
    [InlineData("{\"Version\":2,\"Favorites\":[]}")]
    [InlineData("{\"Version\":1,\"Favorites\":[null]}")]
    [InlineData("{\"Version\":1,\"Favorites\":null}")]
    public async Task Invalid_or_future_documents_fail_without_changing_disk(string json)
    {
        using var folder = new FavoriteTestFolder();
        var path = System.IO.Path.Combine(folder.Path, "favorites.json");
        await File.WriteAllTextAsync(path, json);
        var store = new FileFavoriteRecipeStore(folder.Path);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync());
        await Assert.ThrowsAsync<InvalidDataException>(() => store.RemoveAsync(Guid.NewGuid()));
        Assert.Equal(json, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Duplicate_names_and_failed_writes_do_not_claim_a_save()
    {
        using var folder = new FavoriteTestFolder();
        var recipe = Create(new FileFavoriteRecipeStore(folder.Path));
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "One favorite";
        await recipe.SaveFavoriteCommand.Execute();
        recipe.FavoriteName = "ONE FAVORITE";
        await recipe.SaveFavoriteCommand.Execute();
        Assert.Single(recipe.Favorites);
        Assert.Contains("unique name", recipe.FavoritesStatus);
        Assert.Equal("ONE FAVORITE", recipe.FavoriteName);

        var store = new TestFavoriteStore { AddFailure = new IOException("Disk full") };
        var failed = Create(store);
        await failed.OpenFavoritesCommand.Execute();
        failed.FavoriteName = "Keep my name";
        await failed.SaveFavoriteCommand.Execute();
        Assert.Empty(failed.Favorites);
        Assert.Equal("Keep my name", failed.FavoriteName);
        Assert.Contains("unavailable", failed.FavoritesStatus);
        Assert.False(failed.IsFavoriteBusy);
        Assert.True(failed.SaveFavoriteCommand.CanExecute());
    }

    [Fact]
    public async Task Removal_requires_confirmation_and_does_not_change_canvas()
    {
        using var folder = new FavoriteTestFolder();
        var recipe = Create(new FileFavoriteRecipeStore(folder.Path));
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "Removable";
        await recipe.SaveFavoriteCommand.Execute();
        var current = recipe.RecipeXaml;
        await recipe.DeleteFavoriteCommand.Execute();
        Assert.Single(recipe.Favorites);
        Assert.Equal("CONFIRM REMOVE", recipe.DeleteFavoriteLabel);
        await recipe.PreviewRecipeCommand.Execute();
        await recipe.OpenFavoritesCommand.Execute();
        Assert.Equal("REMOVE", recipe.DeleteFavoriteLabel);
        await recipe.DeleteFavoriteCommand.Execute();
        await recipe.DeleteFavoriteCommand.Execute();
        Assert.Empty(recipe.Favorites);
        Assert.Empty(await new FileFavoriteRecipeStore(folder.Path).LoadAsync());
        Assert.Equal(current, recipe.RecipeXaml);
        Assert.False(recipe.LoadFavoriteCommand.CanExecute());
    }

    [Fact]
    public async Task Invalid_layout_cannot_be_saved()
    {
        var recipe = Create(new TestFavoriteStore());
        await recipe.OpenFavoritesCommand.Execute();
        recipe.FavoriteName = "Invalid";
        recipe.Items[0].Grow = float.NaN;
        await recipe.SaveFavoriteCommand.Execute();
        Assert.Empty(recipe.Favorites);
        Assert.Contains("Check the layout", recipe.FavoritesStatus);
        Assert.False(recipe.IsFavoriteBusy);
    }

    private static MainPageViewModel Create(IFavoriteRecipeStore store) =>
        new(new FlexRecipeExporter(new TestClipboard()), new PanelBoss(), store);
}

internal sealed class TestFavoriteStore : IFavoriteRecipeStore
{
    public List<FavoriteRecipe> Saved { get; } = [];
    public TaskCompletionSource? AddCompletion { get; set; }
    public Exception? AddFailure { get; set; }
    public Task<IReadOnlyList<FavoriteRecipe>> LoadAsync() => Task.FromResult<IReadOnlyList<FavoriteRecipe>>(Saved.ToArray());
    public async Task AddAsync(FavoriteRecipe favorite)
    {
        if (AddCompletion is not null) await AddCompletion.Task;
        if (AddFailure is not null) throw AddFailure;
        Saved.Insert(0, favorite);
    }
    public Task RemoveAsync(Guid id) { Saved.RemoveAll(f => f.Id == id); return Task.CompletedTask; }
}

internal sealed class FavoriteTestFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlexlerFavoriteTests", Guid.NewGuid().ToString("N"));
    public FavoriteTestFolder() => Directory.CreateDirectory(Path);
    public void Dispose()
    {
        var testRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlexlerFavoriteTests"))
            + System.IO.Path.DirectorySeparatorChar;
        var ownedFolder = System.IO.Path.GetFullPath(Path);
        if (!ownedFolder.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase)
            || !Guid.TryParseExact(System.IO.Path.GetFileName(ownedFolder), "N", out _))
        {
            throw new InvalidOperationException("Refusing to remove a folder outside this test's temporary scope.");
        }
        Directory.Delete(ownedFolder, recursive: true);
    }
}
