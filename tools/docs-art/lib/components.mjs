// The shared building blocks of the DewDrop docs art: the dew drop, leaves, wooden plaques, parchment panels,
// arrows and the icon set. Banners, diagrams and screenshot frames are all assembled from these.

import { Canvas } from './canvas.mjs';
import { P } from './palette.mjs';
import { measure, wrap, CAP } from './font.mjs';

// ---------------------------------------------------------------------------------------------------------------
// The dew drop (the logo)

/** A shaded dew drop `h` pixels tall (before its outline), lit from the upper left. */
export function dewDrop(h) {
  const w = Math.max(5, Math.round(h * 0.72)) | 1;
  const R = w / 2;
  const cy = h - R;
  const c = new Canvas(w, h);
  const half = (y) => (y >= cy ? Math.sqrt(Math.max(0, R * R - (y - cy) ** 2)) : R * Math.pow(Math.sin((y / cy) * Math.PI / 2), 1.6));
  for (let y = 0; y < h; y++) {
    const hw = half(y + 0.5);
    for (let x = 0; x < w; x++) {
      const dx = x + 0.5 - R;
      if (Math.abs(dx) > hw) continue;
      const nx = dx / R, ny = (y + 0.5 - cy) / R;
      const v = -(nx * 0.75 + ny * 0.55);
      const bands = [[0.45, P.dewLight], [-0.05, P.dew], [-0.6, P.dewMid], [-Infinity, P.dewDeep]];
      let col = P.dewDeep;
      for (let i = 0; i < bands.length; i++) {
        if (v > bands[i][0]) {
          col = bands[i][1];
          // Dither the lower edge of the band into the next one.
          const next = bands[i + 1];
          if (next && v - bands[i][0] < 0.12 && Canvas.dither(x, y, 0.5)) col = next[1];
          break;
        }
      }
      c.set(x, y, col);
    }
  }
  // Rim light along the lower left, inside the edge.
  for (let y = Math.floor(cy); y < h - 1; y++) {
    const hw = half(y + 0.5);
    const x = Math.ceil(R - hw);
    if (y > cy + R * 0.2) c.set(x, y, P.dew);
  }
  // Glint.
  const gx = R - R * 0.42, gy = cy - R * 0.38;
  const gr = Math.max(1, R * 0.2);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const ex = (x + 0.5 - gx) / gr, ey = (y + 0.5 - gy) / (gr * 1.5);
      if (ex * ex + ey * ey <= 1 && c.get(x, y)[3]) c.set(x, y, P.white);
    }
  }
  if (h >= 14) c.set(Math.round(gx + gr * 0.2), Math.round(gy + gr * 2.6), P.white);
  return c.outlined(P.ink);
}

/** A leaf pointing right, `len` pixels long, with a midrib. */
export function leaf(len, { flip = false } = {}) {
  const h = Math.max(5, Math.round(len * 0.45)) | 1;
  const c = new Canvas(len, h);
  const mid = (h - 1) / 2;
  for (let x = 0; x < len; x++) {
    const t = x / (len - 1);
    const hw = (h / 2) * Math.pow(Math.sin(Math.PI * Math.pow(t, 0.8)), 0.9);
    for (let y = 0; y < h; y++) {
      const d = y - mid;
      if (Math.abs(d) > hw) continue;
      let col = d < 0 ? P.leafLight : P.leaf;
      if (d > hw - 1.2) col = P.leafDark;
      if (d < -hw + 1.2 && t > 0.15 && t < 0.8) col = P.leafPale;
      c.set(x, y, col);
    }
  }
  for (let x = 1; x < len - 2; x++) c.set(x, Math.round(mid), P.leafDark);
  const out = c.outlined(P.ink);
  return flip ? out.flipX() : out;
}

/** The DewDrop mark: a drop resting on two leaves. `h` is the drop height. */
export function logoMark(h) {
  const drop = dewDrop(h);
  const l = leaf(Math.round(h * 0.75));
  const r = leaf(Math.round(h * 0.62), { flip: true });
  const w = drop.width + Math.round(h * 0.5);
  const c = new Canvas(w + 4, drop.height + Math.round(l.height * 0.6));
  const dx = Math.floor((c.width - drop.width) / 2);
  const baseY = drop.height - Math.round(l.height * 0.45);
  c.blit(r, 0, baseY + 1);
  c.blit(l.flipX().flipX(), c.width - l.width, baseY);
  c.blit(drop, dx, 0);
  return c;
}

// ---------------------------------------------------------------------------------------------------------------
// Panels and plaques

/** A wooden frame with a parchment inside, like a Stardew menu box. Border is 4px: ink, wood x2, ink. */
export function panel(w, h, { fill = 'paper', border = 'wood' } = {}) {
  const c = new Canvas(w, h);
  const fills = {
    paper: [P.paperLight, P.paper],
    shade: [P.paper, P.paperShade],
    dew: [P.dewLight, P.dew],
    night: [P.nightSoft, P.night],
    leaf: [P.leafPale, P.leafLight],
  };
  const borders = {
    wood: [P.woodLight, P.wood, P.woodDark],
    dew: [P.dew, P.dewMid, P.dewDeep],
    leaf: [P.leafLight, P.leaf, P.leafDark],
    berry: [P.berryLight, P.berry, P.woodDark],
    gold: [P.goldLight, P.gold, P.woodDark],
    steel: [P.steelLight, P.steel, P.steelDark],
  };
  const [hi, mid, lo] = borders[border];
  const [f1, f2] = fills[fill];
  // Ink outer edge with rounded corners
  c.roundBox(0, 0, w, h, P.ink);
  // Wood bevel
  c.rect(1, 1, w - 2, h - 2, mid);
  c.hline(2, 1, w - 4, hi);
  c.vline(1, 2, h - 4, hi);
  c.hline(2, h - 2, w - 4, lo);
  c.vline(w - 2, 2, h - 4, lo);
  // Inner ink line and fill
  c.box(3, 3, w - 6, h - 6, P.ink);
  c.rect(4, 4, w - 8, h - 8, f1);
  // Inset shading: the parchment sits a little below the frame on the top and left
  c.hline(4, 4, w - 8, f2);
  c.hline(4, 5, w - 8, f2);
  c.vline(4, 6, h - 10, f2);
  c.vline(5, 6, h - 10, f2);
  // Corner nails
  for (const [x, y] of [[2, 2], [w - 3, 2], [2, h - 3], [w - 3, h - 3]]) c.set(x, y, lo);
  return c;
}

/** A wooden sign with centred text: the section heading style used across the docs. */
export function plaque(text, { scale = 2, padX = 8, padY = 5, border = 'wood', fill = 'wood', color = P.paperLight, minWidth = 0 } = {}) {
  const tw = measure(text, { bold: true }) * scale;
  const w = Math.max(minWidth, tw + padX * 2 + 8);
  const h = CAP * scale + padY * 2 + 8;
  const c = new Canvas(w, h);
  const borders = {
    wood: [P.woodLight, P.wood, P.woodDark],
    dew: [P.dewLight, P.dewMid, P.dewDeep],
    leaf: [P.leafPale, P.leaf, P.leafDark],
    gold: [P.goldLight, P.gold, P.woodDark],
    berry: [P.berryLight, P.berry, P.woodDark],
  };
  const [hi, mid, lo] = borders[border];
  const faces = { wood: [P.woodLight, P.wood], dew: [P.dew, P.dewMid], leaf: [P.leafLight, P.leaf], paper: [P.paperLight, P.paper], gold: [P.goldLight, P.gold], berry: [P.berryLight, P.berry] };
  const [face1, face2] = faces[fill];
  c.roundBox(0, 0, w, h, P.ink);
  c.rect(1, 1, w - 2, h - 2, mid);
  c.hline(2, 1, w - 4, hi);
  c.vline(1, 2, h - 4, hi);
  c.hline(2, h - 2, w - 4, lo);
  c.vline(w - 2, 2, h - 4, lo);
  c.box(3, 3, w - 6, h - 6, lo);
  c.rect(4, 4, w - 8, h - 8, face1);
  c.rect(4, 4 + Math.floor((h - 8) / 2), w - 8, Math.ceil((h - 8) / 2), face2);
  // Wood grain
  if (fill === 'wood') {
    for (let y = 6; y < h - 6; y += 4) {
      for (let x = 6 + ((y * 7) % 11); x < w - 8; x += 13 + ((x + y) % 5)) c.hline(x, y, 4, lo);
    }
  }
  for (const [x, y] of [[2, 2], [w - 3, 2], [2, h - 3], [w - 3, h - 3]]) c.set(x, y, P.ink);
  c.text(text, Math.floor(w / 2), 4 + padY, color, { scale, bold: true, outline: P.ink, align: 'center' });
  return c;
}

/** A thick pixel arrow from (x0,y0) to (x1,y1), axis-aligned segments only. */
export function arrow(c, x0, y0, x1, y1, { color = P.woodDark, head = 4 } = {}) {
  const horizontal = y0 === y1;
  if (horizontal) {
    const dir = Math.sign(x1 - x0);
    const end = x1 - dir * head;
    const a = Math.min(x0, end), b = Math.max(x0, end);
    c.rect(a, y0 - 2, b - a + 1, 5, P.ink);
    c.rect(a, y0 - 1, b - a + 1, 3, color);
    for (let i = 0; i <= head; i++) {
      const x = end + dir * i;
      const hh = head + 1 - i;
      c.vline(x, y0 - hh - 1, hh * 2 + 3, P.ink);
    }
    for (let i = 0; i < head; i++) {
      const x = end + dir * i;
      const hh = head - i;
      c.vline(x, y0 - hh, hh * 2 + 1, color);
    }
  } else {
    const dir = Math.sign(y1 - y0);
    const end = y1 - dir * head;
    const a = Math.min(y0, end), b = Math.max(y0, end);
    c.rect(x0 - 2, a, 5, b - a + 1, P.ink);
    c.rect(x0 - 1, a, 3, b - a + 1, color);
    for (let i = 0; i <= head; i++) {
      const y = end + dir * i;
      const hh = head + 1 - i;
      c.hline(x0 - hh - 1, y, hh * 2 + 3, P.ink);
    }
    for (let i = 0; i < head; i++) {
      const y = end + dir * i;
      const hh = head - i;
      c.hline(x0 - hh, y, hh * 2 + 1, color);
    }
  }
  return c;
}

// ---------------------------------------------------------------------------------------------------------------
// Icons (16x16, ink outline included)

const L = {
  k: P.ink, s: P.inkSoft,
  W: P.woodDark, w: P.wood, l: P.woodLight,
  q: P.paperLight, p: P.paper, P: P.paperShade,
  D: P.dewDeep, m: P.dewMid, d: P.dew, L: P.dewLight, H: P.white,
  g: P.leafDark, G: P.leaf, e: P.leafLight, E: P.leafPale,
  r: P.berry, R: P.berryLight, y: P.gold, Y: P.goldLight,
  t: P.steelDark, T: P.steel, U: P.steelLight,
  n: P.night, N: P.nightSoft, u: P.plum,
};

const ICONS = {
  installed: [
    '................',
    '..kkkkkkkkkkkk..',
    '..kqqqqqqqqqpk..',
    '..kqkkkqqqqqpk..',
    '..kqkGkqssssPk..',
    '..kqkkkqqqqqpk..',
    '..kqqqqqqqqqpk..',
    '..kqkkkqqqqqpk..',
    '..kqkGkqsssqPk..',
    '..kqkkkqqqqqpk..',
    '..kqqqqqqqqqpk..',
    '..kqkkkqqqqqpk..',
    '..kqkqkqssssPk..',
    '..kqkkkqqqqqpk..',
    '..kPPPPPPPPPPk..',
    '..kkkkkkkkkkkk..',
  ],
  browse: [
    '................',
    '....kkkkk.......',
    '...kLLLddk......',
    '..kLHHLdddk.....',
    '..kLHLddddk.....',
    '..kLLdddddk.....',
    '..kddddddmk.....',
    '..kdddddmmk.....',
    '...kdddmmkk.....',
    '....kkkkkkWk....',
    '.........kWlWk..',
    '..........kWlWk.',
    '...........kWlWk',
    '............kWWk',
    '.............kk.',
    '................',
  ],
  profiles: [
    '................',
    '.........kkkk...',
    '........kqqqpk..',
    '........kqqqpk..',
    '...kkkk.kpppPk..',
    '..kqqqpk.kkkk...',
    '..kqqqpkkmmmmk..',
    '..kpppPkdddddmk.',
    '...kkkkkddddddk.',
    '..kGGGGGkddddmk.',
    '.keeeGGGGkdddmk.',
    '.keGGGGGGkkkkkk.',
    '.keGGGGGGgk.....',
    '.keGGGGGGgk.....',
    '.kkkkkkkkkk.....',
    '................',
  ],
  downloads: [
    '................',
    '......kkkk......',
    '......kddk......',
    '......kddk......',
    '......kdmk......',
    '...kkkkdmkkkk...',
    '...kLddddddmk...',
    '....kLddddmk....',
    '.kk..kLddmk..kk.',
    '.kWk..kLmk..kWk.',
    '.kWlk..kk..klWk.',
    '.kWllkkkkkkllWk.',
    '.kWlllllllllWWk.',
    '.kWWWWWWWWWWWWk.',
    '..kkkkkkkkkkkk..',
    '................',
  ],
  account: [
    '................',
    '.....kkkkkk.....',
    '....kqqqqqpk....',
    '...kqqqqqqqpk...',
    '...kqqqqqqqpk...',
    '...kqqqqqqqpk...',
    '...kpqqqqqpPk...',
    '....kpppppPk....',
    '.....kkkkkk.....',
    '...kkdddddmkk...',
    '..kLddddddddmk..',
    '.kLddddddddddmk.',
    '.kLdddddddddmmk.',
    '.kddddddddddmmk.',
    '.kkkkkkkkkkkkkk.',
    '................',
  ],
  restart: [
    '................',
    '.....kkkkkk.....',
    '...kkeeeeeekk...',
    '..keeGkkkkGeek..',
    '.keGkk....kkGk..',
    '.keGk......kk...',
    'keGk....kkkkkkk.',
    'keGk.....keeeGk.',
    'keGk......keGk..',
    'keGk.......kk...',
    '.keGk...........',
    '.keGkk....kk....',
    '..keGGkkkkGk....',
    '...kkGGGGGkk....',
    '.....kkkkkk.....',
    '................',
  ],
  folder: [
    '................',
    '................',
    '.kkkkkk.........',
    'kyYYYYyk........',
    'kyYYYYYkkkkkkkk.',
    'kyyyyyyyyyyyyyk.',
    'kkkkkkkkkkkkkkkk',
    'kYYYYYYYYYYYYYYk',
    'kYYYYYYYYYYYYYyk',
    'kYYYYYYYYYYYYYyk',
    'kYYYYYYYYYYYYYyk',
    'kYYYYYYYYYYYYyyk',
    'kyyyyyyyyyyyyyyk',
    'kkkkkkkkkkkkkkkk',
    '................',
    '................',
  ],
  warning: [
    '................',
    '.......kk.......',
    '......kYYk......',
    '......kYYk......',
    '.....kYkkYk.....',
    '.....kYkkYk.....',
    '....kYYkkYYk....',
    '....kYYkkYYk....',
    '...kYYYkkYYYk...',
    '...kYYYYYYYyk...',
    '..kYYYYkkYYyyk..',
    '..kYYYYkkYyyyk..',
    '.kYYYYYYYyyyyyk.',
    '.kkkkkkkkkkkkkk.',
    '................',
    '................',
  ],
  tip: [
    '................',
    '.....kkkkkk.....',
    '....kYYYYYyk....',
    '...kYqqYYYYyk...',
    '...kYqYYYYYyk...',
    '...kYYYYYYYyk...',
    '...kYYYYYYyyk...',
    '....kYYYYyyk....',
    '.....kyYyyk.....',
    '.....kkkkkk.....',
    '.....kUTTtk.....',
    '.....kkkkkk.....',
    '.....kUTTtk.....',
    '......kkkk......',
    '................',
    '................',
  ],
  info: [
    '................',
    '.....kkkkkk.....',
    '...kkLLLLddkk...',
    '..kLLLLkkdddmk..',
    '..kLLLLkkdddmk..',
    '.kLLLLLLddddmmk.',
    '.kLLLkkkkdddmmk.',
    '.kLLLLLkkdddmmk.',
    '.kLLLLLkkdddmmk.',
    '.kLLLLLkkdddmmk.',
    '..kdddkkkkdmmk..',
    '..kddddddddmmk..',
    '...kkddddmmkk...',
    '.....kkkkkk.....',
    '................',
    '................',
  ],
  danger: [
    '................',
    '.....kkkkkk.....',
    '...kkRRRRrrkk...',
    '..kRRRRkkrrrrk..',
    '..kRRRRkkrrrrk..',
    '.kRRRRRkkrrrrWk.',
    '.kRRRRRkkrrrrWk.',
    '.kRRRRRkkrrrrWk.',
    '.kRRRRRkkrrrrWk.',
    '.kRRRRrrrrrrrWk.',
    '..kRrrrkkrrrWk..',
    '..krrrrkkrrrWk..',
    '...kkrrrrrWkk...',
    '.....kkkkkk.....',
    '................',
    '................',
  ],
  terminal: [
    '................',
    '.kkkkkkkkkkkkkk.',
    '.kTTTTTTTTTTTtk.',
    '.kkkkkkkkkkkkkk.',
    '.knnnnnnnnnnnnk.',
    '.knLnnnnnnnnnnk.',
    '.knnLnnnnnnnnnk.',
    '.knnnLnnnnnnnnk.',
    '.knnLnnnnnnnnnk.',
    '.knLnnLLLLnnnnk.',
    '.knnnnnnnnnnnnk.',
    '.knnnnnnnnnnnnk.',
    '.kkkkkkkkkkkkkk.',
    '................',
    '................',
    '................',
  ],
  book: [
    '................',
    '..kkkkkkkkkkkk..',
    '.kmddddddddddk..',
    '.kmdLLLLLLLddk..',
    '.kmdddddddddLk..',
    '.kmdLLLLLLddLk..',
    '.kmdddddddddLk..',
    '.kmddddddddddk..',
    '.kmddddddddddk..',
    '.kmddddddddddk..',
    '.kmddddddddddk..',
    '.kmkkkkkkkkkkk..',
    '.kmkqqqqqqqqqk..',
    '.kmmkkkkkkkkkk..',
    '..kk............',
    '................',
  ],
  bug: [
    '................',
    '..k.........k...',
    '...k..kkk..k....',
    '....kkGGGkk.....',
    '...kGGeeGGGk....',
    '.kkkkkkkkkkkkk..',
    '...kGgeGGgeGk...',
    'kkkGGgeGGgeGGkkk',
    '...kGGGgeGGGk...',
    '.kkkGgeGGgeGkkk.',
    '...kGGGGGGGGk...',
    '.kkkkGGgeGGkkkk.',
    '....kkGGGGkk....',
    '......kkkk......',
    '................',
    '................',
  ],
  question: [
    '................',
    '..kkkkkkkkkkkk..',
    '.kqqqqqkkqqqqpk.',
    '.kqqqqkqqkqqqpk.',
    '.kqqqqqqqkqqqpk.',
    '.kqqqqqqkqqqqpk.',
    '.kqqqqqkqqqqqpk.',
    '.kqqqqqkqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqkqqqqqpk.',
    '.kpppppppppppPk.',
    '..kkkkkkpppkkk..',
    '.......kppk.....',
    '.......kpk......',
    '.......kk.......',
    '................',
  ],
  wrench: [
    '................',
    '..........kkk...',
    '.........kUUk.k.',
    '.........kTk.kTk',
    '.........kTkkTTk',
    '........kTTTTTk.',
    '.......kTTTTtk..',
    '......kTTTtkk...',
    '.....kTTTtk.....',
    '....kTTTtk......',
    '...kWWWtk.......',
    '..kWllWk........',
    '.kWllWk.........',
    '.kWlWk..........',
    '..kkk...........',
    '................',
  ],
  link: [
    '................',
    '................',
    '.........kkkk...',
    '........kTTTTk..',
    '.......kTkkkTtk.',
    '......kTk...kTk.',
    '....kkkTk..kTtk.',
    '...kTTTtkkkTtk..',
    '..kTtkkTTTTtk...',
    '.kTtk..kTkkk....',
    '.kTk...kTk......',
    '.ktTkkkTk.......',
    '..ktTTTk........',
    '...kkkk.........',
    '................',
    '................',
  ],
  smapi: [
    '................',
    '......kkkk......',
    '...kk.kTTk.kk...',
    '..kTTkkTTkkTtk..',
    '..kTTTTTTTTTtk..',
    '...kTTkkkkTtk...',
    '.kkkTk....kTkkk.',
    '.kTTTk.dd.kTTtk.',
    '.kTTTk.dm.kTttk.',
    '.kkkTk....kTkkk.',
    '...kTTkkkkTtk...',
    '..kTTTTTTTttk...',
    '..kTtkkTtkkttk..',
    '...kk.kTtk.kk...',
    '......kkkk......',
    '................',
  ],
  star: [
    '................',
    '.......kk.......',
    '......kYYk......',
    '......kYYk......',
    '.....kYYYyk.....',
    '.kkkkkYYYYkkkkk.',
    '.kYYYYYqYYYYYyk.',
    '..kYYYYYYYYYyk..',
    '...kYYYYYYYyk...',
    '....kYYYYYyk....',
    '....kYYYkYyk....',
    '...kYYYk.kyyk...',
    '...kYyk...kyyk..',
    '...kkk.....kkk..',
    '................',
    '................',
  ],
  heart: [
    '................',
    '................',
    '..kkkk...kkkk...',
    '.kRRrrk.kRrrrk..',
    'kRqRrrrkRrrrrrk.',
    'kRRrrrrrrrrrrrk.',
    'kRrrrrrrrrrrrrk.',
    'kRrrrrrrrrrrrWk.',
    '.krrrrrrrrrrWk..',
    '..krrrrrrrrWk...',
    '...krrrrrrWk....',
    '....krrrrWk.....',
    '.....krrWk......',
    '......kWk.......',
    '.......k........',
    '................',
  ],
  check: [
    '................',
    '................',
    '............kk..',
    '...........keGk.',
    '..........keGgk.',
    '.kk......keGgk..',
    'keGk....keGgk...',
    'kGeGk..keGgk....',
    '.kGeGkkeGgk.....',
    '..kGeGeGgk......',
    '...kGeGgk.......',
    '....kGgk........',
    '.....kk.........',
    '................',
    '................',
    '................',
  ],
  cross: [
    '................',
    '................',
    '..kk.......kk...',
    '.kRrk.....kRrk..',
    '.krrrk...krrrk..',
    '..krrrk.krrrk...',
    '...krrrkrrrk....',
    '....krrrrrk.....',
    '...krrrkrrrk....',
    '..krrrk.krrrk...',
    '.krrrk...krrrk..',
    '.krrk.....krrk..',
    '..kk.......kk...',
    '................',
    '................',
    '................',
  ],
  puzzle: [
    '................',
    '......kkk.......',
    '.....kGeGk......',
    '.kkkkkGGGkkkkk..',
    '.keeeeGGGGGGGk..',
    '.keGGGGGGGGGGkk.',
    '.keGGGGGGGGGGeGk',
    '.keGGGGGGGGGGGGk',
    '.keGGGGGGGGGGGgk',
    '.keGGGGGGGGGGkk.',
    '.keGGGGGGGGGgk..',
    '.keGGGGkkGGGgk..',
    '.kGGGGk..kGGgk..',
    '.kkkkkk..kkkkk..',
    '................',
    '................',
  ],
  gear: [
    '................',
    '......kkkk......',
    '...kk.kTTk.kk...',
    '..kTTkkTTkkTtk..',
    '..kTTTTTTTTTtk..',
    '...kTTkkkkTtk...',
    '.kkkTk....kTkkk.',
    '.kTTTk....kTTtk.',
    '.kTTTk....kTttk.',
    '.kkkTk....kTkkk.',
    '...kTTkkkkTtk...',
    '..kTTTTTTTttk...',
    '..kTtkkTtkkttk..',
    '...kk.kTtk.kk...',
    '......kkkk......',
    '................',
  ],
  key: [
    '................',
    '................',
    '..kkkk..........',
    '.kYYYyk.........',
    'kYYkkYyk........',
    'kYk..kYkkkkkkkk.',
    'kYk..kYYYYYYYYyk',
    'kYYkkYykkkykkyk.',
    '.kYYyyk..kyk.kk.',
    '..kkkk....k.....',
    '................',
    '................',
    '................',
    '................',
    '................',
    '................',
  ],
  window: [
    '................',
    '.kkkkkkkkkkkkkk.',
    '.kddddddddqkrkk.',
    '.kkkkkkkkkkkkkk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kqqqqqqqqqqqpk.',
    '.kPPPPPPPPPPPPk.',
    '.kkkkkkkkkkkkkk.',
    '................',
    '................',
    '................',
  ],
};

export const ICON_NAMES = Object.keys(ICONS);

/** An icon by name, trimmed to its pixels and centred on a 16x16 cell. */
export function icon(name) {
  const rows = ICONS[name];
  if (!rows) throw new Error(`no icon ${name}`);
  return Canvas.sprite(rows, L);
}

export const LEGEND = L;

/**
 * A metal trash can with its lid on, 18x26 like the one on the Installed tab (which the game draws at 2x). Drawn
 * here in the icon style, since the game's own sprite can't be used in the docs art.
 */
export function trashCan() {
  return Canvas.sprite([
    '......kkkkkk......',
    '.....kUUUTTtk.....',
    '.....kTkkkktk.....',
    '.kkkkkkkkkkkkkkkk.',
    'kUUUUUUUUUUUUUUUTk',
    'kUTTTTTTTTTTTTTTtk',
    'kkkkkkkkkkkkkkkkkk',
    '.kttttttttttttttk.',
    '.kUUUUUUUUUUUUTtk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kUTUtTUtTUtTTttk.',
    '.kttttttttttttttk.',
    '.kUUUUUUUUUUUUTtk.',
    '.kTTTTTTTTTTTTttk.',
    '.kttttttttttttttk.',
    '..kkkkkkkkkkkkkk..',
  ], L);
}

// ---------------------------------------------------------------------------------------------------------------
// Screenshot frames and diagram cards

/**
 * Puts a real screenshot inside a wooden frame with a labelled plaque, at output resolution. The frame is drawn
 * in art pixels and scaled by `px`, the screenshot is downscaled smoothly to `width` output pixels.
 */
export function framedShot(shot, { width = 960, label = '', iconName = null, px = 3 } = {}) {
  const innerW = width;
  const innerH = Math.round((shot.height / shot.width) * innerW);
  const border = 6; // art pixels
  const top = label ? 10 : 0; // art pixels of headroom for the plaque
  const fw = Math.ceil(innerW / px) + border * 2;
  const fh = Math.ceil(innerH / px) + border * 2;
  const art = new Canvas(fw, fh + top);
  art.blit(panel(fw, fh, { fill: 'night' }), 0, top);
  const out = art.scaled(px);
  const ix = border * px, iy = (top + border) * px;
  out.blitResized(shot, ix, iy, innerW, innerH);
  // The plaque goes on last so it overlaps the picture's top edge
  if (label) {
    const tag = plaque(label, { scale: 1, padX: iconName ? 16 : 6, padY: 3 });
    const layer = new Canvas(tag.width, tag.height);
    layer.blit(tag, 0, 0);
    if (iconName) layer.blit(icon(iconName), 5, Math.round((tag.height - 16) / 2));
    out.blit(layer, 8 * px, 0, px);
  }
  return out;
}

/** A parchment card with an icon, a bold title and wrapped body text, for diagrams. */
export function card(title, body = '', { width = 110, iconName = null, border = 'wood', fill = 'paper', titleColor = P.ink, minHeight = 0 } = {}) {
  const pad = 8;
  const textW = width - pad * 2 - (iconName ? 20 : 0);
  const titleLines = wrap(title, textW, { bold: true });
  const lines = body ? wrap(body, textW) : [];
  const titleH = titleLines.length * 10;
  const h = pad * 2 + titleH - 1 + (lines.length ? 3 + lines.length * 10 : 0) + 2;
  const c = panel(width, Math.max(h, iconName ? 36 : 0, minHeight), { fill, border });
  const tx = pad + (iconName ? 20 : 0);
  if (iconName) c.blit(icon(iconName), pad - 1, pad - 1);
  titleLines.forEach((line, i) => c.text(line, tx, pad + i * 10, titleColor, { bold: true }));
  lines.forEach((line, i) => c.text(line, tx, pad + titleH + 2 + i * 10, P.inkSoft));
  return c;
}

/** Height a stacked node would need, so a row of nodes can share one height. */
export function nodeHeight(title, body, width) {
  const inner = width - 12;
  return 8 + 16 + 4 + wrap(title, inner, { bold: true }).length * 10 + (body ? 2 + wrap(body, inner).length * 10 : 0) + 6;
}

/** A diagram node: icon on top, then a centred bold title and a centred grey description. */
export function stackedNode(title, body = '', { width = 90, iconName = null, border = 'wood', fill = 'paper', minHeight = 0 } = {}) {
  const inner = width - 12;
  const titleLines = wrap(title, inner, { bold: true });
  const lines = body ? wrap(body, inner) : [];
  const h = Math.max(minHeight, nodeHeight(title, body, width));
  const c = panel(width, h, { fill, border });
  const cx = Math.floor(width / 2);
  let y = 8;
  if (iconName) c.blit(icon(iconName), cx - 8, y);
  y += 20;
  titleLines.forEach((line) => { c.text(line, cx, y, P.ink, { bold: true, align: 'center' }); y += 10; });
  y += 2;
  lines.forEach((line) => { c.text(line, cx, y, P.inkSoft, { align: 'center' }); y += 10; });
  return c;
}
