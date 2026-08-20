namespace CodeCrafty.DapperDan.Tests;

public sealed class AndroidViewportInsetLifecycleContractTests
{
    [Fact]
    public void PanelBossViewportInsetLeaseOwnsOneLoadedHandlerGeneration()
    {
        var root = FindRepositoryRoot();
        var sharedSource = Compact(ReadSharedSource(root));
        var androidSource = Compact(ReadAndroidSource(root));

        Assert.Contains(
            "InitializePlatformViewportInsetContract();",
            sharedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "Loaded+=OnViewportInsetViewLoaded;",
            androidSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "Unloaded+=OnViewportInsetViewUnloaded;",
            androidSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "privatePanelBossViewportInsetLease?_viewportInsetLease;",
            androidSource,
            StringComparison.Ordinal);

        var handlerChanging = Slice(
            sharedSource,
            "protectedoverridevoidOnHandlerChanging",
            "privatevoidRegisterHostPageAppearing");
        AssertOrdered(
            handlerChanging,
            "ClearPlatformViewportInsetContract();",
            "base.OnHandlerChanging(args);");

        var applyContract = Slice(
            androidSource,
            "partialvoidApplyPlatformViewportInsetContract()",
            "partialvoidClearPlatformViewportInsetContract()");
        AssertOrdered(
            applyContract,
            "if(!IsLoaded)",
            "PanelBossViewportInsetLease.TryAttach(this,platformView)");
        AssertOrdered(
            applyContract,
            "varcurrentLease=_viewportInsetLease;",
            "currentLease.QueueCurrentInsetReport();");

        var loadedHandler = Slice(
            androidSource,
            "privatevoidOnViewportInsetViewLoaded",
            "privatevoidOnViewportInsetViewUnloaded");
        Assert.Contains(
            "ApplyPlatformViewportInsetContract();",
            loadedHandler,
            StringComparison.Ordinal);

        var unloadedHandler = Slice(
            androidSource,
            "privatevoidOnViewportInsetViewUnloaded",
            "privatevoidRetirePlatformViewportInsetLease");
        AssertOrdered(
            unloadedHandler,
            "ClearPlatformViewportInsetContract();",
            "ReportPlatformViewportBottomInset(0);");
    }

    [Fact]
    public void LeaseRetiresBeforeNativeRemovalAndGuardsDelayedViewReads()
    {
        var source = Compact(ReadAndroidSource(FindRepositoryRoot()));
        var detach = Slice(source, "publicvoidDetach()", "publicvoidOnGlobalLayout()");
        var report = Slice(
            source,
            "privatevoidReportCurrentInset()",
            "privatestaticboolTryGetLiveRegistrationTarget");

        AssertOrdered(
            detach,
            "_isActive=false;",
            "TryRemoveFromObserver(registeredObserver);");
        Assert.Contains(
            "ReferenceEquals(owner.Handler?.PlatformView,contentView)",
            report,
            StringComparison.Ordinal);
        Assert.Contains("owner.IsLoaded", report, StringComparison.Ordinal);
        Assert.Contains("contentView.IsAttachedToWindow", report, StringComparison.Ordinal);
        AssertOrdered(report, "if(!_isActive", "GetKeyboardBottomInsetDip(contentView,rootView)");
        AssertOrdered(report, "owner.IsLoaded", "GetKeyboardBottomInsetDip(contentView,rootView)");
        AssertOrdered(
            report,
            "ReferenceEquals(owner.Handler?.PlatformView,contentView)",
            "GetKeyboardBottomInsetDip(contentView,rootView)");
        AssertOrdered(
            report,
            "contentView.IsAttachedToWindow",
            "GetKeyboardBottomInsetDip(contentView,rootView)");
        AssertOrdered(
            report,
            "rootView.IsAttachedToWindow",
            "GetKeyboardBottomInsetDip(contentView,rootView)");
        Assert.Contains("rootView.Post(ReportCurrentInset);", source, StringComparison.Ordinal);
        AssertOrdered(report, "catch(ObjectDisposedException)", "RetireAfterNativeFailure();");
        Assert.Contains("_registeredObserver=observer;", source, StringComparison.Ordinal);
        Assert.Contains("varcurrentObserver=rootView.ViewTreeObserver;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PanelBossKeyboardInsetListener", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "()=>_viewportInsetListener?.ReportCurrentInset()",
            source,
            StringComparison.Ordinal);
    }

    private static string ReadSharedSource(DirectoryInfo root) =>
        File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "PanelBossKit",
            "Views",
            "PanelBossBody_DefaultView.xaml.cs"));

    private static string ReadAndroidSource(DirectoryInfo root) =>
        File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "DapperDan",
            "Platforms",
            "Android",
            "PanelBossBody_DefaultView.android.cs"));

    private static void AssertOrdered(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);

        Assert.True(firstIndex >= 0, $"Missing expected source fragment: {first}");
        Assert.True(secondIndex >= 0, $"Missing expected source fragment: {second}");
        Assert.True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Missing source slice start: {start}");

        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        Assert.True(endIndex > startIndex, $"Missing source slice end: {end}");

        return source[startIndex..endIndex];
    }

    private static string Compact(string source) =>
        string.Concat(source.Where(character => !char.IsWhiteSpace(character)));

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
            "Could not locate the CodeCrafty.DapperDan repository root.");
    }
}
