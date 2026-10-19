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
