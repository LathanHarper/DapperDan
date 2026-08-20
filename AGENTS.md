# Dapper Dan working contract

- Treat the entire repository, issue history, CI log, and artifact set as public.
- Never copy private product code, routes, schemas, identities, endpoints, configuration, screenshots, logs, signing material, or credentials into this project.
- Reproduce platform quirks with the smallest independently authored sample, a regression test when practical, and neutral names/data.
- Keep Android as the behavior baseline and iOS as the distribution canary.
- Keep Prism properties and MAUI `BindableProperty` wrappers field-first.
- Keep `PanelBossBody_DefaultView` as the first real child of hosted pages; it owns the layout grid.
- Prefer host-owned XAML and direct `RichButton` commands over wrappers or gesture recognizers.
- Preserve the page-owned five-cell action grid: four direct actions plus More. Do not introduce `AppTabBar`.
- Give interactive seams stable semantic `AutomationId` values.
- Keep automatic pull-request workflows secret-free. Never use `pull_request_target` for app builds.
- Pin GitHub Actions to full commit SHAs. Signing belongs only in the manual protected TestFlight environment.
- Update `PROVENANCE.md` when the public/private boundary changes.
