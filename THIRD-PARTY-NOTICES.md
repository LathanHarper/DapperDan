# Third-party notices

## Prism 9

Dapper Dan directly references `Prism.DryIoc.Maui` 9.0.537. Its locked transitive Prism dependency set includes:

- `Prism.Maui`, `Prism.Core`, and `Prism.Events` 9.0.537;
- `Prism.Container.DryIoc` and `Prism.Container.Abstractions` 9.0.106.

The NuGet metadata identifies Brian Lagunas and Dan Siegel as the package authors. The Prism container package metadata carries the notice `Copyright (C) 2015-2024 Prism Software, LLC - all rights reserved`. These Prism packages use a custom file-based Community/Commercial license, require license acceptance, and are **not** licensed under Dapper Dan's MIT license. A clone, fork, contribution, or copy of Dapper Dan does not grant a Prism license. Before restoring, building, testing, or using these packages, each developer must independently be covered by a valid Prism Community or Commercial license and comply with its terms.

The package notice is preserved at [`THIRD-PARTY-LICENSES/Prism-9-package-notice.txt`](THIRD-PARTY-LICENSES/Prism-9-package-notice.txt). Read the [current full Prism Software License Agreement](https://cdn.prismlibrary.com/downloads/prism_license.pdf) and [Prism licensing guidance](https://docs.prismlibrary.com/docs/current/#licensing). Operational guidance for this repository is in [`docs/PRISM-LICENSING.md`](docs/PRISM-LICENSING.md).

## DryIoc

`DryIoc.dll` 5.4.3 is a transitive dependency of `Prism.Container.DryIoc`. It is copyright © 2013–2021 Maksim Volkau and licensed under the MIT License. Its license is preserved at [`THIRD-PARTY-LICENSES/DryIoc-MIT.txt`](THIRD-PARTY-LICENSES/DryIoc-MIT.txt).

## .NET MAUI

The Android and iOS native primary-touch adapters under `src/DapperDan/Platforms` contain focused implementation work derived from .NET MAUI Controls 10.0.20 behavior at commit `0d1705adc4a6b4ec531e316ec956755abbe059c5`. The iOS accessibility activation seam also records upstream commit `09980daa` in source comments.

.NET MAUI is copyright .NET Foundation and Contributors and is licensed under the MIT License. The upstream license is available in the [.NET MAUI repository](https://github.com/dotnet/maui/blob/main/LICENSE).

## Open Sans

The actual bundled `OpenSans-Regular.ttf` and `OpenSans-Semibold.ttf` files identify the Apache License, Version 2.0 in their embedded name tables. Regular records digitized-data copyright 2010–2011 Google Corporation; Semibold records 2011 Google Corporation. Preserve the accompanying `OpenSans-Apache-2.0.txt` notice. These are older font files; a license for a newer upstream Open Sans release does not describe these binaries. The canonical license text is available from the [Apache Software Foundation](https://www.apache.org/licenses/LICENSE-2.0.txt).

## Instrument Sans

The FleXler showcase uses Instrument Sans Regular and SemiBold. Copyright 2022 The Instrument Sans Project Authors. These fonts use the SIL Open Font License 1.1; preserve the accompanying `InstrumentSans-OFL.txt` text. Upstream: [Instrument Sans](https://github.com/Instrument/instrument-sans).

## Geist Mono

The FleXler showcase uses Geist Mono Medium. Copyright 2024 The Geist Project Authors. This font uses the SIL Open Font License 1.1; preserve the accompanying `Geist-OFL.txt` text. Upstream: [Geist](https://github.com/vercel/geist-font).

## FleXler showcase artwork and source

CodeCrafty authorized the full faithful FleXler feature source, semantic resources and approved visual assets for this public showcase on September 15, 2026. The spring/wordmark identity and labradorite textures are original ImageGen artwork created for FleXler and transferred as approved existing assets. This authorization is limited to the showcase described in [PROVENANCE.md](PROVENANCE.md); it does not expose the standalone app's private history, identities, signing material or diagnostics. Keep upstream MAUI MIT notices with the feature's source-derived adapters. Font and dependency licenses remain separate from the authorization to publish the showcase.

These notices cover third-party material. CodeCrafty-authored Dapper Dan source is licensed separately under the repository's [MIT License](LICENSE); that license does not relicense any third-party component.
