using DewDrop.Framework;
using DewDrop.UI.Pages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>
    /// The DewDrop screen, opened as the title menu's sub-menu. It draws the frame, tab row, bottom bar (active
    /// profile, restart status, Apply &amp; Restart), toasts, tooltips and confirm dialogs; tabs draw the rest.
    /// </summary>
    internal class DewDropMenu : IClickableMenu, IDisposable
    {
        private const int Padding = 32;
        private const int TabHeight = 60;
        private const int BottomBarHeight = 116;
        private const int MaxFrameWidth = 1600;
        private const int MaxFrameHeight = 1150;

        /// <summary>Frames shorter than this (720p, the Steam Deck) get tighter gaps so the lists fit more rows.</summary>
        private const int CompactFrameHeight = 640;

        private readonly List<Page> _pages = new();
        private readonly List<Rectangle> _tabBounds = new();
        private readonly List<(Rectangle Box, Toast Toast)> _toastBoxes = new();
        private readonly UiButton _apply;
        private Page _current;
        private Rectangle _frame;
        private Rectangle _profileLabel;
        private Rectangle _statusLabel;
        private string _statusText = "";
        private string? _hoverText;
        private ConfirmDialog? _confirm;
        private bool _disposed;

        /// <summary>
        /// Set when a controller's B was used inside the menu (to close a dialog or leave a text box), so the title
        /// screen's own B handling, which runs right after, doesn't close the menu too. Cleared on the next update.
        /// </summary>
        private bool _holdOpen;

        public InstalledPage Installed { get; }
        public BrowsePage Browse { get; }
        public ProfilesPage ProfilesTab { get; }
        public DownloadsPage DownloadsTab { get; }
        public AccountPage Account { get; }

        public DewDropMenu()
        {
            Installed = new InstalledPage(this);
            Browse = new BrowsePage(this);
            ProfilesTab = new ProfilesPage(this);
            DownloadsTab = new DownloadsPage(this);
            Account = new AccountPage(this);
            _pages.AddRange(new Page[] { Installed, Browse, ProfilesTab, DownloadsTab, Account });

            _apply = new UiButton("Apply & Restart", ConfirmRestart, primary: true);
            _current = Installed;

            // Messages from the game's start (a Steam change, mods turned on) wait for the menu, or they'd expire unseen
            ModEntry.ShowWaitingToasts();

            DoLayout();
            _current.OnShow();
        }

        /// <summary>The area pages draw in.</summary>
        public Rectangle Content { get; private set; }

        public void ShowPage(Page page)
        {
            if (page == _current)
            {
                return;
            }

            _current.OnHide();
            _current = page;
            _current.OnShow();
            Game1.playSound("smallSelect");
        }

        /// <summary>Asks a yes/no question over the menu; onYes runs only if the player agrees.</summary>
        public void Confirm(string message, string yesLabel, Action onYes)
        {
            // A text box left selected behind the dialog would still get typing and Enter (creating a profile, say)
            ReleaseTextFocus();
            _confirm = new ConfirmDialog(message, yesLabel, () =>
            {
                _confirm = null;
                onYes();
            }, () => _confirm = null);
            _confirm.Layout(Game1.uiViewport.Width, Game1.uiViewport.Height);
        }

        // ---------- layout ----------

        private void DoLayout()
        {
            var viewWidth = Game1.uiViewport.Width;
            var viewHeight = Game1.uiViewport.Height;

            // Leave room for the banner above and the title screen's back button in the bottom-right corner, and
            // cap the size so big screens get a menu-shaped box rather than a tall column
            var frameWidth = Math.Min(MaxFrameWidth, viewWidth - 128);
            var top = 112;
            var available = Math.Max(360, viewHeight - 120 - top);
            var frameHeight = Math.Min(MaxFrameHeight, available);
            _frame = new Rectangle((viewWidth - frameWidth) / 2, top + (available - frameHeight) / 2, frameWidth, frameHeight);

            xPositionOnScreen = _frame.X;
            yPositionOnScreen = _frame.Y;
            width = _frame.Width;
            height = _frame.Height;

            LayoutTabs();

            // At 720p the usual gaps under the tabs and above the bottom bar leave room for a single list row
            var compact = frameHeight < CompactFrameHeight;
            var contentTop = _frame.Y + 28 + TabHeight + (compact ? 16 : 28);
            var contentBottom = _frame.Bottom - BottomBarHeight - (compact ? 16 : 44);
            Content = new Rectangle(_frame.X + Padding, contentTop, _frame.Width - Padding * 2, Math.Max(120, contentBottom - contentTop));

            _apply.AtRight(_frame.Right - Padding, _frame.Bottom - BottomBarHeight + 6);

            foreach (var page in _pages)
            {
                page.Layout(Content);
            }

            _confirm?.Layout(viewWidth, viewHeight);
        }

        /// <summary>Tab widths follow their labels, which carry live counts, so this runs every tick.</summary>
        private void LayoutTabs()
        {
            _tabBounds.Clear();
            var x = _frame.X + Padding;
            foreach (var page in _pages)
            {
                var tabWidth = (int)Ui.Small.MeasureString(TabLabel(page)).X + 48;
                _tabBounds.Add(new Rectangle(x, _frame.Y + 28, tabWidth, TabHeight));
                x += tabWidth + 8;
            }
        }

        private static string TabLabel(Page page) => page.Badge is { Length: > 0 } badge ? $"{page.Title} ({badge})" : page.Title;

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            DoLayout();
        }

        // ---------- input ----------

        public override bool readyToClose()
        {
            return _broken || (_confirm is null && !_holdOpen && !_pages.Any(p => p.IsTyping));
        }

        /// <summary>
        /// The pages are mouse-driven with no snap points, so a controller's stick should move a free cursor. The game
        /// asks the title screen rather than this menu, so TitleMenuPatches passes this answer on.
        /// </summary>
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        /// <summary>The title screen calls this when the menu opens with a controller: bring the cursor in from the hidden Mods button.</summary>
        public override void snapToDefaultClickableComponent()
        {
            Game1.setMousePosition(Content.Center.X, Content.Center.Y, ui_scale: true);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (_confirm is not null)
            {
                _confirm.ReceiveLeftClick(x, y);
                return;
            }

            // Toasts sit over the page, so a click on one dismisses it rather than reaching what's underneath
            var toast = ToastAt(x, y);
            if (toast >= 0)
            {
                ModEntry.Toasts.Remove(_toastBoxes[toast].Toast);
                _toastBoxes.RemoveAt(toast);
                return;
            }

            for (var i = 0; i < _tabBounds.Count; i++)
            {
                if (_tabBounds[i].Contains(x, y))
                {
                    ShowPage(_pages[i]);
                    return;
                }
            }

            if (_apply.TryClick(x, y))
            {
                return;
            }

            _current.ReceiveLeftClick(x, y);
        }

        public override void leftClickHeld(int x, int y)
        {
            if (_confirm is null)
            {
                _current.LeftClickHeld(x, y);
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (_confirm is null)
            {
                _current.ReleaseLeftClick(x, y);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (_confirm is null)
            {
                _current.ReceiveScrollWheel(direction);
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (_confirm is not null)
            {
                if (key == Keys.Escape)
                {
                    _confirm.Cancel();
                }
                else if (key == Keys.Enter && _confirm.Ready)
                {
                    _confirm.Accept();
                }
                return;
            }

            if (key == Keys.Escape && ReleaseTextFocus())
            {
                return;
            }

            _current.ReceiveKeyPress(key);
        }

        /// <summary>
        /// The title screen never passes its menu keys (Escape and E by default) to a sub-menu, so TitleMenuPatches
        /// offers them here first. Either key closes a confirm dialog, and Escape leaves a text box; returns whether
        /// the key was used.
        /// </summary>
        public bool ReceiveMenuKey(Keys key)
        {
            if (_confirm is not null)
            {
                _confirm.Cancel();
                return true;
            }

            return key == Keys.Escape && ReleaseTextFocus();
        }

        /// <summary>
        /// Controller buttons the title screen passes on. The stick moves the cursor and A clicks, so this only adds
        /// what a mouse would do with keys: the D-pad acts as the arrow keys, the shoulder buttons switch tabs, and
        /// B closes a dialog or leaves a text box before it closes the menu.
        /// </summary>
        public override void receiveGamePadButton(Buttons b)
        {
            if (_confirm is not null)
            {
                if (b == Buttons.B)
                {
                    _confirm.Cancel();
                    _holdOpen = true;
                }
                return;
            }

            switch (b)
            {
                case Buttons.B:
                    if (ReleaseTextFocus())
                    {
                        _holdOpen = true;
                    }
                    break;

                case Buttons.LeftShoulder:
                    ShowPage(_pages[(_pages.IndexOf(_current) + _pages.Count - 1) % _pages.Count]);
                    break;

                case Buttons.RightShoulder:
                    ShowPage(_pages[(_pages.IndexOf(_current) + 1) % _pages.Count]);
                    break;

                // The D-pad also arrives as the move keys (W/A/S/D), but so does the stick, which moves the cursor
                case Buttons.DPadUp:
                    _current.ReceiveKeyPress(Keys.Up);
                    break;

                case Buttons.DPadDown:
                    _current.ReceiveKeyPress(Keys.Down);
                    break;

                case Buttons.DPadLeft:
                    _current.ReceiveKeyPress(Keys.Left);
                    break;

                case Buttons.DPadRight:
                    _current.ReceiveKeyPress(Keys.Right);
                    break;
            }
        }

        /// <summary>Takes keyboard focus from a DewDrop text box; returns whether one had it.</summary>
        private static bool ReleaseTextFocus()
        {
            if (Game1.keyboardDispatcher.Subscriber is not TextInput)
            {
                return false;
            }

            // The dispatcher deselects the box it lets go of
            Game1.keyboardDispatcher.Subscriber = null;
            return true;
        }

        public override void performHoverAction(int x, int y)
        {
            if (_broken)
            {
                return;
            }

            try
            {
                Hover(x, y);
            }
            catch (Exception ex)
            {
                Break("updating", ex);
            }
        }

        private void Hover(int x, int y)
        {
            _hoverText = null;
            if (_confirm is not null)
            {
                _confirm.Hover(x, y);
                return;
            }

            _apply.Hover(x, y);

            // Nothing under a toast reacts to the mouse
            if (ToastAt(x, y) >= 0)
            {
                _current.PerformHover(int.MinValue, int.MinValue);
                return;
            }

            _hoverText = _apply.HoverText(x, y)
                ?? (_profileLabel.Contains(x, y) ? $"Profile: {ModEntry.Mods.ActiveProfile.Name}" : null)
                ?? (_statusLabel.Contains(x, y) ? _statusText : null)
                ?? _current.PerformHover(x, y);
        }

        /// <summary>The index in _toastBoxes of the toast drawn at a point, or -1.</summary>
        private int ToastAt(int x, int y)
        {
            for (var i = 0; i < _toastBoxes.Count; i++)
            {
                if (_toastBoxes[i].Box.Contains(x, y))
                {
                    return i;
                }
            }

            return -1;
        }

        public override void update(GameTime time)
        {
            if (!_broken)
            {
                try
                {
                    base.update(time);
                    _holdOpen = false;
                    LayoutTabs();
                    UpdateApplyButton();
                    _current.Update(time);
                }
                catch (Exception ex)
                {
                    Break("updating", ex);
                }
            }

            if (_broken)
            {
                CloseBroken();
            }
        }

        // ---------- errors ----------

        /// <summary>Set once a page threw while drawing or updating; the menu then draws nothing and closes on the next update.</summary>
        private bool _broken;

        /// <summary>
        /// Logs the first error and marks the menu broken. Left to throw, an error while drawing makes SMAPI close the
        /// whole title screen, and one while updating would repeat (and be logged) every tick.
        /// </summary>
        private void Break(string doing, Exception ex)
        {
            if (_broken)
            {
                return;
            }

            _broken = true;
            ModEntry.Log.Error($"The DewDrop menu hit an error while {doing}, so it was closed: {ex}");
        }

        /// <summary>Closes only this menu, through the title screen's own back action so its buttons slide back in.</summary>
        private void CloseBroken()
        {
            try
            {
                // Called from the title screen's update, where closing its sub-menu is safe (unlike during its draw)
                if (Game1.activeClickableMenu is TitleMenu title)
                {
                    title.backButtonPressed();
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Error($"Couldn't close the DewDrop menu the usual way: {ex}");
            }

            if (TitleMenu.subMenu == this)
            {
                TitleMenu.subMenu = null;
            }
        }

        // ---------- restart ----------

        private void UpdateApplyButton()
        {
            var mods = ModEntry.Mods;
            var blocked = mods.ApplyBlockedReason;
            _apply.Enabled = blocked is null && mods.NeedsRestart();
            _apply.Tooltip = blocked
                ?? (_apply.Enabled
                    ? $"Restart the game with profile '{mods.ActiveProfile.Name}'."
                    : "Nothing to apply: the loaded mods already match this profile.");
        }

        private void ConfirmRestart()
        {
            var mods = ModEntry.Mods;
            if (mods.ApplyBlockedReason is { } blocked)
            {
                ModEntry.Toast(blocked, error: true);
                Game1.playSound("cancel");
                return;
            }

            if (WaitForInstall())
            {
                return;
            }

            var message = $"Restart Stardew Valley now with the '{mods.ActiveProfile.Name}' profile?";

            // The usual surprise is a profile that leaves out mods the player is using right now
            var turnedOff = mods.LoadedModsTurnedOff();
            if (turnedOff.Count > 0)
            {
                var names = string.Join(", ", turnedOff.Take(3)) + (turnedOff.Count > 3 ? $" and {turnedOff.Count - 3} more" : "");
                message += $"\n\nThis turns off {turnedOff.Count} mod{(turnedOff.Count == 1 ? "" : "s")} loaded right now: {names}.";
            }

            if (mods.Removing.Count > 0)
            {
                var n = mods.Removing.Count;
                message += $"\n\nThis also deletes the {(n == 1 ? "mod" : $"{n} mods")} you uninstalled: {UninstallPrompt.Names(mods.Removing.Select(m => m.Name).ToList())}.";
            }

            var downloads = ModEntry.Downloads.ActiveCount;
            if (downloads > 0)
            {
                message += $"\n\n{downloads} download{(downloads == 1 ? " is" : "s are")} still running and will be cancelled.";
            }

            Confirm(message, "Restart", () =>
            {
                // An install may have started while the dialog was open
                if (WaitForInstall())
                {
                    return;
                }

                if (!ModEntry.Mods.ApplyAndRestart(out var error))
                {
                    ModEntry.Toast(error ?? "Couldn't restart the game.", error: true);
                    Game1.playSound("cancel");
                }
            });
        }

        /// <summary>
        /// Quitting mid-install could leave a mod half extracted, or moved into place but never turned on, so the
        /// restart waits for installs (a job stays Installing until the mod is enabled). Returns whether one is running.
        /// </summary>
        private static bool WaitForInstall()
        {
            var installing = ModEntry.Downloads.Jobs.FirstOrDefault(j => j.State == DownloadState.Installing);
            if (installing is null)
            {
                return false;
            }

            ModEntry.Toast($"{installing.Title} is still installing. Restart when it's done.");
            return true;
        }

        // ---------- drawing ----------

        public override void draw(SpriteBatch b)
        {
            if (_broken)
            {
                return;
            }

            try
            {
                DrawMenu(b);
            }
            catch (Exception ex)
            {
                Break("drawing", ex);
            }
        }

        private void DrawMenu(SpriteBatch b)
        {
            Ui.Banner(b, "DewDrop", _frame.Center.X, _frame.Y - 84);
            Ui.Frame(b, _frame);
            DrawTabs(b);

            // The partition sprite's line sits 28-36 px into its 64 px tile
            drawHorizontalPartition(b, Content.Y - 32, small: true);

            _current.Draw(b);

            drawHorizontalPartition(b, _frame.Bottom - BottomBarHeight - 28, small: true);
            DrawBottomBar(b);
            DrawToasts(b);

            if (_confirm is not null)
            {
                _confirm.Draw(b);
            }
            else if (!string.IsNullOrEmpty(_hoverText))
            {
                drawHoverText(b, Ui.Wrap(_hoverText, 600), Game1.smallFont);
            }
        }

        private void DrawTabs(SpriteBatch b)
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                var bounds = _tabBounds[i];
                var selected = page == _current;
                var hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());

                // The current tab sits a little lower, like the game menu's tabs
                var drawn = selected ? new Rectangle(bounds.X, bounds.Y + 6, bounds.Width, bounds.Height) : bounds;
                drawTextureBox(b, Game1.mouseCursors, Ui.ButtonFrame, drawn.X, drawn.Y, drawn.Width, drawn.Height,
                    selected ? Color.White : hovered ? Color.Wheat : Color.LightGray, 4f, drawShadow: !selected);
                Ui.LabelCentered(b, TabLabel(page), drawn, selected ? Ui.Text : Ui.Muted);
            }
        }

        private void DrawBottomBar(SpriteBatch b)
        {
            var mods = ModEntry.Mods;
            var y = _frame.Bottom - BottomBarHeight + 4;
            var x = _frame.X + Padding;

            // Collection profiles carry the collection's often long name; the full one shows on hover
            var profileText = $"Profile: {mods.ActiveProfile.Name}";
            var shownProfile = Ui.Ellipsize(profileText, _apply.Bounds.X - x - 24, Ui.Large);
            _profileLabel = shownProfile == profileText
                ? Rectangle.Empty
                : new Rectangle(x, y, (int)Ui.Large.MeasureString(shownProfile).X, Ui.LineHeight(Ui.Large));
            Ui.Label(b, shownProfile, new Vector2(x, y), Ui.Text, Ui.Large);

            string status;
            Color color;
            if (mods.Problem is { } problem)
            {
                // Apply & Restart is off until this is sorted, so it matters more than the restart state
                status = problem;
                color = Ui.Bad;
            }
            else if (mods.Kind == SessionKind.Plain && PlainSessionStatus(mods) is { } plain)
            {
                // Started from Steam or a shortcut: say what's different, or that nothing is
                status = plain;
                color = Ui.Attention;
            }
            else if (mods.Kind == SessionKind.Custom)
            {
                status = "This session didn't start from a DewDrop profile. Restart to load this profile.";
                color = Ui.Attention;
            }
            else if (mods.RemovingLoaded() is { Count: > 0 } stillLoaded)
            {
                // Gone from the lists, but running until the game restarts
                var n = stillLoaded.Count;
                status = $"{(n == 1 ? $"{stillLoaded[0].Name}, which you uninstalled, is" : $"{n} mods you uninstalled are")} still loaded. Restart to finish uninstalling.";
                color = Ui.Attention;
            }
            else if (mods.NeedsRestart())
            {
                // With Steam starting the game in the profile, closing the game brings the links up to date
                status = ProfileStart.SteamWanted && ProfileStart.SteamOn
                    ? "Changes load the next time you start the game from Steam, or restart now."
                    : "Changes are ready. Restart the game to load them.";
                color = Ui.Attention;
            }
            else if (mods.Removing.Count > 0)
            {
                // Not loaded, so nothing needs a restart; their folders go when the game closes
                status = "The mods loaded right now match this profile. Uninstalled mods are deleted when the game closes.";
                color = Ui.Muted;
            }
            else
            {
                status = "The mods loaded right now match this profile.";
                color = Ui.Muted;
            }

            var downloads = ModEntry.Downloads.ActiveCount;
            if (downloads > 0)
            {
                status += $"  Downloading {downloads}...";
            }

            // A long warning is cut to the line; the whole of it shows on hover
            var statusWidth = _apply.Bounds.X - x - 24;
            var statusY = y + Ui.LineHeight(Ui.Large) + 2;
            var shownStatus = Ui.Ellipsize(status, statusWidth);
            _statusText = status;
            _statusLabel = shownStatus == status
                ? Rectangle.Empty
                : new Rectangle(x, statusY, (int)Ui.Small.MeasureString(shownStatus).X, Ui.LineHeight());
            Ui.Label(b, shownStatus, new Vector2(x, statusY), color);

            _apply.Draw(b);
        }

        /// <summary>
        /// For a session SMAPI started on the Mods folder: which mods differ from the profile, or null when the same mods
        /// loaded (then only pending changes can call for a restart).
        /// </summary>
        private static string? PlainSessionStatus(ModManager mods)
        {
            var difference = mods.Difference();
            if (difference.TurnedOff.Count > 0)
            {
                var n = difference.TurnedOff.Count;
                return $"{n} mod{(n == 1 ? " that is" : "s that are")} off in this profile loaded anyway. Restart to load this profile.";
            }

            if (difference.NotLoaded.Count > 0)
            {
                var n = difference.NotLoaded.Count;
                return $"{n} mod{(n == 1 ? "" : "s")} in this profile didn't load. Restart to load this profile.";
            }

            if (difference.OtherCopy.Count > 0)
            {
                var n = difference.OtherCopy.Count;
                return $"{n} mod{(n == 1 ? "" : "s")} loaded from a different copy than this profile uses. Restart to load this profile.";
            }

            return null;
        }

        private void DrawToasts(SpriteBatch b)
        {
            _toastBoxes.Clear();
            var toasts = ModEntry.Toasts.Where(t => t.Expires > DateTime.Now).ToList();
            var y = Content.Bottom + 4;

            // The newest sits at the bottom and older ones stack upwards, but only as far as a third of the way up the
            // page, so on short screens they don't bury the list and the tabs
            var top = Content.Y + Content.Height / 3;
            foreach (var toast in Enumerable.Reverse(toasts))
            {
                var text = Ui.Ellipsize(toast.Text, Math.Min(900, _frame.Width - 120));
                var size = Ui.Small.MeasureString(text);
                var box = new Rectangle(_frame.Center.X - (int)size.X / 2 - 24, y - (int)size.Y - 24, (int)size.X + 48, (int)size.Y + 24);
                if (box.Y < top && _toastBoxes.Count > 0)
                {
                    break;
                }

                Ui.Panel(b, box);
                Ui.Label(b, text, new Vector2(box.X + 24, box.Y + 14), toast.IsError ? Ui.Bad : Ui.Text);
                _toastBoxes.Add((box, toast));
                y = box.Y - 8;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current.OnHide();
            Game1.keyboardDispatcher.Subscriber = null;
        }

        /// <summary>A modal yes/no box drawn over the menu.</summary>
        private class ConfirmDialog
        {
            private readonly string _message;
            private readonly UiButton _yes;
            private readonly UiButton _no;
            private readonly Action _onYes;
            private readonly Action _onNo;
            private readonly int _openedTick = Game1.ticks;
            private Rectangle _box;
            private Rectangle _screen;
            private string _wrapped = "";

            public ConfirmDialog(string message, string yesLabel, Action onYes, Action onNo)
            {
                _message = message;
                _onYes = onYes;
                _onNo = onNo;
                _yes = new UiButton(yesLabel, onYes, primary: true);
                _no = new UiButton("Cancel", onNo, primary: true) { Sound = "bigDeSelect" };
            }

            /// <summary>False on the tick the dialog opened, so the key press that opened it can't also answer it.</summary>
            public bool Ready => Game1.ticks != _openedTick;

            public void Accept()
            {
                Game1.playSound("bigSelect");
                _onYes();
            }

            public void Layout(int viewWidth, int viewHeight)
            {
                _screen = new Rectangle(0, 0, viewWidth, viewHeight);
                var boxWidth = Math.Min(820, viewWidth - 80);
                _wrapped = Ui.Wrap(_message, boxWidth - 96);
                var textHeight = (int)Ui.Small.MeasureString(_wrapped).Y;
                var boxHeight = textHeight + 96 + 80;
                _box = new Rectangle((viewWidth - boxWidth) / 2, (viewHeight - boxHeight) / 2, boxWidth, boxHeight);

                _no.AtRight(_box.Right - 48, _box.Bottom - 104);
                _yes.AtRight(_no.Bounds.X - 16, _box.Bottom - 104);
            }

            public void Cancel()
            {
                Game1.playSound("bigDeSelect");
                _onNo();
            }

            public void ReceiveLeftClick(int x, int y)
            {
                if (!_yes.TryClick(x, y) && !_no.TryClick(x, y) && !_box.Contains(x, y))
                {
                    Cancel();
                }
            }

            public void Hover(int x, int y)
            {
                _yes.Hover(x, y);
                _no.Hover(x, y);
            }

            public void Draw(SpriteBatch b)
            {
                Ui.Fill(b, _screen, Color.Black * 0.5f);
                Ui.Frame(b, _box);
                Ui.Label(b, _wrapped, new Vector2(_box.X + 48, _box.Y + 48));
                _yes.Draw(b);
                _no.Draw(b);
            }
        }
    }
}
