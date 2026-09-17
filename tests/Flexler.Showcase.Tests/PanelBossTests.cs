using System.Collections.ObjectModel;
using Flexler.PanelBossKit;
using Flexler.PanelBossKit.Views;

namespace Flexler.Tests;

public sealed class PanelBossTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Closing_a_hidden_input_panel_preserves_the_active_sibling(
        bool useSidePanels,
        bool showRecipe)
    {
        var fixture = new InputLane(useSidePanels);
        var active = showRecipe ? fixture.Recipe : fixture.Inspector;
        var hidden = showRecipe ? fixture.Inspector : fixture.Recipe;
        await fixture.Boss.InputPanels_ActivatePanelByName(PanelBoss.GetPanelName(active));

        await fixture.Boss.InputPanels_DeActivatePanelByName(PanelBoss.GetPanelName(hidden));

        AssertOnlyVisible(fixture.Panels, active);
        Assert.Equal(useSidePanels, fixture.Host.UseSidePanelLayout);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Closing_an_active_input_panel_restores_the_default_exclusively(
        bool useSidePanels)
    {
        var fixture = new InputLane(useSidePanels);
        await fixture.Boss.InputPanels_ActivatePanelByName(PanelBoss.GetPanelName(fixture.Inspector));
        // Attached visibility is public. A stale sibling state must not survive
        // restoration and reserve workspace clearance alongside the default.
        PanelBoss.SetPanelIsVisible(fixture.Recipe, true);

        await fixture.Boss.InputPanels_DeActivatePanelByName(PanelBoss.GetPanelName(fixture.Inspector));

        AssertOnlyVisible(fixture.Panels, fixture.Default);
        await fixture.Boss.InputPanels_DeActivatePanelByName(PanelBoss.GetPanelName(fixture.Inspector));
        AssertOnlyVisible(fixture.Panels, fixture.Default);
    }

    private static void AssertOnlyVisible(IEnumerable<View> panels, View expected)
    {
        Assert.Same(expected, Assert.Single(panels, PanelBoss.GetPanelIsVisible));
        foreach (var panel in panels)
        {
            var shouldBeVisible = ReferenceEquals(panel, expected);
            Assert.Equal(shouldBeVisible, panel.IsVisible);
            Assert.Equal(!shouldBeVisible, panel.InputTransparent);
        }
    }

    private sealed class InputLane
    {
        public InputLane(bool useSidePanels)
        {
            Host = new PanelBossBody_DefaultView { UseSidePanelLayout = useSidePanels };
            Panels = useSidePanels ? Host.RightSelectorPanels : Host.BottomInputPanels;
            Default = CreatePanel("Default", priority: 10, isVisible: true);
            Inspector = CreatePanel("Inspector", priority: 0, isVisible: false);
            Recipe = CreatePanel("Recipe", priority: 0, isVisible: false);
            Panels.Add(Default);
            Panels.Add(Inspector);
            Panels.Add(Recipe);
            Host.PanelBossInstance = Boss;
        }

        public PanelBoss Boss { get; } = new();
        public PanelBossBody_DefaultView Host { get; }
        public ObservableCollection<View> Panels { get; }
        public View Default { get; }
        public View Inspector { get; }
        public View Recipe { get; }

        private static View CreatePanel(string name, int priority, bool isVisible)
        {
            var panel = new Grid();
            PanelBoss.SetPanelName(panel, name);
            PanelBoss.SetPanelPriority(panel, priority);
            PanelBoss.SetPanelIsVisible(panel, isVisible);
            PanelBoss.SetPanelTransitionIn(panel, PanelTransition.None);
            PanelBoss.SetPanelTransitionOut(panel, PanelTransition.None);
            return panel;
        }
    }
}
