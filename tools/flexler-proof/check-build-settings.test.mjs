import test from 'node:test';
import assert from 'node:assert/strict';
import { validateBuildSettings } from './check-build-settings.mjs';

const strict = () => ({ Properties: {
  Configuration: 'Release', MauiXamlInflator: 'SourceGen', UseInterpreter: 'False',
  MtouchInterpreter: '', TrimMode: 'partial', UseMonoRuntime: 'True', PublishAot: 'false',
  MtouchUseLlvm: 'true', Registrar: 'managed-static',
} });

test('strict Mono AOT profile accepts its exact evaluated settings', () => {
  assert.equal(validateBuildSettings(strict(), 'Release', 'aot-trim').verified, true);
});

test('host interpreter fallback and NativeAOT cannot masquerade as the strict proof', () => {
  for (const [property, value] of [
    ['UseInterpreter', 'true'], ['MtouchInterpreter', '-all'], ['TrimMode', 'copy'],
    ['UseMonoRuntime', 'false'], ['PublishAot', 'true'], ['Registrar', 'dynamic'],
    ['MtouchUseLlvm', 'false'], ['MtouchInterpreter', undefined],
  ]) {
    const settings = strict(); settings.Properties[property] = value;
    assert.throws(() => validateBuildSettings(settings, 'Release', 'aot-trim'), /Strict proof property/);
  }
});

test('wrong configuration, missing settings and changed XAML inflator fail before compilation', () => {
  assert.throws(() => validateBuildSettings(strict(), 'Debug', 'aot-trim'), /requires Release/);
  assert.throws(() => validateBuildSettings(strict(), 'Debug', 'host'), /configuration/);
  assert.throws(() => validateBuildSettings({}, 'Release', 'host'), /configuration/);
  const changed = strict(); changed.Properties.MauiXamlInflator = 'Runtime';
  assert.throws(() => validateBuildSettings(changed, 'Release', 'host'), /SourceGen/);
  assert.throws(() => validateBuildSettings(strict(), 'Release', 'unknown'), /explicit/);
});

test('host Debug keeps its own settings without claiming strict AOT parity', () => {
  const debug = { Properties: { Configuration: 'Debug', MauiXamlInflator: 'SourceGen',
    UseInterpreter: 'True', MtouchInterpreter: 'all', TrimMode: 'copy' } };
  assert.equal(validateBuildSettings(debug, 'Debug', 'host').runtimeProfile, 'host');
});
