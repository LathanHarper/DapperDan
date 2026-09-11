import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { scenarioContract, validateProductReceipt } from './capture.mjs';
import { orientations, nativePngGeometry, validateOrientedPng } from './capture-squishy.mjs';

const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
function png(width, height, orientation) {
  const bytes = Buffer.alloc(45);
  Buffer.from([137,80,78,71,13,10,26,10]).copy(bytes);
  bytes.writeUInt32BE(13,8);
  bytes.write('IHDR',12); bytes.writeUInt32BE(width,16); bytes.writeUInt32BE(height,20);
  Buffer.from('0000000049454e44ae426082','hex').copy(bytes,33);
  if (orientation !== undefined) {
    const exif = Buffer.alloc(38);
    exif.writeUInt32BE(26,0); exif.write('eXIf',4); exif.write('MM',8);
    exif.writeUInt16BE(42,10); exif.writeUInt32BE(8,12); exif.writeUInt16BE(1,16);
    exif.writeUInt16BE(0x0112,18); exif.writeUInt16BE(3,20); exif.writeUInt32BE(1,22);
    exif.writeUInt16BE(orientation,26);
    return Buffer.concat([bytes.subarray(0,33),exif,bytes.subarray(33)]);
  }
  return bytes;
}

test('Apple landscape EXIF is interpreted without modifying original native pixels', () => {
  const bytes = png(1284,2778,8);
  const original = Buffer.from(bytes);
  assert.deepEqual(nativePngGeometry(bytes), {pixelDimensions:[1284,2778],exifOrientation:8,displayDimensions:[2778,1284]});
  assert.deepEqual(validateOrientedPng(bytes,'iphone','landscape'),[2778,1284]);
  assert.deepEqual(bytes,original);
  assert.deepEqual(validateOrientedPng(png(1284,2778,1),'iphone','portrait'),[1284,2778]);
  assert.throws(()=>validateOrientedPng(bytes,'iphone','portrait'),/Wrong native/);
  assert.throws(()=>nativePngGeometry(png(1284,2778,9)),/Invalid EXIF/);
});

test('four captures use native portrait and landscape dimensions, never resized pixels', () => {
  assert.deepEqual(orientations, ['portrait','landscape']);
  for (const [family,w,h] of [['iphone',1284,2778],['ipad',2064,2752]]) {
    assert.deepEqual(validateOrientedPng(png(w,h),family,'portrait'),[w,h]);
    assert.deepEqual(validateOrientedPng(png(h,w),family,'landscape'),[h,w]);
    assert.throws(()=>validateOrientedPng(png(w,h),family,'landscape'),/Wrong native/);
  }
  assert.throws(()=>validateOrientedPng(png(1284,2778).subarray(0,24),'iphone','portrait'),/Incomplete/);
});

test('squishy reuse cannot accept an old bottom-panel binary receipt', () => {
  const receipt = {schema:2,...scenarioContract('squishy'),repository:'LathanHarper/DapperDan',
    workflow:'.github/workflows/ios-unsigned.yml',sourceRef:'refs/heads/codex/panelboss-bottom-inset-canary',
    sourceCommit:'a'.repeat(40),runId:'123',runAttempt:'1',bundleId:'net.codecrafty.dapperdan',
    configuration:'Debug',runtime:'iossimulator-arm64',archive:'DapperDan-Debug-iossimulator-arm64.tar.gz',
    archiveBytes:100,sha256:'b'.repeat(64),binarySha256:'c'.repeat(64)};
  assert.equal(validateProductReceipt(receipt,'123','squishy'),receipt);
  assert.throws(()=>validateProductReceipt(receipt,'123','bottom-panel'));
  assert.throws(()=>validateProductReceipt({...receipt,...scenarioContract('bottom-panel')},'123','squishy'));
  assert.throws(()=>scenarioContract('unknown'));
});

test('native driver rotates the existing app with XCTest and does not build application source', () => {
  const swift = read('./ScreenshotDriver/ScreenshotTests.swift');
  const project = read('./ScreenshotDriver/ScreenshotDriver.xcodeproj/project.pbxproj');
  assert.match(swift,/XCUIDevice.shared.orientation = orientation/);
  assert.match(swift,/XCUIApplication\(bundleIdentifier: "net.codecrafty.dapperdan"\)/);
  assert.match(swift,/func testPortrait/); assert.match(swift,/func testLandscape/);
  assert.match(swift,/XCUIScreen.main.screenshot\(\)/);
  assert.match(swift,/\.keepAlways/);
  assert.match(project,/com.apple.product-type.bundle.ui-testing/);
  assert.doesNotMatch(project,/csproj|net10|\.cs\b|\.xaml|product-type.application/);
});

test('capture driver uses one install per device and test-without-building for both orientations', () => {
  const script = read('./capture-squishy.mjs');
  assert.match(script,/for \(const family of \['iphone', 'ipad'\]\)/);
  assert.match(script,/for \(const orientation of orientations\)/);
  assert.equal((script.match(/\['simctl', 'install', udid, app\]/g)??[]).length,1);
  assert.match(script,/\['test-without-building', '-xctestrun'/);
  assert.match(script,/'-parallel-testing-enabled', 'NO'/);
  assert.doesNotMatch(script,/dotnet|\.resize\(|ImageMagick/);
  assert.match(script,/validateCaptureMeasurement\(measurement, orientation, record.startedUtc\)/);
  assert.match(script,/Dapper binary changed between captures/);
});

test('manual squishy workflow retains public/free, pin, one-app-build and preservation gates', () => {
  const workflow = read('../../.github/workflows/ios-unsigned.yml');
  assert.match(workflow,/type: choice[\s\S]*- bottom-panel[\s\S]*- squishy/);
  assert.equal((workflow.match(/-p:DapperDanSquishyCanary=/g)??[]).length,2);
  assert.equal((workflow.match(/dotnet build "\$PROJECT_PATH"/g)??[]).length,1);
  assert.ok(workflow.indexOf('Preserve re-sealed Simulator product') < workflow.indexOf('Capture four native squishy'));
  assert.match(workflow,/if: \$\{\{ inputs.scenario == 'squishy' \}\}[\s\S]*capture-squishy.mjs capture/);
  assert.match(workflow,/github.event.repository.private == false/);
  assert.match(workflow,/runs-on: macos-26\s/);
});
