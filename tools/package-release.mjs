#!/usr/bin/env node
// Builds DewDrop in Release and packages the two release downloads into artifacts/release/<version>/:
//
//   DewDrop-<version>.zip            the mod folder on its own (drag into Mods, or for mod managers)
//   DewDrop-<version>-installer.zip  the installer for Windows, macOS and Linux, with the mod bundled inside
//
// The installer zip is written by hand rather than with Compress-Archive so the macOS and Linux launchers keep
// their executable bit; without it, double-clicking them fails.
//
// Usage: node tools/package-release.mjs [--no-build]

import { spawnSync } from 'node:child_process';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = new Set(process.argv.slice(2));

const manifest = JSON.parse(fs.readFileSync(path.join(root, 'DewDrop.Mod/manifest.json'), 'utf8'));
const version = manifest.Version;
const csproj = fs.readFileSync(path.join(root, 'DewDrop.Mod/DewDrop.Mod.csproj'), 'utf8');
const csprojVersion = /<Version>([^<]+)<\/Version>/.exec(csproj)?.[1];
if (csprojVersion && csprojVersion !== version) {
  console.error(`manifest.json says ${version} but DewDrop.Mod.csproj says ${csprojVersion}; make them match first.`);
  process.exit(1);
}

if (!args.has('--no-build')) {
  console.log(`Building DewDrop ${version} (Release, no deploy)...`);
  const build = spawnSync('dotnet', ['build', 'DewDrop.Mod/DewDrop.Mod.csproj', '-c', 'Release', '-p:EnableModDeploy=false', '-nologo', '-v:q'], {
    cwd: root,
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
  if (build.status !== 0) process.exit(build.status ?? 1);
}

const modZip = path.join(root, 'DewDrop.Mod/bin/Release/net6.0', `DewDrop ${version}.zip`);
if (!fs.existsSync(modZip)) {
  console.error(`Missing ${path.relative(root, modZip)}. Build in Release first (or drop --no-build).`);
  process.exit(1);
}
const modZipBytes = fs.readFileSync(modZip);
// The zip is compressed, so check the DLL it was built from: Debug builds contain the debug bridge.
const builtDll = path.join(root, 'DewDrop.Mod/bin/Release/net6.0/DewDrop.dll');
if (!fs.existsSync(builtDll) || fs.statSync(builtDll).mtimeMs + 60_000 < fs.statSync(modZip).mtimeMs) {
  console.error(`Can't confirm ${path.relative(root, modZip)} is a Release build: ${path.relative(root, builtDll)} is missing or older than the zip. Rebuild.`);
  process.exit(1);
}
if (fs.readFileSync(builtDll).includes(Buffer.from('dewdrop-bridge', 'utf16le'))) {
  console.error('The Release DLL contains the debug bridge; package a Release build.');
  process.exit(1);
}

const out = path.join(root, 'artifacts/release', version);
fs.mkdirSync(out, { recursive: true });

// 1. The plain mod zip, exactly as ModBuildConfig made it (a top-level DewDrop/ folder).
const plainName = `DewDrop-${version}.zip`;
fs.writeFileSync(path.join(out, plainName), modZipBytes);

// 2. The installer zip.
const installerDir = path.join(root, 'installer');
const top = `DewDrop ${version} installer/`;
const text = (rel, eol) => {
  const body = fs.readFileSync(path.join(installerDir, rel), 'utf8').replace(/\r\n/g, '\n');
  return Buffer.from(eol === 'crlf' ? body.replace(/\n/g, '\r\n') : body, 'utf8');
};
const entries = [
  { name: top, dir: true },
  { name: top + 'Install on Windows.bat', data: text('Install on Windows.bat', 'crlf'), mode: 0o644 },
  { name: top + 'Install on macOS.command', data: text('Install on macOS.command', 'lf'), mode: 0o755 },
  { name: top + 'Install on Linux.sh', data: text('Install on Linux.sh', 'lf'), mode: 0o755 },
  { name: top + 'README.txt', data: text('README.txt', 'crlf'), mode: 0o644 },
  { name: top + 'internal/', dir: true },
  { name: top + 'internal/install.ps1', data: text('internal/install.ps1', 'crlf'), mode: 0o644 },
  { name: top + 'internal/install.sh', data: text('internal/install.sh', 'lf'), mode: 0o755 },
  { name: top + 'internal/DewDrop.zip', data: modZipBytes, mode: 0o644, store: true },
];
const ps1 = entries.find((e) => e.name.endsWith('install.ps1')).data;
if (ps1.some((b) => b > 127)) {
  console.error('installer/internal/install.ps1 must be plain ASCII (Windows PowerShell reads it as ANSI).');
  process.exit(1);
}
const installerName = `DewDrop-${version}-installer.zip`;
fs.writeFileSync(path.join(out, installerName), writeZip(entries));

for (const name of [plainName, installerName]) {
  const bytes = fs.readFileSync(path.join(out, name));
  const sha = crypto.createHash('sha256').update(bytes).digest('hex');
  console.log(`${path.relative(root, path.join(out, name))}  ${(bytes.length / 1024).toFixed(0)} KB  sha256 ${sha}`);
}

// ---------------------------------------------------------------------------------------------------------------

function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  }
  return ~c >>> 0;
}

function dosTime(d) {
  const time = (d.getHours() << 11) | (d.getMinutes() << 5) | (d.getSeconds() >> 1);
  const date = ((d.getFullYear() - 1980) << 9) | ((d.getMonth() + 1) << 5) | d.getDate();
  return { time, date };
}

/** Writes a zip whose entries carry Unix permissions ("version made by" = Unix), so launchers stay executable. */
function writeZip(list) {
  const { time, date } = dosTime(new Date());
  const locals = [];
  const centrals = [];
  let offset = 0;
  for (const e of list) {
    const name = Buffer.from(e.name, 'utf8');
    const raw = e.dir ? Buffer.alloc(0) : e.data;
    const method = e.dir || e.store ? 0 : 8;
    const body = method === 8 ? zlib.deflateRawSync(raw, { level: 9 }) : raw;
    const crc = e.dir ? 0 : crc32(raw);
    const mode = e.dir ? 0o40755 : 0o100000 | e.mode;

    const local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0);
    local.writeUInt16LE(20, 4);
    local.writeUInt16LE(0x0800, 6); // UTF-8 names
    local.writeUInt16LE(method, 8);
    local.writeUInt16LE(time, 10);
    local.writeUInt16LE(date, 12);
    local.writeUInt32LE(crc, 14);
    local.writeUInt32LE(body.length, 18);
    local.writeUInt32LE(raw.length, 22);
    local.writeUInt16LE(name.length, 26);
    local.writeUInt16LE(0, 28);
    locals.push(local, name, body);

    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0);
    central.writeUInt16LE((3 << 8) | 20, 4); // made by Unix, spec 2.0
    central.writeUInt16LE(20, 6);
    central.writeUInt16LE(0x0800, 8);
    central.writeUInt16LE(method, 10);
    central.writeUInt16LE(time, 12);
    central.writeUInt16LE(date, 14);
    central.writeUInt32LE(crc, 16);
    central.writeUInt32LE(body.length, 20);
    central.writeUInt32LE(raw.length, 24);
    central.writeUInt16LE(name.length, 28);
    central.writeUInt16LE(0, 30);
    central.writeUInt16LE(0, 32);
    central.writeUInt16LE(0, 34);
    central.writeUInt16LE(0, 36);
    central.writeUInt32LE(((mode << 16) | (e.dir ? 0x10 : 0)) >>> 0, 38);
    central.writeUInt32LE(offset, 42);
    centrals.push(central, name);

    offset += local.length + name.length + body.length;
  }
  const cd = Buffer.concat(centrals);
  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0);
  end.writeUInt16LE(list.length, 8);
  end.writeUInt16LE(list.length, 10);
  end.writeUInt32LE(cd.length, 12);
  end.writeUInt32LE(offset, 16);
  return Buffer.concat([...locals, cd, end]);
}
