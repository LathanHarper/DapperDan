using System.Globalization;
using Flexler.Models;
using Flexler.PanelBossKit;
using Flexler.Services;
using Flexler.ViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Layouts;

namespace Flexler.Tests;

public sealed class FlexRecipeExporterTests
{
    [Theory]
    [InlineData("plain text")]
    [InlineData("{Binding Secret}")]
    [InlineData("{}already escaped")]
    [InlineData("{}{}multiple literal braces")]
    [InlineData("  {Binding Secret}")]
    [InlineData("  {}literal braces")]
    [InlineData("{unclosed")]
    [InlineData("quotes: \" '& < >")]
    [InlineData("line one\r\nline two\nline three\rtab\there")]
    [InlineData("  leading and trailing  ")]
    [InlineData("Aloha 🌺 — 日本語 e\u0301")]
    [InlineData("")]
    [InlineData(null)]
    public void Text_survives_the_real_maui_xaml_loader(string? text)
    {
        var (recipe, exporter, _) = CreateRecipe();
        recipe.Items.Clear();
        recipe.Items.Add(new FlexItemViewModel(1, text!, recipe.BasisOptions[0]));

        var layout = Load(exporter.Build(recipe));

        var container = Assert.IsType<Grid>(Assert.Single(layout.Children));
        var label = Assert.IsType<Label>(Assert.Single(container.Children));
        Assert.Equal(text ?? string.Empty, label.Text);
        label.BindingContext = new { Secret = "This must never replace literal text." };
        Assert.Equal(text ?? string.Empty, label.Text);
    }

    [Fact]
    public void Export_contains_only_recipe_items_and_preserves_item_geometry()
    {
        var (recipe, exporter, _) = CreateRecipe();
        recipe.PreviewPadding = 14;
        recipe.PreviewMinimumHeight = -1;

        var layout = Load(exporter.Build(recipe));

        Assert.Equal(recipe.Items.Count, layout.Children.Count);
        Assert.Equal(new Thickness(14), layout.Padding);
        Assert.Equal(-1, layout.MinimumHeightRequest);
        foreach (var child in layout.Children)
        {
            var label = Assert.IsType<Label>(Assert.Single(Assert.IsType<Grid>(child).Children));
            Assert.Equal(new Thickness(14, 8), label.Margin);
            Assert.Equal(32, label.MinimumHeightRequest);
            Assert.Equal(15, label.FontSize);
            Assert.Equal(TextAlignment.Center, label.VerticalTextAlignment);
        }

        recipe.Items.Clear();
        Assert.Empty(Load(exporter.Build(recipe)).Children);
    }

    [Fact]
    public void Every_container_enum_roundtrips_through_maui()
    {
        var (recipe, exporter, _) = CreateRecipe();
        foreach (var value in Enum.GetValues<FlexDirection>())
        {
            recipe.Direction = value;
            Assert.Equal(value, Load(exporter.Build(recipe)).Direction);
        }
        foreach (var value in Enum.GetValues<FlexWrap>())
        {
            recipe.Wrap = value;
            Assert.Equal(value, Load(exporter.Build(recipe)).Wrap);
        }
        foreach (var value in Enum.GetValues<FlexJustify>())
        {
            recipe.JustifyContent = value;
            Assert.Equal(value, Load(exporter.Build(recipe)).JustifyContent);
        }
        foreach (var value in Enum.GetValues<FlexAlignItems>())
        {
            recipe.AlignItems = value;
            Assert.Equal(value, Load(exporter.Build(recipe)).AlignItems);
        }
        foreach (var value in Enum.GetValues<FlexAlignContent>())
        {
            recipe.AlignContent = value;
            Assert.Equal(value, Load(exporter.Build(recipe)).AlignContent);
        }
    }

    [Fact]
    public void Every_item_option_roundtrips_under_a_comma_decimal_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var (recipe, exporter, _) = CreateRecipe();
            var item = recipe.Items[0];
            foreach (var value in Enum.GetValues<FlexAlignSelf>())
            {
                item.AlignSelf = value;
                Assert.Equal(value, FlexLayout.GetAlignSelf(FirstItem(exporter, recipe)));
            }
            foreach (var value in recipe.BasisOptions)
            {
                item.BasisOption = value;
                Assert.Equal(value.Value, FlexLayout.GetBasis(FirstItem(exporter, recipe)));
            }
            foreach (var value in recipe.FlexFactorOptions)
            {
                item.Grow = value;
                item.Shrink = value;
                var child = FirstItem(exporter, recipe);
                Assert.Equal(value, FlexLayout.GetGrow(child));
                Assert.Equal(value, FlexLayout.GetShrink(child));
            }
            foreach (var value in recipe.OrderOptions)
            {
                item.Order = value;
                Assert.Equal(value, FlexLayout.GetOrder(FirstItem(exporter, recipe)));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Basis_uses_the_actual_value_instead_of_an_untrusted_display_token()
    {
        var (recipe, exporter, _) = CreateRecipe();
        recipe.Items[0].BasisOption = new FlexBasisOption("wrong \" {Binding Bad}", new FlexBasis(0.375f, true));
        Assert.Equal(new FlexBasis(0.375f, true), FlexLayout.GetBasis(FirstItem(exporter, recipe)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Invalid_factors_are_rejected_before_copy(float value)
    {
        var (recipe, exporter, _) = CreateRecipe();
        recipe.Items[0].Grow = value;
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.Build(recipe));
        recipe.Items[0].Grow = 0;
        recipe.Items[0].Shrink = value;
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.Build(recipe));
    }

    [Fact]
    public void Undefined_enums_and_invalid_xml_characters_are_rejected()
    {
        var (recipe, exporter, _) = CreateRecipe();
        recipe.Direction = (FlexDirection)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.Build(recipe));
        recipe.Direction = FlexDirection.Row;
        recipe.Items[0].AlignSelf = (FlexAlignSelf)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => exporter.Build(recipe));
        recipe.Items[0].AlignSelf = FlexAlignSelf.Auto;
        recipe.Items[0].Text = "bad\0text";
        Assert.Throws<ArgumentException>(() => exporter.Build(recipe));
    }

    [Fact]
    public async Task Copy_passes_exact_recipe_to_clipboard()
    {
        var (recipe, exporter, clipboard) = CreateRecipe();
        await exporter.CopyAsync(recipe);
        Assert.Equal(exporter.Build(recipe), clipboard.Text);
    }

    internal static (MainPageViewModel Recipe, FlexRecipeExporter Exporter, TestClipboard Clipboard) CreateRecipe()
    {
        var clipboard = new TestClipboard();
        var exporter = new FlexRecipeExporter(clipboard);
        return (new MainPageViewModel(exporter, new PanelBoss(), new TestFavoriteStore()), exporter, clipboard);
    }

    private static FlexLayout Load(string xaml) => new FlexLayout().LoadFromXaml(xaml);

    private static BindableObject FirstItem(FlexRecipeExporter exporter, MainPageViewModel recipe) =>
        Assert.IsType<Grid>(Load(exporter.Build(recipe)).Children[0]);
}

internal sealed class TestClipboard : IClipboard
{
    public string? Text { get; private set; }
    public Exception? Failure { get; set; }
    public bool HasText => !string.IsNullOrEmpty(Text);
    public event EventHandler<EventArgs>? ClipboardContentChanged { add { } remove { } }
    public Task<string?> GetTextAsync() => Task.FromResult(Text);
    public Task SetTextAsync(string? text)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }
        Text = text;
        return Task.CompletedTask;
    }
}
