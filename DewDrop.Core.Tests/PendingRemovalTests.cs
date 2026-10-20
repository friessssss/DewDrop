using System.Text.Json;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// Uninstalls wait in pending-updates.json beside the updates, until the game no longer has the mod loaded: then the
    /// folder is moved to a reserved hidden name and deleted.
    /// </summary>
    public class PendingRemovalTests : IDisposable
    {
        private readonly ScratchFolder _scratch = new();
        private readonly TestLog _log = new();
        private readonly PendingUpdates _pending;
        private readonly string _mods;

        public PendingRemovalTests()
        {
            _pending = new PendingUpdates(new DewDropPaths(Path.Combine(_scratch.Path, "home")), _log);
            _mods = Path.Combine(_scratch.Path, "Mods");
        }

        public void Dispose() => _scratch.Dispose();

        private string Mod(string folder, string id) => _scratch.Mod(Path.Combine("Mods", folder), id);

        [Fact]
        public void ARemovalReservesAHiddenNameBesideTheMod()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");

            var swap = _pending.AddRemoval("Author.SomeMod", folder);

            Assert.True(swap.Remove);
            Assert.Equal(Path.Combine(_mods, PendingUpdates.StagingFolderName), Path.GetDirectoryName(swap.Staged));
            Assert.Matches("^[0-9a-f]{32}-removed$", Path.GetFileName(swap.Staged));
            Assert.False(Directory.Exists(swap.Staged), "nothing is moved until the game closes");
            Assert.Single(_pending.Removals());
            Assert.False(_pending.IsPending(folder), "a removal isn't an update waiting for a restart");
            Assert.True(Directory.Exists(folder));
        }

        [Fact]
        public void ARemovalDropsAnUpdateWaitingForTheSameFolder()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var staged = PendingUpdates.CreateStagingFolder(folder);
            _pending.Add(new PendingUpdates.Swap("Author.SomeMod", folder, staged));

            _pending.AddRemoval("Author.SomeMod", folder);

            Assert.False(Directory.Exists(staged), "the downloaded update would never be used");
            var swap = Assert.Single(_pending.Load());
            Assert.True(swap.Remove);
        }

        [Fact]
        public void AnUpdateForTheFolderReplacesItsRemoval()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);

            _pending.Add(new PendingUpdates.Swap("Author.SomeMod", folder, PendingUpdates.CreateStagingFolder(folder)));

            Assert.Empty(_pending.Removals());
            Assert.True(_pending.IsPending(folder));
        }

        [Fact]
        public void CancelRemovalTakesTheFolderOffTheList()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var other = Mod("Other", "Author.Other");
            _pending.AddRemoval("Author.SomeMod", folder);
            _pending.AddRemoval("Author.Other", other);

            Assert.True(_pending.CancelRemoval(folder + Path.DirectorySeparatorChar));
            Assert.False(_pending.CancelRemoval(folder));

            var left = Assert.Single(_pending.Removals());
            Assert.Equal("Author.Other", left.UniqueId);
        }

        [Fact]
        public void StartupDeletesARemovedModAndItsStagingFolder()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var keep = Mod("Keep", "Author.Keep");
            _pending.AddRemoval("Author.SomeMod", folder);

            var left = _pending.ApplyAll();

            Assert.Equal(0, left);
            Assert.False(Directory.Exists(folder));
            Assert.False(Directory.Exists(Path.Combine(_mods, PendingUpdates.StagingFolderName)), "an empty staging folder is tidied away");
            Assert.True(Directory.Exists(keep));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void StartupLeavesARemovalThisSessionLoaded()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);

            var left = _pending.ApplyAll(swap => swap.UniqueId == "Author.SomeMod");

            Assert.Equal(1, left);
            Assert.True(Directory.Exists(folder));
            var swap = Assert.Single(_pending.Removals());
            Assert.Equal(0, swap.Attempts);
        }

        [Fact]
        public void AFolderThatNowHoldsAnotherModIsLeftAlone()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);

            // The player put something else there by hand since
            Directory.Delete(folder, recursive: true);
            Mod("Some Mod", "Author.Different");

            _pending.ApplyAll();

            Assert.True(File.Exists(Path.Combine(folder, "manifest.json")));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void AFolderWithoutAManifestIsLeftAlone()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);
            File.Delete(Path.Combine(folder, "manifest.json"));

            _pending.ApplyAll();

            Assert.True(Directory.Exists(folder));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void ARecordThatMovesTheFolderAnywhereElseIsIgnored()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var elsewhere = Path.Combine(_scratch.Path, "elsewhere");
            WriteRecords(new PendingUpdates.Swap("Author.SomeMod", folder, elsewhere, Remove: true));

            _pending.ApplyAll();

            Assert.True(Directory.Exists(folder));
            Assert.False(Directory.Exists(elsewhere));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void ClosingTheGameMovesTheModAsideWithoutDeletingIt()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var update = Mod("Updated", "Author.Updated");
            var staged = PendingUpdates.CreateStagingFolder(update);
            _pending.Add(new PendingUpdates.Swap("Author.Updated", update, staged));
            var swap = _pending.AddRemoval("Author.SomeMod", folder);

            var moved = _pending.MoveRemovalsAside();

            Assert.Equal(new[] { folder }, moved);
            Assert.False(Directory.Exists(folder));
            Assert.True(File.Exists(Path.Combine(swap.Staged, "manifest.json")), "deleting a big mod would hold up the exit");
            var left = Assert.Single(_pending.Load());
            Assert.False(left.Remove);

            // The next start clears it, since nothing waits on it any more
            _pending.CleanUpLeftovers(new[] { _mods });
            Assert.False(Directory.Exists(swap.Staged));
            Assert.True(Directory.Exists(staged), "the waiting update's copy is kept");
        }

        [Fact]
        public void AMoveThatHappenedBeforeACrashIsFinishedAtStartup()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            var swap = _pending.AddRemoval("Author.SomeMod", folder);
            Directory.CreateDirectory(Path.GetDirectoryName(swap.Staged)!);
            Directory.Move(folder, swap.Staged);

            _pending.ApplyAll();

            Assert.False(Directory.Exists(swap.Staged));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void ABlockedRemovalIsRetriedThenGivenUp()
        {
            // Only Windows refuses to rename a folder while a file inside is open
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);
            var gaveUp = new List<PendingUpdates.Swap>();

            using (new FileStream(Path.Combine(folder, "config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                for (var i = 1; i < PendingUpdates.MaxRemovalAttempts; i++)
                {
                    Assert.Equal(1, _pending.ApplyAll(gaveUp: gaveUp));
                    Assert.Equal(i, Assert.Single(_pending.Removals()).Attempts);
                }

                Assert.Equal(0, _pending.ApplyAll(gaveUp: gaveUp));
            }

            Assert.Single(gaveUp);
            Assert.True(File.Exists(Path.Combine(folder, "manifest.json")), "a blocked move leaves the whole mod in place");
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void ABlockedMoveAtExitStaysQueuedForTheScript()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);

            using (new FileStream(Path.Combine(folder, "config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.Empty(_pending.MoveRemovalsAside());
            }

            var swap = Assert.Single(_pending.Removals());
            Assert.Equal(0, swap.Attempts);
            Assert.True(Directory.Exists(folder));
        }

        [Fact]
        public void RecordsFromBeforeUninstallingReadAsUpdates()
        {
            var folder = Mod("Some Mod", "Author.SomeMod");
            Directory.CreateDirectory(Path.GetDirectoryName(_pending.FilePath)!);
            File.WriteAllText(_pending.FilePath, $"[{{\"UniqueId\":\"Author.SomeMod\",\"Target\":{JsonSerializer.Serialize(folder)},\"Staged\":{JsonSerializer.Serialize(folder + "-new")}}}]");

            var swap = Assert.Single(_pending.Load());

            Assert.False(swap.Remove);
            Assert.Equal(0, swap.Attempts);
            Assert.True(_pending.IsPending(folder));
        }

        [Fact]
        public void TheScanReportsStagingFoldersLeftWhereNoModIs()
        {
            // An uninstalled mod that was alone in a group folder leaves only its moved copy there
            var group = Path.Combine(_mods, "Group");
            Directory.CreateDirectory(Path.Combine(group, PendingUpdates.StagingFolderName, Guid.NewGuid().ToString("N") + PendingUpdates.RemovedSuffix));
            Mod("Other", "Author.Other");
            var parents = new List<string>();

            var mods = new ModScanner(_log).Scan(_mods, null, ignoreHidden: true, parents);

            Assert.Single(mods);
            Assert.Equal(new[] { group }, parents);
        }

        private void WriteRecords(params PendingUpdates.Swap[] swaps)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_pending.FilePath)!);
            File.WriteAllText(_pending.FilePath, JsonSerializer.Serialize(swaps));
        }
    }
}
