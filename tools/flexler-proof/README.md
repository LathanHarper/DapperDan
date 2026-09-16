# Public Flexler Simulator startup proof

Manually select `proof_scope: flexler` in **iOS unsigned proof**. That selection
skips the normal data/device jobs and runs one standard `macos-26` job. The
default `full` selection and push/PR behavior are unchanged. The focused inputs
select separate comparisons:

| Configuration | Runtime profile | Purpose |
| --- | --- | --- |
| `Debug` (default) | `host` (default) | Faster startup diagnostic using Dapper Dan's Debug defaults. |
| `Release` | `host` | Release startup with Dapper Dan's existing interpreter setting. |
| `Release` | `aot-trim` | Explicit partial trimming and Mono AOT without interpreter fallback. |

`Debug` with `aot-trim` is rejected. Full scope continues its existing Release
builds regardless of these focused inputs. Focused runs have separate concurrency
groups for each configuration and runtime profile. Different combinations can run
alongside each other; a newer run of the same combination on the same ref
supersedes its older run. Full scope and push/PR checks retain their original
concurrency group. Each focused run is one bounded job with no automatic retry.

The job builds the complete public app for `iossimulator-arm64`, with
`FlexlerShowcaseProof=true` selecting the actual Flexler page at startup. It uses
the pinned SDK/workload and locked package graph. Receipts identify the source
commit, configuration and runtime profile; the helper reads that configuration's
output directory on the fresh runner.

## Runtime profiles

`host` does not override interpreter, trimming or LLVM properties. Debug uses
MAUI's default interpreter and avoids the Release-only `-all` rule. Release
preserves Dapper Dan's `MtouchInterpreter=-all`, which AOT-compiles assemblies
while allowing interpreter fallback for runtime-generated code. See
[Microsoft's interpreter guide](https://learn.microsoft.com/en-us/dotnet/maui/macios/interpreter?view=net-maui-10.0)
and [build properties](https://learn.microsoft.com/en-us/dotnet/ios/building-apps/build-properties#mtouchinterpreter).

`aot-trim` deliberately overrides those host settings for this diagnostic only:

| Evaluated property | Required value |
| --- | --- |
| `UseInterpreter` | `false` |
| `MtouchInterpreter` | Empty |
| `TrimMode` | `partial` |
| `UseMonoRuntime` | `true` |
| `PublishAot` | `false` |
| `MtouchUseLlvm` | `true` |
| `Registrar` | `managed-static` |
| `MauiXamlInflator` | `SourceGen` |

This is Mono AOT, not NativeAOT; `PublishAot=false` does not disable Mono's iOS
AOT compilation. The same options flow through restore, evaluated-property
inspection and build. The property checker rejects a strict-profile mismatch
before compilation. Both profiles must preserve the public app's `SourceGen`
XAML inflator.

The strict startup gate also requires the actual launch record to report both
`isDynamicCodeSupported === false` and `isDynamicCodeCompiled === false`.
Missing values, strings, or either flag being true fail; evaluated build settings
alone cannot satisfy this runtime check. The host profile records these flags
without imposing the strict requirement.

## Time and cost bounds

| Configuration | Whole job | Compile step |
| --- | --- | --- |
| `Debug` | 55 minutes | 25 minutes |
| `Release` | 180 minutes | 150 minutes |

Workload/package restore has a separate 12-minute limit. The boot helper has a
nine-minute aggregate operation budget covering local sealing, inventory,
Simulator boot, installation and startup observation. Commands share the
remaining budget instead of each receiving a fresh full allowance. Cleanup is
bounded separately: up to 15 seconds to terminate the app and 30 seconds to shut
down a Simulator booted by this run. The entire boot step has a 12-minute limit
to leave room for final journal capture, reporting and cleanup.

These are ceilings, not expected durations. A previous public canary needed
64 minutes for its Simulator build alone. The earlier 25-minute Flexler Release
attempt timed out with AOT/LLVM work still active; that result did not establish
an ILLink hang or reach native startup. Keep incomplete compilation distinct from
an application crash and from a completed Release proof.

The build prints a progress sample every minute: elapsed time and aggregate CPU,
memory and process counts for known compiler executables. It does not print
process arguments or paths, infer a stalled stage from quiet console output, or
retry the build. GitHub's step and job limits remain authoritative.

The focused lane has no artifact upload, cache or external test service. Original
journals and build files remain on the ephemeral runner, so it creates no
retained-artifact storage charge. Standard public-repository runner compute is
free under [GitHub's billing policy](https://docs.github.com/en/billing/concepts/product-billing/github-actions).
This statement applies to the focused lane: the unchanged `full` workflow
retains unsigned bundles and build evidence for 14 days.

## Native startup and diagnostics

Before installation, the ephemeral Simulator output receives local ad-hoc seals
(`codesign --sign -`), nested native binaries first and the app last. This satisfies
the Simulator loader without an Apple identity, certificate, key or provisioning
profile. It produces neither a TestFlight upload nor a distributable release.

The helper selects one stock available iPad from the newest installed available
iOS runtime with a matching device. It creates no replacement device and
downloads no runtime. It prints each stage with elapsed time, then requires the
exact `FlexlerPageLoaded` and `FlexlerViewModelReady` checkpoints, no recorded exception, and eight more seconds
with the launched process alive, within a 60-second observation window. Missing
journals, ambiguous multiple launches, unrelated page checkpoints and early
process death fail. `aot-trim` additionally applies the runtime flag gate above.

The existing durable crash journal covers app XAML initialization, navigation,
Flexler page XAML/appearance/loading, PanelBoss creation and its host constructor,
the main view-model constructor, atmosphere XAML, and iOS material view creation,
handler connection and shared image-context creation. Constructor/handler failure
boundaries capture and rethrow; there is no exception swallowing or frame-by-frame
logging. These checkpoints locate the last completed startup seam, not a presumed
root cause.

The semantic dictionaries remain anonymous XAML resources. Instrumentation adds
no `x:Class`, typed dictionary constructors, static dictionary references, split
resource loads or early resource probes that could change trimming roots and mask
the late-bound failure under investigation. Their loading remains covered by the
existing `AppXamlEnter`/`AppXamlReady` boundary and exception capture.

Raw journal files stay in the runner's temporary directory. Printed journal
records contain bounded checkpoint, source, boolean and numeric fields. Exception
diagnostics emit only recognized exception types, HRESULTs, exact public resource
key matches, allowlisted public methods, and repository-relative source locations
validated against tracked public files and their line counts. They do not print
raw messages, stacks, absolute paths, identities or arbitrary journal text.
Resource-key matches and inner-exception type mentions are clues, not proof of
causation or a reconstructed exception chain; no match does not rule out a fault.
The summary records restore, build, native startup and runtime-profile outcomes.

## What a pass establishes

A pass establishes startup for the exact public Dapper Dan configuration and
runtime profile tested. Debug success cannot establish Release AOT correctness.
Even strict Release Simulator success does not establish standalone Flexler's
physical-device configuration, signing, TestFlight installation, UI interactions,
visual correctness or production screenshots. Exercise the full feature on the
owner's physical iPad using the validated Dapper Dan TestFlight build, and keep
the standalone app's release gates separate.

Windows can prepare source and checks, then request this public Mac job. The Mac
still compiles and packages the native Simulator app; no Windows-built native
app is handed to the runner. This is a fresh complete build, not Pair to Mac or
an incremental remote build cache.

Local parser, profile, selection and sanitization checks:
`node --test tools/flexler-proof/*.test.mjs`.
