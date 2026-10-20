#!/usr/bin/env node
// Builds every image used by the README and the GitBook into docs/.gitbook/assets/.
//
//   node tools/docs-art/build.mjs                 everything
//   node tools/docs-art/build.mjs --only=banner   one group (banner, logo, social, divider, icons, shots, tour, diagrams, terminal)
//
// Screenshots are framed from the raw captures in tools/docs-art/shots/ (taken in game with the debug bridge;
// see docs/contributing/debug-bridge.md). Everything else is drawn from the shared palette, font and components,
// which is what keeps the art consistent. Never hand-edit the generated PNGs: change this script and rebuild.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Canvas } from './lib/canvas.mjs';
import { P } from './lib/palette.mjs';
import { measure } from './lib/font.mjs';
import { encodeAPNG } from './lib/png.mjs';
import { ICON_NAMES, icon, logoMark, panel, plaque, card, arrow, framedShot, stackedNode, nodeHeight, trashCan } from './lib/components.mjs';
import { banner, meadow, ribbon, divider } from './lib/scenes.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');
const out = path.join(root, 'docs/.gitbook/assets');
const shotsDir = path.join(here, 'shots');
fs.mkdirSync(path.join(out, 'icons'), { recursive: true });

const only = process.argv.find((a) => a.startsWith('--only='))?.slice(7);
const want = (group) => !only || only.split(',').includes(group);
const save = (canvas, name, scale = 1) => {
  const img = canvas.save(path.join(out, name), scale);
  console.log(`  ${name}  ${img.width}x${img.height}`);
  return img;
};
const measureText = (text, opts = {}) => measure(text, opts) * (opts.scale ?? 1);

// ---------------------------------------------------------------------------------------------------------------
// Brand

if (want('banner')) {
  console.log('banner');
  save(banner({ tagline: 'The mod manager that lives inside Stardew Valley' }), 'banner.png', 3);
}

if (want('logo')) {
  console.log('logo');
  const mark = logoMark(44);
  const size = Math.max(mark.width, mark.height) + 6;
  const c = new Canvas(size, size);
  c.blit(mark, Math.floor((size - mark.width) / 2), Math.floor((size - mark.height) / 2));
  save(c, 'logo.png', 4);
  const small = new Canvas(32, 32);
  const m = logoMark(20);
  small.blit(m, Math.floor((32 - m.width) / 2), Math.floor((32 - m.height) / 2));
  save(small, 'logo-small.png', 2);
}

if (want('social')) {
  console.log('social');
  // GitHub's social preview: 1280x640, important content inside the middle 1200x600.
  const c = new Canvas(320, 160);
  meadow(c, { horizon: 100, seed: 23 });
  const mark = logoMark(52);
  const tw = measureText('DewDrop', { scale: 4, bold: true });
  const gx = Math.round((320 - (mark.width + 10 + tw)) / 2);
  c.blit(mark, gx, 22);
  c.text('DewDrop', gx + mark.width + 10, 34, P.paperLight, { scale: 4, bold: true, outline: P.ink, shadow: P.dewDeep });
  const tag = 'Browse, install and switch mods from the title screen';
  const w = measureText(tag) + 14;
  ribbon(c, Math.round((320 - w) / 2), 118, w, tag);
  save(c, 'social-preview.png', 4);
}

if (want('divider')) {
  console.log('divider');
  save(divider(400), 'divider.png', 3);
}

if (want('icons')) {
  console.log('icons');
  for (const name of ICON_NAMES) {
    const c = new Canvas(16, 16);
    c.blit(icon(name), 0, 0);
    c.save(path.join(out, 'icons', `${name}.png`), 4);
  }
  console.log(`  icons/*.png  ${ICON_NAMES.length} icons, 64x64`);
}

// ---------------------------------------------------------------------------------------------------------------
// Screenshots

const SHOTS = {
  // name: [raw capture, label, icon, crop [x, y, w, h] in the 1920x1080 capture or null for the whole screen]
  title: ['title.png', 'Title screen', 'window', null],
  installed: ['installed.png', 'Installed', 'installed', 'menu'],
  browse: ['browse.png', 'Browse', 'browse', 'menu'],
  'mod-page': ['mod-page.png', 'Mod page', 'browse', 'menu'],
  profiles: ['profiles.png', 'Profiles', 'profiles', 'menu'],
  downloads: ['downloads.png', 'Downloads', 'downloads', 'menu'],
  account: ['account.png', 'Account', 'account', 'menu'],
  'apply-restart': ['apply-restart.png', 'Apply & Restart', 'restart', 'menu'],
  'title-prompt': ['title-prompt.png', 'Load your profile?', 'restart', [400, 200, 1120, 680]],
};
const MENU_CROP = [140, 8, 1640, 964];

// Personal details to hide, in capture coordinates: [file, x, y, w, h]
const REDACT = [['account.png', 428, 296, 192, 40]];

// Small UI added after a capture was taken, drawn in at the spot the game draws it until that screenshot is captured
// again: [file, draw(capture)]. Delete an entry when you replace its capture.
const OVERLAYS = [
  // The Installed tab's trash cans: the game draws an 18x26 can at 2x, centered in a 44px column 12px in from each
  // row's right edge (x 1676). Rows are 64px tall every 76px from y 300; DewDrop's own row (the sixth) has none.
  ['installed.png', (c) => { for (const rowY of [300, 376, 452, 528, 604]) c.blit(trashCan(), 1624, rowY + 6, 2); }],
];

function loadShot(file) {
  const c = Canvas.fromPNG(path.join(shotsDir, file));
  for (const [f, x, y, w, h] of REDACT) if (f === file) pixelate(c, x, y, w, h, 10);
  for (const [f, draw] of OVERLAYS) if (f === file) draw(c);
  return c;
}

function pixelate(c, x, y, w, h, block) {
  for (let by = y; by < y + h; by += block) {
    for (let bx = x; bx < x + w; bx += block) {
      let r = 0, g = 0, b = 0, n = 0;
      for (let j = by; j < Math.min(by + block, y + h); j++) {
        for (let i = bx; i < Math.min(bx + block, x + w); i++) {
          const p = c.get(i, j); r += p[0]; g += p[1]; b += p[2]; n++;
        }
      }
      c.rect(bx, by, Math.min(block, x + w - bx), Math.min(block, y + h - by), [Math.round(r / n), Math.round(g / n), Math.round(b / n), 255]);
    }
  }
}

function croppedShot(name) {
  const [file, , , crop] = SHOTS[name];
  const raw = loadShot(file);
  if (!crop) return raw;
  const [x, y, w, h] = crop === 'menu' ? MENU_CROP : crop;
  return raw.crop(x, y, w, h);
}

if (want('shots')) {
  console.log('shots');
  for (const [name, [, label, iconName]] of Object.entries(SHOTS)) {
    const framed = framedShot(croppedShot(name), { width: 960, label: label.toUpperCase(), iconName });
    save(framed, `shot-${name}.png`);
  }
}

// ---------------------------------------------------------------------------------------------------------------
// Animated tour (title screen, then each tab), as an animated PNG

if (want('tour')) {
  console.log('tour');
  const W = 800, H = 450;
  const fit = (src) => {
    // Every frame shows the whole 16:9 capture, scaled down.
    const c = new Canvas(W, H, P.night);
    c.blitResized(src, 0, 0, W, H);
    return c;
  };
  const stills = ['title', 'installed', 'browse', 'mod-page', 'profiles', 'downloads', 'apply-restart'].map((n) => fit(loadShot(SHOTS[n][0])));
  const captions = ['Click Mods on the title screen', 'Turn mods on and off', 'Browse Nexus Mods', 'Open a mod and install a file', 'Keep separate profiles', 'Follow your downloads', 'Apply & Restart to play'];
  const withCaption = (c, text) => {
    const out = new Canvas(W, H);
    out.blit(c, 0, 0);
    const art = new Canvas(Math.ceil(W / 2), 22);
    const w = measureText(text) + 14;
    ribbon(art, Math.round((art.width - w) / 2), 4, w, text);
    out.blit(art, 0, H - 52, 2);
    return out;
  };
  const frames = [];
  const hold = (c, ms) => frames.push({ rgba: c.rgba, delay: ms });
  const slide = (a, b, steps = 6) => {
    for (let i = 1; i < steps; i++) {
      const t = i / steps;
      const e = t * t * (3 - 2 * t);
      const off = Math.round(e * W);
      const c = new Canvas(W, H, P.night);
      c.blit(a.crop(off, 0, W - off, H), 0, 0);
      c.blit(b.crop(0, 0, off, H), W - off, 0);
      frames.push({ rgba: c.rgba, delay: 45 });
    }
  };
  stills.forEach((s, i) => {
    const captioned = withCaption(s, captions[i]);
    hold(captioned, i === 0 ? 2200 : 2600);
    const next = stills[(i + 1) % stills.length];
    slide(captioned, withCaption(next, captions[(i + 1) % stills.length]));
  });
  const bytes = encodeAPNG(W, H, frames);
  fs.writeFileSync(path.join(out, 'demo-tour.png'), bytes);
  console.log(`  demo-tour.png  ${W}x${H}, ${frames.length} frames, ${(bytes.length / 1024 / 1024).toFixed(1)} MB`);
}

// ---------------------------------------------------------------------------------------------------------------
// Diagrams (drawn at 1 art pixel = 3 screen pixels on a parchment board)

/**
 * Draws a diagram: `draw(layer)` paints onto a transparent layer (content starts at y = 34, below the title
 * plaque) and returns the bottom of what it drew. The parchment board is then sized to fit.
 */
function diagram(file, width, title, draw) {
  const layer = new Canvas(width, 400);
  const bottom = draw(layer);
  const c = panel(width, bottom + 10, { fill: 'paper', border: 'wood' });
  const t = plaque(title, { scale: 1, padX: 8, padY: 3 });
  c.blit(t, Math.round((width - t.width) / 2), 8);
  c.blit(layer, 0, 0);
  save(c, file, 3);
}

/** A row of stacked nodes with arrows between them, all the same height. Returns the placed boxes. */
function flowRow(c, y, items, { left = 12, right = 12, gap = 14, arrowColor = P.woodDark, reverse = false } = {}) {
  const n = items.length;
  const width = Math.floor((c.width - left - right - gap * (n - 1)) / n);
  const h = Math.max(...items.map((it) => nodeHeight(it.title, it.body, width)));
  const order = reverse ? [...items].reverse() : items;
  const boxes = order.map((it, i) => {
    const x = left + i * (width + gap);
    c.blit(stackedNode(it.title, it.body, { width, iconName: it.icon, border: it.border ?? 'wood', minHeight: h }), x, y);
    return { x, y, w: width, h };
  });
  const placed = reverse ? [...boxes].reverse() : boxes;
  for (let i = 0; i < n - 1; i++) {
    const a = placed[i], b = placed[i + 1];
    const ay = a.y + 20;
    if (a.x < b.x) arrow(c, a.x + a.w + 1, ay, b.x - 2, ay, { color: arrowColor, head: 3 });
    else arrow(c, a.x - 2, ay, b.x + b.w + 1, ay, { color: arrowColor, head: 3 });
  }
  return placed;
}

function laneLabel(c, text, y) {
  c.text(text, 12, y, P.ink, { bold: true });
}

if (want('diagrams')) {
  console.log('diagrams');

  diagram('diagram-install-paths.png', 320, 'TWO WAYS TO INSTALL', (c) => {
    const a = card('Installer (easiest)', 'Download the installer zip, extract it, double-click "Install on Windows" (or macOS / Linux) and answer a few questions. It installs SMAPI for you if needed.', { width: 146, iconName: 'wrench', border: 'dew' });
    const b = card('Drag and drop', 'Install SMAPI yourself from smapi.io, download the DewDrop zip, then drag its DewDrop folder into your game\'s Mods folder.', { width: 146, iconName: 'folder', border: 'wood', minHeight: a.height });
    c.blit(a, 10, 34);
    c.blit(b, 164, 34);
    const bottom = 34 + Math.max(a.height, b.height);
    arrow(c, 83, bottom + 2, 83, bottom + 14, { color: P.dewMid, head: 3 });
    arrow(c, 237, bottom + 2, 237, bottom + 14, { color: P.woodDark, head: 3 });
    const end = plaque('START THE GAME, CLICK MODS', { scale: 1, border: 'leaf', fill: 'leaf', padX: 10, padY: 3 });
    c.blit(end, Math.round((320 - end.width) / 2), bottom + 16);
    return bottom + 16 + end.height;
  });

  diagram('diagram-installer-steps.png', 330, 'WHAT THE INSTALLER DOES', (c) => {
    const top = flowRow(c, 34, [
      { icon: 'browse', title: 'Find the game', body: 'Steam, GOG or Xbox app' },
      { icon: 'smapi', title: 'Check SMAPI', body: 'Installed and up to date?' },
      { icon: 'downloads', title: 'Install SMAPI', body: 'Official installer, only if needed' },
    ]);
    const y2 = top[0].y + top[0].h + 16;
    const bottom = flowRow(c, y2, [
      { icon: 'puzzle', title: 'Add DewDrop', body: 'Into Mods, keeps your settings' },
      { icon: 'gear', title: 'Launch setup', body: 'Steam option or a shortcut' },
      { icon: 'star', title: 'Play', body: 'Click Mods on the title screen', border: 'leaf' },
    ], { reverse: true });
    const last = top[2];
    arrow(c, last.x + Math.round(last.w / 2), last.y + last.h + 1, last.x + Math.round(last.w / 2), y2 - 2, { head: 3 });
    return y2 + bottom[0].h;
  });

  diagram('diagram-launching.png', 330, 'HOW A SESSION STARTS', (c) => {
    laneLabel(c, 'Starting the game yourself', 34);
    const a = flowRow(c, 46, [
      { icon: 'window', title: 'Steam or shortcut', body: 'Starts SMAPI as usual' },
      { icon: 'folder', title: 'Whole Mods folder', body: 'Every mod in it loads' },
      { icon: 'warning', title: 'Profile not loaded', body: '"Restart to load this profile"', border: 'gold' },
    ]);
    const y2 = a[0].y + a[0].h + 26;
    const b = flowRow(c, y2, [
      { icon: 'restart', title: 'Apply & Restart', body: 'DewDrop restarts the game', border: 'dew' },
      { icon: 'link', title: 'Selected Mods', body: 'Links to your profile\'s mods', border: 'dew' },
      { icon: 'check', title: 'Your profile', body: 'Only its mods load', border: 'leaf' },
    ], { arrowColor: P.dewMid });
    // From "Profile not loaded" down and across to Apply & Restart, above the lane-2 label
    const sx = a[2].x + Math.round(a[2].w / 2), ex = b[0].x + Math.round(b[0].w / 2);
    const fromY = a[2].y + a[2].h + 1, turnY = a[2].y + a[2].h + 7;
    c.rect(sx - 2, fromY, 5, turnY - fromY + 3, P.ink);
    c.rect(ex - 2, turnY - 2, sx - ex + 5, 5, P.ink);
    c.rect(sx - 1, fromY, 3, turnY - fromY + 2, P.gold);
    c.rect(ex - 1, turnY - 1, sx - ex + 3, 3, P.gold);
    c.text('Using Apply & Restart', ex + 10, y2 - 12, P.ink, { bold: true });
    arrow(c, ex, turnY, ex, y2 - 2, { color: P.gold, head: 3 });
    return y2 + b[0].h;
  });

  diagram('diagram-how-it-works.png', 340, 'HOW DEWDROP LOADS A PROFILE', (c) => {
    const row = flowRow(c, 34, [
      { icon: 'installed', title: 'Profile', body: 'The mods you turned on' },
      { icon: 'link', title: 'Selected Mods', body: 'One link per mod', border: 'dew' },
      { icon: 'smapi', title: 'SMAPI', body: 'Started with --mods-path' },
      { icon: 'star', title: 'Game', body: 'Loads just those mods', border: 'leaf' },
    ], { gap: 12 });
    const realY = row[0].y + row[0].h + 22;
    const real = card('Your Mods folder', 'The real mod files. DewDrop never moves or deletes them: turning a mod off only changes the profile.', { width: 220, iconName: 'folder', border: 'wood', fill: 'shade' });
    const rx = Math.round((340 - real.width) / 2);
    c.blit(real, rx, realY);
    const lx = row[1].x + Math.round(row[1].w / 2);
    for (let y = row[1].y + row[1].h + 2; y < realY - 1; y += 4) c.rect(lx - 1, y, 3, 2, P.dewMid);
    c.text('the links point here', lx + 6, realY - 13, P.dewDeep);
    return realY + real.height;
  });

  diagram('diagram-nexus-downloads.png', 330, 'DOWNLOADING FROM NEXUS MODS', (c) => {
    laneLabel(c, 'Premium account', 34);
    const a = flowRow(c, 46, [
      { icon: 'downloads', title: 'Install', body: 'On a mod\'s page', border: 'gold' },
      { icon: 'downloads', title: 'Downloads tab', body: 'Downloads straight away' },
      { icon: 'check', title: 'Installed', body: 'Turned on in your profile', border: 'leaf' },
    ]);
    const y2 = a[0].y + a[0].h + 24;
    laneLabel(c, 'Free account', y2 - 12);
    const b = flowRow(c, y2, [
      { icon: 'browse', title: 'Get on Nexus', body: 'Opens the file on the website' },
      { icon: 'window', title: 'Slow download', body: 'Click it on the website' },
      { icon: 'link', title: 'Mod manager link', body: 'Windows: DewDrop takes it from here', border: 'dew' },
    ], { arrowColor: P.dewMid });
    return y2 + b[0].h;
  });
}

// ---------------------------------------------------------------------------------------------------------------
// Installer terminal card (the real messages install.ps1 prints on a first-time Steam install)

if (want('terminal')) {
  console.log('terminal');
  const C = { cyan: P.dew, green: P.leafLight, grey: P.steel, white: P.paperLight, yellow: P.goldLight, dim: P.steelDark };
  const game = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Stardew Valley';
  const lines = [
    [[C.cyan, '  / \\  '], [C.white, ' DewDrop installer']],
    [[C.cyan, ' (   ) '], [C.grey, ' The mod manager that lives inside Stardew Valley']],
    [],
    [[C.cyan, '==> Looking for Stardew Valley']],
    [[C.green, '    [ok] '], [C.white, `Found it: ${game} (Steam)`]],
    [[C.green, '    [ok] '], [C.white, 'Stardew Valley 1.6.15']],
    [],
    [[C.cyan, '==> Checking SMAPI (the mod loader DewDrop runs on)']],
    [[C.grey, '         SMAPI isn\'t installed yet. I\'ll download the official SMAPI installer']],
    [[C.grey, '         from github.com/Pathoschild/SMAPI and run it for you.']],
    [[C.white, '    ? Install SMAPI 4.5.2 now? [Y/n] '], [C.dim, 'y']],
    [[C.grey, '         Downloading SMAPI-4.5.2-installer.zip (39.9 MB)...']],
    [[C.green, '    [ok] '], [C.white, 'SMAPI 4.5.2 is installed.']],
    [],
    [[C.cyan, '==> Installing DewDrop into your Mods folder']],
    [[C.green, '    [ok] '], [C.white, 'DewDrop 0.1.0 is in ...\\Stardew Valley\\Mods\\DewDrop']],
    [],
    [[C.cyan, '==> Setting up Steam to start the game with SMAPI']],
    [[C.white, '    ? Set it now? (Steam will close for a moment) [Y/n] '], [C.dim, 'y']],
    [[C.green, '    [ok] '], [C.white, 'Set the launch option (backup: localconfig.vdf.dewdrop-backup-...)']],
    [],
    [[C.green, '   All done!']],
    [[C.white, '   Start the game and click the new Mods button on the title screen.']],
    [[C.white, '    ? Start Stardew Valley now? [Y/n] '], [C.dim, 'y']],
  ];
  const lineH = 10;
  const textW = Math.max(...lines.map((segs) => segs.reduce((w, [, t]) => w + measureText(t) + 1, 0)));
  const w = textW + 28;
  const h = lines.length * lineH + 34;
  const c = panel(w, h, { fill: 'night', border: 'steel' });
  // Title bar
  c.rect(4, 4, w - 8, 11, P.steelDark);
  c.hline(4, 15, w - 8, P.ink);
  c.text('Install on Windows.bat', 10, 6, P.paperLight);
  for (const [i, col] of [P.berry, P.gold, P.leafLight].entries()) c.disc(w - 14 - i * 9, 9, 2.6, col);
  lines.forEach((segs, i) => {
    let x = 12;
    for (const [color, text] of segs) x += c.text(text, x, 22 + i * lineH, color) + 1;
  });
  save(c, 'installer-windows.png', 2);
}

console.log(`done -> ${path.relative(root, out)}`);
