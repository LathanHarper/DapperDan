using System.Collections.ObjectModel;
using Flexler.PanelBossKit;
using Flexler.PanelBossKit.Views;
using Flexler.Services;
using Flexler.ViewModels;
using Microsoft.Maui.Layouts;

namespace Flexler.Tests;

public sealed class MainPageRecipeUxTests
{
    [Fact]
    public void Preview_tracks_container_geometry_and_all_item_edits_without_reopening()
    {
        var (recipe, exporter, _) = FlexRecipeExporterTests.CreateRecipe();
        var item = recipe.Items[0];
        Action[] edits =
        [
            () => recipe.Direction = FlexDirection.ColumnReverse,
            () => recipe.Wrap = FlexWrap.NoWrap,
            () => recipe.JustifyContent = FlexJustify.SpaceBetween,
            () => recipe.AlignItems = FlexAlignItems.Center,
            () => recipe.AlignContent = FlexAlignContent.End,
            () => recipe.PreviewPadding = 24,
            () => recipe.PreviewMinimumHeight = 90,
            () => item.Text = "<Aloha> & {Binding belongs to the user}",
            () => item.Grow = 2,
            () => item.Shrink = 0.25f,
            () => item.BasisOption = recipe.BasisOptions[2],
            () => item.AlignSelf = FlexAlignSelf.End,
            () => item.Order = -2,
            () => recipe.AddItemCommand.Execute(),
            () => recipe.Items.Move(0, 2),
            () => recipe.Items.RemoveAt(1)
        ];

        Assert.Equal(exporter.Build(recipe), recipe.RecipeXaml);
        foreach (var edit in edits)
        {
            var before = recipe.RecipeXaml;
            edit();
            Assert.NotEqual(before, recipe.RecipeXaml);
            Assert.Equal(exporter.Build(recipe), recipe.RecipeXaml);
            Assert.Empty(recipe.RecipeError);
        }

        Assert.Contains("5 items", recipe.LayoutSummary);
        Assert.Contains("ColumnReverse", recipe.LayoutSummary);
        Assert.Contains("NoWrap", recipe.LayoutSummary);
        Assert.Contains("Justify SpaceBetween", recipe.LayoutSummary);
        Assert.Contains("Align Center", recipe.LayoutSummary);
    }

    [Fact]
    public async Task Reset_replace_and_clear_detach_old_items_from_live_preview()
    {
        var exporter = new ObservedExporter();
        var recipe = new MainPageViewModel(exporter, new PanelBoss(), new TestFavoriteStore());
        var oldItems = recipe.Items.ToArray();
        await recipe.ResetCommand.Execute();
        var resetBuilds = exporter.BuildCount;
        oldItems[0].Text = "Detached after reset";
        Assert.Equal(resetBuilds, exporter.BuildCount);

        var replaced = recipe.Items[0];
        recipe.Items[0] = new FlexItemViewModel(99, "Replacement", recipe.BasisOptions[0]);
        var replaceBuilds = exporter.BuildCount;
        replaced.Grow = 4;
        Assert.Equal(replaceBuilds, exporter.BuildCount);
        recipe.Items[0].Text = "Replacement remains live";
        Assert.Contains("Replacement remains live", recipe.RecipeXaml);

        var cleared = recipe.Items.ToArray();
        recipe.Items.Clear();
        var clearBuilds = exporter.BuildCount;
        foreach (var item in cleared)
        {
            item.Text = "Detached after clear";
        }
        Assert.Equal(clearBuilds, exporter.BuildCount);
        Assert.StartsWith("0 items", recipe.LayoutSummary);
        Assert.DoesNotContain("<Grid", recipe.RecipeXaml);
    }

    [Fact]
    public async Task Invalid_edit_removes_stale_preview_and_recovers_after_correction()
    {
        var (recipe, exporter, _) = FlexRecipeExporterTests.CreateRecipe();
        await recipe.PreviewRecipeCommand.Execute();
        recipe.Items[0].Text = "bad\0text";
        Assert.Empty(recipe.RecipeXaml);
        Assert.Contains("Check item text", recipe.RecipeError);
        Assert.True(recipe.IsRecipeOpen);

        recipe.Items[0].Text = "Fixed";
        Assert.Empty(recipe.RecipeError);
        Assert.Equal(exporter.Build(recipe), recipe.RecipeXaml);
        Assert.Contains("Fixed", recipe.RecipeXaml);
    }

    [Fact]
    public async Task Unexpected_preview_failure_is_readable_and_reopening_retries()
    {
        var exporter = new ObservedExporter { BuildFailure = new InvalidOperationException("Unavailable") };
        var recipe = new MainPageViewModel(exporter, new PanelBoss(), new TestFavoriteStore());
        Assert.Empty(recipe.RecipeXaml);
        Assert.Contains("preview unavailable", recipe.RecipeError);

        exporter.BuildFailure = null;
        await recipe.PreviewRecipeCommand.Execute();
        Assert.Empty(recipe.RecipeError);
        Assert.NotEmpty(recipe.RecipeXaml);
        Assert.False(recipe.IsRecipeBusy);
    }

    [Fact]
    public async Task Copy_disables_reentry_releases_busy_and_detects_edits_while_clipboard_waits()
    {
        var exporter = new ObservedExporter();
        var recipe = new MainPageViewModel(exporter, new PanelBoss(), new TestFavoriteStore());
        exporter.CopyCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recipe.IsExportBusy = true; // The physical RichButton's TwoWay busy write.
        var copy = recipe.ExportCommand.Execute();
        Assert.True(recipe.IsExportBusy);
        Assert.False(recipe.ExportCommand.CanExecute());
        await recipe.ExportCommand.Execute();
        Assert.Equal(1, exporter.CopyCount);

        recipe.Items[0].Text = "Changed while copying";
        exporter.CopyCompletion.SetResult();
        await copy;
        Assert.False(recipe.IsExportBusy);
        Assert.True(recipe.ExportCommand.CanExecute());
        Assert.Contains("Copy again", recipe.ExportStatus);

        exporter.CopyCompletion = null;
        await recipe.ExportCommand.Execute();
        Assert.StartsWith("Copied 5 items", recipe.ExportStatus);
        recipe.Items[0].Grow = 1;
        Assert.DoesNotContain("Copied", recipe.ExportStatus);
    }

    [Fact]
    public async Task Copy_failure_always_releases_physical_button_busy_state()
    {
        var (recipe, _, clipboard) = FlexRecipeExporterTests.CreateRecipe();
        clipboard.Failure = new InvalidOperationException("Clipboard busy");
        recipe.IsExportBusy = true;
        await recipe.ExportCommand.Execute();
        Assert.False(recipe.IsExportBusy);
        Assert.True(recipe.ExportCommand.CanExecute());

        clipboard.Failure = null;
        recipe.Items[0].Text = "bad\0text";
        recipe.IsExportBusy = true;
        await recipe.ExportCommand.Execute();
        Assert.False(recipe.IsExportBusy);
        Assert.True(recipe.ExportCommand.CanExecute());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Editors_are_exclusive_and_done_returns_default_chrome_in_both_orientations(bool sidePanels)
    {
        var (recipe, _, _) = FlexRecipeExporterTests.CreateRecipe();
        var host = AttachPanels(recipe.ActivePanelBoss);
        await recipe.ActivePanelBoss.SetUseSidePanelLayoutAsync(sidePanels);
        var headerPanels = sidePanels ? host.LeftSelectorPanels : host.TopHeaderPanels;
        var inputPanels = sidePanels ? host.RightSelectorPanels : host.BottomInputPanels;

        recipe.IsContainerBusy = true;
        await recipe.ToggleContainerControlsCommand.Execute();
        AssertOnlyVisible(headerPanels, MainPageViewModel.ContainerControlsPanelName);
        AssertOnlyVisible(inputPanels, "DefaultInput");
        Assert.True(recipe.IsContainerOpen);
        Assert.False(recipe.IsContainerBusy);

        await recipe.EditItemCommand.Execute(recipe.Items[2]);
        Assert.Same(recipe.Items[2], recipe.SelectedItem);
        Assert.True(recipe.Items[2].IsSelected);
        Assert.True(recipe.IsInspectorOpen);
        Assert.False(recipe.IsContainerOpen);
        Assert.False(recipe.IsInspectorBusy);
        AssertOnlyVisible(headerPanels, "DefaultHeader");
        AssertOnlyVisible(inputPanels, MainPageViewModel.InspectorPanelName);

        recipe.IsRecipeBusy = true;
        await recipe.PreviewRecipeCommand.Execute();
        Assert.True(recipe.IsRecipeOpen);
        Assert.False(recipe.IsInspectorOpen);
        Assert.False(recipe.IsRecipeBusy);
        AssertOnlyVisible(inputPanels, MainPageViewModel.RecipePanelName);

        recipe.IsRecipeBusy = true;
        await recipe.CloseRecipeCommand.Execute();
        Assert.False(recipe.IsRecipeOpen);
        Assert.False(recipe.IsRecipeBusy);
        AssertOnlyVisible(inputPanels, "DefaultInput");

        await recipe.EditItemCommand.Execute(recipe.Items[0]);
        recipe.IsRemoveBusy = true;
        await recipe.RemoveSelectedCommand.Execute();
        Assert.Null(recipe.SelectedItem);
        Assert.False(recipe.IsInspectorOpen);
        Assert.False(recipe.IsRemoveBusy);
        AssertOnlyVisible(inputPanels, "DefaultInput");

        await recipe.PreviewRecipeCommand.Execute();
        await recipe.ToggleContainerControlsCommand.Execute();
        Assert.False(recipe.IsRecipeOpen);
        AssertOnlyVisible(inputPanels, "DefaultInput");
        AssertOnlyVisible(headerPanels, MainPageViewModel.ContainerControlsPanelName);

        recipe.IsResetBusy = true;
        await recipe.ResetCommand.Execute();
        Assert.False(recipe.IsContainerOpen);
        Assert.False(recipe.IsInspectorOpen);
        Assert.False(recipe.IsRecipeOpen);
        Assert.False(recipe.IsResetBusy);
        AssertOnlyVisible(headerPanels, "DefaultHeader");
        AssertOnlyVisible(inputPanels, "DefaultInput");
    }

    [Fact]
    public async Task Failed_editor_transition_releases_busy_and_the_operation_gate_for_retry()
    {
        var (recipe, _, _) = FlexRecipeExporterTests.CreateRecipe();
        var host = AttachPanels(recipe.ActivePanelBoss);
        EventHandler<View> failTransition = (_, _) => throw new InvalidOperationException("Transition failed");
        recipe.ActivePanelBoss.PanelShown += failTransition;
        await Assert.ThrowsAsync<InvalidOperationException>(() => recipe.EditItemCommand.Execute(recipe.Items[0]));
        Assert.False(recipe.IsInspectorBusy);
        Assert.True(recipe.EditItemCommand.CanExecute(recipe.Items[0]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => recipe.PreviewRecipeCommand.Execute());
        Assert.False(recipe.IsRecipeBusy);
        recipe.ActivePanelBoss.PanelShown -= failTransition;
        await recipe.PreviewRecipeCommand.Execute();
        Assert.True(recipe.IsRecipeOpen);
        AssertOnlyVisible(host.BottomInputPanels, MainPageViewModel.RecipePanelName);
    }

    private static PanelBossBody_DefaultView AttachPanels(PanelBoss boss)
    {
        var host = new PanelBossBody_DefaultView { PanelBossInstance = boss };
        foreach (var lane in new[] { host.TopHeaderPanels, host.LeftSelectorPanels })
        {
            lane.Add(CreatePanel("DefaultHeader", true));
            lane.Add(CreatePanel(MainPageViewModel.ContainerControlsPanelName, false));
        }
        foreach (var lane in new[] { host.BottomInputPanels, host.RightSelectorPanels })
        {
            lane.Add(CreatePanel("DefaultInput", true));
            lane.Add(CreatePanel(MainPageViewModel.InspectorPanelName, false));
            lane.Add(CreatePanel(MainPageViewModel.RecipePanelName, false));
        }
        return host;
    }

    private static View CreatePanel(string name, bool isDefault)
    {
        var panel = new Grid();
        PanelBoss.SetPanelName(panel, name);
        PanelBoss.SetPanelIsVisible(panel, isDefault);
        PanelBoss.SetPanelPriority(panel, isDefault ? 10 : 0);
        return panel;
    }

    private static void AssertOnlyVisible(ObservableCollection<View> panels, string name) =>
        Assert.Equal(name, PanelBoss.GetPanelName(Assert.Single(panels, PanelBoss.GetPanelIsVisible)));

    private sealed class ObservedExporter : IFlexRecipeExporter
    {
        private readonly FlexRecipeExporter _realExporter = new(new TestClipboard());
        public int BuildCount { get; private set; }
        public int CopyCount { get; private set; }
        public Exception? BuildFailure { get; set; }
        public TaskCompletionSource? CopyCompletion { get; set; }

        public string Build(MainPageViewModel recipe)
        {
            BuildCount++;
            if (BuildFailure is not null)
            {
                throw BuildFailure;
            }
            return _realExporter.Build(recipe);
        }

        public Task CopyAsync(MainPageViewModel recipe)
        {
            CopyCount++;
            return CopyCompletion?.Task ?? Task.CompletedTask;
        }
    }
}
