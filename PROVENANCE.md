# Public provenance boundary

Dapper Dan was cut from a stable, product-neutral .NET 10 MAUI suitcase containing developer-authored infrastructure that was documented as authorized for transfer. This standalone export adds a second boundary: only material intended for a public canary belongs here.

## Included

- PanelBoss layout and panel-lane infrastructure.
- The core RichButton native-input implementation.
- Android and iOS platform adapters needed by those components.
- Dapper Dan page-owned sample XAML and ViewModel logic.
- The Keiki EF Core/SQLite example and focused tests.
- The generated Keiki compiled model and deterministic SQLite v1 seed containing only fixed public canary rows. Their complete generation recipe lives in `tools/DapperDan.DatabaseTool`.
- The generic crash-journal implementation, neutral startup checkpoint names, and tests. It has no upload endpoint or product-specific metadata.
- An independently authored native iOS voice-selection and audio-session canary with neutral speech, on-screen-only metadata, and no product vocabulary or transport.
- An independently authored, product-neutral native MAUI rotation lab and its two geometric edge-marker SVGs. The lab contains no private assets, product layout, Skia renderer, or platform workaround.
- Generic .NET MAUI template resources, CodeCrafty-recorded mechanical feedback sounds, and public CI scaffolding.

## Excluded from this export

- Product business logic, routes, services, schemas, accounts, customer data, configuration, credentials, endpoints, branding, screenshots, and operational logs.
- Absolute workstation paths, user or machine names, private source hashes, transfer tooling, internal planning material, runtime diagnostic journals, and test artifacts.
- IDE state, `bin`, `obj`, packages, app bundles, symbols, signing files, provisioning profiles, and cloud-device test output.
- Preview-framework variants, abandoned experiments, construction/timing comparison probes, and inactive platform targets.
- Three previously transferred feedback WAV files. They were excluded and replaced with fresh clips from a CodeCrafty recording; see `docs/ASSET-PROVENANCE.md`.

Public bug reproductions must be newly minimized around platform behavior. Never paste a private implementation and call it a sample. The packaged database is not a product export: it is generated solely from the two public Keiki entities with fixed Kai sample values. Detailed private transfer records remain private; this summary is the public boundary.

## Public license

CodeCrafty.net has released its public Dapper Dan source under the MIT License so its reusable patterns can be copied, modified, taught, and redistributed. Third-party material remains governed by the licenses recorded in `THIRD-PARTY-NOTICES.md`.

In particular, Prism 9 is separately licensed by Prism Software LLC. Dapper Dan does not vendor Prism source or NuGet packages, and the repository's MIT license grants no rights to Prism. Anyone who restores, builds, tests, or reuses the Prism-backed app must independently qualify for and accept the Prism Community License or obtain a Prism Commercial License. See `docs/PRISM-LICENSING.md`.
