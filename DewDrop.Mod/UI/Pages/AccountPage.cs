using System.Runtime.Versioning;
using DewDrop.Core.Services;
using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>Nexus Mods login, the nxm:// link handler and DewDrop's settings, in three side-by-side panels.</summary>
    internal class AccountPage : Page
    {
        private const int ColumnGap = 12;   // 16 would cut the "Mod manager links" title in a 1280px wide window
        private const int Pad = 16;
        private const int Gap = 8;          // between controls
        private const int TextGap = 4;      // next to text, which carries its own line spacing
        private const int ButtonGap = 12;
        private const int InputGap = 16;
        private const int StarSize = 32;
        private const int SettingRowHeight = 40;
        private const int SettingRowGap = 4;
        private const int CheckboxInset = 6;

        private const string GetKeyLabel = "Get my API key";
        private const string ShortGetKeyLabel = "Get key";
        private const string BrowserLabel = "Log in with browser";
        private const string ShortBrowserLabel = "Web login";

        private static readonly string[] _columnTitles = { "Nexus Mods", "Mod manager links", "Settings" };
        private static readonly string[] _placeholders = { "Paste your API key (Ctrl+V)", "Paste API key (Ctrl+V)", "Paste your API key", "Paste API key" };

        private readonly KeyInput _keyInput = new(_placeholders[0]);
        private readonly UiButton _logIn;
        private readonly UiButton _getKey;
        private readonly UiButton _browserLogIn;
        private readonly UiButton _logOut;
        private readonly UiButton _useDewDrop;
        private readonly UiButton[] _buttons;
        private readonly Setting[] _settings;
        private readonly TextBlock _accountText = new();
        private readonly TextBlock _linksText = new();
        private readonly TextBlock _linksStatus = new();
        private readonly TextBlock _infoText = new();
        private readonly Rectangle[] _columns = new Rectangle[3];
        private readonly string[] _titles = new string[3];
        private readonly Rectangle[] _settingRows;
        private readonly string[] _settingLabels;
        private LayoutState _state;
        private bool _showKeyInput;
        private int _hoverSetting = -1;

        // Logged-in header, measured in Layout
        private string _nameText = "";
        private string _nameFull = "";
        private SpriteFont _nameFont = Ui.Large;
        private Rectangle _nameBounds;
        private Rectangle _planRow;
        private string _planText = "";

        // Browser sign-in; the counter lets a cancelled attempt's late answer be ignored
        private NexusSso? _sso;
        private int _ssoRequest;

        // Who opens nxm:// links, read from the registry on show rather than every frame
        private bool _linksRegistered;
        private string? _linksHandler;
        private bool _linksUserChoice;
        private string _linksMessage = "";
        private Color _linksColor;

        public AccountPage(DewDropMenu menu) : base(menu)
        {
            _logIn = new UiButton("Log in", LogIn);
            _getKey = new UiButton(GetKeyLabel, () => DownloadManager.OpenUrl(NexusApi.ApiKeyPageUrl))
            {
                Tooltip = "Opens your Nexus Mods account in the browser. Copy the Personal API Key from there."
            };
            _browserLogIn = new UiButton(BrowserLabel, ToggleSso)
            {
                Tooltip = "Approve DewDrop on the Nexus Mods website instead of pasting a key."
            };
            _logOut = new UiButton("Log out", ConfirmLogOut)
            {
                Tooltip = "Forget the saved key. Stardrop uses the same one."
            };
            _useDewDrop = new UiButton("Use DewDrop for these links", ConfirmUseDewDrop)
            {
                Tooltip = "Make the website's Mod Manager Download button send mods to DewDrop."
            };
            _buttons = new[] { _logIn, _getKey, _browserLogIn, _logOut, _useDewDrop };

            _keyInput.Masked = true;
            _keyInput.OnSubmit = _ => LogIn();

            _settings = new[]
            {
                new Setting("Turn on mods when installed", "Mods you download are switched on in the active profile right away.",
                    () => ModEntry.Config.EnableModsOnInstall, value => ModEntry.Config.EnableModsOnInstall = value),
                new Setting("Show adult content in Browse", "Include mods Nexus Mods marks as adult content.",
                    () => ModEntry.Config.ShowAdultContent, value => ModEntry.Config.ShowAdultContent = value),
                new Setting("Check for updates at startup", "Look for new versions of your mods each time the game starts.",
                    () => ModEntry.Config.CheckUpdatesOnLaunch, value => ModEntry.Config.CheckUpdatesOnLaunch = value)
            };
            _settingRows = new Rectangle[_settings.Length];
            _settingLabels = new string[_settings.Length];
        }

        public override string Title => "Account";

        public override bool IsTyping => _keyInput.Selected;

        public override void Layout(Rectangle bounds)
        {
            base.Layout(bounds);
            var columnWidth = (bounds.Width - ColumnGap * 2) / 3;
            for (var i = 0; i < _columns.Length; i++)
            {
                _columns[i] = new Rectangle(bounds.X + i * (columnWidth + ColumnGap), bounds.Y, columnWidth, bounds.Height);
            }

            Relayout();
        }

        public override void OnShow()
        {
            RefreshLinks();
            Relayout();
        }

        public override void OnHide()
        {
            _keyInput.Selected = false;
            CancelSso();
        }

        // ---------- layout ----------

        /// <summary>What the layout depends on besides the size; the page lays itself out again when it changes.</summary>
        private readonly record struct LayoutState(
            bool LoggedIn, string? Name, bool Premium, int DailyRemaining, bool LoggingIn, string? Error,
            bool SsoAvailable, bool SsoWaiting, string Links, int LinksWaiting);

        private LayoutState CurrentState()
        {
            var nexus = ModEntry.Nexus;
            var account = nexus.Account;
            return new LayoutState(
                nexus.IsLoggedIn, account?.Name, nexus.IsPremium, account?.DailyRemaining ?? -1, nexus.LoggingIn, nexus.LastError,
                !string.IsNullOrWhiteSpace(ModEntry.Config.NexusSsoSlug), _sso is not null, _linksMessage, ModEntry.NxmWaiting);
        }

        /// <summary>Settings.json couldn't be read at startup, so the saved login didn't load and a new one isn't saved.</summary>
        private static bool SettingsUnreadable => ModEntry.Mods.Settings.IsReadOnly;

        /// <summary>Places everything for the current login and link state; the mods folder is read here too.</summary>
        private void Relayout()
        {
            _state = CurrentState();
            for (var i = 0; i < _columns.Length; i++)
            {
                _titles[i] = Ui.Ellipsize(_columnTitles[i], Inner(i).Width, Ui.Large);
            }

            LayoutAccount(Body(0));
            LayoutLinks(Body(1));
            LayoutSettings(Body(2));
        }

        private Rectangle Inner(int column)
        {
            var c = _columns[column];
            return new Rectangle(c.X + Pad, c.Y + Pad, Math.Max(1, c.Width - Pad * 2), Math.Max(0, c.Height - Pad * 2));
        }

        /// <summary>A panel's area under its title.</summary>
        private Rectangle Body(int column)
        {
            var inner = Inner(column);
            var top = inner.Y + Ui.LineHeight(Ui.Large);
            return new Rectangle(inner.X, top, inner.Width, Math.Max(0, inner.Bottom - top));
        }

        private void LayoutAccount(Rectangle body)
        {
            var loggedIn = _state.LoggedIn;
            _logOut.Visible = loggedIn;
            _logIn.Visible = _getKey.Visible = !loggedIn;
            _browserLogIn.Visible = !loggedIn && _state.SsoAvailable;

            if (loggedIn)
            {
                LayoutLoggedIn(body);
                _showKeyInput = false;
            }
            else
            {
                LayoutLoggedOut(body);
            }

            if (!_showKeyInput)
            {
                _keyInput.Selected = false;
            }

            foreach (var button in new[] { _logIn, _getKey, _browserLogIn, _logOut })
            {
                Clip(button, body);
            }
        }

        private void LayoutLoggedIn(Rectangle body)
        {
            // The big font when the whole line fits, else the small one: a cut-off name helps nobody
            _nameFull = $"Logged in as {_state.Name}";
            _nameFont = Ui.Large.MeasureString(_nameFull).X <= body.Width ? Ui.Large : Ui.Small;
            _nameText = Ui.Ellipsize(_nameFull, body.Width, _nameFont);
            _nameBounds = new Rectangle(body.X, body.Y, body.Width, Ui.LineHeight(_nameFont));

            var rowY = _nameBounds.Bottom + TextGap;
            _logOut.AtRight(body.Right, rowY);
            _planRow = new Rectangle(body.X, rowY, Math.Max(0, _logOut.Bounds.X - ButtonGap - body.X), _logOut.PreferredHeight);
            var planWidth = _planRow.Width - (_state.Premium ? StarSize + 8 : 0);
            _planText = Ui.Ellipsize(_state.Premium ? "Premium" : "Free account", Math.Max(0, planWidth));

            var paragraphs = new List<TextLine>();

            // Logged in, but the key couldn't be saved for next time
            if (!string.IsNullOrWhiteSpace(_state.Error))
            {
                paragraphs.Add(new TextLine(Ui.PlainText(_state.Error), Ui.Attention));
            }
            else if (SettingsUnreadable)
            {
                paragraphs.Add(new TextLine("Stardrop's Settings.json couldn't be read, so this login isn't saved for next time.", Ui.Attention));
            }

            paragraphs.Add(_state.Premium
                ? new TextLine("Install buttons download directly.", Ui.Text)
                : new TextLine("Downloads go through the website's \"Slow download\" button.", Ui.Text));
            if (_state.DailyRemaining >= 0)
            {
                paragraphs.Add(new TextLine($"API requests left today: {_state.DailyRemaining}", Ui.Muted));
            }

            var textTop = _planRow.Bottom + TextGap;
            _accountText.Layout(new Rectangle(body.X, textTop, body.Width, Math.Max(0, body.Bottom - textTop)), paragraphs);
        }

        private void LayoutLoggedOut(Rectangle body)
        {
            _logIn.Label = _state.LoggingIn ? "Logging in..." : "Log in";
            _logIn.Enabled = !_state.LoggingIn;
            _browserLogIn.Enabled = _state.SsoWaiting || !_state.LoggingIn;

            var paragraphs = new List<TextLine>();
            if (_state.SsoWaiting)
            {
                paragraphs.Add(new TextLine("Waiting for approval in your browser...", Ui.Attention));
            }
            if (!string.IsNullOrWhiteSpace(_state.Error))
            {
                paragraphs.Add(new TextLine(Ui.PlainText(_state.Error), Ui.Bad));
            }
            if (_state.LinksWaiting > 0)
            {
                var waiting = _state.LinksWaiting;
                paragraphs.Add(new TextLine($"{waiting} download{(waiting == 1 ? " from the website is" : "s from the website are")} waiting for you to log in.", Ui.Attention));
            }
            if (SettingsUnreadable)
            {
                paragraphs.Add(new TextLine("Stardrop's Settings.json couldn't be read, so your saved login didn't load and a new one won't be saved.", Ui.Bad));
            }
            paragraphs.Add(new TextLine("DewDrop uses your personal Nexus Mods API key. It's shared with Stardrop.", Ui.Text));

            // When wide and tall enough, the key box gets a row of its own with Log in and Get my API key side by
            // side under it; otherwise the buttons come first and the box shares a row with Log in
            var logInWidth = WidestLabel(_logIn, "Log in", "Logging in...");
            var getKeyWidth = WidestLabel(_getKey, GetKeyLabel);
            var ssoWidth = WidestLabel(_browserLogIn, BrowserLabel, "Cancel");

            // In a narrow panel the two link buttons can't share a row, and a third row of buttons doesn't fit
            // a small window; short labels let them sit side by side (their tooltips say what they do)
            var shortLabels = _browserLogIn.Visible && getKeyWidth + ButtonGap + ssoWidth > body.Width;
            var shortGetKeyWidth = shortLabels ? WidestLabel(_getKey, ShortGetKeyLabel) : getKeyWidth;
            var shortSsoWidth = shortLabels ? WidestLabel(_browserLogIn, ShortBrowserLabel, "Cancel") : ssoWidth;

            var stackedButtons = new List<(UiButton, int)> { (_logIn, logInWidth), (_getKey, getKeyWidth) };
            var shortButtons = new List<(UiButton, int)> { (_getKey, shortGetKeyWidth) };
            if (_browserLogIn.Visible)
            {
                stackedButtons.Add((_browserLogIn, ssoWidth));
                shortButtons.Add((_browserLogIn, shortSsoWidth));
            }

            var inputHeight = _keyInput.Height;
            var buttonHeight = _logIn.PreferredHeight;
            var stackedHeight = inputHeight + Gap + Flow(stackedButtons, body, body.Y, place: false);
            var shortHeight = Flow(shortButtons, body, body.Y, place: false) + Gap + buttonHeight;
            var wantedText = TextBlock.Measure(paragraphs, body.Width);
            var oneRow = logInWidth + ButtonGap + getKeyWidth <= body.Width;
            var stacked = oneRow && (stackedHeight <= shortHeight
                || body.Height - stackedHeight - TextGap >= Math.Min(wantedText, Ui.LineHeight() * 2));

            shortLabels &= !stacked;
            _getKey.Label = shortLabels ? ShortGetKeyLabel : GetKeyLabel;
            _browserLogIn.Label = _state.SsoWaiting ? "Cancel" : shortLabels ? ShortBrowserLabel : BrowserLabel;

            var textHeight = Math.Min(wantedText, Math.Max(0, body.Height - (stacked ? stackedHeight : shortHeight) - TextGap));
            _accountText.Layout(new Rectangle(body.X, body.Y, body.Width, textHeight), paragraphs);

            var y = textHeight > 0 ? body.Y + textHeight + TextGap : body.Y;
            if (stacked)
            {
                _keyInput.Bounds = new Rectangle(body.X, y, body.Width, inputHeight);
                Flow(stackedButtons, body, y + inputHeight + Gap, place: true);
            }
            else
            {
                y += Flow(shortButtons, body, y, place: true) + Gap;

                // The widest label's width, as in the stacked layout, so the key box doesn't resize while logging in
                var width = Math.Min(logInWidth, body.Width);
                _logIn.Bounds = new Rectangle(body.Right - width, y, width, buttonHeight);
                var inputWidth = Math.Max(0, _logIn.Bounds.X - InputGap - body.X);
                _keyInput.Bounds = new Rectangle(body.X, y + (buttonHeight - inputHeight) / 2, inputWidth, inputHeight);
            }

            _keyInput.Placeholder = FitPlaceholder(_keyInput.Width - 32);
            _showKeyInput = _keyInput.Width >= 64 && body.Contains(_keyInput.Bounds);
        }

        private void LayoutLinks(Rectangle body)
        {
            var explanation = new List<TextLine>
            {
                new("Free accounts download with the website's \"Mod Manager Download\" or \"Slow download\" button, which opens an nxm:// link.", Ui.Text)
            };
            var status = new List<TextLine>();
            if (_linksMessage.Length > 0)
            {
                status.Add(new TextLine(_linksMessage, _linksColor));
            }

            _useDewDrop.Visible = OperatingSystem.IsWindows() && !_linksRegistered && _linksMessage.Length > 0;
            _useDewDrop.Label = "Use DewDrop for these links";
            if (_useDewDrop.PreferredWidth > body.Width)
            {
                _useDewDrop.Label = "Use DewDrop";
            }

            // The status matters most, so it keeps all its lines; the explanation gets what's left. The button
            // follows text, so it only needs TextGap: with Gap a two-line status is cut in a 1280x720 window
            var room = Math.Max(0, body.Height - (_useDewDrop.Visible ? _useDewDrop.PreferredHeight + TextGap : 0));
            var statusHeight = Math.Min(TextBlock.Measure(status, body.Width), room);
            var explanationHeight = Math.Min(TextBlock.Measure(explanation, body.Width), Math.Max(0, room - statusHeight - TextGap));

            _linksText.Layout(new Rectangle(body.X, body.Y, body.Width, explanationHeight), explanation);
            var y = explanationHeight > 0 ? body.Y + explanationHeight + TextGap : body.Y;
            _linksStatus.Layout(new Rectangle(body.X, y, body.Width, statusHeight), status);
            y += statusHeight + TextGap;

            _useDewDrop.At(body.X, y);
            _useDewDrop.Bounds.Width = Math.Min(_useDewDrop.Bounds.Width, body.Width);
            Clip(_useDewDrop, body);
        }

        private void LayoutSettings(Rectangle body)
        {
            var y = body.Y;
            var labelWidth = Math.Max(0, body.Width - CheckboxInset * 2 - Ui.CheckboxSize - 12);
            for (var i = 0; i < _settings.Length; i++)
            {
                var row = new Rectangle(body.X, y, body.Width, SettingRowHeight);
                _settingRows[i] = row.Bottom <= body.Bottom ? row : Rectangle.Empty;
                _settingLabels[i] = Ui.Ellipsize(_settings[i].Label, labelWidth);
                y += SettingRowHeight + SettingRowGap;
            }

            var info = new List<TextLine>
            {
                new($"Mods folder: {ModEntry.Mods.InstallFolder}", Ui.Muted, Wrap: false),
                new("Profiles are shared with Stardrop.", Ui.Muted)
            };
            if (!ModEntry.Mods.RunningFromProfileFolder)
            {
                info.Add(new TextLine("This session started outside DewDrop's profile folder.", Ui.Attention));
            }

            var infoTop = y - SettingRowGap + TextGap;
            _infoText.Layout(new Rectangle(body.X, infoTop, body.Width, Math.Max(0, body.Bottom - infoTop)), info);
        }

        /// <summary>Lays buttons out left to right, wrapping onto new rows; returns the height they take.</summary>
        private static int Flow(IReadOnlyList<(UiButton Button, int Width)> buttons, Rectangle area, int top, bool place)
        {
            var x = area.X;
            var y = top;
            var rowHeight = 0;
            foreach (var (button, wanted) in buttons)
            {
                var width = Math.Min(wanted, area.Width);
                if (x > area.X && x + width > area.Right)
                {
                    x = area.X;
                    y += rowHeight + Gap;
                    rowHeight = 0;
                }

                rowHeight = Math.Max(rowHeight, button.PreferredHeight);
                if (place)
                {
                    button.Bounds = new Rectangle(x, y, width, button.PreferredHeight);
                }
                x += width + ButtonGap;
            }

            return y + rowHeight - top;
        }

        /// <summary>The width a button needs for the longest of its labels, so it doesn't jump when the label changes.</summary>
        private static int WidestLabel(UiButton button, params string[] labels)
        {
            var current = button.Label;
            var widest = 0;
            foreach (var label in labels)
            {
                button.Label = label;
                widest = Math.Max(widest, button.PreferredWidth);
            }

            button.Label = current;
            return widest;
        }

        /// <summary>Hides a control that doesn't fit its panel, which only happens in very small windows.</summary>
        private static void Clip(UiButton button, Rectangle area)
        {
            if (!area.Contains(button.Bounds))
            {
                button.Visible = false;
            }
        }

        private static string FitPlaceholder(int width)
        {
            foreach (var text in _placeholders)
            {
                if (Ui.Small.MeasureString(text).X <= width)
                {
                    return text;
                }
            }

            return Ui.Ellipsize(_placeholders[^1], Math.Max(0, width));
        }

        // ---------- Nexus login ----------

        private void LogIn()
        {
            var key = _keyInput.Text.Trim();
            if (key.Length == 0)
            {
                ModEntry.Toast("Paste your API key first. 'Get my API key' shows where to find it.", error: true);
                Game1.playSound("cancel");
                return;
            }

            if (ModEntry.Nexus.LoggingIn)
            {
                return;
            }

            ModEntry.Nexus.Login(key, save: true, onDone: OnLoginDone);
            Relayout();
        }

        private void OnLoginDone(bool ok)
        {
            if (ok)
            {
                _keyInput.Clear();
                _keyInput.Selected = false;
                ModEntry.Toast($"Logged in to Nexus Mods as {ModEntry.Nexus.Account?.Name}.");
                if (ModEntry.Nexus.LastError is { } saveError)
                {
                    // The key works this session but won't be there next time
                    ModEntry.Toast(Ui.PlainText(saveError), error: true);
                }
                Game1.playSound("coin");
            }
            else
            {
                ModEntry.Toast(Ui.PlainText(ModEntry.Nexus.LastError ?? "Couldn't log in to Nexus Mods."), error: true);
                Game1.playSound("cancel");
            }

            Relayout();
        }

        private void ConfirmLogOut()
        {
            var name = ModEntry.Nexus.Account?.Name;
            var who = string.IsNullOrEmpty(name) ? "" : $" as {name}";
            Menu.Confirm($"Log out of Nexus Mods{who}?\n\nStardrop uses the same saved key, so it's logged out too.", "Log out", () =>
            {
                ModEntry.Nexus.Logout();
                ModEntry.Toast("Logged out of Nexus Mods.");
                Relayout();
            });
        }

        private void ToggleSso()
        {
            if (_sso is null)
            {
                StartSso();
            }
            else
            {
                CancelSso();
            }

            Relayout();
        }

        /// <summary>Runs the browser sign-in in the background; the answer comes back on the main thread.</summary>
        private void StartSso()
        {
            var slug = ModEntry.Config.NexusSsoSlug?.Trim() ?? "";
            if (slug.Length == 0 || _sso is not null || ModEntry.Nexus.LoggingIn)
            {
                return;
            }

            var sso = new NexusSso();
            var request = ++_ssoRequest;
            _sso = sso;
            MainThread.Run(
                () => sso.GetApiKeyAsync(slug, CancellationToken.None),
                apiKey =>
                {
                    sso.Dispose();
                    OnSsoDone(request, apiKey, null);
                },
                ex =>
                {
                    sso.Dispose();
                    OnSsoDone(request, null, ex);
                });
        }

        private void CancelSso()
        {
            if (_sso is null)
            {
                return;
            }

            // The attempt finishes in the background; bumping the counter makes its answer stale
            _ssoRequest++;
            _sso.Cancel();
            _sso = null;
        }

        private void OnSsoDone(int request, string? apiKey, Exception? error)
        {
            if (request != _ssoRequest)
            {
                return;
            }

            _sso = null;
            if (error is not null)
            {
                ModEntry.Toast($"Browser login didn't work: {Ui.PlainText(error.Message)}", error: true);
                Game1.playSound("cancel");
            }
            else if (apiKey is not null)
            {
                if (ModEntry.Nexus.LoggingIn)
                {
                    ModEntry.Toast("Nexus approved DewDrop, but another login was still running. Try again.", error: true);
                }
                else
                {
                    ModEntry.Nexus.Login(apiKey, save: true, onDone: OnLoginDone);
                }
            }

            Relayout();
        }

        // ---------- nxm:// links ----------

        private void RefreshLinks()
        {
            _linksRegistered = false;
            _linksHandler = null;
            _linksUserChoice = false;
            if (!OperatingSystem.IsWindows())
            {
                _linksMessage = "Only available on Windows for now.";
                _linksColor = Ui.Muted;
                return;
            }

            try
            {
                _linksRegistered = ModEntry.Nxm.IsRegistered(ModEntry.NxmHandlerScriptPath);
                _linksHandler = _linksRegistered ? null : HandlerName(ModEntry.Nxm.CurrentHandler());
                _linksUserChoice = !_linksRegistered && ModEntry.Nxm.UserChoiceProgId() is not null;
                var problem = _linksRegistered ? null : LinksProblem();
                _linksMessage = _linksRegistered
                    ? "DewDrop handles these links."
                    : problem
                        ?? (_linksHandler is null ? "Nothing handles them yet." : $"{char.ToUpperInvariant(_linksHandler[0])}{_linksHandler[1..]} handles them right now.");
                _linksColor = _linksRegistered ? Ui.Good : problem is not null ? Ui.Attention : Ui.Text;
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read the nxm:// link handler: {ex.Message}");
                _linksMessage = "Couldn't check which app handles them.";
                _linksColor = Ui.Bad;
            }
        }

        /// <summary>
        /// Why the website's links don't reach DewDrop, when that's more than "set it up": Windows' Default apps choice
        /// sends them elsewhere (which 'Use DewDrop' can't change), or Stardrop took them back after DewDrop had them.
        /// Null otherwise. It reads the registry, so call it when a tab is shown, not every frame.
        /// </summary>
        [SupportedOSPlatform("windows")]
        internal static string? LinksProblem()
        {
            var nxm = ModEntry.Nxm;
            if (nxm.IsRegistered(ModEntry.NxmHandlerScriptPath))
            {
                return null;
            }

            var handler = HandlerName(nxm.CurrentHandler());
            if (nxm.UserChoiceProgId() is not null)
            {
                return $"Windows' Default apps setting sends these links to {handler ?? "another app"}, which 'Use DewDrop' can't change. Change NXM under Windows Settings, Apps, Default apps.";
            }

            // The handler script is only written when DewDrop registers, so it's there if DewDrop had the links before
            if (handler == "Stardrop" && File.Exists(ModEntry.NxmHandlerScriptPath))
            {
                return "Stardrop took these links back. It does that every time it starts while logged in to Nexus.";
            }

            return null;
        }

        /// <summary>A friendly name for the registered nxm:// command, or null if there is none.</summary>
        internal static string? HandlerName(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            if (command.Contains("Stardrop", StringComparison.OrdinalIgnoreCase))
            {
                return "Stardrop";
            }
            if (command.Contains("Vortex", StringComparison.OrdinalIgnoreCase))
            {
                return "Vortex";
            }
            if (command.Contains("ModOrganizer", StringComparison.OrdinalIgnoreCase) || command.Contains("Mod Organizer", StringComparison.OrdinalIgnoreCase))
            {
                return "Mod Organizer";
            }

            return "another app";
        }

        private void ConfirmUseDewDrop()
        {
            var replaces = _linksUserChoice
                ? $"Windows' Default apps setting sends them to {_linksHandler ?? "another app"} until you change it there (Windows Settings, Apps, Default apps). DewDrop can't change that, but it can be ready for when you do."
                : _linksHandler is null
                    ? "Nothing opens them right now."
                    : $"This replaces {_linksHandler}, which opens them now.";
            Menu.Confirm($"Let DewDrop open the website's Mod Manager Download (nxm://) links?\n\n{replaces}\n\nStardrop takes these links back automatically every time it starts while logged in to Nexus.", "Use DewDrop", UseDewDropForLinks);
        }

        private void UseDewDropForLinks()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                var script = ModEntry.NxmHandlerScriptPath;
                Directory.CreateDirectory(Path.GetDirectoryName(script)!);
                ModEntry.Nxm.Register(script);
                if (ModEntry.Nxm.IsRegistered(script))
                {
                    ModEntry.Toast("DewDrop now opens Mod Manager Download links.");
                    Game1.playSound("newArtifact");
                }
                else
                {
                    // Registered, but Windows' Default apps choice still wins
                    ModEntry.Toast(LinksProblem() ?? "DewDrop is set up, but Windows still sends these links to another app.", error: true);
                    Game1.playSound("cancel");
                }
            }
            catch (Exception ex)
            {
                ModEntry.Toast($"Couldn't make DewDrop the link handler: {Ui.PlainText(ex.Message)}", error: true);
                Game1.playSound("cancel");
            }

            RefreshLinks();
            Relayout();
        }

        // ---------- settings ----------

        private sealed record Setting(string Label, string Description, Func<bool> Get, Action<bool> Set);

        private static void Toggle(Setting setting)
        {
            setting.Set(!setting.Get());
            Game1.playSound("drumkit6");
            ModEntry.SaveConfig();
        }

        // ---------- input ----------

        public override void Update(GameTime time)
        {
            _keyInput.Tick();

            // Logged in some other way while the browser sign-in was waiting
            if (_sso is not null && ModEntry.Nexus.IsLoggedIn)
            {
                CancelSso();
            }

            if (!CurrentState().Equals(_state))
            {
                Relayout();
            }
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (_showKeyInput)
            {
                _keyInput.Click(x, y);
            }
            else
            {
                _keyInput.Selected = false;
            }

            foreach (var button in _buttons)
            {
                if (button.TryClick(x, y))
                {
                    return;
                }
            }

            if (_accountText.ReceiveLeftClick(x, y) || _linksText.ReceiveLeftClick(x, y) || _linksStatus.ReceiveLeftClick(x, y) || _infoText.ReceiveLeftClick(x, y))
            {
                return;
            }

            for (var i = 0; i < _settings.Length; i++)
            {
                if (_settingRows[i].Contains(x, y))
                {
                    Toggle(_settings[i]);
                    return;
                }
            }
        }

        public override void LeftClickHeld(int x, int y)
        {
            _accountText.LeftClickHeld(x, y);
            _linksText.LeftClickHeld(x, y);
            _linksStatus.LeftClickHeld(x, y);
            _infoText.LeftClickHeld(x, y);
        }

        public override void ReleaseLeftClick(int x, int y)
        {
            _accountText.ReleaseLeftClick();
            _linksText.ReleaseLeftClick();
            _linksStatus.ReleaseLeftClick();
            _infoText.ReleaseLeftClick();
        }

        public override void ReceiveScrollWheel(int direction)
        {
            _accountText.ReceiveScrollWheel(direction);
            _linksText.ReceiveScrollWheel(direction);
            _linksStatus.ReceiveScrollWheel(direction);
            _infoText.ReceiveScrollWheel(direction);
        }

        public override void ReceiveKeyPress(Keys key)
        {
            if (_keyInput.Selected)
            {
                return;
            }

            // Enter logs in with a pasted key even after clicking away from the box
            if (key == Keys.Enter && _showKeyInput && _keyInput.Text.Trim().Length > 0)
            {
                LogIn();
            }
        }

        public override string? PerformHover(int x, int y)
        {
            if (_showKeyInput)
            {
                _keyInput.Hover(x, y);
            }

            string? tooltip = null;
            foreach (var button in _buttons)
            {
                button.Hover(x, y);
                tooltip ??= button.HoverText(x, y);
            }

            // A plain loop: this runs every frame, and a capturing lambda would allocate each time
            _hoverSetting = -1;
            for (var i = 0; i < _settingRows.Length; i++)
            {
                if (_settingRows[i].Contains(x, y))
                {
                    _hoverSetting = i;
                    break;
                }
            }

            if (tooltip is not null)
            {
                return tooltip;
            }

            if (_hoverSetting >= 0)
            {
                var setting = _settings[_hoverSetting];
                return _settingLabels[_hoverSetting] == setting.Label ? setting.Description : $"{setting.Label}\n{setting.Description}";
            }

            if (_state.LoggedIn && _nameText != _nameFull && _nameBounds.Contains(x, y))
            {
                return _nameFull;
            }

            return _accountText.Tooltip(x, y) ?? _linksText.Tooltip(x, y) ?? _linksStatus.Tooltip(x, y) ?? _infoText.Tooltip(x, y);
        }

        // ---------- drawing ----------

        public override void Draw(SpriteBatch b)
        {
            for (var i = 0; i < _columns.Length; i++)
            {
                Ui.Panel(b, _columns[i]);
                var inner = Inner(i);
                Ui.Label(b, _titles[i], new Vector2(inner.X, inner.Y), Ui.Text, Ui.Large);
            }

            DrawAccount(b);

            _linksText.Draw(b);
            _linksStatus.Draw(b);
            _useDewDrop.Draw(b);

            DrawSettings(b);
        }

        private void DrawAccount(SpriteBatch b)
        {
            if (_state.LoggedIn)
            {
                Ui.Label(b, _nameText, new Vector2(_nameBounds.X, _nameBounds.Y), Ui.Text, _nameFont);

                var x = _planRow.X;
                if (_state.Premium)
                {
                    Ui.Sprite(b, Ui.Star, new Vector2(x, _planRow.Center.Y - StarSize / 2), StarSize / (float)Ui.Star.Width);
                    x += StarSize + 8;
                }
                Ui.Label(b, _planText, new Vector2(x, _planRow.Center.Y - Ui.LineHeight() / 2), _state.Premium ? Ui.Good : Ui.Text);
                _logOut.Draw(b);
            }
            else
            {
                if (_showKeyInput)
                {
                    _keyInput.Draw(b);
                }
                _getKey.Draw(b);
                _logIn.Draw(b);
                _browserLogIn.Draw(b);
            }

            _accountText.Draw(b);
        }

        private void DrawSettings(SpriteBatch b)
        {
            var lineHeight = Ui.LineHeight();
            for (var i = 0; i < _settings.Length; i++)
            {
                var row = _settingRows[i];
                if (row.IsEmpty)
                {
                    continue;
                }

                if (i == _hoverSetting)
                {
                    Ui.Fill(b, row, Ui.Hover);
                }

                var checkboxX = row.X + CheckboxInset;
                Ui.Checkbox(b, new Vector2(checkboxX, row.Center.Y - Ui.CheckboxSize / 2), _settings[i].Get());
                Ui.Label(b, _settingLabels[i], new Vector2(checkboxX + Ui.CheckboxSize + 12, row.Center.Y - lineHeight / 2));
            }

            _infoText.Draw(b);
        }

        // ---------- helpers ----------

        private sealed record TextLine(string Text, Color Color, bool Wrap = true);

        /// <summary>
        /// Wrapped text in a fixed area: drawn as is when it fits, in a scroll list when the area is tall enough
        /// for one, and otherwise cut short with "..." and the whole text as its tooltip.
        /// </summary>
        private class TextBlock
        {
            private const int ListGap = 12;          // ScrollList's gap between cells
            private const int ScrollBarSpace = 56;   // ScrollList's bar, plus the 4px its arrows stick out
            private const int MinScrollHeight = 160; // room for both arrows and the thumb
            private const int WrapSlack = 2;         // the game's wrap can run a pixel past the width it's given

            private readonly ScrollList<TextLine> _list = new(1);
            private List<TextLine> _lines = new();
            private bool _scrolls;
            private string? _fullText;

            public Rectangle Bounds { get; private set; }

            /// <summary>How tall the paragraphs are at a width, without scrolling.</summary>
            public static int Measure(IReadOnlyList<TextLine> paragraphs, int width)
            {
                return Split(paragraphs, width, out _).Count * Ui.LineHeight();
            }

            public void Layout(Rectangle area, IReadOnlyList<TextLine> paragraphs)
            {
                Bounds = area;
                _scrolls = false;
                var lineHeight = Ui.LineHeight();
                _lines = Split(paragraphs, area.Width, out var cut);

                if (_lines.Count * lineHeight > area.Height)
                {
                    if (area.Height >= MinScrollHeight)
                    {
                        _scrolls = true;
                        _lines = Split(paragraphs, area.Width - ScrollBarSpace, out cut);

                        // Rows sit one line apart; the last row's text runs a gap past its cell, so the list
                        // stops that much short of the area
                        _list.RowHeight = Math.Max(1, lineHeight - ListGap);
                        _list.Layout(new Rectangle(area.X, area.Y, area.Width - 4, area.Height - ListGap));
                        _list.SetItems(_lines, resetScroll: true);
                    }
                    else
                    {
                        var fit = area.Height / lineHeight;
                        _lines = _lines.Take(fit).ToList();
                        if (fit > 0)
                        {
                            _lines[^1] = _lines[^1] with { Text = WithDots(_lines[^1].Text, area.Width) };
                        }
                        cut = true;
                    }
                }

                _fullText = cut ? string.Join("\n", paragraphs.Select(p => p.Text)) : null;
            }

            public void Draw(SpriteBatch b)
            {
                if (_scrolls)
                {
                    _list.Draw(b, (batch, line, cell, _) => Ui.Label(batch, line.Text, new Vector2(cell.X, cell.Y), line.Color));
                    return;
                }

                var lineHeight = Ui.LineHeight();
                for (var i = 0; i < _lines.Count; i++)
                {
                    Ui.Label(b, _lines[i].Text, new Vector2(Bounds.X, Bounds.Y + i * lineHeight), _lines[i].Color);
                }
            }

            public string? Tooltip(int x, int y) => _fullText is not null && Bounds.Contains(x, y) ? _fullText : null;

            public bool ReceiveLeftClick(int x, int y) => _scrolls && _list.ReceiveLeftClick(x, y);

            public void LeftClickHeld(int x, int y)
            {
                if (_scrolls)
                {
                    _list.LeftClickHeld(x, y);
                }
            }

            public void ReleaseLeftClick() => _list.ReleaseLeftClick();

            public void ReceiveScrollWheel(int direction)
            {
                if (_scrolls)
                {
                    _list.ReceiveScrollWheel(direction);
                }
            }

            private static List<TextLine> Split(IReadOnlyList<TextLine> paragraphs, int width, out bool cut)
            {
                width = Math.Max(1, width);
                cut = false;
                var lines = new List<TextLine>();
                foreach (var paragraph in paragraphs)
                {
                    var parts = paragraph.Wrap ? Ui.Wrap(paragraph.Text, Math.Max(1, width - WrapSlack)).Split('\n') : new[] { paragraph.Text };
                    foreach (var part in parts)
                    {
                        // Wrapped lines end in a space that counts towards their width, so trim it before the fit
                        // check. A word too long to wrap (a path, a URL) is cut rather than drawn past the edge
                        var line = part.TrimEnd('\r', ' ');
                        var fitted = Ui.Ellipsize(line, width);
                        cut |= fitted != line;
                        lines.Add(paragraph with { Text = fitted });
                    }
                }

                return lines;
            }

            /// <summary>A line ending in "..." to show more text follows.</summary>
            private static string WithDots(string text, int width)
            {
                text = text.TrimEnd();
                while (text.Length > 0 && Ui.Small.MeasureString(text + "...").X > width)
                {
                    text = text[..^1].TrimEnd();
                }

                return text + "...";
            }
        }

        /// <summary>
        /// The key box. It masks like any TextInput, but only draws as many dots as fit: the vanilla box would
        /// push a long key's dots past its right edge.
        /// </summary>
        private class KeyInput : TextInput
        {
            private readonly float _dotWidth;

            public KeyInput(string placeholder) : base(placeholder, limitToWidth: false)
            {
                _dotWidth = Math.Max(1f, Font.MeasureString(new string('*', 10)).X / 10f);
            }

            public override void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
            {
                var key = Text;
                var fit = Math.Max(1, (int)((Width - 40) / _dotWidth));
                if (!Masked || key.Length <= fit)
                {
                    base.Draw(spriteBatch, drawShadow);
                    return;
                }

                // Masked anyway, so the first characters stand in for the whole key while drawing
                Text = key[..fit];
                base.Draw(spriteBatch, drawShadow);
                Text = key;
            }
        }
    }
}
