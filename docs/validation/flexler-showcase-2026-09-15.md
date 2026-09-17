# FleXler showcase validation — September 15, 2026

All seven Android scenarios passed across the original run and one corrected targeted rerun. The public **iOS Release `aot-trim` Simulator startup proof passed** for the earlier instrumented source, including the runtime check that dynamic code support and compilation were both false. The resource-scope correction below requires fresh native evidence. The earlier Debug pass and incomplete Release build remain separate historical evidence below. Local paths, device identifiers, screenshots and personal diagnostic logs are excluded.

## Resource-scope correction after review

The feature page now owns its anonymous Labradorite and Semantics dictionaries; the independently constructed atmosphere control owns a local Labradorite dictionary. Root backgrounds resolve after their resources are declared. Application resources no longer load the complete feature dictionaries. The host launcher has seven host-owned semantic values, checked against the original colors, fonts and gradients without visual value changes. No typed dictionary roots, interpreter changes, SDK/package changes or signing changes were introduced.

All 72 feature tests and 41 host tests passed locally. The affected iOS managed Compile passed using installed SDK 10.0.303 (141 existing warnings, zero errors; subsequent incremental verification also passed). XML/source-path checks, exact launcher-value comparisons and diff whitespace checks passed. These checks do not establish native resource lookup or visual behavior; new full native CI and strict Release startup proof are required for this corrected source. Previous native results below remain evidence for their recorded commits only. Physical-device interaction and appearance checks remain pending.

## Source and payload

The Android Debug fast-deployment candidate was built from commit `11c8dadaddcd0a577037e5cbeccf4bc3cd7abc00`. Fast deployment consists of the APK together with deployed managed code, so an APK hash alone does not identify this candidate. The local payload receipt records the following files; their SHA-256 hashes were rechecked against the files on disk:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `net.codecrafty.dapperdan-Signed.apk` | 16,779,805 | `D3B7AAEAC5872D4595D5E59F133FACD52886284CAD5C6D2D53593DD314B4B30B` |
| `CodeCrafty.DapperDan.dll` | 1,570,304 | `D4AAA2F4A9E573367500C59317380D2D7C96766F7C6C770071B5686D8B94D7BF` |

Local build and test commands used installed SDK **10.0.303**. The repository's `global.json` still pins SDK **10.0.302** and workload set **10.0.302.1**. A child `dotnet nuget` invocation also reported that the pinned SDK was unavailable, despite the parent build and test commands completing with 10.0.303. The local Android and unit results do not claim execution with the pinned SDK or entirely diagnostic-free logs. Both public iOS runs below used the pinned SDK/workload.

## Confirmed local results

| Check | Result and scope |
| --- | --- |
| FleXler portable functional suite | **72 passed**, covering the actual linked feature source, MAUI XAML parsing, editing, export, favorites, PanelBoss and button behavior. |
| Existing Dapper Dan host suite | **41 passed**. |
| New test-project package locks | Both locks remained byte-for-byte unchanged after fresh NuGet.org restores with isolated package storage, implicit workload library packs disabled and machine fallback folders cleared. The 40 functional-test and 17 UI-test dependency records matched the freshly restored NuGet content hashes. All 72 functional tests passed again against that graph. |
| Android Debug application build/deployment | Completed with **146 existing application warnings**. This is a fast-deployed Debug candidate, not a packaged Release validation. |
| Separate Android Appium project | The corrected test project compiled with **0 warnings and 0 errors**. The separate child-tool SDK diagnostic noted above remains part of the run's limitations. |
| Original full Appium execution | **6 passed, 1 failed.** The six original scenarios passed: startup; editing/removal/reset; container options/export; favorite persistence/removal; collapsed-layout recovery; portrait/landscape field reachability. The added host return/reopen scenario failed on a test viewport assertion. |
| Corrected host return/reopen rerun | **Passed** in about 30 seconds, covering both orientations against the unchanged Android payload. The test now waits for the host Witness action and the absence of the feature's Add action, then uses the existing native scrolling click to reach the launcher. The passing rerun has test results and screenshots retained locally. |

Together these executions cover **all seven scenarios as six plus one**, not a single clean seven-test run. The corrected wait changes test behavior, not the app payload.

The native run used the operator's existing Visual Studio emulator and resolved Visual Studio SDK/JDK/adb setup, with an explicitly selected target and preserved app data. Final functionality checks use coded Appium/UiAutomator2 tests. Cleanup is limited to each scenario's uniquely named synthetic favorites.

A concurrent attempt to rebuild the UI test project encountered an output-file lock while the test suite was active. That test-runner/build overlap is separate from product behavior; it does not establish an application failure.

Before the instrumented strict iOS proof, local revalidation passed **72 feature tests, 41 host tests and 38 proof-helper tests**. Managed iOS Release compilation also completed locally with **141 warnings and zero errors**, using installed SDK 10.0.303; this local compile did not perform native Mac AOT or Simulator launch. The Mac run independently passed all 38 helper tests before compiling the native app.

The static resource audit resolved **752 references to 82 distinct keys** against the two feature dictionaries' **93 declared keys**. Imported semantic values remained unchanged; the host adds only `Flexler.ShowcaseBackGlyphFontSize=24`. The dictionaries remain anonymous, without new typed constructors, static roots or early resource probes. This audit establishes reference resolution and value preservation, not native rendering or the cause of another application's failure.

## Android host startup fix

Before the fix, the Android host launch journal stopped at `CompiledModelEnter`, before opening the FleXler showcase. Android now sets `Microsoft.EntityFrameworkCore.Issue31751=true` before MAUI initialization, matching the existing iOS entry point's generated-model inline initialization path.

The before/after launch evidence and subsequent six original workbench scenarios confirm that this change unblocked Android host startup for the recorded Debug candidate. The host EF initialization problem is separate from Flexler feature behavior and is not a diagnosis of a standalone Flexler device crash. Raw launch journals remain local.

## Public iOS proof

### Strict Release native startup

[Release `aot-trim` run 35038827834](https://github.com/LathanHarper/DapperDan/actions/runs/35038827834) **passed** at exact source commit `eaab272e3dc61ad8a495cccb6b20d445c4fab9ef`. It completed on September 15 in the operator's Central time zone; the timestamps below are September 16 UTC.

| Evidence | Observed result |
| --- | --- |
| Toolchain | SDK `10.0.302`, workload set `10.0.302.1`, Xcode `26.6`; `iossimulator-arm64`. |
| Compilation | **11 minutes 23 seconds**, from `00:10:52Z` to `00:22:15Z`; **141 warnings and zero errors**. |
| Complete job | **15 minutes 19 seconds**, from `00:10:08Z` to `00:25:27Z`; one standard public macOS job. The normal full-scope jobs were skipped. |
| Evaluated runtime settings | `Configuration=Release`, `UseInterpreter=false`, `MtouchInterpreter=""` (empty), `TrimMode=partial`, `UseMonoRuntime=true`, `PublishAot=false`, `MtouchUseLlvm=true`, `Registrar=managed-static`, `MauiXamlInflator=SourceGen`. This is Mono AOT with interpreter fallback disabled, not NativeAOT. |
| Actual launch flags | `isDynamicCodeSupported=false` and `isDynamicCodeCompiled=false`; both required literal booleans were present. |
| Native startup | Stock **iPad (A16), iOS 26.5** Simulator; both `FlexlerViewModelReady` and `FlexlerPageLoaded`, **zero recorded exceptions**, and the launched process alive for the following **eight seconds**. |
| Local seals | **12 native files** received ephemeral ad-hoc seals; no Apple release identity or profile. |
| Sealed app executable SHA-256 | `d38874d0a9a36fab215f874d8a35204e77469f47c99412c4dfd34fcf580959aa` |
| Cost/storage evidence | GitHub's artifact API reported **zero retained artifacts**; its timing API reported **zero billable macOS milliseconds**. No cache or external test service was used. |

The journal recorded these elapsed startup checkpoints:

| Checkpoint | Elapsed time |
| --- | ---: |
| `AppXamlReady` | 340 ms |
| `FlexlerPageXamlReady` | 738 ms |
| `FlexlerViewModelReady` | 754 ms |
| `FlexlerIosMaterialContextReady` | 1,172 ms |
| `FlexlerIosMaterialConnectReady` | 1,173 ms |
| `FlexlerPageLoaded` | 5,331 ms |

These are observations from one launch, not a controlled performance benchmark. Different commits, runtime profiles and runner conditions prevent treating the earlier Debug timing as a before/after speed comparison. The result establishes successful native startup for this exact public app and strict profile; it neither reproduces nor diagnoses a standalone Flexler device failure.

Follow-up commit `9c619c1` changes only the unsigned workflow and its documentation to make retained artifacts an explicit manual opt-in. Its application source is unchanged from the successful run. The validation receipt remains anchored to the run's exact `eaab272e3dc61ad8a495cccb6b20d445c4fab9ef` source. Downloaded run logs and metadata stay in ignored local evidence storage.

### Historical Debug startup

[Debug run 35036551463](https://github.com/LathanHarper/DapperDan/actions/runs/35036551463) **passed** at exact commit `d3349b8ab504964258cef549309c9c8ad46ff44f`.

| Evidence | Observed result |
| --- | --- |
| Toolchain | SDK `10.0.302`, workload set `10.0.302.1`, Xcode `26.6`; `iossimulator-arm64`. |
| Timing | Compile step **55 seconds**; complete job **6 minutes 34 seconds**. |
| Evaluated properties | `Configuration=Debug`, `UseInterpreter=True`, `MtouchInterpreter=all`, `TrimMode=copy`, `MtouchUseLlvm=""` (empty). No workflow override of these interpreter/trimming/LLVM settings. |
| Native startup | Stock **iPad (A16), iOS 26.5** Simulator; `FlexlerPageLoaded` at **6,408 ms**, **zero recorded exceptions**, and the process alive for the following **eight seconds**. |
| Local seals | **12 native files** received ad-hoc seals before installation. No Apple release identity or profile was used. |
| Sealed app executable SHA-256 | `bea04964cfb8fed1912f7a75d4e1be682865c0601e7e5b26b914de1c8ba83a23` |

The [free Simulator lane](../../tools/flexler-proof/README.md) used one standard public-repository macOS job with no retained artifacts. This result establishes the lane's Debug startup checks, not interactions or Release AOT behavior.

### Historical Release build timeout

[Run 35035330116](https://github.com/LathanHarper/DapperDan/actions/runs/35035330116), commit `2cde0e7a0940a2c510ebfd64b9712495335e084a`, failed when its build step reached the 25-minute timeout. The step ran from **23:23:03 to 23:48:16 UTC**; its last reported build progress was **“Optimizing assemblies for size” at 23:23:32 UTC**, while native AOT/LLVM processes remained active at cancellation. The native sealing/startup step was skipped. That attempt remains an incomplete host-profile Release check, not a confirmed ILLink hang, a startup pass or a reproduced application crash. Its earlier commit differs from the passing Debug, Android and strict Release candidates; the later strict result does not retroactively change its outcome.

## Remaining validation boundaries

- The strict Release Simulator profile is verified above. It uses explicit diagnostic overrides and does not validate the signed Dapper Dan host profile or its physical-device behavior.
- Debug fast-deployment results do not prove packaged Release linking, AOT, clean installation or release behavior.
- Simulator startup does not prove physical iPad launch, interaction, rendering or the standalone app's different release configuration.
- Android's seven-scenario receipt belongs to its earlier exact payload, not a rerun of the newly instrumented candidate. No physical iPad interaction pass is claimed here.
- No Windows target or Windows native validation is added by this showcase.

See the [showcase guide](../FLEXLER-SHOWCASE.md) for source ownership and repeatable local test commands.
