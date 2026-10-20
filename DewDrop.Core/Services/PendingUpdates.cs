using System.Text.Json;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Mod updates that couldn't be swapped in while the game was running, and mods waiting to be uninstalled. On
    /// Windows a file the game holds open (a mod's own log, streamed music) stops a loaded mod's folder from being
    /// renamed, and a DLL it loaded can't be deleted; the new copy waits in a hidden staging folder and is swapped in
    /// by the relauncher after the game exits, or at the next startup if that mod isn't loaded then. Removals always
    /// wait for the game to close, since the mods it loaded still read their files.
    /// </summary>
    public class PendingUpdates
    {
        public const string StagingFolderName = ".dewdrop-staging";

        /// <summary>Ends the name reserved for a mod being removed, inside the staging folder beside it.</summary>
        public const string RemovedSuffix = "-removed";

        /// <summary>Startups a removal may fail before DewDrop gives up and asks the player to delete the folder.</summary>
        public const int MaxRemovalAttempts = 3;

        /// <param name="Remove">
        /// An uninstall rather than an update: the mod at <paramref name="Target"/> is moved to <paramref name="Staged"/>
        /// (a name reserved for it, created only by the move) and deleted. An older DewDrop skips the record, since nothing
        /// is staged, and the mod stays.
        /// </param>
        /// <param name="Attempts">Startups that couldn't remove it; see <see cref="MaxRemovalAttempts"/>.</param>
        public record Swap(string UniqueId, string Target, string Staged, bool Remove = false, int Attempts = 0);

        /// <summary>What happened to a mod queued for removal.</summary>
        public enum RemoveResult
        {
            /// <summary>Moved out of the Mods folder (and deleted, when that was asked for).</summary>
            Removed,

            /// <summary>The folder is gone, or now holds some other mod, so there's nothing to remove.</summary>
            Gone,

            /// <summary>Something still holds a file in it; the record stays for the next try.</summary>
            Blocked
        }

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
                foreach (var replaced in all.Where(s => s.Target.Equals(swap.Target, DewDropPaths.PathComparison)).ToList())
                {
                    // A newer download for the same folder supersedes the waiting one, whose copy nothing would use
                    all.Remove(replaced);
                    if (!replaced.Staged.Equals(swap.Staged, DewDropPaths.PathComparison) && Directory.Exists(replaced.Staged))
                    {
                        TryDelete(replaced.Staged, _log);
                    }
                }

                all.Add(swap);
                Save(all);
            }
        }

        /// <summary>Whether an update for this folder is waiting for a restart.</summary>
        public bool IsPending(string modFolder)
        {
            return Load().Any(s => !s.Remove && DewDropPaths.IsSame(s.Target, modFolder));
        }

        /// <summary>
        /// Queues a mod folder to be uninstalled once the game no longer has it loaded. An update waiting for the same
        /// folder is dropped with its downloaded copy, since nothing would use it.
        /// </summary>
        public Swap AddRemoval(string uniqueId, string modFolder)
        {
            var target = Path.GetFullPath(modFolder).TrimEnd(Path.DirectorySeparatorChar);
            var swap = new Swap(uniqueId, target, NewStagingFolder(target) + RemovedSuffix, Remove: true);
            Add(swap);
            return swap;
        }

        /// <summary>Takes a folder off the removal list (an undo, or a new copy installed there); returns whether it was on it.</summary>
        public bool CancelRemoval(string modFolder)
        {
            lock (_lock)
            {
                var all = Load();
                if (all.RemoveAll(s => s.Remove && DewDropPaths.IsSame(s.Target, modFolder)) == 0)
                {
                    return false;
                }

                Save(all);
                return true;
            }
        }

        /// <summary>Mod folders waiting to be uninstalled.</summary>
        public List<Swap> Removals() => Load().Where(s => s.Remove).ToList();

        /// <summary>
        /// Tries every waiting swap and removal; the ones still blocked stay queued. Returns how many are left.
        /// </summary>
        /// <param name="isLoaded">
        /// Whether this session loaded a mod queued for removal (a crash kept its record, then a plain launch loaded it
        /// again). Those wait for this session to end, since the game still reads their files.
        /// </param>
        /// <param name="gaveUp">Gets the removals dropped after <see cref="MaxRemovalAttempts"/> startups, for the player to finish by hand.</param>
        public int ApplyAll(Func<Swap, bool>? isLoaded = null, List<Swap>? gaveUp = null)
        {
            lock (_lock)
            {
                var remaining = new List<Swap>();
                foreach (var swap in Load())
                {
                    // Before the staged-copy check below: a removal has no staged copy until it's moved
                    if (swap.Remove)
                    {
                        if (isLoaded?.Invoke(swap) == true)
                        {
                            remaining.Add(swap);
                            continue;
                        }

                        var result = TryRemove(swap, _log, delete: true);
                        if (result == RemoveResult.Blocked)
                        {
                            var next = swap with { Attempts = swap.Attempts + 1 };
                            if (next.Attempts < MaxRemovalAttempts)
                            {
                                remaining.Add(next);
                            }
                            else
                            {
                                _log.Warn($"Gave up removing {swap.Target}: something keeps a file in it open");
                                gaveUp?.Add(next);
                            }
                        }
                        else if (result == RemoveResult.Removed)
                        {
                            _log.Info($"Finished uninstalling {swap.UniqueId}");
                        }
                        continue;
                    }

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
        /// For the game closing: moves every mod waiting to be uninstalled out of the Mods folder, without deleting it
        /// (deleting a big mod would hold up the exit; the next start clears the staging folder). Returns the folders
        /// that moved. A move that's blocked stays queued for the relaunch script or the next start.
        /// </summary>
        public List<string> MoveRemovalsAside()
        {
            lock (_lock)
            {
                var moved = new List<string>();
                var remaining = new List<Swap>();
                foreach (var swap in Load())
                {
                    if (!swap.Remove)
                    {
                        remaining.Add(swap);
                        continue;
                    }

                    var result = TryRemove(swap, _log, delete: false);
                    if (result == RemoveResult.Blocked)
                    {
                        remaining.Add(swap);
                    }
                    else if (result == RemoveResult.Removed)
                    {
                        moved.Add(swap.Target);
                    }
                }

                Save(remaining);
                return moved;
            }
        }

        /// <summary>
        /// Moves a mod queued for removal to the hidden name reserved for it, then deletes it if asked. Checks first that
        /// the folder still holds that mod, since the player (or Stardrop) may have replaced it since: anything else at
        /// the target is left alone.
        /// </summary>
        public static RemoveResult TryRemove(Swap swap, ILog log, bool delete)
        {
            try
            {
                if (!IsReservedRemovalName(swap))
                {
                    log.Warn($"Left {swap.Target} alone: its removal record doesn't look like one DewDrop made");
                    return RemoveResult.Gone;
                }

                // An earlier move went through but the record outlived it (the game closed before it was saved)
                if (!Directory.Exists(swap.Target))
                {
                    if (delete && Directory.Exists(swap.Staged))
                    {
                        TryDelete(swap.Staged, log);
                        TryDeleteIfEmpty(Path.GetDirectoryName(swap.Staged)!);
                    }
                    return RemoveResult.Gone;
                }

                if (!HoldsMod(swap.Target, swap.UniqueId))
                {
                    log.Info($"Didn't remove {swap.Target}: it no longer holds {swap.UniqueId}");
                    return RemoveResult.Gone;
                }

                var root = Path.GetDirectoryName(swap.Staged)!;
                Directory.CreateDirectory(root);
                try
                {
                    new DirectoryInfo(root).Attributes |= FileAttributes.Hidden;
                }
                catch
                {
                    // Only cosmetic
                }

                // A single rename: on Windows it fails as a whole while a file inside is open, so a blocked removal never
                // leaves half a mod behind (a recursive delete would get as far as the first file in use)
                Directory.Move(swap.Target, swap.Staged);
                log.Info($"Uninstalled {swap.UniqueId} from {swap.Target}");
                if (delete)
                {
                    TryDelete(swap.Staged, log);
                    TryDeleteIfEmpty(root);
                }
                return RemoveResult.Removed;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                log.Trace($"Couldn't remove {swap.Target} yet: {ex.Message}");
                return RemoveResult.Blocked;
            }
        }

        /// <summary>
        /// Whether a removal moves the folder only to a name DewDrop reserves: "&lt;guid&gt;-removed" in the staging folder
        /// beside it. The list is a file anyone can edit, and a removal deletes what it moves.
        /// </summary>
        internal static bool IsReservedRemovalName(Swap swap)
        {
            if (string.IsNullOrEmpty(swap.Target) || string.IsNullOrEmpty(swap.Staged))
            {
                return false;
            }

            var target = Path.GetFullPath(swap.Target).TrimEnd(Path.DirectorySeparatorChar);
            var staged = Path.GetFullPath(swap.Staged).TrimEnd(Path.DirectorySeparatorChar);
            var name = Path.GetFileName(staged);
            var root = Path.GetDirectoryName(staged);
            return name.Length == 32 + RemovedSuffix.Length
                && name.EndsWith(RemovedSuffix, StringComparison.Ordinal)
                && name[..32].All(Uri.IsHexDigit)
                && root is not null
                && Path.GetFileName(root).Equals(StagingFolderName, StringComparison.OrdinalIgnoreCase)
                && Path.GetDirectoryName(target) is { } parent
                && DewDropPaths.IsSame(Path.GetDirectoryName(root)!, parent);
        }

        /// <summary>Whether a folder's manifest.json names this unique ID.</summary>
        private static bool HoldsMod(string folder, string uniqueId)
        {
            var path = Path.Combine(folder, "manifest.json");
            if (!File.Exists(path))
            {
                return false;
            }

            // A file that can't be opened right now throws on to the caller, which tries again later
            var text = File.ReadAllText(path);
            try
            {
                return Manifest.Parse(text) is { } manifest
                    && string.Equals(manifest.UniqueID, uniqueId, StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                // A manifest that can't be read can't be checked, so the folder stays
                return false;
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
                    // Ignoring case here can only keep more, never delete a copy that's still wanted
                    keep = new HashSet<string>(
                        waiting.Where(s => !string.IsNullOrEmpty(s?.Staged)).Select(s => Path.GetFullPath(s.Staged).TrimEnd(Path.DirectorySeparatorChar)),
                        StringComparer.OrdinalIgnoreCase);
                    roots = parents
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Select(p => Path.Combine(Path.GetFullPath(p), StagingFolderName))
                        .Distinct(DewDropPaths.PathComparer)
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
