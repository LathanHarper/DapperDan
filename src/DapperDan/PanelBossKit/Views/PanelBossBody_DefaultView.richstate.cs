using System.ComponentModel;
using System.Reflection;

namespace CodeCrafty.DapperDan.PanelBossKit.Views;

public partial class PanelBossBody_DefaultView
{
    private readonly Dictionary<Type, PropertyInfo> _richVisualStateProperties = [];
    private readonly HashSet<VisualElement> _richStateControls = [];

    private void RegisterRichStateControls()
    {
        if (!IsLoaded)
            return;

        foreach (var control in FindRichStateControls(this))
        {
            if (control is CodeCrafty.DapperDan.Controls.TapViewBase tapView)
            {
                RecordLegacyTapViewRegistration(
                    tapView,
                    TapViewFeatures.RichState);
            }

            if (!_richStateControls.Add(control))
            {
                if (control is CodeCrafty.DapperDan.Controls.TapViewBase existingTapView)
                {
                    MarkLegacyTapViewFeatureApplied(
                        existingTapView,
                        TapViewFeatures.RichState);
                }

                continue;
            }

            control.PropertyChanged += OnRichStateControlPropertyChanged;
            control.Unloaded += OnRichStateControlUnloaded;
            ApplyCurrentRichState(control);

            if (control is CodeCrafty.DapperDan.Controls.TapViewBase registeredTapView)
            {
                MarkLegacyTapViewFeatureApplied(
                    registeredTapView,
                    TapViewFeatures.RichState);
            }
        }
    }

    private IEnumerable<VisualElement> FindRichStateControls(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is VisualElement control &&
                PanelBoss.GetIWantRichState(control) &&
                GetRichVisualStateProperty(control.GetType()) is not null)
            {
                yield return control;
            }

            foreach (var descendant in FindRichStateControls(child))
                yield return descendant;
        }
    }

    private void OnRichStateControlPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is VisualElement control && e.PropertyName == "RichVisualState")
            ApplyCurrentRichState(control);
    }

    private void OnRichStateControlUnloaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement control)
            return;

        // Handler and rich-state churn can deliver an old native Unloaded
        // after the same virtual control is already live again.
        if (control.IsLoaded)
            return;

        UnregisterRichStateControl(control);
        _richStateControls.Remove(control);

        if (control is CodeCrafty.DapperDan.Controls.TapViewBase tapView)
            ForgetLegacyTapViewFeature(tapView, TapViewFeatures.RichState);
    }

    private void ApplyCurrentRichState(VisualElement control)
    {
        var property = GetRichVisualStateProperty(control.GetType());
        var state = property?.GetValue(control) as string;

        if (string.IsNullOrWhiteSpace(state) || control is not IVisualTreeElement root)
            return;

        foreach (var child in root.GetVisualChildren())
            ApplyRichState(child, state);
    }

    private static void ApplyRichState(IVisualTreeElement element, string state)
    {
        if (element is VisualElement visualElement &&
            !CodeCrafty.DapperDan.Controls.TapViewBase.GetRichVisualStateOptOut(visualElement))
        {
            VisualStateManager.GoToState(visualElement, state);
        }

        if (element is BindableObject bindable &&
            !CodeCrafty.DapperDan.Controls.TapViewBase.GetCascadeRichState(bindable))
        {
            return;
        }

        foreach (var child in element.GetVisualChildren())
            ApplyRichState(child, state);
    }

    private PropertyInfo GetRichVisualStateProperty(Type controlType)
    {
        if (_richVisualStateProperties.TryGetValue(controlType, out var cachedProperty))
            return cachedProperty;

        var property = controlType.GetProperty(
            "RichVisualState",
            BindingFlags.Instance | BindingFlags.Public);

        if (property?.PropertyType != typeof(string) || !property.CanRead)
            property = null;

        _richVisualStateProperties[controlType] = property;
        return property;
    }

    private void UnregisterRichStateControls()
    {
        foreach (var control in _richStateControls)
            UnregisterRichStateControl(control);

        _richStateControls.Clear();
    }

    private void UnregisterRichStateControl(VisualElement control)
    {
        control.PropertyChanged -= OnRichStateControlPropertyChanged;
        control.Unloaded -= OnRichStateControlUnloaded;
    }
}
