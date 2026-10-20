#if DEBUG
using DewDrop.Core.Services;
using DewDrop.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.Framework
{
    /// <summary>
    /// Debug builds only: lets a test script drive the title screen without touching the real mouse or keyboard.
    /// Drop a text file of commands into %TEMP%\dewdrop-bridge\in\*.cmd; each line runs on the game thread and the
    /// replies land in %TEMP%\dewdrop-bridge\out\&lt;same name&gt;.out.
    ///
    /// Commands: skipintro | open | back | click X Y | rclick X Y | hover X Y | key KEYNAME | type TEXT |
    /// scroll N | wait MS | shot PATH | status | quit | ask on|off | steam on|off | steamcheck | marker restarting|arrived|clear |
    /// unsee ID... | diff | uninstall ID | undo ID | removals | sources ID
    /// </summary>
    internal static class DebugBridge
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "dewdrop-bridge");
        private static readonly string Inbox = Path.Combine(Root, "in");
        private static readonly string Outbox = Path.Combine(Root, "out");

        private static readonly Queue<string> _pending = new();
        private static string? _replyFile;
        private static DateTime _resumeAt = DateTime.MinValue;

        public static void Init(IModHelper helper)
        {
            Directory.CreateDirectory(Inbox);
            Directory.CreateDirectory(Outbox);
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            ModEntry.Log.Info($"Debug bridge listening in {Inbox}");
        }

        private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_pending.Count == 0 && e.IsMultipleOf(15))
            {
                var next = new DirectoryInfo(Inbox).GetFiles("*.cmd").OrderBy(f => f.Name).FirstOrDefault();
                if (next is not null)
                {
                    try
                    {
                        foreach (var line in File.ReadAllLines(next.FullName))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                _pending.Enqueue(line.Trim());
                            }
                        }
                        _replyFile = Path.Combine(Outbox, Path.GetFileNameWithoutExtension(next.Name) + ".out");
                        File.WriteAllText(_replyFile, "");
                        next.Delete();
                    }
                    catch (IOException)
                    {
                        return;
                    }
                }
            }

            while (_pending.Count > 0 && DateTime.Now >= _resumeAt)
            {
                var line = _pending.Dequeue();
                string reply;
                try
                {
                    reply = Run(line);
                }
                catch (Exception ex)
                {
                    reply = $"ERROR {ex.GetType().Name}: {ex.Message}";
                }

                Reply($"> {line}\n{reply}");
                if (_pending.Count == 0)
                {
                    Reply("DONE");
                }
            }
        }

        private static void Reply(string text)
        {
            if (_replyFile is not null)
            {
                File.AppendAllText(_replyFile, text + "\n");
            }
        }

        private static string Run(string line)
        {
            var parts = line.Split(' ', 2);
            var command = parts[0].ToLowerInvariant();
            var arg = parts.Length > 1 ? parts[1] : "";
            var menu = Game1.activeClickableMenu;

            switch (command)
            {
                case "skipintro":
                    if (menu is TitleMenu title)
                    {
                        title.skipToTitleButtons();
                        return "ok";
                    }
                    return "not on the title screen";

                case "open":
                    if (menu is TitleMenu t)
                    {
                        t.skipToTitleButtons();
                        t.performButtonAction(TitleMenuPatches.ButtonName);
                        return "ok";
                    }
                    return "not on the title screen";

                case "back":
                    if (menu is TitleMenu tb)
                    {
                        tb.backButtonPressed();
                        return "ok";
                    }
                    return "not on the title screen";

                case "click":
                {
                    var (x, y) = Point(arg);
                    menu?.receiveLeftClick(x, y);
                    menu?.releaseLeftClick(x, y);
                    return $"clicked {x},{y}";
                }

                case "rclick":
                {
                    var (x, y) = Point(arg);
                    menu?.receiveRightClick(x, y);
                    return $"right-clicked {x},{y}";
                }

                case "hover":
                {
                    var (x, y) = Point(arg);
                    menu?.performHoverAction(x, y);
                    return $"hovered {x},{y}";
                }

                case "key":
                    menu?.receiveKeyPress(Enum.Parse<Keys>(arg, ignoreCase: true));
                    return "ok";

                case "type":
                    if (Game1.keyboardDispatcher.Subscriber is { } subscriber)
                    {
                        subscriber.RecieveTextInput(arg);
                        return "typed";
                    }
                    return "no text box has focus";

                case "scroll":
                    menu?.receiveScrollWheelAction(int.Parse(arg));
                    return "ok";

                case "wait":
                    _resumeAt = DateTime.Now.AddMilliseconds(int.Parse(arg));
                    return "waiting";

                case "shot":
                    return Screenshot(arg);

                case "status":
                    return $"menu={menu?.GetType().Name ?? "none"} sub={TitleMenu.subMenu?.GetType().Name ?? "none"} ui={Game1.uiViewport.Width}x{Game1.uiViewport.Height} zoom={Game1.options.uiScale} " +
                           $"profile={ModEntry.Mods.ActiveProfile.Name} installed={ModEntry.Mods.Installed.Count} loaded={ModEntry.Mods.LoadedIds.Count} restart={ModEntry.Mods.NeedsRestart()} " +
                           $"kind={ModEntry.Mods.Kind} ask={ModEntry.Config.AskToLoadProfile} askStatus=\"{ProfileStart.Status}\" " +
                           $"steamWanted={ProfileStart.SteamWanted} steamOn={ProfileStart.SteamOn} steamPending={ProfileStart.SteamPending} steamProblem=\"{ProfileStart.SteamUnavailableReason ?? "none"}\" " +
                           $"marker={ModEntry.Mods.Startup.Data.Restart?.Stage ?? "none"} failed={ModEntry.Mods.FailedRestart?.Stage ?? "none"} " +
                           $"nexus={(ModEntry.Nexus.IsLoggedIn ? ModEntry.Nexus.Account!.Name + (ModEntry.Nexus.IsPremium ? "+premium" : "") : "no")} " +
                           $"toasts=[{string.Join(" | ", ModEntry.Toasts.Select(t => t.Text))}]";

                case "window":
                {
                    // Windowed at a size (the game won't go below 1280x720)
                    var (w, h) = Point(arg);
                    Game1.options.setWindowedOption("Windowed");
                    Game1.game1.SetWindowSize(w, h);
                    return $"windowed {w}x{h}";
                }

                case "borderless":
                    Game1.options.setWindowedOption("Windowed Borderless");
                    return "borderless";

                case "uiscale":
                    Game1.options.desiredUIScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
                    return $"ui scale {arg}";

                case "quit":
                    Game1.game1.Exit();
                    return "bye";

                case "ask":
                    ModEntry.Config.AskToLoadProfile = arg.Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
                    ModEntry.SaveConfig();
                    return $"ask {ModEntry.Config.AskToLoadProfile}";

                case "steam":
                    // Only the wish: Steam itself changes when the game closes
                    ProfileStart.SteamWanted = arg.Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
                    return $"steam wanted {ProfileStart.SteamWanted}, pending {ProfileStart.SteamPending}";

                case "steamcheck":
                    ProfileStart.RefreshSteam();
                    return $"problem={ProfileStart.SteamUnavailableReason ?? "none"} option={ProfileStart.SteamOption ?? "(unknown)"} on={ProfileStart.SteamOn} pending={ProfileStart.SteamPending}";

                case "marker":
                {
                    // Leaves a restart record as if one had been scheduled, for the next launch to find
                    var startup = ModEntry.Mods.Startup;
                    var bits = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var stage = bits.Length > 0 ? bits[0].ToLowerInvariant() : "";
                    startup.Data.Restart = stage switch
                    {
                        "restarting" => new RestartRecord { Id = "bridge", Stage = RestartRecord.StageRestarting, Profile = ModEntry.Mods.ActiveProfile.Name, AtUtc = DateTime.UtcNow },
                        "arrived" => new RestartRecord { Id = "bridge", Stage = RestartRecord.StageArrived, Profile = ModEntry.Mods.ActiveProfile.Name, AtUtc = DateTime.UtcNow },
                        _ => null
                    };
                    return startup.Save() ? $"marker {startup.Data.Restart?.Stage ?? "cleared"}" : "couldn't save";
                }

                case "unsee":
                    ModEntry.Mods.Startup.Forget(arg.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    return ModEntry.Mods.Startup.Save() ? "forgotten" : "couldn't save";

                case "diff":
                {
                    var d = ModEntry.Mods.Difference();
                    return $"matches={d.Matches} turnedOff=[{string.Join(", ", d.TurnedOff.Select(m => m.UniqueId))}] notLoaded=[{string.Join(", ", d.NotLoaded.Select(m => m.UniqueId))}] " +
                           $"otherCopy=[{string.Join(", ", d.OtherCopy.Select(m => m.UniqueId))}] rejected=[{string.Join(", ", d.Rejected)}]";
                }

                case "uninstall":
                {
                    // The first loose copy, as the Installed tab's trash can would (without the dialog)
                    var mods = ModEntry.Mods;
                    var mod = mods.Installed.FirstOrDefault(m => m.SourceId is null && m.UniqueId.Equals(arg.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (mod is null)
                    {
                        return "not installed";
                    }
                    return mods.Uninstall(mod, out var error) ? $"uninstalled {mod.Folder.FullName}" : $"refused: {error}";
                }

                case "undo":
                {
                    var mods = ModEntry.Mods;
                    var mod = mods.Removing.FirstOrDefault(m => m.UniqueId.Equals(arg.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (mod is null)
                    {
                        return "not uninstalled";
                    }
                    return mods.Undo(mod, out var message) ? $"kept {mod.Folder.FullName}: {message}" : message;
                }

                case "removals":
                {
                    var mods = ModEntry.Mods;
                    return $"removing=[{string.Join(", ", mods.Removing.Select(m => m.UniqueId))}] stillLoaded=[{string.Join(", ", mods.RemovingLoaded().Select(m => m.UniqueId))}] " +
                           $"queued=[{string.Join(", ", mods.Pending.Removals().Select(s => $"{s.UniqueId}@{s.Target}"))}]";
                }

                case "sources":
                {
                    // What DewDrop knows about where a mod came from on Nexus
                    var mods = ModEntry.Mods;
                    var mod = mods.Installed.FirstOrDefault(m => m.UniqueId.Equals(arg.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (mod is null)
                    {
                        return "not installed";
                    }
                    var install = mods.Sources.InstallOf(mod.UniqueId);
                    return $"nexus=[{string.Join(", ", mods.Sources.NexusIdsOf(mod))}] install={(install is null ? "none" : $"{install.ModId}/{install.FileId} v{install.Version}")} " +
                           $"copies={mods.OtherLooseCopies(mod).Count + 1}";
                }

                default:
                    return "unknown command";
            }
        }

        private static (int, int) Point(string arg)
        {
            var bits = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return (int.Parse(bits[0]), int.Parse(bits[1]));
        }

        /// <summary>Draws the active menu into an off-screen target and saves it, so no window capture is needed.</summary>
        private static string Screenshot(string path)
        {
            var menu = Game1.activeClickableMenu;
            if (menu is null)
            {
                return "no menu to draw";
            }

            var device = Game1.graphics.GraphicsDevice;
            var width = Game1.uiViewport.Width;
            var height = Game1.uiViewport.Height;
            using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            var previous = device.GetRenderTargets();

            device.SetRenderTarget(target);
            device.Clear(Color.Black);
            var batch = Game1.spriteBatch;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            try
            {
                menu.draw(batch);
            }
            finally
            {
                batch.End();
                device.SetRenderTargets(previous);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            target.SaveAsPng(stream, width, height);
            return $"saved {width}x{height} to {path}";
        }
    }
}
#endif
