using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>
    /// A scrolling list (or grid, with Columns &gt; 1) of fixed-height cells with the vanilla scroll bar on the
    /// right. Scrolls a whole row at a time, like the game's own lists.
    /// </summary>
    internal class ScrollList<T>
    {
        private const int ScrollBarWidth = 44;
        private const int ArrowHeight = 48;

        public Rectangle Bounds { get; private set; }
        public int RowHeight { get; set; }
        public int Columns { get; set; } = 1;
        public List<T> Items { get; private set; } = new();
        public int FirstRow { get; private set; }

        /// <summary>Highlighted item (e.g. the selected profile); drawn with a selection tint.</summary>
        public T? Selected { get; set; }

        /// <summary>Shown centred when there are no items.</summary>
        public string EmptyText { get; set; } = "Nothing here yet.";

        /// <summary>Space between rows and columns. Text-line lists can set it to 0.</summary>
        public int RowGap { get; set; } = 12;

        /// <summary>Whether the row under the mouse is tinted; turn off for lists whose rows do nothing on click.</summary>
        public bool HoverHighlight { get; set; } = true;

        /// <summary>Limits the hover tint to rows this accepts, for lists where only some rows do something on click.</summary>
        public Func<T, bool>? CanClick { get; set; }

        private Rectangle _upArrow, _downArrow, _runner, _thumb;
        private bool _showArrows;
        private bool _dragging;
        private int _hoverIndex = -1;

        public ScrollList(int rowHeight, int columns = 1)
        {
            RowHeight = rowHeight;
            Columns = columns;
        }

        public void Layout(Rectangle bounds)
        {
            Bounds = bounds;
            var x = bounds.Right - ScrollBarWidth;

            // Short lists drop the arrows and let the runner use the full height (wheel and drag still work)
            _showArrows = bounds.Height >= ArrowHeight * 2 + 8;
            _upArrow = _showArrows ? new Rectangle(x, bounds.Y, 44, ArrowHeight) : Rectangle.Empty;
            _downArrow = _showArrows ? new Rectangle(x, bounds.Bottom - ArrowHeight, 44, ArrowHeight) : Rectangle.Empty;
            _runner = _showArrows
                ? new Rectangle(x + 12, _upArrow.Bottom + 4, 24, Math.Max(0, _downArrow.Top - _upArrow.Bottom - 8))
                : new Rectangle(x + 12, bounds.Y, 24, Math.Max(0, bounds.Height));
            ClampScroll();
        }

        /// <summary>Replaces the items; keeps the scroll position unless asked to reset it.</summary>
        public void SetItems(IEnumerable<T> items, bool resetScroll = false)
        {
            Items = items.ToList();
            if (resetScroll)
            {
                FirstRow = 0;
            }
            ClampScroll();
        }

        private int ContentWidth => Bounds.Width - ScrollBarWidth - 8;
        private int CellWidth => (ContentWidth - (Columns - 1) * RowGap) / Columns;
        private int VisibleRows => Math.Max(1, (Bounds.Height + RowGap) / (RowHeight + RowGap));
        private int TotalRows => (Items.Count + Columns - 1) / Columns;
        private int MaxFirstRow => Math.Max(0, TotalRows - VisibleRows);
        private bool NeedsScrollBar => TotalRows > VisibleRows;

        private void ClampScroll()
        {
            FirstRow = Math.Clamp(FirstRow, 0, MaxFirstRow);
        }

        public void ScrollBy(int rows)
        {
            var before = FirstRow;
            FirstRow += rows;
            ClampScroll();
            if (FirstRow != before)
            {
                Game1.playSound("shiny4");
            }
        }

        public void ScrollTo(T item)
        {
            var index = Items.IndexOf(item);
            if (index < 0)
            {
                return;
            }

            var row = index / Columns;
            if (row < FirstRow)
            {
                FirstRow = row;
            }
            else if (row >= FirstRow + VisibleRows)
            {
                FirstRow = row - VisibleRows + 1;
            }
            ClampScroll();
        }

        /// <summary>The screen rectangle of a visible item, or null if it's scrolled out of view.</summary>
        public Rectangle? CellBounds(int index)
        {
            var row = index / Columns - FirstRow;
            if (index < 0 || index >= Items.Count || row < 0 || row >= VisibleRows)
            {
                return null;
            }

            var column = index % Columns;
            return new Rectangle(Bounds.X + column * (CellWidth + RowGap), Bounds.Y + row * (RowHeight + RowGap), CellWidth, RowHeight);
        }

        /// <summary>The item under a point, with its cell.</summary>
        public bool TryGetItemAt(int x, int y, out T item, out Rectangle cell)
        {
            for (var i = FirstRow * Columns; i < Math.Min(Items.Count, (FirstRow + VisibleRows) * Columns); i++)
            {
                var bounds = CellBounds(i);
                if (bounds is { } rect && rect.Contains(x, y))
                {
                    item = Items[i];
                    cell = rect;
                    return true;
                }
            }

            item = default!;
            cell = Rectangle.Empty;
            return false;
        }

        public void ReceiveScrollWheel(int direction)
        {
            if (Bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                ScrollBy(direction > 0 ? -1 : 1);
            }
        }

        /// <summary>Handles clicks on the scroll bar; returns true if the click was consumed.</summary>
        public bool ReceiveLeftClick(int x, int y)
        {
            if (!NeedsScrollBar)
            {
                return false;
            }

            if (_upArrow.Contains(x, y))
            {
                ScrollBy(-1);
                return true;
            }
            if (_downArrow.Contains(x, y))
            {
                ScrollBy(1);
                return true;
            }
            if (_runner.Contains(x, y) || _thumb.Contains(x, y))
            {
                _dragging = true;
                DragTo(y);
                return true;
            }

            return false;
        }

        public void LeftClickHeld(int x, int y)
        {
            if (_dragging)
            {
                DragTo(y);
            }
        }

        public void ReleaseLeftClick()
        {
            _dragging = false;
        }

        private void DragTo(int y)
        {
            if (_runner.Height <= 0)
            {
                return;
            }

            var fraction = Math.Clamp((y - _runner.Y) / (float)_runner.Height, 0f, 1f);
            var row = (int)Math.Round(fraction * MaxFirstRow);
            if (row != FirstRow)
            {
                FirstRow = row;
                Game1.playSound("shiny4");
            }
        }

        public void Hover(int x, int y)
        {
            _hoverIndex = -1;
            for (var i = FirstRow * Columns; i < Math.Min(Items.Count, (FirstRow + VisibleRows) * Columns); i++)
            {
                if (CellBounds(i) is { } rect && rect.Contains(x, y))
                {
                    _hoverIndex = i;
                    return;
                }
            }
        }

        /// <param name="drawCell">Draws one item into its cell; the bool says whether the mouse is over it.</param>
        public void Draw(SpriteBatch b, Action<SpriteBatch, T, Rectangle, bool> drawCell)
        {
            if (Items.Count == 0)
            {
                Ui.LabelCentered(b, Ui.Wrap(EmptyText, Math.Max(40, ContentWidth - 32)), new Rectangle(Bounds.X, Bounds.Y, ContentWidth, Math.Min(Bounds.Height, 200)), Ui.Muted);
            }

            for (var i = FirstRow * Columns; i < Math.Min(Items.Count, (FirstRow + VisibleRows) * Columns); i++)
            {
                if (CellBounds(i) is not { } cell)
                {
                    continue;
                }

                var item = Items[i];
                if (Selected is not null && EqualityComparer<T>.Default.Equals(item, Selected))
                {
                    Ui.Fill(b, cell, Ui.Selection);
                }
                else if (i == _hoverIndex && Columns == 1 && HoverHighlight && (CanClick?.Invoke(item) ?? true))
                {
                    Ui.Fill(b, cell, Ui.Hover);
                }

                drawCell(b, item, cell, i == _hoverIndex);
            }

            if (NeedsScrollBar)
            {
                if (_showArrows)
                {
                    Ui.Sprite(b, Ui.ScrollUp, new Vector2(_upArrow.X, _upArrow.Y));
                    Ui.Sprite(b, Ui.ScrollDown, new Vector2(_downArrow.X, _downArrow.Y));
                }

                // The runner box needs at least its corners' height to draw cleanly
                if (_runner.Height < 24)
                {
                    return;
                }
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, Ui.ScrollRunner, _runner.X, _runner.Y, _runner.Width, _runner.Height, Color.White, 4f, drawShadow: false);

                // The thumb sprite is 40px; a shorter runner gets a squashed one so it never spills past the runner
                var thumbHeight = Math.Min(40, _runner.Height);
                var travel = Math.Max(0, _runner.Height - thumbHeight);
                var thumbY = _runner.Y + (MaxFirstRow == 0 ? 0 : (int)(travel * (FirstRow / (float)MaxFirstRow)));
                _thumb = new Rectangle(_runner.X, thumbY, 24, thumbHeight);
                b.Draw(Game1.mouseCursors, _thumb, Ui.ScrollThumb, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
            }
        }
    }
}
