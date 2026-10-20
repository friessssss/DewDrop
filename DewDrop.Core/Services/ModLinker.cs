using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <param name="Failed">Names of the mods that should have been linked but weren't.</param>
    public record LinkResult(int Linked, List<string> Failed)
    {
        public bool Succeeded => Failed.Count == 0;
    }

    /// <summary>
    /// Rebuilds the folder SMAPI loads from (Stardrop's "Selected Mods") as one link per enabled mod: directory
    /// junctions on Windows, symlinks elsewhere. Safe to run while the game is up, since the mods already loaded
    /// keep their open files.
    /// </summary>
    public class ModLinker
    {
        private const string SmapiConfigOverrideName = "SMAPI-config.json";
        private const int MaxCommandLength = 8000;

        private readonly ILog _log;

        public ModLinker(ILog log)
        {
            _log = log;
        }

        /// <param name="alwaysLinkFolder">
        /// A mod folder linked whatever the profile says: the running DewDrop, so the Mods button survives a
        /// collection profile, which never lists it. It may be a link inside <paramref name="selectedModsFolder"/>.
        /// </param>
        /// <returns>What was linked; when <see cref="LinkResult.Succeeded"/> is false, restarting would drop those mods.</returns>
        public LinkResult Rebuild(string selectedModsFolder, Profile profile, IReadOnlyList<InstalledMod> installed, bool suppressCollectionUpdateChecks, string? alwaysLinkFolder = null)
        {
            Directory.CreateDirectory(selectedModsFolder);

            // Resolved before the old links go, since the running copy is usually loaded through one of them
            var failed = new List<string>();
            var always = alwaysLinkFolder is null ? null : FindAlwaysLinked(alwaysLinkFolder, installed);
            if (alwaysLinkFolder is not null && always is null)
            {
                // Rebuilding without it would drop it from the next launch, whoever starts it, so the current links stay
                failed.Add(Path.GetFileName(alwaysLinkFolder.TrimEnd(Path.DirectorySeparatorChar)));
                return new LinkResult(0, failed);
            }

            ClearLinks(selectedModsFolder);

            // Prefer the copy the profile's own collection owns when a unique ID is installed twice
            var enabled = installed
                .Where(profile.IsEnabled)
                .OrderByDescending(m => string.Equals(m.SourceId, profile.SourceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (always is not null)
            {
                enabled.RemoveAll(m => m.UniqueId.Equals(always.UniqueId, StringComparison.OrdinalIgnoreCase));

                // A real folder inside the linked folder survives the rebuild and loads anyway; a link to it as well
                // would load the same ID twice, and SMAPI skips both copies
                if (!IsRealFolderInside(always.Folder.FullName, selectedModsFolder))
                {
                    enabled.Insert(0, always);
                }
            }

            var linkedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var collectionIds = new List<string>();
            var commands = new List<string>();
            var expected = new List<(InstalledMod Mod, string LinkPath)>();
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            foreach (var mod in enabled)
            {
                if (!linkedIds.Add(mod.UniqueId))
                {
                    _log.Warn($"Skipped a second copy of {mod.UniqueId} at {mod.Folder.FullName}");
                    continue;
                }

                if (mod.SourceId is not null)
                {
                    collectionIds.Add(mod.UniqueId);
                }

                var linkPath = Path.Combine(selectedModsFolder, UniqueLinkName(selectedModsFolder, mod, usedNames));
                var target = mod.Folder.FullName;
                expected.Add((mod, linkPath));
                if (isWindows)
                {
                    if (linkPath.Length >= 260)
                    {
                        linkPath = @"\\?\" + linkPath;
                        target = @"\\?\" + target;
                    }
                    commands.Add($"mklink /J \"{linkPath}\" \"{target}\"");
                }
                else
                {
                    // Symlinks need no shell on Unix
                    try
                    {
                        Directory.CreateSymbolicLink(linkPath, target);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Couldn't link {mod.Name}: {ex.Message}");
                    }
                }
            }

            RunBatched(commands);
            WriteSmapiConfigOverride(selectedModsFolder, suppressCollectionUpdateChecks ? collectionIds : new List<string>());

            // mklink only reports problems as text, so check that every link leads to its mod
            var linked = 0;
            foreach (var (mod, linkPath) in expected)
            {
                if (File.Exists(Path.Combine(linkPath, "manifest.json")))
                {
                    linked++;
                }
                else
                {
                    _log.Error($"The link for {mod.Name} wasn't created at {linkPath}");
                    failed.Add(mod.Name);
                }
            }

            _log.Info($"Linked {linked} mods for profile '{profile.Name}'");
            return new LinkResult(linked, failed);
        }

        /// <summary>
        /// Removes the links in the linked folder that lead to these mod folders (ones just uninstalled), so the next
        /// launch in the profile doesn't find a link that leads nowhere. Other links are left as they are.
        /// </summary>
        public void RemoveLinksTo(string selectedModsFolder, IReadOnlyCollection<string> modFolders)
        {
            if (modFolders.Count == 0 || !Directory.Exists(selectedModsFolder))
            {
                return;
            }

            var targets = new HashSet<string>(modFolders.Select(f => Path.GetFullPath(f).TrimEnd(Path.DirectorySeparatorChar)), DewDropPaths.PathComparer);

            // Files too: on Unix a symlink whose folder is already gone is listed as a file
            foreach (var entry in new DirectoryInfo(selectedModsFolder).EnumerateFileSystemInfos())
            {
                try
                {
                    if (entry.LinkTarget is not { Length: > 0 } target)
                    {
                        continue;
                    }

                    // Long paths are linked with the \\?\ prefix (see Rebuild)
                    if (target.StartsWith(@"\\?\", StringComparison.Ordinal) || target.StartsWith(@"\??\", StringComparison.Ordinal))
                    {
                        target = target[4..];
                    }

                    var full = Path.GetFullPath(Path.Combine(selectedModsFolder, target)).TrimEnd(Path.DirectorySeparatorChar);
                    if (targets.Contains(full))
                    {
                        // Removes the junction/symlink itself (a folder's Delete() isn't recursive), never the target's contents
                        entry.Delete();
                    }
                }
                catch (Exception ex)
                {
                    _log.Trace($"Couldn't check the link {entry.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>The installed mod in a folder (following links), or one read from its manifest if the scan didn't list it.</summary>
        private InstalledMod? FindAlwaysLinked(string folder, IReadOnlyList<InstalledMod> installed)
        {
            try
            {
                var info = new DirectoryInfo(folder);
                var real = info.ResolveLinkTarget(returnFinalTarget: true) as DirectoryInfo ?? info;
                var fullName = real.FullName.TrimEnd(Path.DirectorySeparatorChar);
                var match = installed.FirstOrDefault(m => m.Folder.FullName.TrimEnd(Path.DirectorySeparatorChar).Equals(fullName, DewDropPaths.PathComparison));
                if (match is not null)
                {
                    return match;
                }

                var manifest = Manifest.Parse(File.ReadAllText(Path.Combine(fullName, "manifest.json")));
                if (manifest is not null && !string.IsNullOrWhiteSpace(manifest.UniqueID))
                {
                    return new InstalledMod(manifest, new DirectoryInfo(fullName), null);
                }

                _log.Error($"Couldn't link {folder}: its manifest has no UniqueID");
            }
            catch (Exception ex)
            {
                _log.Error($"Couldn't find the mod folder {folder}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Whether a folder sits inside the linked folder under a real (not linked) top-level folder, which
        /// <see cref="ClearLinks"/> leaves in place. One reached through a link is gone once the links are cleared.
        /// </summary>
        private static bool IsRealFolderInside(string folder, string selectedModsFolder)
        {
            if (!DewDropPaths.IsSameOrUnder(folder, selectedModsFolder))
            {
                return false;
            }

            try
            {
                var relative = Path.GetRelativePath(selectedModsFolder, folder);
                var top = new DirectoryInfo(Path.Combine(selectedModsFolder, relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0]));
                return top.Exists && !top.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>The mod's folder name, or a suffixed one when another link or a real folder already has that name.</summary>
        private static string UniqueLinkName(string selectedModsFolder, InstalledMod mod, HashSet<string> usedNames)
        {
            var suffix = mod.SourceId ?? "local";
            var name = mod.Folder.Name;
            for (var n = 1; usedNames.Contains(name) || Directory.Exists(Path.Combine(selectedModsFolder, name)) || File.Exists(Path.Combine(selectedModsFolder, name)); n++)
            {
                name = n == 1 ? $"{mod.Folder.Name} ({suffix})" : $"{mod.Folder.Name} ({suffix} {n})";
            }

            usedNames.Add(name);
            return name;
        }

        /// <summary>Removes only links. A real folder found here is left alone rather than risk deleting a mod.</summary>
        private void ClearLinks(string selectedModsFolder)
        {
            foreach (var entry in new DirectoryInfo(selectedModsFolder).EnumerateDirectories())
            {
                try
                {
                    if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        // Non-recursive delete removes the junction/symlink itself, never the target's contents
                        entry.Delete(false);
                    }
                    else
                    {
                        _log.Warn($"Left a real folder in the linked mods folder alone: {entry.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Couldn't remove link {entry.FullName}: {ex.Message}");
                }
            }

            var overridePath = Path.Combine(selectedModsFolder, SmapiConfigOverrideName);
            if (File.Exists(overridePath))
            {
                File.Delete(overridePath);
            }
        }

        private void RunBatched(List<string> commands)
        {
            var batch = new List<string>();
            var length = 0;
            foreach (var command in commands)
            {
                if (batch.Count > 0 && length + command.Length + 3 >= MaxCommandLength)
                {
                    RunCmd(batch);
                    batch.Clear();
                    length = 0;
                }
                batch.Add(command);
                length += command.Length + 3;
            }

            if (batch.Count > 0)
            {
                RunCmd(batch);
            }
        }

        private void RunCmd(List<string> commands)
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/C {string.Join(" & ", commands)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            try
            {
                using var process = Process.Start(info)!;
                process.StandardOutput.ReadToEnd();
                var errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(errors))
                {
                    _log.Warn($"Linking reported: {errors.Trim()}");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Couldn't run the link command: {ex.Message}");
            }
        }

        /// <summary>Tells SMAPI to skip update checks for collection-pinned mods, scoped to this launch's folder, like Stardrop.</summary>
        private void WriteSmapiConfigOverride(string selectedModsFolder, List<string> suppressedIds)
        {
            if (suppressedIds.Count == 0)
            {
                return;
            }

            var overrides = new Dictionary<string, object> { ["SuppressUpdateChecks"] = suppressedIds };
            File.WriteAllText(Path.Combine(selectedModsFolder, SmapiConfigOverrideName), JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
