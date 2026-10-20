using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// SessionDiff decides whether a Steam or shortcut launch already loaded what the profile would. The folders are
    /// only compared as paths, so none of them need to exist.
    /// </summary>
    public class SessionDiffTests
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "dewdrop-tests", "game");
        private static readonly string ModsPath = Path.Combine(Root, "Mods");
        private static readonly string CollectionsPath = Path.Combine(Root, "Collections");
        private static readonly ISet<string> Ignored = new HashSet<string>(new[] { "Self.DewDrop" }, StringComparer.OrdinalIgnoreCase);

        private static InstalledMod Loose(string id, string? folder = null)
        {
            return new InstalledMod(new Manifest { UniqueID = id, Name = id + " Name" }, new DirectoryInfo(Path.Combine(ModsPath, folder ?? id)), null);
        }

        private static InstalledMod InCollection(string id, string collection)
        {
            return new InstalledMod(new Manifest { UniqueID = id, Name = id + " Name" }, new DirectoryInfo(Path.Combine(CollectionsPath, collection, id)), collection);
        }

        private static LoadedMod LoadedFrom(InstalledMod mod) => new(mod.UniqueId, mod.Name, mod.Folder.FullName);

        private static SessionDifference Compute(
            IReadOnlyList<InstalledMod> installed,
            IEnumerable<InstalledMod> enabled,
            IReadOnlyList<LoadedMod> loaded,
            string? profileSourceId = null,
            string? runningModsPath = null,
            ISet<string>? rejectedAtLaunch = null)
        {
            var on = new HashSet<InstalledMod>(enabled);
            return SessionDiff.Compute(installed, on.Contains, profileSourceId, loaded, runningModsPath ?? ModsPath, Ignored, rejectedAtLaunch);
        }

        [Fact]
        public void SameModsMatch()
        {
            var a = Loose("A");
            var b = Loose("B");

            var diff = Compute(new[] { a, b }, new[] { a, b }, new[] { LoadedFrom(a), LoadedFrom(b) });

            Assert.True(diff.Matches);
            Assert.Empty(diff.Rejected);
        }

        [Fact]
        public void LoadedModTheProfileLeavesOutIsTurnedOff()
        {
            var a = Loose("A");
            var b = Loose("B");

            var diff = Compute(new[] { a, b }, new[] { a }, new[] { LoadedFrom(a), LoadedFrom(b) });

            Assert.False(diff.Matches);
            Assert.Equal(new[] { "B" }, diff.TurnedOff.Select(m => m.UniqueId));
            Assert.Equal("B Name", diff.TurnedOff[0].Name);
            Assert.Empty(diff.NotLoaded);
        }

        [Fact]
        public void LoadedModTheScanDoesNotKnowIsTurnedOff()
        {
            // Say SMAPI loaded the game's Mods folder while Stardrop's mods folder is elsewhere: nothing would link it, so
            // a restart leaves it out
            var a = Loose("A");

            var diff = Compute(new[] { a }, new[] { a }, new[] { LoadedFrom(a), new LoadedMod("Unknown", "Unknown Name", Path.Combine(Root, "Elsewhere", "Unknown")) });

            Assert.False(diff.Matches);
            Assert.Equal(new[] { "Unknown" }, diff.TurnedOff.Select(m => m.UniqueId));
            Assert.Equal("Unknown Name", diff.TurnedOff[0].Name);
        }

        [Fact]
        public void IgnoredIdsNeverCount()
        {
            // DewDrop is loaded but off in the profile, and an unscanned copy of it loaded too
            var self = Loose("Self.DewDrop");
            var loadedOff = Compute(new[] { self }, Array.Empty<InstalledMod>(), new[] { LoadedFrom(self) });
            Assert.True(loadedOff.Matches);

            // DewDrop is on in the profile, from a folder SMAPI didn't load, and didn't load
            var elsewhere = InCollection("Self.DewDrop", "col");
            var notLoaded = Compute(new[] { elsewhere }, new[] { elsewhere }, Array.Empty<LoadedMod>(), profileSourceId: "col");
            Assert.True(notLoaded.Matches);
            Assert.Empty(notLoaded.Rejected);

            var unscanned = Compute(Array.Empty<InstalledMod>(), Array.Empty<InstalledMod>(), new[] { new LoadedMod("self.dewdrop", "DewDrop", null) });
            Assert.True(unscanned.Matches);
        }

        [Fact]
        public void EnabledModOutsideTheRunningFolderIsNotLoaded()
        {
            // A collection copy is never in the game's Mods folder, so a plain launch can't have seen it
            var a = Loose("A");
            var b = InCollection("B", "col");

            var diff = Compute(new[] { a, b }, new[] { a, b }, new[] { LoadedFrom(a) }, profileSourceId: "col");

            Assert.Equal(new[] { "B" }, diff.NotLoaded.Select(m => m.UniqueId));
            Assert.Empty(diff.Rejected);
        }

        [Fact]
        public void EnabledModSmapiSawAndSkippedIsRejected()
        {
            // Its only copy is in the folder SMAPI loaded, so SMAPI turned it down; a restart wouldn't load it either
            var a = Loose("A");
            var broken = Loose("Broken");

            var diff = Compute(new[] { a, broken }, new[] { a, broken }, new[] { LoadedFrom(a) });

            Assert.True(diff.Matches);
            Assert.Equal(new[] { "Broken" }, diff.Rejected);
        }

        [Fact]
        public void OnlyModsRejectedAtLaunchCountAsRejected()
        {
            // Broken was there when SMAPI started and skipped; Added was copied in (or installed) afterwards
            var a = Loose("A");
            var broken = Loose("Broken");
            var added = Loose("Added");
            var atLaunch = new HashSet<string>(new[] { "broken" }, StringComparer.OrdinalIgnoreCase);

            var diff = Compute(new[] { a, broken, added }, new[] { a, broken, added }, new[] { LoadedFrom(a) }, rejectedAtLaunch: atLaunch);

            Assert.Equal(new[] { "Broken" }, diff.Rejected);
            Assert.Equal(new[] { "Added" }, diff.NotLoaded.Select(m => m.UniqueId));
            Assert.False(diff.Matches);
        }

        [Fact]
        public void TwoCopiesInTheRunningFolderAreNotLoadedRatherThanRejected()
        {
            // SMAPI skips both copies of a duplicated ID; a profile launch links only one, which fixes it
            var first = Loose("Dup", "Dup1");
            var second = Loose("Dup", "Dup2");

            var diff = Compute(new[] { first, second }, new[] { first }, Array.Empty<LoadedMod>());

            Assert.Equal(new[] { "Dup" }, diff.NotLoaded.Select(m => m.UniqueId));
            Assert.Empty(diff.Rejected);
        }

        [Fact]
        public void LoadedFromAnotherCopyThanTheProfileWants()
        {
            var loose = Loose("A");
            var collection = InCollection("A", "col");

            // The profile turns on the collection copy, SMAPI loaded the loose one
            var diff = Compute(new[] { loose, collection }, new[] { collection }, new[] { LoadedFrom(loose) }, profileSourceId: "col");

            Assert.Equal(new[] { "A" }, diff.OtherCopy.Select(m => m.UniqueId));
            Assert.Empty(diff.TurnedOff);
            Assert.False(diff.Matches);
        }

        [Fact]
        public void LinkerPreferenceForTheProfilesCollectionDecidesTheWantedCopy()
        {
            var loose = Loose("A");
            var collection = InCollection("A", "col");

            // Both copies are on: the linker picks the profile's collection copy, so loading the loose one differs
            var diff = Compute(new[] { loose, collection }, new[] { loose, collection }, new[] { LoadedFrom(loose) }, profileSourceId: "col");
            Assert.Equal(new[] { "A" }, diff.OtherCopy.Select(m => m.UniqueId));

            // In a plain (non-collection) profile the loose copy is preferred, and that's the one that loaded
            var plain = Compute(new[] { loose, collection }, new[] { loose, collection }, new[] { LoadedFrom(loose) });
            Assert.True(plain.Matches);
        }

        [Fact]
        public void UnknownLoadFolderOnlyFlagsCollectionCopies()
        {
            var loose = Loose("A");
            var collection = InCollection("A", "col");
            var single = Loose("B");
            var unknown = new[] { new LoadedMod("A", "A", null), new LoadedMod("B", "B", null) };

            var diff = Compute(new[] { loose, collection, single }, new[] { collection, single }, unknown, profileSourceId: "col");

            Assert.Equal(new[] { "A" }, diff.OtherCopy.Select(m => m.UniqueId));
        }

        [Fact]
        public void IdsCompareWithoutCaseAndFoldersAsTheFileSystemDoes()
        {
            var a = Loose("Author.Mod");
            var loaded = new LoadedMod("author.mod", "Mod", a.Folder.FullName + Path.DirectorySeparatorChar);
            var loadedFromOtherCase = new LoadedMod("author.mod", "Mod", a.Folder.FullName.ToUpperInvariant() + Path.DirectorySeparatorChar);

            Assert.True(Compute(new[] { a }, new[] { a }, new[] { loaded }).Matches);

            // On Linux a folder whose name differs only in case is another copy of the mod
            Assert.Equal(!OperatingSystem.IsLinux(), Compute(new[] { a }, new[] { a }, new[] { loadedFromOtherCase }).Matches);
        }

        [Fact]
        public void ListsAreSortedByName()
        {
            var installed = new[] { Loose("C"), Loose("A"), Loose("B") };

            var diff = Compute(installed, Array.Empty<InstalledMod>(), installed.Select(LoadedFrom).ToList());

            Assert.Equal(new[] { "A", "B", "C" }, diff.TurnedOff.Select(m => m.UniqueId));
        }
    }
}
