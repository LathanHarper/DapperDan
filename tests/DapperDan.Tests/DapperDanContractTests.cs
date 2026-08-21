using System.Xml.Linq;

namespace CodeCrafty.DapperDan.Tests;

public sealed class DapperDanContractTests
{
    private static readonly string[] PanelBossLanes =
    [
        "NonVisualControls",
        "TopHeaderPagePanels",
        "TopHeaderPanels",
        "ContentPanels",
        "LeftSelectorPanels",
        "RightSelectorPanels",
        "MenuPanels",
        "BottomInputPanels",
        "BottomStatusPanels",
        "FullScreenPopupPanels"
    ];

    [Fact]
    public void AppSourceContainsNoTabBarOrAbsoluteWindowsPaths()
    {
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root.FullName, "src", "DapperDan");
        var sourceFiles = Directory
            .EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        foreach (var sourceFile in sourceFiles)
        {
            var text = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("AppTabBar", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"[A-Za-z]:\\", text);
        }
    }

    [Fact]
    public void DapperDanLeavesNavigationPageRegistrationToPrism()
    {
        var root = FindRepositoryRoot();
        var sourcePath = Path.Combine(root.FullName, "src", "DapperDan", "MauiProgram.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.Contains(
            ".CreateWindow(\"NavigationPage/DapperDanPage\")",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "RegisterForNavigation<NavigationPage>",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DapperDanDeclaresEachPanelBossLaneExactlyOnce()
    {
        var document = LoadDapperDanPage();
        var declaredElements = document
            .Descendants()
            .Select(element => element.Name.LocalName)
            .ToArray();

        foreach (var lane in PanelBossLanes)
        {
            Assert.Single(declaredElements, name =>
                string.Equals(
                    name,
                    $"PanelBossBody_DefaultView.{lane}",
                    StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DapperDanOwnsFiveDirectPageActionsInFixedBottomInputPanel()
    {
        var document = LoadDapperDanPage();
        var bottomInputLane = GetLane(document, "BottomInputPanels");
        var actionGrid = Assert.Single(
            bottomInputLane.Descendants(),
            element => element.Name.LocalName == "Grid" && DirectRichButtons(element).Count == 5);
        var actionButtons = DirectRichButtons(actionGrid);

        Assert.Same(bottomInputLane, actionGrid.Parent);
        Assert.Equal("True", GetPanelAttribute(actionGrid, "PanelIsVisible"));
        Assert.Equal("None", GetPanelAttribute(actionGrid, "PanelTransitionIn"));
        Assert.Equal("None", GetPanelAttribute(actionGrid, "PanelTransitionOut"));
        Assert.True(int.Parse(GetPanelAttribute(actionGrid, "PanelPriority")) > 0);
        Assert.Equal(5, CountGridColumns(actionGrid));
        Assert.Equal(
            Enumerable.Range(0, 5),
            actionButtons.Select(GetGridColumn).Order());

        Assert.All(actionButtons, button =>
        {
            Assert.Contains("SelectPageActionCommand", GetAttribute(button, "Command"), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(GetAttribute(button, "AutomationId")));
        });
        Assert.Equal(
            actionButtons.Count,
            actionButtons
                .Select(button => GetAttribute(button, "AutomationId"))
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Single(
            actionButtons,
            button => GetAttribute(button, "AutomationId").EndsWith("More", StringComparison.Ordinal));
    }

    [Fact]
    public void DapperDanKeepsMorePanelInContentAndOutOfBottomInput()
    {
        var document = LoadDapperDanPage();
        var contentLane = GetLane(document, "ContentPanels");
        var bottomInputLane = GetLane(document, "BottomInputPanels");

        var morePanel = Assert.Single(contentLane.Elements(), IsMorePanel);

        Assert.EndsWith("MorePanel", GetPanelAttribute(morePanel, "PanelName"), StringComparison.Ordinal);
        Assert.DoesNotContain(bottomInputLane.Descendants(), IsMorePanel);
    }

    [Fact]
    public void AndroidManifestDeclaresRichButtonHapticPermission()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Platforms",
            "Android",
            "AndroidManifest.xml");
        var document = XDocument.Load(manifestPath);
        XNamespace android = "http://schemas.android.com/apk/res/android";

        Assert.Contains(
            document.Descendants("uses-permission"),
            element => string.Equals(
                element.Attribute(android + "name")?.Value,
                "android.permission.VIBRATE",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("PanelBoss.leftactions.cs")]
    [InlineData("PanelBoss.rightactions.cs")]
    public void SelectorDeactivationDoesNotReopenThePanelItJustClosed(string sourceFile)
    {
        var root = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "PanelBossKit",
            "ViewModels",
            "PanelBossVM",
            sourceFile);
        var source = File.ReadAllText(sourcePath);

        Assert.Contains(
            "previousActiveLeftPanel != panelToDeActivate",
            source,
            StringComparison.Ordinal);
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

        throw new DirectoryNotFoundException("Could not locate the DapperDan repository root.");
    }

    private static XDocument LoadDapperDanPage()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "DapperDan",
            "DapperDanPage.xaml");

        Assert.True(File.Exists(path), $"DapperDan page is missing: {path}");
        return XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
    }

    private static XElement GetLane(XDocument document, string lane)
        => Assert.Single(document.Descendants(), element =>
            string.Equals(
                element.Name.LocalName,
                $"PanelBossBody_DefaultView.{lane}",
                StringComparison.Ordinal));

    private static List<XElement> DirectRichButtons(XElement element)
        => element
            .Elements()
            .Where(child => child.Name.LocalName == "RichButton")
            .ToList();

    private static int CountGridColumns(XElement grid)
    {
        var columnDefinitions = grid
            .Elements()
            .SingleOrDefault(element => element.Name.LocalName == "Grid.ColumnDefinitions");

        if (columnDefinitions is not null)
        {
            return columnDefinitions
                .Elements()
                .Count(element => element.Name.LocalName == "ColumnDefinition");
        }

        return GetAttribute(grid, "ColumnDefinitions")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static int GetGridColumn(XElement element)
    {
        var value = GetOptionalAttribute(element, "Grid.Column");
        return value is null ? 0 : int.Parse(value);
    }

    private static bool IsMorePanel(XElement element)
        => GetOptionalPanelAttribute(element, "PanelName")?.EndsWith(
            "MorePanel",
            StringComparison.Ordinal) == true;

    private static string GetPanelAttribute(XElement element, string name)
        => GetOptionalPanelAttribute(element, name) ??
            throw new Xunit.Sdk.XunitException($"Missing PanelBoss.{name} on {element.Name.LocalName}.");

    private static string? GetOptionalPanelAttribute(XElement element, string name)
        => GetOptionalAttribute(element, $"PanelBoss.{name}");

    private static string GetAttribute(XElement element, string name)
        => GetOptionalAttribute(element, name) ??
            throw new Xunit.Sdk.XunitException($"Missing {name} on {element.Name.LocalName}.");

    private static string? GetOptionalAttribute(XElement element, string name)
        => element
            .Attributes()
            .SingleOrDefault(attribute => attribute.Name.LocalName == name)
            ?.Value;

}
