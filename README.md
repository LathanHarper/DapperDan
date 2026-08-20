# Dapper Dan

Dapper Dan is CodeCrafty.net's public .NET MAUI distribution canary: one small Android/iOS app that proves the shared code can compile on Apple tooling before production code or signing credentials enter the water.

The app deliberately exercises a useful native cross-section:

- Prism startup and navigation;
- XAML source generation;
- PanelBoss layout, motion, drawers, sheets, status, and popups;
- RichButton native touch and haptic feedback;
- EF Core with an on-device SQLite store;
- iPhone and iPad packaging from the same project that supplies the Android baseline.

It does **not** contain customer code, private routes, accounts, endpoints, credentials, company assets, production configuration, or copied bug implementations. Public iOS quirks belong here only as small, independently authored reproductions.

CodeCrafty-authored source is released under the [MIT License](LICENSE): copy it, adapt it, teach from it, and ship with it—just preserve the copyright and license notice. The runnable app also depends on Prism 9, which is **not covered by Dapper Dan's MIT license**. Before restoring, building, testing, or reusing the Prism-backed source, each developer must qualify for and accept the Prism Community License or obtain a Prism Commercial License. Ordinary users of a compiled Dapper Dan app do not need a Prism developer license. See [Prism licensing for this repository](docs/PRISM-LICENSING.md) and the [third-party notices](THIRD-PARTY-NOTICES.md).

## Repository map

| Path | Purpose |
| --- | --- |
| `src/DapperDan` | .NET 10 MAUI app targeting Android and iOS |
| `tests/DapperDan.Tests` | persistence, layout, and regression contracts |
| `.github/workflows/ios-unsigned.yml` | secret-free Apple compilation and unsigned proof binaries |
| `.github/workflows/testflight.yml` | manual, protected signing and TestFlight upload |
| `docs/IOS-CANARY.md` | operating model and Apple/GitHub setup |

The app display name is `Dapper Dan`; its bundle/application identifier is `net.codecrafty.dapperdan`. That identifier is only the app's technical identity. Apple account ownership is established by the CodeCrafty.net developer team, certificates, provisioning profile, and App Store Connect record.

## Local proof

First confirm that you are covered by a valid Prism license as described in [docs/PRISM-LICENSING.md](docs/PRISM-LICENSING.md). Cloning this repository does not grant one.

```powershell
dotnet restore .\DapperDan.slnx
dotnet test .\tests\DapperDan.Tests\DapperDan.Tests.csproj -c Release --no-restore
dotnet build .\src\DapperDan\DapperDan.csproj -f net10.0-android -c Release --no-restore
```

An iOS build requires macOS, the pinned .NET workload set, and compatible Xcode:

```bash
dotnet workload restore src/DapperDan/DapperDan.csproj
dotnet build src/DapperDan/DapperDan.csproj \
  -f net10.0-ios -c Release -r iossimulator-arm64 \
  -p:EnableCodeSigning=false -p:ArchiveOnBuild=false
```

## Two Apple lanes

The public workflow is automatic and has no secrets. It creates unsigned simulator and device `.app` bundles with the build-only display name `Dapper Dan - UNSIGNED PROOF`, verifies them, records their hashes and sizes, and uploads the complete application builds with binary logs, license notices, and a `RETURN-TO-SENDER.txt` warning for 14 days. These are app products, not Prism packages, loose framework binaries, SDKs, wrappers, or development tooling.

The TestFlight workflow is manual, rebuilds trusted `main` from source, waits behind a protected GitHub environment, signs with CodeCrafty.net's Apple material, and uploads directly to App Store Connect. It never publishes the signed IPA as a GitHub artifact.

See [docs/IOS-CANARY.md](docs/IOS-CANARY.md) before enabling signing. See [CONTRIBUTING.md](CONTRIBUTING.md) before moving a private-app quirk into this public canary.

## License

Copyright © 2026 CodeCrafty.net. CodeCrafty-authored Dapper Dan source is licensed under the [MIT License](LICENSE). Prism and all other third-party components remain under their respective licenses; the MIT license does not relicense them.
