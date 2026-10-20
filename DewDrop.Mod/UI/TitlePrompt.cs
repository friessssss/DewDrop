using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.UI
{
    /// <summary>
    /// The "Load your profile?" box ProfileStart opens as the title screen's sub-menu when a launch loaded different mods
    /// than the profile. It can't be closed by the title's back button, so nothing (a save, a co-op game) can start
    /// underneath while the game may be about to restart.
    /// </summary>
    internal class TitlePrompt : IClickableMenu
    {
        private static readonly TimeSpan InputDelay = TimeSpan.FromSeconds(0.5);

        private readonly ProfileStart.Plan _plan;
        private readonly DateTime _opened = DateTime.Now;
        private readonly UiButton _restart;
        private readonly UiButton _notNow;
        private readonly UiButton _dontAsk;
        private readonly UiButton _ok;
        private bool _error;
        private bool _restarting;
        private string _heading = "";
        private string _body = "";
        private string _errorText = "";
        private bool _steam;
        private Rectangle _screen;
        private Rectangle _box;
        private Rectangle _steamRow;
        private string _wrappedBody = "";
        private string _steamLabel = "";
        private string _footer = "";

        public TitlePrompt(ProfileStart.Plan plan)
        {
            _plan = plan;

            // Right after a restart that didn't come back, changing Steam is something to choose, not the default
            _steam = !plan.AfterFailure;
            _restart = new UiButton("Restart now", Restart, primary: true);
            _notNow = new UiButton("Not now", NotNow, primary: true) { Sound = "bigDeSelect" };
            _dontAsk = new UiButton("Don't ask again", DontAskAgain) { Tooltip = "Stop asking when the game starts with different mods. You can turn this back on in Mods > Account." };
            _ok = new UiButton("OK", Close, primary: true) { Sound = "bigDeSelect" };
            Game1.playSound("newArtifact");
            Layout();

            // A controller starts on the safe answer
            if (Game1.options.gamepadControls)
            {
                Game1.setMousePosition(_notNow.Bounds.Center.X, _notNow.Bounds.Center.Y);
            }
        }

        private bool Ready => DateTime.Now - _opened >= InputDelay;

        private static int Downloads => ModEntry.Downloads.ActiveCount;

        // ---------- layout ----------

        private void Layout()
        {
            var viewWidth = Game1.uiViewport.Width;
            var viewHeight = Game1.uiViewport.Height;
            _screen = new Rectangle(0, 0, viewWidth, viewHeight);
            var boxWidth = Math.Min(860, viewWidth - 80);
            var textWidth = boxWidth - 96;

            _heading = _error ? "Couldn't load your profile" : "Load your profile?";
            _body = BodyText();
            _wrappedBody = Ui.Wrap(_body, textWidth);

            var showSteam = !_error && _plan.OfferSteam;
            _steamLabel = showSteam ? Ui.Ellipsize("Start in my profile from Steam from now on", textWidth - Ui.CheckboxSize - 12) : "";
            _footer = _error
                ? ""
                : showSteam
                    ? Ui.Wrap("With that ticked, Steam closes and opens again once after you quit, to save its new launch option.", textWidth)
                    : "You can change this any time in Mods > Account.";

            var headingHeight = Ui.LineHeight(Ui.Large);
            var bodyHeight = (int)Ui.Small.MeasureString(_wrappedBody).Y;
            var footerHeight = _footer.Length > 0 ? (int)Ui.Small.MeasureString(_footer).Y + 12 : 0;
            var boxHeight = 48 + headingHeight + 16 + bodyHeight + (showSteam ? 16 + 48 : 0) + 24 + footerHeight + 72 + 48;
            _box = new Rectangle((viewWidth - boxWidth) / 2, Math.Max(16, (viewHeight - boxHeight) / 2), boxWidth, boxHeight);

            var y = _box.Y + 48 + headingHeight + 16 + bodyHeight + 16;
            _steamRow = showSteam ? new Rectangle(_box.X + 48, y, textWidth, 40) : Rectangle.Empty;

            var buttonsY = _box.Bottom - 48 - 72;
            if (_error)
            {
                _ok.AtRight(_box.Right - 48, buttonsY);
                _dontAsk.Visible = false;
            }
            else
            {
                _notNow.AtRight(_box.Right - 48, buttonsY);
                _restart.AtRight(_notNow.Bounds.X - 16, buttonsY);
                _dontAsk.At(_box.X + 48, buttonsY + 8);
            }
        }

        private string BodyText()
        {
            if (_error)
            {
                return $"{_errorText}\n\nYou can keep playing with the mods loaded right now.";
            }

            var downloads = Downloads;
            return downloads > 0
                ? $"{_plan.Reason}\n\nWaiting for {downloads} download{(downloads == 1 ? "" : "s")} to finish first..."
                : $"{_plan.Reason}\n\nRestart into your profile now? The game closes and opens again by itself.";
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            Layout();
        }

        // ---------- behaviour ----------

        public override bool readyToClose() => false;

        public override void update(GameTime time)
        {
            _restart.Enabled = Downloads == 0;
            if (BodyText() != _body)
            {
                Layout();
            }
        }

        private void Restart()
        {
            if (Downloads > 0 || _restarting || _error)
            {
                return;
            }

            // Once only: if it fails, the box shows why
            _restarting = true;
            var mods = ModEntry.Mods;

            // Answering counts as a choice about every mod named, so they're not "new" next time
            mods.MarkSeen(_plan.Difference.TurnedOff.Select(m => m.UniqueId));
            if (_steamRow != Rectangle.Empty && _steam)
            {
                // Written when the restarted game closes (this one hands over to the restart)
                ProfileStart.SteamWanted = true;
            }

            string? error;
            bool restarted;
            try
            {
                restarted = mods.ApplyAndRestart(out error);
            }
            catch (Exception ex)
            {
                ModEntry.Log.Error($"Couldn't restart into the profile: {ex}");
                restarted = false;
                error = "Couldn't restart the game. The SMAPI log says why.";
            }

            if (!restarted)
            {
                _errorText = error ?? "Couldn't restart the game.";
                _error = true;
                Game1.playSound("cancel");
                Layout();
            }
        }

        private void NotNow()
        {
            ModEntry.Mods.MarkSeen(_plan.Difference.TurnedOff.Select(m => m.UniqueId));
            Close();
        }

        private void DontAskAgain()
        {
            ModEntry.Config.AskToLoadProfile = false;
            ModEntry.SaveConfig();
            NotNow();
        }

        private void Close()
        {
            if (TitleMenu.subMenu == this)
            {
                TitleMenu.subMenu = null;
            }
        }

        /// <summary>Esc or the menu key: the title screen hands these over (see TitleMenuPatches).</summary>
        public void ReceiveMenuKey()
        {
            if (!Ready)
            {
                return;
            }

            Game1.playSound("bigDeSelect");
            if (_error)
            {
                Close();
            }
            else
            {
                NotNow();
            }
        }

        // ---------- input ----------

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!Ready)
            {
                return;
            }

            if (_error)
            {
                _ok.TryClick(x, y);
                return;
            }

            if (_steamRow.Contains(x, y))
            {
                _steam = !_steam;
                Game1.playSound("drumkit6");
                return;
            }

            if (!_restart.TryClick(x, y) && !_notNow.TryClick(x, y))
            {
                _dontAsk.TryClick(x, y);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void receiveKeyPress(Keys key)
        {
            if (!Ready)
            {
                return;
            }

            // Alt+Enter switches fullscreen; it isn't an answer
            var keyboard = Game1.GetKeyboardState();
            if (key == Keys.Enter && !keyboard.IsKeyDown(Keys.LeftAlt) && !keyboard.IsKeyDown(Keys.RightAlt))
            {
                if (_error)
                {
                    Close();
                }
                else if (_restart.Enabled)
                {
                    Game1.playSound("bigSelect");
                    Restart();
                }
            }
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (Ready && b == Buttons.B)
            {
                ReceiveMenuKey();
            }
        }

        public override bool overrideSnappyMenuCursorMovementBan() => true;

        public override void performHoverAction(int x, int y)
        {
            _restart.Hover(x, y);
            _notNow.Hover(x, y);
            _dontAsk.Hover(x, y);
            _ok.Hover(x, y);
        }

        // ---------- drawing ----------

        public override void draw(SpriteBatch b)
        {
            Ui.Fill(b, _screen, Color.Black * 0.5f);
            Ui.Frame(b, _box);
            var y = _box.Y + 48;
            Ui.Label(b, _heading, new Vector2(_box.X + 48, y), Ui.Text, Ui.Large);
            y += Ui.LineHeight(Ui.Large) + 16;
            Ui.Label(b, _wrappedBody, new Vector2(_box.X + 48, y));

            if (_steamRow != Rectangle.Empty)
            {
                Ui.Checkbox(b, new Vector2(_steamRow.X, _steamRow.Center.Y - Ui.CheckboxSize / 2), _steam);
                Ui.Label(b, _steamLabel, new Vector2(_steamRow.X + Ui.CheckboxSize + 12, _steamRow.Center.Y - Ui.LineHeight() / 2));
            }

            if (_footer.Length > 0)
            {
                var footerHeight = (int)Ui.Small.MeasureString(_footer).Y;
                Ui.Label(b, _footer, new Vector2(_box.X + 48, _box.Bottom - 48 - 72 - footerHeight - 12), Ui.Muted);
            }

            if (_error)
            {
                _ok.Draw(b);
            }
            else
            {
                _restart.Draw(b);
                _notNow.Draw(b);
                _dontAsk.Draw(b);
            }

            var tip = _dontAsk.HoverText(Game1.getMouseX(), Game1.getMouseY());
            if (tip is not null)
            {
                drawHoverText(b, Ui.Wrap(tip, 500), Ui.Small);
            }
        }
    }
}
