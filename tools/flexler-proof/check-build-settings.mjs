import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export function validateBuildSettings(document, configuration, profile) {
  if (!['Debug', 'Release'].includes(configuration) || !['host', 'aot-trim'].includes(profile))
    throw new Error('Select an explicit configuration and runtime profile.');
  if (profile === 'aot-trim' && configuration !== 'Release')
    throw new Error('The aot-trim diagnostic requires Release.');
  const properties = document?.Properties;
  if (!properties || properties.Configuration !== configuration)
    throw new Error('Evaluated configuration does not match the requested proof.');
  if (properties.MauiXamlInflator !== 'SourceGen')
    throw new Error('The proof must preserve the public app SourceGen inflator.');
  if (profile === 'aot-trim') {
    const expected = {
      UseInterpreter: 'false', MtouchInterpreter: '', TrimMode: 'partial',
      UseMonoRuntime: 'true', PublishAot: 'false', MtouchUseLlvm: 'true', Registrar: 'managed-static',
    };
    for (const [name, value] of Object.entries(expected)) {
      if (typeof properties[name] !== 'string' || properties[name].toLowerCase() !== value)
        throw new Error(`Strict proof property ${name} does not match the reviewed profile.`);
    }
  }
  return { configuration, runtimeProfile: profile, verified: true };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const document = JSON.parse(readFileSync(process.argv[2], 'utf8'));
  console.log(JSON.stringify(validateBuildSettings(document,
    process.env.FLEXLER_PROOF_CONFIGURATION, process.env.FLEXLER_PROOF_RUNTIME_PROFILE)));
}
