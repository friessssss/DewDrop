#!/usr/bin/env node
// Sets DewDrop's version in DewDrop.Mod/manifest.json and DewDrop.Mod/DewDrop.Mod.csproj together.
//
//   node tools/bump-version.mjs patch        0.1.0 -> 0.1.1
//   node tools/bump-version.mjs minor        0.1.0 -> 0.2.0
//   node tools/bump-version.mjs major        0.1.0 -> 1.0.0
//   node tools/bump-version.mjs 1.2.0-beta.1 any version higher than the current one
//
// Every release needs a higher version than the last: the check on pull requests into master refuses one that isn't.

import { readVersions, writeVersion, parse, compare } from './version.mjs';

const arg = process.argv[2];
const { manifest, csproj } = readVersions();
if (!arg) {
  console.log(`Current version: ${manifest} (manifest.json), ${csproj} (DewDrop.Mod.csproj)`);
  console.log('Usage: node tools/bump-version.mjs patch|minor|major|<x.y.z>');
  process.exit(0);
}

const current = parse(manifest);
if (!current) {
  console.error(`manifest.json's version "${manifest}" isn't x.y.z; fix it by hand first.`);
  process.exit(1);
}

let next;
if (arg === 'patch') next = `${current.major}.${current.minor}.${current.patch + (current.pre ? 0 : 1)}`;
else if (arg === 'minor') next = `${current.major}.${current.minor + 1}.0`;
else if (arg === 'major') next = `${current.major + 1}.0.0`;
else if (parse(arg)) next = arg;
else {
  console.error(`"${arg}" isn't patch, minor, major or a version like 1.2.0.`);
  process.exit(1);
}

// The same version is allowed only to bring the .csproj back in line with manifest.json
const syncing = next === manifest && csproj !== manifest;
if (compare(next, manifest) <= 0 && !syncing) {
  console.error(`${next} isn't higher than the current version, ${manifest}.`);
  process.exit(1);
}

writeVersion(next);
console.log(`${manifest} -> ${next} in DewDrop.Mod/manifest.json and DewDrop.Mod/DewDrop.Mod.csproj`);
