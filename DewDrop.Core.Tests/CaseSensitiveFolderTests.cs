using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// Linux's file systems (the Steam Deck's included) are case-sensitive, so "Mods/Spacecore" and "Mods/SpaceCore" are
    /// two folders: two copies of one mod, which SMAPI refuses both of. Uninstalling one must leave the other alone.
    /// </summary>
    public class CaseSensitiveFolderTests : IDisposable
    {
        private const string Id = "spacechase0.SpaceCore";

        private readonly ScratchFolder _scratch = new();
        private readonly TestLog _log = new();

        public void Dispose() => _scratch.Dispose();

        /// <summary>The old copy, and a newer one extracted beside it into a folder named with different case.</summary>
        private (string Old, string New) TwoCopies()
        {
            return (_scratch.Mod(Path.Combine("Mods", "Spacecore"), Id, "1.0.0"), _scratch.Mod(Path.Combine("Mods", "SpaceCore"), Id, "1.2.0"));
        }

        private PendingUpdates Pending() => new(new DewDropPaths(Path.Combine(_scratch.Path, "home")), _log);

        [Fact]
        public void FoldersThatDifferOnlyInCaseAreNotTheSame()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            var (old, @new) = TwoCopies();

            Assert.False(DewDropPaths.IsSame(old, @new));
            Assert.False(DewDropPaths.IsSameOrUnder(old, @new));
        }

        [Fact]
        public void ReinstallingIntoTheOtherFolderLeavesTheRemoval()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            var (old, @new) = TwoCopies();
            var pending = Pending();
            pending.AddRemoval(Id, old);

            Assert.False(pending.CancelRemoval(@new));

            var left = Assert.Single(pending.Removals());
            Assert.Equal(old, left.Target);
        }

        [Fact]
        public void AnUpdateForTheOtherFolderLeavesTheRemoval()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            var (old, @new) = TwoCopies();
            var pending = Pending();
            pending.AddRemoval(Id, old);

            pending.Add(new PendingUpdates.Swap(Id, @new, PendingUpdates.CreateStagingFolder(@new)));

            var left = Assert.Single(pending.Removals());
            Assert.Equal(old, left.Target);
            Assert.True(pending.IsPending(@new));
            Assert.False(pending.IsPending(old));
        }

        [Fact]
        public void RemovingTheLinkToOneFolderKeepsTheLinkToTheOther()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            var (old, @new) = TwoCopies();
            var links = Directory.CreateDirectory(Path.Combine(_scratch.Path, "Selected Mods")).FullName;
            var oldLink = Path.Combine(links, "Spacecore");
            var newLink = Path.Combine(links, "SpaceCore");
            try
            {
                Directory.CreateSymbolicLink(oldLink, old);
                Directory.CreateSymbolicLink(newLink, @new);
            }
            catch (UnauthorizedAccessException)
            {
                // Symlinks may need permissions the test runner doesn't have
                return;
            }

            new ModLinker(_log).RemoveLinksTo(links, new[] { old });

            Assert.False(Directory.Exists(oldLink));
            Assert.Contains("1.2.0", File.ReadAllText(Path.Combine(newLink, "manifest.json")));
        }
    }
}
