# FleXler showcase validation — September 15, 2026

All seven Android scenarios have passed across the original run and one corrected targeted rerun. The public **iOS Debug Simulator startup proof passed**. The separate Release build timed out before native startup, leaving the Release check incomplete. Local paths, device identifiers, screenshots and personal diagnostic logs are excluded.

## Source and payload

The Android Debug fast-deployment candidate was built from commit `11c8dadaddcd0a577037e5cbeccf4bc3cd7abc00`. Fast deployment consists of the APK together with deployed managed code, so an APK hash alone does not identify this candidate. The local payload receipt records the following files; their SHA-256 hashes were rechecked against the files on disk:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `net.codecrafty.dapperdan-Signed.apk` | 16,779,805 | `D3B7AAEAC5872D4595D5E59F133FACD52886284CAD5C6D2D53593DD314B4B30B` |
| `CodeCrafty.DapperDan.dll` | 1,570,304 | `D4AAA2F4A9E573367500C59317380D2D7C96766F7C6C770071B5686D8B94D7BF` |

Local build and test commands used installed SDK **10.0.303**. The repository's `global.json` still pins SDK **10.0.302** and workload set **10.0.302.1**. A child `dotnet nuget` invocation also reported that the pinned SDK was unavailable, despite the parent build and test commands completing with 10.0.303. The local Android and unit results do not claim execution with the pinned SDK or entirely diagnostic-free logs. The public iOS Debug run below used the pinned SDK/workload.

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

## Android host startup fix

Before the fix, the Android host launch journal stopped at `CompiledModelEnter`, before opening the FleXler showcase. Android now sets `Microsoft.EntityFrameworkCore.Issue31751=true` before MAUI initialization, matching the existing iOS entry point's generated-model inline initialization path.

The before/after launch evidence and subsequent six original workbench scenarios confirm that this change unblocked Android host startup for the recorded Debug candidate. The host EF initialization problem is separate from Flexler feature behavior and is not a diagnosis of a standalone Flexler device crash. Raw launch journals remain local.

## Public iOS proof

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

**Release build timeout:** [run 35035330116](https://github.com/LathanHarper/DapperDan/actions/runs/35035330116), commit `2cde0e7a0940a2c510ebfd64b9712495335e084a`, failed when its build step reached the 25-minute timeout. The step ran from **23:23:03 to 23:48:16 UTC**; its last reported build progress was **“Optimizing assemblies for size” at 23:23:32 UTC**. The native sealing/startup step was skipped. This is an incomplete Release check, not a Release startup pass or a reproduced application crash. Its earlier commit also differs from the passing Debug and Android candidates.

## Remaining validation boundaries

- Release compilation and native startup remain unverified after the build timeout.
- Debug fast-deployment results do not prove packaged Release linking, AOT, clean installation or release behavior.
- Simulator startup does not prove physical iPad launch, interaction, rendering or the standalone app's different release configuration.
- No Windows target or Windows native validation is added by this showcase.

See the [showcase guide](../FLEXLER-SHOWCASE.md) for source ownership and repeatable local test commands.
