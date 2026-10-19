using DewDrop.Core.Models;
using DewDrop.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>The profiles shared with Stardrop: pick, create, duplicate, rename and delete them, and see their mods.</summary>
    internal class ProfilesPage : Page
    {
        private const int ProfileRowHeight = 72;
        private const int ModRowHeight = 44;
        private const int ColumnGap = 40;
        private const int ButtonGap = 12;
        private const string Separator = "  |  ";

        private readonly TextInput _nameInput = new("New profile name");
        private readonly UiButton _create;
        private readonly UiButton _use;
        private readonly UiButton _duplicate;
        private readonly UiButton _rename;
        private readonly UiButton _delete;
        private readonly UiButton[] _actions;
        private readonly ScrollList<Profile> _profileList = new(ProfileRowHeight);
        private readonly ScrollList<ModRow> _modList = new(ModRowHeight);

        // Profiles are replaced whenever the mod manager reloads, so the selection is kept by name and these
        // snapshots only tell when the lists need rebuilding
        private string? _selectedName;
        private List<Profile> _shownProfiles = new();
        private Profile? _modsProfile;
        private string? _modsProfileName;
        private int _modsCount = -1;
        private List<InstalledMod>? _modsInstalled;
        private int _missingCount;

        private Rectangle _divider;
        private Rectangle _details;
        private bool _compactHeader;
        private bool _showModList;

        public ProfilesPage(DewDropMenu menu) : base(menu)
        {
            _create = new UiButton("Create", Create) { Tooltip = "Make an empty profile with this name. Only DewDrop starts turned on." };
            _use = new UiButton("Use this profile", UseSelected);
            _duplicate = new UiButton("Duplicate", DuplicateSelected);
            _rename = new UiButton("Rename", RenameSelected);
            _delete = new UiButton("Delete", DeleteSelected) { Color = Ui.Bad };
            _actions = new[] { _use, _duplicate, _rename, _delete };

            _nameInput.OnSubmit = _ => Create();
            _profileList.EmptyText = "No profiles yet.";
            _modList.EmptyText = "No mods are turned on in this profile.";

            // The mod rows are only a list to look at; clicking one does nothing
            _modList.HoverHighlight = false;
        }

        public override string Title => "Profiles";

        public override bool IsTyping => _nameInput.Selected;

        /// <summary>The profile shown on the right; falls back to the active one if the selection is gone.</summary>
        private Profile SelectedProfile => ModEntry.Mods.Profiles.Get(_selectedName) ?? ModEntry.Mods.ActiveProfile;

        private static bool IsActive(Profile profile) => profile.Name.Equals(ModEntry.Mods.ActiveProfile.Name, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Whether the profile's file exists but couldn't be read. The copy shown is only a stand-in (for Default, or
        /// the active profile), so DewDrop neither saves it nor copies it.
        /// </summary>
        private static bool IsUnreadable(Profile profile) => ModEntry.Mods.Profiles.IsUnreadable(profile.Name);

        public override void Layout(Rectangle bounds)
        {
            base.Layout(bounds);

            // Left column: name box and Create on top, the profile list under them
            var leftWidth = (int)(bounds.Width * 0.4f);
            _create.AtRight(bounds.X + leftWidth, bounds.Y);
            _nameInput.Bounds = new Rectangle(bounds.X, bounds.Y + 4, Math.Max(80, _create.Bounds.X - 16 - bounds.X), 48);
            var listTop = bounds.Y + 72;
            _profileList.Layout(new Rectangle(bounds.X, listTop, leftWidth, Math.Max(0, bounds.Bottom - listTop)));
            _divider = new Rectangle(bounds.X + leftWidth + ColumnGap / 2 - 1, bounds.Y, 2, bounds.Height);

            // Right column: name, status line, actions, then the profile's mods
            var detailsX = bounds.X + leftWidth + ColumnGap;
            _details = new Rectangle(detailsX, bounds.Y, bounds.Right - detailsX, bounds.Height);

            var nameHeight = Ui.LineHeight(Ui.Large);
            var fullHeader = nameHeight + 4 + Ui.LineHeight();
            var buttonHeight = _use.PreferredHeight;

            // Short windows put the status beside the name so the mod list keeps at least two rows
            _compactHeader = bounds.Height - (fullHeader + 12 + buttonHeight + 16) < ModRowHeight * 2 + 12;
            var buttonsY = bounds.Y + (_compactHeader ? nameHeight : fullHeader) + 12;
            LayoutActions(detailsX, buttonsY, _details.Width);

            var modsTop = buttonsY + buttonHeight + 16;
            var modsHeight = bounds.Bottom - modsTop;
            _showModList = modsHeight >= ModRowHeight;

            // ScrollList's arrows reach 4px past its right edge, so keep that inside Bounds
            _modList.Layout(new Rectangle(detailsX, modsTop, _details.Width - 4, Math.Max(0, modsHeight)));
        }

        /// <summary>Puts the action buttons in a row, shrinking them evenly when the column is too narrow.</summary>
        private void LayoutActions(int x, int y, int width)
        {
            var preferred = _actions.Sum(b => b.PreferredWidth);
            var room = width - ButtonGap * (_actions.Length - 1);
            var scale = preferred > room ? room / (float)preferred : 1f;
            foreach (var button in _actions)
            {
                button.At(x, y);
                button.Bounds.Width = (int)(button.PreferredWidth * scale);
                x = button.Bounds.Right + ButtonGap;
            }
        }

        public override void OnShow()
        {
            Refresh();
            RefreshButtons();

            // The active profile can be far down a long list; bring the selection into view
            if (_profileList.Selected is { } selected)
            {
                _profileList.ScrollTo(selected);
            }
        }

        public override void OnHide()
        {
            _nameInput.Selected = false;
        }

        /// <summary>Rebuilds the profile list from the store, keeping the selection by name.</summary>
        private void Refresh(bool resetScroll = false)
        {
            var store = ModEntry.Mods.Profiles;
            if (store.Get(_selectedName) is null)
            {
                _selectedName = ModEntry.Mods.ActiveProfile.Name;
            }

            // Same order as the store loads them in: Default first, collection profiles last, the rest by name
            static int Rank(Profile p) => p.Name == ProfileStore.DefaultProfileName ? 0 : p.IsFromCollection ? 2 : 1;
            var ordered = store.Profiles
                .OrderBy(Rank)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _profileList.SetItems(ordered, resetScroll);
            _profileList.Selected = store.Get(_selectedName);
            _shownProfiles = store.Profiles.ToList();
            RefreshMods();
        }

        /// <summary>Rebuilds the right-hand mod list for the selected profile; scrolls back up when that's a different profile.</summary>
        private void RefreshMods()
        {
            var mods = ModEntry.Mods;
            var profile = SelectedProfile;
            var resetScroll = profile.Name != _modsProfileName;

            var installed = new Dictionary<ModReference, InstalledMod>();
            foreach (var mod in mods.Installed)
            {
                installed.TryAdd(new ModReference(mod.UniqueId, mod.SourceId), mod);
            }

            var rows = (profile.EnabledModIds ?? new List<ModReference>())
                .Where(r => r is not null)
                .Select(r => new ModRow(r, installed.GetValueOrDefault(r)))
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _modList.SetItems(rows, resetScroll);
            _missingCount = rows.Count(r => r.Mod is null);
            _modsProfile = profile;
            _modsProfileName = profile.Name;
            _modsCount = ModIdCount(profile);
            _modsInstalled = mods.Installed;
        }

        /// <summary>Rebuilds whatever went stale: the store reloads (replacing every profile) after downloads and installs.</summary>
        private void SyncWithStore()
        {
            var mods = ModEntry.Mods;
            if (!_shownProfiles.SequenceEqual(mods.Profiles.Profiles))
            {
                Refresh();
                return;
            }

            var profile = SelectedProfile;
            if (!ReferenceEquals(profile, _modsProfile) || ModIdCount(profile) != _modsCount || !ReferenceEquals(mods.Installed, _modsInstalled))
            {
                RefreshMods();
            }
        }

        private void Select(string name)
        {
            _selectedName = name;
            _profileList.Selected = ModEntry.Mods.Profiles.Get(name);
            if (_profileList.Selected is { } profile)
            {
                _profileList.ScrollTo(profile);
            }
            RefreshMods();
        }

        public override void Update(GameTime time)
        {
            _nameInput.Tick();
            SyncWithStore();
            RefreshButtons();
        }

        /// <summary>Enables the action buttons for the selected profile and says why when one can't be used.</summary>
        private void RefreshButtons()
        {
            var profile = SelectedProfile;
            var active = IsActive(profile);

            _use.Enabled = !active;
            _use.Tooltip = active ? "This is already the active profile." : "Make this the active profile. Its mods load after a restart.";

            // What's shown for a file that couldn't be read is a stand-in or an older copy, not what the file holds
            var unreadable = IsUnreadable(profile);
            _duplicate.Enabled = !unreadable;
            _duplicate.Tooltip = unreadable ? "Its file couldn't be read, so DewDrop won't copy it."
                : profile.IsFromCollection
                    ? $"Make an editable copy of this collection profile, named from the box or '{profile.Name} copy'."
                    : $"Copy this profile, named from the box or '{profile.Name} copy'.";

            // Stardrop marks collection profiles as protected too, so the collection reason is checked first
            _rename.Enabled = !profile.IsProtected && !profile.IsFromCollection && !unreadable;
            _rename.Tooltip = profile.IsFromCollection ? "Collection profiles are read-only and can't be renamed. Duplicate it instead."
                : profile.IsProtected ? "This profile is protected and can't be renamed."
                : unreadable ? "Its file couldn't be read, so DewDrop won't rename it."
                : "Rename it to the name typed in the box.";

            _delete.Enabled = !profile.IsProtected && !profile.IsFromCollection && !active;
            _delete.Tooltip = profile.IsFromCollection ? "Collection profiles come from Stardrop and can't be deleted here."
                : profile.IsProtected ? "This profile is protected and can't be deleted."
                : active ? "Switch to another profile first"
                : "Delete this profile. The mods themselves stay installed.";
        }

        // ---------- actions ----------

        private void Create()
        {
            var name = _nameInput.Text.Trim();
            if (!IsUsableName(name))
            {
                return;
            }

            var mods = ModEntry.Mods;
            var profile = mods.Profiles.Create(name);
            EnsureSelfEnabled(profile);

            _nameInput.Clear();
            Refresh();
            Select(profile.Name);
            Game1.playSound("newArtifact");
            ModEntry.Toast($"Created '{profile.Name}'. Use it, then pick its mods on the Installed tab.");
        }

        private void UseSelected()
        {
            var profile = SelectedProfile;
            if (IsActive(profile))
            {
                return;
            }

            ModEntry.Mods.SwitchProfile(profile);
            Game1.playSound("coin");
            ModEntry.Toast(ModEntry.Mods.NeedsRestart()
                ? $"Switched to {profile.Name}. Restart to load its mods."
                : $"Switched to {profile.Name}. Its mods are already loaded.");
        }

        private void DuplicateSelected()
        {
            var source = SelectedProfile;
            if (IsUnreadable(source))
            {
                return;
            }

            var typed = _nameInput.Text.Trim();
            string name;
            if (typed.Length > 0)
            {
                if (!IsUsableName(typed))
                {
                    return;
                }
                name = typed;
            }
            else
            {
                name = FreeCopyName(source.Name);
            }

            // The copy never inherits the collection link, so it's a plain editable profile
            var copy = ModEntry.Mods.Profiles.Create(name, copyFrom: source);
            EnsureSelfEnabled(copy);

            _nameInput.Clear();
            Refresh();
            Select(copy.Name);
            Game1.playSound("newArtifact");
            ModEntry.Toast($"Made '{copy.Name}' from '{source.Name}'.");
        }

        private void RenameSelected()
        {
            var profile = SelectedProfile;
            if (profile.IsProtected || profile.IsFromCollection || IsUnreadable(profile))
            {
                return;
            }

            var name = _nameInput.Text.Trim();
            if (name.Length == 0)
            {
                Reject("Type the new name in the box first.");
                return;
            }
            // Profile files can't tell capitals apart, so a case-only change counts as the same name
            if (name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase))
            {
                Reject("It already has that name.");
                return;
            }
            if (!IsUsableName(name))
            {
                return;
            }

            var oldName = profile.Name;
            var wasActive = IsActive(profile);
            string? error = null;
            try
            {
                if (!ModEntry.Mods.Profiles.Rename(profile, name))
                {
                    error = $"Couldn't rename '{oldName}'.";
                }
            }
            catch (Exception ex)
            {
                // The new file may be written already even though removing the old one failed
                error = $"Couldn't finish renaming '{oldName}': {ex.Message}";
            }

            // Keeps Settings.LastSelectedProfileName pointing at the new name
            if (wasActive && profile.Name == name)
            {
                ModEntry.Mods.SwitchProfile(profile);
            }

            // Still the same profile on the right, so its mod list keeps its scroll position
            _selectedName = profile.Name;
            _modsProfileName = profile.Name;
            Refresh();
            if (error is not null)
            {
                Reject(error);
                return;
            }

            _nameInput.Clear();
            _profileList.ScrollTo(profile);
            Game1.playSound("coin");
            ModEntry.Toast($"Renamed '{oldName}' to '{profile.Name}'.");
        }

        private void DeleteSelected()
        {
            var profile = SelectedProfile;
            if (profile.IsProtected || profile.IsFromCollection || IsActive(profile))
            {
                return;
            }

            var name = profile.Name;
            Menu.Confirm($"Delete the profile '{name}'?\n\nStardrop shares this profile, so it will be gone there too. The mods themselves stay installed.", "Delete", () =>
            {
                // The store may have reloaded while the dialog was open, so look the profile up again
                var target = ModEntry.Mods.Profiles.Get(name);
                if (target is null || target.IsProtected || target.IsFromCollection || IsActive(target))
                {
                    return;
                }

                try
                {
                    if (!ModEntry.Mods.Profiles.Delete(target))
                    {
                        Reject($"Couldn't delete '{name}'.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Reject($"Couldn't delete '{name}': {ex.Message}");
                    Refresh();
                    return;
                }

                Game1.playSound("trashcan");
                _selectedName = ModEntry.Mods.ActiveProfile.Name;
                Refresh();
                _profileList.ScrollTo(SelectedProfile);
                ModEntry.Toast($"Deleted '{name}'.");
            });
        }

        /// <summary>DewDrop has to stay on in every profile, or the menu would be gone after a restart.</summary>
        private static void EnsureSelfEnabled(Profile profile)
        {
            var mods = ModEntry.Mods;
            if (!profile.EnabledModIds.Any(r => r is not null && r.UniqueId.Equals(mods.SelfId, StringComparison.OrdinalIgnoreCase)))
            {
                profile.EnabledModIds.Add(new ModReference(mods.SelfId));
                mods.Profiles.Save(profile);
            }
        }

        /// <summary>Checks a typed profile name; toasts why it can't be used.</summary>
        private static bool IsUsableName(string name)
        {
            if (name.Length == 0)
            {
                Reject("Type a name for the profile first.");
                return false;
            }

            var invalid = Path.GetInvalidFileNameChars();
            if (name.All(c => Array.IndexOf(invalid, c) >= 0))
            {
                Reject("That name has no characters a file name can use.");
                return false;
            }

            var store = ModEntry.Mods.Profiles;
            if (store.IsNameTaken(name))
            {
                // The store compares file names, where characters like ? and / all become _
                var clash = store.Profiles.FirstOrDefault(p => FileKey(p.Name).Equals(FileKey(name), StringComparison.OrdinalIgnoreCase));
                Reject(clash is null || clash.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    ? $"Another profile already uses the name '{name}'."
                    : $"'{name}' would be saved in the same file as '{clash.Name}'. Pick another name.");
                return false;
            }

            return true;
        }

        /// <summary>The profile's file name the way ProfileStore builds it, without the extension.</summary>
        private static string FileKey(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
        }

        /// <summary>"Name copy", then "Name copy 2", "Name copy 3"... until one is free.</summary>
        private static string FreeCopyName(string name)
        {
            var store = ModEntry.Mods.Profiles;
            var candidate = $"{name} copy";
            for (var i = 2; store.IsNameTaken(candidate); i++)
            {
                candidate = $"{name} copy {i}";
            }

            return candidate;
        }

        private static void Reject(string message)
        {
            ModEntry.Toast(message, error: true);
            Game1.playSound("cancel");
        }

        // ---------- input ----------

        public override void ReceiveLeftClick(int x, int y)
        {
            SyncWithStore();
            _nameInput.Click(x, y);
            if (_create.TryClick(x, y) || _actions.Any(b => b.TryClick(x, y)) || _profileList.ReceiveLeftClick(x, y))
            {
                return;
            }
            if (_showModList && _modList.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (_profileList.TryGetItemAt(x, y, out var profile, out _))
            {
                if (!profile.Name.Equals(_selectedName, StringComparison.OrdinalIgnoreCase))
                {
                    Game1.playSound("smallSelect");
                }
                Select(profile.Name);
            }
        }

        public override void LeftClickHeld(int x, int y)
        {
            _profileList.LeftClickHeld(x, y);
            _modList.LeftClickHeld(x, y);
        }

        public override void ReleaseLeftClick(int x, int y)
        {
            _profileList.ReleaseLeftClick();
            _modList.ReleaseLeftClick();
        }

        public override void ReceiveScrollWheel(int direction)
        {
            _profileList.ReceiveScrollWheel(direction);
            if (_showModList)
            {
                _modList.ReceiveScrollWheel(direction);
            }
        }

        public override void ReceiveKeyPress(Keys key)
        {
            if (_nameInput.Selected)
            {
                return;
            }

            if (key == Keys.Down)
            {
                MoveSelection(1);
            }
            else if (key == Keys.Up)
            {
                MoveSelection(-1);
            }
        }

        /// <summary>Selects the profile above or below the current one.</summary>
        private void MoveSelection(int offset)
        {
            SyncWithStore();
            var items = _profileList.Items;
            if (items.Count == 0)
            {
                return;
            }

            var index = items.FindIndex(p => p.Name.Equals(_selectedName, StringComparison.OrdinalIgnoreCase));
            var next = Math.Clamp(index + offset, 0, items.Count - 1);
            if (next != index)
            {
                Game1.playSound("smallSelect");
                Select(items[next].Name);
            }
        }

        public override string? PerformHover(int x, int y)
        {
            _nameInput.Hover(x, y);
            _create.Hover(x, y);
            foreach (var button in _actions)
            {
                button.Hover(x, y);
            }
            _profileList.Hover(x, y);
            _modList.Hover(x, y);

            var buttonTip = _create.HoverText(x, y) ?? _actions.Select(b => b.HoverText(x, y)).FirstOrDefault(t => t is not null);
            if (buttonTip is not null)
            {
                return buttonTip;
            }

            if (_profileList.TryGetItemAt(x, y, out var profile, out _))
            {
                return ProfileTooltip(profile);
            }

            if (_showModList && _modList.TryGetItemAt(x, y, out var row, out _))
            {
                return ModTooltip(row, SelectedProfile);
            }

            return null;
        }

        private static string ProfileTooltip(Profile profile)
        {
            var lines = new List<string> { profile.Name, ModCount(ModIdCount(profile)) };
            var notes = profile.Notes?.Count ?? 0;
            if (notes > 0)
            {
                lines.Add($"{notes} note{(notes == 1 ? "" : "s")}");
            }
            if (profile.IsFromCollection)
            {
                lines.Add($"From collection {profile.SourceId}, read-only");
            }
            if (ModEntry.Mods.Profiles.IsUnreadable(profile.Name))
            {
                lines.Add("Its file couldn't be read, so DewDrop leaves it alone.");
            }
            return string.Join("\n", lines);
        }

        private static string ModTooltip(ModRow row, Profile profile)
        {
            var lines = new List<string> { row.Name };
            if (row.Mod is { Author.Length: > 0 } mod)
            {
                lines.Add($"by {mod.Author}");
            }
            lines.Add("");
            lines.Add($"ID: {row.Ref.UniqueId}");
            if (row.Ref.IsFromCollection)
            {
                lines.Add($"From collection {row.Ref.SourceId}");
            }
            if (row.Mod is null)
            {
                lines.Add("Not installed, so it won't load with this profile.");
            }

            // Notes come from Stardrop's files, which may leave fields out
            var note = profile.Notes?.FirstOrDefault(n => n?.UniqueId is not null && n.UniqueId.Equals(row.Ref.UniqueId, StringComparison.OrdinalIgnoreCase));
            if (note?.Note is { Length: > 0 } text)
            {
                lines.Add($"Note: {Ui.PlainText(text)}");
            }
            return string.Join("\n", lines);
        }

        private static string ModCount(int count) => $"{count} mod{(count == 1 ? "" : "s")}";

        /// <summary>A hand-edited profile file can leave the list null; count that as no mods rather than failing every frame.</summary>
        private static int ModIdCount(Profile profile) => profile.EnabledModIds?.Count ?? 0;

        // ---------- drawing ----------

        public override void Draw(SpriteBatch b)
        {
            _nameInput.Draw(b);
            _create.Draw(b);
            _profileList.Draw(b, DrawProfileRow);

            Ui.Fill(b, _divider, Ui.Muted * 0.35f);
            DrawDetails(b);
        }

        private void DrawProfileRow(SpriteBatch b, Profile profile, Rectangle cell, bool hovered)
        {
            var active = IsActive(profile);
            if (active)
            {
                Ui.Sprite(b, Ui.Star, new Vector2(cell.X + 12, cell.Center.Y - 16), 2f);
            }

            // Names line up whether or not a row has the star; both lines stay inside the 72px row
            var textX = cell.X + 12 + 32 + 12;
            var textWidth = Math.Max(40, cell.Right - 12 - textX);
            Ui.Label(b, Ui.Ellipsize(profile.Name, textWidth), new Vector2(textX, cell.Y + 4), Ui.Text);

            var details = new List<string> { ModCount(ModIdCount(profile)) };
            if (active)
            {
                details.Add("active");
            }
            if (profile.IsFromCollection)
            {
                details.Add("collection, read-only");
            }
            else if (profile.IsProtected)
            {
                details.Add("protected");
            }
            Ui.Label(b, Ui.Ellipsize(string.Join(Separator, details), textWidth), new Vector2(textX, cell.Y + 36), Ui.Muted);
        }

        private void DrawDetails(SpriteBatch b)
        {
            var profile = SelectedProfile;
            var active = IsActive(profile);

            var nameWidth = _compactHeader ? (int)(_details.Width * 0.55f) : _details.Width;
            var name = Ui.Ellipsize(profile.Name, nameWidth, Ui.Large);
            Ui.Label(b, name, new Vector2(_details.X, _details.Y), Ui.Text, Ui.Large);

            Vector2 statusPosition;
            if (_compactHeader)
            {
                var x = _details.X + (int)Ui.Large.MeasureString(name).X + 24;
                statusPosition = new Vector2(x, _details.Y + (Ui.LineHeight(Ui.Large) - Ui.LineHeight()) / 2);
            }
            else
            {
                statusPosition = new Vector2(_details.X, _details.Y + Ui.LineHeight(Ui.Large) + 4);
            }

            var segments = new List<(string Text, Color Color)>
            {
                active ? ("Active profile", Ui.Good) : ("Not active", Ui.Muted),
                (ModCount(ModIdCount(profile)), Ui.Muted)
            };
            if (_missingCount > 0)
            {
                segments.Add(($"{_missingCount} not installed", Ui.Bad));
            }
            if (IsUnreadable(profile))
            {
                // Before the mod count, which is the stand-in's rather than the file's
                segments.Insert(1, ("its file couldn't be read", Ui.Bad));
            }
            if (profile.IsFromCollection)
            {
                segments.Add(("from a collection, read-only", Ui.Attention));
            }
            else if (profile.IsProtected)
            {
                segments.Add(("protected", Ui.Muted));
            }
            DrawSegments(b, segments, statusPosition, (int)(_details.Right - statusPosition.X));

            foreach (var button in _actions)
            {
                button.Draw(b);
            }

            if (_showModList)
            {
                _modList.Draw(b, DrawModRow);
            }
        }

        /// <summary>Coloured pieces of one line joined by "|", stopping (with "...") where the width runs out.</summary>
        private static void DrawSegments(SpriteBatch b, IEnumerable<(string Text, Color Color)> segments, Vector2 position, int maxWidth)
        {
            var x = position.X;
            var right = position.X + maxWidth;
            var first = true;
            foreach (var (text, color) in segments)
            {
                if (!first)
                {
                    var separatorWidth = Ui.Small.MeasureString(Separator).X;
                    if (x + separatorWidth >= right)
                    {
                        return;
                    }
                    Ui.Label(b, Separator, new Vector2(x, position.Y), Ui.Muted);
                    x += separatorWidth;
                }
                first = false;

                var room = (int)(right - x);
                var shown = Ui.Ellipsize(text, room);
                var width = Ui.Small.MeasureString(shown).X;
                if (width > room)
                {
                    return;
                }

                Ui.Label(b, shown, new Vector2(x, position.Y), color);
                x += width;
                if (shown != text)
                {
                    return;
                }
            }
        }

        private void DrawModRow(SpriteBatch b, ModRow row, Rectangle cell, bool hovered)
        {
            var y = cell.Center.Y - Ui.LineHeight() / 2;
            var x = cell.X + 12;
            var right = cell.Right - 12;

            if (row.Ref.IsFromCollection)
            {
                const string tag = "collection";
                var tagWidth = (int)Ui.Small.MeasureString(tag).X;
                if (right - tagWidth - 16 - x > 120)
                {
                    Ui.Label(b, tag, new Vector2(right - tagWidth, y), Ui.Muted);
                    right -= tagWidth + 16;
                }
            }

            if (row.Mod is null)
            {
                Ui.Label(b, Ui.Ellipsize($"{row.Ref.UniqueId} (not installed)", right - x), new Vector2(x, y), Ui.Bad);
                return;
            }

            var version = row.Mod.Version.Length > 0 ? $"  v{row.Mod.Version}" : "";
            var versionWidth = (int)Ui.Small.MeasureString(version).X;
            var nameText = Ui.Ellipsize(row.Mod.Name, Math.Max(40, right - x - versionWidth));
            Ui.Label(b, nameText, new Vector2(x, y), Ui.Text);

            var versionX = x + (int)Ui.Small.MeasureString(nameText).X;
            if (version.Length > 0 && versionX < right)
            {
                Ui.Label(b, Ui.Ellipsize(version, right - versionX), new Vector2(versionX, y), Ui.Muted);
            }
        }

        /// <summary>One of the selected profile's mods, with the installed copy it points at if there is one.</summary>
        private record ModRow(ModReference Ref, InstalledMod? Mod)
        {
            public string Name => Mod?.Name ?? Ref.UniqueId;
        }
    }
}
