# Dapper Dan implementation instructions

Dapper Dan is a public .NET 10 MAUI + Prism iOS canary owned by CodeCrafty.net.

- Assume all source, prompts, logs, and artifacts are public. Never import private product code or data.
- Use XAML-first UI and field-first Prism properties.
- Keep PanelBoss as the page root and express its lanes with host-owned XAML.
- Use the core `RichButton` as the command invoker. Long-running work clears two-way busy state when it actually finishes; timed reset is for debounce-only actions.
- Keep four direct page actions plus More in the fixed bottom grid. More is a page-owned PanelBoss action, not navigation infrastructure.
- Keep the SQLite sample limited to the neutral Keiki example.
- Use stable `AutomationId` values and real production paths; do not add test-only action paths.
- A public iOS fix must be a minimized, independently authored reproduction with no private identifiers, endpoints, schemas, assets, or logs.
- Pull-request CI has no secrets. TestFlight remains manual and protected.
