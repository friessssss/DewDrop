// Larger compositions: the dawn-meadow scene behind the banner and social preview, the grass divider, and the
// helpers diagrams use. Randomness is seeded so every build produces the same pixels.

import { Canvas } from './canvas.mjs';
import { P } from './palette.mjs';
import { logoMark } from './components.mjs';

export function rng(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

/** Dawn sky with a dithered gradient, a rising sun and a few clouds. */
export function sky(c, horizon, { seed = 7, sunX = 0.8, clouds = 4 } = {}) {
  c.gradient(0, 0, c.width, horizon, [P.skyTop, P.sky, P.skyLow, P.dawn]);
  // Sun with a dithered halo
  const sx = Math.round(c.width * sunX), sy = horizon - 4, r = Math.round(horizon * 0.22);
  for (let y = sy - r * 2; y <= sy + r; y++) {
    for (let x = sx - r * 2; x <= sx + r * 2; x++) {
      const d = Math.hypot(x - sx + 0.5, y - sy + 0.5);
      if (d < r) c.set(x, y, d < r * 0.65 ? P.sunCore : P.sun);
      else if (d < r * 1.5 && Canvas.dither(x, y, 1 - (d - r) / (r * 0.5))) c.set(x, y, P.dawn);
    }
  }
  const rand = rng(seed);
  for (let i = 0; i < clouds; i++) {
    const cx = Math.round(((i + 0.3 + rand() * 0.5) / clouds) * c.width);
    const cy = Math.round(horizon * (0.15 + rand() * 0.35));
    cloud(c, cx, cy, 10 + Math.round(rand() * 12), rand);
  }
}

/** A flat-bottomed pixel cloud made of overlapping puffs. */
export function cloud(c, cx, cy, size, rand = Math.random) {
  const puffs = [];
  const n = 3 + Math.round(size / 6);
  for (let i = 0; i < n; i++) {
    const t = i / (n - 1);
    const r = size * (0.35 + 0.35 * Math.sin(Math.PI * t)) * (0.8 + rand() * 0.3);
    puffs.push([cx - size + t * size * 2, cy - r * 0.3, r]);
  }
  const inside = (x, y) => y <= cy + 1 && puffs.some(([px, py, r]) => (x - px) ** 2 + (y - py) ** 2 <= r * r);
  for (let y = Math.floor(cy - size); y <= cy + 2; y++) {
    for (let x = Math.floor(cx - size * 2); x <= cx + size * 2; x++) {
      if (!inside(x, y)) continue;
      const shade = !inside(x, y + 2) || !inside(x + 2, y + 1);
      c.set(x, y, shade ? P.skyLow : P.white);
    }
  }
}

/** Rolling hills from a sum of sines, with a lit top edge and grass tufts. */
export function hills(c, baseY, amp, colors, { seed = 3, tufts = true, flowers = 0 } = {}) {
  const rand = rng(seed);
  const p1 = rand() * 6, p2 = rand() * 6, f1 = 0.012 + rand() * 0.01, f2 = 0.035 + rand() * 0.02;
  const top = [];
  for (let x = 0; x < c.width; x++) {
    top[x] = Math.round(baseY - amp * (0.6 * Math.sin(x * f1 + p1) + 0.4 * Math.sin(x * f2 + p2)));
  }
  const [edge, body, deep] = colors;
  for (let x = 0; x < c.width; x++) {
    for (let y = top[x]; y < c.height; y++) {
      const depth = y - top[x];
      let col = body;
      if (depth === 0) col = edge;
      else if (depth > 10 && Canvas.dither(x, y, Math.min(1, (depth - 10) / 14))) col = deep;
      c.set(x, y, col);
    }
  }
  if (tufts) {
    for (let x = 2; x < c.width - 2; x += 5 + Math.round(rand() * 6)) {
      const y = top[x];
      // A three-blade tuft growing out of the hill edge
      c.set(x, y - 1, edge);
      c.set(x, y - 2, edge);
      c.set(x - 1, y - 1, edge);
      c.set(x + 1, y - 1, edge);
      if (rand() < 0.5) c.set(x - 2, y - 2, edge);
      else c.set(x + 2, y - 2, edge);
    }
  }
  for (let i = 0; i < flowers; i++) {
    const x = 3 + Math.round(rand() * (c.width - 6));
    const y = top[x] + 3 + Math.round(rand() * 8);
    const petal = [P.berryLight, P.goldLight, P.white][i % 3];
    c.set(x, y - 1, petal); c.set(x - 1, y, petal); c.set(x + 1, y, petal); c.set(x, y + 1, petal);
    c.set(x, y, P.gold);
  }
  return top;
}

/** A four-point sparkle, like light catching a dew drop. */
export function sparkle(c, x, y, size = 2, color = P.white) {
  c.set(x, y, color);
  for (let i = 1; i <= size; i++) {
    c.set(x - i, y, color); c.set(x + i, y, color); c.set(x, y - i, color); c.set(x, y + i, color);
  }
}

/** The meadow scene: sky, sun, clouds, two hill layers, flowers and sparkles. Returns the near hill's top line. */
export function meadow(c, { horizon = Math.round(c.height * 0.62), seed = 11 } = {}) {
  sky(c, horizon + 6, { seed, sunX: 0.82, clouds: Math.max(3, Math.round(c.width / 110)) });
  hills(c, horizon, 6, [P.leafPale, P.leafLight, P.leaf], { seed: seed + 1, tufts: false });
  const top = hills(c, horizon + Math.round(c.height * 0.16), 5, [P.leafLight, P.leaf, P.leafDark], { seed: seed + 2, flowers: Math.round(c.width / 18) });
  const rand = rng(seed + 9);
  for (let i = 0; i < Math.round(c.width / 40); i++) {
    const x = 4 + Math.round(rand() * (c.width - 8));
    sparkle(c, x, top[x] + 4 + Math.round(rand() * 10), rand() < 0.3 ? 2 : 1, P.dewLight);
  }
  return top;
}

/** README / GitBook hero banner: the logo and wordmark over the meadow, with the tagline on a ribbon. */
export function banner({ width = 400, height = 120, title = "DewDrop", tagline = "" } = {}) {
  const c = new Canvas(width, height);
  meadow(c, { horizon: Math.round(height * 0.6) });
  const mark = logoMark(44);
  const titleScale = 3;
  const scratch = new Canvas(1, 1);
  const tw = scratch.text(title, -1000, -1000, P.white, { scale: titleScale, bold: true });
  const groupW = mark.width + 12 + tw;
  const gx = Math.round((width - groupW) / 2);
  const gy = Math.round(height * 0.1);
  c.blit(mark, gx, gy);
  c.text(title, gx + mark.width + 12, gy + 14, P.paperLight, { scale: titleScale, bold: true, outline: P.ink, shadow: P.dewDeep });
  if (tagline) {
    const tagW = scratch.text(tagline, -1000, -1000, P.ink);
    const rw = tagW + 14;
    ribbon(c, Math.round((width - rw) / 2), height - 26, rw, tagline);
  }
  return c;
}

/** A parchment ribbon with text, used for taglines and captions on scenes. (x, y) is the ribbon face. */
export function ribbon(c, x, y, w, text, { color = P.ink } = {}) {
  const h = 13, tail = 7;
  const layer = new Canvas(w + tail * 2, h + 3);
  // Tails sit lower than the face and have a notch cut into their outer ends
  for (const [tx, outer, inward] of [[0, 0, 1], [w + tail - 1 - 1, w + tail * 2 - 1, -1]]) {
    const ty = 3;
    const tw = tail + 2;
    const left = Math.min(tx, outer);
    layer.rect(left, ty, tw, h, P.paperShade);
    layer.box(left, ty, tw, h, P.ink);
    const my = ty + Math.floor(h / 2);
    for (let k = -2; k <= 2; k++) for (let d = 0; d < 3 - Math.abs(k); d++) layer.erase(outer + inward * d, my + k);
    for (let k = -2; k <= 2; k++) layer.set(outer + inward * (3 - Math.abs(k)), my + k, P.ink);
  }
  layer.roundBox(tail, 0, w, h, P.ink);
  layer.rect(tail + 1, 1, w - 2, h - 2, P.paperLight);
  layer.hline(tail + 1, h - 2, w - 2, P.paper);
  layer.text(text, tail + Math.round(w / 2), 3, color, { align: "center" });
  c.blit(layer, x - tail, y);
}

/** A strip of grass used as a section divider. */
export function divider(width = 400) {
  const c = new Canvas(width, 14);
  const rand = rng(21);
  hills(c, 8, 1.5, [P.leafLight, P.leaf, P.leafDark], { seed: 5, flowers: Math.round(width / 30) });
  for (let i = 0; i < width / 50; i++) {
    const x = 6 + Math.round(rand() * (width - 12));
    sparkle(c, x, 10, 1, P.dewLight);
  }
  // Fade the top edge into transparency so it sits on any background
  return c;
}

export { logoMark };
