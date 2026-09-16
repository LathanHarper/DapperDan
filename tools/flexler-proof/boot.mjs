import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { appendFileSync, copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createExceptionSanitizer } from './diagnostics.mjs';

const bundleId = 'net.codecrafty.dapperdan';
const executableName = 'CodeCrafty.DapperDan';
const delay = milliseconds => new Promise(done => setTimeout(done, milliseconds));
let operationDeadline = Infinity;

export function proofSettings(configuration, runtimeProfile = 'host') {
  if (!['Debug', 'Release'].includes(configuration)) throw new Error('Select the explicit Debug or Release proof configuration.');
  if (!['host', 'aot-trim'].includes(runtimeProfile)) throw new Error('Select the explicit host or aot-trim runtime profile.');
  if (runtimeProfile === 'aot-trim' && configuration !== 'Release') throw new Error('The aot-trim diagnostic requires Release.');
  return {
    configuration, runtimeProfile,
    outputDirectory: `src/DapperDan/bin/${configuration}/net10.0-ios/iossimulator-arm64`,
    interpreterSetting: runtimeProfile === 'aot-trim'
      ? 'Explicit Mono AOT; interpreter disabled; partial trimming; managed-static registrar'
      : configuration === 'Debug'
      ? 'MAUI Debug default; no workflow override'
      : 'Repository Release MtouchInterpreter=-all; no workflow override',
  };
}

export function selectIpad(inventory) {
  const runtimes = (inventory.runtimes ?? [])
    .filter(runtime => runtime.isAvailable === true && /^com\.apple\.CoreSimulator\.SimRuntime\.iOS-/.test(runtime.identifier))
    .sort((left, right) => right.version.localeCompare(left.version, undefined, { numeric: true }));
  for (const runtime of runtimes) {
    const devices = (inventory.devices?.[runtime.identifier] ?? [])
      .filter(device => device.isAvailable === true && /^iPad\b/.test(device.name)
        && /^[A-Fa-f0-9-]{36}$/.test(device.udid) && ['Shutdown', 'Booted'].includes(device.state))
      .sort((left, right) => left.name.localeCompare(right.name));
    if (devices.length) return { ...devices[0], runtime: runtime.identifier };
  }
  throw new Error('No stock available iPad on an installed iOS runtime; no device creation or runtime download.');
}

export function sanitizeRecord(record) {
  const safe = {};
  for (const key of ['seq', 'elapsedMs', 'hresult']) {
    if (Number.isSafeInteger(record[key])) safe[key] = record[key];
  }
  for (const key of ['kind', 'point', 'source']) {
    if (typeof record[key] === 'string' && /^[A-Za-z_][A-Za-z0-9_.`+-]{0,180}$/.test(record[key])) safe[key] = record[key];
  }
  for (const key of ['terminating', 'isDynamicCodeSupported', 'isDynamicCodeCompiled']) {
    if (typeof record[key] === 'boolean') safe[key] = record[key];
  }
  return safe; // Never print exception text, stacks, identities, paths or arbitrary journal fields.
}

export function evaluateJournal(records, alive, runtimeProfile = 'host') {
  const loaded = records.some(record => record.kind === 'checkpoint' && record.point === 'FlexlerPageLoaded');
  const viewModelReady = records.some(record => record.kind === 'checkpoint' && record.point === 'FlexlerViewModelReady');
  const exceptions = records.filter(record => ['exception', 'emergency-exception'].includes(record.kind));
  const launch = records.find(record => record.kind === 'launch');
  const runtimeConfirmed = runtimeProfile === 'host'
    || (runtimeProfile === 'aot-trim' && launch?.isDynamicCodeSupported === false && launch?.isDynamicCodeCompiled === false);
  return { loaded, viewModelReady, alive, exceptionCount: exceptions.length, runtimeConfirmed,
    passed: alive && loaded && viewModelReady && exceptions.length === 0 && runtimeConfirmed };
}

function publicExceptionSanitizer() {
  const paths = command('git', ['ls-files', '--', 'src/DapperDan'], 'Read public source catalog')
    .split('\n').filter(path => /\.(cs|xaml)$/.test(path));
  const sourceFiles = paths.map(path => ({ path, lineCount: readFileSync(path, 'utf8').split('\n').length }));
  const resourceKeys = paths.filter(path => /^src\/DapperDan\/Features\/Flexler\/Resources\/[^/]+\.xaml$/.test(path))
    .flatMap(path => [...readFileSync(path, 'utf8').matchAll(/\bx:Key="([^"]+)"/g)].map(match => match[1]));
  return createExceptionSanitizer({ sourceFiles, resourceKeys });
}

function command(program, args, label, timeout = 60000) {
  const remaining = operationDeadline - Date.now();
  if (remaining <= 0) throw new Error('Simulator diagnostic budget exhausted; preserving the current stage.');
  timeout = Math.min(timeout, remaining);
  const result = spawnSync(program, args, { encoding: 'utf8', timeout, maxBuffer: 4 * 1024 * 1024 });
  if (result.error || result.status !== 0) throw new Error(`${label} failed (exit ${result.status ?? 'none'}, ${result.error?.code ?? 'command'}).`);
  return result.stdout.trim();
}

function sealSimulator(app) {
  const files = readdirSync(app, { recursive: true })
    .filter(name => statSync(join(app, name)).isFile());
  const native = files.filter(name => command('file', ['-b', join(app, name)], 'Inspect local binary').includes('Mach-O'));
  for (const name of native.filter(name => name !== executableName).sort((a, b) => b.split('/').length - a.split('/').length)) {
    command('codesign', ['--force', '--sign', '-', '--timestamp=none', '--preserve-metadata=entitlements', join(app, name)], 'Seal nested Simulator binary');
  }
  command('codesign', ['--force', '--sign', '-', '--timestamp=none', '--preserve-metadata=entitlements', app], 'Seal Simulator app');
  command('codesign', ['--verify', '--deep', '--strict', app], 'Verify Simulator seals');
  return native.length;
}

function processAlive(pid) {
  const result = spawnSync('ps', ['-p', String(pid), '-o', 'comm='], { encoding: 'utf8', timeout: 10000 });
  return result.status === 0 && basename(result.stdout.trim()) === executableName;
}

function journalNames(directory) {
  return existsSync(directory) ? readdirSync(directory).filter(name => /^[A-Za-z0-9.-]+\.jsonl$/.test(name)) : [];
}

function readNewJournal(directory, baseline, destination) {
  const names = journalNames(directory).filter(name => !baseline.has(name));
  const records = [];
  for (const name of names) {
    const path = join(directory, name);
    if (statSync(path).size > 1024 * 1024) throw new Error('Unexpectedly large local launch journal.');
    const text = readFileSync(path, 'utf8');
    if (destination) copyFileSync(path, join(destination, name));
    const completeLines = text.split('\n');
    completeLines.pop(); // A concurrent writer may not yet have completed its final line.
    for (const line of completeLines.filter(line => line.trim())) records.push(JSON.parse(line));
  }
  if (new Set(records.map(record => record.launchId)).size > 1) throw new Error('Multiple new app launches; refusing ambiguous readiness evidence.');
  return records.sort((left, right) => left.seq - right.seq);
}

async function boot() {
  if (process.platform !== 'darwin' || process.env.GITHUB_REPOSITORY !== 'LathanHarper/DapperDan'
      || process.env.GITHUB_EVENT_NAME !== 'workflow_dispatch' || !process.env.RUNNER_TEMP)
    throw new Error('This boot helper only runs in the public manual macOS proof job.');
  const settings = proofSettings(process.env.FLEXLER_PROOF_CONFIGURATION, process.env.FLEXLER_PROOF_RUNTIME_PROFILE);
  const startedAt = Date.now();
  operationDeadline = startedAt + 9 * 60 * 1000;
  const report = {
    scope: `Flexler ${settings.configuration} Simulator startup`, configuration: settings.configuration,
    runtimeProfile: settings.runtimeProfile,
    retainedArtifacts: 0, interpreterSetting: settings.interpreterSetting, stage: 'inspect-app', passed: false,
  };
  const stage = name => {
    report.stage = name;
    console.log(`Flexler stage ${JSON.stringify({ stage: name, elapsedMs: Date.now() - startedAt })}`);
  };
  const local = join(process.env.RUNNER_TEMP, 'flexler-proof');
  mkdirSync(local, { recursive: true });
  let device, bootedHere = false, pid, journalDirectory, baseline, records = [];
  let sanitizeException = createExceptionSanitizer();
  try {
    stage('load-public-diagnostic-catalog');
    sanitizeException = publicExceptionSanitizer();
    stage('inspect-app');
    const output = resolve(settings.outputDirectory);
    const apps = readdirSync(output).filter(name => name.endsWith('.app'));
    if (apps.length !== 1) throw new Error(`Expected one freshly built ${settings.configuration} Simulator app.`);
    const app = join(output, apps[0]);
    if (command('/usr/libexec/PlistBuddy', ['-c', 'Print :CFBundleIdentifier', join(app, 'Info.plist')], 'Read bundle ID') !== bundleId)
      throw new Error('Unexpected app identity.');
    stage('ephemeral-simulator-seal');
    report.sealedNativeFiles = sealSimulator(app);
    report.binarySha256 = createHash('sha256').update(readFileSync(join(app, executableName))).digest('hex');
    stage('select-stock-ipad');
    device = selectIpad(JSON.parse(command('xcrun', ['simctl', 'list', '-j'], 'Read Simulator inventory')));
    report.device = device.name;
    report.runtime = device.runtime;
    stage('boot-stock-ipad');
    if (device.state === 'Shutdown') {
      command('xcrun', ['simctl', 'boot', device.udid], 'Boot selected iPad');
      bootedHere = true;
    }
    command('xcrun', ['simctl', 'bootstatus', device.udid, '-b'], 'Wait for iPad boot', 180000);
    stage('install');
    command('xcrun', ['simctl', 'install', device.udid, app], 'Install local Simulator app', 120000);
    const container = command('xcrun', ['simctl', 'get_app_container', device.udid, bundleId, 'data'], 'Locate local app container');
    journalDirectory = join(container, 'Library', 'Application Support', 'DapperDan', 'CrashJournal');
    baseline = new Set(journalNames(journalDirectory));
    stage('launch');
    const launch = command('xcrun', ['simctl', 'launch', device.udid, bundleId], 'Launch Flexler route');
    const match = launch.match(/^net\.codecrafty\.dapperdan:\s*(\d+)$/);
    if (!match) throw new Error('Launch did not return the expected application PID.');
    pid = Number(match[1]);
    stage('wait-for-flexler-loaded');
    const deadline = Math.min(Date.now() + 60000, operationDeadline);
    let loadedAt;
    while (Date.now() < deadline) {
      records = readNewJournal(journalDirectory, baseline);
      const state = evaluateJournal(records, processAlive(pid), settings.runtimeProfile);
      if (!state.alive) throw new Error('Simulator app process exited before the startup proof completed.');
      if (state.exceptionCount) throw new Error('The local launch journal recorded an exception.');
      if (records.some(record => record.kind === 'launch') && !state.runtimeConfirmed)
        throw new Error('Runtime dynamic-code flags do not match the strict proof.');
      if (state.loaded && state.viewModelReady) loadedAt ??= Date.now();
      if (loadedAt !== undefined && Date.now() - loadedAt >= 8000) {
        report.passed = state.passed;
        stage('loaded-and-alive');
        break;
      }
      await delay(1000);
    }
    if (!report.passed) throw new Error('No Flexler page and ViewModel readiness with eight seconds of process survival within 60 seconds.');
  } catch (error) {
    report.failure = error instanceof SyntaxError ? 'Malformed local JSON response.'
      : error.code ? `${report.stage} failed (${String(error.code).replace(/[^A-Z0-9_]/g, '')}).`
        : error.message;
    process.exitCode = 1;
  } finally {
    report.observationElapsedMs = Date.now() - startedAt;
    if (journalDirectory && baseline) {
      try { records = readNewJournal(journalDirectory, baseline, local); }
      catch { report.journalCaptureFailed = true; report.passed = false; process.exitCode = 1; }
    }
    report.journal = evaluateJournal(records, pid ? processAlive(pid) : false, settings.runtimeProfile);
    if (!report.journal.passed) { report.passed = false; process.exitCode = 1; }
    for (const record of records.slice(-256)) {
      const diagnostic = sanitizeException(record);
      console.log(`Flexler journal ${JSON.stringify({ ...sanitizeRecord(record), ...(diagnostic ? { diagnostic } : {}) })}`);
    }
    console.log(`Flexler proof ${JSON.stringify(report)}`);
    if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY,
      `\n### Flexler native startup: ${report.passed ? 'passed' : 'failed'}\n\n` +
      `\x60\x60\x60json\n${JSON.stringify(report, null, 2)}\n\x60\x60\x60\n\n` +
      `${settings.configuration} iPad Simulator. ${settings.interpreterSetting}. ` +
      'Ephemeral local ad-hoc seals only; no Apple credentials, profile or distributable release. No retained artifacts or cache. Debug startup does not prove Release AOT; neither Simulator configuration proves standalone Flexler device AOT, interactions, rendering or store screenshots.\n');
    if (pid && device) spawnSync('xcrun', ['simctl', 'terminate', device.udid, bundleId], { timeout: 15000, stdio: 'ignore' });
    if (bootedHere) spawnSync('xcrun', ['simctl', 'shutdown', device.udid], { timeout: 30000, stdio: 'ignore' });
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  boot().catch(() => { console.error('Flexler boot helper could not initialize.'); process.exitCode = 1; });
}
