# Contributing

Dapper Dan is a public canary, so the clean boundary matters as much as the fix.

## Prism license prerequisite

This repository's MIT license covers CodeCrafty-authored source only. It does not grant a license to Prism 9. Before restoring packages, building or testing the app, or contributing code that uses Prism, you must be covered by and comply with either the Prism Community License or a Prism Commercial License. Read [`docs/PRISM-LICENSING.md`](docs/PRISM-LICENSING.md) first. Contributors remain responsible for their own eligibility and acceptance; submitting a pull request does not place them under CodeCrafty's license.

## Bringing over an iOS quirk

The general steps below apply to private features without a specific public-transfer authorization. CodeCrafty authorized the complete FleXler showcase on September 15, 2026: its developer-tool source, XAML, look and feel, semantic values, supporting components and approved brand images. Preserve that fidelity under `src/DapperDan/Features/Flexler`; it does not need to be replaced with an independently authored abstraction. Keep the host's Dapper Dan application identity and release configuration. This exception does not authorize unrelated private code, private Git history, standalone-app account identifiers, signing assets or personal logs. See [PROVENANCE.md](PROVENANCE.md).

1. Describe the observable behavior without private product names or workflows.
2. Recreate only the platform seam with neutral code and data in Dapper Dan.
3. Record the Android result as the known-good baseline.
4. Add a focused regression test or a stable manual witness.
5. Prove the change with the unsigned iOS workflow and, when authorized, the protected TestFlight lane.
6. Apply the learned pattern privately by reimplementation, not by moving private source into this repository.

Before opening an issue or pull request, remove credentials, tokens, account identifiers, customer data, internal URLs, private package feeds, workstation paths, proprietary screenshots, production logs, and signing files. If safe redaction would make the report ambiguous, keep the evidence private and submit only the independently authored reproduction.

Audio, images, fonts, and other assets must be original CodeCrafty work, approved FleXler artwork within the authorization above, or carry an explicit redistribution license recorded in `THIRD-PARTY-NOTICES.md`. Keep the Instrument Sans, Geist Mono, Open Sans and upstream MAUI notices with the resources they cover. Publishing the showcase does not relicense Prism or other third-party dependencies.

After changing the public Keiki entities or `DapperDanDbContext`, regenerate and review both database artifacts:

```powershell
.\tools\DapperDan.DatabaseTool\Generate.ps1
```

Never hand-edit the compiled model or replace the seed with an application database.

Run:

Only after satisfying the Prism license prerequisite, run:

```powershell
dotnet restore .\DapperDan.slnx
dotnet test .\tests\DapperDan.Tests\DapperDan.Tests.csproj -c Release --no-restore
dotnet build .\src\DapperDan\DapperDan.csproj -f net10.0-android -c Release --no-restore
```
