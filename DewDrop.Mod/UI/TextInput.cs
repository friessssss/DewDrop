using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>
    /// The vanilla text box, with three changes: it accepts every character (vanilla swallows quotes and plays
    /// sounds for symbols, which breaks API keys), it can show placeholder text, and it reports edits.
    /// Clipboard paste (Ctrl+V) is handled by the game's keyboard dispatcher.
    /// </summary>
    internal class TextInput : TextBox
    {
        private static Texture2D? _texture;
        private string _lastText = "";

        public string Placeholder { get; set; } = "";

        /// <summary>Raised on the main thread when the text changes.</summary>
        public Action<string>? OnChanged { get; set; }

        /// <summary>Raised when Enter is pressed.</summary>
        public Action<string>? OnSubmit { get; set; }

        /// <summary>Show dots instead of the text (e.g. for an API key).</summary>
        public bool Masked { get; set; }

        public TextInput(string placeholder = "", bool limitToWidth = true)
            : base(_texture ??= Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            Placeholder = placeholder;
            limitWidth = limitToWidth;
            Height = 48;
            OnEnterPressed += _ => OnSubmit?.Invoke(Text);
        }

        public Rectangle Bounds
        {
            get => new(X, Y, Width, Height);
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = 48;
            }
        }

        /// <summary>Call from the menu's left click: selects the box when clicked, deselects otherwise.</summary>
        public void Click(int x, int y)
        {
            var inside = Bounds.Contains(x, y);
            if (inside && !Selected)
            {
                Game1.playSound("smallSelect");
            }
            Selected = inside;

            // A controller has no keys to type with; open the game's on-screen keyboard, as vanilla text boxes do
            if (inside && Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse)
            {
                Game1.showTextEntry(this);
            }
        }

        /// <summary>Call every tick to raise OnChanged.</summary>
        public void Tick()
        {
            if (Text != _lastText)
            {
                _lastText = Text;
                OnChanged?.Invoke(Text);
            }
        }

        public void Clear()
        {
            Text = "";
            _lastText = "";
        }

        public override void RecieveTextInput(char inputChar)
        {
            if (!Selected || (textLimit != -1 && Text.Length >= textLimit))
            {
                return;
            }

            Text += inputChar;
        }

        /// <summary>
        /// Same look as the vanilla box, but the shown text is cut to what fits inside it (vanilla lets long text,
        /// like a pasted API key, run past the right edge), and masked text is drawn as stars.
        /// </summary>
        public override void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
        {
            var texture = _texture!;
            spriteBatch.Draw(texture, new Rectangle(X, Y, 16, Height), new Rectangle(0, 0, 16, Height), Color.White);
            spriteBatch.Draw(texture, new Rectangle(X + 16, Y, Math.Max(0, Width - 32), Height), new Rectangle(16, 0, 4, Height), Color.White);
            spriteBatch.Draw(texture, new Rectangle(X + Width - 16, Y, 16, Height), new Rectangle(texture.Bounds.Width - 16, 0, 16, Height), Color.White);

            var shown = Masked ? new string('*', Text.Length) : Text;
            var room = Math.Max(0, Width - 40);

            // Keep the end of the text (where the caret is) visible, like vanilla
            var start = 0;
            while (start < shown.Length && Font.MeasureString(shown[start..]).X > room)
            {
                start++;
            }
            shown = shown[start..];

            var textPosition = new Vector2(X + 16, Y + 12);
            if (shown.Length == 0 && !string.IsNullOrEmpty(Placeholder))
            {
                Utility.drawTextWithShadow(spriteBatch, Ui.Ellipsize(Placeholder, room, Font), Font, textPosition, Game1.textColor * 0.45f);
            }
            else if (drawShadow)
            {
                Utility.drawTextWithShadow(spriteBatch, shown, Font, textPosition, TextColor);
            }
            else
            {
                spriteBatch.DrawString(Font, shown, textPosition, TextColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
            }

            var blinkOn = Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000.0 >= 500.0;
            if (Selected && blinkOn)
            {
                var caretX = X + 16 + (int)Font.MeasureString(shown).X + 2;
                spriteBatch.Draw(Game1.staminaRect, new Rectangle(caretX, Y + 8, 4, 32), TextColor);
            }
        }
    }
}
