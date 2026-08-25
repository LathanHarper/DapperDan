using System.Xml.Linq;

namespace CodeCrafty.DapperDan.Tests;

public sealed class RotationCanaryContractTests
{
    private static readonly string[] SliderAutomationIds =
    [
        "DapperDan_Rotation_Slider",
        "DapperDan_RotationX_Slider",
        "DapperDan_RotationY_Slider",
    ];

    private static readonly string[] SwitchAutomationIds =
    [
        "DapperDan_Rotation_UnderlaySwitch",
        "DapperDan_Rotation_ZIndexSwitch",
        "DapperDan_Rotation_ClipSwitch",
        "DapperDan_Rotation_OpaqueSwitch",
        "DapperDan_Rotation_StackSwitch",
    ];

    [Fact]
    public void CanaryPanelOpensDedicatedRotationPageThroughPrism()
    {
        var root = FindRepositoryRoot();
        var mainPage = XDocument.Load(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "DapperDan",
            "DapperDanPage.xaml"));
        var witnessPanel = Assert.Single(
            mainPage.Descendants(),
            element => GetOptionalAttribute(element, "PanelBoss.PanelName") ==
                "DapperDanWitnessPanel");
        var openButton = Assert.Single(
            witnessPanel.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Canary_OpenRotationLab");

        Assert.Equal("RichButton", openButton.Name.LocalName);
        Assert.Contains(
            "OpenRotationCanaryCommand",
            GetOptionalAttribute(openButton, "Command"),
            StringComparison.Ordinal);

        var startup = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "MauiProgram.cs"));
        Assert.Contains(
            "RegisterForNavigation<RotationCanaryPage, RotationCanaryViewModel>()",
            startup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RotationLabUsesOneTargetAndThreeDirectMauiTransforms()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "RotationCanary",
            "RotationCanaryPage.xaml");
        var source = File.ReadAllText(pagePath);
        var document = XDocument.Parse(source);
        var target = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Rotation_Target");

        Assert.Equal("ContentView", target.Name.LocalName);
        Assert.Contains("{Binding Rotation}", GetOptionalAttribute(target, "Rotation"), StringComparison.Ordinal);
        Assert.Contains("{Binding RotationX}", GetOptionalAttribute(target, "RotationX"), StringComparison.Ordinal);
        Assert.Contains("{Binding RotationY}", GetOptionalAttribute(target, "RotationY"), StringComparison.Ordinal);
        Assert.DoesNotContain(
            document.Root!.Attributes(),
            attribute => attribute.IsNamespaceDeclaration &&
                attribute.Value.Contains("Skia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            document.Descendants(),
            element => element.Name.LocalName.Contains("SKCanvas", StringComparison.OrdinalIgnoreCase));

        foreach (var automationId in SliderAutomationIds)
        {
            var slider = Assert.Single(
                document.Descendants(),
                element => GetOptionalAttribute(element, "AutomationId") == automationId);
            Assert.Equal("Slider", slider.Name.LocalName);
            Assert.Equal("-45", GetOptionalAttribute(slider, "Minimum"));
            Assert.Equal("45", GetOptionalAttribute(slider, "Maximum"));
        }
    }

    [Fact]
    public void RotationLabUsesPanelBossHostAndKeepsFullSweepInsideStage()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "RotationCanary",
            "RotationCanaryPage.xaml");
        var document = XDocument.Load(pagePath);
        var pageChild = Assert.Single(document.Root!.Elements());

        Assert.Equal("PanelBossBody_DefaultView", pageChild.Name.LocalName);

        var stage = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Rotation_Stage");
        var target = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Rotation_Target");
        var stageWidth = double.Parse(GetOptionalAttribute(stage, "WidthRequest")!);
        var stageHeight = double.Parse(GetOptionalAttribute(stage, "HeightRequest")!);
        var targetWidth = double.Parse(GetOptionalAttribute(target, "WidthRequest")!);
        var targetHeight = double.Parse(GetOptionalAttribute(target, "HeightRequest")!);
        var targetDiagonal = Math.Sqrt(
            (targetWidth * targetWidth) + (targetHeight * targetHeight));

        Assert.True(stageWidth > targetDiagonal);
        Assert.True(stageHeight > targetDiagonal);
        Assert.Equal("Center", GetOptionalAttribute(target, "HorizontalOptions"));
        Assert.Equal("Center", GetOptionalAttribute(target, "VerticalOptions"));
    }

    [Fact]
    public void RotationLabAddsEachRiskyCompositionIngredientIndependently()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "RotationCanary",
            "RotationCanaryPage.xaml");
        var document = XDocument.Load(pagePath);

        foreach (var automationId in SwitchAutomationIds)
        {
            Assert.Single(
                document.Descendants(),
                element => GetOptionalAttribute(element, "AutomationId") == automationId);
        }

        var target = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Rotation_Target");
        var images = target.Descendants()
            .Where(element => element.Name.LocalName == "Image")
            .ToArray();

        Assert.Equal(2, images.Length);
        Assert.Equal(
            ["rotation_canary_a.png", "rotation_canary_b.png"],
            images.Select(image => GetOptionalAttribute(image, "Source")));
        Assert.All(images, image =>
            Assert.Equal("Fill", GetOptionalAttribute(image, "Aspect")));
        Assert.True(File.Exists(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Resources",
            "Images",
            "rotation_canary_a.svg")));
        Assert.True(File.Exists(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Resources",
            "Images",
            "rotation_canary_b.svg")));
        var crossFadeButton = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "AutomationId") ==
                "DapperDan_Rotation_CrossFade");
        Assert.Contains(
            "Mode=OneWay",
            GetOptionalAttribute(crossFadeButton, "IsBusy"),
            StringComparison.Ordinal);

        var codeBehind = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "RotationCanary",
            "RotationCanaryPage.xaml.cs"));
        Assert.Contains("OnDisappearing", codeBehind, StringComparison.Ordinal);
        Assert.Contains("CancelCrossFade", codeBehind, StringComparison.Ordinal);
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
