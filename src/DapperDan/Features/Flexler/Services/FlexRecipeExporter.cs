using System.Globalization;
using System.Text;
using System.Xml;
using Flexler.ViewModels;
using Microsoft.Maui.Converters;
using Microsoft.Maui.Layouts;

namespace Flexler.Services;

public sealed class FlexRecipeExporter(IClipboard clipboard) : IFlexRecipeExporter
{
    private const string MauiNamespace = "http://schemas.microsoft.com/dotnet/2021/maui";

    public string Build(MainPageViewModel recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ValidateGeometry(recipe.PreviewPadding, nameof(recipe.PreviewPadding));
        ValidateGeometry(recipe.PreviewMinimumHeight, nameof(recipe.PreviewMinimumHeight), allowUnset: true);

        var xaml = new StringBuilder();
        using var writer = XmlWriter.Create(xaml, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Entitize
        });

        writer.WriteComment(" Flexler recipe. Destination fonts and available space affect layout. ");
        writer.WriteStartElement("FlexLayout", MauiNamespace);
        WriteEnum(writer, "Direction", recipe.Direction);
        WriteEnum(writer, "Wrap", recipe.Wrap);
        WriteEnum(writer, "JustifyContent", recipe.JustifyContent);
        WriteEnum(writer, "AlignItems", recipe.AlignItems);
        WriteEnum(writer, "AlignContent", recipe.AlignContent);
        writer.WriteAttributeString("Padding", recipe.PreviewPadding.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("MinimumHeightRequest", recipe.PreviewMinimumHeight.ToString(CultureInfo.InvariantCulture));

        foreach (var item in recipe.Items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ValidateFactor(item.Grow, nameof(item.Grow));
            ValidateFactor(item.Shrink, nameof(item.Shrink));
            var basis = FormatBasis(item);

            // Keep the preview's item container and text insets. App-only decoration,
            // selection state and editing controls are deliberately not exported.
            writer.WriteStartElement("Grid", MauiNamespace);
            WriteEnum(writer, "FlexLayout.AlignSelf", item.AlignSelf);
            writer.WriteAttributeString("FlexLayout.Basis", basis);
            writer.WriteAttributeString("FlexLayout.Grow", item.Grow.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("FlexLayout.Shrink", item.Shrink.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("FlexLayout.Order", item.Order.ToString(CultureInfo.InvariantCulture));
            writer.WriteStartElement("Label", MauiNamespace);
            var text = item.Text ?? string.Empty;
            // MAUI removes an attribute escape while parsing, then another leading
            // "{}" during string conversion. Protect an existing escape twice.
            var literal = text.StartsWith("{}", StringComparison.Ordinal)
                ? "{}{}" + text
                : text.AsSpan().TrimStart().StartsWith("{") ? "{}" + text : text;
            writer.WriteAttributeString("Text", literal);
            writer.WriteAttributeString("Margin", "14,8");
            writer.WriteAttributeString("MinimumHeightRequest", "32");
            writer.WriteAttributeString("FontSize", "15");
            writer.WriteAttributeString("VerticalTextAlignment", "Center");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.Flush();
        return xaml.ToString();
    }

    public Task CopyAsync(MainPageViewModel recipe) => clipboard.SetTextAsync(Build(recipe));

    private static void WriteEnum<T>(XmlWriter writer, string name, T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "Choose a supported FlexLayout value.");
        }

        writer.WriteAttributeString(name, value.ToString());
    }

    private static string FormatBasis(FlexItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item.BasisOption);
        var basis = item.BasisOption.Value;
        if (basis == FlexBasis.Auto)
        {
            return "Auto";
        }

        ValidateFactor(basis.Length, nameof(item.Basis));
        return new FlexBasisTypeConverter().ConvertToInvariantString(basis)
            ?? throw new ArgumentException("Choose a supported FlexLayout basis.", nameof(item.Basis));
    }

    private static void ValidateFactor(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Flex values must be finite and nonnegative.");
        }
    }

    private static void ValidateGeometry(double value, string name, bool allowUnset = false)
    {
        if (!double.IsFinite(value) || (value < 0 && !(allowUnset && value == -1)))
        {
            throw new ArgumentOutOfRangeException(name, value, "Layout dimensions must be finite and nonnegative, or -1 when unset.");
        }
    }
}
