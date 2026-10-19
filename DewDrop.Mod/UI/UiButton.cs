using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>A text button in the style of the options menu buttons.</summary>
    internal class UiButton
    {
        public Rectangle Bounds;
        public string Label { get; set; }
        public Action? OnClick { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;

        /// <summary>Primary buttons use the big font and a louder click.</summary>
        public bool Primary { get; set; }

        /// <summary>Optional text colour, e.g. Ui.Good for an Update button.</summary>
        public Color? Color { get; set; }

        public string? Tooltip { get; set; }

        /// <summary>Optional click sound, e.g. "bigDeSelect" for a Cancel button; otherwise it follows Primary.</summary>
        public string? Sound { get; set; }

        private bool _hovered;

        public UiButton(string label, Action? onClick = null, bool primary = false)
        {
            Label = label;
            OnClick = onClick;
            Primary = primary;
        }

        public SpriteFont Font => Primary ? Ui.Large : Ui.Small;

        /// <summary>Width that fits the label with padding.</summary>
        public int PreferredWidth => (int)Font.MeasureString(Label).X + (Primary ? 64 : 40);

        public int PreferredHeight => Primary ? 72 : 56;

        /// <summary>Places the button with its preferred size at a point.</summary>
        public UiButton At(int x, int y)
        {
            Bounds = new Rectangle(x, y, PreferredWidth, PreferredHeight);
            return this;
        }

        /// <summary>Places the button so its right edge sits at x.</summary>
        public UiButton AtRight(int right, int y)
        {
            Bounds = new Rectangle(right - PreferredWidth, y, PreferredWidth, PreferredHeight);
            return this;
        }

        public bool Contains(int x, int y) => Visible && Bounds.Contains(x, y);

        public void Hover(int x, int y)
        {
            _hovered = Visible && Enabled && Bounds.Contains(x, y);
        }

        /// <summary>Runs the click if the point is on the button; returns whether it was.</summary>
        public bool TryClick(int x, int y)
        {
            if (!Visible || !Bounds.Contains(x, y))
            {
                return false;
            }

            if (Enabled)
            {
                Game1.playSound(Sound ?? (Primary ? "bigSelect" : "drumkit6"));
                OnClick?.Invoke();
            }

            return true;
        }

        public void Draw(SpriteBatch b)
        {
            if (!Visible)
            {
                return;
            }

            var tint = !Enabled ? Microsoft.Xna.Framework.Color.Gray * 0.8f : _hovered ? Microsoft.Xna.Framework.Color.Wheat : Microsoft.Xna.Framework.Color.White;
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, Ui.ButtonFrame, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, tint, 4f, drawShadow: Enabled);

            var label = Ui.Ellipsize(Label, Bounds.Width - 24, Font);
            var color = !Enabled ? Ui.Muted : Color ?? Ui.Text;
            var size = Font.MeasureString(label);
            var offset = _hovered ? -2 : 0;
            Ui.Label(b, label, new Vector2(Bounds.Center.X - size.X / 2, Bounds.Center.Y - size.Y / 2 + 2 + offset), color, Font);
        }

        public string? HoverText(int x, int y) => Visible && Bounds.Contains(x, y) ? Tooltip : null;
    }
}
