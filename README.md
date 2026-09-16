# Dapper Dan

Dapper Dan is CodeCrafty.net's public .NET MAUI distribution canary: one small Android/iOS app that proves the shared code can compile on Apple tooling before production code or signing credentials enter the water.

The app deliberately exercises a useful native cross-section:

- Prism startup and navigation;
- XAML source generation;
- PanelBoss layout, motion, drawers, sheets, status, and popups;
- RichButton native touch and haptic feedback;
- EF Core with an on-device SQLite store;
- a checked-in compiled EF model and versioned packaged SQLite seed that avoid runtime design-time schema operations;
- iOS Release assemblies kept AOT-compiled while Mono's documented interpreter fallback handles runtime-generated EF query delegates;
- a dependency-free, durable iOS launch journal that brackets MAUI, Prism, XAML, the compiled model, SQLite, and the first responsive UI dispatch;
- a physical-iOS native voice A/B/C that isolates language-default selection, arbitrary installed-voice ranking, and Apple-managed speech-session ownership without transmitting diagnostics;
- a product-neutral native MAUI rotation lab with direct `Rotation`, `RotationX`, and `RotationY` sliders plus independently switchable layer-composition ingredients;
- the owner-authorized FleXler layout-recipe workbench, with its full XAML interface, semantic values, mineral materials, approved branding, recipe export and local favorites;
- iPhone and iPad packaging from the same project that supplies the Android baseline.

CodeCrafty explicitly authorized the full FleXler showcase source, look and feel, semantic resources and approved brand images for this public app on September 15, 2026. That scope lives under `src/DapperDan/Features/Flexler` and its required app resources. The standalone Flexler repository remains private. Unrelated private code and history, customer data, private accounts/endpoints, credentials, signing material and personal diagnostic logs remain excluded. Other private-app quirks belong here as small, independently authored reproductions; see [the precise provenance boundary](PROVENANCE.md).

CodeCrafty-authored source is released under the [MIT License](LICENSE): copy it, adapt it, teach from it, and ship with it—just preserve the copyright and license notice. The runnable app also depends on Prism 9, which is **not covered by Dapper Dan's MIT license**. Before restoring, building, testing, or reusing the Prism-backed source, each developer must qualify for and accept the Prism Community License or obtain a Prism Commercial License. Ordinary users of a compiled Dapper Dan app do not need a Prism developer license. See [Prism licensing for this repository](docs/PRISM-LICENSING.md) and the [third-party notices](THIRD-PARTY-NOTICES.md).

## Repository map

| Path | Purpose |
| --- | --- |
| `src/DapperDan` | .NET 10 MAUI app targeting Android and iOS |
| `src/DapperDan/Features/Flexler` | Faithful FleXler showcase source, XAML workbench and semantic resources |
| `tests/DapperDan.Tests` | persistence, layout, and regression contracts |
| `.github/workflows/ios-unsigned.yml` | secret-free Apple compilation and unsigned proof binaries |
| `.github/workflows/testflight.yml` | manual, protected signing and TestFlight upload |
| `docs/IOS-CANARY.md` | operating model and Apple/GitHub setup |

See the [FleXler showcase guide](docs/FLEXLER-SHOWCASE.md) for feature ownership, isolated resources and favorites, local unit/Appium commands, and the remaining native verification gates.

The app display name is `Dapper Dan`; its bundle/application identifier is `net.codecrafty.dapperdan`. That identifier is only the app's technical identity. Apple account ownership is established by the CodeCrafty.net developer team, certificates, provisioning profile, and App Store Connect record.

## Local proof

First confirm that you are covered by a valid Prism license as described in [docs/PRISM-LICENSING.md](docs/PRISM-LICENSING.md). Cloning this repository does not grant one.

```powershell
dotnet restore .\DapperDan.slnx
dotnet test .\tests\DapperDan.Tests\DapperDan.Tests.csproj -c Release --no-restore
dotnet build .\src\DapperDan\DapperDan.csproj -f net10.0-android -c Release --no-restore
```

When the neutral entities or `DapperDanDbContext` change, regenerate the compiled model and deterministic seed before testing:

```powershell
.\tools\DapperDan.DatabaseTool\Generate.ps1
```

EF schema generation is intentionally confined to that ordinary build-time tool. It emits a canonical-LF create script before producing the reviewed seed. The mobile app copies `dapper-dan-seed-v1.db3` into a versioned writable app-data path on first launch, validates its SQLite identity/schema/integrity, opens it without create fallback, and supplies EF with the checked-in compiled model. Later launches validate but never overwrite the user's writable copy.

This canary deliberately tests one AOT variable at a time. iOS Release uses [`MtouchInterpreter=-all`](https://learn.microsoft.com/dotnet/maui/macios/interpreter?view=net-maui-10.0#enable-the-interpreter), which keeps normal assemblies AOT-compiled while retaining Mono's supported interpreter path for runtime-generated code. The current slice keeps the compiled model proven by build 12, does not enable EF's experimental precompiled-query interceptors or NativeAOT, and exercises ordinary repository queries in the signed iPad build. Every iOS session's first `launch` record includes `isDynamicCodeSupported` and `isDynamicCodeCompiled`; the expected device values for this lane are `true` and `false`.

On iOS, each launch writes a private, synchronously flushed JSONL journal before `UIApplication.Main`. The next launch seals the prior session and exports it to **Files → On My iPad → Dapper Dan → DapperDan Diagnostics** before MAUI starts. The journal has no network transport and does not intentionally collect database rows, device identifiers, accounts, credentials, or arbitrary application metadata; bounded exception text is redacted before it is written. See [the iOS canary guide](docs/IOS-CANARY.md#recovering-an-ios-launch-journal) for retrieval and interpretation.

An iOS build requires macOS, the pinned .NET workload set, and compatible Xcode:

```bash
dotnet workload restore src/DapperDan/DapperDan.csproj
dotnet build src/DapperDan/DapperDan.csproj \
  -f net10.0-ios -c Release -r iossimulator-arm64 \
  -p:EnableCodeSigning=false -p:ArchiveOnBuild=false
```

## Two Apple lanes

The public workflow is automatic and has no secrets. A cheap Linux gate regenerates the compiled model and SQLite seed, rejects drift, and runs the data tests before macOS minutes begin. The Apple job creates unsigned simulator and device `.app` bundles with the build-only display name `Dapper Dan - UNSIGNED PROOF`, verifies the packaged seed inside both bundles, and reports hashes, sizes and toolchain details. Pushes, pull requests and default manual runs retain no artifacts.

To retain the complete unsigned builds, binary logs, license notices and `RETURN-TO-SENDER.txt` warning, manually select `proof_scope: full` and explicitly enable `retain_unsigned_artifacts`. That opt-in retains files for 14 days; it may incur storage charges even though standard public-runner compute is free. Before enabling it, the owner must verify available free storage or authorize the storage expense. The focused Flexler proof never uploads artifacts. These are app products, not Prism packages, loose framework binaries, SDKs, wrappers, or development tooling.

The TestFlight workflow is manual, rebuilds trusted `main` from source, waits behind a protected GitHub environment, signs with CodeCrafty.net's Apple material, and uploads directly to App Store Connect. It never publishes the signed IPA as a GitHub artifact.

See [docs/IOS-CANARY.md](docs/IOS-CANARY.md) before enabling signing. See [CONTRIBUTING.md](CONTRIBUTING.md) before moving a private-app quirk into this public canary.

## License

Copyright © 2026 CodeCrafty.net. CodeCrafty-authored Dapper Dan source is licensed under the [MIT License](LICENSE). Prism and all other third-party components remain under their respective licenses; the MIT license does not relicense them.
