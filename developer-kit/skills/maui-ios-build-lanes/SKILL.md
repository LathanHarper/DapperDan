---
name: maui-ios-build-lanes
description: Prepare or change CodeCrafty MAUI iOS CI and TestFlight workflows; select Debug Simulator checks versus Release physical-device builds and preserve learned release constraints.
---

# MAUI iOS Build Lanes

Source: BrainSwapLite kit and fragment `MAUI iOS Build Lanes`. Refresh these copies when the fragment changes.

**CodeCrafty's standing MAUI iOS rule, September 17, 2026.**

- Simulator checks use Debug. Do not run Release Simulator builds in routine checks, PR gates, or release preparation. Test Release trimming/AOT/signing on ios-arm64 physical-device candidates. A special Release Simulator experiment requires Crafty's explicit request for that experiment; free compute is not an exception.
- Before changing or dispatching an iOS workflow, read the MAUI iOS Build Lanes kit or installed maui-ios-build-lanes skill and the project's release recipe. Inspect push/PR workflows too: publishing a documentation or workflow edit can launch an expensive automatic build.
- Separate local managed/resource preflight, Debug Simulator exploration, full native Mac Release publish, and local Apple-processing checks. Windows Compile does not produce a native iOS release. Keep the verified upload wait-for-processing=false flow; verify successful upload commitment before local processing checks.
- Verify configuration and RID from evaluated settings and commands before running. A job's title is not proof. Keep SDK/workload/package/signing identity changes explicit. Record interpreter, trim and IL-stripping differences; changing interpreter fallback can change SDK-derived behavior.
- Preserve exact commit, payload hash, phase timings, actual device result and matching symbols/startup diagnostics. A nearest exported symbol is not a symbolicated crash. Measure compiler phases before claiming which optimization caused a long build.
- Apply the existing GitHub spend/public-content policy. Free public experiments remain allowed; private paid execution remains final verified TestFlight attempts only. Avoid duplicate dispatches.
- When extracting a new app or moving a proof, carry applicable functional kits and their AGENTS load pointers into the destination. Read them there and compare actual workflows against the rules; copying product source alone is not a completed handoff.
- At a meaningful new lesson, update the existing BrainSwapLite fragment/kit, read it back, and refresh its project/installed skill copies and AGENTS load pointer. Keep raw logs, secrets and private operational data outside reusable public guidance. A saved receipt alone is not a loaded kit. Do not apply a generated kit over unrelated project instructions; preview and preserve them.
