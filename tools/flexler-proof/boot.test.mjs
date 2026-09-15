import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluateJournal, sanitizeRecord, selectIpad } from './boot.mjs';

const runtime = (version, available = true) => ({ identifier: `com.apple.CoreSimulator.SimRuntime.iOS-${version.replaceAll('.', '-')}`, version, isAvailable: available });
const ipad = (name, overrides = {}) => ({ name, udid: '11111111-2222-3333-4444-555555555555', isAvailable: true, state: 'Shutdown', ...overrides });

test('chooses an existing available iPad from the newest installed usable runtime', () => {
  const older = runtime('18.5'), newer = runtime('26.5'), unavailable = runtime('27.0', false);
  const inventory = {
    runtimes: [older, unavailable, newer],
    devices: {
      [older.identifier]: [ipad('iPad Pro')],
      [newer.identifier]: [ipad('iPhone 17'), ipad('iPad Air', { isAvailable: false }), ipad('iPad Pro 13-inch')],
      [unavailable.identifier]: [ipad('iPad Pro')],
    },
  };
  assert.equal(selectIpad(inventory).name, 'iPad Pro 13-inch');
  assert.equal(selectIpad(inventory).runtime, newer.identifier);
});

test('does not select unavailable, booting or non-iPad devices', () => {
  const current = runtime('26.5');
  assert.throws(() => selectIpad({ runtimes: [current], devices: { [current.identifier]: [
    ipad('iPhone 17'), ipad('iPad Pro', { isAvailable: false }), ipad('iPad Air', { state: 'Booting' }),
  ] } }), /No stock available iPad/);
});

test('a live process alone and unrelated page readiness never pass', () => {
  assert.equal(evaluateJournal([], true).passed, false);
  assert.equal(evaluateJournal([{ kind: 'checkpoint', point: 'PageLoaded' }], true).passed, false);
});

test('only the exact Flexler checkpoint plus a live process can pass', () => {
  const loaded = [{ kind: 'checkpoint', point: 'FlexlerPageLoaded' }];
  assert.equal(evaluateJournal(loaded, true).passed, true);
  assert.equal(evaluateJournal(loaded, false).passed, false);
  assert.equal(evaluateJournal([{ kind: 'exception', point: 'FlexlerPageLoaded' }], true).passed, false);
});

test('exceptions veto readiness, including emergency records', () => {
  for (const kind of ['exception', 'emergency-exception']) {
    const result = evaluateJournal([{ kind: 'checkpoint', point: 'FlexlerPageLoaded' }, { kind, terminating: false }], true);
    assert.equal(result.passed, false);
    assert.equal(result.exceptionCount, 1);
  }
});

test('published journal fields exclude text, paths, identities and arbitrary values', () => {
  const safe = sanitizeRecord({
    seq: 5, elapsedMs: 1200, kind: 'exception', point: 'FlexlerPageXamlEnter',
    exceptionType: 'System.InvalidOperationException', terminating: true, isDynamicCodeSupported: true,
    message: 'private@example.invalid', stack: '/Users/example/private', inner: 'sensitive',
    launchId: 'private-id', source: 'bad\n::warning::content', os: 'private machine', secret: 'hidden',
  });
  assert.deepEqual(safe, {
    seq: 5, elapsedMs: 1200, kind: 'exception', point: 'FlexlerPageXamlEnter',
    exceptionType: 'System.InvalidOperationException', terminating: true, isDynamicCodeSupported: true,
  });
});
