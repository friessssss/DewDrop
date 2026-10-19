using DewDrop.Core.Models;
using DewDrop.Core.Services;
using DewDrop.Framework;
using DewDrop.Patches;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop
{
    /// <summary>A short message shown at the bottom of the DewDrop menu.</summary>
    internal record Toast(string Text, bool IsError, DateTime Expires);

    internal class ModEntry : Mod
    {
        private const int NxmPollTicks = 60;

        internal static ILog Log { get; private set; } = null!;
        internal static IModHelper ModHelper { get; private set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;
        internal static ModManager Mods { get; private set; } = null!;
        internal static NexusSession Nexus { get; private set; } = null!;
        internal static DownloadManager Downloads { get; private set; } = null!;
        internal static ThumbnailCache Thumbnails { get; private set; } = null!;
        internal static NxmBridge Nxm { get; private set; } = null!;

        /// <summary>Messages for the menu to show; newest last.</summary>
        internal static List<Toast> Toasts { get; } = new();

        /// <summary>Links from the website's "Mod manager download" button waiting in the queue for a Nexus login.</summary>
        internal static int NxmWaiting { get; private set; }

        /// <summary>Where the nxm:// handler script lives, outside the mod folder so a relinked mods folder can't break it.</summary>
        internal static string NxmHandlerScriptPath => Path.Combine(Mods.Paths.CacheFolder, "DewDrop", "nxm-handler.ps1");

        public override void Entry(IModHelper helper)
        {
            Log = new SmapiLog(Monitor);
            ModHelper = helper;
            Config = helper.ReadConfig<ModConfig>();

            try
            {
                Mods = new ModManager(helper, ModManifest, Log);
            }
            catch (Exception ex)
            {
                Monitor.Log($"DewDrop couldn't read the mod and profile folders, so it's turned off this session: {ex}", LogLevel.Error);
                return;
            }

            var version = ModManifest.Version.ToString();
            var api = new NexusApi(version, Log);
            Nexus = new NexusSession(api, new KeyVault(Mods.Paths, Mods.Settings, Log));
            Downloads = new DownloadManager(Nexus, Mods, Config);
            Thumbnails = new ThumbnailCache(api, Mods.Paths.ThumbnailsFolder);
            Nxm = new NxmBridge(Mods.Paths, Log);

            // Installs that registered an older handler script (no BOM, queue path written into it) get the current one
            if (OperatingSystem.IsWindows())
            {
                Nxm.RefreshHandlerScript(NxmHandlerScriptPath);
            }

            TitleMenuPatches.Apply(new Harmony(ModManifest.UniqueID));

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetsInvalidated += TitleMenuPatches.OnAssetsInvalidated;
#if DEBUG
            DebugBridge.Init(helper);
#endif

            helper.ConsoleCommands.Add("dewdrop_nxm", "Queue an nxm:// link for download, as if the website had sent it.\n\nUsage: dewdrop_nxm <link>", (_, args) =>
            {
                var link = NxmLink.Parse(string.Join(" ", args));
                if (link is null)
                {
                    Monitor.Log("That isn't a Stardew Valley nxm:// mod file link.", LogLevel.Error);
                    return;
                }
                Downloads.Start(link);
            });
            helper.ConsoleCommands.Add("dewdrop_export_title", "Save the vanilla title buttons texture and the built Mods button as PNGs.\n\nUsage: dewdrop_export_title <folder>", (_, args) =>
            {
                var folder = args.Length > 0 ? string.Join(" ", args) : Path.Combine(helper.DirectoryPath, "debug");
                ExportTitleArt(folder);
            });
            helper.ConsoleCommands.Add("dewdrop_status", "Show what DewDrop sees: mods path, profile, login.", (_, _) =>
            {
                Monitor.Log($"Running mods path: {Mods.RunningModsPath} (profile folder: {Mods.RunningFromProfileFolder})", LogLevel.Info);
                Monitor.Log($"Mods folder: {Mods.ModsFolder}; install folder: {Mods.InstallFolder}", LogLevel.Info);
                Monitor.Log($"Active profile: {Mods.ActiveProfile.Name}; installed mods: {Mods.Installed.Count}; needs restart: {Mods.NeedsRestart()}", LogLevel.Info);
                Monitor.Log($"Nexus: {(Nexus.IsLoggedIn ? $"{Nexus.Account!.Name}{(Nexus.IsPremium ? " (Premium)" : "")}" : "not logged in")}", LogLevel.Info);
            });
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Nexus.RestoreSavedLogin();
            if (Config.CheckUpdatesOnLaunch)
            {
                Mods.CheckForUpdates();
            }

            if (!Mods.RunningFromProfileFolder)
            {
                Monitor.Log($"SMAPI loaded mods from {Mods.RunningModsPath} rather than the profile folder. Mods > Apply & Restart switches to profiles.", LogLevel.Info);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            MainThread.Pump();
            Thumbnails.Upload();

            if (e.IsMultipleOf(NxmPollTicks))
            {
                PollNxmQueue();
                Toasts.RemoveAll(t => t.Expires < DateTime.Now);
            }
        }

        /// <summary>The Browse pictures aren't needed in game; they load again from the disk cache next time.</summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Thumbnails.Clear();
        }

        private void PollNxmQueue()
        {
            // Links clicked while the game was closed are already queued at launch, before the saved login is back
            // (the first poll runs on the same tick as GameLaunched). Downloads need a login, so they stay queued
            // until there is one rather than being drained and thrown away.
            if (!Nexus.IsLoggedIn)
            {
                if (!Nexus.LoggingIn)
                {
                    var waiting = CountQueuedNxmLinks();
                    if (waiting > 0 && waiting != NxmWaiting)
                    {
                        Toast($"{waiting} Nexus download{(waiting == 1 ? " is" : "s are")} waiting. Log in on the Account tab to start {(waiting == 1 ? "it" : "them")}.", error: true);
                    }
                    NxmWaiting = waiting;
                }
                return;
            }

            NxmWaiting = 0;
            foreach (var raw in Nxm.Drain())
            {
                // Drain has already taken the whole batch off the queue, so one bad link mustn't lose the rest
                try
                {
                    var link = NxmLink.Parse(raw);
                    if (link is null)
                    {
                        Log.Trace($"Ignored an nxm link DewDrop can't use: {raw}");
                        Toast(NxmLink.IsCollectionLink(raw)
                            ? "That was a Nexus collection. DewDrop installs single mods; collections install through Stardrop."
                            : "The website sent a link that isn't a Stardew Valley mod file, so DewDrop ignored it.", error: true);
                        continue;
                    }

                    if (long.TryParse(link.Expires, out var expires) && expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        Toast($"The website's download link for Nexus mod {link.ModId} expired. Click it on the website again.", error: true);
                        continue;
                    }

                    Downloads.Start(link);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Ignored an nxm link DewDrop can't use: {raw} ({ex})");
                    Toast("DewDrop couldn't use a download link from the website. Try the download button again.", error: true);
                }
            }
        }

        private static int CountQueuedNxmLinks()
        {
            try
            {
                var folder = Mods.Paths.NxmQueueFolder;
                return Directory.Exists(folder) ? Directory.GetFiles(folder, "*.nxm").Length : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal static void Toast(string text, bool error = false)
        {
            Toasts.Add(new Toast(text, error, DateTime.Now.AddSeconds(error ? 8 : 5)));
            if (Toasts.Count > 4)
            {
                Toasts.RemoveAt(0);
            }

            if (error)
            {
                Log.Warn(text);
            }
            else
            {
                Log.Trace(text);
            }
        }

        internal static void SaveConfig() => ModHelper.WriteConfig(Config);

        private void ExportTitleArt(string folder)
        {
            if (Game1.activeClickableMenu is not TitleMenu title)
            {
                Monitor.Log("Run this from the title screen.", LogLevel.Error);
                return;
            }

            TitleButtonArt.Export(title.titleButtonsTexture, Path.Combine(folder, "TitleButtons.png"));
            TitleButtonArt.Export(TitleMenuPatches.ButtonTexture ?? TitleButtonArt.Build(title.titleButtonsTexture), Path.Combine(folder, "ModsButton.png"));
            Monitor.Log($"Exported to {folder}", LogLevel.Info);
        }
    }
}
