# Bottom-panel safe-area canary

This public-neutral sample observes a child Grid inside the existing PanelBoss bottom-input lane. It does not change PanelBoss or RichButton, and is not an app-release build.

## Controlled geometry

- Panel height: 124 logical points.
- Rows: 52 and `*`; five equal-width action cells in the second row.
- Each border keeps its `2,2,2,16` margin and a centered two-label stack (12/9-point text).
- The otherwise empty body has a small measurement readout and a named clearance contract with the bottom panel.
- Four sample actions plus More have stable IDs and direct commands. They only update the sample readout.

The baseline leaves the child action Grid's `SafeAreaEdges` at its default. The comparison sets only that Grid to `SafeAreaEdges.None` before initial layout. Both cases use the same executable, dimensions, controls, fonts, margins, and content. The centered stack is intentional: replacing it would introduce another variable.

Without an inset, the expected border height is `124 - 52 - 18 = 54` points. Whether a nested inset consumes the remaining row is the hypothesis under test, not a hard-coded expected result.

## Run the sample

The existing default app route is unchanged. Add `-p:DapperDanBottomPanelCanary=true` to a **Debug** build to open this page. The property has no effect on Release startup. The environment variable `DAPPERDAN_PANEL_CASE` selects `baseline` (also the default) or `safe-area-none`.

For a Simulator launch, `SIMCTL_CHILD_DAPPERDAN_PANEL_CASE` passes the case into the app. The manual workflow on `codex/panelboss-bottom-inset-canary` builds once, retains the original product, seals a separate Simulator copy with credential-free ad-hoc signing, then runs both cases on each selected iPhone and iPad. It boots one Simulator at a time. The comparison is a diagnostic experiment, not a shipping fix.

The page writes `bottom-panel-canary.json` in its app data directory after layout. It records the MAUI-arranged panel/header rectangles, all five button/border rectangles, the native window safe-area insets, and the case. Rectangles are logical points in their respective parents' coordinate spaces, not UIKit frames or global screen coordinates. Measurements never drive layout. They contain no user content, device identifiers, or upload behavior.

Capture fails if the process is absent, the native PNG is incomplete, or measurement evidence is missing/invalid. A completed baseline and comparison are both required. Original and ad-hoc Simulator products are retained separately, with hashes and license notices, before capture. Native screenshots and partial failure evidence are preserved too. Download artifacts promptly; the workflow requests one-day retention.

## Cost and scope

Use only this **public** repository's standard `macos-26` runner. GitHub documents standard public-repository runner usage as free; larger runners are billable. No paid runner, signing identity, certificate, provisioning profile, or store upload is part of this workflow. Artifact storage has separate account limits; retention is deliberately short and this is not a promise about unrelated account charges. [GitHub billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)

Local source checks: `node --test tools/simulator-proof/*.test.mjs`. Format the page with XAML Styler's CLI and the maintainer's normal configuration, rather than manually imitating attribute order. Neither source checks nor formatting establish native layout behavior; native evidence is required for that conclusion.

Android remains the behavior baseline. This page can use the existing Visual Studio-managed Android stack and an already-running emulator; do not install another SDK or start an extra emulator to run it. An iOS finding does not by itself establish Android equivalence or Release-mode acceptance.

## Observed result — 2026-09-11

[Public run 34578015605](https://github.com/LathanHarper/DapperDan/actions/runs/34578015605), source `cf975337197aaa0118480c45ef12ce2067d1a328`, compiled the Simulator app in 74.80 seconds. All 19 local/runner Node contracts passed. Compilation had zero errors and 141 existing-project warnings; none named the new canary files. SDK, workload, package pins, PanelBoss, RichButton, and the protected TestFlight workflow were unchanged.

The **iPhone 14 Plus / iOS 26.5 comparison completed**. Both native PNGs are 1284 by 2778 pixels, both app processes survived capture, and both PNG/measurement hashes match the retained receipt. Visual inspection showed the baseline clipping both text lines and the comparison showing all five complete button faces.

| iPhone measurement (points) | Baseline | SafeAreaEdges.None |
| --- | ---: | ---: |
| Outer panel | 124 | 124 |
| Header row | 52 | 52 |
| Each RichButton | 38 | 72 |
| Each painted border | 20 | 54 |
| Native window bottom inset | 34 | 34 |

Only the child action Grid's safe-area setting changed. In this reproduction its default `Container` setting consumes the 34-point bottom inset inside the fixed-height panel, reducing the star row. Explicit `None` restores those 34 points. This confirms the mechanism in the sample; it is not a production patch, Android canary result, or Release acceptance claim.

**The overall workflow failed:** the iPad launch command timed out and the capture step reached its 12-minute limit. No complete iPad A/B evidence was obtained. Its partial journal/receipt must not be treated as a passing iPad capture. The completed iPhone comparison remains valid independent evidence. No retry or additional build was started.

Both original and prepared Simulator archives, their notices, source/toolchain receipts, and the available capture evidence were downloaded for local safekeeping. Verified hashes:

- Original archive SHA-256: `2d168cb130244455e73e72f40a2124ef70e084f63af1e63aac89186c0a6ae2ff`
- Prepared archive SHA-256: `3c7bd7257f693b838dcf5acdfc9a2287bdc098cfa082ae7101f5536aed9526cf`
- Same prepared executable in both iPhone cases: `4faa7bfd235b22bf6c2761dbbaa9889fe23f7c72072cb78eb1c42426b69aebfc`

GitHub's completed-run timing response reported `billable.MACOS.total_ms: 0` (one job), with `run_duration_ms: 980000`. That verifies zero billable runner time for this run, not an account-wide storage/billing assertion.
