# Branch-only Debug Simulator capture proof

This independently authored public automation answers a narrow platform question: can one unsigned .NET MAUI Debug Simulator app be preserved and reused to capture native large-iPhone and large-iPad images on a standard Apple Silicon GitHub runner?

**Verified outcome:** yes, after credential-free local re-sealing of the copied native Simulator binaries. [Passing run 34553339801](https://github.com/LathanHarper/DapperDan/actions/runs/34553339801) used the retained app without recompilation and produced independently visually inspected, fully rendered Dapper Dan UI on both required device families. There were no screenshot-command warnings in this final run. This is generic capture/reuse evidence, not a signed-device release proof.

It uses only the existing public Dapper Dan app. It imports no private source, assets, configuration, credentials, screenshots, or logs. These generic images are **not screenshots of any other app and cannot be submitted as another app's store assets**.

## Scope and dispatch

The `codex/simulator-capture-proof` branch alone replaces `.github/workflows/ios-unsigned.yml` with a manual-only, read-only, unsigned Debug capture workflow. Default `main`, its automatic Release canary, and the protected TestFlight workflow remain unchanged. Do not merge this branch merely to dispatch it.

GitHub requires a manually dispatched workflow to exist on the default branch. The existing registered workflow ID is reused with the explicit proof branch; the proof branch has no automatic triggers. Confirm repository visibility is PUBLIC and inspect the exact branch workflow before running:

```powershell
gh workflow run ios-unsigned.yml --repo LathanHarper/DapperDan --ref codex/simulator-capture-proof -f reuse_run_id=34551157760 -f repair_adhoc=true
```

The job additionally rejects every repository/ref other than this public proof branch. It uses standard `macos-26`, not a larger runner. No environment, signing material, App Store upload, or secret references are present. Standard public-repository hosted compute is free under GitHub's current policy; artifact storage is a separate account meter, so these proof artifacts use one-day retention.

The command above requires the original retained artifact to remain available. Both original and transformed products were also downloaded and hash-verified locally for safekeeping. This branch remains a controlled laboratory: a blank `reuse_run_id` rebuilds the original signing-disabled fixture used to reproduce the loader failure, not a recommended production capture configuration. Do not dispatch that failing baseline just to repeat the successful capture proof. The tested path reuses the retained original, seals a copy and captures it; downstream capture lanes must include the proven signature completion/verification before launch.

## Preservation and evidence

1. Verify pinned .NET 10.0.302, workload 10.0.302.1, Xcode 26.6 and Apple Silicon.
2. Install only `maui-ios`. Project the checked-in lock's existing iOS base and Simulator sections into a temporary lock with no package/version changes, restore with locked mode and `TargetFrameworks=net10.0-ios`, then build **Debug iossimulator-arm64 once**. Do not install Android workloads, AOT-build Release Simulator, or build any device target.
3. Package the complete `.app` with source/run/toolchain receipt, SHA-256, logs and license notices. Upload this reusable product **before boot/capture operations**.
4. Use an already-installed iOS runtime and known native accepted-size device types. Boot, install, launch and capture each device sequentially. Validate PNG headers and exact dimensions; never stretch or crop.
5. Shut down the created Simulator after each attempt. Preserve partial captures and timings even if another device fails. Inspect the actual image afterward: successful launch and PNG dimensions alone do not prove the foreground app rendered correctly.

The reusable app requires a compatible Apple Silicon Mac and installed iOS Simulator runtime. It does not run on Windows or physical iOS devices. A later capture-only job can download the retained archive, verify its receipt/hash, extract into a fresh folder, set `SIMULATOR_APP_DIRECTORY` to that folder, and invoke `capture.mjs capture` without compiling. Source and toolchain must remain compatible. Nothing here is a signed IPA or device-release runtime proof.

Job/step timeouts bound unattended resource occupancy, not guarantee a successful capture. No paid private run should be projected solely from generic sample timing: real application complexity and cold toolchain setup differ.

## Local contracts

```powershell
node --test tools/simulator-proof/capture.test.mjs
```

Runtime results and actual captured dimensions are recorded in each run's artifacts, not asserted by this implementation document.

## First measured run: mechanics passed, app UI witness failed

[Run 34551157760](https://github.com/LathanHarper/DapperDan/actions/runs/34551157760), source `1e26b6110b80b64a774f37cc2b98fa211fc8dc71`, completed its standard-runner job in 8 minutes 22 seconds: workload/locked restore 23 seconds, Debug build 36 seconds, preservation 5 seconds, early artifact upload 1 second, two sequential Simulator capture sessions 6 minutes 53 seconds. The app archive is 49,943,417 bytes with SHA-256 `494c9e94afc0bab3b843ddd23c32c371761d2d14ad8ec64d0b6cfab6ef7f3544`.

Although launch commands returned PIDs and both native PNG sizes were correct, visual inspection found **SpringBoard/Home on both**, not the app UI. That is not a successful UI witness. The next diagnostic iteration adds retained-process, runtime-log and public canned-session journal evidence, and an optional `reuse_run_id` input that hash-verifies the original archive and skips every SDK/restore/build step. A successful command or PNG size is never a substitute for inspecting its content.

[Capture-only diagnostic run 34551985540](https://github.com/LathanHarper/DapperDan/actions/runs/34551985540) reproduced the exit on both devices without compilation. Both native crash reports identify `SIGKILL (Code Signature Invalid)` with `CODESIGNING / Invalid Page` before managed entry; no app journal was created. Signature display showed only an ad-hoc linker signature with unbound Info.plist and no sealed resources. This evidence points at the forced signing-disabled Simulator output, not business logic or a debugger wait.

The optional `repair_adhoc` input operates on a fresh copy of the retained generic app, explicitly signs each nested Mach-O binary inside-out and the app last with the pseudo-identity `-`, preserves existing entitlements without inventing new ones, and verifies signatures. It preserves a transformed archive and a before/after receipt before capture, checks managed/data files did not change, and re-checks the original archive's hash. This is **Sign to Run Locally**, not Apple account signing or Ad Hoc device distribution. No certificate, provisioning profile, private key, or App Store upload is involved. First-phone process survival gates the second cold boot; images still require visual review.

[Repair run 34552794252](https://github.com/LathanHarper/DapperDan/actions/runs/34552794252) produced a visually verified native iPhone image of the actual Dapper Dan page after re-sealing the retained app, confirming the signature repair fixes its early loader failure. It made no compilation or managed/data change. The screenshot command nevertheless timed out after producing the complete image, so the first-device guard stopped the run before iPad. The following automation correction writes prelaunch diagnostics directly to a file descriptor to avoid pipe backpressure during synchronous commands, always gathers process evidence on failure, and only retains a timed-out capture as usable when its PNG has a complete final IEND chunk, correct native dimensions, a surviving app process, and subsequent visual inspection. The timeout remains explicit in its receipt.

- [Apple: ad-hoc signatures have no signing identity](https://developer.apple.com/documentation/security/seccodesignatureflags/adhoc)
- [Apple: sign nested components inside-out, verify recursively](https://developer.apple.com/library/archive/technotes/tn2206/)

## Final passing receipt

- App source: `1e26b6110b80b64a774f37cc2b98fa211fc8dc71`.
- Executed automation: `c9ab6ef55d6402a55eb1f3b4d31fb65ce4a9fca9`.
- Run: `34553339801`, standard `macos-26`, 2026-09-11 02:06:52–02:16:23 UTC, **9 minutes 31 seconds** total.
- Download: 2 seconds; archive verification/extraction: 1 second; signature-copy/verification/preservation: 11 seconds; transformed artifact upload: 1 second; sequential native capture step: 9 minutes 4 seconds.
- SDK install, workloads, package restore and app compilation: **all skipped**. Across these four controlled experiments, the generic app was compiled only once (36 seconds); later attempts reused it.
- Runtime: iOS 26.5, Xcode 26.6, Apple Silicon.

| Device | Native PNG | Session time | Verified witness |
| --- | --- | --- | --- |
| iPhone 14 Plus | 1284 × 2778 | 288 seconds | Full Dapper Dan page, surviving process, app journal |
| iPad Pro 13-inch (M4) | 2064 × 2752 | 248 seconds | Full Dapper Dan page, surviving process, app journal |

Phone PNG SHA-256: `2d0f9e91ad8c4afb0ae937647006efa0cdc6ec82932ab9858b565b7f998b4011`.

iPad PNG SHA-256: `f5769a6c5a9c4df9bac5271d45b0b1006bf06f521dc0531dc759569a403d181d`.

Final transformed app archive: 49,939,700 bytes; SHA-256 `c24471ddb2b1bfda868d1eb972f240aafebcc018fd3061b519ca77fe707ef0d3`. Its receipt records the exact inside-out signature transformation and confirms all 246 managed/data files remained unchanged. The original retained archive and its SHA-256 remain unchanged. Original and transformed receipts plus screenshots/logs were saved locally.

Five local automation contracts passed. The original app build had zero errors and 141 existing warnings; this narrow automation task did not modify app business logic or attempt unrelated warning cleanup. No private source or signing credential entered this repository, no default-branch change or pull request was made, and no further proof runs were needed after the final visual verification.

## Primary references checked 2026-09-10

- [GitHub manual workflows and branch selection](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)
- [GitHub hosted macOS 26 runner image and installed Xcode/runtime inventory](https://github.com/actions/runner-images/blob/main/images/macos/macos-26-arm64-Readme.md)
- [GitHub Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [.NET MAUI iOS command-line builds and Simulator selection](https://learn.microsoft.com/en-us/dotnet/maui/ios/cli?view=net-maui-10.0)
- [Apple screenshot dimensions](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/)
