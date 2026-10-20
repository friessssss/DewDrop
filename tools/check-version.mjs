#!/usr/bin/env node
// Checks DewDrop's version before a release, and prints it.
//
//   node tools/check-version.mjs               manifest.json and DewDrop.Mod.csproj agree, and the version is x.y.z
//   node tools/check-version.mjs --bumped      ...and it's higher than every release tag: pull requests into master
//   node tools/check-version.mjs --unreleased  ...and there's no tag for it yet: the release workflow, before publishing
//
// Release tags are the bare version (0.2.0), which SMAPI's update check compares; an older v0.2.0 tag counts too.
// Reads the tags from git, so CI checks out with fetch-depth: 0.

import { spawnSync } from 'node:child_process';
import { root, readVersions, parse, compare } from './version.mjs';

const bumped = process.argv.includes('--bumped');
const unreleased = process.argv.includes('--unreleased');

function fail(message) {
  // GitHub Actions shows ::error:: lines on the pull request; elsewhere it's just a message
  console.error(process.env.GITHUB_ACTIONS ? `::error::${message}` : message);
  process.exit(1);
}

const { manifest, csproj } = readVersions();
if (!manifest || !csproj) fail('Couldn\'t find the version in DewDrop.Mod/manifest.json or DewDrop.Mod/DewDrop.Mod.csproj.');
if (manifest !== csproj) fail(`manifest.json says ${manifest} but DewDrop.Mod.csproj says ${csproj}. Set both with: node tools/bump-version.mjs ${manifest}`);
if (!parse(manifest)) fail(`"${manifest}" isn't a version like 1.2.0 (or 1.2.0-beta.1).`);

if (bumped || unreleased) {
  const git = spawnSync('git', ['tag', '--list'], { cwd: root, encoding: 'utf8' });
  if (git.status !== 0) fail(`Couldn't list the git tags: ${git.stderr || git.error}`);
  const released = git.stdout.split(/\r?\n/).map((t) => t.trim().replace(/^v/, '')).filter((v) => parse(v));

  if (released.includes(manifest)) {
    fail(`${manifest} is already released (tag ${manifest}). Bump the version on development: node tools/bump-version.mjs patch|minor|major`);
  }

  if (bumped) {
    const latest = released.sort(compare).at(-1);
    if (latest && compare(manifest, latest) <= 0) {
      fail(`${manifest} isn't higher than the latest release, ${latest}. Bump the version on development: node tools/bump-version.mjs patch|minor|major`);
    }
    console.error(latest ? `${manifest} is higher than the latest release, ${latest}.` : `${manifest} will be the first release.`);
  }
}

console.log(manifest);
