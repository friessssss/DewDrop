using System.Text;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Folder layout. DewDrop deliberately lives inside Stardrop's data folder so both tools see the same profiles,
    /// settings, Nexus login and linked mods folder.
    /// </summary>
    public class DewDropPaths
    {
        public string HomePath { get; }

        public DewDropPaths(string? homePath = null)
        {
            HomePath = homePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Stardrop", "Data");
        }

        public string SettingsPath => Path.Combine(HomePath, "Settings.json");
        public string ProfilesFolder => Path.Combine(HomePath, "Profiles");
        public string SelectedModsFolder => Path.Combine(HomePath, "Selected Mods");
        public string CacheFolder => Path.Combine(HomePath, "Cache");
        public string NotionCachePath => Path.Combine(CacheFolder, "Notion.json");
        public string ThumbnailsFolder => Path.Combine(HomePath, "Thumbnails", "Nexus");
        public string DefaultCollectionsFolder => Path.Combine(HomePath, "Collections", "Mods");

        /// <summary>
        /// Where archives download to before they're installed. DewDrop's own folder, not Stardrop's Data\Nexus:
        /// Stardrop empties that one when it starts, and fails to start if a file there is still being written.
        /// </summary>
        public string DownloadsFolder => Path.Combine(CacheFolder, "DewDrop", "downloads");

        /// <summary>Drop folder for nxm:// links handed over by the protocol handler script, one file per link.</summary>
        public string NxmQueueFolder => Path.Combine(CacheFolder, "DewDrop", "nxm");

        /// <summary>What DewDrop remembers between sessions about restarts and mods it has seen (see <see cref="StartupState"/>).</summary>
        public string StartupStatePath => Path.Combine(CacheFolder, "DewDrop", "startup.json");

        /// <summary>Which Nexus mod and file installed mods came from (see <see cref="ModSources"/>).</summary>
        public string ModSourcesPath => Path.Combine(CacheFolder, "DewDrop", "mod-sources.json");

        /// <summary>DewDrop's own folder in the cache: scripts the game runs or Steam starts, and their logs.</summary>
        public string ScriptFolder => Path.Combine(CacheFolder, "DewDrop");

        /// <summary>The Steam launcher on Linux and the Steam Deck (see <see cref="SteamLauncher"/>), with its files beside it.</summary>
        public string SteamLauncherPath => Path.Combine(ScriptFolder, SteamLauncher.ScriptName);

        /// <summary>
        /// A Nexus API key the installer was given (on a Steam Deck, typing one into the game is no fun), taken and saved
        /// to the login at the next start.
        /// </summary>
        public string NexusKeyHandoffPath => Path.Combine(ScriptFolder, "nexus-key.txt");

        public static bool IsSame(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The path with every junction or symlink along it resolved, so one folder reached by two names (a Mods folder
        /// moved to another drive with a junction, Steam's two library paths on Linux) compares equal. Touches the disk,
        /// so call it once, not per mod. Falls back to the plain full path.
        /// </summary>
        public static string Canonical(string path)
        {
            return Canonical(path, 0);
        }

        private static string Canonical(string path, int depth)
        {
            var full = Path.GetFullPath(path);
            if (depth > 8)
            {
                return full;
            }

            try
            {
                var root = Path.GetPathRoot(full) ?? "";
                var current = root;
                foreach (var part in full.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, part);
                    var folder = new DirectoryInfo(current);
                    if (folder.Exists && folder.LinkTarget is not null && folder.ResolveLinkTarget(returnFinalTarget: true) is { } target)
                    {
                        // The target can itself sit under a link
                        current = Canonical(target.FullName, depth + 1);
                    }
                }

                return current;
            }
            catch
            {
                return full;
            }
        }

        public void EnsureFolders()
        {
            foreach (var folder in new[] { HomePath, ProfilesFolder, SelectedModsFolder, CacheFolder, DownloadsFolder, ThumbnailsFolder, NxmQueueFolder })
            {
                Directory.CreateDirectory(folder);
            }

            // Runs at startup, when nothing is downloading yet: archives left by a crash or a failed install are done with
            foreach (var file in new DirectoryInfo(DownloadsFolder).EnumerateFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Still open somewhere; a later start removes it
                }
            }
        }

        /// <summary>A name from an outside source made safe to use as one path segment.</summary>
        public static string SafeSegment(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "unnamed";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            var safe = builder.ToString().Trim().TrimEnd('.', ' ');
            return safe.Length == 0 ? "unnamed" : safe;
        }

        public static bool IsSameOrUnder(string path, string parent)
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
        }
    }
}
