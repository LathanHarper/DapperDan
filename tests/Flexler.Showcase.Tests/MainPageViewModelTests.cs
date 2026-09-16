using Flexler.ViewModels;

namespace Flexler.Tests;

public sealed class MainPageViewModelTests
{
    [Fact]
    public async Task Clipboard_failure_is_visible_and_a_retry_can_succeed()
    {
        var (recipe, _, clipboard) = FlexRecipeExporterTests.CreateRecipe();
        clipboard.Failure = new InvalidOperationException("Clipboard busy");

        await recipe.ExportCommand.Execute();

        Assert.Contains("Clipboard unavailable", recipe.ExportStatus);
        Assert.Null(clipboard.Text);
        Assert.Equal(5, recipe.Items.Count);
        clipboard.Failure = null;
        await recipe.ExportCommand.Execute();
        Assert.StartsWith("Copied 5 items", recipe.ExportStatus);
        Assert.NotNull(clipboard.Text);
    }

    [Fact]
    public async Task Invalid_recipe_reports_a_recoverable_error_without_copying()
    {
        var (recipe, _, clipboard) = FlexRecipeExporterTests.CreateRecipe();
        recipe.Items[0].Text = "bad\0text";
        await recipe.ExportCommand.Execute();
        Assert.StartsWith("Could not export", recipe.ExportStatus);
        Assert.Null(clipboard.Text);
    }

    [Fact]
    public async Task Selection_shape_removal_and_reset_keep_commands_consistent()
    {
        var (recipe, _, _) = FlexRecipeExporterTests.CreateRecipe();
        Assert.False(recipe.ShapeSelectedCommand.CanExecute());
        var first = recipe.Items[0];
        var second = recipe.Items[1];
        recipe.Select(first);
        Assert.True(first.IsSelected);
        Assert.True(recipe.ShapeSelectedCommand.CanExecute());
        recipe.Select(second);
        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
        await recipe.ShapeSelectedCommand.Execute();
        Assert.True(recipe.IsInspectorOpen);
        await recipe.RemoveSelectedCommand.Execute();
        Assert.False(second.IsSelected);
        Assert.DoesNotContain(second, recipe.Items);
        Assert.Null(recipe.SelectedItem);
        Assert.False(recipe.IsInspectorOpen);
        Assert.False(recipe.ShapeSelectedCommand.CanExecute());
        await recipe.ResetCommand.Execute();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, recipe.Items.Select(item => item.Id));
        recipe.AddItemCommand.Execute();
        Assert.Equal(6, recipe.Items[^1].Id);
    }

    [Fact]
    public async Task Removed_items_cannot_be_selected_or_reopen_the_inspector()
    {
        var (recipe, _, _) = FlexRecipeExporterTests.CreateRecipe();
        var removed = recipe.Items[0];
        recipe.Items.Remove(removed);
        recipe.Select(removed);
        await recipe.OpenInspectorAsync(removed);
        Assert.Null(recipe.SelectedItem);
        Assert.False(recipe.IsInspectorOpen);
    }
}
