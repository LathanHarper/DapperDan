# FleXler showcase

Open **Witness → FleXler** in Dapper Dan to shape a live FlexLayout, edit its items and container options, preview or copy the resulting XAML, and save favorite recipes locally. This is the owner-authorized production feature, including its full workbench, semantic values, materials and approved artwork. See [provenance](../PROVENANCE.md) for the public source boundary and [Prism licensing](PRISM-LICENSING.md) before restoring or developing the app.

## Source and native ownership

The feature lives under [`src/DapperDan/Features/Flexler`](../src/DapperDan/Features/Flexler), retaining its `Flexler.*` namespaces and source filenames. `MainPage.xaml` owns the visible layout, commands, state rules and decorative layers. Its first visual child is the feature's PanelBoss host; the page view model owns its separately resolved PanelBoss. Grids own measured layout and interaction, borders remain empty decorative layers, and FlexLayout owns the live recipe surface. Direct RichButtons retain their native input behavior and semantic automation targets.

The feature's Android and iOS input, feedback and material implementations live under each platform's `Flexler` folder. Dapper Dan registers those components alongside its existing controls. The host keeps its **Dapper Dan** name, `net.codecrafty.dapperdan` application identity, icon, splash, database, signing and release configuration. The app still targets only Android and iOS. Retained Windows handler source does not enable a Windows target or establish Windows validation.

[`Resources/Semantics.xaml`](../src/DapperDan/Features/Flexler/Resources/Semantics.xaml) and [`Resources/Labradorite.xaml`](../src/DapperDan/Features/Flexler/Resources/Labradorite.xaml) expose **93 unique keys**, all prefixed `Flexler.`. Controls reference those semantic values directly. The page contains empty implicit styles solely to shadow Dapper Dan's application defaults for the affected native types. These styles have no setters, templates or shared visual trees; they do not hide the feature's layout or material ownership.

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

The local import check passed all 72 tests using installed SDK **10.0.303**. Both new test lock files were also checked against fresh NuGet.org packages in an isolated cache with machine fallback folders disabled; their contents were unchanged, and all 72 tests passed again. The repository remains pinned to **10.0.302** and workload set **10.0.302.1**. Record this local SDK difference; it is not a result from the pinned SDK or native Apple tooling.

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

The Android test project compiles without warnings or errors. **Native functionality verification is still pending**; compiling the suite is not a device pass.

## Current host startup check

Local Android diagnosis found a Dapper Dan host startup hang at `CompiledModelEnter`, before the Flexler showcase opened. Android now selects `Microsoft.EntityFrameworkCore.Issue31751=true` before building MAUI, matching the existing iOS entry point's inline compiled-model initialization path. Native validation of that change remains pending. This is a host EF startup issue, separate from the feature's UI behavior; it does not establish the cause of a standalone Flexler device crash.

## Free public iOS startup proof

The [public Flexler Simulator proof guide](../tools/flexler-proof/README.md) describes the manual `proof_scope: flexler` lane: one bounded standard GitHub-hosted macOS job in the public repository, no credentials or release signing, and no retained artifacts. It launches the complete showcase and checks native startup checkpoints.

That lane provides Simulator startup evidence only. It does not replace Android Appium interaction checks, physical-device validation, visual review or a signed release check. Dapper Dan retains its existing iOS interpreter configuration; a Simulator result is not proof of another app's device AOT behavior.
