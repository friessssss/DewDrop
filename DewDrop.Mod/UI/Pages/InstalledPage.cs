using DewDrop.Core.Models;
using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>Installed mods with on/off checkboxes for the active profile, search, filters and update buttons.</summary>
    internal class InstalledPage : Page
    {
        private enum Filter
        {
            All,
            Enabled,
            Disabled,
            Updates,
            Problems
        }

        private const int RowHeight = 76;
        private const int CompactRowHeight = 64;
        private const string ReadOnlyNotice = "This collection profile is read-only. Duplicate it on the Profiles tab to change it.";

        private readonly TextInput _search = new("Search installed mods");
        private readonly UiButton _filterButton;
        private readonly UiButton _checkUpdates;
        private readonly ScrollList<InstalledMod> _list = new(RowHeight);
        private readonly Dictionary<InstalledMod, UiButton> _updateButtons = new();
        private Filter _filter = Filter.All;
        private int _listVersion = -1;
        private Rectangle _noticeBanner;

        /// <summary>
        /// The notice the layout made room for (null for none). The active profile, and so the notice, can change while
        /// the menu is open.
        /// </summary>
        private (string Text, Color Color)? _notice;

        public InstalledPage(DewDropMenu menu) : base(menu)
        {
            _filterButton = new UiButton("Show: All", CycleFilter);
            _checkUpdates = new UiButton("Check for updates", CheckForUpdates);
            _search.OnChanged = _ => Refresh(resetScroll: true);
            _list.EmptyText = "No mods match.";
        }

        public override string Title => "Installed";

        public override string? Badge
        {
            get
            {
                var updates = ModEntry.Mods.Installed.Count(ModEntry.Mods.HasNewUpdate);
                return updates > 0 ? $"{updates} new" : null;
            }
        }

        public override bool IsTyping => _search.Selected;

        /// <summary>An update was found and isn't already downloaded and waiting for a restart.</summary>
        private static bool HasNewUpdate(InstalledMod mod) => ModEntry.Mods.HasNewUpdate(mod);

        /// <summary>The line under the toolbar: a problem with the settings or profile, or why the checkboxes are locked.</summary>
        private static (string Text, Color Color)? CurrentNotice()
        {
            var mods = ModEntry.Mods;
            if (mods.Problem is { } problem)
            {
                return (problem, Ui.Bad);
            }

            return mods.ActiveProfile.IsReadOnly ? (ReadOnlyNotice, Ui.Attention) : null;
        }

        private bool NoticeChanged() => CurrentNotice()?.Text != _notice?.Text;

        public override void Layout(Rectangle bounds)
        {
            base.Layout(bounds);
            _search.Bounds = new Rectangle(bounds.X, bounds.Y, Math.Min(420, bounds.Width / 3), 48);
            _filterButton.At(_search.Bounds.Right + 16, bounds.Y - 4);
            _filterButton.Bounds.Width = 220;
            _checkUpdates.AtRight(bounds.Right, bounds.Y - 4);

            // A collection profile gets a line under the toolbar saying why the checkboxes are locked, and a profile
            // or settings file that couldn't be read one saying what's off
            var listTop = bounds.Y + 72;
            _notice = CurrentNotice();
            _noticeBanner = Rectangle.Empty;
            if (_notice is not null)
            {
                _noticeBanner = new Rectangle(bounds.X, bounds.Y + 62, bounds.Width, Ui.LineHeight());
                listTop = _noticeBanner.Bottom + 8;
            }

            // Short screens (720p) get slimmer rows when that fits another one in
            var listHeight = bounds.Bottom - listTop;
            _list.RowHeight = RowsThatFit(listHeight, CompactRowHeight) > RowsThatFit(listHeight, RowHeight) ? CompactRowHeight : RowHeight;
            _list.Layout(new Rectangle(bounds.X, listTop, bounds.Width, listHeight));
        }

        private int RowsThatFit(int height, int rowHeight) => (height + _list.RowGap) / (rowHeight + _list.RowGap);

        public override void OnShow()
        {
            if (NoticeChanged())
            {
                Layout(Bounds);
            }

            Refresh();
        }

        public override void OnHide()
        {
            _search.Selected = false;
        }

        /// <summary>Rebuilds the visible list from the installed mods, filter and search.</summary>
        private void Refresh(bool resetScroll = false)
        {
            var mods = ModEntry.Mods;
            var query = _search.Text.Trim();
            IEnumerable<InstalledMod> items = mods.Installed;

            if (query.Length > 0)
            {
                items = items.Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || m.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || m.UniqueId.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            items = _filter switch
            {
                Filter.Enabled => items.Where(mods.IsEnabled),
                Filter.Disabled => items.Where(m => !mods.IsEnabled(m)),
                Filter.Updates => items.Where(m => HasNewUpdate(m)),
                Filter.Problems => items.Where(m => mods.IsEnabled(m) && mods.MissingDependencies(m).Count > 0),
                _ => items
            };

            _list.SetItems(items, resetScroll);
            _listVersion = mods.Installed.GetHashCode();
            _updateButtons.Clear();
        }

        private void CycleFilter()
        {
            _filter = (Filter)(((int)_filter + 1) % Enum.GetValues<Filter>().Length);
            _filterButton.Label = $"Show: {_filter}";
            Refresh(resetScroll: true);
        }

        private void CheckForUpdates()
        {
            var mods = ModEntry.Mods;
            if (mods.UpdateCheckRunning)
            {
                return;
            }

            mods.CheckForUpdates(
                _ =>
                {
                    // Count what the tab badge shows: updates already downloaded and waiting for a restart aren't new
                    var count = mods.Installed.Count(mods.HasNewUpdate);
                    ModEntry.Toast(count == 0 ? "Everything's up to date." : $"{count} update{(count == 1 ? "" : "s")} available.");
                    Refresh();
                },
                // A failed or empty smapi.io reply ends here; ModManager only logs it, so this is the one toast
                ex => ModEntry.Toast(ex switch
                {
                    TaskCanceledException => "smapi.io took too long to answer, so the update check didn't finish.",
                    HttpRequestException => "Couldn't reach smapi.io to check for updates.",
                    _ => Ui.PlainText(ex.Message) is { Length: > 0 } message ? message : "The update check didn't finish."
                }, error: true));
        }

        public override void Update(GameTime time)
        {
            _search.Tick();

            // The installed list object is replaced on rescan (e.g. after a download finishes)
            if (ModEntry.Mods.Installed.GetHashCode() != _listVersion)
            {
                Refresh();
            }

            // The active profile can change on the Profiles tab or after an install
            if (NoticeChanged())
            {
                Layout(Bounds);
            }

            _checkUpdates.Enabled = !ModEntry.Mods.UpdateCheckRunning;
            _checkUpdates.Label = ModEntry.Mods.UpdateCheckRunning ? "Checking..." : "Check for updates";
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            _search.Click(x, y);
            if (_filterButton.TryClick(x, y) || _checkUpdates.TryClick(x, y) || _list.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (!_list.TryGetItemAt(x, y, out var mod, out _))
            {
                return;
            }

            if (_updateButtons.TryGetValue(mod, out var update) && update.TryClick(x, y))
            {
                return;
            }

            Toggle(mod);
        }

        private void Toggle(InstalledMod mod)
        {
            var mods = ModEntry.Mods;
            if (mods.IsLocked(mod))
            {
                ModEntry.Toast("DewDrop has to stay on so this menu is here next time.");
                Game1.playSound("cancel");
                return;
            }

            if (mods.ActiveProfile.IsReadOnly)
            {
                ModEntry.Toast("Collection profiles can't be edited. Duplicate it on the Profiles tab first.", error: true);
                Game1.playSound("cancel");
                return;
            }

            if (mods.ActiveProfileUnreadable)
            {
                ModEntry.Toast($"The '{mods.ActiveProfile.Name}' profile file couldn't be read, so DewDrop won't change it.", error: true);
                Game1.playSound("cancel");
                return;
            }

            var enable = !mods.IsEnabled(mod);
            var alsoEnabled = mods.SetEnabled(mod, enable);
            Game1.playSound(enable ? "coin" : "drumkit6");
            if (alsoEnabled.Count > 0)
            {
                ModEntry.Toast($"Also turned on what it needs: {string.Join(", ", alsoEnabled)}");
            }

            if (_filter is Filter.Enabled or Filter.Disabled or Filter.Problems)
            {
                Refresh();
            }
        }

        public override void LeftClickHeld(int x, int y) => _list.LeftClickHeld(x, y);

        public override void ReleaseLeftClick(int x, int y) => _list.ReleaseLeftClick();

        public override void ReceiveScrollWheel(int direction) => _list.ReceiveScrollWheel(direction);

        public override void ReceiveKeyPress(Keys key)
        {
            if (_search.Selected)
            {
                return;
            }

            if (key == Keys.Down)
            {
                _list.ScrollBy(1);
            }
            else if (key == Keys.Up)
            {
                _list.ScrollBy(-1);
            }
        }

        public override string? PerformHover(int x, int y)
        {
            _search.Hover(x, y);
            _filterButton.Hover(x, y);
            _checkUpdates.Hover(x, y);
            _list.Hover(x, y);
            foreach (var button in _updateButtons.Values)
            {
                button.Hover(x, y);
            }

            if (_notice is { } notice && _noticeBanner.Contains(x, y) && Ui.Small.MeasureString(notice.Text).X > _noticeBanner.Width)
            {
                return notice.Text;
            }

            if (!_list.TryGetItemAt(x, y, out var mod, out _))
            {
                return null;
            }

            if (_updateButtons.TryGetValue(mod, out var update) && update.Contains(x, y))
            {
                return update.Tooltip;
            }

            return Tooltip(mod);
        }

        private static string Tooltip(InstalledMod mod)
        {
            var lines = new List<string> { mod.Name };
            var description = Ui.PlainText(mod.Description);
            if (description.Length > 0)
            {
                lines.Add(description);
            }
            lines.Add("");
            lines.Add($"ID: {mod.UniqueId}");
            if (mod.IsContentPack)
            {
                lines.Add($"Content pack for {mod.Manifest.ContentPackFor!.UniqueID}");
            }
            if (mod.SourceId is not null)
            {
                lines.Add($"From collection {mod.SourceId}");
            }
            lines.Add($"Folder: {mod.Folder.Name}");
            return string.Join("\n", lines);
        }

        public override void Draw(SpriteBatch b)
        {
            var mods = ModEntry.Mods;
            _search.Draw(b);
            _filterButton.Draw(b);
            _checkUpdates.Draw(b);

            var enabledCount = mods.Installed.Count(mods.IsEnabled);
            var summary = $"{mods.Installed.Count} installed, {enabledCount} on";
            var summarySize = Ui.Small.MeasureString(summary);
            var summaryX = _checkUpdates.Bounds.X - 24 - summarySize.X;
            if (summaryX > _filterButton.Bounds.Right + 16)
            {
                Ui.Label(b, summary, new Vector2(summaryX, Bounds.Y + 12), Ui.Muted);
            }

            if (_notice is { } notice)
            {
                Ui.Label(b, Ui.Ellipsize(notice.Text, _noticeBanner.Width), new Vector2(_noticeBanner.X, _noticeBanner.Y), notice.Color);
            }

            _list.Draw(b, DrawRow);
        }

        private void DrawRow(SpriteBatch b, InstalledMod mod, Rectangle cell, bool hovered)
        {
            var mods = ModEntry.Mods;
            var enabled = mods.IsEnabled(mod);
            var locked = mods.IsLocked(mod) || !mods.CanEditActiveProfile;

            Ui.Checkbox(b, new Vector2(cell.X + 12, cell.Center.Y - Ui.CheckboxSize / 2), enabled, !locked);

            // Right-hand side: update button, then status notes, measured first so the name can use the rest
            var right = cell.Right - 12;
            if (mods.HasNewUpdate(mod) && mod.Update is { } update)
            {
                if (!_updateButtons.TryGetValue(mod, out var button))
                {
                    button = new UiButton($"Update to {update.Version}", () => UpdateActions.Update(mod))
                    {
                        Color = Ui.Good,
                        Tooltip = ModEntry.Nexus.IsPremium ? "Download and install the new version." : "Open the new version on Nexus Mods."
                    };
                    _updateButtons[mod] = button;
                }

                button.AtRight(right, cell.Center.Y - button.PreferredHeight / 2);
                button.Draw(b);
                right = button.Bounds.X - 16;
            }
            else
            {
                // A button that isn't drawn mustn't keep catching clicks at its old position
                _updateButtons.Remove(mod);
            }

            var note = StatusNote(mod, enabled);
            if (note is { } n)
            {
                var size = Ui.Small.MeasureString(n.Text);
                Ui.Label(b, n.Text, new Vector2(right - size.X, cell.Center.Y - size.Y / 2), n.Color);
                right -= (int)size.X + 16;
            }

            // Compact rows pull the two lines closer together
            var compact = cell.Height < RowHeight;
            var textX = cell.X + 12 + Ui.CheckboxSize + 20;
            var textWidth = Math.Max(80, right - textX);
            var nameColor = enabled ? Ui.Text : Ui.Muted;
            Ui.Label(b, Ui.Ellipsize(mod.Name, textWidth), new Vector2(textX, cell.Y + (compact ? 2 : 8)), nameColor);

            var details = new List<string>();
            if (mod.Author.Length > 0)
            {
                details.Add($"by {mod.Author}");
            }
            if (mod.Version.Length > 0)
            {
                details.Add($"v{mod.Version}");
            }
            if (mod.IsContentPack)
            {
                details.Add("content pack");
            }
            if (mod.SourceId is not null)
            {
                details.Add("collection");
            }
            Ui.Label(b, Ui.Ellipsize(string.Join("  |  ", details), textWidth), new Vector2(textX, cell.Y + (compact ? 30 : 40)), Ui.Muted);
        }

        /// <summary>A short note about a problem or a pending change, in priority order.</summary>
        private static (string Text, Color Color)? StatusNote(InstalledMod mod, bool enabled)
        {
            var mods = ModEntry.Mods;
            if (mods.HasPendingUpdate(mod))
            {
                return ("Update ready, restart to finish", Ui.Good);
            }

            if (enabled)
            {
                var missing = mods.MissingDependencies(mod);
                if (missing.Count > 0)
                {
                    return ($"Needs {Ui.Ellipsize(string.Join(", ", missing), 260)}", Ui.Bad);
                }
            }

            // Nothing changes "after restart" while Apply & Restart is off (the banner says why)
            if (!mods.CanApply)
            {
                return null;
            }

            // Shown whatever folder SMAPI started from: the first Apply & Restart is where a profile that leaves out
            // what's loaded now surprises people. A copy whose twin (loose or collection) stays on isn't turned off.
            if (!enabled && mods.WillTurnOff(mod))
            {
                return ("Off after restart", Ui.Attention);
            }

            // Only known from a profile launch: mods that failed to load then are left out of the restart check
            if (mods.RunningFromProfileFolder && enabled && !mods.IsLoaded(mod))
            {
                return ("On after restart", Ui.Attention);
            }

            return null;
        }
    }
}
