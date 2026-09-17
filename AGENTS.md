# Dapper Dan working contract

- Native iOS builds are manual, purposeful experiments or signed delivery attempts. Do not add push/PR native builds or require native compilation merely to merge; do not duplicate an imminent signed candidate with an unsigned merge-gate build. Free compute still consumes owner time. Keep other PR protections intact.

- Before editing or dispatching iOS workflows, load [MAUI iOS Build Lanes](developer-kit/skills/maui-ios-build-lanes/SKILL.md), backed by the BrainSwapLite kit of the same name. Simulator checks use Debug; Release validation targets physical devices. Inspect automatic push/PR triggers before publishing.
- When extracting an app or moving a proof to another repository, carry applicable functional kits plus their AGENTS load pointers, read them in the destination and verify workflow conformance. Preserve destination instructions when composing BrainSwapLite output. Save reusable lessons in the kit and refresh copies; receipts alone are not a handoff.

- Treat the entire repository, issue history, CI log, and artifact set as public.
- CodeCrafty explicitly authorized a full, faithful FleXler showcase under `src/DapperDan/Features/Flexler` on September 15, 2026. This includes its developer-tool functionality, XAML, look and feel, semantic resource values, local supporting components, and approved brand images. Preserve that scope rather than reducing it to an abstract reproduction.
- This authorization does not include unrelated private product code or history, private application/account identities, endpoints, operational configuration, signing material, credentials, screenshots, or personal logs. Keep the standalone Flexler repository private. Inspect every published change for material outside the approved showcase.
- For other private-app quirks, reproduce the smallest independently authored platform sample, a regression test when practical, and neutral names/data.
- Host the FleXler showcase inside Dapper Dan's existing application identity and navigation. Do not replace Dapper Dan's app name, bundle identity, store record, signing, or release settings with standalone Flexler settings. Keep feature resources scoped and retain their licenses and provenance.
- Keep Android as the behavior baseline and iOS as the distribution canary.
- Keep Prism properties and MAUI `BindableProperty` wrappers field-first.
- Keep `PanelBossBody_DefaultView` as the first real child of hosted pages; it owns the layout grid.
- Prefer host-owned XAML and direct `RichButton` commands over wrappers or gesture recognizers.
- Preserve the page-owned five-cell action grid: four direct actions plus More. Do not introduce `AppTabBar`.
- Give interactive seams stable semantic `AutomationId` values.
- Keep automatic pull-request workflows secret-free. Never use `pull_request_target` for app builds.
- Pin GitHub Actions to full commit SHAs. Signing belongs only in the manual protected TestFlight environment.
- Update `PROVENANCE.md` when the public/private boundary changes.
