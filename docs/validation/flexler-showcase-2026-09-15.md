# FleXler showcase validation — September 15, 2026

This receipt records the public showcase checks completed at this checkpoint. All seven Android scenarios have passed across the original run and one corrected targeted rerun; the public iOS Simulator result remains pending. Local paths, device identifiers, screenshots and personal diagnostic logs are intentionally excluded.

## Source and payload

The Android Debug fast-deployment candidate was built from commit `11c8dadaddcd0a577037e5cbeccf4bc3cd7abc00`. Fast deployment consists of the APK together with deployed managed code, so an APK hash alone does not identify this candidate. The local payload receipt records the following files; their SHA-256 hashes were rechecked against the files on disk:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `net.codecrafty.dapperdan-Signed.apk` | 16,779,805 | `D3B7AAEAC5872D4595D5E59F133FACD52886284CAD5C6D2D53593DD314B4B30B` |
| `CodeCrafty.DapperDan.dll` | 1,570,304 | `D4AAA2F4A9E573367500C59317380D2D7C96766F7C6C770071B5686D8B94D7BF` |

Local build and test commands used installed SDK **10.0.303**. The repository's `global.json` still pins SDK **10.0.302** and workload set **10.0.302.1**. A child `dotnet nuget` invocation also reported that the pinned SDK was unavailable, despite the parent build and test commands completing with 10.0.303. These results do not claim execution with the pinned SDK or entirely diagnostic-free logs.

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

[Public iOS run 35035330116](https://github.com/LathanHarper/DapperDan/actions/runs/35035330116) was still running at this checkpoint. It uses commit `2cde0e7a0940a2c510ebfd64b9712495335e084a`, which differs from the Android candidate above. **No iOS result is claimed yet.**

The [free Simulator lane](../../tools/flexler-proof/README.md) uses one standard public-repository macOS job, no Apple release credentials and no retained artifacts. Its eventual result can establish only the startup checks described by that lane.

## Remaining validation boundaries

- Record the public iOS run's actual outcome against its own commit.
- Debug fast-deployment results do not prove packaged Release linking, AOT, clean installation or release behavior.
- Simulator startup does not prove physical iPad launch, interaction, rendering or the standalone app's different release configuration.
- No Windows target or Windows native validation is added by this showcase.

See the [showcase guide](../FLEXLER-SHOWCASE.md) for source ownership and repeatable local test commands.
