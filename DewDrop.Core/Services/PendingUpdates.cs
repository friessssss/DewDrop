using System.Text.Json;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Mod updates that couldn't be swapped in while the game was running. SMAPI keeps a mod's extra DLLs open,
    /// so replacing a loaded mod's folder fails on Windows; the new copy waits in a hidden staging folder and is
    /// swapped in by the relauncher after the game exits, or at the next startup if that mod isn't loaded then.
    /// </summary>
    public class PendingUpdates
    {
        public const string StagingFolderName = ".dewdrop-staging";

        public record Swap(string UniqueId, string Target, string Staged);

        private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };
        private readonly ILog _log;
        private readonly object _lock = new();

        public string FilePath { get; }

        public PendingUpdates(DewDropPaths paths, ILog log)
        {
            _log = log;
            FilePath = Path.Combine(paths.CacheFolder, "DewDrop", "pending-updates.json");
        }

        /// <summary>A hidden folder beside a mod's folder, so the final move stays on one drive and is instant.</summary>
        public static string NewStagingFolder(string modFolder)
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(modFolder).TrimEnd(Path.DirectorySeparatorChar))!;
            return Path.Combine(parent, StagingFolderName, Guid.NewGuid().ToString("N"));
        }

        /// <summary>Creates a new staging folder (see <see cref="NewStagingFolder"/>), with the staging root marked hidden.</summary>
        public static string CreateStagingFolder(string modFolder)
        {
            var staging = NewStagingFolder(modFolder);
            Directory.CreateDirectory(staging);
            try
            {
                // The dot already hides it from SMAPI and the scanner; the attribute also covers Explorer and Stardrop
                var root = new DirectoryInfo(Path.GetDirectoryName(staging)!);
                root.Attributes |= FileAttributes.Hidden;
            }
            catch
            {
                // Only cosmetic
            }

            return staging;
        }

        public List<Swap> Load()
        {
            lock (_lock)
            {
                try
                {
                    return File.Exists(FilePath) ? JsonSerializer.Deserialize<List<Swap>>(File.ReadAllText(FilePath)) ?? new() : new();
                }
                catch (Exception ex)
                {
                    _log.Warn($"Couldn't read pending updates: {ex.Message}");
                    return new();
                }
            }
        }

        public void Add(Swap swap)
        {
            lock (_lock)
            {
                var all = Load();
                foreach (var replaced in all.Where(s => s.Target.Equals(swap.Target, StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    // A newer download for the same folder supersedes the waiting one, whose copy nothing would use
                    all.Remove(replaced);
                    if (!replaced.Staged.Equals(swap.Staged, StringComparison.OrdinalIgnoreCase) && Directory.Exists(replaced.Staged))
                    {
                        TryDelete(replaced.Staged, _log);
                    }
                }

                all.Add(swap);
                Save(all);
            }
        }

        public bool IsPending(string modFolder)
        {
            var full = Path.GetFullPath(modFolder).TrimEnd(Path.DirectorySeparatorChar);
            return Load().Any(s => Path.GetFullPath(s.Target).TrimEnd(Path.DirectorySeparatorChar).Equals(full, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Tries every waiting swap; the ones still blocked stay queued. Returns how many are left.</summary>
        public int ApplyAll()
        {
            lock (_lock)
            {
                var remaining = new List<Swap>();
                foreach (var swap in Load())
                {
                    if (!Directory.Exists(swap.Staged))
                    {
                        continue;
                    }

                    if (TrySwap(swap.Target, swap.Staged, _log))
                    {
                        _log.Info($"Finished updating {swap.UniqueId}");
                    }
                    else
                    {
                        remaining.Add(swap);
                    }
                }

                Save(remaining);
                return remaining.Count;
            }
        }

        /// <summary>
        /// Removes staging folders nothing waits on any more: extracts cut short by a crash or a quit mid-install, and
        /// old copies a swap couldn't delete while a file was open. Only for startup, before anything installs.
        /// </summary>
        /// <param name="parents">Folders that may hold a staging folder: the install folder and the parents of installed mods.</param>
        public void CleanUpLeftovers(IEnumerable<string> parents)
        {
            lock (_lock)
            {
                // Unlike Load, a list that can't be read stops the cleanup: there'd be no telling which copies are still wanted
                List<Swap> waiting;
                try
                {
                    waiting = File.Exists(FilePath) ? JsonSerializer.Deserialize<List<Swap>>(File.ReadAllText(FilePath)) ?? new() : new();
                }
                catch (Exception ex)
                {
                    _log.Warn($"Left the update staging folders alone, since the pending updates couldn't be read: {ex.Message}");
                    return;
                }

                HashSet<string> keep;
                List<string> roots;
                try
                {
                    keep = new HashSet<string>(
                        waiting.Where(s => !string.IsNullOrEmpty(s?.Staged)).Select(s => Path.GetFullPath(s.Staged).TrimEnd(Path.DirectorySeparatorChar)),
                        StringComparer.OrdinalIgnoreCase);
                    roots = parents
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Select(p => Path.Combine(Path.GetFullPath(p), StagingFolderName))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception ex)
                {
                    // A path that won't resolve (hand-edited list, odd folder name); only a cleanup is lost
                    _log.Warn($"Left the update staging folders alone: {ex.Message}");
                    return;
                }

                foreach (var root in roots)
                {
                    try
                    {
                        if (!Directory.Exists(root))
                        {
                            continue;
                        }

                        foreach (var folder in Directory.GetDirectories(root))
                        {
                            if (!keep.Contains(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar)))
                            {
                                _log.Trace($"Removing leftover update copy {folder}");
                                TryDelete(folder, _log);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Trace($"Couldn't clean up {root}: {ex.Message}");
                    }

                    TryDeleteIfEmpty(root);
                }
            }
        }

        /// <summary>
        /// Replaces a mod folder with a fully extracted copy. The old folder is renamed out of the way first: on
        /// Windows that rename fails as a whole while any file inside is in use, so a blocked swap never leaves a
        /// half-deleted mod. The player's own files are carried over (see <see cref="CarryOverPlayerFiles"/>).
        /// </summary>
        public static bool TrySwap(string target, string staged, ILog log)
        {
            string? old = null;
            var carried = new List<string>();
            try
            {
                if (Directory.Exists(target))
                {
                    CarryOverPlayerFiles(target, staged, carried);

                    // A leftover from an earlier swap would block the rename, so clear it or pick a fresh name
                    old = staged + "-old";
                    if (Directory.Exists(old))
                    {
                        TryDelete(old, log);
                    }
                    if (Directory.Exists(old))
                    {
                        old = staged + "-old-" + Guid.NewGuid().ToString("N");
                    }

                    Directory.Move(target, old);
                }

                Directory.Move(staged, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Put the original back if it had already been moved aside
                if (old is not null && Directory.Exists(old) && !Directory.Exists(target))
                {
                    try
                    {
                        Directory.Move(old, target);
                    }
                    catch (Exception restoreEx)
                    {
                        log.Error($"Couldn't put {target} back; the old copy is at {old}: {restoreEx.Message}");
                    }
                }

                // Take the carried files back out, so the next try copies whatever the mod writes in the meantime
                foreach (var file in carried)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Only an older copy of a player file in the waiting folder
                    }
                }

                log.Trace($"Couldn't swap in the new copy of {target} yet: {ex.Message}");
                return false;
            }

            if (old is not null)
            {
                TryDelete(old, log);
            }
            TryDeleteIfEmpty(Path.GetDirectoryName(staged)!);
            return true;
        }

        /// <summary>
        /// Copies the player's own files from the installed copy into the new one: every config.json (replacing a
        /// shipped default), plus files under data/ and top-level .json files the new version doesn't ship, which is
        /// where SMAPI mods keep per-save data. Other leftovers aren't merged, so files a new version dropped stay
        /// gone. Files added to <paramref name="to"/> are listed in <paramref name="added"/>. The relaunch script
        /// applies the same rules.
        /// </summary>
        private static void CarryOverPlayerFiles(string from, string to, List<string> added)
        {
            var root = Path.GetFullPath(from).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetFullPath(file).Substring(root.Length);
                var name = Path.GetFileName(relative);
                var isConfig = name.Equals("config.json", StringComparison.OrdinalIgnoreCase);
                var isData = relative.StartsWith("data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || (!relative.Contains(Path.DirectorySeparatorChar) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

                var destination = Path.Combine(to, relative);
                var exists = File.Exists(destination);
                if (isConfig || (isData && !exists))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(file, destination, overwrite: true);
                    if (!exists)
                    {
                        added.Add(destination);
                    }
                }
            }
        }

        private static void TryDelete(string folder, ILog log)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex)
            {
                // Lives in the hidden staging folder, which nothing loads from; CleanUpLeftovers tries again at the next start
                log.Trace($"Left the old copy at {folder}: {ex.Message}");
            }
        }

        private static void TryDeleteIfEmpty(string folder)
        {
            try
            {
                if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }
            catch
            {
                // An empty hidden folder is harmless
            }
        }

        private void Save(List<Swap> swaps)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                if (swaps.Count == 0)
                {
                    File.Delete(FilePath);
                    return;
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(swaps, _json));
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't save pending updates: {ex.Message}");
            }
        }
    }
}
