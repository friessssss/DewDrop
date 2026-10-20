// Minimal PNG / APNG reader and writer with no dependencies (Node's zlib only).
// Reads 8-bit non-interlaced RGB, RGBA, grey, grey+alpha and palette images, which covers MonoGame screenshots.

import zlib from 'node:zlib';

const SIGNATURE = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

function ihdr(width, height) {
  const b = Buffer.alloc(13);
  b.writeUInt32BE(width, 0);
  b.writeUInt32BE(height, 4);
  b[8] = 8; // bit depth
  b[9] = 6; // RGBA
  b[10] = 0;
  b[11] = 0;
  b[12] = 0;
  return b;
}

/** Filters each row with the cheapest of the five PNG filters (sum of absolute values heuristic). */
function filterRows(width, height, rgba) {
  const stride = width * 4;
  const out = Buffer.alloc((stride + 1) * height);
  const candidate = Buffer.alloc(stride);
  for (let y = 0; y < height; y++) {
    const row = y * stride;
    const prev = y > 0 ? row - stride : -1;
    let bestType = 0;
    let bestScore = Infinity;
    let best = null;
    for (let type = 0; type < 5; type++) {
      let score = 0;
      for (let x = 0; x < stride; x++) {
        const cur = rgba[row + x];
        const a = x >= 4 ? rgba[row + x - 4] : 0;
        const b = prev >= 0 ? rgba[prev + x] : 0;
        const c = prev >= 0 && x >= 4 ? rgba[prev + x - 4] : 0;
        let v;
        switch (type) {
          case 0: v = cur; break;
          case 1: v = cur - a; break;
          case 2: v = cur - b; break;
          case 3: v = cur - ((a + b) >> 1); break;
          default: {
            const p = a + b - c;
            const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
            v = cur - (pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
          }
        }
        v &= 0xff;
        candidate[x] = v;
        score += v < 128 ? v : 256 - v;
        if (score >= bestScore) break;
      }
      if (score < bestScore) {
        bestScore = score;
        bestType = type;
        best = Buffer.from(candidate);
      }
    }
    out[y * (stride + 1)] = bestType;
    best.copy(out, y * (stride + 1) + 1);
  }
  return out;
}

/** Encodes an RGBA buffer as a PNG. */
export function encodePNG(width, height, rgba) {
  const idat = zlib.deflateSync(filterRows(width, height, rgba), { level: 9 });
  return Buffer.concat([SIGNATURE, chunk('IHDR', ihdr(width, height)), chunk('IDAT', idat), chunk('IEND', Buffer.alloc(0))]);
}

/**
 * Encodes an animated PNG. Every frame is full size; delays are in milliseconds.
 * Browsers, GitHub and GitBook show APNG as animation; anything older shows the first frame.
 */
export function encodeAPNG(width, height, frames, { loops = 0 } = {}) {
  const parts = [SIGNATURE, chunk('IHDR', ihdr(width, height))];
  const actl = Buffer.alloc(8);
  actl.writeUInt32BE(frames.length, 0);
  actl.writeUInt32BE(loops, 4);
  parts.push(chunk('acTL', actl));
  let seq = 0;
  frames.forEach((frame, i) => {
    const fctl = Buffer.alloc(26);
    fctl.writeUInt32BE(seq++, 0);
    fctl.writeUInt32BE(width, 4);
    fctl.writeUInt32BE(height, 8);
    fctl.writeUInt32BE(0, 12);
    fctl.writeUInt32BE(0, 16);
    fctl.writeUInt16BE(Math.round(frame.delay), 20);
    fctl.writeUInt16BE(1000, 22);
    fctl[24] = 0; // dispose: none
    fctl[25] = 0; // blend: source
    parts.push(chunk('fcTL', fctl));
    const data = zlib.deflateSync(filterRows(width, height, frame.rgba), { level: 9 });
    if (i === 0) {
      parts.push(chunk('IDAT', data));
    } else {
      const seqBuf = Buffer.alloc(4);
      seqBuf.writeUInt32BE(seq++);
      parts.push(chunk('fdAT', Buffer.concat([seqBuf, data])));
    }
  });
  parts.push(chunk('IEND', Buffer.alloc(0)));
  return Buffer.concat(parts);
}

/** Decodes a PNG into { width, height, rgba }. */
export function decodePNG(buf) {
  if (!buf.subarray(0, 8).equals(SIGNATURE)) throw new Error('not a PNG');
  let pos = 8;
  let width = 0, height = 0, depth = 0, colorType = 0, interlace = 0;
  let palette = null, trns = null;
  const idat = [];
  while (pos < buf.length) {
    const len = buf.readUInt32BE(pos);
    const type = buf.toString('ascii', pos + 4, pos + 8);
    const data = buf.subarray(pos + 8, pos + 8 + len);
    pos += 12 + len;
    if (type === 'IHDR') {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
      depth = data[8];
      colorType = data[9];
      interlace = data[12];
    } else if (type === 'PLTE') palette = data;
    else if (type === 'tRNS') trns = data;
    else if (type === 'IDAT') idat.push(data);
    else if (type === 'IEND') break;
  }
  if (depth !== 8 || interlace !== 0) throw new Error(`unsupported PNG (depth ${depth}, interlace ${interlace})`);
  const channels = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[colorType];
  if (!channels) throw new Error(`unsupported colour type ${colorType}`);
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = width * channels;
  const px = Buffer.alloc(stride * height);
  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)];
    const src = y * (stride + 1) + 1;
    const dst = y * stride;
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? px[dst + x - channels] : 0;
      const b = y > 0 ? px[dst - stride + x] : 0;
      const c = y > 0 && x >= channels ? px[dst - stride + x - channels] : 0;
      let v = raw[src + x];
      switch (filter) {
        case 1: v += a; break;
        case 2: v += b; break;
        case 3: v += (a + b) >> 1; break;
        case 4: {
          const p = a + b - c;
          const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
          v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
          break;
        }
      }
      px[dst + x] = v & 0xff;
    }
  }
  const rgba = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i++) {
    let r, g, b, a = 255;
    if (colorType === 6) [r, g, b, a] = [px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]];
    else if (colorType === 2) [r, g, b] = [px[i * 3], px[i * 3 + 1], px[i * 3 + 2]];
    else if (colorType === 0) r = g = b = px[i];
    else if (colorType === 4) { r = g = b = px[i * 2]; a = px[i * 2 + 1]; }
    else {
      const idx = px[i];
      [r, g, b] = [palette[idx * 3], palette[idx * 3 + 1], palette[idx * 3 + 2]];
      if (trns && idx < trns.length) a = trns[idx];
    }
    rgba.set([r, g, b, a], i * 4);
  }
  return { width, height, rgba };
}
