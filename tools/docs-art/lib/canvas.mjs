// A tiny RGBA pixel canvas. Everything is drawn at 1 art pixel per canvas pixel and scaled up with
// nearest-neighbour at the end, so edges stay crisp.

import { encodePNG, decodePNG } from './png.mjs';
import { rasterize, measure, LINE } from './font.mjs';
import { P } from './palette.mjs';
import fs from 'node:fs';

const BAYER4 = [
  [0, 8, 2, 10],
  [12, 4, 14, 6],
  [3, 11, 1, 9],
  [15, 7, 13, 5],
];

export class Canvas {
  constructor(width, height, fill = P.clear) {
    this.width = width;
    this.height = height;
    this.rgba = Buffer.alloc(width * height * 4);
    if (fill[3]) this.fill(fill);
  }

  static fromPNG(file) {
    const { width, height, rgba } = decodePNG(fs.readFileSync(file));
    const c = new Canvas(width, height);
    rgba.copy(c.rgba);
    return c;
  }

  /** Builds a sprite from string rows; each character maps through the legend ('.' and ' ' are clear). */
  static sprite(rows, legend) {
    const c = new Canvas(rows[0].length, rows.length);
    rows.forEach((row, y) => {
      if (row.length !== c.width) throw new Error(`sprite row ${y} is ${row.length} wide, expected ${c.width}`);
      [...row].forEach((ch, x) => {
        if (ch === '.' || ch === ' ') return;
        const color = legend[ch];
        if (!color) throw new Error(`sprite uses unknown key ${JSON.stringify(ch)}`);
        c.set(x, y, color);
      });
    });
    return c;
  }

  inside(x, y) {
    return x >= 0 && y >= 0 && x < this.width && y < this.height;
  }

  get(x, y) {
    if (!this.inside(x, y)) return P.clear;
    const i = (y * this.width + x) * 4;
    return [this.rgba[i], this.rgba[i + 1], this.rgba[i + 2], this.rgba[i + 3]];
  }

  /** Sets a pixel, alpha-blending when the colour is translucent. */
  set(x, y, c) {
    x |= 0;
    y |= 0;
    if (!this.inside(x, y) || !c[3]) return;
    const i = (y * this.width + x) * 4;
    if (c[3] === 255) {
      this.rgba[i] = c[0];
      this.rgba[i + 1] = c[1];
      this.rgba[i + 2] = c[2];
      this.rgba[i + 3] = 255;
      return;
    }
    const a = c[3] / 255;
    const da = this.rgba[i + 3] / 255;
    const oa = a + da * (1 - a);
    for (let k = 0; k < 3; k++) {
      this.rgba[i + k] = Math.round((c[k] * a + this.rgba[i + k] * da * (1 - a)) / (oa || 1));
    }
    this.rgba[i + 3] = Math.round(oa * 255);
  }

  /** Makes a pixel fully transparent. */
  erase(x, y) {
    if (!this.inside(x, y)) return;
    this.rgba.fill(0, (y * this.width + x) * 4, (y * this.width + x) * 4 + 4);
  }

  fill(c) {
    for (let y = 0; y < this.height; y++) for (let x = 0; x < this.width; x++) this.set(x, y, c);
    return this;
  }

  rect(x, y, w, h, c) {
    for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) this.set(i, j, c);
    return this;
  }

  hline(x, y, w, c) {
    return this.rect(x, y, w, 1, c);
  }

  vline(x, y, h, c) {
    return this.rect(x, y, 1, h, c);
  }

  /** 1px rectangle outline. */
  box(x, y, w, h, c) {
    this.hline(x, y, w, c);
    this.hline(x, y + h - 1, w, c);
    this.vline(x, y, h, c);
    this.vline(x + w - 1, y, h, c);
    return this;
  }

  /** Box with the four corner pixels left out, the classic pixel-art rounded corner. */
  roundBox(x, y, w, h, c) {
    this.hline(x + 1, y, w - 2, c);
    this.hline(x + 1, y + h - 1, w - 2, c);
    this.vline(x, y + 1, h - 2, c);
    this.vline(x + w - 1, y + 1, h - 2, c);
    return this;
  }

  line(x0, y0, x1, y1, c) {
    const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0);
    const sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
    let err = dx + dy;
    for (;;) {
      this.set(x0, y0, c);
      if (x0 === x1 && y0 === y1) break;
      const e2 = 2 * err;
      if (e2 >= dy) { err += dy; x0 += sx; }
      if (e2 <= dx) { err += dx; y0 += sy; }
    }
    return this;
  }

  disc(cx, cy, r, c) {
    for (let y = Math.floor(cy - r); y <= Math.ceil(cy + r); y++) {
      for (let x = Math.floor(cx - r); x <= Math.ceil(cx + r); x++) {
        if ((x - cx + 0.5) ** 2 + (y - cy + 0.5) ** 2 <= r * r) this.set(x, y, c);
      }
    }
    return this;
  }

  /** True when an ordered-dither threshold says to use the second colour for blend t in [0,1]. */
  static dither(x, y, t) {
    return t * 16 > BAYER4[y & 3][x & 3] + 0.5;
  }

  /** Vertical gradient through colour stops, dithered at each step so the bands read as pixel art. */
  gradient(x, y, w, h, stops) {
    for (let j = 0; j < h; j++) {
      const t = (j / Math.max(1, h - 1)) * (stops.length - 1);
      const k = Math.min(stops.length - 2, Math.floor(t));
      const f = t - k;
      for (let i = 0; i < w; i++) {
        this.set(x + i, y + j, Canvas.dither(x + i, y + j, f) ? stops[k + 1] : stops[k]);
      }
    }
    return this;
  }

  /** Draws another canvas on top (alpha-blended), optionally scaled by an integer factor. */
  blit(src, dx, dy, scale = 1) {
    for (let y = 0; y < src.height; y++) {
      for (let x = 0; x < src.width; x++) {
        const c = src.get(x, y);
        if (!c[3]) continue;
        if (scale === 1) this.set(dx + x, dy + y, c);
        else this.rect(dx + x * scale, dy + y * scale, scale, scale, c);
      }
    }
    return this;
  }

  /** Smooth (area-averaged) downscale into a region, for putting real screenshots into pixel frames. */
  blitResized(src, dx, dy, w, h) {
    const sx = src.width / w, sy = src.height / h;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        const x0 = Math.floor(x * sx), x1 = Math.max(x0 + 1, Math.floor((x + 1) * sx));
        const y0 = Math.floor(y * sy), y1 = Math.max(y0 + 1, Math.floor((y + 1) * sy));
        let r = 0, g = 0, b = 0, a = 0, n = 0;
        for (let yy = y0; yy < y1; yy++) {
          for (let xx = x0; xx < x1; xx++) {
            const i = (yy * src.width + xx) * 4;
            r += src.rgba[i]; g += src.rgba[i + 1]; b += src.rgba[i + 2]; a += src.rgba[i + 3]; n++;
          }
        }
        this.set(dx + x, dy + y, [Math.round(r / n), Math.round(g / n), Math.round(b / n), Math.round(a / n)]);
      }
    }
    return this;
  }

  crop(x, y, w, h) {
    const c = new Canvas(w, h);
    for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) c.set(i, j, this.get(x + i, y + j));
    return c;
  }

  flipX() {
    const c = new Canvas(this.width, this.height);
    for (let y = 0; y < this.height; y++) for (let x = 0; x < this.width; x++) c.set(this.width - 1 - x, y, this.get(x, y));
    return c;
  }

  /** Returns a copy with a 1px outline of the given colour around every opaque pixel (grows by 1 on each side). */
  outlined(c = P.ink, { diagonal = false } = {}) {
    const out = new Canvas(this.width + 2, this.height + 2);
    const n = diagonal
      ? [[-1, -1], [0, -1], [1, -1], [-1, 0], [1, 0], [-1, 1], [0, 1], [1, 1]]
      : [[0, -1], [-1, 0], [1, 0], [0, 1]];
    for (let y = -1; y <= this.height; y++) {
      for (let x = -1; x <= this.width; x++) {
        if (this.get(x, y)[3]) continue;
        if (n.some(([dx, dy]) => this.get(x + dx, y + dy)[3] > 128)) out.set(x + 1, y + 1, c);
      }
    }
    out.blit(this, 1, 1);
    return out;
  }

  /** Nearest-neighbour upscale. */
  scaled(n) {
    const c = new Canvas(this.width * n, this.height * n);
    c.blit(this, 0, 0, n);
    return c;
  }

  /**
   * Draws text. `scale` is how many canvas pixels make one font pixel. Options: bold, outline (colour),
   * shadow (colour), align ('left' | 'center' | 'right' around x).
   */
  text(str, x, y, color, { scale = 1, bold = false, outline = null, shadow = null, align = 'left', spacing = 1 } = {}) {
    const w = measure(str, { bold, spacing }) * scale;
    if (align === 'center') x -= Math.floor(w / 2);
    else if (align === 'right') x -= w;
    const layer = new Canvas(w + 4 * scale + 2, (LINE + 2) * scale + 2);
    rasterize(str, (px, py) => layer.rect(px * scale + scale, py * scale + scale, scale, scale, color), { bold, spacing });
    let drawn = layer;
    if (outline) {
      // Outline at the font's scale so a 2x heading gets a 2px border.
      drawn = layer;
      for (let k = 0; k < scale; k++) drawn = drawn.outlined(outline, { diagonal: k === 0 });
    }
    const off = outline ? scale : 0;
    if (shadow) {
      const sh = new Canvas(drawn.width, drawn.height);
      for (let j = 0; j < drawn.height; j++) for (let i = 0; i < drawn.width; i++) if (drawn.get(i, j)[3]) sh.set(i, j, shadow);
      this.blit(sh, x - scale - off + scale, y - scale - off + scale);
    }
    this.blit(drawn, x - scale - off, y - scale - off);
    return w;
  }

  save(file, scale = 1) {
    const img = scale === 1 ? this : this.scaled(scale);
    fs.writeFileSync(file, encodePNG(img.width, img.height, img.rgba));
    return img;
  }
}
