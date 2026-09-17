using System.ComponentModel;

namespace Flexler.Behaviors;

/// <summary>
/// Keeps live FlexLayout edits visible when a child's intrinsic size is unchanged.
/// MAUI 10.0.20 invalidates only the child for attached Flex properties; WinUI can
/// then skip arranging its parent. Request the layout's own measure pass as well.
/// </summary>
public sealed class FlexLayoutRefreshBehavior : Behavior<FlexLayout>
{
    private readonly HashSet<BindableObject> _children = [];
    private FlexLayout? _layout;

    protected override void OnAttachedTo(FlexLayout bindable)
    {
        base.OnAttachedTo(bindable);
        _layout = bindable;
        bindable.ChildAdded += OnChildAdded;
        bindable.ChildRemoved += OnChildRemoved;
        foreach (var child in bindable.Children.OfType<BindableObject>())
        {
            Observe(child);
        }
    }

    protected override void OnDetachingFrom(FlexLayout bindable)
    {
        bindable.ChildAdded -= OnChildAdded;
        bindable.ChildRemoved -= OnChildRemoved;
        foreach (var child in _children)
        {
            child.PropertyChanged -= OnChildPropertyChanged;
        }
        _children.Clear();
        _layout = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnChildAdded(object? sender, ElementEventArgs e) => Observe(e.Element);

    private void OnChildRemoved(object? sender, ElementEventArgs e)
    {
        if (_children.Remove(e.Element))
        {
            e.Element.PropertyChanged -= OnChildPropertyChanged;
        }
    }

    private void Observe(BindableObject child)
    {
        if (_children.Add(child))
        {
            child.PropertyChanged += OnChildPropertyChanged;
        }
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == FlexLayout.GrowProperty.PropertyName
            || e.PropertyName == FlexLayout.ShrinkProperty.PropertyName
            || e.PropertyName == FlexLayout.BasisProperty.PropertyName
            || e.PropertyName == FlexLayout.AlignSelfProperty.PropertyName
            || e.PropertyName == FlexLayout.OrderProperty.PropertyName)
        {
            // Invalidation schedules the native pass after this property's setter
            // finishes updating its Flex.Item; no forced synchronous layout is needed.
            ((IView?)_layout)?.InvalidateMeasure();
        }
    }
}
