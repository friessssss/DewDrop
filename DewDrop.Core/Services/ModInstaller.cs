using DewDrop.Core.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace DewDrop.Core.Services
{
    /// <param name="PendingIds">Updates downloaded but waiting for a restart, because the old copy is in use.</param>
    public record InstallResult(List<string> InstalledIds, List<string> UpdatedIds, List<string> PendingIds, List<string> Errors)
    {
        /// <summary>Every mod folder the install wrote (or will, for an update waiting on a restart), with what went in it.</summary>
        public List<InstalledFolder> Folders { get; } = new();
    }

    /// <param name="Version">The manifest version in the folder now, or once a waiting update goes in.</param>
    /// <param name="NexusId">The Nexus mod ID from the manifest's update keys, if it has one.</param>
    public record InstalledFolder(string UniqueId, string Version, string Folder, int? NexusId);

    /// <summary>
    /// Installs mods from an archive. Every folder in the archive holding a manifest.json is a mod; each is
    /// extracted into the install folder, replacing the existing copy of the same unique ID if there is one.
    /// </summary>
    public class ModInstaller
    {
        private readonly ILog _log;
        private readonly PendingUpdates _pending;

        public ModInstaller(ILog log, PendingUpdates pending)
        {
            _log = log;
            _pending = pending;
        }

        public InstallResult Install(string archivePath, string installFolder, IReadOnlyList<InstalledMod> installed)
        {
            var result = new InstallResult(new(), new(), new(), new());
            using var archive = ArchiveFactory.Open(archivePath);
            var entries = archive.Entries.Where(e => !e.IsDirectory && e.Key is not null).ToList();

            // A mod root is the folder holding a manifest.json; nested manifests inside another mod are ignored
            var roots = entries
                .Where(e => Path.GetFileName(e.Key!).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                .Select(e => NormalizeFolder(Path.GetDirectoryName(e.Key!) ?? ""))
                .OrderBy(r => r.Length)
                .ToList();
            roots = roots.Where(r => !roots.Any(other => other != r && other.Length > 0 && r.StartsWith(other + "/"))).ToList();

            if (roots.Count == 0)
            {
                result.Errors.Add("The download doesn't contain any SMAPI mods (no manifest.json).");
                return result;
            }

            // Read every manifest first, so alternative versions of one mod can be told apart before anything is written
            var candidates = new List<(string Root, Manifest Manifest)>();
            foreach (var root in roots)
            {
                try
                {
                    var manifestEntry = entries.First(e => NormalizeFolder(e.Key!).Equals(Combine(root, "manifest.json"), StringComparison.OrdinalIgnoreCase));
                    Manifest? manifest;
                    using (var reader = new StreamReader(manifestEntry.OpenEntryStream()))
                    {
                        manifest = Manifest.Parse(reader.ReadToEnd());
                    }

                    if (manifest is null || string.IsNullOrWhiteSpace(manifest.UniqueID))
                    {
                        result.Errors.Add($"Skipped '{root}': its manifest has no UniqueID.");
                        continue;
                    }

                    candidates.Add((root, manifest));
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Couldn't install '{root}': {ex.Message}");
                    _log.Error($"Reading the manifest of '{root}' in {archivePath} failed: {ex}");
                }
            }

            foreach (var group in candidates.GroupBy(c => c.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                // Update in place when the mod is already installed loosely, so it keeps its folder and config
                var existing = installed.FirstOrDefault(m => m.SourceId is null && m.UniqueId.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
                var variants = group.ToList();
                var chosen = variants.Count == 1 ? variants[0] : PickVariant(variants, existing);
                if (chosen is null)
                {
                    // "Pick one" archives ship each option as its own folder with the same ID; installing several
                    // would leave duplicate IDs, and an update must not swap in a different option than the player's
                    var options = string.Join(", ", variants.Select(v => v.Root.Length == 0 ? "(top folder)" : $"'{v.Root.Split('/').Last()}'"));
                    var name = string.IsNullOrWhiteSpace(variants[0].Manifest.Name) ? group.Key : variants[0].Manifest.Name;
                    result.Errors.Add($"This download has {variants.Count} alternative versions of {name} ({options}) and DewDrop can't tell which one you want, so it installed none. Download it from the mod's Nexus page and copy the one you want into your Mods folder.");
                    continue;
                }

                InstallRoot(chosen.Value.Root, chosen.Value.Manifest, existing, entries, archivePath, installFolder, result);
            }

            return result;
        }

        /// <summary>
        /// For an archive holding several folders with the same unique ID, the one matching the installed copy:
        /// by folder name, then by manifest name. Null when there's nothing installed or no single match.
        /// </summary>
        private static (string Root, Manifest Manifest)? PickVariant(List<(string Root, Manifest Manifest)> variants, InstalledMod? existing)
        {
            if (existing is null)
            {
                return null;
            }

            var byFolder = variants.Where(v => v.Root.Length > 0 && v.Root.Split('/').Last().Equals(existing.Folder.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (byFolder.Count == 1)
            {
                return byFolder[0];
            }

            var byName = variants.Where(v => string.Equals(v.Manifest.Name, existing.Manifest.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            return byName.Count == 1 ? byName[0] : null;
        }

        private void InstallRoot(string root, Manifest manifest, InstalledMod? existing, List<IArchiveEntry> entries, string archivePath, string installFolder, InstallResult result)
        {
            string? staging = null;
            try
            {
                var folderName = root.Length == 0 ? DewDropPaths.SafeSegment(manifest.Name) : DewDropPaths.SafeSegment(root.Split('/').Last());
                var destination = existing?.Folder.FullName ?? Path.Combine(installFolder, folderName);
                if (existing is null && FindSameMod(destination, manifest.UniqueID) is { } sameMod)
                {
                    // The installed list can lag behind a copy installed moments ago; a folder of this name holding the
                    // same mod is that copy, so it's updated rather than installed a second time beside it
                    existing = sameMod;
                }
                if (existing is null)
                {
                    // Never overwrite a folder that belongs to some other mod
                    for (var suffix = 2; Directory.Exists(destination); suffix++)
                    {
                        destination = Path.Combine(installFolder, $"{folderName} ({suffix})");
                    }
                }

                // Extract into a hidden folder beside the destination (SMAPI and the scanner skip dot-folders),
                // so a half-finished extract is never seen as a mod and the final move is instant
                staging = PendingUpdates.CreateStagingFolder(destination);

                var prefix = root.Length == 0 ? "" : root + "/";
                foreach (var entry in entries.Where(e => NormalizeFolder(e.Key!).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    var relative = NormalizeFolder(entry.Key!).Substring(prefix.Length);
                    var target = SafeCombine(staging, relative);
                    if (target is null)
                    {
                        _log.Warn($"Skipped an archive entry that points outside the mod folder: {entry.Key}");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.WriteToFile(target, new ExtractionOptions { Overwrite = true });
                }

                // Putting the mod back where it was waiting to be uninstalled means the player wants it after all. Taken off
                // the list before the new copy goes in, so a game closing right now has either moved the old copy away
                // already (and the new one goes into an empty spot) or leaves the folder alone.
                if (existing is not null)
                {
                    _pending.CancelRemoval(destination);
                }

                if (existing is null)
                {
                    Directory.Move(staging, destination);
                    staging = null;
                    result.InstalledIds.Add(manifest.UniqueID);
                    _log.Info($"Installed {manifest.Name} {manifest.Version} to {destination}");
                }
                else if (PendingUpdates.TrySwap(destination, staging, _log))
                {
                    result.UpdatedIds.Add(manifest.UniqueID);
                    staging = null;
                    _log.Info($"Updated {manifest.Name} to {manifest.Version} in {destination}");
                }
                else
                {
                    // The running game holds the old copy's files; finish after the restart. This also replaces a
                    // removal waiting for the folder, since the player wants the mod again.
                    _pending.Add(new PendingUpdates.Swap(manifest.UniqueID, destination, staging));
                    staging = null;
                    result.PendingIds.Add(manifest.UniqueID);
                    _log.Info($"Downloaded {manifest.Name} {manifest.Version}; it replaces the running copy on restart");
                }

                // "UpdateKeys": null reads as an empty list, but a hand-built manifest can still hold null
                var nexusId = manifest.UpdateKeys is null ? null : manifest.GetNexusId();
                result.Folders.Add(new InstalledFolder(manifest.UniqueID, manifest.Version ?? "", destination, nexusId));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Couldn't install '{root}': {ex.Message}");
                _log.Error($"Install of '{root}' from {archivePath} failed: {ex}");
            }
            finally
            {
                // Only a failed extract leaves its staging folder behind
                if (staging is not null && Directory.Exists(staging))
                {
                    try
                    {
                        Directory.Delete(staging, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        _log.Trace($"Couldn't remove {staging}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>The mod in a folder if its manifest has this unique ID, else null.</summary>
        private InstalledMod? FindSameMod(string folder, string uniqueId)
        {
            try
            {
                var path = Path.Combine(folder, "manifest.json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var manifest = Manifest.Parse(File.ReadAllText(path));
                return manifest is not null && string.Equals(manifest.UniqueID, uniqueId, StringComparison.OrdinalIgnoreCase)
                    ? new InstalledMod(manifest, new DirectoryInfo(folder), null)
                    : null;
            }
            catch (Exception ex)
            {
                _log.Trace($"Couldn't read the manifest in {folder}: {ex.Message}");
                return null;
            }
        }

        private static string NormalizeFolder(string path) => path.Replace('\\', '/').Trim('/');

        private static string Combine(string root, string name) => root.Length == 0 ? name : root + "/" + name;

        /// <summary>Joins an archive path onto a folder, refusing anything that climbs out of it.</summary>
        private static string? SafeCombine(string folder, string relative)
        {
            var path = folder;
            foreach (var segment in relative.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == "..")
                {
                    return null;
                }
                if (segment != ".")
                {
                    path = Path.Combine(path, DewDropPaths.SafeSegment(segment));
                }
            }

            return DewDropPaths.IsSameOrUnder(path, folder) ? path : null;
        }
    }
}
