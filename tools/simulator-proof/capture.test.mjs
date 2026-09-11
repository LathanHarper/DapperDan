import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { selectDeviceType, selectRuntime, pngDimensions, validateDimensions } from './capture.mjs';
import { projectSimulatorLock } from './project-lock.mjs';

function png(width, height) {
  const bytes = Buffer.alloc(24);
  Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]).copy(bytes);
  bytes.write('IHDR', 12, 'ascii');
  bytes.writeUInt32BE(width, 16);
  bytes.writeUInt32BE(height, 20);
  return bytes;
}

test('known native large phone and tablet types are selected without size conversion', () => {
  assert.equal(selectDeviceType([{ name: 'iPhone 14 Plus', identifier: 'phone' }], 'iphone').identifier, 'phone');
  assert.equal(selectDeviceType([{ name: 'iPad Pro 13-inch (M4)', identifier: 'tablet' }], 'ipad').identifier, 'tablet');
  assert.throws(() => selectDeviceType([{ name: 'iPad mini' }], 'ipad'), /No known accepted-size/);
});

test('only installed available iOS runtimes qualify, selecting latest numerically', () => {
  const make = (version, isAvailable = true, prefix = 'iOS-') => ({ version, isAvailable, identifier: `com.apple.CoreSimulator.SimRuntime.${prefix}${version}` });
  assert.equal(selectRuntime([make('26.5'), make('26.10'), make('27', false), make('99', true, 'tvOS-')]).version, '26.10');
  assert.throws(() => selectRuntime([make('27', false)]), /No installed available/);
});

test('captures require PNG signature and accepted unresized native dimensions', () => {
  assert.deepEqual(pngDimensions(png(1284, 2778)), [1284, 2778]);
  assert.deepEqual(validateDimensions(png(2064, 2752), 'ipad'), [2064, 2752]);
  assert.throws(() => pngDimensions(Buffer.alloc(24)), /not a PNG/);
  assert.throws(() => validateDimensions(png(1536, 2048), 'ipad'), /native accepted/);
});

test('iOS-only restore keeps exact pinned dependency objects without Android or device targets', () => {
  const original = JSON.parse(readFileSync(new URL('../../src/DapperDan/packages.lock.json', import.meta.url), 'utf8'));
  const projected = projectSimulatorLock(original);
  assert.equal(Object.keys(projected.dependencies).length, 2);
  for (const [key, value] of Object.entries(projected.dependencies)) {
    assert.strictEqual(value, original.dependencies[key]);
    assert.doesNotMatch(key, /android|\/ios-arm64/);
  }
});

test('branch-only workflow is manual public standard-runner Debug and preserves app first', () => {
  const text = readFileSync(new URL('../../.github/workflows/ios-unsigned.yml', import.meta.url), 'utf8');
  assert.match(text, /workflow_dispatch:/);
  assert.doesNotMatch(text, /^\s+(push|pull_request|pull_request_target|schedule|workflow_run):/m);
  assert.match(text, /github\.event\.repository\.private == false/);
  assert.match(text, /runs-on: macos-26\n/);
  assert.match(text, /contents: read/);
  assert.doesNotMatch(text, /secrets\.|environment:|signing-certs|upload-testflight|\-c Release/);
  assert.match(text, /-c Debug -r iossimulator-arm64/);
  assert.match(text, /EnableCodeSigning=false/);
  assert.match(text, /dotnet workload install maui-ios/);
  assert.match(text, /TargetFrameworks=net10.0-ios/);
  assert.ok(text.indexOf('Retain app and public provenance') < text.indexOf('Capture native iPhone'));
  for (const line of text.split('\n').filter(value => value.includes('uses:'))) assert.match(line, /@[0-9a-f]{40}\b/);
});
