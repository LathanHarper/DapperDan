import { spawn, spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { closeSync, cpSync, existsSync, mkdirSync, openSync, readFileSync, readdirSync, writeFileSync, copyFileSync, statSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { homedir } from 'node:os';
import { fileURLToPath } from 'node:url';

export const acceptedDimensions = {
  iphone: [[1242, 2688], [1284, 2778]],
  ipad: [[2048, 2732], [2064, 2752]],
};
export const panelCases = ['baseline', 'safe-area-none'];
const sourceRef = 'refs/heads/codex/panelboss-bottom-inset-canary';
const archiveName = 'DapperDan-Debug-iossimulator-arm64.tar.gz';
const appName = 'CodeCrafty.DapperDan.app';
const executableName = 'CodeCrafty.DapperDan';

export function validateMeasurements(value, expectedCase) {
  if (!panelCases.includes(expectedCase) || value?.schema !== 1 || value.case !== expectedCase || value.measurementError)
    throw new Error('Missing or wrong bottom-panel measurement case/schema.');
  const rectangle = (rect, label) => {
    if (!rect || !['x', 'y', 'width', 'height'].every(key => typeof rect[key] === 'number' && Number.isFinite(rect[key])) || rect.width <= 0 || rect.height <= 0)
      throw new Error(`Invalid positive finite ${label} bounds.`);
  };
  rectangle(value.panel, 'panel');
  if (!Array.isArray(value.buttons) || value.buttons.length !== 5)
    throw new Error('Expected exactly five measured buttons.');
  const ids = new Set();
  for (const button of value.buttons) {
    if (typeof button?.id !== 'string' || !button.id.trim() || ids.has(button.id))
      throw new Error('Measured button IDs must be unique nonempty strings.');
    ids.add(button.id);
    rectangle(button.button, 'button');
    rectangle(button.border, 'border');
  }
  if (!value.iosInsets || !['top', 'bottom', 'left', 'right'].every(key => typeof value.iosInsets[key] === 'number' && Number.isFinite(value.iosInsets[key]) && value.iosInsets[key] >= 0))
    throw new Error('Expected finite nonnegative iOS insets.');
  if (value.tongue !== undefined) rectangle(value.tongue, 'tongue');
  return value;
}

export function validateProductReceipt(receipt, expectedRunId) {
  if (receipt?.schema !== 2 || receipt.canary !== 'bottom-panel-v1' || receipt.repository !== 'LathanHarper/DapperDan' || receipt.workflow !== '.github/workflows/ios-unsigned.yml' || receipt.sourceRef !== sourceRef ||
      !/^[a-f0-9]{40}$/.test(receipt.sourceCommit) || !/^[1-9][0-9]{0,19}$/.test(receipt.runId) || !/^[1-9][0-9]*$/.test(receipt.runAttempt) || (expectedRunId && receipt.runId !== expectedRunId) ||
      receipt.bundleId !== 'net.codecrafty.dapperdan' || receipt.configuration !== 'Debug' || receipt.runtime !== 'iossimulator-arm64' ||
      receipt.archive !== archiveName || !/^[a-f0-9]{64}$/.test(receipt.sha256) || !/^[a-f0-9]{64}$/.test(receipt.binarySha256) ||
      !Number.isSafeInteger(receipt.archiveBytes) || receipt.archiveBytes <= 0 || JSON.stringify(receipt.cases) !== JSON.stringify(panelCases))
    throw new Error('Retained product is not the expected public bottom-panel Simulator canary.');
  return receipt;
}

export function validateArchivePaths(entries) {
  if (!entries.length || entries.some(entry => typeof entry !== 'string' || (entry !== appName && !entry.startsWith(`${appName}/`)) || entry.includes('\\') || entry.split('/').includes('..')))
    throw new Error('Unexpected archive paths; extraction refused.');
}

export function caseLaunchEnvironment(caseName, inherited = process.env) {
  if (!panelCases.includes(caseName)) throw new Error('Unknown bottom-panel case.');
  return { ...inherited, SIMCTL_CHILD_DAPPERDAN_PANEL_CASE: caseName };
}

export async function runPanelCases(captureCase, terminate) {
  for (const [index, caseName] of panelCases.entries()) {
    if (index > 0) await terminate();
    await captureCase(caseName);
  }
}

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
  if (!bytes.subarray(-12).equals(Buffer.from('0000000049454e44ae426082', 'hex'))) {
    throw new Error('PNG output is incomplete: final IEND chunk is missing.');
  }
  if (!acceptedDimensions[family].some(expected => expected.every((value, index) => value === size[index]))) {
    throw new Error(`Unexpected ${family} capture size ${size.join('x')}; native accepted dimensions required.`);
  }
  return size;
}

function command(program, args, timeout = 120000, env = process.env) {
  const result = spawnSync(program, args, { encoding: 'utf8', timeout, env, maxBuffer: 16 * 1024 * 1024 });
  if (result.error || result.status !== 0) {
    throw new Error(`${program} ${args.join(' ')} failed: ${result.error?.message ?? result.status}\n${result.stderr?.slice(-4000)}\n${result.stdout?.slice(-4000)}`);
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
  const archive = join(output, archiveName);
  command('tar', ['-czf', archive, '-C', appDirectory, apps[0]], 180000);
  copyFileSync('LICENSE', join(output, 'LICENSE.txt'));
  copyFileSync('THIRD-PARTY-NOTICES.md', join(output, 'THIRD-PARTY-NOTICES.md'));
  copyFileSync('THIRD-PARTY-LICENSES/Prism-9-package-notice.txt', join(output, 'PRISM-NOTICE.txt'));
  copyFileSync('THIRD-PARTY-LICENSES/DryIoc-MIT.txt', join(output, 'DRYIOC-LICENSE.txt'));
  copyFileSync('src/DapperDan/packages.lock.json', join(output, 'packages.lock.json'));
  saveJson(join(output, 'receipt.json'), {
    schema: 2, canary: 'bottom-panel-v1', cases: panelCases,
    repository: process.env.GITHUB_REPOSITORY, workflow: '.github/workflows/ios-unsigned.yml', sourceRef: process.env.GITHUB_REF,
    sourceCommit: process.env.GITHUB_SHA,
    runId: process.env.GITHUB_RUN_ID, runAttempt: process.env.GITHUB_RUN_ATTEMPT,
    configuration: 'Debug', runtime: 'iossimulator-arm64', bundleId,
    sdk: command('dotnet', ['--version']), workload: command('dotnet', ['workload', '--version']),
    xcode: command('xcodebuild', ['-version']),
    archive: basename(archive), archiveBytes: statSync(archive).size, sha256: sha256(archive),
    binarySha256: sha256(join(app, executableName)),
    disclaimer: 'Public generic canary only. Unsigned Simulator app: not a device IPA, App Store submission, or product screenshot. Requires a compatible Apple Silicon Mac Simulator. Preserve licenses; no framework extraction for development reuse.',
  });
  writeFileSync(process.env.GITHUB_ENV, `SIMULATOR_PRODUCT_DIRECTORY=${output}\nSIMULATOR_APP_DIRECTORY=${appDirectory}\nSIMULATOR_SOURCE_SHA=${process.env.GITHUB_SHA}\n`, { flag: 'a' });
  console.log(`Preserved ${basename(archive)} before capture (${statSync(archive).size} bytes).`);
}

function restore() {
  const retained = resolve('artifacts/reuse');
  const receipt = validateProductReceipt(JSON.parse(readFileSync(join(retained, 'receipt.json'), 'utf8')), process.env.REUSE_RUN_ID);
  const archive = join(retained, receipt.archive);
  if (statSync(archive).size !== receipt.archiveBytes || sha256(archive) !== receipt.sha256) throw new Error('Retained app SHA-256/size mismatch.');
  const output = resolve('artifacts/extracted');
  if (existsSync(output)) throw new Error('Reuse extraction destination must be new.');
  const entries = command('tar', ['-tzf', archive]).split('\n');
  validateArchivePaths(entries);
  mkdirSync(output, { recursive: true });
  // Reuse only this workflow's retained, hash-verified public product in a fresh directory.
  command('tar', ['-xzf', archive, '-C', output], 180000);
  if (sha256(join(output, appName, executableName)) !== receipt.binarySha256) throw new Error('Extracted executable SHA-256 mismatch.');
  writeFileSync(process.env.GITHUB_ENV, `SIMULATOR_PRODUCT_DIRECTORY=${retained}\nSIMULATOR_APP_DIRECTORY=${output}\nSIMULATOR_SOURCE_SHA=${receipt.sourceCommit}\n`, { flag: 'a' });
  mkdirSync('artifacts/captures', { recursive: true });
  saveJson('artifacts/captures/reused-product-receipt.json', receipt);
  console.log(`Reused hash-verified Simulator app from ${receipt.sourceCommit}; no .NET SDK, restore, or compilation required.`);
}

function signSimulator() {
  const retained = resolve(process.env.SIMULATOR_PRODUCT_DIRECTORY);
  const receipt = validateProductReceipt(JSON.parse(readFileSync(join(retained, 'receipt.json'), 'utf8')));
  const originalArchive = join(retained, receipt.archive);
  if (sha256(originalArchive) !== receipt.sha256) throw new Error('Original retained archive changed.');
  const original = resolve(process.env.SIMULATOR_APP_DIRECTORY, 'CodeCrafty.DapperDan.app');
  if (sha256(join(original, executableName)) !== receipt.binarySha256) throw new Error('Original executable does not match retained product.');
  const copiedRoot = resolve('artifacts/adhoc-app');
  if (existsSync(copiedRoot)) throw new Error('Ad-hoc copy destination must be new.');
  mkdirSync(copiedRoot, { recursive: true });
  const copied = join(copiedRoot, basename(original));
  cpSync(original, copied, { recursive: true, preserveTimestamps: true, dereference: false, verbatimSymlinks: true });
  const output = resolve('artifacts/adhoc-recovery');
  mkdirSync(output, { recursive: true });
  const files = readdirSync(copied, { recursive: true }).filter(name => statSync(join(copied, name)).isFile());
  const nativeFiles = files.filter(name => command('file', ['-b', join(copied, name)]).includes('Mach-O'));
  const managedFiles = files.filter(name => /\.(dll|json|xaml|db3|bin)$/.test(name));
  const dataHashes = Object.fromEntries(managedFiles.map(name => [name, sha256(join(copied, name))]));
  const changes = [];
  for (const name of nativeFiles.filter(name => name !== 'CodeCrafty.DapperDan').sort((a, b) => b.split('/').length - a.split('/').length)) {
    const path = join(copied, name);
    const before = sha256(path);
    command('codesign', ['--force', '--sign', '-', '--timestamp=none', '--preserve-metadata=entitlements', path]);
    command('codesign', ['--verify', '--strict', path]);
    changes.push({ path: name, before, after: sha256(path) });
  }
  const executable = join(copied, 'CodeCrafty.DapperDan');
  const executableBefore = sha256(executable);
  command('codesign', ['--force', '--sign', '-', '--timestamp=none', '--preserve-metadata=entitlements', copied]);
  command('codesign', ['--verify', '--deep', '--strict', copied]);
  changes.push({ path: 'CodeCrafty.DapperDan', before: executableBefore, after: sha256(executable) });
  for (const [name, expected] of Object.entries(dataHashes)) {
    if (sha256(join(copied, name)) !== expected) throw new Error(`Non-native content changed during signing: ${name}`);
  }
  if (sha256(originalArchive) !== receipt.sha256) throw new Error('Original archive changed during transformation.');
  const archive = join(output, 'DapperDan-Debug-iossimulator-arm64-adhoc.tar.gz');
  command('tar', ['-czf', archive, '-C', copiedRoot, basename(copied)], 180000);
  for (const name of ['LICENSE.txt', 'THIRD-PARTY-NOTICES.md', 'PRISM-NOTICE.txt', 'DRYIOC-LICENSE.txt']) copyFileSync(join(retained, name), join(output, name));
  saveJson(join(output, 'transformation-receipt.json'), {
    sourceCommit: receipt.sourceCommit, automationCommit: process.env.GITHUB_SHA, runId: process.env.GITHUB_RUN_ID,
    sourceArchiveSha256: receipt.sha256, originalArchiveUnchanged: true,
    canary: receipt.canary, cases: receipt.cases, binarySha256: sha256(executable),
    transformation: 'Fresh copy only. Sign nested Mach-O binaries inside-out, then app, using codesign --force --sign - --timestamp=none --preserve-metadata=entitlements. Verify every nested native file and app --deep --strict. No identity, certificate, profile, secret, SDK install, restore, compilation, or new entitlement.',
    nativeChanges: changes, unchangedManagedAndDataFiles: managedFiles.length,
    archive: basename(archive), archiveBytes: statSync(archive).size, sha256: sha256(archive),
  });
  writeFileSync(process.env.GITHUB_ENV, `SIMULATOR_APP_DIRECTORY=${copiedRoot}\n`, { flag: 'a' });
  console.log('Ad-hoc Simulator copy verified; original archive unchanged and transformed product preserved.');
}

function captureDiagnostics(udid, output, family, record) {
  const bundleId = process.env.BUNDLE_ID;
  try {
    const services = command('xcrun', ['simctl', 'spawn', udid, 'launchctl', 'list']);
    record.processRows = services.split('\n').filter(line => line.includes(bundleId));
    record.appProcessAlive = record.processRows.some(line => /^\d+\s/.test(line.trim()));
    writeFileSync(join(output, `${family}-process.txt`), `${record.processRows.join('\n')}\n`);
  } catch (error) { record.processCheckError = error.message; }
  try {
    const log = command('xcrun', ['simctl', 'spawn', udid, 'log', 'show', '--style', 'compact', '--last', '2m', '--predicate', `process == "CodeCrafty.DapperDan" OR eventMessage CONTAINS "${bundleId}"`], 60000);
    writeFileSync(join(output, `${family}-runtime.log`), log);
  } catch (error) { record.runtimeLogError = error.message; }
  try {
    const container = command('xcrun', ['simctl', 'get_app_container', udid, bundleId, 'data']);
    const journal = join(container, 'Library', 'Application Support', 'DapperDan', 'CrashJournal');
    record.journalPresent = existsSync(journal);
    if (record.journalPresent) {
      for (const name of readdirSync(journal).filter(name => name.endsWith('.jsonl'))) {
        copyFileSync(join(journal, name), join(output, `${family}-${name}`));
      }
    }
  } catch (error) { record.journalError = error.message; }
}

function captureMeasurements(udid, output, prefix, caseName, record) {
  const container = command('xcrun', ['simctl', 'get_app_container', udid, process.env.BUNDLE_ID, 'data']);
  const measurements = join(output, `${prefix}-bottom-panel-canary.json`);
  copyFileSync(join(container, 'Library', 'bottom-panel-canary.json'), measurements);
  record.measurements = basename(measurements);
  record.measurementsSha256 = sha256(measurements);
  const measured = validateMeasurements(JSON.parse(readFileSync(measurements, 'utf8')), caseName);
  record.iosInsets = measured.iosInsets;
  record.panelSafeAreaEdges = measured.panelSafeAreaEdges;
}

async function capture() {
  const output = resolve('artifacts/captures');
  mkdirSync(output, { recursive: true });
  const retained = resolve(process.env.SIMULATOR_PRODUCT_DIRECTORY);
  const original = validateProductReceipt(JSON.parse(readFileSync(join(retained, 'receipt.json'), 'utf8')));
  const prepared = JSON.parse(readFileSync('artifacts/adhoc-recovery/transformation-receipt.json', 'utf8'));
  if (prepared.sourceCommit !== original.sourceCommit || prepared.sourceArchiveSha256 !== original.sha256 || prepared.originalArchiveUnchanged !== true ||
      prepared.archive !== 'DapperDan-Debug-iossimulator-arm64-adhoc.tar.gz' ||
      sha256(join(retained, original.archive)) !== original.sha256 || sha256(join('artifacts/adhoc-recovery', prepared.archive)) !== prepared.sha256)
    throw new Error('Prepared capture product provenance mismatch.');
  const appDirectory = resolve(process.env.SIMULATOR_APP_DIRECTORY);
  const appNames = readdirSync(appDirectory).filter(name => name.endsWith('.app'));
  if (appNames.length !== 1) throw new Error('Expected exactly one reusable Simulator app.');
  const app = join(appDirectory, appNames[0]);
  const binarySha256 = sha256(join(app, executableName));
  if (binarySha256 !== prepared.binarySha256) throw new Error('Capture executable differs from prepared product.');
  command('codesign', ['--verify', '--deep', '--strict', app]);
  const signature = spawnSync('codesign', ['-dv', '--verbose=4', app], { encoding: 'utf8', timeout: 30000 });
  writeFileSync(join(output, 'app-signature.txt'), `status=${signature.status}\n${signature.stdout ?? ''}\n${signature.stderr ?? ''}\n`);
  const inventory = JSON.parse(command('xcrun', ['simctl', 'list', '-j']));
  const runtime = selectRuntime(inventory.runtimes);
  const evidence = {
    schema: 2, canary: 'bottom-panel-v1', cases: panelCases,
    sourceCommit: original.sourceCommit, sourceRunId: original.runId, sourceRunAttempt: original.runAttempt,
    automationCommit: process.env.GITHUB_SHA, runId: process.env.GITHUB_RUN_ID, runAttempt: process.env.GITHUB_RUN_ATTEMPT,
    sourceArchiveSha256: original.sha256, sourceBinarySha256: original.binarySha256,
    preparedArchiveSha256: prepared.sha256, binarySha256,
    runtime: runtime.identifier, devices: [], captures: [],
  };
  let failures = 0;
  for (const family of ['iphone', 'ipad']) {
    let udid;
    const device = { family, startedUtc: new Date().toISOString() };
    const start = Date.now();
    try {
      const type = selectDeviceType(inventory.devicetypes, family);
      device.deviceType = type.identifier;
      device.deviceName = type.name;
      udid = command('xcrun', ['simctl', 'create', `DapperDan capture ${family}`, type.identifier, runtime.identifier]);
      command('xcrun', ['simctl', 'boot', udid]);
      command('xcrun', ['simctl', 'bootstatus', udid, '-b'], 240000);
      command('xcrun', ['simctl', 'status_bar', udid, 'override', '--time', '9:41', '--batteryState', 'charged', '--batteryLevel', '100']);
      command('xcrun', ['simctl', 'ui', udid, 'appearance', 'light']);
      command('xcrun', ['simctl', 'install', udid, app], 120000);
      // One boot and install per family; the exact same binary serves both fresh processes.
      await runPanelCases(async caseName => {
        const prefix = `${family}-${caseName}`;
        const record = { family, case: caseName, deviceName: type.name, deviceType: type.identifier, binarySha256, startedUtc: new Date().toISOString() };
        const caseStart = Date.now();
        let runtimeLog;
        let runtimeLogFd;
        try {
          if (sha256(join(app, executableName)) !== binarySha256) throw new Error('Capture binary changed between cases.');
          runtimeLogFd = openSync(join(output, `${prefix}-launch-stream.log`), 'w');
          runtimeLog = spawn('xcrun', ['simctl', 'spawn', udid, 'log', 'stream', '--style', 'compact', '--level', 'debug', '--predicate', `process == "CodeCrafty.DapperDan" OR eventMessage CONTAINS "${process.env.BUNDLE_ID}" OR eventMessage CONTAINS "CodeCrafty.DapperDan"`], { stdio: ['ignore', runtimeLogFd, runtimeLogFd] });
          runtimeLog.on('error', error => { record.runtimeStreamError = error.message; });
          record.launch = command('xcrun', ['simctl', 'launch', udid, process.env.BUNDLE_ID], 60000, caseLaunchEnvironment(caseName));
          await new Promise(resolveWait => setTimeout(resolveWait, 12000));
          const image = join(output, `${prefix}.png`);
          if (existsSync(image)) throw new Error('Capture destination already exists; stale PNG refused.');
          try {
            command('xcrun', ['simctl', 'io', udid, 'screenshot', '--type=png', image], 60000);
          } catch (error) {
            if (!error.message.includes('ETIMEDOUT') || !existsSync(image)) throw error;
            validateDimensions(readFileSync(image), family);
            record.screenshotCommandWarning = error.message;
          }
          record.dimensions = validateDimensions(readFileSync(image), family);
          record.image = basename(image);
          record.sha256 = sha256(image);
          captureDiagnostics(udid, output, prefix, record);
          if (record.appProcessAlive !== true) throw new Error('App process did not remain running after launch. PNG retained for diagnosis, not app UI proof.');
          captureMeasurements(udid, output, prefix, caseName, record);
          if (sha256(join(app, executableName)) !== binarySha256) throw new Error('Capture binary changed during case.');
          record.status = 'captured-awaiting-human-visual-check';
          console.log(`${prefix}: ${type.name}, ${record.dimensions.join('x')}, live process and valid five-button geometry.`);
        } catch (error) {
          record.status = 'failed';
          record.error = error.message;
          throw error;
        } finally {
          if (runtimeLog) {
            runtimeLog.kill('SIGTERM');
            await new Promise(resolveWait => setTimeout(resolveWait, 300));
          }
          if (runtimeLogFd !== undefined) closeSync(runtimeLogFd);
          if (!record.processRows) captureDiagnostics(udid, output, prefix, record);
          if (!record.measurements) {
            try { captureMeasurements(udid, output, prefix, caseName, record); }
            catch (error) { record.measurementError = error.message; }
          }
          const reports = join(homedir(), 'Library', 'Logs', 'DiagnosticReports');
          if (existsSync(reports)) {
            for (const name of readdirSync(reports).filter(name => name.startsWith(executableName) && name.endsWith('.ips')))
              copyFileSync(join(reports, name), join(output, `${prefix}-${name}`));
          }
          record.seconds = Math.round((Date.now() - caseStart) / 1000);
          evidence.captures.push(record);
          saveJson(join(output, 'capture-receipt.json'), evidence);
        }
      }, () => command('xcrun', ['simctl', 'terminate', udid, process.env.BUNDLE_ID], 60000));
      device.status = 'both-cases-captured';
    } catch (error) {
      device.status = 'failed';
      device.error = error.message;
      failures++;
      console.error(`${family}: ${error.message}`);
    } finally {
      if (udid) {
        try { command('xcrun', ['simctl', 'shutdown', udid], 60000); }
        catch (error) { device.shutdownWarning = error.message; }
      }
      device.seconds = Math.round((Date.now() - start) / 1000);
      evidence.devices.push(device);
      saveJson(join(output, 'capture-receipt.json'), evidence);
    }
    // A launch failure is enough evidence: do not spend another cold boot on the same failure.
    if (failures) break;
  }
  if (failures) throw new Error(`${failures} capture family failed; partial evidence retained.`);
}

const isMain = process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  if (process.platform !== 'darwin') throw new Error('This operation requires macOS; local unit tests do not.');
  if (process.argv[2] === 'preserve') preserve();
  else if (process.argv[2] === 'restore') restore();
  else if (process.argv[2] === 'sign-simulator') signSimulator();
  else if (process.argv[2] === 'capture') await capture();
  else throw new Error('Usage: node tools/simulator-proof/capture.mjs preserve|restore|sign-simulator|capture');
}
