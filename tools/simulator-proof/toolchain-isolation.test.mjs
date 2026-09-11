import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = new URL('../../', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8').replace(/\r\n/g, '\n');
const iosPin = JSON.parse(read('.github/ios-global.json'));

// A CI job may already have copied the frozen iOS file over the working root.
// Assert the Android source pin against its committed source in that case.
const androidPin = JSON.parse(process.env.GITHUB_ACTIONS === 'true'
  ? execFileSync('git', ['show', 'HEAD:global.json'], { cwd: fileURLToPath(root), encoding: 'utf8' })
  : read('global.json'));

const exactPin = version => ({
  sdk: {
    version,
    rollForward: 'disable',
    allowPrerelease: false,
    workloadVersion: '10.0.302.1',
  },
});

test('local Android SDK matches Visual Studio without advancing the workload set', () => {
  assert.deepEqual(androidPin, exactPin('10.0.303'));
});

test('iOS retains its separate exact SDK and workload pin', () => {
  assert.deepEqual(iosPin, exactPin('10.0.302'));
  assert.equal(androidPin.sdk.workloadVersion, iosPin.sdk.workloadVersion);
});

test('every iOS workflow selects the frozen pin before SDK setup and dotnet execution', () => {
  const workflowNames = readdirSync(new URL('.github/workflows/', root))
    .filter(name => /\.ya?ml$/.test(name))
    .filter(name => /net10\.0-ios|maui-ios/.test(read(`.github/workflows/${name}`)));
  assert.deepEqual(workflowNames.sort(), ['ios-unsigned.yml', 'testflight.yml']);

  for (const name of workflowNames) {
    const workflow = read(`.github/workflows/${name}`);
    const steps = workflow.split(/(?=^      - name: )/m).slice(1);
    const copyIndex = steps.findIndex(step => /run: cp \.github\/ios-global\.json global\.json\s*$/.test(step));
    assert.ok(copyIndex >= 0, `${name} must select the frozen iOS pin.`);
    assert.doesNotMatch(steps[copyIndex], /^\s+if:/m, `${name} must select the pin unconditionally.`);
    assert.match(steps[copyIndex], /shell: bash/);
    assert.equal(steps.filter(step => /cp \.github\/ios-global\.json global\.json/.test(step)).length, 1);

    const checkoutIndex = steps.findIndex(step => /uses: actions\/checkout@/.test(step));
    const setupIndexes = steps.flatMap((step, index) => /uses: actions\/setup-dotnet@/.test(step) ? [index] : []);
    const commandIndexes = steps.flatMap((step, index) => /\bdotnet\s+(?:--|[a-z])/.test(step) ? [index] : []);
    assert.ok(checkoutIndex >= 0 && checkoutIndex < copyIndex, `${name} must check out source before selecting its pin.`);
    assert.ok(setupIndexes.length > 0 && commandIndexes.length > 0);
    assert.ok([...setupIndexes, ...commandIndexes].every(index => copyIndex < index), `${name} must select the iOS pin before setup or any dotnet command.`);
    for (const index of setupIndexes) assert.match(steps[index], /global-json-file: global\.json\s*$/m);

    assert.match(workflow, new RegExp(`WORKLOAD_VERSION: ${iosPin.sdk.workloadVersion.replaceAll('.', '\\.')}\\s*$`, 'm'));
    assert.ok(workflow.includes(`test "$(dotnet --version)" = "${iosPin.sdk.version}"`));
    assert.ok(workflow.includes('test "$(dotnet workload --version)" = "$WORKLOAD_VERSION"'));
    assert.match(workflow, /runs-on: macos-26\s*$/m);
    assert.match(workflow, /DEVELOPER_DIR: \/Applications\/Xcode_26\.6\.app\/Contents\/Developer\s*$/m);
  }
});
