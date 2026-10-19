using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DewDrop.UI
{
    /// <summary>
    /// One tab of the DewDrop menu. The menu owns the frame, tabs, bottom bar, toasts, tooltips and confirm
    /// dialogs; a page only lays out and draws inside <see cref="Bounds"/>.
    /// </summary>
    internal abstract class Page
    {
        protected DewDropMenu Menu { get; }

        /// <summary>The content area this page may draw in.</summary>
        public Rectangle Bounds { get; private set; }

        protected Page(DewDropMenu menu)
        {
            Menu = menu;
        }

        /// <summary>Tab label.</summary>
        public abstract string Title { get; }

        /// <summary>Optional small count shown on the tab (e.g. active downloads, updates).</summary>
        public virtual string? Badge => null;

        /// <summary>Whether a text box on this page has keyboard focus; the menu won't close while it does.</summary>
        public virtual bool IsTyping => false;

        /// <summary>Called when the window size changes and once before first show.</summary>
        public virtual void Layout(Rectangle bounds)
        {
            Bounds = bounds;
        }

        /// <summary>Called each time the tab becomes the current one.</summary>
        public virtual void OnShow() { }

        /// <summary>Called when switching away; deselect text boxes here.</summary>
        public virtual void OnHide() { }

        public abstract void Draw(SpriteBatch b);

        public virtual void Update(GameTime time) { }
        public virtual void ReceiveLeftClick(int x, int y) { }
        public virtual void LeftClickHeld(int x, int y) { }
        public virtual void ReleaseLeftClick(int x, int y) { }
        public virtual void ReceiveScrollWheel(int direction) { }
        public virtual void ReceiveKeyPress(Keys key) { }

        /// <summary>Update hover states; return tooltip text to show, if any.</summary>
        public virtual string? PerformHover(int x, int y) => null;
    }
}
