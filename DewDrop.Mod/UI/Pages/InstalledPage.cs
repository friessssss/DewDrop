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

        /// <summary>The trash can is drawn at half the inventory's size, which fits the slimmer rows.</summary>
        private const float TrashScale = 2f;
        private const int TrashWidth = 44;

        private readonly TextInput _search = new("Search installed mods");
        private readonly UiButton _filterButton;
        private readonly UiButton _checkUpdates;
        private readonly UiButton _rescan;
        private readonly ScrollList<InstalledMod> _list = new(RowHeight);
        private readonly Dictionary<InstalledMod, UiButton> _updateButtons = new();
        private readonly Dictionary<InstalledMod, UiButton> _undoButtons = new();

        /// <summary>Where each row's trash can was last drawn, for clicks and hover.</summary>
        private readonly Dictionary<InstalledMod, Rectangle> _trashCans = new();
        private InstalledMod? _hoveredTrash;
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
            _rescan = new UiButton("Rescan", Rescan)
            {
                Tooltip = "Look through your Mods folder again, for mods you added or removed while the game was running."
            };
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
            _rescan.AtRight(_checkUpdates.Bounds.X - 12, bounds.Y - 4);

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
                items = items.Where(m => Matches(m, query));
            }

            items = _filter switch
            {
                Filter.Enabled => items.Where(mods.IsEnabled),
                Filter.Disabled => items.Where(m => !mods.IsEnabled(m)),
                Filter.Updates => items.Where(m => HasNewUpdate(m)),
                Filter.Problems => items.Where(m => (mods.IsEnabled(m) && mods.MissingDependencies(m).Count > 0) || mods.OtherLooseCopies(m).Count > 0),
                _ => items
            };

            // Uninstalled mods stay listed in every view, with Undo, until the game closes and their folders go, so a
            // row doesn't vanish from the filter it was uninstalled in
            var removing = mods.Removing.Where(m => query.Length == 0 || Matches(m, query)).ToList();
            if (removing.Count > 0)
            {
                items = items.Concat(removing).OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase);
            }

            _list.SetItems(items, resetScroll);
            _listVersion = mods.Installed.GetHashCode();
            _updateButtons.Clear();
            _undoButtons.Clear();
            _trashCans.Clear();
        }

        private static bool Matches(InstalledMod mod, string query)
        {
            return mod.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || mod.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
                || mod.UniqueId.Contains(query, StringComparison.OrdinalIgnoreCase);
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

        /// <summary>Picks up mods copied into (or deleted from) the Mods folder by hand since the game started.</summary>
        private void Rescan()
        {
            if (ModEntry.Downloads.Jobs.FirstOrDefault(j => j.State == DownloadState.Installing) is { } installing)
            {
                ModEntry.Toast($"{installing.Title} is still installing. Rescan when it's done.");
                return;
            }

            var mods = ModEntry.Mods;
            var (added, removed) = mods.Rescan();
            Refresh();

            static string Names(IReadOnlyList<string> names) =>
                string.Join(", ", names.Take(3)) + (names.Count > 3 ? $" and {names.Count - 3} more" : "");

            if (added.Count == 0 && removed.Count == 0)
            {
                ModEntry.Toast("No changes: your Mods folder has the same mods as before.");
                return;
            }

            if (added.Count > 0)
            {
                // Mods copied in by hand aren't in the profile yet, so they'd stay off after a restart; "Turn on mods
                // when installed" covers them like a download
                if (ModEntry.Config.EnableModsOnInstall)
                {
                    if (mods.TurnOnUnseen(added).Count > 0)
                    {
                        Refresh();
                    }
                }
                else
                {
                    mods.MarkSeen(added.Select(m => m.UniqueId));
                }

                var off = added.Count(m => !mods.IsEnabled(m));
                var them = added.Count == 1 ? "it" : "them";
                var next = off > 0 ? $" Turn {them} on, then restart to load {them}." : $" Restart to load {them}.";
                ModEntry.Toast($"Found {added.Count} new mod{(added.Count == 1 ? "" : "s")}: {Names(added.Select(m => m.Name).ToList())}.{next}");
            }

            if (removed.Count > 0)
            {
                ModEntry.Toast($"{removed.Count} mod{(removed.Count == 1 ? " is" : "s are")} no longer in your Mods folder: {Names(removed)}.");
            }
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
            if (_filterButton.TryClick(x, y) || _checkUpdates.TryClick(x, y) || _rescan.TryClick(x, y) || _list.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (!_list.TryGetItemAt(x, y, out var mod, out _))
            {
                return;
            }

            if (_undoButtons.TryGetValue(mod, out var undo) && undo.TryClick(x, y))
            {
                return;
            }

            // An uninstalled mod's row only offers Undo; its checkbox is gone
            if (ModEntry.Mods.Removing.Contains(mod))
            {
                ModEntry.Toast($"{mod.Name} is uninstalled. Click Undo to keep it.");
                Game1.playSound("cancel");
                return;
            }

            if (_trashCans.TryGetValue(mod, out var trash) && trash.Contains(x, y))
            {
                UninstallPrompt.Show(Menu, new[] { mod }, onDone: () => Refresh());
                return;
            }

            if (_updateButtons.TryGetValue(mod, out var update) && update.TryClick(x, y))
            {
                return;
            }

            Toggle(mod);
        }

        private void Undo(InstalledMod mod)
        {
            var kept = ModEntry.Mods.Undo(mod, out var message);
            ModEntry.Toast(message, error: !kept);
            Refresh();
        }

        /// <summary>Rows that get a trash can: not DewDrop itself, and not a copy a Stardrop collection owns.</summary>
        private static bool OffersUninstall(InstalledMod mod) => mod.SourceId is null && !ModEntry.Mods.IsLocked(mod);

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
            _rescan.Hover(x, y);
            _list.Hover(x, y);
            foreach (var button in _updateButtons.Values.Concat(_undoButtons.Values))
            {
                button.Hover(x, y);
            }

            _hoveredTrash = null;
            if (_rescan.HoverText(x, y) is { } rescanTip)
            {
                return rescanTip;
            }

            if (_notice is { } notice && _noticeBanner.Contains(x, y) && Ui.Small.MeasureString(notice.Text).X > _noticeBanner.Width)
            {
                return notice.Text;
            }

            if (!_list.TryGetItemAt(x, y, out var mod, out _))
            {
                return null;
            }

            if (_undoButtons.TryGetValue(mod, out var undo) && undo.Contains(x, y))
            {
                return undo.Tooltip;
            }

            if (_trashCans.TryGetValue(mod, out var trash) && trash.Contains(x, y))
            {
                _hoveredTrash = mod;
                return ModEntry.Mods.UninstallBlockedReason(mod) ?? $"Uninstall {mod.Name}. Its folder is deleted when the game closes.";
            }

            if (_updateButtons.TryGetValue(mod, out var update) && update.Contains(x, y))
            {
                return update.Tooltip;
            }

            return Tooltip(mod);
        }

        private static string Tooltip(InstalledMod mod)
        {
            var mods = ModEntry.Mods;
            var lines = new List<string> { mod.Name };
            if (mods.Removing.Contains(mod))
            {
                lines.Add("Uninstalled. Its folder is deleted when the game closes.");
            }
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

            // SMAPI loads neither copy, so the player needs to know where the other one is to remove it
            foreach (var other in mods.OtherLooseCopies(mod))
            {
                lines.Add($"Also installed in: {other.Folder.Name}{(other.Version.Length > 0 ? $" (v{other.Version})" : "")}");
            }
            return string.Join("\n", lines);
        }

        public override void Draw(SpriteBatch b)
        {
            var mods = ModEntry.Mods;
            _search.Draw(b);
            _filterButton.Draw(b);
            _checkUpdates.Draw(b);
            _rescan.Draw(b);

            var enabledCount = mods.Installed.Count(mods.IsEnabled);
            var summary = $"{mods.Installed.Count} installed, {enabledCount} on";

            // The uninstalled count only when it fits; the rows themselves say so too
            var longer = $"{summary}, {mods.Removing.Count} to delete";
            if (mods.Removing.Count > 0 && _rescan.Bounds.X - 24 - Ui.Small.MeasureString(longer).X > _filterButton.Bounds.Right + 16)
            {
                summary = longer;
            }
            var summarySize = Ui.Small.MeasureString(summary);
            var summaryX = _rescan.Bounds.X - 24 - summarySize.X;
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
            if (mods.Removing.Contains(mod))
            {
                DrawRemovingRow(b, mod, cell);
                return;
            }

            var enabled = mods.IsEnabled(mod);
            var locked = mods.IsLocked(mod) || !mods.CanEditActiveProfile;

            Ui.Checkbox(b, new Vector2(cell.X + 12, cell.Center.Y - Ui.CheckboxSize / 2), enabled, !locked);

            // Right-hand side: the trash can in a column of its own (left empty where there's none, so they line up),
            // then the update button and status notes, measured first so the name can use the rest
            var right = cell.Right - 12;
            if (OffersUninstall(mod) && cell.Height >= Ui.TrashCan.Height * TrashScale)
            {
                var trash = new Rectangle(right - TrashWidth, cell.Center.Y - (int)(Ui.TrashCan.Height * TrashScale) / 2, TrashWidth, (int)(Ui.TrashCan.Height * TrashScale));
                _trashCans[mod] = trash;
                Ui.TrashCanIcon(b, new Vector2(trash.X + (TrashWidth - Ui.TrashCan.Width * TrashScale) / 2, trash.Y), TrashScale, open: _hoveredTrash == mod);
            }
            else
            {
                _trashCans.Remove(mod);
            }
            right -= TrashWidth + 16;

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
            DrawDetails(b, mod, cell, textX, textWidth);
        }

        /// <summary>An uninstalled mod waiting for the game to close: no checkbox, a note, and Undo.</summary>
        private void DrawRemovingRow(SpriteBatch b, InstalledMod mod, Rectangle cell)
        {
            _trashCans.Remove(mod);
            _updateButtons.Remove(mod);
            if (!_undoButtons.TryGetValue(mod, out var undo))
            {
                undo = new UiButton("Undo", () => Undo(mod))
                {
                    Tooltip = "Keep this mod. If uninstalling turned it off, it's turned back on."
                };
                _undoButtons[mod] = undo;
            }

            var right = cell.Right - 12;
            undo.AtRight(right, cell.Center.Y - undo.PreferredHeight / 2);
            undo.Draw(b);
            right = undo.Bounds.X - 16;

            const string note = "Deleted when the game closes";
            var size = Ui.Small.MeasureString(note);
            Ui.Label(b, note, new Vector2(right - size.X, cell.Center.Y - size.Y / 2), Ui.Attention);
            right -= (int)size.X + 16;

            var compact = cell.Height < RowHeight;
            var textX = cell.X + 12 + Ui.CheckboxSize + 20;
            var textWidth = Math.Max(80, right - textX);
            Ui.Label(b, Ui.Ellipsize(mod.Name, textWidth), new Vector2(textX, cell.Y + (compact ? 2 : 8)), Ui.Muted);
            DrawDetails(b, mod, cell, textX, textWidth);
        }

        private static void DrawDetails(SpriteBatch b, InstalledMod mod, Rectangle cell, int textX, int textWidth)
        {
            var compact = cell.Height < RowHeight;
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

            // SMAPI skips every copy of a mod that's in the Mods folder twice; the tooltip says where the other one is
            if (mods.OtherLooseCopies(mod).Count > 0)
            {
                return ("Installed twice", Ui.Bad);
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

            // A Steam or shortcut launch: on in the profile, not loaded, and SMAPI didn't reject it
            if (mods.Kind == SessionKind.Plain && enabled && mods.Difference().NotLoaded.Any(m => m.UniqueId.Equals(mod.UniqueId, StringComparison.OrdinalIgnoreCase)))
            {
                return ("On after restart", Ui.Attention);
            }

            return null;
        }
    }
}
