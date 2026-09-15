# Dapper Dan working contract

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
