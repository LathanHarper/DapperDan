# Content-sized PanelBoss chrome canary

This branch-only, independently authored sample tests a reusable layout contract.
It contains neutral sample text, not private application content. Normal Dapper
startup and the previous bottom-panel comparison remain unchanged.

## Ownership contract

- `PanelBossBody_DefaultView` is the first real child and fills the supplied page.
- The header and bottom action panel occupy independent PanelBoss overlay lanes.
  Their `Auto` rows size from their own content; neither reads the viewport size.
- The main content panel uses `Auto,*,Auto,Auto`: header clearance, remaining
  viewport, action clearance, and coordinator-owned native bottom clearance.
- Clearance placeholders follow the actual header/action heights with one-way
  XAML bindings. They cannot constrain either source through a shared sizing row.
- Five equal-width RichButtons use natural label height and a shared 56-point
  minimum, not a fixed total strip height. Larger text and a visible tongue grow
  the strip; the scroll viewport yields the corresponding space.
- More owns its own natural header and follows the same bottom action panel.
  Closing More restores the main panel/header through the existing PanelBoss API.
- The root owns top/side safe areas. On Android, the existing PanelBoss coordinator
  owns the bottom navigation clearance; on iOS, the root owns the bottom safe area.
  Nested layouts declare `SafeAreaEdges=None` so clearance is not counted twice.
- Code-behind observes geometry for evidence only. It does not assign sizes,
  handle rotation, invalidate measurement, or copy measured heights into layout.

This is not a rule against coordinator-owned calculations: PanelBoss may publish
an authoritative dynamic dimension when orchestration actually owns it. The
contract is one owner and one-way followers, without competing size writers.

## Local Android lane

Use the existing Visual Studio SDK, Java, and one explicitly selected emulator.
Do not install another SDK, change an AVD's resolution/density, clear application
data, or start a second emulator alongside the selected device.

The authorized root SDK is `10.0.303`; workload set remains `10.0.302.1`.
Both hosted iOS workflows select `.github/ios-global.json` first, retaining SDK
`10.0.302`, workload `10.0.302.1`, and the existing Xcode selection.

SDK 303 uses an ignored `obj/packages.sdk-10.0.303.lock.json`. The canonical
`packages.lock.json` remains unchanged for iOS. The local graph contains the
SDK's ILLink patch and existing installed-library-pack fingerprints for MAUI
Controls.Build.Tasks/Resizetizer 10.0.20. Those versions were not upgraded; the
archives differ from the canonical NuGet fingerprints. Android proof does not
claim identical toolchain bytes or substitute for native iOS proof.

From this worktree, with the selected emulator already running:

```powershell
dotnet --version
dotnet workload --version
dotnet restore src/DapperDan/DapperDan.csproj -p:TargetFrameworks=net10.0-android -p:DapperDanSquishyCanary=true
dotnet build src/DapperDan/DapperDan.csproj -f net10.0-android -c Debug --no-restore -p:DapperDanSquishyCanary=true -t:Install -p:Device=emulator-5554
```

Use the MAUI Install target for fast deployment, not a bare APK install that omits
Debug companion assemblies. Resolve the installed activity through the selected
VS `adb` and launch it. The package is `net.codecrafty.dapperdan`.

The Debug-only `DapperDanSquishyCanary=true` property selects this page. It is
separate from `DapperDanBottomPanelCanary=true` and does not alter Release startup.

## Native evidence

Tap Normal text, Large text, Toggle tongue, and More by their semantic
`AutomationId` values. Derive input coordinates from a fresh UI tree. Rotate using
the emulator's normal controls or virtual sensor, without overriding dimensions.

```powershell
./tools/Capture-SquishyAndroid.ps1 -Serial emulator-5554 -Name phone-portrait-normal
node tools/simulator-proof/squishy-evidence.mjs artifacts/squishy-local/phone-portrait-normal.json
node --test tools/simulator-proof/*.test.mjs
```

Each capture saves a native PNG, full UI XML, compact semantic UI summary, and
the app's read-only geometry JSON. The helper rejects stale measurements and
cross-checks panel visibility and viewport/action bounds against the current
native tree. Wait for layout to settle before capturing; a mismatch is not proof.

The geometry validator checks native top/side/bottom ownership, contiguous
viewport edges, five equal-height buttons, and contained glyph/text frames.
`compareSquishyEvidence` additionally verifies natural strip growth and exact
space returned when hiding the tongue. Screenshots remain necessary: geometrical
containment alone does not prove the appearance or every rendered character.

### Android phone receipt — 2026-09-11

Existing Pixel 9 Pro API 36, `emulator-5554`, x86_64, native 1280 x 2856 at density
480. No AVD size, density, SDK, package-cache, or system font-scale change.

- SDK 303 restore: 1.31 seconds; Debug build: 59.71 seconds; fast-deploy Install:
  33.12 seconds. Final install reported 0 errors and 146 existing warnings; no
  warnings remained in the new canary files.
- Cold launch succeeded. Normal/large text, tongue show/hide, both orientations,
  More header ownership, restore, repeated More toggles, and native scrolling were
  exercised. All observed header/viewport/action boundaries were contiguous.
- Landscape font growth changed the strip by +41 points and viewport by -41.
  Hiding the large-text tongue returned exactly 49 points to the viewport.
- A portrait/landscape round trip returned the normal tongue-free strip to 68
  points, with a 189-point landscape viewport: no stale portrait reservation.
- Narrow large-text action labels wrapped and their row grew uniformly. This is
  controlled font stress, not a claim of exhaustive accessibility/font-scale QA.
- Repeated More toggles produced fresh sequences 12, 13, 14 and states true,
  false, true. Scrolling moved sample rows without moving the chrome.
- .NET regression suite: 41 passed. Node suite: 44 passed, including native inset
  regression checks. The crash buffer was empty when checked.

Local PNG/XML/JSON/build logs are under `artifacts/squishy-local/`. These are ignored
evidence, not store screenshots or release binaries. The capture receipt records
the exact local payload and source identities.

## Remaining platform gate

Android tablet proof is pending: use the owner's existing clean tablet, one
emulator at a time. Repeat both orientations, font growth, tongue removal, and
More restore. The owner subsequently authorized proceeding directly to the four
iOS Simulator views; this does not count as Android tablet evidence.

The manual branch workflow accepts `scenario=squishy`. That selects this Debug
startup and a distinct product receipt; reuse rejects an old bottom-panel binary.
The default `bottom-panel` scenario keeps the earlier A/B comparison intact.

The squishy lane builds a tiny standalone XCTest UI driver (not another copy of
the MAUI application), then builds Dapper once and preserves its original and
ad-hoc Simulator products before capture. Each family boots/installs once; native
`XCUIDevice.orientation` selects portrait and landscape. Four
`test-without-building` executions retain screenshots and accessibility trees.
The capture script verifies native PNG dimensions, fresh orientation-specific
geometry, and the same Dapper binary hash. Simulators run sequentially.

Keep the public-repository/standard-runner gates, one-day remote artifact
retention, and frozen iOS inputs. Download products and evidence locally while
available. Do not dispatch a paid/private Release or TestFlight run.

Do not port the layout to a production app or describe the pattern as
cross-platform-proven until the tablet and native iOS gates are complete.
