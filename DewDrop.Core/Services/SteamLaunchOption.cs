using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Stardew Valley's launch option in Steam (Windows), which "Start in my profile from Steam" points at the linked
    /// profile folder with SMAPI's --mods-path. Steam keeps the option in the signed-in account's localconfig.vdf and
    /// writes that file over when it closes, so this only reads it; the change itself is written by the relauncher
    /// script once the game and Steam have closed (see <see cref="Relauncher"/>).
    /// </summary>
    public static class SteamLaunchOption
    {
        public const string AppId = "413150";

        private static readonly Regex Tokens = new("\"((?:[^\"\\\\]|\\\\.)*)\"|\\{|\\}", RegexOptions.Compiled);
        // As SMAPI reads it: exactly "--mods-path" then the folder as the next argument
        private static readonly Regex ModsPathArgument = new("\\s*--mods-path\\s+(?:\"[^\"]*\"|\\S+)", RegexOptions.Compiled);

        /// <summary>Steam's folder from the registry, or null when Steam isn't installed (or this isn't Windows).</summary>
        public static string? FindSteamRoot()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            try
            {
                var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
                    ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string
                    ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                path = Path.GetFullPath(path.Replace('/', '\\'));
                return Directory.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The signed-in account's localconfig.vdf: the running Steam's account, else the one marked MostRecent in
        /// loginusers.vdf, else the most recently written one. Other accounts on the PC are left alone.
        /// </summary>
        public static string? FindUserConfig(string steamRoot)
        {
            try
            {
                string? FileFor(string? accountId)
                {
                    if (string.IsNullOrEmpty(accountId) || accountId == "0")
                    {
                        return null;
                    }
                    var file = Path.Combine(steamRoot, "userdata", accountId, "config", "localconfig.vdf");
                    return File.Exists(file) ? file : null;
                }

                if (OperatingSystem.IsWindows() && FileFor(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess", "ActiveUser", null)?.ToString()) is { } active)
                {
                    return active;
                }

                var logins = Path.Combine(steamRoot, "config", "loginusers.vdf");
                if (File.Exists(logins))
                {
                    var match = Regex.Match(File.ReadAllText(logins), "\"(\\d{17})\"\\s*\\{[^{}]*?\"MostRecent\"\\s*\"1\"", RegexOptions.IgnoreCase);
                    if (match.Success && FileFor((long.Parse(match.Groups[1].Value) - 76561197960265728L).ToString()) is { } recent)
                    {
                        return recent;
                    }
                }

                var userdata = Path.Combine(steamRoot, "userdata");
                return Directory.Exists(userdata)
                    ? new DirectoryInfo(userdata).GetDirectories()
                        .Select(d => new FileInfo(Path.Combine(d.FullName, "config", "localconfig.vdf")))
                        .Where(f => f.Exists)
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault()?.FullName
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Stardew Valley's launch option in a localconfig.vdf: "" when the game has no option set, null when the file
        /// doesn't have the expected layout (then nothing should be written to it).
        /// </summary>
        public static string? Read(string vdfText)
        {
            var names = new List<string>();
            string? pendingKey = null;
            var sawApps = false;
            var appDepth = -1;
            string? options = null;
            foreach (Match token in Tokens.Matches(vdfText))
            {
                switch (token.Value)
                {
                    case "{":
                        names.Add(pendingKey?.ToLowerInvariant() ?? "");
                        pendingKey = null;
                        if (EndsWith(names, "software", "valve", "steam", "apps"))
                        {
                            sawApps = true;
                        }
                        else if (appDepth < 0 && EndsWith(names, "software", "valve", "steam", "apps", AppId))
                        {
                            appDepth = names.Count;
                        }
                        break;

                    case "}":
                        if (names.Count == appDepth)
                        {
                            appDepth = int.MaxValue;
                        }
                        if (names.Count > 0)
                        {
                            names.RemoveAt(names.Count - 1);
                        }
                        pendingKey = null;
                        break;

                    default:
                        var value = token.Groups[1].Value;
                        if (pendingKey is null)
                        {
                            pendingKey = value;
                        }
                        else
                        {
                            if (names.Count == appDepth && pendingKey.Equals("LaunchOptions", StringComparison.OrdinalIgnoreCase))
                            {
                                options = Unescape(value);
                            }
                            pendingKey = null;
                        }
                        break;
                }
            }

            return sawApps ? options ?? "" : null;
        }

        /// <summary>The folder a launch option's --mods-path points at (the last one, as SMAPI uses), or null when it has none.</summary>
        public static string? ModsPath(string? option)
        {
            var match = string.IsNullOrEmpty(option) ? null : ModsPathArgument.Matches(option).LastOrDefault();
            if (match is null)
            {
                return null;
            }

            var value = Regex.Replace(match.Value.Trim(), "^--mods-path\\s+", "").Trim('"');
            return value.Trim().Length == 0 ? null : value;
        }

        /// <summary>Whether a launch option starts SMAPI with --mods-path pointing at a folder.</summary>
        public static bool PointsAt(string? option, string modsFolder)
        {
            try
            {
                return ModsPath(option) is { } path && DewDropPaths.IsSame(TrimSeparators(path), TrimSeparators(modsFolder));
            }
            catch (ArgumentException)
            {
                // Not a path at all
                return false;
            }
        }

        /// <summary>
        /// The launch option with --mods-path pointing at a folder, keeping everything else. Null when the option starts
        /// some other program than SMAPI, whose arguments DewDrop can't know.
        /// </summary>
        public static string? WithModsPath(string? option, string smapiExe, string modsFolder)
        {
            var argument = $" --mods-path \"{modsFolder.TrimEnd('\\', '/')}\"";
            var current = Without(option).Trim();
            if (current.Length == 0)
            {
                return $"\"{smapiExe}\"{argument} %command%";
            }

            // Launch options hold Windows paths, so the name is split off by hand: Path.GetFileName only knows this OS's separator
            var (exe, rest) = SplitProgram(current);
            var name = exe.Substring(exe.LastIndexOfAny(new[] { '\\', '/' }) + 1);
            if (!name.StartsWith("StardewModdingAPI", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!rest.Contains("%command%", StringComparison.OrdinalIgnoreCase))
            {
                rest = rest.TrimEnd() + " %command%";
            }
            return $"\"{exe}\"{argument}{rest}";
        }

        /// <summary>The launch option without any --mods-path, as SMAPI's own instructions set it.</summary>
        public static string Without(string? option)
        {
            return string.IsNullOrEmpty(option) ? "" : ModsPathArgument.Replace(option, "");
        }

        /// <summary>The program a launch option starts (the first quoted path, or the first word) and what follows it.</summary>
        public static (string Exe, string Arguments) SplitProgram(string option)
        {
            var o = option.Trim();
            if (o.StartsWith('"'))
            {
                var end = o.IndexOf('"', 1);
                if (end > 0)
                {
                    return (o.Substring(1, end - 1), o.Substring(end + 1));
                }
            }

            var space = o.IndexOf(' ');
            return space > 0 ? (o.Substring(0, space), o.Substring(space)) : (o, "");
        }

        private static bool EndsWith(List<string> names, params string[] tail)
        {
            if (names.Count < tail.Length)
            {
                return false;
            }

            for (var i = 0; i < tail.Length; i++)
            {
                if (names[names.Count - tail.Length + i] != tail[i].ToLowerInvariant())
                {
                    return false;
                }
            }
            return true;
        }

        private static string Unescape(string value) => value.Replace("\\\"", "\"").Replace("\\\\", "\\");

        /// <summary>A folder without trailing slashes of either kind (a drive root like "C:\" stays as it is).</summary>
        private static string TrimSeparators(string path)
        {
            var trimmed = path.TrimEnd('\\', '/');
            return trimmed.Length == 0 || trimmed.EndsWith(':') ? path : trimmed;
        }
    }
}
