using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Flexler.UITests;

public sealed class WorkbenchTests
{
    private static Exception? _androidConnectionFailure;
    [Fact]
    public void AppOpensSmoke() => Run(ui =>
    {
        ui.Count(5);
        Assert.True(ui.Enabled(UiTargets.Add));
        ui.Snapshot("app-opens");
    });

    [Fact]
    public void Showcase_returns_to_host_and_reopens_in_both_orientations() => Run(ui =>
    {
        foreach (var compact in new[] { true, false })
        {
            ui.SetCompact(compact);
            ui.Count(5);
            ui.Click(UiTargets.ShowcaseBack);
            // The launcher is below the fold in landscape; Click reaches it through
            // the native scroll viewport after the visible host confirms navigation.
            ui.Wait(() => ui.Present(UiTargets.HostWitness) && !ui.Present(UiTargets.Add),
                "returned Dapper Dan host");
            ui.Snapshot(compact ? "compact-host-return" : "wide-host-return");
            ui.Click(UiTargets.HostOpenFlexler);
            ui.Wait(() => ui.Present(UiTargets.Add) && ui.Present(UiTargets.ShowcaseBack),
                "reopened Flexler showcase");
            ui.Count(5);
            Assert.True(ui.Enabled(UiTargets.Add));
            ui.Snapshot(compact ? "compact-showcase-reopened" : "wide-showcase-reopened");
        }
    });

    [Fact]
    public void Add_edit_remove_empty_and_reset() => Run(ui =>
    {
        ui.Count(5);
        ui.Click(UiTargets.Add);
        ui.Count(6);
        ui.Click(UiTargets.Item(6));
        ui.Enter(UiTargets.ItemText, "Aloha & <reef> \"🌺\" 日本語");
        ui.Select(UiTargets.Picker("AlignSelf"), "Center");
        ui.Select(UiTargets.Picker("Basis"), "50%");
        ui.Select(UiTargets.Picker("Grow"), "2");
        ui.Select(UiTargets.Picker("Shrink"), "0.5");
        ui.Select(UiTargets.Picker("Order"), "-2");
        ui.Snapshot("edited-item");
        ui.Click(UiTargets.ItemDone);
        var item = CopyRecipe(ui).Elements().Last();
        Assert.Equal("Center", (string?)item.Attribute("FlexLayout.AlignSelf"));
        Assert.Equal("50%", (string?)item.Attribute("FlexLayout.Basis"));
        Assert.Equal("2", (string?)item.Attribute("FlexLayout.Grow"));
        Assert.Equal("0.5", (string?)item.Attribute("FlexLayout.Shrink"));
        Assert.Equal("-2", (string?)item.Attribute("FlexLayout.Order"));
        Assert.Equal("Aloha & <reef> \"🌺\" 日本語", (string?)item.Elements().Single().Attribute("Text"));
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Item(6));
        ui.Click(UiTargets.RemoveItem);
        ui.Count(5);
        for (var remaining = 5; remaining > 0; remaining--)
        {
            ui.Click(UiTargets.Item(remaining));
            ui.Click(UiTargets.RemoveItem);
            ui.Count(remaining - 1);
        }
        Assert.True(ui.Present(UiTargets.Empty));
        ui.Snapshot("empty-state");
        ui.Click(UiTargets.Reset);
        ui.Count(5);
        ui.ExpectText(UiTargets.Summary, "Row · Wrap · Justify Start · Align Stretch");
    });

    [Fact]
    public void Every_container_picker_and_exported_value() => Run(ui =>
    {
        ui.Click(UiTargets.Container);
        var options = new Dictionary<string, string[]>
        {
            ["Direction"] = ["Row", "RowReverse", "Column", "ColumnReverse"],
            ["Wrap"] = ["NoWrap", "Wrap", "Reverse"],
            ["Justify"] = ["Start", "Center", "End", "SpaceBetween", "SpaceAround", "SpaceEvenly"],
            ["AlignItems"] = ["Start", "Center", "End", "Stretch"],
            ["AlignContent"] = ["Start", "Center", "End", "Stretch", "SpaceBetween", "SpaceAround"]
        };
        foreach (var (property, values) in options)
            foreach (var value in values) ui.Select(UiTargets.Picker(property), value);
        ui.Snapshot("all-container-pickers");
        ui.Click(UiTargets.ContainerDone);
        var recipe = CopyRecipe(ui);
        Assert.Equal("ColumnReverse", (string?)recipe.Attribute("Direction"));
        Assert.Equal("Reverse", (string?)recipe.Attribute("Wrap"));
        Assert.Equal("SpaceEvenly", (string?)recipe.Attribute("JustifyContent"));
        Assert.Equal("Stretch", (string?)recipe.Attribute("AlignItems"));
        Assert.Equal("SpaceAround", (string?)recipe.Attribute("AlignContent"));
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Container);
        ui.Click(UiTargets.ContainerReset);
        ui.ExpectText(UiTargets.Summary, "Row · Wrap · Justify Start · Align Stretch");
    });

    [Fact]
    public void Favorites_save_validate_restart_load_and_confirm_removal() => Run(ui =>
    {
        var name = "UI recipe " + Guid.NewGuid().ToString("N")[..10];
        ui.Click(UiTargets.Add);
        ui.Count(6);
        ui.Click(UiTargets.Item(6));
        ui.Enter(UiTargets.ItemText, "Saved 🌺 日本語 & <reef>");
        ui.Select(UiTargets.Picker("Basis"), "50%");
        ui.Select(UiTargets.Picker("Grow"), "2");
        ui.Click(UiTargets.ItemDone);
        var savedRecipe = CopyRecipe(ui);
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Favorites);
        ui.Wait(() => ui.Present(UiTargets.FavoriteName), "favorites form");
        ui.Wait(() => ui.Present(UiTargets.Saved), "saved recipe picker");
        Assert.False(ui.Enabled(UiTargets.Save));
        ui.Enter(UiTargets.FavoriteName, name);
        ui.Click(UiTargets.Save);
        ui.ExpectText(UiTargets.FavoritesStatus, $"Saved “{name}”");
        ui.OwnFavorite(name);
        ui.Enter(UiTargets.FavoriteName, name);
        ui.Click(UiTargets.Save);
        ui.ExpectText(UiTargets.FavoritesStatus, "unique name");
        ui.Enter(UiTargets.FavoriteName, "");
        ui.Snapshot("saved-favorite-and-duplicate-validation");
        ui.Click(UiTargets.FavoritePreview);
        ui.Wait(() => ui.Text(UiTargets.Recipe).Contains("</FlexLayout>", StringComparison.Ordinal), "XAML from favorites panel");
        Assert.True(XNode.DeepEquals(savedRecipe, XElement.Parse(ui.Text(UiTargets.Recipe))));
        ui.Restart();
        ui.Count(5);
        ui.Click(UiTargets.Favorites);
        ui.Select(UiTargets.Saved, name);
        ui.Click(UiTargets.Load);
        ui.Count(6);
        ui.ExpectText(UiTargets.ExportStatus, $"Loaded “{name}”");
        Assert.True(XNode.DeepEquals(savedRecipe, CopyRecipe(ui)), "Favorite must retain every exported container and item value.");
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Reset);
        ui.Count(5);
        ui.Click(UiTargets.Favorites);
        ui.Select(UiTargets.Saved, name);
        ui.Click(UiTargets.Delete);
        ui.ExpectText(UiTargets.FavoritesStatus, "CONFIRM REMOVE");
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Favorites);
        ui.Select(UiTargets.Saved, name);
        ui.Click(UiTargets.Delete);
        ui.ExpectText(UiTargets.FavoritesStatus, "CONFIRM REMOVE");
        ui.Snapshot("delete-confirmation");
        ui.Click(UiTargets.Delete); // Only this scenario's unique synthetic favorite.
        ui.ExpectText(UiTargets.FavoritesStatus, $"Removed “{name}”");
        ui.ForgetFavorite(name);
        ui.Click(UiTargets.RecipeDone);
        ui.Count(5);
    });

    [Fact]
    public void Collapsed_native_flex_combination_keeps_recipe_and_reset_reachable() => Run(ui =>
    {
        ui.Click(UiTargets.Container);
        ui.Select(UiTargets.Picker("AlignContent"), "Center");
        ui.Click(UiTargets.ContainerDone);
        ui.Count(5);
        ui.Snapshot("native-stretch-center-combination");
        // MAUI 10.0.20 can arrange these auto-sized, stretched children at zero height.
        // Flexler must preserve the requested recipe and keep recovery outside the canvas.
        var recipe = CopyRecipe(ui);
        Assert.Equal(5, recipe.Elements().Count());
        Assert.Equal("Stretch", (string?)recipe.Attribute("AlignItems"));
        Assert.Equal("Center", (string?)recipe.Attribute("AlignContent"));
        ui.Click(UiTargets.RecipeDone);
        ui.Click(UiTargets.Reset);
        ui.Wait(() => ui.Present(UiTargets.Item(2)), "visible default facets after reset");
    });

    [Fact]
    public void Compact_and_wide_chrome_keep_fields_reachable() => Run(ui =>
    {
        foreach (var compact in new[] { true, false })
        {
            ui.SetCompact(compact);
            ui.Snapshot(compact ? "compact-resized" : "wide-resized");
            ui.Wait(() => ui.Present(UiTargets.Item(2)), "visible item after resize");
            ui.Click(UiTargets.Container);
            ui.Select(UiTargets.Picker("AlignItems"), "Start");
            ui.Select(UiTargets.Picker("AlignContent"), "Center");
            ui.Snapshot(compact ? "compact-container" : "wide-container");
            ui.Click(UiTargets.ContainerDone);
            ui.Click(UiTargets.Item(2));
            ui.Select(UiTargets.Picker("Order"), "10");
            ui.Enter(UiTargets.ItemText, "Reachable field");
            ui.Snapshot(compact ? "compact-item" : "wide-item");
            ui.Click(UiTargets.ItemDone);
            Assert.Equal("10", (string?)CopyRecipe(ui).Elements().ElementAt(1).Attribute("FlexLayout.Order"));
            ui.Click(UiTargets.RecipeDone);
        }
    });

    private static XElement CopyRecipe(Workbench ui)
    {
        ui.Click(UiTargets.Preview);
        ui.Wait(() => ui.Text(UiTargets.Recipe).Contains("</FlexLayout>", StringComparison.Ordinal), "complete live XAML preview");
        var preview = ui.Text(UiTargets.Recipe);
        ui.Click(UiTargets.Copy);
        ui.ExpectText(UiTargets.PreviewStatus, "Copied");
        var clipboard = ui.ClipboardText();
        Assert.Equal(preview.Replace("\r\n", "\n"), clipboard.Replace("\r\n", "\n"));
        return XElement.Parse(clipboard);
    }

    private static void Run(Action<Workbench> scenario, [CallerMemberName] string name = "")
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var evidence = Environment.GetEnvironmentVariable("DAPPER_FLEXLER_UI_EVIDENCE")
                    ?? throw new InvalidOperationException("Set DAPPER_FLEXLER_UI_EVIDENCE to a new evidence folder.");
                if (_androidConnectionFailure is not null)
                    throw new InvalidOperationException("Android run stopped after connectivity failed. Crafty must restore the agreed emulator/Appium connection before a new run.", _androidConnectionFailure);
                using Workbench ui = new AndroidWorkbench(Path.Combine(evidence, name));
                Exception? scenarioFailure = null;
                try { scenario(ui); ui.Snapshot("passed"); }
                catch (Exception error)
                {
                    scenarioFailure = error;
                    RecordConnectionFailure(error, evidence);
                    if (_androidConnectionFailure is null)
                        try { ui.Snapshot("FAILED"); }
                        catch (Exception capture)
                        {
                            RecordConnectionFailure(capture, evidence);
                            Console.Error.WriteLine(capture.Message);
                        }
                }
                if (_androidConnectionFailure is null)
                {
                    try { ui.CleanUpFavorites(); }
                    catch (Exception cleanup)
                    {
                        RecordConnectionFailure(cleanup, evidence);
                        scenarioFailure = scenarioFailure is null ? cleanup : new AggregateException(scenarioFailure, cleanup);
                    }
                }
                if (scenarioFailure is not null) ExceptionDispatchInfo.Capture(scenarioFailure).Throw();
            }
            catch (Exception error)
            {
                failure = error;
                RecordConnectionFailure(error, Environment.GetEnvironmentVariable("DAPPER_FLEXLER_UI_EVIDENCE"));
            }
        }) { IsBackground = true };
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromMinutes(8)), "UI scenario exceeded its eight-minute bound.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void RecordConnectionFailure(Exception error, string? evidence)
    {
        if (_androidConnectionFailure is not null) return;
        var message = error.ToString();
        string[] signals = ["socket hang up", "ECONNREFUSED", "ECONNRESET", "Could not proxy command", "Could not find a connected Android device",
            "device offline", "device unauthorized", "instrumentation process is not running", "invalid session id", "Connection refused"];
        if (!signals.Any(signal => message.Contains(signal, StringComparison.OrdinalIgnoreCase))) return;
        _androidConnectionFailure = error;
        if (evidence is null) return;
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "NEEDS-CRAFTY.txt"),
            "Use Hollar out loud. Restore the same Visual Studio emulator/SDK and Appium connection with Crafty; no infrastructure retries or replacements.\n\n" + message);
    }
}
