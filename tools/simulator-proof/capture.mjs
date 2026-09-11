import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, readdirSync, writeFileSync, copyFileSync, statSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const acceptedDimensions = {
  iphone: [[1242, 2688], [1284, 2778]],
  ipad: [[2048, 2732], [2064, 2752]],
};

const preferredNames = {
  iphone: ['iPhone 14 Plus', 'iPhone 13 Pro Max', 'iPhone 12 Pro Max', 'iPhone 11 Pro Max', 'iPhone XS Max'],
  ipad: ['iPad Pro 13-inch (M4)', 'iPad Pro (12.9-inch) (6th generation)', 'iPad Pro (12.9-inch) (5th generation)'],
};

export function selectDeviceType(types, family) {
  for (const name of preferredNames[family]) {
    const match = types.find(type => type.name === name);
    if (match) return match;
  }
  throw new Error(`No known accepted-size ${family} device type is installed; do not resize an image.`);
}

export function selectRuntime(runtimes) {
  const candidates = runtimes.filter(runtime => runtime.isAvailable && runtime.identifier.startsWith('com.apple.CoreSimulator.SimRuntime.iOS-'));
  candidates.sort((a, b) => b.version.localeCompare(a.version, undefined, { numeric: true }));
  if (!candidates.length) throw new Error('No installed available iOS Simulator runtime; no automatic runtime download.');
  return candidates[0];
}

export function pngDimensions(bytes) {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  if (bytes.length < 24 || !bytes.subarray(0, 8).equals(signature) || bytes.toString('ascii', 12, 16) !== 'IHDR') {
    throw new Error('Capture is not a PNG image with an IHDR header.');
  }
  return [bytes.readUInt32BE(16), bytes.readUInt32BE(20)];
}

export function validateDimensions(bytes, family) {
  const size = pngDimensions(bytes);
  if (!acceptedDimensions[family].some(expected => expected.every((value, index) => value === size[index]))) {
    throw new Error(`Unexpected ${family} capture size ${size.join('x')}; native accepted dimensions required.`);
  }
  return size;
}

function command(program, args, timeout = 120000) {
  const result = spawnSync(program, args, { encoding: 'utf8', timeout, maxBuffer: 16 * 1024 * 1024 });
  if (result.error || result.status !== 0) {
    throw new Error(`${program} ${args.join(' ')} failed: ${result.error?.message ?? result.stderr ?? result.status}`);
  }
  return result.stdout.trim();
}

function sha256(path) {
  return createHash('sha256').update(readFileSync(path)).digest('hex');
}

function saveJson(path, data) {
  writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`);
}

function preserve() {
  const output = resolve('artifacts/recovery');
  mkdirSync(output, { recursive: true });
  const appDirectory = resolve('src/DapperDan/bin/Debug/net10.0-ios/iossimulator-arm64');
  const apps = readdirSync(appDirectory).filter(name => name.endsWith('.app'));
  if (apps.length !== 1) throw new Error(`Expected one Debug Simulator .app; found ${apps.length}.`);
  const app = join(appDirectory, apps[0]);
  const bundleId = command('/usr/libexec/PlistBuddy', ['-c', 'Print :CFBundleIdentifier', join(app, 'Info.plist')]);
  if (bundleId !== process.env.BUNDLE_ID) throw new Error('Unexpected public canary bundle identifier.');
  const archive = join(output, 'DapperDan-Debug-iossimulator-arm64.tar.gz');
  command('tar', ['-czf', archive, '-C', appDirectory, apps[0]], 180000);
  copyFileSync('LICENSE', join(output, 'LICENSE.txt'));
  copyFileSync('THIRD-PARTY-NOTICES.md', join(output, 'THIRD-PARTY-NOTICES.md'));
  copyFileSync('THIRD-PARTY-LICENSES/Prism-9-package-notice.txt', join(output, 'PRISM-NOTICE.txt'));
  copyFileSync('THIRD-PARTY-LICENSES/DryIoc-MIT.txt', join(output, 'DRYIOC-LICENSE.txt'));
  copyFileSync('src/DapperDan/packages.lock.json', join(output, 'packages.lock.json'));
  saveJson(join(output, 'receipt.json'), {
    sourceCommit: process.env.GITHUB_SHA,
    runId: process.env.GITHUB_RUN_ID,
    configuration: 'Debug', runtime: 'iossimulator-arm64', bundleId,
    sdk: command('dotnet', ['--version']), workload: command('dotnet', ['workload', '--version']),
    xcode: command('xcodebuild', ['-version']),
    archive: basename(archive), archiveBytes: statSync(archive).size, sha256: sha256(archive),
    disclaimer: 'Public generic canary only. Unsigned Simulator app: not a device IPA, App Store submission, or product screenshot. Requires a compatible Apple Silicon Mac Simulator. Preserve licenses; no framework extraction for development reuse.',
  });
  console.log(`Preserved ${basename(archive)} before capture (${statSync(archive).size} bytes).`);
}

async function capture() {
  const output = resolve('artifacts/captures');
  mkdirSync(output, { recursive: true });
  const appDirectory = resolve(process.env.SIMULATOR_APP_DIRECTORY ?? 'src/DapperDan/bin/Debug/net10.0-ios/iossimulator-arm64');
  const appNames = readdirSync(appDirectory).filter(name => name.endsWith('.app'));
  if (appNames.length !== 1) throw new Error('Expected exactly one reusable Simulator app.');
  const app = join(appDirectory, appNames[0]);
  const inventory = JSON.parse(command('xcrun', ['simctl', 'list', '-j']));
  const runtime = selectRuntime(inventory.runtimes);
  const evidence = { sourceCommit: process.env.GITHUB_SHA, runId: process.env.GITHUB_RUN_ID, runtime: runtime.identifier, captures: [] };
  let failures = 0;
  for (const family of ['iphone', 'ipad']) {
    let udid;
    const record = { family, startedUtc: new Date().toISOString() };
    const start = Date.now();
    try {
      const type = selectDeviceType(inventory.devicetypes, family);
      record.deviceType = type.identifier;
      record.deviceName = type.name;
      udid = command('xcrun', ['simctl', 'create', `DapperDan capture ${family}`, type.identifier, runtime.identifier]);
      command('xcrun', ['simctl', 'boot', udid]);
      command('xcrun', ['simctl', 'bootstatus', udid, '-b'], 240000);
      command('xcrun', ['simctl', 'status_bar', udid, 'override', '--time', '9:41', '--batteryState', 'charged', '--batteryLevel', '100']);
      command('xcrun', ['simctl', 'ui', udid, 'appearance', 'light']);
      command('xcrun', ['simctl', 'install', udid, app], 120000);
      record.launch = command('xcrun', ['simctl', 'launch', udid, process.env.BUNDLE_ID], 60000);
      await new Promise(resolveWait => setTimeout(resolveWait, 12000));
      const image = join(output, `${family}.png`);
      command('xcrun', ['simctl', 'io', udid, 'screenshot', '--type=png', image], 60000);
      record.dimensions = validateDimensions(readFileSync(image), family);
      record.image = basename(image);
      record.sha256 = sha256(image);
      record.status = 'captured-awaiting-human-visual-check';
      console.log(`${family}: ${record.deviceName}, ${record.dimensions.join('x')}, successful launch and native capture.`);
    } catch (error) {
      record.status = 'failed';
      record.error = error.message;
      failures++;
      console.error(`${family}: ${error.message}`);
    } finally {
      if (udid) {
        try { command('xcrun', ['simctl', 'shutdown', udid], 60000); }
        catch (error) { record.shutdownWarning = error.message; }
      }
      record.seconds = Math.round((Date.now() - start) / 1000);
      evidence.captures.push(record);
      saveJson(join(output, 'capture-receipt.json'), evidence);
    }
  }
  if (failures) throw new Error(`${failures} capture family failed; partial evidence retained.`);
}

const isMain = process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  if (process.platform !== 'darwin') throw new Error('This operation requires macOS; local unit tests do not.');
  if (process.argv[2] === 'preserve') preserve();
  else if (process.argv[2] === 'capture') await capture();
  else throw new Error('Usage: node tools/simulator-proof/capture.mjs preserve|capture');
}
