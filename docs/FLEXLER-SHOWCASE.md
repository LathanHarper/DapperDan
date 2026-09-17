# FleXler showcase

Open **Witness → FleXler** in Dapper Dan to shape a live FlexLayout, edit its items and container options, preview or copy the resulting XAML, and save favorite recipes locally. This is the owner-authorized production feature, including its full workbench, semantic values, materials and approved artwork. See [provenance](../PROVENANCE.md) for the public source boundary and [Prism licensing](PRISM-LICENSING.md) before restoring or developing the app.

## Source and native ownership

The feature lives under [`src/DapperDan/Features/Flexler`](../src/DapperDan/Features/Flexler), retaining its `Flexler.*` namespaces and source filenames. `MainPage.xaml` owns the visible layout, commands, state rules and decorative layers. Its first visual child is the feature's PanelBoss host; the page view model owns its separately resolved PanelBoss. Grids own measured layout and interaction, borders remain empty decorative layers, and FlexLayout owns the live recipe surface. Direct RichButtons retain their native input behavior and semantic automation targets.

The feature's Android and iOS input, feedback and material implementations live under each platform's `Flexler` folder. Dapper Dan registers those components alongside its existing controls. The host keeps its **Dapper Dan** name, `net.codecrafty.dapperdan` application identity, icon, splash, database, signing and release configuration. The app still targets only Android and iOS. Retained Windows handler source does not enable a Windows target or establish Windows validation.

[`Resources/Semantics.xaml`](../src/DapperDan/Features/Flexler/Resources/Semantics.xaml) and [`Resources/Labradorite.xaml`](../src/DapperDan/Features/Flexler/Resources/Labradorite.xaml) expose **93 unique keys**, all prefixed `Flexler.`. Controls reference those semantic values directly. The page contains empty implicit styles solely to shadow Dapper Dan's application defaults for the affected native types. These styles have no setters, templates or shared visual trees; they do not hide the feature's layout or material ownership.

The latest static audit resolved **752 references to 82 distinct keys**. Imported resource values remain unchanged; the host adds `Flexler.ShowcaseBackGlyphFontSize=24`. Startup instrumentation preserves anonymous resource dictionaries without typed constructors, static roots or early resource probes. Their loading remains inside the app XAML checkpoint boundary, avoiding diagnostic changes that could hide a trimming-related failure.

The approved spring, FleXler wordmark, standalone Fl mark and two labradorite textures are included unchanged. The paired header keeps the spring beside the wordmark, while drawers use the standalone mark. Font and adapted native-input notices accompany the assets; see [the asset inventory](../src/DapperDan/Resources/Raw/Flexler/AboutAssets.txt) and [third-party notices](../THIRD-PARTY-NOTICES.md).

## Host integration

The feature adds a Back command in both orientations. It returns through Prism navigation; a proof build that starts directly at the showcase can fall back to Dapper Dan's root page. Editing and export remain in the production view model and services.

Favorites use `FlexlerShowcase/favorites.json` under the host's app-data directory, isolated from Dapper Dan's database and other feature storage. There is no recipe network transport. Optional mechanical feedback uses `Flexler/rich_touch.wav`, `Flexler/rich_long_touch.wav` and `Flexler/rich_negative_feedback.wav`. No clips are packaged at those paths, preserving the workbench's silent fallback and avoiding unrelated host sounds.

## Portable functional tests

[`tests/Flexler.Showcase.Tests`](../tests/Flexler.Showcase.Tests/README.md) links the actual feature source into a plain `net10.0` test project. Its **72 tests** exercise XAML export through MAUI's real parser, editing and command state, favorite persistence and recovery, PanelBoss transitions, FlexLayout invalidation and button activation. Fixtures use synthetic values, a fake clipboard and disposable temporary folders.

From the repository root, with its pinned SDK available:

```powershell
dotnet test ./tests/Flexler.Showcase.Tests/Flexler.Showcase.Tests.csproj -c Release --nologo
```

The local import check passed all 72 tests using installed SDK **10.0.303**; the existing Dapper Dan host suite also passed all **41 tests**. Both new test lock files were checked against fresh NuGet.org packages in an isolated cache with machine fallback folders disabled; their contents were unchanged, and all 72 feature tests passed again. The repository remains pinned to **10.0.302** and workload set **10.0.302.1**. A child-tool SDK diagnostic is recorded in the [validation receipt](validation/flexler-showcase-2026-09-15.md), alongside the tested Android payload and remaining checks. These local results do not claim execution with the pinned SDK or native Apple tooling.

Before the strict iOS run, the instrumented source passed those **72 feature and 41 host tests** again, plus **38 proof-helper tests**. Local managed iOS Release compilation completed with **141 warnings and zero errors**; native compilation and startup were subsequently verified on the pinned public Mac runner as recorded below.

## Android Appium verification

[`tests/Flexler.Showcase.UITests`](../tests/Flexler.Showcase.UITests/README.md) is a separate `net10.0` Appium/UiAutomator2 project with **seven coded scenarios**: startup, editing/removal/reset, container choices and export, favorite persistence/removal, recovery from a collapsed native layout, portrait/landscape reachability, and returning to the host and reopening. It enters Witness → FleXler after launch and restart. It contains no Windows automation dependencies.

Use the operator's existing Visual Studio-created emulator and the same Android SDK, JDK and adb as Visual Studio. Resolve and record their actual paths, AVD name, explicit serial and exact installed payload before running. Install the candidate separately. For fast-deployed Debug builds, record the APK and deployed managed assemblies together; that proof is distinct from a packaged Release build.

After the operator has prepared that device and the Appium server, replace the placeholders below with the resolved values. Evidence stays in an ignored, run-specific directory.

```powershell
$env:DAPPER_FLEXLER_UI_ANDROID_SERIAL = '<existing-emulator-serial>'
$env:DAPPER_FLEXLER_UI_ANDROID_ACTIVITY = '<resolved-installed-launch-activity>'
$env:DAPPER_FLEXLER_UI_APPIUM_URL = 'http://127.0.0.1:4725'
$env:DAPPER_FLEXLER_UI_EVIDENCE = Join-Path $PWD ('artifacts/flexler-ui/' + [guid]::NewGuid().ToString('N'))

dotnet test ./tests/Flexler.Showcase.UITests/Flexler.Showcase.UITests.csproj `
    -c Release --nologo --logger trx `
    --results-directory (Join-Path $env:DAPPER_FLEXLER_UI_EVIDENCE 'results')
```

The suite uses `noReset=true` and cleans up only its uniquely named synthetic favorites. It does not create devices, install packages, start Appium or restart adb. Preserve app data and keep screenshots, native trees and personal diagnostic content out of public source and artifacts.

On emulator, adb or Appium connectivity failure, preserve the first error and stop the affected run. Recognized connection errors latch the suite against additional sessions and create `NEEDS-CRAFTY.txt`. Contact the operator to repair the same environment. Maintainers with the local **Hollar out loud** skill should use its audible help request; that workstation skill is not bundled here. Do not substitute another emulator, SDK or renderer, restart shared adb, or enter reconnect loops. Resume after the operator confirms the agreed environment is ready. Ordinary assertions with healthy connectivity remain product debugging.

The corrected Android test project compiled with zero warnings and errors. The original full run finished with six passes and one test viewport assertion failure. After correcting the off-screen launcher wait, a targeted return/reopen rerun passed in both orientations against the unchanged Debug fast-deployed candidate. **All seven scenarios are covered as six plus one across two executions**, not a single clean seven-test run. See the [validation receipt](validation/flexler-showcase-2026-09-15.md) for the exact payload, SDK diagnostic and release limitations.

## Current host startup check

Local Android diagnosis found a Dapper Dan host startup hang at `CompiledModelEnter`, before the Flexler showcase opened. Android now selects `Microsoft.EntityFrameworkCore.Issue31751=true` before building MAUI, matching the existing iOS entry point's inline compiled-model initialization path. Before/after launch evidence and the six original Appium workbench scenarios confirm that this unblocked the recorded Android Debug candidate. This is a host EF startup issue, separate from the feature's UI behavior; it does not establish the cause of a standalone Flexler device crash.

## Free public iOS startup proof

The [public Flexler Simulator proof guide](../tools/flexler-proof/README.md) describes the manual `proof_scope: flexler` lane: one bounded standard GitHub-hosted macOS job in the public repository, no credentials or release signing, and no retained artifacts. It launches the complete showcase and checks native startup checkpoints.

Select `proof_configuration: Debug` with `proof_runtime_profile: host` for the default startup diagnostic. `Release` can preserve the host profile or explicitly select `aot-trim` to probe partial trimming and Mono AOT with interpreter fallback disabled. The strict profile verifies both evaluated settings and runtime dynamic-code flags. Each uses its own configuration's output; both remain full Mac builds.

That lane provides Simulator startup evidence only. It does not replace Android Appium interaction checks, physical-device validation, visual review or a signed release check. Dapper Dan's project defaults retain their existing iOS interpreter configuration; the strict diagnostic uses explicit workflow overrides. A Simulator result is not proof of another app's device AOT behavior.

The **strict Release proof passed** in [run 35038827834](https://github.com/LathanHarper/DapperDan/actions/runs/35038827834) at exact commit `eaab272e3dc61ad8a495cccb6b20d445c4fab9ef`. Native compilation took **11 minutes 23 seconds**, with **141 warnings and zero errors**; the whole job took **15 minutes 19 seconds**. On the stock **iPad (A16), iOS 26.5** Simulator, the actual launch flags were both false, `FlexlerViewModelReady` appeared at **754 ms**, and `FlexlerPageLoaded` at **5,331 ms**, followed by eight seconds alive with no recorded exceptions. Both checkpoints were required. GitHub reported **zero retained artifacts and zero billable macOS milliseconds**.

The [exact validation receipt](validation/flexler-showcase-2026-09-15.md#strict-release-native-startup) records all evaluated strict settings, the sealed executable hash and intermediate XAML/material checkpoints. Follow-up `9c619c1` changes only workflow/documentation for explicit artifact-storage opt-in; application source is unchanged. These are observed startup timings, not a controlled benchmark or a diagnosis of standalone Flexler's device behavior. Physical iPad interaction, rendering and signed-distribution verification remain separate gates.

The [historical Debug proof](validation/flexler-showcase-2026-09-15.md#historical-debug-startup) passed at commit `d3349b8ab504964258cef549309c9c8ad46ff44f`: 55 seconds to compile, 6 minutes 34 seconds for the complete job, and `FlexlerPageLoaded` at 6,408 ms on an iPad (A16) Simulator running iOS 26.5, with no recorded exceptions and eight further seconds alive. Its settings were `UseInterpreter=True`, `MtouchInterpreter=all`, `TrimMode=copy` and an empty `MtouchUseLlvm` value. This older source and different runtime profile are not a controlled comparison with strict Release.

The [separate earlier Release run](validation/flexler-showcase-2026-09-15.md#historical-release-build-timeout) **failed at its 25-minute build timeout**, with assembly-size optimization as its last console progress and native AOT/LLVM processes still active at cancellation. It never ran native startup and did not establish an ILLink hang or reproduce a Release application crash. Its host-profile result remains incomplete even though the later strict-profile run succeeded. The instrumented diagnostic allows up to 150 minutes for Release compilation within one 180-minute free public job, consistent with historical full-host Simulator builds of 48–64 minutes. These limits are ceilings, not expected durations.
