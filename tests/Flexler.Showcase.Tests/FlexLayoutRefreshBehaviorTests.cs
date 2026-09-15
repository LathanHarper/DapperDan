using Flexler.Behaviors;
using Microsoft.Maui.Layouts;

namespace Flexler.Tests;

public sealed class FlexLayoutRefreshBehaviorTests
{
    [Theory]
    [InlineData("Grow")]
    [InlineData("Shrink")]
    [InlineData("Basis")]
    [InlineData("AlignSelf")]
    [InlineData("Order")]
    public void Attached_flex_edit_requests_parent_measure_even_when_child_content_is_unchanged(string property)
    {
        var (layout, _) = CreateLayout();
        var child = new ContentView { Content = new Label { Text = "Same content" } };
        layout.Children.Add(child);
        layout.Behaviors.Add(new FlexLayoutRefreshBehavior());
        layout.ResetInvalidations();

        switch (property)
        {
            case "Grow": FlexLayout.SetGrow(child, 1); break;
            case "Shrink": FlexLayout.SetShrink(child, 0.25f); break;
            case "Basis": FlexLayout.SetBasis(child, new FlexBasis(0.5f, true)); break;
            case "AlignSelf": FlexLayout.SetAlignSelf(child, FlexAlignSelf.End); break;
            case "Order": FlexLayout.SetOrder(child, 2); break;
        }

        // Observe the actual MAUI IView invalidation entry point. No production
        // handler or attached-property callback is replaced by a test stub.
        Assert.Equal(1, layout.MeasureInvalidations);
        Assert.Equal("Same content", Assert.IsType<Label>(child.Content).Text);
    }

    [Fact]
    public void Children_added_later_are_observed_and_removed_or_cleared_children_are_released()
    {
        var (layout, _) = CreateLayout();
        layout.Behaviors.Add(new FlexLayoutRefreshBehavior());
        var removed = new ContentView();
        var cleared = new ContentView();
        layout.Children.Add(removed);
        layout.Children.Add(cleared);
        layout.ResetInvalidations();
        FlexLayout.SetGrow(removed, 1);
        FlexLayout.SetGrow(cleared, 1);
        Assert.Equal(2, layout.MeasureInvalidations);

        layout.Children.Remove(removed);
        layout.Children.Clear();
        layout.ResetInvalidations();
        FlexLayout.SetGrow(removed, 2);
        FlexLayout.SetGrow(cleared, 2);
        Assert.Equal(0, layout.MeasureInvalidations);
    }

    [Fact]
    public void Detach_stops_observation_and_reattach_subscribes_existing_children_once()
    {
        var (layout, _) = CreateLayout();
        var child = new ContentView();
        layout.Children.Add(child);
        var behavior = new FlexLayoutRefreshBehavior();
        layout.Behaviors.Add(behavior);
        layout.Behaviors.Remove(behavior);
        var laterChild = new ContentView();
        layout.Children.Add(laterChild);
        layout.ResetInvalidations();
        FlexLayout.SetGrow(child, 1);
        FlexLayout.SetGrow(laterChild, 1);
        Assert.Equal(0, layout.MeasureInvalidations);

        layout.Behaviors.Add(behavior);
        layout.ResetInvalidations();
        FlexLayout.SetGrow(child, 2);
        FlexLayout.SetGrow(laterChild, 2);
        Assert.Equal(2, layout.MeasureInvalidations);
    }

    [Fact]
    public void Decorative_state_changes_do_not_request_extra_parent_measure()
    {
        var (layout, _) = CreateLayout();
        var child = new ContentView();
        layout.Children.Add(child);
        layout.Behaviors.Add(new FlexLayoutRefreshBehavior());
        layout.ResetInvalidations();

        child.Opacity = 0.65;
        child.BackgroundColor = Colors.Blue;
        child.InputTransparent = true;

        Assert.Equal(0, layout.MeasureInvalidations);
    }

    private static (ObservedFlexLayout Layout, Grid Parent) CreateLayout()
    {
        var layout = new ObservedFlexLayout();
        // Parent the real FlexLayout so its normal Flex.Item engine is initialized.
        var parent = new Grid();
        parent.Children.Add(layout);
        return (layout, parent);
    }

    private sealed class ObservedFlexLayout : FlexLayout
    {
        public int MeasureInvalidations { get; private set; }

        public void ResetInvalidations() => MeasureInvalidations = 0;

        protected override void InvalidateMeasureOverride()
        {
            MeasureInvalidations++;
            base.InvalidateMeasureOverride();
        }
    }
}
