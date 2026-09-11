import { spawnSync } from 'node:child_process';
import { closeSync, copyFileSync, existsSync, mkdirSync, openSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { acceptedDimensions, command, pngDimensions, selectDeviceType, selectRuntime, sha256, validateProductReceipt } from './capture.mjs';
import { validateSquishyEvidence } from './squishy-evidence.mjs';

export const orientations = ['portrait', 'landscape'];
const project = 'tools/simulator-proof/ScreenshotDriver/ScreenshotDriver.xcodeproj';
const derived = 'artifacts/ui-driver';
const appName = 'CodeCrafty.DapperDan.app';
const executable = 'CodeCrafty.DapperDan';
const save = (path, value) => writeFileSync(path, JSON.stringify(value, null, 2) + '\n');

export function nativePngGeometry(bytes) {
  const pixelDimensions = pngDimensions(bytes);
  if (!bytes.subarray(-12).equals(Buffer.from('0000000049454e44ae426082', 'hex')))
    throw new Error('Incomplete native PNG.');
  let exifOrientation = 1;
  for (let offset = 8; offset + 12 <= bytes.length;) {
    const length = bytes.readUInt32BE(offset);
    if (offset + length + 12 > bytes.length) throw new Error('Truncated PNG chunk.');
    if (bytes.toString('ascii', offset + 4, offset + 8) === 'eXIf') {
      const tiff = bytes.subarray(offset + 8, offset + 8 + length);
      const order = tiff.toString('ascii', 0, 2);
      if (tiff.length < 8 || !['II', 'MM'].includes(order)) throw new Error('Invalid PNG EXIF header.');
      const u16 = p => order === 'II' ? tiff.readUInt16LE(p) : tiff.readUInt16BE(p);
      const u32 = p => order === 'II' ? tiff.readUInt32LE(p) : tiff.readUInt32BE(p);
      if (u16(2) !== 42) throw new Error('Invalid TIFF marker.');
      const ifd = u32(4);
      const count = u16(ifd);
      for (let i = 0; i < count; i++) {
        const entry = ifd + 2 + i * 12;
        if (entry + 12 > tiff.length) throw new Error('Truncated EXIF entry.');
        if (u16(entry) === 0x0112) {
          if (u16(entry + 2) !== 3 || u32(entry + 4) !== 1) throw new Error('Invalid EXIF orientation type.');
          exifOrientation = u16(entry + 8);
          if (exifOrientation < 1 || exifOrientation > 8) throw new Error('Invalid EXIF orientation value.');
        }
      }
    }
    offset += length + 12;
  }
  // XCTest preserves sensor-axis pixels and encodes landscape as TIFF orientation
  // (observed value 8). Interpret that metadata; never rotate or rewrite the PNG.
  return { pixelDimensions, exifOrientation,
    displayDimensions: exifOrientation >= 5 ? [...pixelDimensions].reverse() : pixelDimensions };
}

export function validateOrientedPng(bytes, family, orientation) {
  if (!orientations.includes(orientation)) throw new Error('Unknown orientation.');
  const size = nativePngGeometry(bytes).displayDimensions;
  const expected = acceptedDimensions[family].map(pair => orientation === 'portrait' ? pair : [...pair].reverse());
  if (!expected.some(pair => pair.every((value, i) => value === size[i])))
    throw new Error(`Wrong native ${family}/${orientation} dimensions: ${size.join('x')}.`);
  return size;
}

export function validateCaptureMeasurement(value, orientation, startedUtc) {
  const result = validateSquishyEvidence(value);
  if (!result.ok) throw new Error(`Native layout failed: ${result.errors.join('; ')}`);
  if (value.platform !== 'iOS' || value.moreVisible || value.textMode !== 'normal' || !value.tongueVisible)
    throw new Error('Wrong default iOS squishy state.');
  if ((value.host.height > value.host.width) !== (orientation === 'portrait'))
    throw new Error('Geometry is from the wrong orientation.');
  if (Date.parse(value.capturedUtc) < Date.parse(startedUtc) - 1000)
    throw new Error('Geometry predates this native test.');
  return result;
}

function loggedXcode(args, logPath, timeout) {
  console.log(`Native driver: xcodebuild ${args.join(' ')}`);
  const fd = openSync(logPath, 'w');
  let result;
  try { result = spawnSync('xcodebuild', args, { stdio: ['ignore', fd, fd], timeout }); }
  finally { closeSync(fd); }
  if (result.error || result.status !== 0)
    throw new Error(`Native driver failed (${result.error?.message ?? result.status}); ${logPath}\n${readFileSync(logPath, 'utf8').slice(-6000)}`);
}

function buildDriver() {
  mkdirSync('artifacts/captures', { recursive: true });
  loggedXcode(['build-for-testing', '-project', project, '-scheme', 'ScreenshotTests',
    '-configuration', 'Debug', '-destination', 'generic/platform=iOS Simulator',
    '-derivedDataPath', derived, '-quiet'], 'artifacts/captures/ui-driver-build.log', 180000);
  console.log('Native rotation driver built once; no MAUI application build in this step.');
}

async function capture() {
  const output = resolve('artifacts/captures');
  mkdirSync(output, { recursive: true });
  const retained = resolve(process.env.SIMULATOR_PRODUCT_DIRECTORY);
  const original = validateProductReceipt(JSON.parse(readFileSync(join(retained, 'receipt.json'), 'utf8')), undefined, 'squishy');
  const prepared = JSON.parse(readFileSync('artifacts/adhoc-recovery/transformation-receipt.json', 'utf8'));
  const app = resolve(process.env.SIMULATOR_APP_DIRECTORY, appName);
  const binarySha256 = sha256(join(app, executable));
  if (prepared.canary !== original.canary || prepared.sourceCommit !== original.sourceCommit ||
      prepared.sourceArchiveSha256 !== original.sha256 || prepared.originalArchiveUnchanged !== true ||
      prepared.archive !== 'DapperDan-Debug-iossimulator-arm64-adhoc.tar.gz' ||
      sha256(join(retained, original.archive)) !== original.sha256 ||
      sha256(join('artifacts/adhoc-recovery', prepared.archive)) !== prepared.sha256 || binarySha256 !== prepared.binarySha256)
    throw new Error('Prepared squishy product provenance mismatch.');
  command('codesign', ['--verify', '--deep', '--strict', app]);
  const products = resolve(derived, 'Build/Products');
  const runners = readdirSync(products).filter(name => name.endsWith('.xctestrun'));
  if (runners.length !== 1) throw new Error('Expected one already-built native test driver.');
  const runner = join(products, runners[0]);
  const inventory = JSON.parse(command('xcrun', ['simctl', 'list', '-j']));
  const runtime = selectRuntime(inventory.runtimes);
  const receipt = {
    schema: 1, canary: 'squishy-v1', sourceCommit: original.sourceCommit,
    sourceRunId: original.runId, runId: process.env.GITHUB_RUN_ID, automationCommit: process.env.GITHUB_SHA,
    sourceArchiveSha256: original.sha256, preparedArchiveSha256: prepared.sha256, binarySha256,
    runtime: runtime.identifier, captures: [], devices: [],
  };
  for (const family of ['iphone', 'ipad']) {
    let udid;
    const device = { family, startedUtc: new Date().toISOString() };
    try {
      const type = selectDeviceType(inventory.devicetypes, family);
      device.name = type.name;
      console.log(`Boot/install once: ${type.name}. Both orientations reuse the same Dapper binary.`);
      udid = command('xcrun', ['simctl', 'create', `Dapper squishy ${family}`, type.identifier, runtime.identifier]);
      command('xcrun', ['simctl', 'boot', udid]);
      command('xcrun', ['simctl', 'bootstatus', udid, '-b'], 240000);
      command('xcrun', ['simctl', 'status_bar', udid, 'override', '--time', '9:41', '--batteryState', 'charged', '--batteryLevel', '100']);
      command('xcrun', ['simctl', 'ui', udid, 'appearance', 'light']);
      command('xcrun', ['simctl', 'install', udid, app]);
      for (const orientation of orientations) {
        const prefix = `${family}-${orientation}`;
        const record = { family, orientation, deviceName: type.name, binarySha256, startedUtc: new Date().toISOString() };
        receipt.captures.push(record);
        try {
          const resultPath = join(output, `${prefix}.xcresult`);
          const method = orientation === 'portrait' ? 'testPortrait' : 'testLandscape';
          loggedXcode(['test-without-building', '-xctestrun', runner,
            '-destination', `platform=iOS Simulator,id=${udid}`, '-destination-timeout', '60',
            '-parallel-testing-enabled', 'NO', '-maximum-concurrent-test-simulator-destinations', '1',
            '-only-testing', `ScreenshotTests/ScreenshotTests/${method}`,
            '-resultBundlePath', resultPath, '-quiet'], join(output, `${prefix}-driver.log`), 240000);
          const exported = join(output, `${prefix}-attachments`);
          command('xcrun', ['xcresulttool', 'export', 'attachments', '--path', resultPath, '--output-path', exported]);
          const pngs = readdirSync(exported, { recursive: true }).filter(name => name.endsWith('.png'));
          if (pngs.length !== 1) throw new Error(`Expected one explicitly retained native screenshot, found ${pngs.length}; attachments retained.`);
          const image = join(output, `${prefix}.png`);
          copyFileSync(join(exported, pngs[0]), image);
          Object.assign(record, nativePngGeometry(readFileSync(image)));
          record.dimensions = validateOrientedPng(readFileSync(image), family, orientation);
          record.image = basename(image);
          record.imageSha256 = sha256(image);
          const container = command('xcrun', ['simctl', 'get_app_container', udid, process.env.BUNDLE_ID, 'data']);
          const measuredPath = join(output, `${prefix}-squishy.json`);
          copyFileSync(join(container, 'Library', 'squishy-canary.json'), measuredPath);
          const measurement = JSON.parse(readFileSync(measuredPath, 'utf8'));
          record.geometry = validateCaptureMeasurement(measurement, orientation, record.startedUtc);
          record.measurements = basename(measuredPath);
          record.measurementsSha256 = sha256(measuredPath);
          if (sha256(join(app, executable)) !== binarySha256) throw new Error('Dapper binary changed between captures.');
          record.status = 'captured-awaiting-visual-check';
          console.log(`${prefix}: ${record.dimensions.join('x')}, native UI test passed and geometry verified.`);
        } catch (error) {
          record.status = 'failed'; record.error = error.message; throw error;
        } finally { save(join(output, 'squishy-capture-receipt.json'), receipt); }
      }
      device.status = 'both-orientations-captured';
    } catch (error) {
      device.status = 'failed'; device.error = error.message; throw error;
    } finally {
      if (udid) {
        try { command('xcrun', ['simctl', 'shutdown', udid], 60000); }
        catch (error) { device.shutdownWarning = error.message; }
      }
      device.finishedUtc = new Date().toISOString();
      receipt.devices.push(device);
      save(join(output, 'squishy-capture-receipt.json'), receipt);
    }
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  if (process.platform !== 'darwin') throw new Error('Native Simulator capture requires macOS.');
  if (process.argv[2] === 'build-driver') buildDriver();
  else if (process.argv[2] === 'capture') await capture();
  else throw new Error('Expected build-driver or capture.');
}
