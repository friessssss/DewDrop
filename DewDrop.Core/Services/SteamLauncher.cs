using System.Diagnostics;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// DewDrop's Steam launcher on Linux and the Steam Deck: a script in DewDrop's data folder that Stardew Valley's
    /// launch option in Steam runs (<c>"&lt;script&gt;" %command%</c>, set once by the installer). It starts SMAPI with no
    /// terminal window, which Game Mode can't show, and on the profile folder when DewDrop asks it to, through files
    /// beside it. So, unlike Windows, nothing in Steam has to change (and Steam never has to close) when the player
    /// turns "Start in my profile from Steam" on or off, or when Apply &amp; Restart goes back through Steam.
    /// The script ships in the mod folder (assets/steam-launch.sh); DewDrop copies it to the data folder when it changes.
    /// </summary>
    public static class SteamLauncher
    {
        public const string ScriptName = "steam-launch.sh";

        /// <summary>Present while "Start in my profile from Steam" is on.</summary>
        public const string StartInProfileName = "start-in-profile";

        /// <summary>Written by Apply &amp; Restart with the restart's ID; the next launch starts in the profile and takes it.</summary>
        public const string RestartName = "restart";

        /// <summary>Whether a launch option runs DewDrop's launcher at this path.</summary>
        public static bool IsUsedBy(string? option, string scriptPath)
        {
            return !string.IsNullOrEmpty(option) && option.Contains(scriptPath, StringComparison.Ordinal);
        }

        /// <summary>
        /// Puts a copy of the launcher at <paramref name="target"/> when it's missing or differs from
        /// <paramref name="source"/>, and makes sure it can run. Returns false (and logs) when it couldn't.
        /// </summary>
        public static bool Install(string source, string target, ILog log)
        {
            try
            {
                if (!File.Exists(source))
                {
                    log.Warn($"DewDrop's Steam launcher is missing from {source}; reinstall DewDrop to get it back.");
                    return false;
                }

                var wanted = File.ReadAllText(source).Replace("\r\n", "\n");
                if (File.Exists(target) && File.ReadAllText(target) == wanted)
                {
                    // A launcher Steam can't run means the game doesn't start at all, so this is checked every time
                    return MakeExecutable(target, log);
                }

                // Written beside it and renamed over it, so Steam finds the old launcher or the new one, never half of one
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var temp = target + ".tmp";
                File.WriteAllText(temp, wanted);
                if (!MakeExecutable(temp, log))
                {
                    File.Delete(temp);
                    return false;
                }
                File.Move(temp, target, overwrite: true);
                log.Trace($"Updated DewDrop's Steam launcher at {target}");
                return true;
            }
            catch (Exception ex)
            {
                log.Warn($"Couldn't update DewDrop's Steam launcher at {target}: {ex.Message}");
                return false;
            }
        }

        private static bool MakeExecutable(string path, ILog log)
        {
            try
            {
                var info = new ProcessStartInfo("chmod") { UseShellExecute = false, RedirectStandardError = true };
                info.ArgumentList.Add("755");
                info.ArgumentList.Add(path);
                using var process = Process.Start(info)!;
                var errors = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);
                if (process.ExitCode != 0)
                {
                    log.Warn($"Couldn't make {path} runnable: {errors.Trim()}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Warn($"Couldn't make {path} runnable: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Steam's folder on Linux (the Steam Deck's is ~/.local/share/Steam), or null when there's none with accounts
        /// in it. Flatpak and Snap Steam aren't looked for: games they start see their own sandboxed folders.
        /// </summary>
        public static string? FindSteamRoot(string home)
        {
            foreach (var candidate in new[] { Path.Combine(home, ".steam", "steam"), Path.Combine(home, ".local", "share", "Steam"), Path.Combine(home, ".steam", "debian-installation") })
            {
                if (Directory.Exists(Path.Combine(candidate, "userdata")))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
