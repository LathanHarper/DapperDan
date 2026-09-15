# Public Flexler Simulator startup proof

Manually select `proof_scope: flexler` in **iOS unsigned proof**. That selection
skips the normal data/device jobs and runs one standard `macos-26` job, bounded
to 35 minutes. The default `full` selection and push/PR behavior are unchanged.

The job builds the complete public app in Release for `iossimulator-arm64`, with
`FlexlerShowcaseProof=true` selecting the actual Flexler page at startup. It uses
the repository's pinned SDK/workload and locked package graph. It preserves
Dapper Dan's `MtouchInterpreter=-all`; this does **not** reproduce the standalone
Flexler device build's interpreter configuration or prove device AOT behavior.

Before installation, the ephemeral Simulator output receives local ad-hoc
seals (`codesign --sign -`), nested native binaries first and the app last.
This is a local loader requirement already demonstrated by the public capture
canary. No Apple identity, certificate, key, provisioning profile, TestFlight
upload or distributable release is involved.

The helper selects one stock available iPad from an installed runtime. It never
creates a replacement device or downloads a runtime. It requires the exact
`FlexlerPageLoaded` journal checkpoint, no recorded exception, and eight more
seconds with the launched process alive, within a 60-second observation window.
Missing journals, unrelated page checkpoints and early process death fail.

Original journal files stay only in the ephemeral runner's temporary directory.
Workflow logs contain allowlisted checkpoint/type/boolean/numeric fields, not
exception messages, stacks, paths, identities or arbitrary journal text. The
summary records build outcome and native startup outcome. There is no artifact
upload, cache or external test service, so this lane creates no retained-artifact
storage charge. Standard public-repository runner compute is free under
[GitHub's billing policy](https://docs.github.com/en/billing/concepts/product-billing/github-actions).

This is startup evidence, not interaction coverage, visual inspection, device
installation proof, or production screenshot capture. Check the standalone app
separately before another release attempt.

Local parser/selection checks: `node --test tools/flexler-proof/*.test.mjs`.
