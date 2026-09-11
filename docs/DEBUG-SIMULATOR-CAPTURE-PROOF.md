# Branch-only Debug Simulator capture proof

This independently authored public automation answers a narrow platform question: can one unsigned .NET MAUI Debug Simulator app be preserved and reused to capture native large-iPhone and large-iPad images on a standard Apple Silicon GitHub runner?

It uses only the existing public Dapper Dan app. It imports no private source, assets, configuration, credentials, screenshots, or logs. These generic images are **not screenshots of any other app and cannot be submitted as another app's store assets**.

## Scope and dispatch

The `codex/simulator-capture-proof` branch alone replaces `.github/workflows/ios-unsigned.yml` with a manual-only, read-only, unsigned Debug capture workflow. Default `main`, its automatic Release canary, and the protected TestFlight workflow remain unchanged. Do not merge this branch merely to dispatch it.

GitHub requires a manually dispatched workflow to exist on the default branch. The existing registered workflow ID is reused with the explicit proof branch; the proof branch has no automatic triggers. Confirm repository visibility is PUBLIC and inspect the exact branch workflow before running:

```powershell
gh workflow run ios-unsigned.yml --repo LathanHarper/DapperDan --ref codex/simulator-capture-proof
```

The job additionally rejects every repository/ref other than this public proof branch. It uses standard `macos-26`, not a larger runner. No environment, signing material, App Store upload, or secret references are present. Standard public-repository hosted compute is free under GitHub's current policy; artifact storage is a separate account meter, so these proof artifacts use one-day retention.

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

## Primary references checked 2026-09-10

- [GitHub manual workflows and branch selection](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)
- [GitHub hosted macOS 26 runner image and installed Xcode/runtime inventory](https://github.com/actions/runner-images/blob/main/images/macos/macos-26-arm64-Readme.md)
- [GitHub Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [.NET MAUI iOS command-line builds and Simulator selection](https://learn.microsoft.com/en-us/dotnet/maui/ios/cli?view=net-maui-10.0)
- [Apple screenshot dimensions](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/)
