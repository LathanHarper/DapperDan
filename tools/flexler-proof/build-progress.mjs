import { execFile, spawn } from 'node:child_process';
import { constants } from 'node:os';
import { posix, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// Exact executable basenames only. Paths, arguments, PIDs and user names are never
// returned by the parser or printed by the heartbeat.
const executables = new Set([
  'dotnet', 'mono-aot-cross', 'llc', 'clang', 'clang++', 'ld', 'ld-classic',
  'actool', 'ibtool', 'metal', 'metallib', 'swift-frontend', 'codesign',
  'dsymutil', 'strip', 'lipo',
]);
const round = value => Math.round(value * 100) / 100;

function cpuSeconds(value) {
  // BSD ps TIME is commonly minutes:seconds.hundredths; accept hours and days too.
  const match = value.match(/^(?:(\d+)-)?(\d+):(\d{2})(?::(\d{2}))?(\.\d{1,6})?$/);
  if (!match) return null;
  const days = Number(match[1] ?? 0);
  const hasHours = match[4] !== undefined;
  const hours = hasHours ? Number(match[2]) : 0;
  const minutes = Number(hasHours ? match[3] : match[2]);
  const seconds = Number(hasHours ? match[4] : match[3]) + Number(match[5] ?? 0);
  if (seconds >= 60 || (hasHours && minutes >= 60)
      || (match[1] !== undefined && (!hasHours || hours >= 24))) return null;
  const total = days * 86400 + hours * 3600 + minutes * 60 + seconds;
  return Number.isFinite(total) && total >= 0 && total < Number.MAX_SAFE_INTEGER ? total : null;
}

/** Parse macOS `ps -ww -axo pcpu=,rss=,time=,comm=` without retaining raw text. */
export function parseProcessSnapshot(snapshot) {
  if (typeof snapshot !== 'string' || snapshot.length > 1024 * 1024) return [];
  const totals = new Map();
  for (const line of snapshot.split(/\r?\n/).slice(0, 10000)) {
    const match = line.match(/^\s*(\d+(?:\.\d+)?)\s+(\d+)\s+(\S+)\s+(\S+)\s*$/);
    if (!match || /[\x00-\x1f\x7f]/.test(match[4])) continue;
    const executable = posix.basename(match[4]);
    if (!executables.has(executable)) continue;
    const cpuPercent = Number(match[1]), rssKiB = Number(match[2]), time = cpuSeconds(match[3]);
    if (!Number.isFinite(cpuPercent) || cpuPercent > 1000000
        || !Number.isSafeInteger(rssKiB) || time === null) continue;
    const total = totals.get(executable) ?? { executable, count: 0, cpuPercent: 0, rssKiB: 0, cpuSeconds: 0 };
    if (!Number.isSafeInteger(total.rssKiB + rssKiB)
        || total.cpuSeconds + time >= Number.MAX_SAFE_INTEGER) continue;
    total.count++;
    total.cpuPercent += cpuPercent;
    total.rssKiB += rssKiB;
    total.cpuSeconds += time;
    totals.set(executable, total);
  }
  return [...totals.values()].sort((left, right) => left.executable.localeCompare(right.executable))
    .map(total => ({ ...total, cpuPercent: round(total.cpuPercent), cpuSeconds: round(total.cpuSeconds) }));
}

function sampleMacProcesses() {
  return new Promise(resolveSample => {
    execFile('/bin/ps', ['-ww', '-axo', 'pcpu=,rss=,time=,comm='], {
      encoding: 'utf8', timeout: 10000, maxBuffer: 1024 * 1024,
      env: { ...process.env, LC_ALL: 'C' },
    }, (error, stdout) => resolveSample(error ? null : parseProcessSnapshot(stdout)));
  });
}

/**
 * Run the fixed dotnet executable with unchanged arguments and inherited stdio.
 * Injectable runtime operations keep process/signal tests local and deterministic.
 * Sampling describes all allowlisted tools on this runner, not a process-tree
 * attribution. It adds neither build timeouts nor retries.
 */
export function runDotnet(args, {
  host = process, spawnProcess = spawn, sampleProcesses = sampleMacProcesses,
  now = () => performance.now(), schedule = setInterval, unschedule = clearInterval,
} = {}) {
  return new Promise(resolveExit => {
    const started = now();
    let child, timer, settled = false, monitoringStopped = false, sampling = false;
    const stopMonitoring = () => {
      monitoringStopped = true;
      if (timer !== undefined) { unschedule(timer); timer = undefined; }
    };
    const forward = signal => {
      stopMonitoring();
      try { child.kill(signal); } catch { /* The child may already have exited. */ }
    };
    const interrupt = () => forward('SIGINT');
    const terminate = () => forward('SIGTERM');
    const finish = (code, signal) => {
      if (settled) return;
      settled = true;
      stopMonitoring();
      host.removeListener('SIGINT', interrupt);
      host.removeListener('SIGTERM', terminate);
      resolveExit(Number.isInteger(code) ? code : signal ? 128 + (constants.signals[signal] ?? 1) : 1);
    };
    const heartbeat = async () => {
      if (monitoringStopped || sampling) return;
      sampling = true;
      let snapshot = null;
      try { snapshot = await sampleProcesses(); } catch { /* Sampling cannot fail the build. */ }
      finally { sampling = false; }
      if (monitoringStopped) return;
      host.stdout.write(`Build progress ${JSON.stringify({
        elapsedSeconds: Math.max(0, Math.floor((now() - started) / 1000)),
        sampleAvailable: snapshot !== null, processes: snapshot ?? [],
      })}\n`);
    };
    try {
      child = spawnProcess('dotnet', args, { stdio: 'inherit', shell: false });
    } catch {
      host.stderr.write('Build progress: could not start dotnet.\n');
      finish(127);
      return;
    }
    host.on('SIGINT', interrupt);
    host.on('SIGTERM', terminate);
    child.once('error', () => {
      host.stderr.write('Build progress: could not start dotnet.\n');
      finish(127);
    });
    child.once('exit', finish);
    if (host.platform === 'darwin') {
      timer = schedule(() => { void heartbeat(); }, 60000);
      timer.unref?.();
    }
  });
}

// Usage: node tools/flexler-proof/build-progress.mjs build <dotnet build arguments>
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url))
  process.exitCode = await runDotnet(process.argv.slice(2));
