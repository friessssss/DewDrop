// DewDrop's version lives in two places that must agree: DewDrop.Mod/manifest.json (what SMAPI and update checks read)
// and DewDrop.Mod/DewDrop.Mod.csproj (the build and the release zips' names). Shared by bump-version.mjs and
// check-version.mjs.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const manifestPath = path.join(root, 'DewDrop.Mod/manifest.json');
export const csprojPath = path.join(root, 'DewDrop.Mod/DewDrop.Mod.csproj');

const manifestVersion = /("Version"\s*:\s*")([^"]*)(")/;
const csprojVersion = /(<Version>)([^<]*)(<\/Version>)/;

/** x.y.z with an optional prerelease tag (x.y.z-beta.1), as SMAPI accepts. */
const semver = /^(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?$/;

export function readVersions() {
  const manifest = fs.readFileSync(manifestPath, 'utf8').match(manifestVersion)?.[2];
  const csproj = fs.readFileSync(csprojPath, 'utf8').match(csprojVersion)?.[2];
  return { manifest, csproj };
}

export function writeVersion(version) {
  for (const [file, pattern] of [[manifestPath, manifestVersion], [csprojPath, csprojVersion]]) {
    const text = fs.readFileSync(file, 'utf8');
    if (!pattern.test(text)) throw new Error(`No version found in ${path.relative(root, file)}.`);
    fs.writeFileSync(file, text.replace(pattern, `$1${version}$3`));
  }
}

export function parse(version) {
  const m = semver.exec(version ?? '');
  return m ? { major: +m[1], minor: +m[2], patch: +m[3], pre: m[4] ?? null } : null;
}

/** Negative when a < b, 0 when equal, positive when a > b. A prerelease comes before its release (1.0.0-beta < 1.0.0). */
export function compare(a, b) {
  const x = parse(a);
  const y = parse(b);
  for (const k of ['major', 'minor', 'patch']) {
    if (x[k] !== y[k]) return x[k] - y[k];
  }
  if (x.pre === y.pre) return 0;
  if (x.pre === null) return 1;
  if (y.pre === null) return -1;
  // Dot-separated identifiers: numbers compare as numbers and sort before words
  const xs = x.pre.split('.');
  const ys = y.pre.split('.');
  for (let i = 0; i < Math.max(xs.length, ys.length); i++) {
    if (xs[i] === undefined) return -1;
    if (ys[i] === undefined) return 1;
    const xn = /^\d+$/.test(xs[i]);
    const yn = /^\d+$/.test(ys[i]);
    if (xn && yn && +xs[i] !== +ys[i]) return +xs[i] - +ys[i];
    if (xn !== yn) return xn ? -1 : 1;
    if (xs[i] !== ys[i]) return xs[i] < ys[i] ? -1 : 1;
  }
  return 0;
}
