import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { selectDeviceType, selectRuntime, pngDimensions, validateDimensions, panelCases, validateMeasurements, validateProductReceipt, validateArchivePaths, caseLaunchEnvironment, runPanelCases } from './capture.mjs';
import { projectSimulatorLock } from './project-lock.mjs';

function png(width, height) {
  const bytes = Buffer.alloc(36);
  Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]).copy(bytes);
  bytes.write('IHDR', 12, 'ascii');
  bytes.writeUInt32BE(width, 16);
  bytes.writeUInt32BE(height, 20);
  Buffer.from('0000000049454e44ae426082', 'hex').copy(bytes, 24);
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
  assert.throws(() => validateDimensions(png(1284, 2778).subarray(0, 24), 'iphone'), /incomplete/);
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
  const text = readFileSync(new URL('../../.github/workflows/ios-unsigned.yml', import.meta.url), 'utf8').replace(/\r\n/g, '\n');
  assert.match(text, /workflow_dispatch:/);
  assert.doesNotMatch(text, /^\s+(push|pull_request|pull_request_target|schedule|workflow_run):/m);
  assert.match(text, /github\.event\.repository\.private == false/);
  assert.match(text, /github\.ref == 'refs\/heads\/codex\/panelboss-bottom-inset-canary'/);
  assert.match(text, /github\.event_name == 'workflow_dispatch'/);
  assert.match(text, /runs-on: macos-26\n/);
  assert.match(text, /contents: read/);
  assert.doesNotMatch(text, /secrets\.|environment:|signing-certs|upload-testflight|\-c Release/);
  assert.match(text, /-c Debug -r iossimulator-arm64/);
  assert.match(text, /EnableCodeSigning=false/);
  assert.match(text, /dotnet workload install maui-ios/);
  assert.match(text, /TargetFrameworks=net10.0-ios/);
  assert.equal((text.match(/dotnet build "\$PROJECT_PATH"/g) ?? []).length, 1);
  assert.equal((text.match(/-p:DapperDanBottomPanelCanary=true/g) ?? []).length, 2);
  assert.match(text, /node --test tools\/simulator-proof\/\*\.test\.mjs/);
  assert.ok(text.indexOf('Retain app and public provenance') < text.indexOf('Capture native iPhone'));
  const signing = text.indexOf('- name: Re-seal a fresh Simulator copy');
  const prepared = text.indexOf('- name: Preserve re-sealed Simulator product');
  assert.ok(text.indexOf('Retain app and public provenance') < signing);
  assert.ok(signing < prepared && prepared < text.indexOf('Capture native iPhone'));
  assert.doesNotMatch(text.slice(signing, prepared), /if:|inputs\./);
  for (const line of text.split('\n').filter(value => value.includes('uses:'))) assert.match(line, /@[0-9a-f]{40}\b/);
});

function measurements(caseName = 'baseline') {
  const rect = { x: 0, y: 1, width: 100, height: 36 };
  return {
    schema: 1, case: caseName, panel: { x: 0, y: 600, width: 430, height: 124 },
    buttons: Array.from({ length: 5 }, (_, index) => ({ id: `action-${index}`, button: { ...rect }, border: { ...rect } })),
    iosInsets: { top: 59, bottom: 34, left: 0, right: 0 },
  };
}

test('case measurements require correct case and five valid panel/button/border bounds', () => {
  for (const caseName of panelCases) assert.equal(validateMeasurements(measurements(caseName), caseName).case, caseName);
  assert.throws(() => validateMeasurements(measurements(), 'safe-area-none'), /case\/schema/);
  for (const mutate of [
    value => { value.schema = 2; },
    value => { value.measurementError = 'layout failed'; },
    value => { value.buttons.pop(); },
    value => { value.buttons[1].id = value.buttons[0].id; },
    value => { value.buttons[0].id = ''; },
    value => { value.panel.width = 0; },
    value => { value.buttons[0].button.height = -1; },
    value => { value.buttons[0].border.width = 0; },
    value => { value.buttons[0].border.x = Infinity; },
    value => { value.panel.y = NaN; },
    value => { value.panel.width = '100'; },
    value => { value.iosInsets.bottom = -1; },
    value => { value.iosInsets = undefined; },
  ]) {
    const value = measurements();
    mutate(value);
    assert.throws(() => validateMeasurements(value, 'baseline'));
  }
  const negativePosition = measurements();
  negativePosition.panel.y = -10;
  assert.doesNotThrow(() => validateMeasurements(negativePosition, 'baseline'));
});

test('case launch environments are isolated and never mutate inherited settings', () => {
  const inherited = { PATH: 'test-path', SIMCTL_CHILD_DAPPERDAN_PANEL_CASE: 'old' };
  assert.deepEqual(caseLaunchEnvironment('baseline', inherited), { PATH: 'test-path', SIMCTL_CHILD_DAPPERDAN_PANEL_CASE: 'baseline' });
  assert.equal(caseLaunchEnvironment('safe-area-none', inherited).SIMCTL_CHILD_DAPPERDAN_PANEL_CASE, 'safe-area-none');
  assert.equal(inherited.SIMCTL_CHILD_DAPPERDAN_PANEL_CASE, 'old');
  assert.throws(() => caseLaunchEnvironment('unknown', inherited), /Unknown/);
});

test('two cases reuse the device and terminate exactly once between fresh launches', async () => {
  const events = [];
  await runPanelCases(name => events.push(name), () => events.push('terminate'));
  assert.deepEqual(events, ['baseline', 'terminate', 'safe-area-none']);
});

test('failed first case or termination never advances to another case', async () => {
  const events = [];
  await assert.rejects(runPanelCases(name => { events.push(name); throw new Error('launch failed'); }, () => events.push('terminate')), /launch failed/);
  assert.deepEqual(events, ['baseline']);
  await assert.rejects(runPanelCases(name => events.push(name), () => { throw new Error('termination failed'); }), /termination failed/);
  assert.deepEqual(events, ['baseline', 'baseline']);
});

function productReceipt() {
  return {
    schema: 2, canary: 'bottom-panel-v1', cases: [...panelCases], repository: 'LathanHarper/DapperDan',
    workflow: '.github/workflows/ios-unsigned.yml', sourceRef: 'refs/heads/codex/panelboss-bottom-inset-canary',
    sourceCommit: 'a'.repeat(40), runId: '123', runAttempt: '1', bundleId: 'net.codecrafty.dapperdan',
    configuration: 'Debug', runtime: 'iossimulator-arm64', archive: 'DapperDan-Debug-iossimulator-arm64.tar.gz',
    archiveBytes: 100, sha256: 'b'.repeat(64), binarySha256: 'c'.repeat(64),
  };
}

test('reuse requires matching producer identity and a binary carrying both canary cases', () => {
  assert.equal(validateProductReceipt(productReceipt(), '123').sourceCommit, 'a'.repeat(40));
  assert.throws(() => validateProductReceipt(productReceipt(), '456'), /expected public/);
  for (const mutate of [
    value => { value.canary = undefined; },
    value => { value.cases.pop(); },
    value => { value.repository = 'other/repo'; },
    value => { value.workflow = '.github/workflows/other.yml'; },
    value => { value.sourceRef = 'refs/heads/codex/simulator-capture-proof'; },
    value => { value.sourceCommit = 'not-a-sha'; },
    value => { value.runAttempt = ''; },
    value => { value.archive = '../escape.tar.gz'; },
    value => { value.archiveBytes = -1; },
    value => { value.binarySha256 = undefined; },
  ]) {
    const value = productReceipt();
    mutate(value);
    assert.throws(() => validateProductReceipt(value), /expected public/);
  }
});

test('archive name validation refuses traversal and foreign roots before extraction', () => {
  assert.doesNotThrow(() => validateArchivePaths(['CodeCrafty.DapperDan.app/', 'CodeCrafty.DapperDan.app/Info.plist']));
  for (const name of ['../outside', '/CodeCrafty.DapperDan.app/file', 'CodeCrafty.DapperDan.app/../outside', 'CodeCrafty.DapperDan.app\\file', 'Other.app/file'])
    assert.throws(() => validateArchivePaths([name]), /extraction refused/);
  const text = readFileSync(new URL('./capture.mjs', import.meta.url), 'utf8');
  assert.match(text, /command\('tar', \['-xzf', archive, '-C', output\]/);
  assert.match(text, /dereference: false, verbatimSymlinks: true/);
});

test('case evidence is distinct, preserves failure diagnostics, and hashes the same binary', () => {
  const text = readFileSync(new URL('./capture.mjs', import.meta.url), 'utf8');
  assert.match(text, /const prefix = `\$\{family\}-\$\{caseName\}`/);
  assert.match(text, /caseLaunchEnvironment\(caseName\)/);
  assert.match(text, /captureDiagnostics\(udid, output, prefix, record\)/);
  assert.match(text, /captureMeasurements\(udid, output, prefix, caseName, record\)/);
  assert.match(text, /Capture binary changed between cases/);
  assert.match(text, /Capture binary changed during case/);
  assert.equal((text.match(/\['simctl', 'boot', udid\]/g) ?? []).length, 1);
  assert.equal((text.match(/\['simctl', 'install', udid, app\]/g) ?? []).length, 1);
  assert.match(text, /if \(failures\) break/);
});
