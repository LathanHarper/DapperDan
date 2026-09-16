import test from 'node:test';
import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { parseProcessSnapshot, runDotnet } from './build-progress.mjs';

test('aggregates macOS tools with multi-core CPU, KiB RSS and cumulative CPU time', () => {
  assert.deepEqual(parseProcessSnapshot([
    '58.4 1024 80:05.23 /opt/dotnet/dotnet',
    '3.6 2048 00:02.77 dotnet',
    '220.5 4096 1-02:03:04.50 /tools/mono-aot-cross',
    '0.1 512 00:01 /usr/bin/clang++',
    '99.0 8888 99:59 private-app',
  ].join('\n')), [
    { executable: 'clang++', count: 1, cpuPercent: 0.1, rssKiB: 512, cpuSeconds: 1 },
    { executable: 'dotnet', count: 2, cpuPercent: 62, rssKiB: 3072, cpuSeconds: 4808 },
    { executable: 'mono-aot-cross', count: 1, cpuPercent: 220.5, rssKiB: 4096, cpuSeconds: 93784.5 },
  ]);
});

test('only exact basenames survive; paths, arguments and arbitrary process names never escape', () => {
  const secret = 'SYNTHETIC_PRIVATE_MARKER';
  const parsed = parseProcessSnapshot([
    `1.0 123 00:01 /Users/${secret}/dotnet`,
    `2.0 234 00:02 dotnet --password ${secret}`,
    `3.0 345 00:03 /usr/bin/python --pretend /tools/mono-aot-cross`,
    `4.0 456 00:04 ${secret}`,
    '5.0 567 00:05 dotnet-private',
    '6.0 678 00:06 /tools/clang\u001b[31m',
  ].join('\n'));
  assert.deepEqual(parsed, [{ executable: 'dotnet', count: 1, cpuPercent: 1, rssKiB: 123, cpuSeconds: 1 }]);
  assert.doesNotMatch(JSON.stringify(parsed), /PRIVATE|Users|password|python|pretend|tools/);
});

test('malformed counters and times are ignored without poisoning useful rows', () => {
  const parsed = parseProcessSnapshot([
    '-1.0 100 00:01 dotnet', 'NaN 100 00:01 dotnet', '1.0 -100 00:01 dotnet',
    '1.0 999999999999999999999 00:01 dotnet', '1.0 100 00:60 dotnet',
    '1.0 100 01:70:00 dotnet', '1.0 100 1-25:00:00 dotnet', '1.0 100 nonsense dotnet',
    '1.0 100 1-01:00 dotnet', '1,5 100 00:01 dotnet',
    '0.0 200 02:03:04.25 /usr/bin/ld',
  ].join('\n'));
  assert.deepEqual(parsed, [{ executable: 'ld', count: 1, cpuPercent: 0, rssKiB: 200, cpuSeconds: 7384.25 }]);
  assert.deepEqual(parseProcessSnapshot(null), []);
  assert.deepEqual(parseProcessSnapshot('x'.repeat(1024 * 1024 + 1)), []);
});

function runtime(overrides = {}) {
  const host = new EventEmitter();
  const stdout = [], stderr = [], signals = [], calls = [], cleared = [];
  host.platform = 'darwin';
  host.stdout = { write: value => stdout.push(value) };
  host.stderr = { write: value => stderr.push(value) };
  const child = new EventEmitter();
  child.kill = signal => { signals.push(signal); return true; };
  let tick, clock = 0;
  const timer = { unref() {} };
  return {
    host, child, stdout, stderr, signals, calls, cleared,
    tick: async () => { tick?.(); await new Promise(resolve => setImmediate(resolve)); },
    advance: value => { clock = value; },
    dependencies: {
      host, spawnProcess: (...args) => { calls.push(args); return child; },
      sampleProcesses: async () => parseProcessSnapshot('42.5 1024 02:03 /tools/mono-aot-cross'),
      now: () => clock,
      schedule: (callback, interval) => { assert.equal(interval, 60000); tick = callback; return timer; },
      unschedule: value => { cleared.push(value); },
      ...overrides,
    },
  };
}

test('forwards every argument unchanged, inherits stdio and preserves a nonzero build exit', async () => {
  const state = runtime();
  const args = ['build', 'source with spaces.csproj', '-p:Property=$literal;unchanged', '--no-restore'];
  const result = runDotnet(args, state.dependencies);
  assert.deepEqual(state.calls, [['dotnet', args, { stdio: 'inherit', shell: false }]]);
  state.advance(60123);
  await state.tick();
  assert.deepEqual(JSON.parse(state.stdout[0].slice('Build progress '.length)), {
    elapsedSeconds: 60, sampleAvailable: true,
    processes: [{ executable: 'mono-aot-cross', count: 1, cpuPercent: 42.5, rssKiB: 1024, cpuSeconds: 123 }],
  });
  state.child.emit('exit', 37, null);
  assert.equal(await result, 37);
  assert.equal(state.cleared.length, 1);
  assert.equal(state.host.listenerCount('SIGINT'), 0);
  await state.tick();
  assert.equal(state.stdout.length, 1);
});

test('forwards SIGINT and SIGTERM to dotnet, stops sampling and returns signal exit status', async () => {
  for (const [signal, expected] of [['SIGINT', 130], ['SIGTERM', 143]]) {
    const state = runtime();
    const result = runDotnet(['build'], state.dependencies);
    state.host.emit(signal);
    assert.deepEqual(state.signals, [signal]);
    assert.equal(state.cleared.length, 1);
    await state.tick();
    assert.deepEqual(state.stdout, []);
    state.child.emit('exit', null, signal);
    assert.equal(await result, expected);
    assert.equal(state.host.listenerCount('SIGTERM'), 0);
  }
});

test('a failed process sample cannot fail or retry the build, and never prints its raw error', async () => {
  const state = runtime({ sampleProcesses: async () => { throw new Error('/private/SYNTHETIC_PRIVATE_MARKER'); } });
  const result = runDotnet(['build'], state.dependencies);
  state.advance(60000);
  await state.tick();
  assert.deepEqual(JSON.parse(state.stdout[0].slice('Build progress '.length)),
    { elapsedSeconds: 60, sampleAvailable: false, processes: [] });
  state.child.emit('exit', 0, null);
  assert.equal(await result, 0);
  assert.equal(state.calls.length, 1);
  assert.deepEqual(state.stderr, []);
});

test('in-flight samples cannot print after the child exits', async () => {
  let finishSample;
  const state = runtime({ sampleProcesses: () => new Promise(resolve => { finishSample = resolve; }) });
  const result = runDotnet(['build'], state.dependencies);
  await state.tick();
  state.child.emit('exit', 0, null);
  assert.equal(await result, 0);
  finishSample([]);
  await new Promise(resolve => setImmediate(resolve));
  assert.deepEqual(state.stdout, []);
});

test('spawn failures return 127 without exposing error text or leaving monitoring active', async () => {
  const state = runtime();
  const result = runDotnet(['build'], state.dependencies);
  state.child.emit('error', new Error('/private/SYNTHETIC_PRIVATE_MARKER'));
  assert.equal(await result, 127);
  assert.deepEqual(state.stderr, ['Build progress: could not start dotnet.\n']);
  assert.equal(state.cleared.length, 1);
  assert.equal(state.host.listenerCount('SIGINT'), 0);
});

test('non-macOS hosts run dotnet normally without process sampling', async () => {
  const state = runtime({ schedule: () => { throw new Error('Must not schedule on this platform.'); } });
  state.host.platform = 'win32';
  const result = runDotnet(['--version'], state.dependencies);
  state.child.emit('exit', 0, null);
  assert.equal(await result, 0);
  assert.deepEqual(state.stdout, []);
});
