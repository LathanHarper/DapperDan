import test from 'node:test';
import assert from 'node:assert/strict';
import { actionIds, compareSquishyEvidence, validateSquishyEvidence } from './squishy-evidence.mjs';

const frame = (x, y, width, height) => ({ x, y, width, height });
function capture({ font = 'normal', tongue = true, platform = 'Android', inset = 0, clearance = 24,
  topInset = 20, leftInset = 0, rightInset = 0 } = {}) {
  const large = font === 'large';
  const buttonHeight = large ? 104 : 72;
  const tongueHeight = tongue ? (large ? 60 : 36) : 0;
  const actionsBottom = 800 - clearance - inset;
  const actionsTop = actionsBottom - buttonHeight - tongueHeight;
  const buttonTop = actionsBottom - buttonHeight;
  const contentWidth = 400 - leftInset - rightInset;
  const columnWidth = contentWidth / 5;
  const viewportTop = topInset + 100;
  return {
    schema: 1, scenario: 'squishy', sequence: 1, capturedUtc: '2026-09-11T12:00:00Z', platform,
    textMode: font, actionFontSize: large ? 24 : 14, tongueVisible: tongue, moreVisible: false,
    nativeInsets: { left: leftInset, top: topInset, right: rightInset, bottom: platform === 'Android' ? clearance : inset },
    host: frame(0, 0, 400, 800), header: frame(leftInset, topInset, contentWidth, 100),
    viewport: frame(leftInset, viewportTop, contentWidth, actionsTop - viewportTop),
    scroll: frame(leftInset, viewportTop + 14, contentWidth, actionsTop - viewportTop - 28),
    actions: frame(leftInset, actionsTop, contentWidth, buttonHeight + tongueHeight),
    tongue: tongue ? frame(leftInset, actionsTop, contentWidth, tongueHeight) : null,
    platformBottomClearance: clearance,
    buttons: actionIds.map((id, index) => ({
      id,
      button: frame(leftInset + index * columnWidth, buttonTop, columnWidth, buttonHeight),
      border: frame(leftInset + index * columnWidth + 2, buttonTop + 2, columnWidth - 4, buttonHeight - 4),
      glyph: frame(leftInset + index * columnWidth + 10, buttonTop + 10, columnWidth - 20, large ? 30 : 24),
      text: frame(leftInset + index * columnWidth + 10, buttonTop + (large ? 42 : 36), columnWidth - 20, large ? 50 : 26),
    })),
  };
}
const reject = (evidence, message) => {
  const result = validateSquishyEvidence(evidence);
  assert.equal(result.ok, false);
  assert.ok(result.errors.some(error => message.test(error)), result.errors.join('\n'));
};

test('valid normal, large and tongue-hidden captures pass with truthful bottom-clearance scope', () => {
  for (const options of [{}, { font: 'large' }, { tongue: false }]) {
    const result = validateSquishyEvidence(capture(options), { hostBottomInset: 0 });
    assert.deepEqual(result.errors, []);
    assert.equal(result.ok, true);
    assert.equal(result.metrics.headerGap, 0);
    assert.equal(result.metrics.actionGap, 0);
    assert.equal(result.metrics.topInsetVerified, true);
    assert.equal(result.metrics.sideInsetsVerified, true);
    assert.equal(result.metrics.bottomInsetVerified, true);
    assert.equal(result.metrics.unattributedBottomGap, 0);
  }
  const unknown = capture({ platform: 'iOS', inset: 34, clearance: 0 });
  delete unknown.nativeInsets;
  const unknownInset = validateSquishyEvidence(unknown, { hostTopInset: 20, hostLeftInset: 0, hostRightInset: 0 });
  assert.equal(unknownInset.ok, true);
  assert.equal(unknownInset.metrics.bottomInsetVerified, false);
  assert.equal(unknownInset.metrics.unattributedBottomGap, 34);
});

test('explicit iOS safe-area clearance is accepted once, not silently doubled', () => {
  const ios = capture({ platform: 'iOS', inset: 34, clearance: 0 });
  assert.equal(validateSquishyEvidence(ios, { hostBottomInset: 34 }).ok, true);
  const doubled = capture({ platform: 'iOS', inset: 68, clearance: 0 });
  const result = validateSquishyEvidence(doubled, { hostBottomInset: 34 });
  assert.equal(result.ok, false);
  assert.match(result.errors.join('\n'), /Host bottom gap/);
});

test('native inset evidence follows the declared platform-specific host policy', () => {
  const ios = capture({ platform: 'iOS', inset: 34, clearance: 0, topInset: 47 });
  assert.equal(validateSquishyEvidence(ios).ok, true);
  assert.equal(validateSquishyEvidence(ios).metrics.bottomInsetVerified, true);
  assert.equal(validateSquishyEvidence(ios).metrics.unattributedBottomGap, 0);
  const android = capture({ topInset: 24 });
  assert.equal(validateSquishyEvidence(android).ok, true);
  android.nativeInsets.bottom = -1;
  reject(android, /Native insets must be finite and nonnegative/);
  const doubledAndroid = capture({ inset: 24 });
  reject(doubledAndroid, /Host bottom gap/);
});

test('an extra 20-point top band fails even when both inter-panel gaps remain zero', () => {
  for (const platform of ['Android', 'iOS']) {
    const shifted = capture({ platform });
    shifted.header.y += 20;
    shifted.viewport.y += 20;
    shifted.viewport.height -= 20;
    shifted.scroll.y += 20;
    shifted.scroll.height -= 20;
    reject(shifted, /Header top must follow the host top safe-area inset/);
  }
});

test('top and side anchoring require evidence rather than guessing missing insets', () => {
  const missing = capture();
  delete missing.nativeInsets;
  reject(missing, /Native top inset evidence/);
  reject(missing, /Native side inset evidence/);
  const explicit = validateSquishyEvidence(missing, { hostTopInset: 20, hostLeftInset: 0, hostRightInset: 0 });
  assert.equal(explicit.ok, true);
  const alternate = capture();
  const mismatch = validateSquishyEvidence(alternate, { hostTopInset: 30 });
  assert.equal(mismatch.ok, false);
  for (const name of ['hostTopInset', 'hostLeftInset', 'hostRightInset', 'hostBottomInset']) {
    for (const value of [-1, NaN, Infinity]) assert.throws(() => validateSquishyEvidence(capture(), { [name]: value }), new RegExp(name));
  }
});

test('content occupies the full safe-area width without doubled side insets', () => {
  for (const platform of ['Android', 'iOS']) {
    const inset = capture({ platform, leftInset: 24, rightInset: 12 });
    assert.equal(validateSquishyEvidence(inset).ok, true);
    const doubled = structuredClone(inset);
    doubled.nativeInsets.left = 12;
    reject(doubled, /left must follow the host left safe-area inset/);
    const narrow = structuredClone(inset);
    narrow.nativeInsets.right = 0;
    reject(narrow, /right must follow the host right safe-area inset/);
  }
});

test('viewport gap and overlap both fail, at either chrome boundary', () => {
  for (const delta of [-8, 8]) {
    const top = capture();
    top.viewport.y += delta;
    top.viewport.height -= delta;
    reject(top, /Viewport top must follow header bottom/);
    const lower = capture();
    lower.viewport.height += delta;
    reject(lower, /Viewport bottom must follow action top/);
  }
});

test('nonfinite, zero and negative bounds fail without throwing on malformed evidence', () => {
  for (const value of [NaN, Infinity, 0, -1]) {
    const bad = capture();
    bad.viewport.height = value;
    reject(bad, /viewport must have finite coordinates and positive dimensions/);
  }
  for (const bad of [null, {}, { schema: 1 }, { ...capture(), buttons: [null] }]) {
    assert.equal(validateSquishyEvidence(bad).ok, false);
  }
  assert.throws(() => validateSquishyEvidence(capture(), { tolerance: NaN }), /tolerance/);
});

test('missing, extra, reordered and unequal action buttons fail', () => {
  const missing = capture();
  missing.buttons.pop();
  reject(missing, /Exactly five/);
  const extra = capture();
  extra.buttons.push(structuredClone(extra.buttons[0]));
  reject(extra, /Exactly five/);
  const reordered = capture();
  [reordered.buttons[0], reordered.buttons[1]] = [reordered.buttons[1], reordered.buttons[0]];
  reject(reordered, /stable ID/);
  const unequal = capture();
  unequal.buttons[2].button.height += 5;
  reject(unequal, /equal row height/);
});

test('glyph/text clipping, overlapping text and content escaping the viewport fail', () => {
  const clippedText = capture();
  clippedText.buttons[1].text.height += 30;
  reject(clippedText, /Squishy_Bravo text must fit/);
  const clippedGlyph = capture();
  clippedGlyph.buttons[0].glyph.x = -5;
  reject(clippedGlyph, /Squishy_Alpha glyph must fit/);
  const overlapping = capture();
  overlapping.buttons[0].text.y -= 10;
  reject(overlapping, /glyph must not overlap/);
  const scrollOverflow = capture();
  scrollOverflow.scroll.height = scrollOverflow.viewport.height + 10;
  reject(scrollOverflow, /Scroll must fit/);
});

test('host overflow and consumed coordinator clearance fail', () => {
  const outside = capture();
  outside.actions.x = -10;
  reject(outside, /actions must fit/);
  const consumed = capture();
  consumed.platformBottomClearance = 35;
  reject(consumed, /must not consume the coordinator bottom clearance/);
});

test('font growth releases matching viewport space without fixed platform font dimensions', () => {
  const result = compareSquishyEvidence(capture(), capture({ font: 'large' }), { change: 'font-growth' });
  assert.deepEqual(result.errors, []);
  assert.equal(result.ok, true);
  assert.equal(result.metrics.actionDelta, 56);
  assert.equal(result.metrics.viewportDelta, -56);
  const falseGrowth = capture();
  falseGrowth.textMode = 'large';
  falseGrowth.actionFontSize = 24;
  assert.equal(compareSquishyEvidence(capture(), falseGrowth, { change: 'font-growth' }).ok, false);
});

test('hiding the tongue gives exactly its height back without resizing the buttons', () => {
  const result = compareSquishyEvidence(capture(), capture({ tongue: false }), { change: 'tongue-hide' });
  assert.deepEqual(result.errors, []);
  assert.equal(result.ok, true);
  assert.equal(result.metrics.actionDelta, -36);
  assert.equal(result.metrics.viewportDelta, 36);
});

test('a comparison cannot mix devices, rotations, panels or unsafe captures', () => {
  for (const mutate of [
    evidence => { evidence.host.width += 10; },
    evidence => { evidence.platform = 'iOS'; },
    evidence => { evidence.moreVisible = true; },
    evidence => { evidence.viewport.height += 10; },
  ]) {
    const after = capture({ font: 'large' });
    mutate(after);
    assert.equal(compareSquishyEvidence(capture(), after, { change: 'font-growth' }).ok, false);
  }
  assert.throws(() => compareSquishyEvidence(capture(), capture(), { change: 'other' }), /change must be/);
});
