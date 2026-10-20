using System.Globalization;
using DewDrop.Core.Models;
using DewDrop.Core.Services;
using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>Nexus Mods search as a grid of cards with sorting and paging; a card opens the mod's details and files.</summary>
    internal class BrowsePage : Page
    {
        private const int PageSize = 24;
        private const int CardGap = 12;          // ScrollList's gap between cells
        private const int MinCardHeight = 230;
        private const int MaxCardHeight = 330;
        private const int SideCardHeight = 204;  // shorter cards put the picture beside the text
        private const int CardTextHeight = 112;
        private const int StripHeight = 52;
        private const int MinSearchWidth = 270;
        private const int MaxSearchWidth = 480;

        /// <summary>Sort buttons with their labels from longest to shortest, for narrow windows.</summary>
        private static readonly (NexusSort Sort, string[] Labels, string Tooltip)[] Sorts =
        {
            (NexusSort.Trending, new[] { "Trending", "Trending", "Trending" }, "The most endorsed mods from the last 30 days. With a search, the most endorsed matches come first."),
            (NexusSort.Endorsements, new[] { "Top rated", "Top rated", "Top" }, "The most endorsed mods of all time."),
            (NexusSort.Downloads, new[] { "Most downloaded", "Downloads", "Downloads" }, "The most downloaded mods of all time."),
            (NexusSort.Updated, new[] { "Updated", "Updated", "Updated" }, "Recently updated mods first."),
            (NexusSort.Newest, new[] { "New", "New", "New" }, "The newest mods first.")
        };

        private readonly record struct SearchKey(string Query, NexusSort Sort, int Page, bool Adult);

        private readonly TextInput _search = new("Search Nexus Mods");
        private readonly UiButton _searchButton;
        private readonly List<UiButton> _sortButtons = new();
        private readonly UiButton _retry;
        private readonly ScrollList<NexusMod> _grid = new(MinCardHeight, 2);
        // Shared by every opening of the menu this session; entries go stale after CacheLifetime
        private static readonly Dictionary<SearchKey, (NexusSearchPage Page, DateTime Fetched)> _cache = new();
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);
        /// <summary>By Nexus mod ID: the installed version to show, and the new update's version when one was found.</summary>
        private readonly Dictionary<int, (string Version, string? Update)> _installed = new();
        private ModDetailView? _detail;
        private string _query = "";
        private NexusSort _sort = NexusSort.Trending;
        private int _page;
        private int _totalCount = -1;
        private int _requestId;
        private bool _loading;
        private string? _error;
        private bool _started;
        private bool _adult;
        private int _installedVersion = -1;
        private int _installedChanges = -1;
        private Rectangle _strip;
        private Rectangle _prevArrow;
        private Rectangle _nextArrow;
        private bool _prevHovered;
        private bool _nextHovered;

        public BrowsePage(DewDropMenu menu) : base(menu)
        {
            _searchButton = new UiButton("Search", Search);
            _retry = new UiButton("Try again", () => Load(_page)) { Visible = false };
            _search.OnSubmit = _ => Search();

            foreach (var (sort, labels, tooltip) in Sorts)
            {
                _sortButtons.Add(new UiButton(labels[0], () => SetSort(sort)) { Tooltip = tooltip });
            }
            UpdateSortButtons();
        }

        public override string Title => "Browse";

        public override bool IsTyping => _search.Selected;

        /// <summary>Trending is a date filter, which means nothing next to search text, so searches sort by relevance instead.</summary>
        private NexusSort EffectiveSort => _query.Length > 0 && _sort == NexusSort.Trending ? NexusSort.Relevance : _sort;

        private int PageCount => Math.Max(1, (_totalCount + PageSize - 1) / PageSize);

        private bool CanGoBack => _page > 0;

        private bool CanGoForward => _totalCount >= 0 && (_page + 1) * PageSize < _totalCount;

        public override void Layout(Rectangle bounds)
        {
            base.Layout(bounds);

            // The search box and sort buttons share a row when they fit, trying shorter sort labels before wrapping
            var searchButtonWidth = _searchButton.PreferredWidth;
            var searchRowWidth = MinSearchWidth + 12 + searchButtonWidth + 20;
            var labelSet = Enumerable.Range(0, 3).FirstOrDefault(set => searchRowWidth + SortRowWidth(set) <= bounds.Width, -1);
            var wrap = labelSet < 0;
            if (wrap)
            {
                labelSet = Enumerable.Range(0, 3).FirstOrDefault(set => SortRowWidth(set) <= bounds.Width, 2);
            }
            ApplyLabels(labelSet);

            var sortsWidth = SortRowWidth(labelSet);
            var searchWidth = wrap
                ? Math.Min(MaxSearchWidth, bounds.Width - 12 - searchButtonWidth)
                : Math.Clamp(bounds.Width - 12 - searchButtonWidth - 20 - sortsWidth, MinSearchWidth, MaxSearchWidth);
            _search.Bounds = new Rectangle(bounds.X, bounds.Y + 4, searchWidth, 48);
            _searchButton.At(_search.Bounds.Right + 12, bounds.Y);

            var sortY = wrap ? _searchButton.Bounds.Bottom + 8 : bounds.Y;
            var x = wrap ? bounds.X : bounds.Right - sortsWidth;
            foreach (var button in _sortButtons)
            {
                button.At(x, sortY);
                x = button.Bounds.Right + 8;
            }

            // Bottom strip: result count on the left, page arrows around a fixed slot for "Page X of Y"
            _strip = new Rectangle(bounds.X, bounds.Bottom - StripHeight, bounds.Width, StripHeight);
            var slot = (int)Ui.Small.MeasureString("Page 9999 of 9999").X;
            var arrowY = _strip.Center.Y - Ui.ArrowLeft.Height * 2;
            _prevArrow = new Rectangle(_strip.Center.X - slot / 2 - 16 - Ui.ArrowLeft.Width * 4, arrowY, Ui.ArrowLeft.Width * 4, Ui.ArrowLeft.Height * 4);
            _nextArrow = new Rectangle(_strip.Center.X + slot / 2 + 16, arrowY, Ui.ArrowRight.Width * 4, Ui.ArrowRight.Height * 4);

            // Cards stretch so whole rows fill the grid; a short window gets one row of side-by-side cards
            var gridTop = sortY + _sortButtons[0].PreferredHeight + 12;
            var gridHeight = Math.Max(48, _strip.Y - 8 - gridTop);
            var rows = Math.Max(1, (gridHeight + CardGap) / (MinCardHeight + CardGap));
            _grid.Columns = Math.Max(2, bounds.Width / 330);
            _grid.RowHeight = Math.Min(MaxCardHeight, (gridHeight + CardGap) / rows - CardGap);
            _grid.Layout(new Rectangle(bounds.X, gridTop, bounds.Width, gridHeight));

            // Kept inside the grid so a short window doesn't push it into the page strip
            _retry.At(_grid.Bounds.Center.X - _retry.PreferredWidth / 2, Math.Min(_grid.Bounds.Center.Y + 8, _grid.Bounds.Bottom - _retry.PreferredHeight));
            _detail?.Layout(bounds);
        }

        private int SortRowWidth(int labelSet)
        {
            return Sorts.Sum(s => (int)Ui.Small.MeasureString(s.Labels[labelSet]).X + 40) + (Sorts.Length - 1) * 8;
        }

        private void ApplyLabels(int labelSet)
        {
            for (var i = 0; i < Sorts.Length; i++)
            {
                _sortButtons[i].Label = Sorts[i].Labels[labelSet];
            }
        }

        public override void OnShow()
        {
            RefreshInstalled();
            if (!_started)
            {
                _started = true;
                _adult = ModEntry.Config.ShowAdultContent;
                Load(0);
            }
        }

        public override void OnHide()
        {
            _search.Selected = false;
        }

        // ---------- searching ----------

        private void Search()
        {
            _search.Selected = false;
            _query = _search.Text.Trim();
            _totalCount = -1;
            Load(0);
        }

        private void SetSort(NexusSort sort)
        {
            _sort = sort;
            _query = _search.Text.Trim();
            _totalCount = -1;
            UpdateSortButtons();
            Load(0);
        }

        private void UpdateSortButtons()
        {
            for (var i = 0; i < Sorts.Length; i++)
            {
                _sortButtons[i].Color = Sorts[i].Sort == _sort ? Ui.Good : null;
            }
        }

        /// <summary>Shows one page of results for the current search and sort, from the cache or from Nexus.</summary>
        private void Load(int page)
        {
            var key = new SearchKey(_query.ToLowerInvariant(), EffectiveSort, page, _adult);
            var request = ++_requestId;
            var query = _query;
            _page = page;
            _error = null;
            _retry.Visible = false;

            if (_cache.TryGetValue(key, out var cached) && DateTime.Now - cached.Fetched < CacheLifetime)
            {
                ShowResults(cached.Page);
                return;
            }

            _loading = true;
            _grid.SetItems(Array.Empty<NexusMod>(), resetScroll: true);
            MainThread.Run(
                () => ModEntry.Nexus.Api.SearchAsync(query, key.Sort, page * PageSize, PageSize, key.Adult),
                result =>
                {
                    _cache[key] = (result, DateTime.Now);

                    // A slow reply for an older search must not replace the one the player is waiting for
                    if (request == _requestId)
                    {
                        ShowResults(result);
                    }
                },
                ex =>
                {
                    if (request != _requestId)
                    {
                        return;
                    }

                    _loading = false;
                    _error = ModDetailView.ErrorText(ex);

                    // A search Nexus rejects (say, one it can't match) fails the same way every time
                    _retry.Visible = ex is not NexusApiException { CanRetry: false };
                });
        }

        private void ShowResults(NexusSearchPage result)
        {
            _loading = false;
            _totalCount = result.TotalCount;
            _grid.EmptyText = _query.Length > 0 ? $"No mods match \"{Ui.Ellipsize(ModDetailView.Clean(_query), 300)}\"." : "No mods found.";
            _grid.SetItems(result.Mods, resetScroll: true);
        }

        private void GoToPage(int page)
        {
            Game1.playSound("smallSelect");
            Load(page);
        }

        /// <summary>
        /// Nexus IDs of installed mods, for the "Installed" tag on cards: from their update keys, from what DewDrop
        /// installed, and from what smapi.io knows them by. Uninstalled mods waiting to be deleted aren't installed.
        /// </summary>
        private void RefreshInstalled()
        {
            var mods = ModEntry.Mods;
            _installedVersion = mods.Installed.GetHashCode();
            _installedChanges = mods.ChangeCount;
            _installed.Clear();

            // Loose copies first, so a collection copy's version only shows when there's nothing else
            foreach (var mod in mods.Installed.OrderBy(m => m.SourceId is null ? 0 : 1))
            {
                var update = mods.HasNewUpdate(mod) ? mod.Update?.Version : null;
                foreach (var id in mods.Sources.NexusIdsOf(mod))
                {
                    if (!_installed.TryGetValue(id, out var shown))
                    {
                        _installed[id] = (mod.Version, update);
                    }
                    else if (shown.Update is null && update is not null)
                    {
                        // Any copy with an update makes the card say so
                        _installed[id] = (mod.Version, update);
                    }
                }
            }
        }

        // ---------- detail view ----------

        private void OpenDetail(NexusMod mod)
        {
            Game1.playSound("smallSelect");
            _search.Selected = false;
            _detail?.Close();
            _detail = new ModDetailView(Menu, mod, CloseDetail);
            _detail.Layout(Bounds);
        }

        /// <summary>Back to the grid, which kept its page and scroll position.</summary>
        private void CloseDetail()
        {
            _detail?.Close();
            _detail = null;
        }

        // ---------- input ----------

        public override void Update(GameTime time)
        {
            _search.Tick();

            // The installed list object is replaced on rescan (e.g. after a download finishes), and an update check can
            // bring updates and Nexus IDs
            if (ModEntry.Mods.Installed.GetHashCode() != _installedVersion || ModEntry.Mods.ChangeCount != _installedChanges)
            {
                RefreshInstalled();
            }

            // Cached pages were filtered with the old setting
            if (_started && ModEntry.Config.ShowAdultContent != _adult)
            {
                _adult = ModEntry.Config.ShowAdultContent;
                _cache.Clear();
                _totalCount = -1;
                Load(0);
            }

            _detail?.Update(time);
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (_detail is not null)
            {
                _search.Selected = false;
                _detail.ReceiveLeftClick(x, y);
                return;
            }

            _search.Click(x, y);
            if (_searchButton.TryClick(x, y) || _sortButtons.Any(b => b.TryClick(x, y)) || _retry.TryClick(x, y))
            {
                return;
            }

            if (_prevArrow.Contains(x, y))
            {
                if (CanGoBack)
                {
                    GoToPage(_page - 1);
                }
                return;
            }

            if (_nextArrow.Contains(x, y))
            {
                if (CanGoForward)
                {
                    GoToPage(_page + 1);
                }
                return;
            }

            if (_loading || _error is not null || _grid.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (_grid.TryGetItemAt(x, y, out var mod, out _))
            {
                OpenDetail(mod);
            }
        }

        public override void LeftClickHeld(int x, int y)
        {
            if (_detail is not null)
            {
                _detail.LeftClickHeld(x, y);
                return;
            }

            _grid.LeftClickHeld(x, y);
        }

        public override void ReleaseLeftClick(int x, int y)
        {
            _detail?.ReleaseLeftClick(x, y);
            _grid.ReleaseLeftClick();
        }

        public override void ReceiveScrollWheel(int direction)
        {
            if (_detail is not null)
            {
                _detail.ReceiveScrollWheel(direction);
                return;
            }

            _grid.ReceiveScrollWheel(direction);
        }

        public override void ReceiveKeyPress(Keys key)
        {
            if (_search.Selected)
            {
                return;
            }

            if (_detail is not null)
            {
                _detail.ReceiveKeyPress(key);
                return;
            }

            if (key == Keys.Down)
            {
                _grid.ScrollBy(1);
            }
            else if (key == Keys.Up)
            {
                _grid.ScrollBy(-1);
            }
            else if (key == Keys.Left && CanGoBack)
            {
                GoToPage(_page - 1);
            }
            else if (key == Keys.Right && CanGoForward)
            {
                GoToPage(_page + 1);
            }
        }

        public override string? PerformHover(int x, int y)
        {
            if (_detail is not null)
            {
                return _detail.PerformHover(x, y);
            }

            _search.Hover(x, y);
            _searchButton.Hover(x, y);
            _retry.Hover(x, y);
            foreach (var button in _sortButtons)
            {
                button.Hover(x, y);
            }
            _prevHovered = CanGoBack && _prevArrow.Contains(x, y);
            _nextHovered = CanGoForward && _nextArrow.Contains(x, y);
            _grid.Hover(x, y);

            var tooltip = _sortButtons.Select(b => b.HoverText(x, y)).FirstOrDefault(t => t is not null);
            if (tooltip is not null)
            {
                return tooltip;
            }

            if (_loading || _error is not null || !_grid.TryGetItemAt(x, y, out var mod, out _))
            {
                return null;
            }

            return Tooltip(mod);
        }

        private string Tooltip(NexusMod mod)
        {
            var summary = Ui.PlainText(mod.Summary);
            var name = ModDetailView.Clean(mod.Name);
            var text = summary.Length > 0 ? $"{name}\n{summary}" : name;
            if (_installed.TryGetValue(mod.ModId, out var installed))
            {
                var version = installed.Version.Length > 0 ? $" v{installed.Version}" : "";
                text += installed.Update is { } update ? $"\n\nInstalled{version}. Update available: v{update}" : $"\n\nInstalled{version}.";
            }
            return text;
        }

        /// <summary>The tag over a card: Installed, or Update when a new version of the installed mod was found.</summary>
        private (string Text, Color Color)? Tag(NexusMod mod)
        {
            if (!_installed.TryGetValue(mod.ModId, out var installed))
            {
                return null;
            }

            return installed.Update is not null ? ("Update", Ui.Attention) : ("Installed", Ui.Good);
        }

        // ---------- drawing ----------

        public override void Draw(SpriteBatch b)
        {
            if (_detail is not null)
            {
                _detail.Draw(b);
                return;
            }

            _search.Draw(b);
            _searchButton.Draw(b);
            foreach (var button in _sortButtons)
            {
                button.Draw(b);
            }

            if (_loading)
            {
                DrawLoading(b);
            }
            else if (_error is not null)
            {
                DrawError(b, _error);
            }
            else
            {
                _grid.Draw(b, DrawCard);
            }

            DrawStrip(b);
        }

        private void DrawLoading(SpriteBatch b)
        {
            // Measure with all three dots so the word doesn't shift as they animate
            var dots = (int)(Environment.TickCount64 / 400 % 4);
            var size = Ui.Small.MeasureString("Loading...");
            var area = _grid.Bounds;
            Ui.Label(b, "Loading" + new string('.', dots), new Vector2(area.Center.X - size.X / 2, area.Center.Y - size.Y / 2), Ui.Muted);
        }

        private void DrawError(SpriteBatch b, string error)
        {
            var area = _grid.Bounds;
            var lineHeight = Ui.LineHeight();
            var maxLines = (_retry.Bounds.Y - 8 - area.Y) / lineHeight;
            if (maxLines < 1)
            {
                // Too short to stack: one line to the left of the button
                var beside = Ui.Ellipsize(error, _retry.Bounds.X - 24 - area.X);
                var besideWidth = Ui.Small.MeasureString(beside).X;
                Ui.Label(b, beside, new Vector2(_retry.Bounds.X - 24 - besideWidth, _retry.Bounds.Center.Y - lineHeight / 2), Ui.Bad);
                _retry.Draw(b);
                return;
            }

            var lines = Ui.Wrap(error, area.Width - 80).Split('\n').Select(l => l.TrimEnd('\r', ' ')).Take(maxLines).ToList();
            var y = _retry.Bounds.Y - 8 - lines.Count * lineHeight;
            foreach (var line in lines)
            {
                var text = Ui.Ellipsize(line, area.Width - 40);
                var width = Ui.Small.MeasureString(text).X;
                Ui.Label(b, text, new Vector2(area.Center.X - width / 2, y), Ui.Bad);
                y += lineHeight;
            }

            _retry.Draw(b);
        }

        private void DrawStrip(SpriteBatch b)
        {
            var lineHeight = Ui.LineHeight();
            var textY = _strip.Center.Y - lineHeight / 2;

            string results;
            if (_totalCount < 0)
            {
                results = _loading ? "Searching..." : "";
            }
            else
            {
                results = $"{_totalCount.ToString("N0", CultureInfo.InvariantCulture)} result{(_totalCount == 1 ? "" : "s")}";
                if (_query.Length > 0)
                {
                    results += $" for \"{ModDetailView.Clean(_query)}\"";
                }
            }
            Ui.Label(b, Ui.Ellipsize(results, _prevArrow.X - 24 - _strip.X), new Vector2(_strip.X, textY), Ui.Muted);

            var pageText = _totalCount < 0 ? $"Page {_page + 1}" : $"Page {_page + 1} of {PageCount}";
            Ui.LabelCentered(b, pageText, new Rectangle(_prevArrow.Right, _strip.Y, _nextArrow.X - _prevArrow.Right, _strip.Height));

            DrawArrow(b, Ui.ArrowLeft, _prevArrow, CanGoBack, _prevHovered);
            DrawArrow(b, Ui.ArrowRight, _nextArrow, CanGoForward, _nextHovered);
        }

        /// <summary>A page arrow that grows a little on hover, like vanilla's, and dims when there's no page that way.</summary>
        private static void DrawArrow(SpriteBatch b, Rectangle source, Rectangle bounds, bool enabled, bool hovered)
        {
            var scale = hovered ? 4.4f : 4f;
            var position = new Vector2(bounds.Center.X - source.Width * scale / 2, bounds.Center.Y - source.Height * scale / 2);
            Ui.Sprite(b, source, position, scale, enabled ? Color.White : Color.White * 0.35f);
        }

        private void DrawCard(SpriteBatch b, NexusMod mod, Rectangle cell, bool hovered)
        {
            Ui.Panel(b, cell);
            if (hovered)
            {
                Ui.Fill(b, new Rectangle(cell.X + 8, cell.Y + 8, cell.Width - 16, cell.Height - 16), Ui.Hover);
            }

            // The panel's border is 12px deep and its corners bevel in to 20px, so the label keeps 24px from each
            // side and a border's depth above and below its line
            var tagWidth = (int)Ui.Small.MeasureString("Installed").X + 48;
            var tagHeight = Ui.LineHeight() + 24;
            Rectangle picture;
            Rectangle tag;
            int textX, textY, textWidth;
            if (cell.Height >= SideCardHeight)
            {
                // Tall card: picture on top, tag over its top-right corner
                picture = new Rectangle(cell.X + 12, cell.Y + 12, cell.Width - 24, cell.Height - 12 - CardTextHeight);
                tag = new Rectangle(cell.Right - 8 - tagWidth, cell.Y + 8, tagWidth, tagHeight);
                textX = picture.X + 4;
                textY = picture.Bottom + 6;
                textWidth = picture.Width - 8;
            }
            else
            {
                // Short card: picture on the left with the tag along its bottom, so the name keeps the full text width
                var height = Math.Max(0, cell.Height - 24);
                var width = Math.Min(Math.Max(cell.Width / 3, tagWidth + 8), height * 16 / 9);
                picture = new Rectangle(cell.X + 12, cell.Y + 12, width, height);
                tag = new Rectangle(picture.Center.X - tagWidth / 2, picture.Bottom + 4 - tagHeight, tagWidth, tagHeight);
                textX = picture.Right + 12;
                textY = cell.Y + 10;
                textWidth = Math.Max(40, cell.Right - 16 - textX);
            }

            if (picture.Height > 8)
            {
                DrawPicture(b, mod.ThumbnailUrl ?? mod.PictureUrl, picture);
            }

            // Lines that don't fit a very short card are left out rather than drawn over the frame
            var lineHeight = Ui.LineHeight();
            var bottom = cell.Bottom - 6;
            if (textY + lineHeight <= bottom)
            {
                Ui.Label(b, Ui.Ellipsize(ModDetailView.Clean(mod.Name), textWidth), new Vector2(textX, textY), Ui.Text);
                textY += lineHeight - 4;
            }
            if (textY + lineHeight <= bottom)
            {
                Ui.Label(b, Ui.Ellipsize($"by {ModDetailView.Clean(mod.Author ?? "unknown")}", textWidth), new Vector2(textX, textY), Ui.Muted);
                textY += lineHeight - 4;
            }
            if (textY + lineHeight <= bottom)
            {
                DrawStats(b, mod, textX, textY, textWidth);
            }

            // Sized for "Installed", the longer label, so cards keep their layout either way
            if (Tag(mod) is { } label && tag.X >= cell.X && tag.Bottom <= cell.Bottom)
            {
                Ui.Panel(b, tag);
                Ui.LabelCentered(b, label.Text, tag, label.Color);
            }
        }

        private static void DrawStats(SpriteBatch b, NexusMod mod, int x, int y, int width)
        {
            Ui.Sprite(b, Ui.Heart, new Vector2(x, y + 8), 3f);
            var textX = x + Ui.Heart.Width * 3 + 8;
            var available = x + width - textX;

            var endorsements = Ui.Count(mod.Endorsements);
            var text = $"{endorsements}   {Ui.Count(mod.Downloads)} downloads";
            if (Ui.Small.MeasureString(text).X > available)
            {
                text = $"{endorsements}   {Ui.Count(mod.Downloads)}";
            }
            Ui.Label(b, Ui.Ellipsize(text, available), new Vector2(textX, y), Ui.Muted);
        }

        private static void DrawPicture(SpriteBatch b, string? url, Rectangle area)
        {
            var texture = ModEntry.Thumbnails.Get(url);
            if (texture is null)
            {
                var gone = url is null || ModEntry.Thumbnails.IsFailed(url);
                var missing = Ui.Small.MeasureString("No picture").X <= area.Width - 16 ? "No picture" : "";
                Ui.PicturePlaceholder(b, area, gone ? missing : "...");
                return;
            }

            Ui.Picture(b, texture, area, cover: true);
        }
    }
}
