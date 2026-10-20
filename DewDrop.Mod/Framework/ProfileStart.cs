using DewDrop.Core.Services;
using DewDrop.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.Framework
{
    /// <summary>
    /// Starting the game in the active profile. SMAPI loads the whole Mods folder unless it's pointed at the linked
    /// profile folder (Selected Mods), and a mod can't change that once the game is running. So:
    /// <list type="bullet">
    ///   <item>"Start in my profile from Steam" (Windows, Steam version) adds --mods-path to Stardew Valley's launch option
    ///   in Steam, written once the game has closed, since Steam has to be closed to change it. On Linux and the Steam
    ///   Deck, the launch option runs DewDrop's launcher instead (set once by the installer), and the setting is a file
    ///   the launcher reads, so it changes at once. Either way, with it on, the links are brought up to date as the game
    ///   closes, so the next launch opens straight into the profile.</item>
    ///   <item>Otherwise, on the first title screen of a launch that loaded different mods than the profile, the "Load your
    ///   profile?" box offers a restart (and to turn the Steam setting on).</item>
    /// </list>
    /// </summary>
    internal static class ProfileStart
    {
        /// <summary>Three minutes of the game actually running: time paused in the background doesn't count.</summary>
        private const int GiveUpAfterTicks = 3 * 60 * 60;
        private static int _activeTicks;
        private static bool _done;
        private static bool _decided;
        private static bool _skippedIntro;
        private static Plan? _plan;
        private static bool? _relaunchAvailable;
        private static bool _exited;

        private static string? _steamRoot;
        private static string? _steamConfig;
        private static string? _steamOption;

        /// <summary>What the "Load your profile?" box shows.</summary>
        /// <param name="AfterFailure">The last restart didn't come back (then the Steam checkbox starts unticked).</param>
        /// <param name="OfferSteam">Whether to offer turning on "Start in my profile from Steam".</param>
        internal record Plan(string Reason, SessionDifference Difference, bool AfterFailure, bool OfferSteam);

        /// <summary>What happened on the title screen this session, for the log and the debug bridge.</summary>
        public static string Status { get; private set; } = "waiting for the title screen";

        /// <summary>False under Wine or Proton, where a restart can't start the game again.</summary>
        public static bool RelaunchAvailable => _relaunchAvailable ??= !Relauncher.IsWine();

        /// <summary>
        /// Linux and the Steam Deck: Steam's launch option runs DewDrop's launcher (see <see cref="SteamLauncher"/>), so
        /// "Start in my profile from Steam" is a file beside it, and Apply &amp; Restart runs the launcher again.
        /// </summary>
        public static bool UsesSteamLauncher { get; private set; }

        // ---------- Start in my profile from Steam ----------

        /// <summary>What the player chose; Steam itself is changed when the game closes.</summary>
        public static bool SteamWanted
        {
            get => ModEntry.Config.StartInProfileFromSteam ?? SteamOn;
            set
            {
                ModEntry.Config.StartInProfileFromSteam = value;
                ModEntry.SaveConfig();
                SyncLauncherSetting();
            }
        }

        /// <summary>Why "Start in my profile from Steam" can't be used here, or null when it can.</summary>
        public static string? SteamUnavailableReason { get; private set; } = "Not checked yet.";

        /// <summary>Whether Steam starts the game in the profile: its launch option points at the profile folder, as of the last read, or the launcher is told to.</summary>
        public static bool SteamOn => UsesSteamLauncher
            ? File.Exists(StartInProfileFile)
            : OperatingSystem.IsWindows() && SteamLaunchOption.PointsAt(_steamOption, ModEntry.Mods.Paths.SelectedModsFolder);

        /// <summary>Whether Steam's launch option will change when the game closes (never with DewDrop's launcher, which reads the setting each launch).</summary>
        public static bool SteamPending => SteamUnavailableReason is null && !UsesSteamLauncher && SteamWanted != SteamOn;

        /// <summary>Stardew Valley's launch option in Steam, as last read (null when unknown).</summary>
        public static string? SteamOption => _steamOption;

        private static string SmapiExe => Path.Combine(Constants.GamePath, "StardewModdingAPI.exe");

        private static string ScriptFolder => ModEntry.Mods.Paths.ScriptFolder;

        private static string StartInProfileFile => Path.Combine(ScriptFolder, SteamLauncher.StartInProfileName);

        private static string SteamResultFile => Path.Combine(ScriptFolder, "steam-result.json");

        /// <summary>Reads Stardew Valley's launch option from Steam's settings, and whether the setting can be used.</summary>
        public static void RefreshSteam()
        {
            _steamOption = null;
            UsesSteamLauncher = false;
            SteamUnavailableReason = OperatingSystem.IsLinux() ? FindSteamOnLinux() : FindSteam();
        }

        private static string? FindSteam()
        {
            if (!OperatingSystem.IsWindows())
            {
                return "Only for the Steam version of the game on Windows, Linux and the Steam Deck.";
            }

            if (!RelaunchAvailable)
            {
                return "Not available when the game runs through Proton or Wine.";
            }

            if (!Constants.GamePath.Contains("steamapps", StringComparison.OrdinalIgnoreCase))
            {
                return "Only for the Steam version of the game. Use Apply & Restart to load your profile.";
            }

#if DEBUG
            // Testing: a scratch Steam folder (with localconfig.vdf in it), so a test never closes the real Steam or edits
            // its settings. Restarts pass it on, since the game started them.
            if (Environment.GetEnvironmentVariable("DEWDROP_FAKE_STEAM") is { Length: > 0 } fake)
            {
                _steamRoot = fake;
                _steamConfig = Path.Combine(fake, "localconfig.vdf");
            }
#endif
            _steamRoot ??= SteamLaunchOption.FindSteamRoot();
            if (_steamRoot is null)
            {
                return "Couldn't find Steam on this PC.";
            }

            _steamConfig ??= SteamLaunchOption.FindUserConfig(_steamRoot);
            if (_steamConfig is null)
            {
                return "Couldn't find your Steam account's settings. Sign in to Steam, then start the game again.";
            }

            try
            {
                _steamOption = SteamLaunchOption.Read(File.ReadAllText(_steamConfig));
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read {_steamConfig}: {ex.Message}");
            }

            if (_steamOption is null)
            {
                return "Couldn't read Steam's settings for the game.";
            }

            // An option that runs some other program (a launcher of its own) can't be added to safely
            if (!SteamOn && SteamLaunchOption.WithModsPath(_steamOption, SmapiExe, ModEntry.Mods.Paths.SelectedModsFolder) is null)
            {
                return $"Stardew Valley's launch option in Steam starts another program, so DewDrop can't add to it: {_steamOption}";
            }

            return null;
        }

        /// <summary>
        /// Linux and the Steam Deck: the setting works when Steam's launch option runs DewDrop's launcher, which only the
        /// installer sets, since Steam has to be closed to change it (and Game Mode never closes Steam). The launcher
        /// is brought up to date here for the next launch.
        /// </summary>
        private static string? FindSteamOnLinux()
        {
            if (!RelaunchAvailable)
            {
                return "Not available when the game runs through Proton or Wine.";
            }

            if (!Constants.GamePath.Contains("steamapps", StringComparison.OrdinalIgnoreCase))
            {
                return "Only for the Steam version of the game. Use Apply & Restart to load your profile.";
            }

#if DEBUG
            // Testing: a scratch Steam folder, as on Windows
            if (Environment.GetEnvironmentVariable("DEWDROP_FAKE_STEAM") is { Length: > 0 } fake)
            {
                _steamRoot = fake;
                _steamConfig = Path.Combine(fake, "localconfig.vdf");
            }
#endif
            _steamRoot ??= SteamLauncher.FindSteamRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            if (_steamRoot is null)
            {
                return "Couldn't find Steam's folder.";
            }

            _steamConfig ??= SteamLaunchOption.FindUserConfig(_steamRoot);
            if (_steamConfig is null)
            {
                return "Couldn't find your Steam account's settings. Sign in to Steam, then start the game again.";
            }

            try
            {
                _steamOption = SteamLaunchOption.Read(File.ReadAllText(_steamConfig));
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read {_steamConfig}: {ex.Message}");
            }

            if (!SteamLauncher.IsUsedBy(_steamOption, ModEntry.Mods.Paths.SteamLauncherPath))
            {
                return SteamDeck.Current
                    ? "Set up by the DewDrop installer: run it once in Desktop Mode, and let it set up Steam."
                    : "Needs DewDrop's Steam launcher: in a terminal, run the DewDrop installer again with --steam-setup.";
            }

            if (!SteamLauncher.Install(Path.Combine(ModEntry.ModHelper.DirectoryPath, "assets", SteamLauncher.ScriptName), ModEntry.Mods.Paths.SteamLauncherPath, ModEntry.Log))
            {
                return "DewDrop's Steam launcher couldn't be updated. The SMAPI log says why.";
            }

            UsesSteamLauncher = true;
            return null;
        }

        /// <summary>With DewDrop's launcher, the setting is a file it reads at each launch: brought in line with the choice.</summary>
        private static void SyncLauncherSetting()
        {
            if (!UsesSteamLauncher || SteamWanted == SteamOn)
            {
                return;
            }

            try
            {
                if (SteamWanted)
                {
                    File.WriteAllText(StartInProfileFile, "Written by DewDrop: Steam starts the game in your profile while this file is here.\n");
                }
                else
                {
                    File.Delete(StartInProfileFile);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't change {StartInProfileFile}: {ex.Message}");
            }
        }

        /// <summary>Called when the game has launched: reports last session's Steam change and checks Steam now.</summary>
        public static void OnGameLaunched()
        {
            ReadSteamResult();
            RefreshSteam();

            // Never chosen: take what Steam already does as the choice
            if (ModEntry.Config.StartInProfileFromSteam is null && SteamUnavailableReason is null)
            {
                SteamWanted = SteamOn;
            }
            SyncLauncherSetting();

            var mods = ModEntry.Mods;
            if (ModEntry.Config.EnableModsOnInstall)
            {
                var turnedOn = mods.TurnOnNewMods();
                if (turnedOn.Count > 0)
                {
                    // Short, so it fits the toast; the rows say "On after restart" and the status line says when they load
                    ModEntry.ToastWhenMenuOpens($"Turned on {Names(turnedOn.Select(m => m.Name))}, new in your Mods folder.");
                }
            }
            else
            {
                mods.MarkSeen(mods.NewMods.Select(m => m.UniqueId));
            }

            ModEntry.Log.Trace($"Start in my profile from Steam: wanted {SteamWanted}, launch option '{_steamOption}', {(SteamUnavailableReason ?? (SteamPending ? "changes when the game closes" : "up to date"))}.");
            if (OperatingSystem.IsLinux())
            {
                ModEntry.Log.Trace($"Linux: SteamOS {SteamDeck.Current}, Game Mode {SteamDeck.InGameMode}, DewDrop's Steam launcher {(UsesSteamLauncher ? "in use" : "not in use")}.");
            }
        }

        private static void ReadSteamResult()
        {
            try
            {
                if (!File.Exists(SteamResultFile))
                {
                    return;
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<Relauncher.SteamJobResult>(File.ReadAllText(SteamResultFile));
                File.Delete(SteamResultFile);
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    ModEntry.ToastWhenMenuOpens($"Steam's launch option wasn't changed: {result.Message}", error: true);
                }
                else if (SteamLaunchOption.PointsAt(result.Value, ModEntry.Mods.Paths.SelectedModsFolder))
                {
                    ModEntry.ToastWhenMenuOpens("Steam now starts the game in your profile.");
                }
                else
                {
                    ModEntry.ToastWhenMenuOpens("Steam no longer starts the game in your profile.");
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read {SteamResultFile}: {ex.Message}");
            }
        }

        /// <summary>
        /// The game is closing: with the Steam setting on, bring the links up to date for the next launch, then hand the
        /// waiting mod updates and any launch option change to the script that runs once the game has closed. Runs once,
        /// and never after Apply &amp; Restart (that restart does its own).
        /// </summary>
        public static void OnExiting()
        {
            if (_exited)
            {
                return;
            }
            _exited = true;

            try
            {
                var mods = ModEntry.Mods;
                if (mods is null || mods.RestartScheduled || !(OperatingSystem.IsWindows() || UsesSteamLauncher))
                {
                    return;
                }

                var linksReady = mods.Kind == SessionKind.Profile && !mods.LinksMayBeStale;
                if (SteamWanted && SteamUnavailableReason is null && mods.CanApply && mods.LinksMayBeStale)
                {
                    linksReady = mods.RebuildLinks(out var error);
                    if (!linksReady)
                    {
                        ModEntry.Log.Warn($"Couldn't update the profile links as the game closed: {error}");
                    }
                }

                // DewDrop's launcher reads the setting itself, and Unix never leaves updates waiting
                if (!OperatingSystem.IsWindows())
                {
                    return;
                }

                Relauncher.SteamJob? job = null;
                if (SteamPending && _steamConfig is not null && _steamRoot is not null && _steamOption is not null)
                {
                    // Turning it on needs the links in place, or the next launch would have no mods (and no Mods button)
                    var wanted = SteamWanted
                        ? linksReady ? SteamLaunchOption.WithModsPath(_steamOption, SmapiExe, mods.Paths.SelectedModsFolder) : null
                        : SteamLaunchOption.Without(_steamOption).Trim();
                    if (wanted is not null && wanted != _steamOption)
                    {
                        job = new Relauncher.SteamJob(Path.Combine(_steamRoot, "steam.exe"), _steamConfig, _steamOption, wanted, SteamResultFile);
                    }
                }

                if (job is not null || mods.PendingUpdateCount > 0)
                {
                    Relauncher.ScheduleAfterExit(Constants.GamePath, mods.Paths.SelectedModsFolder, mods.Pending.FilePath, ScriptFolder, ModEntry.Log, job);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Error($"Couldn't finish up as the game closed: {ex}");
            }
        }

        // ---------- the "Load your profile?" box ----------

        /// <summary>Runs every tick until the title screen has been dealt with.</summary>
        public static void Update()
        {
            if (_done)
            {
                return;
            }

            try
            {
                Step();
            }
            catch (Exception ex)
            {
                _done = true;
                Status = "stopped after an error";
                ModEntry.Log.Error($"The 'Load your profile?' check stopped for this session: {ex}");
            }
        }

        private static void Step()
        {
            if (Game1.game1.IsActive)
            {
                _activeTicks++;
            }

            if (Context.IsWorldReady || _activeTicks > GiveUpAfterTicks)
            {
                Finish("a save loaded, or no title screen came up in time");
                return;
            }

            if (Game1.activeClickableMenu is not TitleMenu title)
            {
                return;
            }

            var mods = ModEntry.Mods;
            if (mods.ConfirmArrived() is { } arrived)
            {
                // This session is DewDrop's restart, and the player saw the intro a moment ago: straight to the buttons
                title.skipToTitleButtons();
                ModEntry.Log.Info($"Started in the '{arrived.Profile}' profile.");
                Finish("this session is DewDrop's restart");
                return;
            }

            if (!_decided)
            {
                _decided = true;
                _plan = Decide();
                if (_plan is null)
                {
                    Finish(Status);
                    return;
                }
            }

            // The player opened Load, Co-op or another title menu first: their choice wins
            if (TitleMenu.subMenu is not null)
            {
                Finish("another title menu opened first");
                return;
            }

            // Only skip the intro when there's something to ask, and only once
            if (!_skippedIntro && (title.logoFadeTimer > 0 || title.fadeFromWhiteTimer > 0 || !title.titleInPosition))
            {
                _skippedIntro = true;
                title.skipToTitleButtons();
                return;
            }

            if (!title.titleInPosition || title.isTransitioningButtons || title.buttonsToShow < 4 || title.logoFadeTimer > 0 || title.fadeFromWhiteTimer > 0)
            {
                return;
            }

            TitleMenu.subMenu = new TitlePrompt(_plan!);
            Finish("asked");
        }

        private static void Finish(string status)
        {
            _done = true;
            Status = status;
            ModEntry.Log.Trace($"Load your profile: {status}.");
        }

        /// <summary>Whether to ask at all; null (with <see cref="Status"/> saying why) when not.</summary>
        private static Plan? Decide()
        {
            var mods = ModEntry.Mods;
            string? skip = null;
            if (mods.Kind != SessionKind.Plain)
            {
                skip = mods.Kind == SessionKind.Profile ? "the game started in a profile" : "SMAPI started on a folder DewDrop doesn't manage";
            }
            else if (!ModEntry.Config.AskToLoadProfile)
            {
                skip = "turned off";
            }
            else if (!RelaunchAvailable)
            {
                skip = "not available under Wine or Proton";
            }
            else if (mods.ApplyBlockedReason is not null)
            {
                skip = "the settings or profile couldn't be read";
            }
            else if (Environment.GetCommandLineArgs().Skip(1).Any(a => a.StartsWith("+")))
            {
                // Steam passes "+connect_lobby ..." when the game starts from an invite
                skip = "started from a Steam invite";
            }
            else if (!File.Exists(RestartLauncherPath))
            {
                skip = "SMAPI's launcher is missing";
            }

            var difference = mods.Difference();
            if (skip is null && difference.Matches)
            {
                skip = "the mods loaded already match the profile";
            }

            if (skip is not null)
            {
                Status = skip;
                return null;
            }

            var failed = mods.FailedRestart;
            var reason = failed is not null
                ? failed.Stage == RestartRecord.StageArrived
                    ? "Last time, the game closed while your profile was loading."
                    : "Last time DewDrop restarted the game, it didn't come back in your profile."
                : Reason(difference, mods.ActiveProfile.Name);
            return new Plan(reason, difference, failed is not null, SteamUnavailableReason is null && !SteamWanted);
        }

        /// <summary>What a restart starts: SMAPI itself through DewDrop's Steam launcher, else as <see cref="Relauncher.LauncherPath"/> says.</summary>
        public static string RestartLauncherPath => UsesSteamLauncher
            ? Path.Combine(Constants.GamePath, "StardewModdingAPI")
            : Relauncher.LauncherPath(Constants.GamePath);

        /// <summary>The first line of the box: which mods differ.</summary>
        public static string Reason(SessionDifference difference, string profile)
        {
            var lines = new List<string>();
            if (difference.TurnedOff.Count > 0)
            {
                var n = difference.TurnedOff.Count;
                lines.Add($"{n} mod{(n == 1 ? " that is" : "s that are")} off in your '{profile}' profile loaded anyway: {Names(difference.TurnedOff.Select(m => m.Name))}.");
            }

            if (difference.NotLoaded.Count > 0)
            {
                var n = difference.NotLoaded.Count;
                lines.Add($"{n} mod{(n == 1 ? "" : "s")} from your '{profile}' profile didn't load: {Names(difference.NotLoaded.Select(m => m.Name))}.");
            }

            return lines.Count > 0 ? string.Join(" ", lines) : $"The mods loaded right now don't match your '{profile}' profile.";
        }

        /// <summary>"A, B, C and 2 more".</summary>
        public static string Names(IEnumerable<string> names)
        {
            var list = names.ToList();
            return string.Join(", ", list.Take(3)) + (list.Count > 3 ? $" and {list.Count - 3} more" : "");
        }
    }
}
