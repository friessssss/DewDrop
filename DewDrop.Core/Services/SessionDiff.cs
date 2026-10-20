using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>A mod SMAPI loaded this session: its unique ID, name, and the folder it came from when SMAPI says.</summary>
    public record LoadedMod(string UniqueId, string Name, string? Folder);

    /// <summary>A mod named in a <see cref="SessionDifference"/>.</summary>
    public record DiffMod(string UniqueId, string Name);

    /// <summary>How the mods SMAPI loaded differ from what a restart into the profile would load.</summary>
    public record SessionDifference(
        IReadOnlyList<DiffMod> TurnedOff,
        IReadOnlyList<DiffMod> NotLoaded,
        IReadOnlyList<DiffMod> OtherCopy,
        IReadOnlyList<string> Rejected)
    {
        /// <summary>
        /// Loaded mods a restart would leave out: no copy of them is on in the profile, or DewDrop's scan doesn't know them
        /// at all (say they're in the game's Mods folder while Stardrop's mods folder is elsewhere), so nothing links them.
        /// </summary>
        public IReadOnlyList<DiffMod> TurnedOff { get; } = TurnedOff;

        /// <summary>Mods on in the profile that didn't load, and would load after a restart.</summary>
        public IReadOnlyList<DiffMod> NotLoaded { get; } = NotLoaded;

        /// <summary>Mods that loaded, but from another copy than the one the profile would link.</summary>
        public IReadOnlyList<DiffMod> OtherCopy { get; } = OtherCopy;

        /// <summary>
        /// IDs of mods on in the profile whose only copy SMAPI saw this session and still didn't load (broken, or missing
        /// something). A restart wouldn't load them either, so they don't count as a difference.
        /// </summary>
        public IReadOnlyList<string> Rejected { get; } = Rejected;

        public bool Matches => TurnedOff.Count == 0 && NotLoaded.Count == 0 && OtherCopy.Count == 0;
    }

    /// <summary>
    /// Works out whether a session SMAPI started on some other folder (the game's Mods folder, say) already loaded what
    /// the profile would. Pure, so it can be tested without the game: ModManager feeds it the scan, the profile and
    /// what SMAPI reports.
    /// </summary>
    public static class SessionDiff
    {
        /// <param name="installed">Every mod DewDrop's scan found.</param>
        /// <param name="isEnabled">Whether the profile turns a given installed copy on.</param>
        /// <param name="profileSourceId">The profile's collection, whose copies the linker prefers when an ID is installed twice.</param>
        /// <param name="loaded">What SMAPI loaded this session.</param>
        /// <param name="runningModsPath">The folder SMAPI loaded from; mods inside it that didn't load were seen and rejected.</param>
        /// <param name="ignoredIds">IDs never counted either way (DewDrop itself, which is always linked).</param>
        /// <param name="rejectedAtLaunch">
        /// When given, only these IDs can count as rejected: the ones a first call (without it) found as the game started.
        /// A mod copied in or installed later wasn't there for SMAPI to see, so it's not loaded rather than rejected.
        /// </param>
        public static SessionDifference Compute(
            IReadOnlyList<InstalledMod> installed,
            Func<InstalledMod, bool> isEnabled,
            string? profileSourceId,
            IReadOnlyList<LoadedMod> loaded,
            string runningModsPath,
            ISet<string> ignoredIds,
            ISet<string>? rejectedAtLaunch = null)
        {
            var loadedById = loaded
                .GroupBy(m => m.UniqueId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var installedById = installed
                .GroupBy(m => m.UniqueId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var turnedOff = new List<DiffMod>();
            var notLoaded = new List<DiffMod>();
            var otherCopy = new List<DiffMod>();
            var rejected = new List<string>();

            // Loaded, and either no copy of it is on in the profile or the scan doesn't know it (nothing would link it)
            foreach (var mod in loaded)
            {
                if (ignoredIds.Contains(mod.UniqueId))
                {
                    continue;
                }

                if (!installedById.TryGetValue(mod.UniqueId, out var copies) || !copies.Any(isEnabled))
                {
                    turnedOff.Add(new DiffMod(mod.UniqueId, mod.Name));
                }
            }

            foreach (var (id, copies) in installedById)
            {
                if (ignoredIds.Contains(id))
                {
                    continue;
                }

                // The copy the linker would pick: an enabled one, preferring the profile's own collection
                var wanted = copies
                    .Where(isEnabled)
                    .OrderByDescending(m => string.Equals(m.SourceId, profileSourceId, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                if (wanted is null)
                {
                    continue;
                }

                if (!loadedById.TryGetValue(id, out var loadedMod))
                {
                    // SMAPI saw the only copy in its folder and didn't load it, so a restart won't either. Two copies of
                    // one ID make SMAPI skip both, and a profile launch (one link) does fix that.
                    var inRunningFolder = copies.Count(c => DewDropPaths.IsSameOrUnder(c.Folder.FullName, runningModsPath));
                    if (inRunningFolder == 1 && DewDropPaths.IsSameOrUnder(wanted.Folder.FullName, runningModsPath) && (rejectedAtLaunch is null || rejectedAtLaunch.Contains(id)))
                    {
                        rejected.Add(id);
                    }
                    else
                    {
                        notLoaded.Add(new DiffMod(id, wanted.Name));
                    }
                    continue;
                }

                if (loadedMod.Folder is null)
                {
                    // Without SMAPI's folder there's no telling which copy loaded; only a collection copy can differ
                    if (wanted.SourceId is not null && copies.Count > 1)
                    {
                        otherCopy.Add(new DiffMod(id, wanted.Name));
                    }
                }
                else if (!DewDropPaths.IsSame(loadedMod.Folder, wanted.Folder.FullName))
                {
                    otherCopy.Add(new DiffMod(id, wanted.Name));
                }
            }

            static List<DiffMod> Sorted(List<DiffMod> list) => list.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
            return new SessionDifference(Sorted(turnedOff), Sorted(notLoaded), Sorted(otherCopy), rejected);
        }
    }
}
