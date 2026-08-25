using System.Xml.Linq;

namespace CodeCrafty.DapperDan.Tests;

public sealed class BillboardCanaryContractTests
{
    [Fact]
    public void CanaryPanelLinksDedicatedBillboardPageThroughPrism()
    {
        var root = FindRepositoryRoot();
        var mainPage = XDocument.Load(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "DapperDan",
            "DapperDanPage.xaml"));
        var canaryPanel = Assert.Single(
            mainPage.Descendants(),
            element => GetOptionalAttribute(element, "PanelBoss.PanelName") ==
                "DapperDanWitnessPanel");
        var openButton = Assert.Single(
            canaryPanel.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Canary_OpenBillboardLab");

        Assert.Equal("RichButton", openButton.Name.LocalName);
        Assert.Contains(
            "OpenBillboardCanaryCommand",
            GetOptionalAttribute(openButton, "Command"),
            StringComparison.Ordinal);

        var startup = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "MauiProgram.cs"));
        Assert.Contains(
            "RegisterForNavigation<BillboardCanaryPage, BillboardCanaryViewModel>()",
            startup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PageUsesOneSceneGridAndOneDirectUpperLeftPresenter()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "BillboardCanary",
            "BillboardCanaryPage.xaml");
        var source = File.ReadAllText(pagePath);
        var document = XDocument.Parse(source);
        var pageChild = Assert.Single(document.Root!.Elements());

        Assert.Equal("PanelBossBody_DefaultView", pageChild.Name.LocalName);
        Assert.Contains(
            "ActivePanelBoss",
            GetOptionalAttribute(pageChild, "PanelBossInstance"),
            StringComparison.Ordinal);

        var viewport = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Billboard_Viewport");
        var presenter = Assert.Single(
            viewport.Elements(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Billboard_SignPresenter");

        Assert.Equal("Grid", viewport.Name.LocalName);
        Assert.Equal("Grid", presenter.Name.LocalName);
        Assert.Equal("Start", GetOptionalAttribute(presenter, "HorizontalOptions"));
        Assert.Equal("Start", GetOptionalAttribute(presenter, "VerticalOptions"));
        Assert.DoesNotContain(
            document.Descendants(),
            element => element.Name.LocalName == "AbsoluteLayout");
        Assert.DoesNotContain(
            document.Root!.Attributes(),
            attribute => attribute.IsNamespaceDeclaration &&
                attribute.Value.Contains("Skia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            document.Descendants(),
            element => element.Name.LocalName.Contains("SKCanvas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PresenterUsesOnlyNativePoseAndUncroppedStackedImages()
    {
        var root = FindRepositoryRoot();
        var pageDirectory = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "BillboardCanary");
        var document = XDocument.Load(Path.Combine(
            pageDirectory,
            "BillboardCanaryPage.xaml"));
        var presenter = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Billboard_SignPresenter");
        var images = presenter.Elements()
            .Where(element => element.Name.LocalName == "Image")
            .ToArray();

        Assert.Equal(2, images.Length);
        Assert.Equal(
            ["billboard_canary_b.png", "billboard_canary_a.png"],
            images.Select(image => GetOptionalAttribute(image, "Source")));
        Assert.All(images, image =>
            Assert.Equal("Fill", GetOptionalAttribute(image, "Aspect")));

        var codeBehind = File.ReadAllText(Path.Combine(
            pageDirectory,
            "BillboardCanaryPage.xaml.cs"));
        Assert.Contains("SignPresenter.TranslationX", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SignPresenter.TranslationY", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SignPresenter.Rotation =", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SignPresenter.RotationX =", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SignPresenter.RotationY =", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("SceneCanvas", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain(".Scale =", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicCanaryAssetsExistAndAreDocumented()
    {
        var root = FindRepositoryRoot();
        var imageDirectory = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Resources",
            "Images");

        foreach (var asset in new[]
                 {
                     "billboard_canary_scene.svg",
                     "billboard_canary_a.svg",
                     "billboard_canary_b.svg",
                 })
        {
            Assert.True(File.Exists(Path.Combine(imageDirectory, asset)), asset);
        }

        var provenance = File.ReadAllText(Path.Combine(
            root.FullName,
            "docs",
            "ASSET-PROVENANCE.md"));
        Assert.Contains("Billboard layout canary", provenance, StringComparison.Ordinal);
        Assert.Contains("billboard_canary_scene.svg", provenance, StringComparison.Ordinal);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DapperDan.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the DapperDan repository root.");
    }

    private static string? GetOptionalAttribute(XElement element, string name) =>
        element.Attributes().SingleOrDefault(
            attribute => attribute.Name.LocalName == name)?.Value;
}
