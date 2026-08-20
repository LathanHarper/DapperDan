# Prism licensing for Dapper Dan

## The boundary

Dapper Dan distributes CodeCrafty-authored source under MIT. It does not vendor or redistribute Prism source or NuGet packages. A package restore downloads Prism directly from NuGet.org, under Prism Software LLC's terms.

The MIT license in this repository does **not** grant, sublicense, or replace a Prism license. Copying or forking Dapper Dan therefore gives you the CodeCrafty code, but no right to restore, build, test, or use Prism unless you are separately licensed by Prism.

## Packages in the locked graph

- `Prism.DryIoc.Maui`, `Prism.Maui`, `Prism.Core`, and `Prism.Events` 9.0.537
- `Prism.Container.DryIoc` and `Prism.Container.Abstractions` 9.0.106

All of these Prism packages use a custom file-based license rather than an SPDX license expression, and their NuGet metadata declares that license acceptance is required. They must never be labeled MIT. DryIoc itself is a separate MIT-licensed transitive dependency; see `THIRD-PARTY-NOTICES.md`.

## CodeCrafty's lane

The maintainer has confirmed that CodeCrafty is covered by the Prism Community License and remains well within its current income eligibility threshold. The app and public sample are free, but project price alone does not determine Prism eligibility. The maintainer must continue to satisfy the complete current agreement, keep every developer End-User properly covered, and re-check eligibility whenever revenue, funding, ownership, team size, contracting, or intended use changes.

The public Prism summary currently describes Community eligibility using annual gross revenue or outside-capital thresholds. The full agreement uses a stricter revenue test, limits an entity to five total Community developer End-Users, and contains other conditions. Its Community section says the license continues while eligibility remains, while a later general section describes all rights as subscription-based. Follow the strict intersection, re-check the current terms regularly, and obtain written Prism guidance before relying on either ambiguity. The full agreement controls; do not rely on this summary as a substitute.

## Application distribution boundary

Prism's Community offering allows unlimited deployment. The full agreement permits licensed customers to redistribute release builds of Prism libraries as incorporated into customer application products, and its Schedule A marks every Prism assembly in Dapper Dan's locked graph as redistributable. The restrictions prevent Prism from being exposed or repackaged for development reuse and prohibit competitive frameworks, controls, wrappers, and similar reusable products.

Dapper Dan stays on the application side of that boundary:

- it is a complete end-user MAUI app with substantial CodeCrafty-authored UI, interaction, persistence, and platform behavior;
- CI publishes compiled `.app` products, not Prism NuGet packages, source, loose development assemblies, SDKs, wrappers, control libraries, or low-code tooling;
- the app does not expose Prism as a reusable API or provide a way to develop other products with the bundled framework;
- each artifact carries the CodeCrafty license, Prism package notice, third-party notices, and DryIoc license.

Do not turn Dapper Dan into a Prism distribution vehicle. Anyone reusing its source must obtain Prism from the official NuGet packages under their own valid Prism license, and nobody may extract or republish Prism from a Dapper Dan build for development use.

Ordinary users receiving or running a compiled Dapper Dan application are not Prism developers and do not need their own developer license. A person who restores, writes, builds, debugs, tool-tests, or reuses the Prism-backed source does.

## Before you restore or contribute

1. Read Prism's [current licensing guidance](https://docs.prismlibrary.com/docs/current/#licensing) and [full Software License Agreement](https://cdn.prismlibrary.com/downloads/prism_license.pdf).
2. Confirm that you and any entity on whose behalf you work qualify for the Community License, or obtain the appropriate Commercial License from [Prism Library](https://prismlibrary.com/).
3. Accept Prism's terms through Prism's current licensing process.
4. Ensure every developer who writes, builds, tool-tests, or otherwise works with the Prism-backed application is properly covered. A fork, pull request, GitHub Actions run, or CodeCrafty MIT license does not cover you automatically.
5. Preserve `THIRD-PARTY-NOTICES.md` and the Prism notice with any permitted application binary distribution.

If the license classification is uncertain, stop restoring/building Prism and ask Prism Software LLC at `support@prismlibrary.com`. This file records the repository boundary; it is not legal advice and does not alter Prism's agreement.
