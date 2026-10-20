using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>Finds installed mods: any folder holding a manifest.json, without descending past it.</summary>
    public class ModScanner
    {
        private readonly ILog _log;

        public ModScanner(ILog log)
        {
            _log = log;
        }

        /// <param name="modsFolder">The real mods folder (not the linked Selected Mods folder).</param>
        /// <param name="collectionsFolder">Stardrop's collection install root; each child is one collection's SourceId.</param>
        /// <param name="stagingParents">
        /// Gets every folder holding a staging folder, so leftovers there are cleaned up even once no mod is left beside
        /// them (an uninstalled mod that was alone in a group folder).
        /// </param>
        public List<InstalledMod> Scan(string modsFolder, string? collectionsFolder, bool ignoreHidden = true, ICollection<string>? stagingParents = null)
        {
            var mods = new List<InstalledMod>();
            if (Directory.Exists(modsFolder))
            {
                Walk(new DirectoryInfo(modsFolder), null, ignoreHidden, mods, stagingParents);
            }

            if (collectionsFolder is not null && Directory.Exists(collectionsFolder) && !DewDropPaths.IsSameOrUnder(collectionsFolder, modsFolder))
            {
                foreach (var collection in new DirectoryInfo(collectionsFolder).EnumerateDirectories())
                {
                    Walk(collection, collection.Name, ignoreHidden, mods, stagingParents);
                }
            }

            return mods;
        }

        private void Walk(DirectoryInfo folder, string? sourceId, bool ignoreHidden, List<InstalledMod> found, ICollection<string>? stagingParents)
        {
            IEnumerable<DirectoryInfo> children;
            try
            {
                children = folder.EnumerateDirectories().ToList();
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't read {folder.FullName}: {ex.Message}");
                return;
            }

            foreach (var child in children)
            {
                // Updates waiting to be swapped in (and the copies they replace) aren't installed mods, whatever the
                // hidden-folder setting says; listing them would link a staged copy instead of the real one
                if (child.Name.Equals(PendingUpdates.StagingFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    stagingParents?.Add(folder.FullName);
                    continue;
                }

                // SMAPI itself skips dot-folders, which is also how people park disabled mods
                if (ignoreHidden && (child.Name.StartsWith('.') || child.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    continue;
                }

                var manifestPath = Path.Combine(child.FullName, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    Walk(child, sourceId, ignoreHidden, found, stagingParents);
                    continue;
                }

                try
                {
                    var manifest = Manifest.Parse(File.ReadAllText(manifestPath));
                    if (manifest is null || string.IsNullOrWhiteSpace(manifest.UniqueID))
                    {
                        _log.Warn($"Skipped {child.FullName}: manifest has no UniqueID");
                        continue;
                    }

                    found.Add(new InstalledMod(manifest, child, sourceId));
                }
                catch (Exception ex)
                {
                    _log.Warn($"Skipped {child.FullName}: unreadable manifest ({ex.Message})");
                }
            }
        }
    }
}
