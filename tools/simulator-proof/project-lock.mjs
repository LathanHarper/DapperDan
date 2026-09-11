import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// A platform subset of the public checked-in lock, never a version resolution.
export function projectSimulatorLock(lock) {
  const entries = Object.entries(lock.dependencies).filter(([target]) => /^net10\.0-ios[\d.]+(?:\/iossimulator-arm64)?$/.test(target));
  if (entries.length !== 2 || !entries.some(([target]) => target.endsWith('/iossimulator-arm64'))) {
    throw new Error('Expected one iOS base target plus its Simulator lock section.');
  }
  return { ...lock, dependencies: Object.fromEntries(entries) };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const input = JSON.parse(readFileSync('src/DapperDan/packages.lock.json', 'utf8'));
  writeFileSync('artifacts/recovery/packages.iossimulator.lock.json', `${JSON.stringify(projectSimulatorLock(input), null, 2)}\n`);
}
