# iOS canary operating guide

The two build lanes share source but never share trust.

Both lanes restore Prism 9. The maintainer and every developer whose work is built must first be covered by a valid Prism Community or Commercial license. The workflow does not grant or silently accept a license; see [`PRISM-LICENSING.md`](PRISM-LICENSING.md).

## Secret-free public lane

`.github/workflows/ios-unsigned.yml` runs on pushes and pull requests. It has read-only repository permission, no environment, no `secrets.*` references, and no signing material. A Linux prerequisite regenerates the EF compiled model and deterministic SQLite seed, rejects drift, checks the database, and runs the regression tests. One macOS job then builds both simulator and unsigned device bundles with the build-only display name `Dapper Dan - UNSIGNED PROOF`, verifies that each contains the exact healthy seed without WAL/SHM sidecars, and publishes the complete application builds with archive checksums, sizes, toolchain details, binary logs, license notices, and a `RETURN-TO-SENDER.txt` warning for 14 days.

An unsigned `.app` proves that the .NET/iOS/Xcode toolchain compiled and packaged the source. The simulator build can be used only with compatible simulator tooling; the unsigned device build cannot be installed on a physical iPad or enter TestFlight. The signed lane is still the physical-device proof that the compiled model, first-launch database copy, and normal EF queries survive iOS AOT.

This slice does not enable EF's experimental precompiled-query interceptors. Keeping those out of the build isolates the compiled-model and packaged-database fix from a separate query-generation experiment.

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
