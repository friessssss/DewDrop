// The DewDrop documentation palette. Every generated image draws only from these colours, which is what keeps
// the banner, icons, diagrams and screenshot frames looking like one set. See docs/contributing/docs-art.md.

const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16), 255];

export const P = {
  // Outline and text
  ink: hex('#2b1a12'),        // the 1px outline around everything, and dark text
  inkSoft: hex('#5a3a28'),    // secondary text, shadows on paper

  // Wood and parchment (frames, plaques, cards)
  woodDark: hex('#7a3f1d'),
  wood: hex('#b8652f'),
  woodLight: hex('#dd8f4a'),
  paperShade: hex('#f0c27a'),
  paper: hex('#fbdc9c'),
  paperLight: hex('#fff1cc'),

  // Dew (the brand colour)
  dewDeep: hex('#1f5d8f'),
  dewMid: hex('#2f8fc4'),
  dew: hex('#57c3e6'),
  dewLight: hex('#b6ecfa'),
  white: hex('#ffffff'),

  // Leaves and hills
  leafDark: hex('#2d6a34'),
  leaf: hex('#4f9a3c'),
  leafLight: hex('#86c95a'),
  leafPale: hex('#c4e88a'),

  // Dawn sky
  skyTop: hex('#6fb8e8'),
  sky: hex('#9fd7f2'),
  skyLow: hex('#cdeaf2'),
  dawn: hex('#ffd9a8'),
  sun: hex('#ffe58a'),
  sunCore: hex('#fff6c8'),

  // Metal (magnifier rims, keys, gears)
  steelDark: hex('#4f5d70'),
  steel: hex('#8fa1b5'),
  steelLight: hex('#d4dfea'),

  // Night (terminal backgrounds)
  night: hex('#1d2433'),
  nightSoft: hex('#2c3650'),

  // Signals (callouts, warnings)
  berry: hex('#d9455f'),
  berryLight: hex('#f28b9a'),
  gold: hex('#f5b52e'),
  goldLight: hex('#ffe066'),
  plum: hex('#6a4c93'),

  clear: [0, 0, 0, 0],
};

/** Colour as a CSS hex string, for the docs style guide. */
export const css = (c) => '#' + c.slice(0, 3).map((v) => v.toString(16).padStart(2, '0')).join('');
