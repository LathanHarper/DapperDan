namespace CodeCrafty.DapperDan.Controls;

internal static class BindablePropertyValue
{
    public static bool GetBool(BindableObject bindable, BindableProperty property, bool fallback)
    {
        return bindable.GetValue(property) is bool value
            ? value
            : fallback;
    }

    public static void SetBool(BindableObject bindable, BindableProperty property, bool value)
    {
        bindable.SetValue(property, value);
    }

    public static string GetString(BindableObject bindable, BindableProperty property, string fallback = "")
    {
        return bindable.GetValue(property) is string value
            ? value
            : fallback;
    }

    public static void SetString(BindableObject bindable, BindableProperty property, string value)
    {
        bindable.SetValue(property, value);
    }

    public static T GetValue<T>(BindableObject bindable, BindableProperty property, T fallback = default)
    {
        return bindable.GetValue(property) is T value
            ? value
            : fallback;
    }

    public static void SetValue<T>(BindableObject bindable, BindableProperty property, T value)
    {
        bindable.SetValue(property, value);
    }
}
