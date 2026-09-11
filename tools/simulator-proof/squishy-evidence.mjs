import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

export const actionIds = Object.freeze(['Alpha', 'Bravo', 'Charlie', 'Delta', 'More'].map(name => `Squishy_${name}`));
const right = frame => frame.x + frame.width;
const bottom = frame => frame.y + frame.height;

/** Validate observed host-coordinate geometry; this never controls application layout.
 * Android and iOS Container top/side policies use recorded native insets. Android's
 * declared policy leaves the host bottom to the coordinator; iOS uses its native bottom
 * inset too. Explicit host*Inset options describe independently observed alternate hosts.
 * Missing top/side evidence fails host anchoring; an unknown bottom remains unverified.
 */
export function validateSquishyEvidence(evidence, {
  tolerance = 1, hostTopInset, hostLeftInset, hostRightInset, hostBottomInset,
} = {}) {
  const errors = [];
  const check = (condition, message) => { if (!condition) errors.push(message); };
  if (!Number.isFinite(tolerance) || tolerance < 0) throw new TypeError('tolerance must be a finite nonnegative number.');
  for (const [name, value] of Object.entries({ hostTopInset, hostLeftInset, hostRightInset, hostBottomInset })) {
    if (value !== undefined && (!Number.isFinite(value) || value < 0)) {
      throw new TypeError(`${name} must be a finite nonnegative number.`);
    }
  }
  if (!evidence || typeof evidence !== 'object') return { ok: false, errors: ['Evidence must be an object.'], metrics: null };
  check(evidence.schema === 1, 'Expected evidence schema 1.');
  check(evidence.scenario === 'squishy', 'Expected the squishy scenario.');
  check(Number.isInteger(evidence.sequence) && evidence.sequence > 0, 'Sequence must be a positive integer.');
  check(typeof evidence.capturedUtc === 'string' && Number.isFinite(Date.parse(evidence.capturedUtc)), 'capturedUtc must be an ISO timestamp.');
  check(typeof evidence.platform === 'string' && evidence.platform.length > 0, 'Platform is required.');
  check(['normal', 'large'].includes(evidence.textMode), 'Text mode must be normal or large.');
  check(Number.isFinite(evidence.actionFontSize) && evidence.actionFontSize > 0, 'Action font size must be positive.');
  check(typeof evidence.tongueVisible === 'boolean', 'tongueVisible must be boolean.');
  check(typeof evidence.moreVisible === 'boolean', 'moreVisible must be boolean.');
  check(Number.isFinite(evidence.platformBottomClearance) && evidence.platformBottomClearance >= 0, 'Platform bottom clearance must be finite and nonnegative.');
  const nativeInsetsValid = evidence.nativeInsets && ['left', 'top', 'right', 'bottom']
    .every(key => Number.isFinite(evidence.nativeInsets[key]) && evidence.nativeInsets[key] >= 0);
  if (evidence.nativeInsets !== undefined && evidence.nativeInsets !== null) {
    check(nativeInsetsValid, 'Native insets must be finite and nonnegative on all four sides.');
  }
  const nativeHostInsets = ['Android', 'iOS'].includes(evidence.platform) && nativeInsetsValid
    ? evidence.nativeInsets : undefined;
  const effectiveTopInset = hostTopInset ?? nativeHostInsets?.top;
  const effectiveLeftInset = hostLeftInset ?? nativeHostInsets?.left;
  const effectiveRightInset = hostRightInset ?? nativeHostInsets?.right;
  check(effectiveTopInset !== undefined, 'Native top inset evidence or an explicit hostTopInset is required to verify host anchoring.');
  check(effectiveLeftInset !== undefined && effectiveRightInset !== undefined,
    'Native side inset evidence or explicit hostLeftInset and hostRightInset values are required to verify content width.');
  const effectiveBottomInset = hostBottomInset ?? (evidence.platform === 'Android' ? 0
    : evidence.platform === 'iOS' && nativeInsetsValid ? evidence.nativeInsets.bottom : undefined);

  const validFrame = (frame, name) => {
    const valid = frame && ['x', 'y', 'width', 'height'].every(key => Number.isFinite(frame[key]))
      && frame.width > 0 && frame.height > 0;
    check(valid, `${name} must have finite coordinates and positive dimensions.`);
    return valid;
  };
  const near = (a, b, name) => check(Math.abs(a - b) <= tolerance, `${name}: expected ${b}, observed ${a}.`);
  const inside = (inner, outer, name) => check(inner.x >= outer.x - tolerance && inner.y >= outer.y - tolerance
    && right(inner) <= right(outer) + tolerance && bottom(inner) <= bottom(outer) + tolerance, `${name} must fit inside its owner.`);
  const framesReady = ['host', 'header', 'viewport', 'scroll', 'actions'].map(name => validFrame(evidence[name], name)).every(Boolean);
  let metrics = null;
  if (framesReady) {
    const { host, header, viewport, scroll, actions } = evidence;
    near(host.x, 0, 'Host X');
    near(host.y, 0, 'Host Y');
    for (const [name, frame] of Object.entries({ header, viewport, actions })) inside(frame, host, name);
    inside(scroll, viewport, 'Scroll');
    if (effectiveTopInset !== undefined) near(header.y, host.y + effectiveTopInset, 'Header top must follow the host top safe-area inset');
    for (const [name, frame] of Object.entries({ header, viewport, actions })) {
      if (effectiveLeftInset !== undefined) near(frame.x, host.x + effectiveLeftInset, `${name} left must follow the host left safe-area inset`);
      if (effectiveRightInset !== undefined) near(right(frame), right(host) - effectiveRightInset, `${name} right must follow the host right safe-area inset`);
    }
    near(viewport.y, bottom(header), 'Viewport top must follow header bottom without a gap or overlap');
    near(bottom(viewport), actions.y, 'Viewport bottom must follow action top without a gap or overlap');
    near(viewport.x, actions.x, 'Viewport left edge');
    near(right(viewport), right(actions), 'Viewport right edge');
    const bottomGap = bottom(host) - bottom(actions);
    check(bottomGap + tolerance >= evidence.platformBottomClearance, 'Actions must not consume the coordinator bottom clearance.');
    if (effectiveBottomInset !== undefined) near(bottomGap, evidence.platformBottomClearance + effectiveBottomInset, 'Host bottom gap must equal its stated coordinator and safe-area clearances');
    metrics = {
      viewportHeight: viewport.height,
      actionHeight: actions.height,
      headerHeight: header.height,
      headerGap: viewport.y - bottom(header),
      actionGap: actions.y - bottom(viewport),
      hostTopGap: header.y - host.y,
      topInsetVerified: effectiveTopInset !== undefined,
      sideInsetsVerified: effectiveLeftInset !== undefined && effectiveRightInset !== undefined,
      hostBottomGap: bottomGap,
      unattributedBottomGap: bottomGap - evidence.platformBottomClearance - (effectiveBottomInset ?? 0),
      bottomInsetVerified: effectiveBottomInset !== undefined,
    };
  }

  if (evidence.tongueVisible) {
    if (validFrame(evidence.tongue, 'Tongue') && framesReady) inside(evidence.tongue, evidence.actions, 'Tongue');
  } else check(evidence.tongue === null, 'Hidden tongue must be recorded as null.');

  check(Array.isArray(evidence.buttons) && evidence.buttons.length === 5, 'Exactly five action buttons are required.');
  const buttons = Array.isArray(evidence.buttons) ? evidence.buttons : [];
  const validButtons = [];
  buttons.forEach((entry, index) => {
    if (!entry || typeof entry !== 'object') { check(false, `Button ${index} must be an object.`); return; }
    check(entry.id === actionIds[index], `Button ${index} must have the stable ID ${actionIds[index] ?? '(no sixth button)'}.`);
    const ready = ['button', 'border', 'glyph', 'text'].map(name => validFrame(entry[name], `${entry.id}.${name}`)).every(Boolean);
    if (!ready) return;
    validButtons.push(entry);
    check(entry.button.height + tolerance >= 56, `${entry.id} must preserve its 56-point minimum action height.`);
    inside(entry.border, entry.button, `${entry.id} border`);
    inside(entry.glyph, entry.border, `${entry.id} glyph`);
    inside(entry.text, entry.border, `${entry.id} text`);
    check(bottom(entry.glyph) <= entry.text.y + tolerance, `${entry.id} glyph must not overlap its text.`);
    if (framesReady) {
      inside(entry.button, evidence.actions, `${entry.id} button`);
      near(bottom(entry.button), bottom(evidence.actions), `${entry.id} bottom edge`);
      if (evidence.tongueVisible && evidence.tongue) {
        near(entry.button.y, bottom(evidence.tongue), `${entry.id} must follow the visible tongue`);
      } else near(entry.button.y, evidence.actions.y, `${entry.id} must fill the tongue-free strip`);
    }
  });
  if (validButtons.length === 5) {
    const first = validButtons[0].button;
    validButtons.forEach((entry, index) => {
      near(entry.button.height, first.height, `${entry.id} equal row height`);
      near(entry.button.y, first.y, `${entry.id} equal row top`);
      if (index > 0) near(entry.button.x, right(validButtons[index - 1].button), `${entry.id} contiguous column`);
    });
    if (framesReady) {
      near(first.x, evidence.actions.x, 'First action left edge');
      near(right(validButtons[4].button), right(evidence.actions), 'Last action right edge');
    }
    if (metrics) metrics.buttonHeights = validButtons.map(entry => entry.button.height);
  }
  return { ok: errors.length === 0, errors, metrics };
}

/** Compare captures on the same device, orientation, and panel; no exact font metrics. */
export function compareSquishyEvidence(before, after, { change, tolerance = 1 } = {}) {
  if (!['font-growth', 'tongue-hide'].includes(change)) throw new TypeError('change must be font-growth or tongue-hide.');
  const beforeResult = validateSquishyEvidence(before, { tolerance });
  const afterResult = validateSquishyEvidence(after, { tolerance });
  const errors = [...beforeResult.errors.map(error => `Before: ${error}`), ...afterResult.errors.map(error => `After: ${error}`)];
  if (errors.length > 0) return { ok: false, errors, metrics: null };
  const check = (condition, message) => { if (!condition) errors.push(message); };
  const near = (a, b, message) => check(Math.abs(a - b) <= tolerance, message);
  for (const key of ['x', 'y', 'width', 'height']) near(before.host[key], after.host[key], `Host ${key} must remain constant for comparison.`);
  check(before.platform === after.platform, 'Comparison must use the same platform.');
  check(before.moreVisible === after.moreVisible, 'Comparison must use the same content panel.');
  near(before.platformBottomClearance, after.platformBottomClearance, 'Coordinator clearance must remain constant.');
  near(before.header.height, after.header.height, 'Header height must remain constant.');
  near(before.header.y, after.header.y, 'Header position must remain constant.');
  near(bottom(before.actions), bottom(after.actions), 'Bottom action edge must remain anchored.');
  const actionDelta = after.actions.height - before.actions.height;
  const viewportDelta = after.viewport.height - before.viewport.height;
  near(actionDelta + viewportDelta, 0, 'Action-height changes must be balanced by the remaining viewport.');
  if (change === 'font-growth') {
    check(before.textMode === 'normal' && after.textMode === 'large', 'Font growth requires normal then large captures.');
    check(after.actionFontSize > before.actionFontSize, 'Font size must increase.');
    check(before.tongueVisible === after.tongueVisible, 'Tongue visibility must not change in a font comparison.');
    check(actionDelta > tolerance, 'Larger content must grow the natural action strip.');
    check(viewportDelta < -tolerance, 'Viewport must yield space to the larger action strip.');
    before.buttons.forEach((entry, index) => {
      check(after.buttons[index].button.height > entry.button.height + tolerance, `${entry.id} must grow with larger text.`);
    });
  } else {
    check(before.tongueVisible === true && after.tongueVisible === false, 'Tongue-hide comparison requires visible then hidden captures.');
    check(before.textMode === after.textMode && before.actionFontSize === after.actionFontSize, 'Text size must remain constant when hiding the tongue.');
    near(-actionDelta, before.tongue?.height ?? NaN, 'Hiding the tongue must release exactly its natural height.');
    before.buttons.forEach((entry, index) => near(entry.button.height, after.buttons[index].button.height, `${entry.id} height must not change when hiding the tongue.`));
  }
  return { ok: errors.length === 0, errors, metrics: { actionDelta, viewportDelta } };
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const paths = process.argv.slice(2);
  if (paths.length === 0) {
    process.stderr.write('Usage: node tools/simulator-proof/squishy-evidence.mjs <capture.json> [capture.json ...]\n');
    process.exitCode = 2;
  }
  for (const path of paths) {
    try {
      const result = validateSquishyEvidence(JSON.parse(readFileSync(path, 'utf8').replace(/^\uFEFF/, '')));
      process.stdout.write(`${JSON.stringify({ path, ...result }, null, 2)}\n`);
      if (!result.ok) process.exitCode = 1;
    } catch (error) {
      process.stderr.write(`${path}: ${error.message}\n`);
      process.exitCode = 1;
    }
  }
}
