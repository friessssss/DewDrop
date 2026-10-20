using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>
    /// Drawing helpers built only from vanilla sprites and fonts, so every DewDrop screen looks like part of the
    /// game. Pages should draw through these rather than picking sprites themselves.
    /// </summary>
    internal static class Ui
    {
        // ---------- vanilla sprite sources (Game1.mouseCursors unless noted) ----------
        public static readonly Rectangle MenuFrame = new(0, 256, 60, 60);              // Game1.menuTexture
        public static readonly Rectangle ButtonFrame = new(432, 439, 9, 9);            // options-menu button box
        public static readonly Rectangle InsetFrame = new(384, 396, 15, 15);           // light parchment box
        public static readonly Rectangle CheckboxOff = new(227, 425, 9, 9);
        public static readonly Rectangle CheckboxOn = new(236, 425, 9, 9);
        public static readonly Rectangle ScrollUp = new(421, 459, 11, 12);
        public static readonly Rectangle ScrollDown = new(421, 472, 11, 12);
        public static readonly Rectangle ScrollThumb = new(435, 463, 6, 10);
        public static readonly Rectangle ScrollRunner = new(403, 383, 6, 6);
        public static readonly Rectangle ArrowLeft = new(352, 495, 12, 11);
        public static readonly Rectangle ArrowRight = new(365, 495, 12, 11);
        public static readonly Rectangle Heart = new(211, 428, 7, 6);
        public static readonly Rectangle Star = new(310, 392, 16, 16);                 // gold quality star
        public static readonly Rectangle Warning = new(403, 496, 5, 14);               // red "!"
        public static readonly Rectangle TrashCan = new(564, 102, 18, 26);            // the inventory's trash can
        public static readonly Rectangle TrashCanLid = new(564, 129, 18, 10);         // its lid, drawn over the top

        // ---------- colours ----------
        public static Color Text => Game1.textColor;
        public static Color Muted => Game1.textColor * 0.6f;
        public static Color Good => new(36, 120, 36);
        public static Color Bad => new(170, 32, 32);
        public static Color Attention => new(196, 108, 0);
        public static Color Selection => Color.Wheat * 0.6f;
        public static Color Hover => Color.Wheat * 0.35f;

        public static SpriteFont Small => Game1.smallFont;
        public static SpriteFont Large => Game1.dialogueFont;

        /// <summary>The standard menu frame (the one the inventory and dialogue boxes use).</summary>
        public static void Frame(SpriteBatch b, Rectangle r, Color? tint = null)
        {
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, MenuFrame, r.X, r.Y, r.Width, r.Height, tint ?? Color.White, 1f, drawShadow: true);
        }

        /// <summary>A lighter panel for cards and grouped content inside a frame.</summary>
        public static void Panel(SpriteBatch b, Rectangle r, Color? tint = null)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, InsetFrame, r.X, r.Y, r.Width, r.Height, tint ?? Color.White, 4f, drawShadow: false);
        }

        /// <summary>A plain filled rectangle, for row highlights and progress fills.</summary>
        public static void Fill(SpriteBatch b, Rectangle r, Color color)
        {
            b.Draw(Game1.staminaRect, r, color);
        }

        public static void Checkbox(SpriteBatch b, Vector2 position, bool isChecked, bool enabled = true)
        {
            b.Draw(Game1.mouseCursors, position, isChecked ? CheckboxOn : CheckboxOff, enabled ? Color.White : Color.White * 0.4f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
        }

        public const int CheckboxSize = 36;

        public static void Sprite(SpriteBatch b, Rectangle source, Vector2 position, float scale = 4f, Color? tint = null, Texture2D? texture = null)
        {
            b.Draw(texture ?? Game1.mouseCursors, position, source, tint ?? Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
        }

        /// <summary>The inventory's trash can at a scale, with its lid tipped open like vanilla's while hovered.</summary>
        public static void TrashCanIcon(SpriteBatch b, Vector2 position, float scale, bool open, bool enabled = true)
        {
            var tint = enabled ? Color.White : Color.White * 0.4f;
            b.Draw(Game1.mouseCursors, position, TrashCan, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);

            // Vanilla draws the lid 15,10 pixels into the can, turning about the lid's own 16,10
            b.Draw(Game1.mouseCursors, position + new Vector2(15, 10) * scale, TrashCanLid, tint, open ? MathF.PI / 4 : 0f, new Vector2(16, 10), scale, SpriteEffects.None, 0.91f);
        }

        /// <summary>Text with the game's usual drop shadow.</summary>
        public static void Label(SpriteBatch b, string text, Vector2 position, Color? color = null, SpriteFont? font = null)
        {
            Utility.drawTextWithShadow(b, text, font ?? Small, position, color ?? Text);
        }

        public static void LabelCentered(SpriteBatch b, string text, Rectangle area, Color? color = null, SpriteFont? font = null)
        {
            font ??= Small;
            var size = font.MeasureString(text);
            Label(b, text, new Vector2(area.Center.X - size.X / 2, area.Center.Y - size.Y / 2), color, font);
        }

        /// <summary>The big bitmap header font, as used for menu titles.</summary>
        public static void Heading(SpriteBatch b, string text, int x, int y, bool centered = false)
        {
            if (centered)
            {
                SpriteText.drawStringHorizontallyCenteredAt(b, text, x, y);
            }
            else
            {
                SpriteText.drawString(b, text, x, y);
            }
        }

        /// <summary>The scroll-shaped banner vanilla uses for screen titles.</summary>
        public static void Banner(SpriteBatch b, string text, int centerX, int y)
        {
            SpriteText.drawStringWithScrollCenteredAt(b, text, centerX, y);
        }

        public static void ProgressBar(SpriteBatch b, Rectangle r, double progress, Color? fill = null)
        {
            Fill(b, r, Game1.textColor * 0.25f);
            var inner = new Rectangle(r.X + 2, r.Y + 2, (int)((r.Width - 4) * Math.Clamp(progress, 0, 1)), r.Height - 4);
            Fill(b, inner, fill ?? Good);
        }

        /// <summary>Picture scaled to fit inside the area, centred, keeping its aspect ratio.</summary>
        public static void Picture(SpriteBatch b, Texture2D texture, Rectangle area, bool cover = false)
        {
            var scale = cover
                ? Math.Max((float)area.Width / texture.Width, (float)area.Height / texture.Height)
                : Math.Min((float)area.Width / texture.Width, (float)area.Height / texture.Height);
            var width = (int)(texture.Width * scale);
            var height = (int)(texture.Height * scale);

            if (cover)
            {
                // Crop the source so the drawn picture never spills outside the area
                var sourceWidth = (int)(area.Width / scale);
                var sourceHeight = (int)(area.Height / scale);
                var source = new Rectangle((texture.Width - sourceWidth) / 2, (texture.Height - sourceHeight) / 2, sourceWidth, sourceHeight);
                b.Draw(texture, area, source, Color.White);
                return;
            }

            b.Draw(texture, new Rectangle(area.Center.X - width / 2, area.Center.Y - height / 2, width, height), Color.White);
        }

        /// <summary>A placeholder where a picture is still loading.</summary>
        public static void PicturePlaceholder(SpriteBatch b, Rectangle area, string text = "...")
        {
            Fill(b, area, Game1.textColor * 0.12f);
            LabelCentered(b, text, area, Muted);
        }

        // ---------- text ----------

        /// <summary>Text wrapped to a width, lines separated by "\n".</summary>
        public static string Wrap(string text, int width, SpriteFont? font = null)
        {
            return string.Join("\n", WrapLines(text, width, font));
        }

        /// <summary>
        /// Text wrapped to a width as separate lines. Game1.parseText leaves a trailing space on each line, can run
        /// a pixel over, and never breaks a word longer than the width (long URLs in Nexus descriptions), so this
        /// tidies its output and hard-breaks anything still too wide.
        /// </summary>
        public static List<string> WrapLines(string text, int width, SpriteFont? font = null)
        {
            font ??= Small;
            width = Math.Max(16, width);
            var lines = new List<string>();
            foreach (var raw in Game1.parseText(text, font, width - 2).Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.TrimEnd();
                while (line.Length > 1 && font.MeasureString(line).X > width)
                {
                    var fit = line.Length - 1;
                    while (fit > 1 && font.MeasureString(line[..fit]).X > width)
                    {
                        fit--;
                    }
                    lines.Add(line[..fit]);
                    line = line[fit..];
                }
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>Cuts text to fit a width, ending in "...".</summary>
        public static string Ellipsize(string text, int width, SpriteFont? font = null)
        {
            font ??= Small;
            if (font.MeasureString(text).X <= width)
            {
                return text;
            }

            const string dots = "...";
            var low = 0;
            var high = text.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (font.MeasureString(text[..mid] + dots).X <= width)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return text[..low].TrimEnd() + dots;
        }

        public static int LineHeight(SpriteFont? font = null) => (int)(font ?? Small).MeasureString("Ag").Y;

        /// <summary>1234 -> "1.2K", 3400000 -> "3.4M".</summary>
        public static string Count(long value)
        {
            return value switch
            {
                >= 1_000_000 => (value / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "M",
                >= 1_000 => (value / 1_000d).ToString("0.#", CultureInfo.InvariantCulture) + "K",
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }

        public static string Size(long bytes)
        {
            return bytes switch
            {
                >= 1L << 30 => (bytes / (double)(1L << 30)).ToString("0.0", CultureInfo.InvariantCulture) + " GB",
                >= 1L << 20 => (bytes / (double)(1L << 20)).ToString("0.0", CultureInfo.InvariantCulture) + " MB",
                >= 1L << 10 => (bytes / (double)(1L << 10)).ToString("0", CultureInfo.InvariantCulture) + " KB",
                _ => bytes + " B"
            };
        }

        public static string Ago(DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            return span.TotalDays switch
            {
                >= 365 => $"{(int)(span.TotalDays / 365)}y ago",
                >= 30 => $"{(int)(span.TotalDays / 30)}mo ago",
                >= 1 => $"{(int)span.TotalDays}d ago",
                _ => span.TotalHours >= 1 ? $"{(int)span.TotalHours}h ago" : "just now"
            };
        }

        private static SpriteFont? _glyphFont;
        private static HashSet<char> _glyphs = new();
        private static readonly Dictionary<char, string> _fallbacks = new();

        /// <summary>Letters that keep no plain base letter when their accent is taken off.</summary>
        private static readonly Dictionary<char, string> _letterFallbacks = new()
        {
            ['ł'] = "l", ['Ł'] = "L", ['ı'] = "i", ['đ'] = "d", ['Đ'] = "D", ['ħ'] = "h", ['Ħ'] = "H", ['œ'] = "oe", ['Œ'] = "OE"
        };

        /// <summary>
        /// Whether the small font can draw a character. The font follows the game language (the Russian and Chinese
        /// ones have Cyrillic and CJK glyphs the English one lacks), so the glyph set is cached per font.
        /// </summary>
        public static bool HasGlyph(char c)
        {
            if (!ReferenceEquals(_glyphFont, Small))
            {
                _glyphFont = Small;
                _glyphs = new HashSet<char>(Small.Characters);
                _fallbacks.Clear();
            }

            return _glyphs.Contains(c);
        }

        /// <summary>
        /// Appends a character the small font can draw: as it is, else without its accent (č becomes c, ł becomes l),
        /// else nothing. A missing glyph would otherwise draw as "*".
        /// </summary>
        public static void AppendDrawable(StringBuilder text, char c)
        {
            if (HasGlyph(c))
            {
                text.Append(c);
                return;
            }

            if (!_fallbacks.TryGetValue(c, out var fallback))
            {
                fallback = _letterFallbacks.TryGetValue(c, out var mapped) ? mapped : WithoutAccents(c);
                if (!fallback.All(HasGlyph))
                {
                    fallback = "";
                }
                _fallbacks[c] = fallback;
            }

            text.Append(fallback);
        }

        private static string WithoutAccents(char c)
        {
            // Normalize throws on half of a surrogate pair (emoji and other characters outside the BMP)
            if (char.IsSurrogate(c))
            {
                return "";
            }

            try
            {
                return string.Concat(c.ToString().Normalize(NormalizationForm.FormD).Where(d => CharUnicodeInfo.GetUnicodeCategory(d) != UnicodeCategory.NonSpacingMark));
            }
            catch (ArgumentException)
            {
                return "";
            }
        }

        private static readonly Regex _bbCode = new(@"\[/?[a-zA-Z*]+(=[^\]]*)?\]", RegexOptions.Compiled);

        // Pictures and videos can't be shown as text, and their addresses are only noise in a description
        private static readonly Regex _media = new(@"\[(img|youtube)(=[^\]]*)?\].*?\[/\1\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex _imageUrlLine = new(@"^[ \t]*https?://\S+\.(png|jpe?g|gif|webp|bmp)(\?\S*)?[ \t]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex _html = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex _blankLines = new(@"\n{3,}", RegexOptions.Compiled);
        private static readonly Regex _spaces = new(@" {2,}", RegexOptions.Compiled);

        /// <summary>Nexus descriptions mix BBCode and HTML; reduce them to plain text the game fonts can show.</summary>
        public static string PlainText(string? markup)
        {
            if (string.IsNullOrWhiteSpace(markup))
            {
                return "";
            }

            var text = markup.Replace("\r", "")
                .Replace("<br />", "\n").Replace("<br/>", "\n").Replace("<br>", "\n")
                .Replace("[*]", "\n- ");
            text = _media.Replace(text, "");
            text = _html.Replace(text, "");
            text = _bbCode.Replace(text, "");
            text = WebUtility.HtmlDecode(text);
            text = _imageUrlLine.Replace(text, "");

            // Keep what the current game font can draw (it depends on the language), take accents off letters it
            // lacks, and drop the rest (most symbols and emoji)
            var cleaned = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c == '\n' || (c >= ' ' && c <= '~'))
                {
                    cleaned.Append(c);
                }
                else if (c == '’' || c == '‘')
                {
                    cleaned.Append('\'');
                }
                else if (c == '“' || c == '”')
                {
                    cleaned.Append('"');
                }
                else if (c == '–' || c == '—')
                {
                    cleaned.Append('-');
                }
                else if (char.IsWhiteSpace(c))
                {
                    cleaned.Append(' ');
                }
                else
                {
                    AppendDrawable(cleaned, c);
                }
            }

            // Dropped characters leave runs of spaces (a browser would collapse them anyway)
            var lines = cleaned.ToString().Split('\n').Select(l => _spaces.Replace(l, " ").TrimEnd());
            var plain = _blankLines.Replace(string.Join("\n", lines), "\n\n").Trim();

            // Text in a script the font can't draw leaves only digits and punctuation; return nothing so callers fall
            // back to the summary or "no description"
            return plain.Any(char.IsLetter) ? plain : "";
        }
    }
}
