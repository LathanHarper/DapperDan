# Public Flexler Simulator startup proof

Manually select `proof_scope: flexler` in **iOS unsigned proof**. That selection
skips the normal data/device jobs and runs one standard `macos-26` job, bounded
to 35 minutes. The default `full` selection and push/PR behavior are unchanged.
`proof_configuration` applies only to the Flexler scope: it defaults to `Debug`,
with `Release` available as an explicit separate comparison. Full scope continues
its existing Release builds regardless of this input.

Focused manual Debug and Release runs have separate concurrency groups, so one
can run alongside the other without cancelling it. A newer run of the same
configuration on the same ref still supersedes its older run. Full scope and
push/PR checks keep their original concurrency group. A Debug run using this
updated workflow also leaves an already-running legacy Release proof alone.
Each configuration remains a single bounded job; no automatic second run is
scheduled, and neither focused configuration retains artifacts.

The job builds the complete public app for `iossimulator-arm64`, with
`FlexlerShowcaseProof=true` selecting the actual Flexler page at startup. It uses
the repository's pinned SDK/workload and locked package graph. Build output and
startup receipts are explicitly labeled Debug or Release; the helper reads only
that configuration's output directory.

The workflow does not override interpreter, trimming or LLVM properties. It logs
their evaluated values before compiling. With this repository's settings,
Debug uses MAUI's default interpreter and avoids the Release-only `-all` rule;
Release preserves Dapper Dan's `MtouchInterpreter=-all`, which AOT-compiles
assemblies while allowing interpreter fallback for runtime-generated code.
Microsoft documents `UseInterpreter=true` as MAUI's Debug default, and LLVM AOT
as an iOS Release default. This makes Debug a useful faster startup diagnostic,
not evidence that Release AOT will behave identically or a guaranteed build time.
See [Microsoft's interpreter guide](https://learn.microsoft.com/en-us/dotnet/maui/macios/interpreter?view=net-maui-10.0)
and [build properties](https://learn.microsoft.com/en-us/dotnet/ios/building-apps/build-properties#mtouchinterpreter).

Neither selection reproduces standalone Flexler's device configuration. A Debug
pass cannot establish Release AOT correctness; keep the Release result distinct.
Windows can prepare source and checks, then request this public Mac job. The Mac
still compiles and packages the native Simulator app; no Windows-built native
app is handed to the runner. This lane is not Pair to Mac or an incremental
remote build cache, and creates a fresh complete build for each manual run.

Before installation, the ephemeral Simulator output receives local ad-hoc
seals (`codesign --sign -`), nested native binaries first and the app last.
This is a local loader requirement already demonstrated by the public capture
canary. No Apple identity, certificate, key, provisioning profile, TestFlight
upload or distributable release is involved.

The helper selects one stock available iPad from an installed runtime. It never
creates a replacement device or downloads a runtime. It requires the exact
`FlexlerPageLoaded` journal checkpoint, no recorded exception, and eight more
seconds with the launched process alive, within a 60-second observation window.
Missing journals, unrelated page checkpoints and early process death fail.

Original journal files stay only in the ephemeral runner's temporary directory.
Workflow logs contain allowlisted checkpoint/type/boolean/numeric fields, not
exception messages, stacks, paths, identities or arbitrary journal text. The
summary records build outcome and native startup outcome. There is no artifact
upload, cache or external test service, so this lane creates no retained-artifact
storage charge. Standard public-repository runner compute is free under
[GitHub's billing policy](https://docs.github.com/en/billing/concepts/product-billing/github-actions).

This is startup evidence, not interaction coverage, visual inspection, device
installation proof, or production screenshot capture. Check the standalone app
separately before another release attempt.

Local parser/selection checks: `node --test tools/flexler-proof/*.test.mjs`.
