using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace DewDrop.Framework
{
    /// <summary>
    /// Builds the "Mods" title button at runtime out of the game's own title button art, so it matches the
    /// vanilla buttons (including texture-replacement mods) and no game art ships with DewDrop.
    ///
    /// The wooden frame and the shaded rim of the sign come from the vanilla buttons (where all four agree), the
    /// middle is filled with their parchment, and "MODS" is hand-lettered in the vanilla lettering colours, with a
    /// dew drop on a leaf where the other buttons have an icon.
    /// </summary>
    internal static class TitleButtonArt
    {
        // TitleMenu's hover code flips sourceRect.Y between these two rows, so the built texture keeps them
        public const int NormalY = 187;
        public const int HoverY = 245;

        private const int Width = 74;
        private const int Height = 58;

        // Vanilla New, Load, Co-op and Exit buttons sit side by side in the texture
        private static readonly int[] SourceX = { 0, 74, 148, 222 };

        // The parchment inside the wooden border, and the part of it clear of the shaded rim
        private static readonly (int Left, int Top, int Right, int Bottom) SignInside = (6, 6, Width - 6, Height - 7);
        private static readonly (int Left, int Top, int Right, int Bottom) CleanArea = (9, 12, Width - 10, Height - 9);

        // Vanilla lettering colours, used if they can't be read from the texture
        private static readonly Color FallbackInk = new(0xCE, 0x52, 0x52);
        private static readonly Color FallbackSoft = new(0xDB, 0x80, 0x53);

        /// <summary>Where the small Mods button (used when the window is too narrow for five big ones) sits in the built texture.</summary>
        public const int CompactX = Width;
        public const int CompactWidth = 22;
        public const int CompactHeight = 25;

        // Vanilla's small About button, normal and hovered; the compact button reuses its frame
        private static readonly Point AboutSource = new(8, 458);
        private static readonly Point AboutHoverSource = new(30, 458);

        public static Texture2D Build(Texture2D titleButtons)
        {
            var textureWidth = Width + CompactWidth;
            var texture = new Texture2D(Game1.graphics.GraphicsDevice, textureWidth, HoverY + Height);
            var pixels = new Color[textureWidth * texture.Height];

            foreach (var (row, about) in new[] { (NormalY, AboutSource), (HoverY, AboutHoverSource) })
            {
                var frame = BuildFrame(titleButtons, row);
                for (var y = 0; y < Height; y++)
                {
                    Array.Copy(frame, y * Width, pixels, (row + y) * textureWidth, Width);
                }

                var compact = BuildCompactFrame(titleButtons, about);
                for (var y = 0; y < CompactHeight; y++)
                {
                    Array.Copy(compact, y * CompactWidth, pixels, (row + y) * textureWidth + CompactX, CompactWidth);
                }
            }

            texture.SetData(pixels);
            return texture;
        }

        /// <summary>The vanilla About button's frame with its "?" swapped for a small dew drop.</summary>
        private static Color[] BuildCompactFrame(Texture2D titleButtons, Point source)
        {
            var frame = new Color[CompactWidth * CompactHeight];
            titleButtons.GetData(0, new Rectangle(source.X, source.Y, CompactWidth, CompactHeight), frame, 0, frame.Length);

            // The "?" sits in the middle of the parchment; everything there becomes the parchment colour
            const int left = 7, top = 7, right = 15, bottom = 19;
            var parchment = Enumerable.Range(top, bottom - top)
                .SelectMany(y => Enumerable.Range(left, right - left).Select(x => frame[y * CompactWidth + x]))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .First().Key;
            for (var y = top; y < bottom; y++)
            {
                for (var x = left; x < right; x++)
                {
                    frame[y * CompactWidth + x] = parchment;
                }
            }

            for (var y = 0; y < SmallDrop.Length; y++)
            {
                for (var x = 0; x < SmallDrop[y].Length; x++)
                {
                    if (IconPalette.TryGetValue(SmallDrop[y][x], out var color))
                    {
                        frame[(8 + y) * CompactWidth + 8 + x] = color;
                    }
                }
            }

            return frame;
        }

        private static readonly string[] SmallDrop =
        {
            "..BB..",
            "..BB..",
            ".BLMB.",
            ".BLMB.",
            "BLWMMB",
            "BLWMMB",
            "BLMMDB",
            "BMMMDB",
            ".BDDB.",
            "..BB.."
        };

        /// <summary>Saves a texture as a PNG, for working on the art.</summary>
        public static void Export(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
        }

        private static Color[] BuildFrame(Texture2D titleButtons, int row)
        {
            var frames = SourceX.Select(x =>
            {
                var data = new Color[Width * Height];
                titleButtons.GetData(0, new Rectangle(x, row, Width, Height), data, 0, data.Length);
                return data;
            }).ToList();

            // The parchment is the commonest colour inside the sign
            var parchment = frames
                .SelectMany(f => Interior().Select(i => f[i]))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var (ink, soft) = ReadLetteringColours(frames, parchment);

            // The wooden border differs a little between buttons (knots, nails), so it's taken whole from Exit
            var blank = (Color[])frames[^1].Clone();

            // The middle of the sign is plain parchment in every vanilla button, so it's filled outright; any
            // vanilla lettering or icon pixel left there would show as a speck. Only the shaded rim along the
            // edges needs reading, keeping a pixel where all four buttons agree (three, if it isn't lettering).
            for (var y = SignInside.Top; y < SignInside.Bottom; y++)
            {
                for (var x = SignInside.Left; x < SignInside.Right; x++)
                {
                    var i = y * Width + x;
                    if (x >= CleanArea.Left && x < CleanArea.Right && y >= CleanArea.Top && y < CleanArea.Bottom)
                    {
                        blank[i] = parchment;
                        continue;
                    }

                    var top = frames.GroupBy(f => f[i]).OrderByDescending(g => g.Count()).First();
                    var agreed = top.Count() == frames.Count || (top.Count() >= frames.Count - 1 && Distance(top.Key, ink) > 60 && Distance(top.Key, soft) > 30);
                    blank[i] = agreed ? top.Key : parchment;
                }
            }

            DrawWord(blank, ink, soft);
            DrawIcon(blank, soft);
            return blank;
        }

        /// <summary>Pixel indexes of the sign's inside, away from the wooden frame.</summary>
        private static IEnumerable<int> Interior()
        {
            for (var y = 10; y < Height - 8; y++)
            {
                for (var x = 8; x < Width - 8; x++)
                {
                    yield return y * Width + x;
                }
            }
        }

        /// <summary>The lettering's main colour and its softening colour: the commonest non-parchment colours in the lettering band.</summary>
        private static (Color Ink, Color Soft) ReadLetteringColours(List<Color[]> frames, Color parchment)
        {
            var counts = new Dictionary<Color, int>();
            foreach (var frame in frames)
            {
                // Only the lettering band, so the icons' colours don't win
                for (var y = 12; y < 30; y++)
                {
                    for (var x = 10; x < Width - 10; x++)
                    {
                        var c = frame[y * Width + x];
                        if (c != parchment && c.A == 255)
                        {
                            counts[c] = counts.GetValueOrDefault(c) + 1;
                        }
                    }
                }
            }

            var ranked = counts.OrderByDescending(p => p.Value).Select(p => p.Key).ToList();
            if (ranked.Count == 0)
            {
                return (FallbackInk, FallbackSoft);
            }

            var ink = ranked[0];
            var soft = ranked.Skip(1).FirstOrDefault(c => Distance(c, ink) > 30);
            return (ink, soft == default ? FallbackSoft : soft);
        }

        // ---------- lettering ----------

        // Glyphs in the vanilla hand-lettered style: '#' ink, '+' the softening colour on rounded corners. O and D
        // are the vanilla LOAD button's own letters; M and S are drawn to match their 3-4px strokes and wobble.
        private static readonly string[] M =
        {
            "###+.....+###",
            "####+...+####",
            "#####+.+#####",
            "######+######",
            "###+#####+###",
            "###.+###+.###",
            "###..+#+..###",
            "+##...+...###",
            "+##.......###",
            "###.......###",
            "###.......+##",
            "###.......###",
            "###.......###",
            "###+......###",
            "###.......###",
            "###.......###",
            "+#+.......+#+"
        };

        private static readonly string[] O =
        {
            ".+#####+...",
            "+########..",
            "##########.",
            "##########+",
            "###+..+####",
            "###....+###",
            "###.....###",
            "###.....###",
            "###+....###",
            "####...+###",
            "+####++####",
            ".#########+",
            "..########.",
            "...+####+.."
        };

        private static readonly string[] D =
        {
            "+####+........",
            "########+.....",
            "##########....",
            "###########+..",
            "####++######..",
            "####...+#####.",
            "####.....####+",
            "+###......####",
            ".###......+###",
            ".###.......###",
            ".###.......###",
            ".###......+###",
            ".###.....####+",
            ".###....#####.",
            ".###..+#####+.",
            ".###+######+..",
            ".#########....",
            ".########.....",
            ".######+......",
            ".+###+........"
        };

        private static readonly string[] S =
        {
            "..+#####+..",
            ".+########+",
            "+###+..+###",
            "###.....+#.",
            "###........",
            "+####+.....",
            ".+#######+.",
            "...+######+",
            "......+####",
            ".......+###",
            "+.......###",
            "##+....+###",
            "+####+####+",
            ".+#######+.",
            "...+###+..."
        };

        /// <summary>
        /// Draws MODS across the lettering band where vanilla's LOAD sits (x 8-62), each letter at its own height
        /// like the vanilla hand lettering.
        /// </summary>
        private static void DrawWord(Color[] frame, Color ink, Color soft)
        {
            var glyphs = new[] { (Glyph: M, Top: 13), (Glyph: O, Top: 16), (Glyph: D, Top: 12), (Glyph: S, Top: 15) };
            const int gap = 2;
            var x = 8;

            foreach (var (glyph, top) in glyphs)
            {
                Stamp(frame, glyph, x, top, ink, soft);
                x += glyph[0].Length + gap;
            }
        }

        // ---------- icon: a dew drop resting on a leaf ----------

        // The leaf uses the vanilla New button sprout's greens, the drop is shaded the way vanilla icons are (a dark
        // outline in the object's own hue), and the shadow sits under it like the vanilla icons' shadows
        private static readonly string[] Leaf =
        {
            "..............oo...",
            "...........oooffo..",
            "........oooffffeo..",
            "......ooffeeeeego..",
            "....ooffeeegggeo...",
            "...ofeeeggggeeo....",
            "..ofeggggeeeeOo....",
            ".ogggeeeeeOOoo.....",
            ".oOOOOOOOoo........",
            "o.ooooooo.........."
        };

        private static readonly string[] Drop =
        {
            "....BB....",
            "...BLMB...",
            "...BLMB...",
            "..BLWMMB..",
            "..BLWMMB..",
            ".BLWMMMMB.",
            ".BLWMMMDB.",
            "BLWMMMMMDB",
            "BLMMMMMMDB",
            "BMMMMMMDDB",
            ".BMMMDDDB.",
            "..BBBBBB.."
        };

        private static readonly Dictionary<char, Color> IconPalette = new()
        {
            ['o'] = new(0x01, 0x44, 0x3B),
            ['O'] = new(0x06, 0x5B, 0x50),
            ['g'] = new(0x16, 0x79, 0x5A),
            ['e'] = new(0x32, 0xA9, 0x60),
            ['f'] = new(0x6C, 0xEE, 0x73),
            ['B'] = new(0x1C, 0x2E, 0x78),
            ['D'] = new(0x2C, 0x64, 0xC0),
            ['M'] = new(0x4C, 0x9C, 0xE8),
            ['L'] = new(0x8C, 0xD4, 0xFF),
            ['W'] = new(0xF0, 0xFC, 0xFF)
        };

        private static void DrawIcon(Color[] frame, Color shadow)
        {
            // Centred under the lettering like the vanilla icons: the leaf tilts up to the right and the drop
            // sits on its widest part
            const int leafLeft = 26;
            const int leafTop = 39;
            const int dropLeft = leafLeft + 3;
            const int dropTop = leafTop - 6;

            // Shadow first, so the leaf sits on it
            for (var x = 1; x <= 12; x++)
            {
                Set(frame, leafLeft + x, leafTop + Leaf.Length, shadow);
            }
            for (var x = 3; x <= 9; x++)
            {
                Set(frame, leafLeft + x, leafTop + Leaf.Length + 1, shadow);
            }

            Paint(frame, Leaf, leafLeft, leafTop);
            Paint(frame, Drop, dropLeft, dropTop);
        }

        private static void Paint(Color[] frame, string[] sprite, int left, int top)
        {
            for (var y = 0; y < sprite.Length; y++)
            {
                for (var x = 0; x < sprite[y].Length; x++)
                {
                    if (IconPalette.TryGetValue(sprite[y][x], out var color))
                    {
                        Set(frame, left + x, top + y, color);
                    }
                }
            }
        }

        // ---------- helpers ----------

        private static void Stamp(Color[] frame, string[] glyph, int left, int top, Color ink, Color soft)
        {
            for (var y = 0; y < glyph.Length; y++)
            {
                for (var x = 0; x < glyph[y].Length; x++)
                {
                    var c = glyph[y][x];
                    if (c == '#')
                    {
                        Set(frame, left + x, top + y, ink);
                    }
                    else if (c == '+')
                    {
                        Set(frame, left + x, top + y, soft);
                    }
                }
            }
        }

        private static void Set(Color[] frame, int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                frame[y * Width + x] = color;
            }
        }

        private static double Distance(Color a, Color b)
        {
            var dr = a.R - b.R;
            var dg = a.G - b.G;
            var db = a.B - b.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
