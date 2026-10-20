using System.Net;
using System.Text;
using DewDrop.Core.Models;
using DewDrop.Core.Services;
using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>
    /// One Nexus mod opened from Browse: picture and description on the left, details and the file list with
    /// install buttons on the right. Browse owns it and forwards its input while it's open.
    /// </summary>
    internal class ModDetailView
    {
        private const int FileRowHeight = 84;
        private const int CellGap = 12;        // ScrollList's gap between rows
        private const int CompactHeight = 360; // below this the header shrinks to make room for files
        private const string InstalledLabel = "Installed";

        private readonly DewDropMenu _menu;
        private readonly NexusMod _mod;
        private readonly string _name;
        private readonly UiButton _back;
        private readonly UiButton _openOnNexus;
        private readonly UiButton _toggleOld;
        private readonly UiButton _uninstall;
        private readonly UiButton _retryFiles;
        private readonly ScrollList<string> _description;
        private readonly ScrollList<NexusModFile> _files = new(FileRowHeight);
        private readonly Dictionary<NexusModFile, UiButton> _fileButtons = new();
        private string? _descriptionText;
        private List<NexusModFile>? _allFiles;
        private string? _filesError;
        private int _filesRequest;
        private bool _showOld;
        private bool _closed;
        private List<InstalledMod> _installed = new();
        private int _installedVersion = -1;
        private int _installedChanges = -1;

        /// <summary>IDs of this mod's files that are installed now; their rows say so instead of offering Install.</summary>
        private HashSet<int> _installedFiles = new();
        private Rectangle _bounds;
        private Rectangle _picture;
        private Rectangle _right;
        private List<string> _nameLines = new();
        private bool _compact;
        private int _bylineY;
        private int _statsY;
        private int _wrappedWidth = -1;
        private int _downloadingWidth;

        public ModDetailView(DewDropMenu menu, NexusMod mod, Action onBack)
        {
            _menu = menu;
            _mod = mod;
            _name = Clean(mod.Name) is { Length: > 0 } name ? name : $"Mod {mod.ModId}";

            _back = new UiButton("Back", onBack);
            _openOnNexus = new UiButton("Open on Nexus", () => DownloadManager.OpenUrl(mod.PageUrl)) { Tooltip = "Open this mod's page in your browser." };
            _toggleOld = new UiButton("Show old files", ToggleOldFiles) { Tooltip = "Old versions are hidden unless you ask for them.", Visible = false };
            _uninstall = new UiButton("Uninstall", Uninstall) { Color = Ui.Bad, Visible = false };
            _retryFiles = new UiButton("Try again", LoadFiles) { Visible = false };

            // Rows are a line tall less the list's gap, so lines sit at the font's own spacing (each line's text runs into the gap below it)
            _description = new ScrollList<string>(Math.Max(1, Ui.LineHeight() - CellGap)) { EmptyText = "Loading description..." };
            _files.EmptyText = "Loading files...";

            // A file row does nothing on click (its Install button does), so it doesn't light up under the mouse
            _files.HoverHighlight = false;

            // Description and files load side by side; whichever arrives first shows first
            LoadDescription();
            LoadFiles();
        }

        private static int LineStep => Ui.LineHeight() - 2;

        /// <summary>Stops late replies from touching a view the player has left.</summary>
        public void Close()
        {
            _closed = true;
        }

        public void Layout(Rectangle bounds)
        {
            _bounds = bounds;

            // Toolbar: Back on the left, mod-wide buttons on the right, installed status between them
            _back.At(bounds.X, bounds.Y);
            _openOnNexus.AtRight(bounds.Right, bounds.Y);
            var toggleWidth = (int)Math.Max(Ui.Small.MeasureString("Show old files").X, Ui.Small.MeasureString("Hide old files").X) + 40;
            _toggleOld.Bounds = new Rectangle(_openOnNexus.Bounds.X - 12 - toggleWidth, bounds.Y, toggleWidth, _toggleOld.PreferredHeight);
            PlaceUninstall();
            _downloadingWidth = (int)Ui.Small.MeasureString("Downloading 100%").X + 40;

            var bodyTop = _back.Bounds.Bottom + 8;
            var bodyHeight = Math.Max(0, bounds.Bottom - bodyTop);
            _compact = bodyHeight < CompactHeight;

            // Left column: picture in the top 40%, keeping enough room under it for a few lines of description
            var leftWidth = (int)(bounds.Width * 0.45f);
            var pictureHeight = Math.Max(0, Math.Min((int)(bodyHeight * 0.4f), bodyHeight - 12 - 112));
            _picture = new Rectangle(bounds.X, bodyTop, leftWidth, pictureHeight);
            var descriptionTop = _picture.Bottom + 12;
            var descriptionHeight = Math.Max(_description.RowHeight, bounds.Bottom - descriptionTop - CellGap);
            _description.Layout(new Rectangle(bounds.X, descriptionTop, leftWidth, descriptionHeight));
            WrapDescription();

            // Right column: name, author and stats, then the files
            _right = new Rectangle(_picture.Right + 28, bodyTop, bounds.Right - _picture.Right - 28, bodyHeight);
            _nameLines = NameLines(_right.Width, _compact ? 1 : 2);
            var y = _right.Y + _nameLines.Count * Ui.LineHeight(Ui.Large);
            _bylineY = y;
            if (!_compact)
            {
                y += LineStep;
            }
            _statsY = y;
            y += LineStep + 12;

            var filesArea = new Rectangle(_right.X, y, _right.Width, Math.Max(1, _right.Bottom - y));
            _files.RowHeight = Math.Min(FileRowHeight, filesArea.Height);
            _files.Layout(filesArea);

            // Try again goes under the error line, or at its right end when there's no room below
            var retryY = filesArea.Y + Ui.LineHeight() + 12;
            if (retryY + _retryFiles.PreferredHeight <= _bounds.Bottom)
            {
                _retryFiles.At(filesArea.Center.X - _retryFiles.PreferredWidth / 2, retryY);
            }
            else
            {
                _retryFiles.AtRight(filesArea.Right, Math.Max(_statsY + Ui.LineHeight(), Math.Min(filesArea.Y, _bounds.Bottom - _retryFiles.PreferredHeight)));
            }
        }

        /// <summary>Uninstall sits left of the other toolbar buttons, which come and go as the files load.</summary>
        private void PlaceUninstall()
        {
            var left = _toggleOld.Visible ? _toggleOld.Bounds.X : _openOnNexus.Bounds.X;
            _uninstall.AtRight(left - 12, _openOnNexus.Bounds.Y);
        }

        /// <summary>The leftmost toolbar button on the right, which the installed status stops short of.</summary>
        private int ToolbarRightEdge => _uninstall.Visible ? _uninstall.Bounds.X : _toggleOld.Visible ? _toggleOld.Bounds.X : _openOnNexus.Bounds.X;

        /// <summary>The name in the big font, wrapped to at most a few lines with the last one cut short.</summary>
        private List<string> NameLines(int width, int maxLines)
        {
            var lines = SplitLines(Ui.Wrap(_name, width, Ui.Large)).Where(l => l.Length > 0).ToList();
            if (lines.Count > maxLines)
            {
                var rest = string.Join(" ", lines.Skip(maxLines - 1));
                lines = lines.Take(maxLines - 1).Append(rest).ToList();
            }

            return lines.Select(l => Ui.Ellipsize(l, width, Ui.Large)).ToList();
        }

        // ---------- loading ----------

        private void LoadDescription()
        {
            var modId = _mod.ModId;
            MainThread.Run(
                () => ModEntry.Nexus.Api.GetModAsync(modId),
                full =>
                {
                    if (!_closed)
                    {
                        SetDescription(Ui.PlainText(full?.Description));
                    }
                },
                ex =>
                {
                    if (!_closed)
                    {
                        SetDescription("", $"(Couldn't load the full description. {ErrorText(ex)})");
                    }
                });
        }

        /// <summary>Uses the mod's summary when the description is missing.</summary>
        private void SetDescription(string text, string? note = null)
        {
            if (text.Length == 0)
            {
                text = Ui.PlainText(_mod.Summary);
            }
            if (text.Length == 0)
            {
                text = "This mod has no description.";
            }
            if (note is not null)
            {
                text += "\n\n" + note;
            }

            _descriptionText = text;
            _wrappedWidth = -1;
            WrapDescription();
        }

        private void WrapDescription()
        {
            // Leave room for the scroll bar
            var width = _description.Bounds.Width - 56;
            if (_descriptionText is null || width <= 0 || width == _wrappedWidth)
            {
                return;
            }

            _wrappedWidth = width;
            _description.SetItems(WrapLines(_descriptionText, width));
        }

        private void LoadFiles()
        {
            var request = ++_filesRequest;
            var modId = _mod.ModId;
            _filesError = null;
            _retryFiles.Visible = false;
            _files.EmptyText = "Loading files...";
            MainThread.Run(
                () => ModEntry.Nexus.Api.GetFilesAsync(modId),
                files =>
                {
                    if (_closed || request != _filesRequest)
                    {
                        return;
                    }

                    _allFiles = files;
                    _toggleOld.Visible = files.Any(f => !f.IsCurrent);
                    PlaceUninstall();
                    RefreshInstalledFiles();
                    RefreshFiles(resetScroll: true);
                },
                ex =>
                {
                    if (_closed || request != _filesRequest)
                    {
                        return;
                    }

                    _filesError = ErrorText(ex);
                    _retryFiles.Visible = ex is not NexusApiException { CanRetry: false };
                });
        }

        private void RefreshFiles(bool resetScroll = false)
        {
            var files = _allFiles ?? new List<NexusModFile>();
            _files.EmptyText = files.Count == 0 ? "This mod has no files." : "No current files.";
            _files.SetItems(_showOld ? files : files.Where(f => f.IsCurrent), resetScroll);
        }

        private void ToggleOldFiles()
        {
            _showOld = !_showOld;
            _toggleOld.Label = _showOld ? "Hide old files" : "Show old files";
            RefreshFiles(resetScroll: true);
        }

        // ---------- file actions ----------

        private UiButton FileButton(NexusModFile file)
        {
            if (!_fileButtons.TryGetValue(file, out var button))
            {
                button = new UiButton("Install", () => FileAction(file));
                _fileButtons[file] = button;
            }

            return button;
        }

        private DownloadJob? ActiveJob(NexusModFile file)
        {
            return ModEntry.Downloads.Jobs.FirstOrDefault(j => j.ModId == _mod.ModId && j.FileId == file.FileId && j.IsActive);
        }

        /// <summary>The button follows the account and download state, so it's refreshed every time it's drawn.</summary>
        private void UpdateFileButton(NexusModFile file, UiButton button)
        {
            button.Enabled = true;
            button.Visible = true;
            button.Color = null;

            if (ActiveJob(file) is { } job)
            {
                button.Enabled = false;
                button.Label = job.State switch
                {
                    DownloadState.Installing => "Installing...",
                    DownloadState.Queued => "Queued...",
                    _ => $"Downloading {(int)(Math.Clamp(job.Progress, 0, 1) * 100)}%"
                };
                button.Tooltip = "DewDrop is getting this file. Progress is on the Downloads tab.";
            }
            else if (_installedFiles.Contains(file.FileId))
            {
                // Drawn as a green "Installed" in the button's place (see DrawFileRow), so the same file isn't installed again
                button.Visible = false;
                button.Label = InstalledLabel;
                button.Tooltip = "You have this file. To install it again, uninstall it first.";
            }
            else if (!ModEntry.Nexus.IsLoggedIn)
            {
                button.Label = "Log in to install";
                button.Tooltip = "Log in to Nexus Mods on the Account tab to download mods in-game.";
            }
            else if (ModEntry.Nexus.IsPremium)
            {
                button.Label = "Install";
                button.Color = Ui.Good;
                button.Tooltip = "Download and install this file.";
            }
            else
            {
                button.Label = "Get on Nexus";
                button.Tooltip = UpdateActions.FreeAccountHint;
            }
        }

        /// <summary>Decides what a click does at click time, so a login or download that changed since the last draw is respected.</summary>
        private void FileAction(NexusModFile file)
        {
            if (ActiveJob(file) is not null || _installedFiles.Contains(file.FileId))
            {
                return;
            }

            if (!ModEntry.Nexus.IsLoggedIn)
            {
                _menu.ShowPage(_menu.Account);
                return;
            }

            if (ModEntry.Nexus.IsPremium)
            {
                ModEntry.Downloads.Start(_mod.ModId, file.FileId, $"{_name} {file.Version}".TrimEnd());
                return;
            }

            UpdateActions.OpenForFreeAccount(_mod.ModId, file.FileId);
        }

        /// <summary>The copies the toolbar's Uninstall removes: every loose one that belongs to this Nexus mod.</summary>
        private List<InstalledMod> UninstallTargets()
        {
            var mods = ModEntry.Mods;
            return _installed.Where(m => m.SourceId is null && !mods.IsLocked(m)).ToList();
        }

        private void Uninstall()
        {
            var targets = UninstallTargets();
            UninstallPrompt.Show(_menu, targets, targets.Count > 1 ? _name : null);
        }

        private void RefreshInstalledFiles()
        {
            _installedFiles = _allFiles is null ? new HashSet<int>() : ModEntry.Mods.InstalledFileIds(_mod.ModId, _allFiles);
        }

        // ---------- input ----------

        public void Update(GameTime time)
        {
            // The installed list object is replaced on rescan (e.g. after this mod's download finishes), and an uninstall
            // or update check changes what's known without one
            var mods = ModEntry.Mods;
            if (mods.Installed.GetHashCode() != _installedVersion || mods.ChangeCount != _installedChanges)
            {
                _installedVersion = mods.Installed.GetHashCode();
                _installedChanges = mods.ChangeCount;
                _installed = mods.CopiesOfNexusMod(_mod.ModId);
                RefreshInstalledFiles();
                _uninstall.Visible = UninstallTargets().Count > 0;
                _uninstall.Tooltip = UninstallTooltip();
                PlaceUninstall();
            }
        }

        private string UninstallTooltip()
        {
            var targets = UninstallTargets();
            return targets.Count switch
            {
                0 => "",
                1 => $"Uninstall {targets[0].Name}. Its folder is deleted when the game closes.",
                _ => $"Uninstall the {targets.Count} mods that make up {_name}: {UninstallPrompt.Names(targets.Select(m => m.Name).ToList())}."
            };
        }

        public void ReceiveLeftClick(int x, int y)
        {
            if (_back.TryClick(x, y) || _toggleOld.TryClick(x, y) || _uninstall.TryClick(x, y) || _openOnNexus.TryClick(x, y) || _retryFiles.TryClick(x, y))
            {
                return;
            }

            if (_description.ReceiveLeftClick(x, y) || _filesError is not null || _files.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (_files.TryGetItemAt(x, y, out var file, out _) && _fileButtons.TryGetValue(file, out var button))
            {
                button.TryClick(x, y);
            }
        }

        public void LeftClickHeld(int x, int y)
        {
            _description.LeftClickHeld(x, y);
            _files.LeftClickHeld(x, y);
        }

        public void ReleaseLeftClick(int x, int y)
        {
            _description.ReleaseLeftClick();
            _files.ReleaseLeftClick();
        }

        public void ReceiveScrollWheel(int direction)
        {
            // A notch moves the description a few lines, since its rows are single lines of text
            if (_description.Bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                _description.ScrollBy(direction > 0 ? -3 : 3);
                return;
            }

            _files.ReceiveScrollWheel(direction);
        }

        public void ReceiveKeyPress(Keys key)
        {
            if (key == Keys.Down)
            {
                _description.ScrollBy(3);
            }
            else if (key == Keys.Up)
            {
                _description.ScrollBy(-3);
            }
            else if (key == Keys.Back)
            {
                _back.OnClick?.Invoke();
                Game1.playSound("smallSelect");
            }
        }

        public string? PerformHover(int x, int y)
        {
            _back.Hover(x, y);
            _toggleOld.Hover(x, y);
            _uninstall.Hover(x, y);
            _openOnNexus.Hover(x, y);
            _retryFiles.Hover(x, y);
            _files.Hover(x, y);

            // Only the row under the mouse has a button where it was last drawn
            foreach (var other in _fileButtons.Values)
            {
                other.Hover(-1, -1);
            }

            // Says why not when it can't uninstall right now (a download of it running, say), like the Installed tab
            if (_uninstall.Contains(x, y))
            {
                var targets = UninstallTargets();
                _uninstall.Tooltip = targets.Select(ModEntry.Mods.UninstallBlockedReason).FirstOrDefault(r => r is not null) ?? UninstallTooltip();
            }

            var tooltip = _openOnNexus.HoverText(x, y) ?? _toggleOld.HoverText(x, y) ?? _uninstall.HoverText(x, y);
            if (tooltip is not null)
            {
                return tooltip;
            }

            if (_filesError is not null || !_files.TryGetItemAt(x, y, out var file, out _))
            {
                return null;
            }

            if (_fileButtons.TryGetValue(file, out var button))
            {
                button.Hover(x, y);

                // An installed file's button is hidden behind its green label, which still explains itself
                if (button.Contains(x, y) || (!button.Visible && button.Bounds.Contains(x, y)))
                {
                    return button.Tooltip;
                }
            }

            // Rows too short for the details line carry it in the tooltip instead
            var description = Ui.PlainText(file.Description);
            if (_files.RowHeight < LineStep + Ui.LineHeight())
            {
                description = description.Length > 0 ? $"{FileDetails(file)}\n{description}" : FileDetails(file);
            }
            return description.Length > 0 ? description : null;
        }

        // ---------- drawing ----------

        public void Draw(SpriteBatch b)
        {
            _back.Draw(b);
            _toggleOld.Draw(b);
            _uninstall.Draw(b);
            _openOnNexus.Draw(b);
            DrawInstalledStatus(b);

            DrawPicture(b);
            _description.Draw(b, (batch, line, cell, _) => Ui.Label(batch, line, new Vector2(cell.X, cell.Y), Ui.Text));

            DrawHeader(b);
            if (_filesError is not null)
            {
                var area = _files.Bounds;
                var width = _retryFiles.Bounds.Y >= area.Y + Ui.LineHeight() ? area.Width : _retryFiles.Bounds.X - 16 - area.X;
                Ui.Label(b, Ui.Ellipsize(_filesError, width), new Vector2(area.X, area.Y), Ui.Bad);
                _retryFiles.Draw(b);
            }
            else
            {
                _files.Draw(b, DrawFileRow);
            }
        }

        /// <summary>The installed copy (preferring one with a new or a waiting update), right-aligned in the toolbar.</summary>
        private void DrawInstalledStatus(SpriteBatch b)
        {
            var mods = ModEntry.Mods;
            var mod = _installed.FirstOrDefault(mods.HasNewUpdate)
                ?? _installed.FirstOrDefault(mods.HasPendingUpdate)
                ?? _installed.FirstOrDefault(m => !m.IsContentPack)
                ?? _installed.FirstOrDefault();
            if (mod is null)
            {
                return;
            }

            var left = _back.Bounds.Right + 24;
            var right = ToolbarRightEdge - 24;
            if (right - left < 40)
            {
                return;
            }

            const int gap = 24;
            var installedText = $"Installed {Version(mod.Version)}".TrimEnd();

            // An update already downloaded only waits for the restart, so it isn't offered as new again
            var pending = mods.HasPendingUpdate(mod);
            var updateText = pending
                ? "Update ready, restart to finish"
                : mods.HasNewUpdate(mod) ? $"Update available: {Version(mod.Update!.Version)}" : null;
            if (updateText is not null && Measure(installedText) + gap + Measure(updateText) > right - left)
            {
                updateText = pending ? "Restart to update" : $"Update: {Version(mod.Update!.Version)}";
            }

            var y = _back.Bounds.Center.Y - Ui.LineHeight() / 2;
            var x = right;
            if (updateText is not null)
            {
                var text = Ui.Ellipsize(updateText, right - left);
                x -= Measure(text);
                Ui.Label(b, text, new Vector2(x, y), pending ? Ui.Good : Ui.Attention);
                x -= gap;
            }

            if (x - left >= 40)
            {
                var text = Ui.Ellipsize(installedText, x - left);
                Ui.Label(b, text, new Vector2(x - Measure(text), y), Ui.Good);
            }
        }

        private void DrawPicture(SpriteBatch b)
        {
            if (_picture.Height < 16)
            {
                return;
            }

            // The thumbnail Browse already loaded stands in while the full picture downloads
            var texture = ModEntry.Thumbnails.Get(_mod.PictureUrl) ?? ModEntry.Thumbnails.Get(_mod.ThumbnailUrl);
            if (texture is null)
            {
                var gone = (_mod.PictureUrl is null || ModEntry.Thumbnails.IsFailed(_mod.PictureUrl)) && (_mod.ThumbnailUrl is null || ModEntry.Thumbnails.IsFailed(_mod.ThumbnailUrl));
                Ui.PicturePlaceholder(b, _picture, gone ? "No picture" : "...");
                return;
            }

            Ui.Picture(b, texture, _picture);
        }

        private void DrawHeader(SpriteBatch b)
        {
            var y = _right.Y;
            foreach (var line in _nameLines)
            {
                Ui.Label(b, line, new Vector2(_right.X, y), Ui.Text, Ui.Large);
                y += Ui.LineHeight(Ui.Large);
            }

            var byline = $"by {Clean(_mod.Author ?? "unknown")}";
            var x = _right.X;
            if (_compact)
            {
                // Short window: the author shares the stats line
                var text = Ui.Ellipsize(byline, _right.Width * 2 / 5) + "  |  ";
                Ui.Label(b, text, new Vector2(x, _statsY), Ui.Muted);
                x += Measure(text);
            }
            else
            {
                Ui.Label(b, Ui.Ellipsize(byline, _right.Width), new Vector2(x, _bylineY), Ui.Muted);
            }

            Ui.Sprite(b, Ui.Heart, new Vector2(x, _statsY + 8), 3f);
            x += Ui.Heart.Width * 3 + 8;
            Ui.Label(b, StatsText(_right.Right - x), new Vector2(x, _statsY), Ui.Muted);
        }

        /// <summary>Endorsements and downloads, then version and age while they fit.</summary>
        private string StatsText(int width)
        {
            var text = $"{Ui.Count(_mod.Endorsements)}   {Ui.Count(_mod.Downloads)} downloads";
            var extras = new List<string>();
            if (Version(_mod.Version) is { Length: > 0 } version)
            {
                extras.Add(version);
            }
            if (_mod.UpdatedAt is { } updated)
            {
                extras.Add($"updated {Ui.Ago(updated.Kind == DateTimeKind.Local ? updated.ToUniversalTime() : updated)}");
            }

            foreach (var extra in extras)
            {
                var longer = $"{text}  |  {extra}";
                if (Measure(longer) > width)
                {
                    break;
                }
                text = longer;
            }

            return Ui.Ellipsize(text, Math.Max(0, width));
        }

        private void DrawFileRow(SpriteBatch b, NexusModFile file, Rectangle cell, bool hovered)
        {
            var button = FileButton(file);
            UpdateFileButton(file, button);
            var width = ActiveJob(file) is { State: DownloadState.Downloading } ? _downloadingWidth : button.PreferredWidth;
            var height = Math.Min(button.PreferredHeight, cell.Height);
            button.Bounds = new Rectangle(cell.Right - 8 - width, cell.Center.Y - height / 2, width, height);
            button.Draw(b);
            if (!button.Visible && button.Label == InstalledLabel)
            {
                Ui.LabelCentered(b, InstalledLabel, button.Bounds, Ui.Good);
            }

            // A row too short for two lines (very small window) keeps just the name; PerformHover adds the details to the tooltip
            var textX = cell.X + 12;
            var textWidth = Math.Max(40, button.Bounds.X - 16 - textX);
            var twoLines = cell.Height >= LineStep + Ui.LineHeight();
            var top = cell.Y + Math.Max(0, (cell.Height - (twoLines ? LineStep : 0) - Ui.LineHeight()) / 2);
            Ui.Label(b, Ui.Ellipsize(Clean(file.Name) is { Length: > 0 } name ? name : $"File {file.FileId}", textWidth), new Vector2(textX, top), Ui.Text);
            if (twoLines)
            {
                Ui.Label(b, Ui.Ellipsize(FileDetails(file), textWidth), new Vector2(textX, top + LineStep), Ui.Muted);
            }
        }

        private static string FileDetails(NexusModFile file)
        {
            var parts = new List<string>();
            if (file.Category.Length > 0)
            {
                parts.Add(file.Category.Replace('_', ' '));
            }
            if (Version(file.Version) is { Length: > 0 } version)
            {
                parts.Add(version);
            }
            if (file.SizeInBytes > 0)
            {
                parts.Add(Ui.Size(file.SizeInBytes));
            }
            if (file.Date.Year > 1970)
            {
                parts.Add(Ui.Ago(file.Date));
            }

            return string.Join(" | ", parts);
        }

        // ---------- text ----------

        private static int Measure(string text) => (int)Ui.Small.MeasureString(text).X;

        /// <summary>"1.2" -> "v1.2"; empty when there's no version.</summary>
        private static string Version(string? version)
        {
            version = Clean(version);
            if (version.Length == 0)
            {
                return "";
            }

            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : "v" + version;
        }

        private static IEnumerable<string> SplitLines(string text) => text.Split('\n').Select(l => l.TrimEnd('\r', ' '));

        /// <summary>Wraps text to a width as separate lines, breaking words that are wider than a line on their own (usually links).</summary>
        private static List<string> WrapLines(string text, int width)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                if (paragraph.Trim().Length == 0)
                {
                    lines.Add("");
                    continue;
                }

                foreach (var wrapped in SplitLines(Ui.Wrap(paragraph, width)))
                {
                    var line = wrapped;
                    while (line.Length > 1 && Measure(line) > width)
                    {
                        var fit = FitLength(line, width);
                        lines.Add(line[..fit]);
                        line = line[fit..];
                    }
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>How many leading characters fit in a width (at least one).</summary>
        private static int FitLength(string text, int width)
        {
            var low = 1;
            var high = text.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (Measure(text[..mid]) <= width)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return low;
        }

        /// <summary>
        /// Names and versions from Nexus can hold characters the game fonts lack (they'd draw as "*"): decode HTML
        /// entities, straighten quotes, take accents off letters the font lacks and drop the rest. Use Ui.PlainText
        /// for descriptions, which also strips markup.
        /// </summary>
        internal static string Clean(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var cleaned = new StringBuilder(text.Length);
            foreach (var c in WebUtility.HtmlDecode(text))
            {
                if (c is (char)0x2018 or (char)0x2019)
                {
                    cleaned.Append('\'');
                }
                else if (c is (char)0x201C or (char)0x201D)
                {
                    cleaned.Append('"');
                }
                else if (c is (char)0x2013 or (char)0x2014)
                {
                    cleaned.Append('-');
                }
                else if (char.IsWhiteSpace(c))
                {
                    cleaned.Append(' ');
                }
                else
                {
                    // Same per-font glyph check as descriptions: "ł" becomes "l" rather than vanishing
                    Ui.AppendDrawable(cleaned, c);
                }
            }

            return cleaned.ToString().Trim();
        }

        /// <summary>A short, readable reason a Nexus request failed.</summary>
        internal static string ErrorText(Exception ex)
        {
            return ex switch
            {
                // Already worded for the player
                NexusApiException => Clean(ex.Message),
                TaskCanceledException => "Nexus Mods took too long to answer.",
                HttpRequestException => Clean($"Couldn't reach Nexus Mods: {ex.Message}"),
                _ => Clean($"Something went wrong: {ex.Message}")
            };
        }
    }
}
