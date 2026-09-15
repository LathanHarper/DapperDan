using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using ScreenOrientation = OpenQA.Selenium.ScreenOrientation;

namespace Flexler.UITests;

internal sealed class AndroidWorkbench : Workbench
{
    private const string Package = "net.codecrafty.dapperdan";
    private readonly AndroidDriver _driver;

    public AndroidWorkbench(string evidence) : base(evidence)
    {
        var serial = Environment.GetEnvironmentVariable("DAPPER_FLEXLER_UI_ANDROID_SERIAL")
            ?? throw new InvalidOperationException("Set DAPPER_FLEXLER_UI_ANDROID_SERIAL to an explicitly selected adb target.");
        var activity = Environment.GetEnvironmentVariable("DAPPER_FLEXLER_UI_ANDROID_ACTIVITY")
            ?? throw new InvalidOperationException("Set DAPPER_FLEXLER_UI_ANDROID_ACTIVITY to the installed APK's resolved activity.");
        var options = new AppiumOptions { PlatformName = "Android", AutomationName = "UiAutomator2" };
        options.AddAdditionalAppiumOption("udid", serial);
        options.AddAdditionalAppiumOption("appPackage", Package);
        options.AddAdditionalAppiumOption("appActivity", activity);
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("forceAppLaunch", true);
        options.AddAdditionalAppiumOption("shouldTerminateApp", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 120);
        options.AddAdditionalAppiumOption("adbExecTimeout", 60000);
        _driver = new AndroidDriver(new Uri(Environment.GetEnvironmentVariable("DAPPER_FLEXLER_UI_APPIUM_URL")
            ?? "http://127.0.0.1:4725"), options, TimeSpan.FromSeconds(90));
        File.WriteAllText(Path.Combine(Evidence, "session.txt"), $"serial={serial}\npackage={Package}\nactivity={activity}\nsession={_driver.SessionId}\n");
        try
        {
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            _driver.Orientation = ScreenOrientation.Portrait;
            Landscape = false;
            EnterShowcase();
        }
        catch
        {
            try { _driver.Quit(); } catch (WebDriverException cleanup) { Console.Error.WriteLine(cleanup.Message); }
            throw;
        }
    }

    private void EnterShowcase()
    {
        Wait(() => Present(UiTargets.HostWitness), "Dapper Dan host actions", 30);
        Click(UiTargets.HostWitness);
        Click(UiTargets.HostOpenFlexler);
        Wait(() => Present(UiTargets.Add), "Flexler showcase workbench", 30);
        Wait(() => Present(UiTargets.Container), "orientation-specific showcase chrome", 30);
    }

    private AppiumElement? Optional(UiTarget target) => _driver.FindElements(By.Id(Package + ":id/" + target.For(Landscape))).FirstOrDefault();
    private AppiumElement Find(UiTarget target)
    {
        AppiumElement? element = null;
        Wait(() => (element = Optional(target)) is not null, $"native Android target {target.For(Landscape)}");
        return element!;
    }

    private AppiumElement Reach(UiTarget target)
    {
        var element = Optional(target);
        if (element is { Displayed: true }) return element;
        // Scroll the actual native form viewport. No screenshot-derived or fixed screen coordinates.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            // UiSelector class matching can return only Prism's outer navigation ScrollView.
            // XPath uses the observed accessibility tree, including the nested form viewport.
            var scroll = _driver.FindElements(By.XPath("//*[@class='android.widget.ScrollView' and @scrollable='true']"))
                .LastOrDefault(e => e.Displayed);
            if (scroll is null) break;
            _driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
            {
                ["elementId"] = scroll.Id,
                ["direction"] = attempt < 4 ? "down" : "up", ["percent"] = 0.75, ["speed"] = 1000
            });
            element = Optional(target);
            if (element is { Displayed: true }) return element;
        }
        return Find(target);
    }

    public override bool Present(UiTarget target) => Optional(target)?.Displayed == true;
    public override bool Enabled(UiTarget target) => Optional(target)?.Enabled == true;
    public override string Text(UiTarget target)
    {
        var element = Optional(target);
        if (element is null) return "";
        var text = element.Text;
        if (target.AndroidTextLabel is not { } label) return text;
        if (text == label) return "";
        return text.StartsWith(label + ", ", StringComparison.Ordinal) ? text[(label.Length + 2)..] : text;
    }
    public override void Click(UiTarget target)
    {
        var element = Reach(target);
        Wait(() => element.Enabled, $"enabled {target}");
        element.Click();
    }
    public override void Enter(UiTarget target, string text)
    {
        var element = Reach(target);
        element.Clear();
        if (text.Length > 0) element.SendKeys(text);
        if (_driver.IsKeyboardShown()) _driver.HideKeyboard();
        Wait(() => Text(target) == text, $"Android entry {target}");
    }
    public override void Select(UiTarget target, string text)
    {
        Click(target);
        var literal = text.Contains('\'') ? "\"" + text + "\"" : "'" + text + "'";
        AppiumElement? option = null;
        Wait(() => (option = _driver.FindElements(By.XPath($"//*[@text={literal} and (@class='android.widget.CheckedTextView' or @class='android.widget.TextView')]")).FirstOrDefault(e => e.Displayed)) is not null,
            $"native picker option '{text}'");
        option!.Click();
        // MAUI includes SemanticProperties.Description before the chosen value in Android's accessibility text.
        Wait(() => Text(target) == text || Text(target).EndsWith(", " + text, StringComparison.Ordinal), $"Android picker {target} = {text}");
    }
    public override void SetCompact(bool compact)
    {
        _driver.Orientation = compact ? ScreenOrientation.Portrait : ScreenOrientation.Landscape;
        Landscape = !compact;
        Wait(() => Present(UiTargets.Container), "orientation-specific native chrome", 30);
    }
    public override void Restart()
    {
        _driver.TerminateApp(Package);
        _driver.ActivateApp(Package);
        EnterShowcase();
    }
    public override string ClipboardText() => _driver.GetClipboardText();
    protected override void SaveEvidence(string path)
    {
        File.WriteAllText(path + ".xml", _driver.PageSource);
        _driver.GetScreenshot().SaveAsFile(path + ".png");
    }
    public override void Dispose()
    {
        try { _driver.Quit(); }
        catch (WebDriverException error)
        {
            File.WriteAllText(Path.Combine(Evidence, "session-cleanup-error.txt"), error.ToString());
            throw;
        }
    }
}
