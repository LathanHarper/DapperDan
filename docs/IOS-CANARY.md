# iOS canary operating guide

The two build lanes share source but never share trust.

Both lanes restore Prism 9. The maintainer and every developer whose work is built must first be covered by a valid Prism Community or Commercial license. The workflow does not grant or silently accept a license; see [`PRISM-LICENSING.md`](PRISM-LICENSING.md).

## Secret-free public lane

`.github/workflows/ios-unsigned.yml` runs on pushes and pull requests. It has read-only repository permission, no environment, no `secrets.*` references, and no signing material. A Linux prerequisite regenerates the EF compiled model and deterministic SQLite seed, rejects drift, checks the database, and runs the regression tests. One macOS job then builds both simulator and unsigned device bundles with the build-only display name `Dapper Dan - UNSIGNED PROOF`, verifies that each contains the exact healthy seed without WAL/SHM sidecars, and publishes the complete application builds with archive checksums, sizes, toolchain details, binary logs, license notices, and a `RETURN-TO-SENDER.txt` warning for 14 days.

An unsigned `.app` proves that the .NET/iOS/Xcode toolchain compiled and packaged the source. The simulator build can be used only with compatible simulator tooling; the unsigned device build cannot be installed on a physical iPad or enter TestFlight. The signed lane is still the physical-device proof that the compiled model, first-launch database copy, and normal EF queries survive iOS AOT.

This slice does not enable EF's experimental precompiled-query interceptors or NativeAOT. iOS Release instead uses Microsoft's documented [`MtouchInterpreter=-all`](https://learn.microsoft.com/dotnet/maui/macios/interpreter?view=net-maui-10.0#enable-the-interpreter) mode: every normal assembly remains AOT-compiled, while Mono retains its interpreter for runtime-generated delegates. Keeping the build-12 compiled model in place for this slice isolates that single query-runtime change.

## Recovering an iOS launch journal

Dapper Dan starts a small private JSONL journal near the start of managed `Program.Main`, before `UIApplication.Main`, MAUI, Prism, EF Core, or SQLite initialization. Each checkpoint is appended synchronously and asks the OS to flush it to storage. Managed, unobserved-task, managed-to-Objective-C, and Objective-C-to-managed exception hooks add bounded exception details when those runtime paths are available. The recorder does not change exception-marshaling modes or task-observation behavior.

Each session's first `launch` record also contains `isDynamicCodeSupported` and `isDynamicCodeCompiled`. The expected physical-device values for the Mono AOT plus `MtouchInterpreter=-all` lane are `true` and `false`: EF may create its ordinary query delegates through the interpreter, but iOS still does not JIT-compile them.

After a crash or launch failure:

1. Open Dapper Dan one more time. Before MAUI starts, this seals the prior `.active.jsonl` file as `.interrupted.jsonl` and copies it into the app's Documents directory.
2. Open **Files → On My iPad → Dapper Dan → DapperDan Diagnostics**.
3. Review, then share or copy the newest `.interrupted.jsonl` file. It is line-delimited JSON; read every complete line even if the final line was interrupted mid-write. Redaction is best effort, so inspect exception text before posting a report publicly.
4. Find the final complete `checkpoint` record. A missing matching `Ready` marker isolates the startup seam that did not return. In particular, `CompiledModelEnter` without `CompiledModelReady` isolates the generated EF compiled-model static initialization.

An interrupted session is evidence that no clean process-return marker was written, not proof of a crash. Force-quit, watchdog termination, memory pressure, device shutdown, and normal iOS process reclamation can look the same. Managed hooks also cannot guarantee capture of native signals, aborts, stack overflow, dyld failures before `Main`, watchdog termination, or jetsam. For those cases, the last durable checkpoint and Apple's TestFlight crash report are the next tools.

Build 11's first physical-iPad journal ended at `CompiledModelEnter`, after SQLite, MAUI construction, dependency injection, and App XAML had all returned. EF Core 10.0.5's generated compiled-model initializer normally starts a helper thread with a 10 MiB requested stack and immediately joins it. EF tracks [this generated thread path freezing MAUI applications](https://github.com/dotnet/efcore/issues/32346) under its still-open [compiled-model initialization work](https://github.com/dotnet/efcore/issues/31370). The iOS entry point now enables EF's generated `Microsoft.EntityFrameworkCore.Issue31751` compatibility switch before `UIApplication.Main`, selecting direct initialization for Dapper Dan's two-entity model. This retains the checked-in compiled model and normal iOS AOT; it does not fall back to design-time model building and it does not edit generated files.

Build 12 then booted and rendered both Dapper Dan pages on the physical iPad, but the first ordinary EF query reported `Query wasn't precompiled and dynamic code isn't supported with NativeAOT.` The app was actually on .NET 10's default Mono AOT runtime, not NativeAOT; EF uses that message whenever `RuntimeFeature.IsDynamicCodeSupported` is false. The next canary keeps the known-good startup path and enables the supported interpreter fallback so the same query can use EF's normal runtime compilation path.

The journal has no LAN/cloud upload, background sender, crash SDK, database dependency, or UI dependency. It records allowlisted runtime identity, stage names, and bounded exception type/message/stack data, then redacts known container paths, email-shaped text, URLs, and bearer values. It does not inspect `Exception.Data` or intentionally collect Keiki rows, device identifiers, accounts, environment variables, or signing material. The app does not transmit journals; Files access and device-backup behavior remain under iOS and the tester's settings.

## Comparing native voice timbre

The **Canary → Native voice A/B/C** card is a physical-device probe for distorted `AVSpeechSynthesizer` output. Every trial speaks the same neutral sentence and leaves rate, pitch, and volume at their native defaults. Its RichButtons use `FeedbackMode=None`, so a tap sound cannot overlap the first phonemes.

| Trial | Voice selection | Speech audio session | Experimental purpose |
| --- | --- | --- | --- |
| A | `AVSpeechSynthesisVoice.FromLanguage("en-US")` | Shared application session | Language-default baseline |
| B | Installed en-US voices ranked by quality, then ordinal name | Shared application session | Reproduces the unsafe assumption that quality/name ranking implies a natural voice |
| C | `AVSpeechSynthesisVoice.FromLanguage("en-US")` | Separate Apple-managed session | Changes only audio-session ownership from A |

Run A, B, and C with the same device volume and output route. The result card reports the selected voice name, identifier, language, quality, matching voice count, and read-only snapshots of the shared application audio session before, at the start of, and after speech. Trial C's speech session is separate and is not exposed through `AVAudioSession.SharedInstance`; the shared-session snapshots are retained only to prove that the canary did not mutate it. Voice quality is a fidelity/download tier, not a naturalness or novelty guarantee.

Interpret the physical result this way:

- A sounds natural and B sounds distorted: arbitrary installed-voice ranking selected the wrong kind of voice. Use a language default instead of treating quality or enumeration order as a naturalness score.
- A sounds distorted and C sounds natural: Dapper Dan's configured shared application-session path is implicated. Prefer Apple-managed speech-session ownership unless the app has a demonstrated need to own interruption, mixing, or ducking policy.
- A, B, and C all sound distorted: keep voice selection and session ownership constant, then investigate the output route, downloaded voice asset, iOS version, and device-level accessibility/audio settings.
- All three sound natural while another app remains distorted: reproduce that app's remaining audio-session difference here as a new independent trial before changing production code.

The speech canary service never configures, activates, or deactivates the shared audio session. Dapper Dan's existing RichButton sound player primes that shared session to Ambient + MixWithOthers when page buttons load—even when these three trial buttons suppress tap playback—so A and B intentionally observe the app's normal configured baseline. The result snapshots make that state explicit. The canary does not write voice details to disk or transmit results. Selected metadata exists only on screen; inspect it before sharing a screenshot from a device with Personal Voice installed. The implementation is an independently authored platform sample, not a copy of private application code.

Those bundles are complete Dapper Dan application products with substantial CodeCrafty functionality. They do not redistribute Prism as a NuGet package, loose development library, SDK, wrapper, control suite, low-code platform, or other reusable development component. The responsible developers must remain properly licensed for Prism, and the repository license and third-party notices stay alongside every proof artifact.

## Protected TestFlight lane

`.github/workflows/testflight.yml` is manual, accepts a numeric Apple build number, and runs only from `main`. It rebuilds source rather than consuming a pull-request artifact, verifies the provisioning profile against `net.codecrafty.dapperdan`, signs the IPA, validates its signature and bundle identifier, then uploads directly to App Store Connect. The signed IPA is never published as a GitHub artifact.

The IPA verification also locates exactly one packaged `dapper-dan-seed-v1.db3`, byte-compares it with the reviewed source asset, verifies its identity/schema/integrity/foreign keys, and rejects SQLite sidecars. The writable destination is `dapper-dan-canary-v1.db3`; that new versioned name ensures build 10 exercises first-install copy even over an older Dapper Dan TestFlight installation.

Before enabling it:

1. Confirm that every responsible developer has a valid Prism license as recorded in `PRISM-LICENSING.md`.
2. Renew the Apple Developer Program membership for the CodeCrafty.net team.
3. Register `net.codecrafty.dapperdan` and create its App Store Connect app record.
4. Create an Apple Distribution certificate and an App Store provisioning profile dedicated to this bundle ID.
5. Create an App Store Connect API key suitable for the upload action. A team key can reach more than this canary, so protect and rotate it accordingly.
6. Create a GitHub environment named `testflight-canary`. Restrict deployments to `main`, require a reviewer, prevent self-review, and disable administrator bypass where available.
7. Configure these environment variables:
   - `APPSTORE_ISSUER_ID`
   - `APPSTORE_API_KEY_ID`
   - `IOS_CODESIGN_IDENTITY`
8. Configure these environment secrets:
   - `IOS_DISTRIBUTION_CERT_P12_BASE64`
   - `IOS_DISTRIBUTION_CERT_P12_PASSWORD`
   - `IOS_APPSTORE_PROFILE_BASE64`
   - `APPSTORE_API_PRIVATE_KEY` (raw `.p8` content)

Keep the default GitHub token read-only, protect `main`, require review for workflow/toolchain/project-file changes, allow only approved actions pinned to full SHAs, and never attach a self-hosted runner to untrusted public pull requests.

## Toolchain pin

The repository pins .NET SDK `10.0.302` and workload set `10.0.302.1`. CI selects Xcode 26.6 on GitHub's ARM64 `macos-26` image. Upgrade the SDK, workload set, runner image, and Xcode selection as one reviewed change.

## Compute math

Public repositories currently receive standard GitHub-hosted runner use without a per-minute charge. If this build is mirrored into a private repository after its allowance, the planning formula is:

`build cost = billed macOS minutes × current macOS per-minute rate`

At the previously checked `$0.062/min` rate, 20, 30, and 40 minutes are about `$1.24`, `$1.86`, and `$2.48`. Verify [GitHub's current runner pricing](https://docs.github.com/billing/reference/actions-runner-pricing) before budgeting; image class and pricing can change. Keeping both unsigned targets in one job avoids paying startup/restore time twice.
