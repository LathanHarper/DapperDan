using System.Xml;
using Microsoft.Maui.Layouts;

namespace Flexler.Models;

public sealed record FavoriteRecipe(Guid Id, string Name, DateTimeOffset SavedAt, FlexRecipeSnapshot Recipe)
{
    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Recipe is null)
            throw new InvalidDataException("Invalid favorite metadata.");
        Recipe.Validate();
    }
}

public sealed record FlexRecipeSnapshot(
    FlexDirection Direction, FlexWrap Wrap, FlexJustify JustifyContent,
    FlexAlignItems AlignItems, FlexAlignContent AlignContent,
    double Padding, double MinimumHeight, IReadOnlyList<FlexItemSnapshot> Items)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Direction) || !Enum.IsDefined(Wrap) || !Enum.IsDefined(JustifyContent)
            || !Enum.IsDefined(AlignItems) || !Enum.IsDefined(AlignContent)
            || !double.IsFinite(Padding) || Padding < 0
            || !double.IsFinite(MinimumHeight) || (MinimumHeight < 0 && MinimumHeight != -1)
            || Items is null)
            throw new InvalidDataException("Invalid favorite layout.");

        foreach (var item in Items)
        {
            if (item is null || item.Text is null || !Enum.IsDefined(item.AlignSelf)
                || !float.IsFinite(item.Grow) || item.Grow < 0
                || !float.IsFinite(item.Shrink) || item.Shrink < 0
                || (item.BasisLength is { } length && (!float.IsFinite(length) || length < 0)))
                throw new InvalidDataException("Invalid favorite item.");
            try { XmlConvert.VerifyXmlChars(item.Text); }
            catch (XmlException exception) { throw new InvalidDataException("Invalid favorite text.", exception); }
        }
    }
}

// null length represents Auto without serializing FlexBasis.Auto's NaN value.
public sealed record FlexItemSnapshot(
    string Text, FlexAlignSelf AlignSelf, float Grow, float Shrink, int Order,
    float? BasisLength, bool BasisIsRelative);
