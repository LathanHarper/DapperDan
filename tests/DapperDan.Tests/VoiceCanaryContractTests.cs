using System.Xml.Linq;

namespace CodeCrafty.DapperDan.Tests;

public sealed class VoiceCanaryContractTests
{
    private static readonly string[] TrialAutomationIds =
    [
        "DapperDan_Voice_DefaultAppSession",
        "DapperDan_Voice_RankedAppSession",
        "DapperDan_Voice_DefaultSystemSession",
    ];

    [Fact]
    public void VoiceTrialsStayInTheExistingCanaryPanelWithoutTapAudio()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Views",
            "DapperDan",
            "DapperDanPage.xaml");
        var document = XDocument.Load(pagePath);
        var witnessPanel = Assert.Single(
            document.Descendants(),
            element => GetOptionalAttribute(element, "PanelBoss.PanelName") ==
                "DapperDanWitnessPanel");

        foreach (var automationId in TrialAutomationIds)
        {
            var button = Assert.Single(
                witnessPanel.Descendants(),
                element => GetOptionalAttribute(element, "AutomationId") ==
                    automationId);
            Assert.Equal("RichButton", button.Name.LocalName);
            Assert.Equal("None", GetOptionalAttribute(button, "FeedbackMode"));
            Assert.Contains(
                "RunVoiceCanaryCommand",
                GetOptionalAttribute(button, "Command"),
                StringComparison.Ordinal);
        }

        Assert.DoesNotContain(
            document.Descendants(),
            element =>
                TrialAutomationIds.Contains(
                    GetOptionalAttribute(element, "AutomationId"),
                    StringComparer.Ordinal) &&
                !element.AncestorsAndSelf().Contains(witnessPanel));
    }

    [Fact]
    public void IosVoiceCanaryKeepsAudioSessionObservationReadOnly()
    {
        var root = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Platforms",
            "iOS",
            "Speech",
            "IosVoiceCanaryService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("AVSpeechSynthesisVoice.FromLanguage", source, StringComparison.Ordinal);
        Assert.Contains("AVSpeechSynthesisVoice.GetSpeechVoices", source, StringComparison.Ordinal);
        Assert.Contains("UsesApplicationAudioSession", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetCategory(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetMode(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetActive(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VoiceTraits", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IDisposable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public void Dispose", source, StringComparison.Ordinal);
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
