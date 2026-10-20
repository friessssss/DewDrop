using System.Globalization;
using DewDrop.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace DewDrop.UI.Pages
{
    /// <summary>Downloads started from DewDrop or handed over by the website, with progress, results and retry.</summary>
    internal class DownloadsPage : Page
    {
        private const int RowHeight = 84;

        /// <summary>Where the list starts below the top of the page; the account note may use all of this band.</summary>
        private const int ListTop = 72;

        /// <summary>Below this the account note isn't drawn at all (nor its tooltip shown).</summary>
        private const int MinExplanationWidth = 60;

        private readonly UiButton _clearFinished;
        private readonly UiButton _openFolder;
        private readonly UiButton _login;
        private readonly UiButton _setUp;
        private readonly ScrollList<DownloadJob> _list = new(RowHeight);
        private readonly Dictionary<DownloadJob, RowButtons> _buttons = new();
        private string _explanation = "";
        private (string Text, int Width, List<string> Lines) _explanationCache = ("", -1, new List<string>());
        private bool _needsRestart;
        private bool _handlerReady = true;

        /// <summary>Why the website's links don't reach DewDrop when that's something the Set up button can't fix, or null.</summary>
        private string? _handlerProblem;

        /// <summary>The buttons on one job's row, made once and moved with the row.</summary>
        private sealed record RowButtons(UiButton Cancel, UiButton Retry, UiButton View);

        public DownloadsPage(DewDropMenu menu) : base(menu)
        {
            _clearFinished = new UiButton("Clear finished", () =>
            {
                ModEntry.Downloads.ClearFinished();
                Refresh();
            })
            {
                Tooltip = "Remove finished and failed downloads from this list."
            };
            _openFolder = new UiButton("Open mods folder", OpenModsFolder)
            {
                Tooltip = "Open the folder new mods are installed into."
            };
            _login = new UiButton("Log in", () => Menu.ShowPage(Menu.Account))
            {
                Tooltip = "Go to the Account tab."
            };
            _setUp = new UiButton("Set up", () => Menu.ShowPage(Menu.Account))
            {
                Tooltip = "Let the website's 'Mod manager download' buttons send files to DewDrop. Set it up on the Account tab."
            };
            _list.EmptyText = "No downloads yet. Find mods on the Browse tab.";

            // Only finished rows do something on click (they open the Installed tab)
            _list.CanClick = job => job.State == DownloadState.Done;
        }

        public override string Title => "Downloads";

        public override string? Badge
        {
            get
            {
                var active = ModEntry.Downloads.ActiveCount;
                return active > 0 ? active.ToString(CultureInfo.InvariantCulture) : null;
            }
        }

        public override void Layout(Rectangle bounds)
        {
            base.Layout(bounds);
            _clearFinished.At(bounds.X, bounds.Y - 4);
            _openFolder.At(_clearFinished.Bounds.Right + 16, bounds.Y - 4);
            _login.AtRight(bounds.Right, bounds.Y - 4);
            _setUp.AtRight(bounds.Right, bounds.Y - 4);

            var listTop = bounds.Y + ListTop;
            _list.Layout(new Rectangle(bounds.X, listTop, bounds.Width, bounds.Bottom - listTop));
        }

        public override void OnShow()
        {
            CheckHandler();
            Refresh();
            UpdateTopRow();
        }

        /// <summary>Copies the jobs into the list (keeping the scroll) and forgets buttons for jobs that are gone.</summary>
        private void Refresh()
        {
            var jobs = ModEntry.Downloads.Jobs;
            _list.SetItems(jobs);

            if (_buttons.Keys.Any(j => !jobs.Contains(j)))
            {
                foreach (var gone in _buttons.Keys.Where(j => !jobs.Contains(j)).ToList())
                {
                    _buttons.Remove(gone);
                }
            }

            // Only worth asking when a finished row will show the restart hint
            _needsRestart = jobs.Any(j => j.State == DownloadState.Done) && ModEntry.Mods.NeedsRestart();
        }

        /// <summary>
        /// Whether the website's nxm:// links reach DewDrop. It reads the registry, so it runs only when the tab is
        /// shown; the handler is set up on the Account tab, and coming back here checks again.
        /// </summary>
        private void CheckHandler()
        {
            _handlerReady = true;
            _handlerProblem = null;
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                _handlerReady = ModEntry.Nxm.IsRegistered(ModEntry.NxmHandlerScriptPath);
                if (!_handlerReady)
                {
                    _handlerProblem = AccountPage.LinksProblem();
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Couldn't check the nxm:// handler: {ex.Message}");
                _handlerReady = false;
            }
        }

        /// <summary>Button states and the account explanation, which follow login changes made elsewhere.</summary>
        private void UpdateTopRow()
        {
            var nexus = ModEntry.Nexus;
            _clearFinished.Enabled = ModEntry.Downloads.Jobs.Any(j => !j.IsActive);
            _login.Visible = !nexus.IsLoggedIn;
            _setUp.Visible = nexus.IsLoggedIn && !nexus.IsPremium && !_handlerReady;
            var waiting = ModEntry.NxmWaiting;
            _explanation = !nexus.IsLoggedIn
                ? waiting > 0
                    ? $"{waiting} download{(waiting == 1 ? " from the website is" : "s from the website are")} waiting for you to log in on the Account tab."
                    : "Log in on the Account tab to download mods."
                : nexus.IsPremium
                    ? "Premium: Install buttons download straight into the game."
                    : _handlerProblem ?? "Free account: pick 'Get on Nexus', then 'Slow download' on the website.";
        }

        public override void Update(GameTime time)
        {
            Refresh();
            UpdateTopRow();
        }

        private static void OpenModsFolder()
        {
            var folder = ModEntry.Mods.InstallFolder;
            if (!Directory.Exists(folder))
            {
                ModEntry.Toast($"The mods folder doesn't exist: {folder}", error: true);
                return;
            }

            DownloadManager.OpenUrl(folder);
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (_clearFinished.TryClick(x, y) || _openFolder.TryClick(x, y) || _login.TryClick(x, y) || _setUp.TryClick(x, y)
                || _list.ReceiveLeftClick(x, y))
            {
                return;
            }

            if (!_list.TryGetItemAt(x, y, out var job, out var cell))
            {
                return;
            }

            var buttons = ButtonsFor(job);
            PlaceButtons(job, buttons, cell);
            if (buttons.Cancel.TryClick(x, y) || buttons.Retry.TryClick(x, y) || buttons.View.TryClick(x, y))
            {
                return;
            }

            if (job.State == DownloadState.Done)
            {
                Menu.ShowPage(Menu.Installed);
            }
        }

        private void Retry(DownloadJob job)
        {
            var nexus = ModEntry.Nexus;
            if (!nexus.IsLoggedIn)
            {
                ModEntry.Toast("Log in on the Account tab to download mods.", error: true);
                Menu.ShowPage(Menu.Account);
                return;
            }

            if (!nexus.IsPremium)
            {
                UpdateActions.OpenForFreeAccount(job.ModId, job.FileId);
                return;
            }

            var downloads = ModEntry.Downloads;

            // Installed meanwhile (from the mod's page, say): there's nothing left to retry
            if (ModEntry.Mods.IsFileInstalled(job.ModId, job.FileId))
            {
                downloads.Jobs.Remove(job);
                Refresh();
                ModEntry.Toast($"{job.Title} is already installed.");
                return;
            }

            var before = downloads.Jobs.Count;
            downloads.Start(job.ModId, job.FileId, job.Title);
            if (downloads.Jobs.Count > before)
            {
                // The new attempt takes the failed row's place
                downloads.Jobs.Remove(job);
                Refresh();
            }
            else
            {
                // Start skips a file that's already downloading, without saying so
                ModEntry.Toast("That file is already downloading.");
            }
        }

        public override void LeftClickHeld(int x, int y) => _list.LeftClickHeld(x, y);

        public override void ReleaseLeftClick(int x, int y) => _list.ReleaseLeftClick();

        public override void ReceiveScrollWheel(int direction) => _list.ReceiveScrollWheel(direction);

        public override void ReceiveKeyPress(Keys key)
        {
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
            _clearFinished.Hover(x, y);
            _openFolder.Hover(x, y);
            _login.Hover(x, y);
            _setUp.Hover(x, y);
            _list.Hover(x, y);

            // Row buttons still sit where they were last drawn, which after a scroll is another row's place;
            // clear them all and hover only the row under the mouse once its buttons are moved there
            foreach (var buttons in _buttons.Values)
            {
                buttons.Cancel.Hover(int.MinValue, int.MinValue);
                buttons.Retry.Hover(int.MinValue, int.MinValue);
                buttons.View.Hover(int.MinValue, int.MinValue);
            }

            var tip = _clearFinished.HoverText(x, y) ?? _openFolder.HoverText(x, y) ?? _login.HoverText(x, y) ?? _setUp.HoverText(x, y);
            if (tip is not null)
            {
                return tip;
            }

            var area = ExplanationArea();
            if (area.Width >= MinExplanationWidth && area.Contains(x, y) && ExplanationLines(area.Width) is { Count: > 0 } lines
                && lines[^1].EndsWith("..."))
            {
                return _explanation;
            }

            if (!_list.TryGetItemAt(x, y, out var job, out var cell))
            {
                return null;
            }

            // Put the row's buttons where this cell is, so a hover right after scrolling hits the right ones
            var rowButtons = ButtonsFor(job);
            PlaceButtons(job, rowButtons, cell);
            rowButtons.Cancel.Hover(x, y);
            rowButtons.Retry.Hover(x, y);
            rowButtons.View.Hover(x, y);
            if (rowButtons.Cancel.Contains(x, y))
            {
                return rowButtons.Cancel.Tooltip;
            }
            if (rowButtons.Retry.Contains(x, y))
            {
                return rowButtons.Retry.Tooltip;
            }
            if (rowButtons.View.Contains(x, y))
            {
                return rowButtons.View.Tooltip;
            }

            return Drawable(Tooltip(job));
        }

        private string Tooltip(DownloadJob job)
        {
            var lines = new List<string> { job.Title, StateLabel(job).Text };
            if (job.Message.Length > 0)
            {
                lines.Add(job.Message);
            }
            if (job.State == DownloadState.Done && _needsRestart)
            {
                lines.Add("Restart the game to load it.");
            }
            lines.Add("");
            lines.Add($"Nexus mod {job.ModId} | file {job.FileId}");
            lines.Add($"Started {job.Started.ToString("HH:mm", CultureInfo.InvariantCulture)}");
            if (job.State == DownloadState.Done)
            {
                lines.Add("Click to see your installed mods.");
            }
            return string.Join("\n", lines);
        }

        public override void Draw(SpriteBatch b)
        {
            _clearFinished.Draw(b);
            _openFolder.Draw(b);
            _login.Draw(b);
            _setUp.Draw(b);
            DrawExplanation(b);

            _list.Draw(b, DrawRow);
        }

        /// <summary>The space between the left buttons and the right-hand button (if any), down to the list, for the account note.</summary>
        private Rectangle ExplanationArea()
        {
            var left = _openFolder.Bounds.Right + 24;
            var right = _login.Visible ? _login.Bounds.X - 16 : _setUp.Visible ? _setUp.Bounds.X - 16 : Bounds.Right;
            return new Rectangle(left, Bounds.Y, Math.Max(0, right - left), ListTop);
        }

        /// <summary>The account note on one line if it fits, else on two (the band above the list is tall enough), else cut short.</summary>
        private List<string> ExplanationLines(int width)
        {
            if (_explanationCache.Text == _explanation && _explanationCache.Width == width)
            {
                return _explanationCache.Lines;
            }

            List<string> lines;
            if (Ui.Small.MeasureString(_explanation).X <= width)
            {
                lines = new List<string> { _explanation };
            }
            else if (Ui.LineHeight() * 2 <= ListTop)
            {
                // parseText breaks lines with Environment.NewLine, so drop the '\r' too
                var wrapped = Ui.Wrap(_explanation, width).Split('\n').Select(l => l.Trim('\r', ' ')).Where(l => l.Length > 0).ToArray();
                lines = wrapped.Length <= 2
                    ? wrapped.Select(l => Ui.Ellipsize(l, width)).ToList()
                    : new List<string> { Ui.Ellipsize(wrapped[0], width), Ui.Ellipsize(string.Join(" ", wrapped.Skip(1)), width) };
            }
            else
            {
                lines = new List<string> { Ui.Ellipsize(_explanation, width) };
            }

            _explanationCache = (_explanation, width, lines);
            return lines;
        }

        private void DrawExplanation(SpriteBatch b)
        {
            var area = ExplanationArea();
            if (area.Width < MinExplanationWidth)
            {
                return;
            }

            var lines = ExplanationLines(area.Width);
            if (lines.Count == 0)
            {
                return;
            }

            var lineHeight = Ui.LineHeight();
            var y = lines.Count == 1 ? Bounds.Y + 12 : Math.Max(Bounds.Y, Bounds.Y + 24 - lineHeight);
            var width = lines.Max(l => Ui.Small.MeasureString(l).X);
            var x = Math.Max(area.X, area.Right - width);
            foreach (var line in lines)
            {
                Ui.Label(b, line, new Vector2(x, y), Ui.Muted);
                y += lineHeight;
            }
        }

        private RowButtons ButtonsFor(DownloadJob job)
        {
            if (!_buttons.TryGetValue(job, out var buttons))
            {
                buttons = new RowButtons(
                    new UiButton("Cancel", () => ModEntry.Downloads.Cancel(job))
                    {
                        Tooltip = "Stop this download.",
                        Sound = "bigDeSelect"
                    },
                    new UiButton("Retry", () => Retry(job)),
                    new UiButton("View on Nexus", () => DownloadManager.OpenOnNexus(job.ModId))
                    {
                        Tooltip = "Open this mod's page on Nexus Mods."
                    });
                _buttons[job] = buttons;
            }

            return buttons;
        }

        /// <summary>Puts a row's buttons at its right edge; returns the x where they start.</summary>
        private static int PlaceButtons(DownloadJob job, RowButtons buttons, Rectangle cell)
        {
            var nexus = ModEntry.Nexus;
            var y = cell.Center.Y - buttons.View.PreferredHeight / 2;
            buttons.View.AtRight(cell.Right - 12, y);
            var left = buttons.View.Bounds.X;

            // A queued job hasn't started, and a downloading one is stopped at the next chunk; an install can't be
            buttons.Cancel.Visible = job.CanCancel;
            if (buttons.Cancel.Visible)
            {
                buttons.Cancel.AtRight(left - 12, y);
                left = buttons.Cancel.Bounds.X;
            }

            // A premium retry asks Nexus for the same link again, which won't help when the file is gone or the link
            // was refused; a free one opens the file on the website, which is the way to get a fresh link
            buttons.Retry.Visible = job.State == DownloadState.Failed && (job.CanRetry || (nexus.IsLoggedIn && !nexus.IsPremium));
            if (buttons.Retry.Visible)
            {
                buttons.Retry.Label = nexus.IsLoggedIn && !nexus.IsPremium ? "Get on Nexus" : "Retry";
                buttons.Retry.AtRight(left - 12, y);
                buttons.Retry.Tooltip = !nexus.IsLoggedIn
                    ? "Log in on the Account tab first."
                    : nexus.IsPremium
                        ? "Download this file again."
                        : "Open the file on Nexus; pick 'Slow download' to send it here.";
                left = buttons.Retry.Bounds.X;
            }

            return left;
        }

        private void DrawRow(SpriteBatch b, DownloadJob job, Rectangle cell, bool hovered)
        {
            var buttons = ButtonsFor(job);
            var right = PlaceButtons(job, buttons, cell) - 20;
            buttons.Cancel.Draw(b);
            buttons.Retry.Draw(b);
            buttons.View.Draw(b);

            // State on the right of the first line, measured first so the title can use the rest
            var textX = cell.X + 16;
            var state = StateLabel(job);
            var stateSize = Ui.Small.MeasureString(state.Text);
            Ui.Label(b, state.Text, new Vector2(right - stateSize.X, cell.Y + 10), state.Color);

            var titleWidth = Math.Max(80, right - (int)stateSize.X - 24 - textX);
            Ui.Label(b, Ui.Ellipsize(Drawable(OneLine(job.Title)), titleWidth), new Vector2(textX, cell.Y + 10), Ui.Text);

            var lineWidth = Math.Max(80, right - textX);
            var bar = new Rectangle(textX, cell.Y + 52, lineWidth, 20);
            var lineY = cell.Y + 46;
            switch (job.State)
            {
                case DownloadState.Queued:
                    Ui.Label(b, "Waiting to start...", new Vector2(textX, lineY), Ui.Muted);
                    break;

                case DownloadState.Downloading:
                    Ui.ProgressBar(b, bar, job.Progress);
                    break;

                case DownloadState.Installing:
                    Ui.ProgressBar(b, bar, 1, Ui.Attention);
                    break;

                case DownloadState.Done:
                    var done = (job.Message.Length > 0 ? job.Message : "Installed") + (_needsRestart ? " - restart to load it" : "");
                    Ui.Label(b, Ui.Ellipsize(Drawable(OneLine(done)), lineWidth), new Vector2(textX, lineY), Ui.Muted);
                    break;

                case DownloadState.Failed:
                    var error = job.Message.Length > 0 ? job.Message : "Something went wrong.";
                    Ui.Label(b, Ui.Ellipsize(Drawable(OneLine(error)), lineWidth), new Vector2(textX, lineY), Ui.Bad);
                    break;
            }
        }

        /// <summary>The short state shown on the right of a row, with its colour.</summary>
        private static (string Text, Color Color) StateLabel(DownloadJob job)
        {
            return job.State switch
            {
                DownloadState.Queued => ("Queued", Ui.Muted),
                DownloadState.Downloading => ($"Downloading {(int)(Math.Clamp(job.Progress, 0, 1) * 100)}%", Ui.Text),
                DownloadState.Installing => ("Installing...", Ui.Attention),
                DownloadState.Done => ("Done", Ui.Good),
                _ => ("Failed", Ui.Bad)
            };
        }

        private static string OneLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ');

        /// <summary>
        /// Titles and error messages come from Nexus and exceptions, and the game font throws on characters it has
        /// no glyph for; swap those for "?".
        /// </summary>
        private static string Drawable(string text)
        {
            if (Ui.Small.DefaultCharacter is not null)
            {
                return text;
            }

            static bool CanDraw(char c) => c is '\n' or '\r' || Ui.HasGlyph(c);
            return text.All(CanDraw) ? text : new string(text.Select(c => CanDraw(c) ? c : '?').ToArray());
        }
    }
}
