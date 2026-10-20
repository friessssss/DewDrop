using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <param name="Version">The manifest version installed, or the one a waiting update brings in.</param>
    /// <param name="Source">The Nexus file DewDrop installed it from, if it did.</param>
    public record InstalledCopy(string UniqueId, string Version, InstallSource? Source);

    /// <summary>Works out which of a Nexus mod's files the player already has, for the Installed marks on a mod's page.</summary>
    public static class InstalledFiles
    {
        /// <summary>
        /// The IDs of the files installed. A copy DewDrop installed names its file exactly, as long as it's still that
        /// version. Any other copy matches the one file with its version; when several share it (alternatives, or an
        /// optional file kept in step), only a single MAIN one counts, and otherwise none does rather than guess.
        /// </summary>
        /// <param name="modId">The Nexus mod whose page this is.</param>
        /// <param name="files">Its files on Nexus.</param>
        /// <param name="copies">The installed loose copies that belong to this Nexus mod.</param>
        /// <param name="isComplete">
        /// Whether everything a file installed is still there. One with a part uninstalled isn't marked (nor guessed at by
        /// version), so the part can be put back.
        /// </param>
        public static HashSet<int> Find(int modId, IReadOnlyList<NexusModFile> files, IEnumerable<InstalledCopy> copies, Func<int, bool>? isComplete = null)
        {
            var installed = new HashSet<int>();
            foreach (var copy in copies)
            {
                // A record for another Nexus mod means this copy came bundled in that one's download
                if (copy.Source is { } source && source.ModId == modId && SameVersion(source.Version, copy.Version) && files.Any(f => f.FileId == source.FileId))
                {
                    if (isComplete?.Invoke(source.FileId) != false)
                    {
                        installed.Add(source.FileId);
                    }
                    continue;
                }

                var same = files.Where(f => SameVersion(f.Version, copy.Version)).ToList();
                if (same.Count > 1)
                {
                    same = same.Where(f => f.Category.Equals("MAIN", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (same.Count == 1)
                {
                    installed.Add(same[0].FileId);
                }
            }

            return installed;
        }

        /// <summary>
        /// Whether two version texts name the same version, the way people write them on Nexus and in manifests:
        /// "v1.2" is "1.2", "1.2.0" is "1.2", and capitals don't matter. Empty never matches.
        /// </summary>
        public static bool SameVersion(string? a, string? b)
        {
            var left = Normalize(a);
            var right = Normalize(b);
            return left.Length > 0 && left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string? version)
        {
            var text = version?.Trim() ?? "";
            if (text.Length > 1 && (text[0] == 'v' || text[0] == 'V') && char.IsDigit(text[1]))
            {
                text = text[1..];
            }

            // Trailing ".0" parts only in the numeric part, so "1.0-beta.0" keeps its pre-release tag
            var cut = text.IndexOfAny(new[] { '-', '+' });
            var core = cut < 0 ? text : text[..cut];
            var rest = cut < 0 ? "" : text[cut..];
            var parts = core.Split('.').ToList();
            while (parts.Count > 1 && parts[^1] == "0")
            {
                parts.RemoveAt(parts.Count - 1);
            }

            return string.Join(".", parts) + rest;
        }
    }
}
