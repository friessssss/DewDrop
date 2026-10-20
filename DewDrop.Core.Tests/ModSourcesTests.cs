using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>mod-sources.json: which Nexus mod and file installed mods came from, for Browse's Installed marks.</summary>
    public class ModSourcesTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        private readonly ScratchFolder _scratch = new();
        private readonly TestLog _log = new();

        private string FilePath => Path.Combine(_scratch.Path, "DewDrop", "mod-sources.json");

        public void Dispose() => _scratch.Dispose();

        private ModSources Load()
        {
            var sources = new ModSources(FilePath, _log);
            sources.Load();
            return sources;
        }

        private static InstalledMod Mod(string id, string? sourceId = null, params string[] keys)
        {
            return new InstalledMod(new Manifest { UniqueID = id, Name = id, Version = "1.0.0", UpdateKeys = keys }, new DirectoryInfo(Path.Combine(Path.GetTempPath(), id)), sourceId);
        }

        [Fact]
        public void AnInstallIsRememberedAcrossSessions()
        {
            var sources = Load();

            Assert.True(sources.RecordInstall("Author.Mod", 100, 2000, "1.2.0", null, Now));
            Assert.True(sources.Save());

            var again = Load();
            var source = again.InstallOf("author.mod");
            Assert.NotNull(source);
            Assert.Equal(100, source!.ModId);
            Assert.Equal(2000, source.FileId);
            Assert.Equal("1.2.0", source.Version);
            Assert.Equal(Now, source.InstalledUtc);
        }

        [Fact]
        public void TheSameInstallAgainChangesNothing()
        {
            var sources = Load();
            sources.RecordInstall("Author.Mod", 100, 2000, "1.2.0", null, Now);

            Assert.False(sources.RecordInstall("Author.Mod", 100, 2000, "1.2.0", 100, Now.AddDays(1)));
            Assert.True(sources.RecordInstall("Author.Mod", 100, 2001, "1.3.0", 100, Now.AddDays(1)));
        }

        [Fact]
        public void AModWhoseManifestNamesAnotherNexusModIsntClaimed()
        {
            var sources = Load();

            // A framework bundled in someone else's download keeps belonging to its own page
            Assert.False(sources.RecordInstall("Framework.Core", 100, 2000, "3.0.0", 1348, Now));
            Assert.Null(sources.InstallOf("Framework.Core"));
        }

        [Fact]
        public void AModInstalledFromSeveralDownloadsBelongsToAllOfThem()
        {
            var sources = Load();
            var framework = Mod("Framework.Core");

            // Bundled in mod 100's download first, then installed from its own page
            sources.RecordInstall("Framework.Core", 100, 2000, "3.0.0", null, Now);
            Assert.True(sources.RecordInstall("Framework.Core", 1348, 9000, "3.0.0", null, Now));

            var source = sources.InstallOf("Framework.Core")!;
            Assert.Equal(1348, source.ModId);
            Assert.Equal(9000, source.FileId);
            Assert.Equal(new[] { 100, 1348 }, sources.NexusIdsOf(framework).OrderBy(id => id));

            // Bundled again: the file installed now is the bundle's, and both pages still have it
            Assert.True(sources.RecordInstall("Framework.Core", 100, 2001, "3.0.0", null, Now));
            Assert.Equal(2001, sources.InstallOf("Framework.Core")!.FileId);
            Assert.Equal(new[] { 100, 1348 }, sources.NexusIdsOf(framework).OrderBy(id => id));
        }

        [Fact]
        public void AFileKnowsWhichModsItInstalled()
        {
            var sources = Load();
            sources.RecordInstall("Author.Code", 100, 2000, "1.0.0", null, Now);
            sources.RecordInstall("Author.CP", 100, 2000, "1.0.0", null, Now);
            sources.RecordInstall("Author.Optional", 100, 2001, "1.0.0", null, Now);

            Assert.Equal(new[] { "Author.Code", "Author.CP" }, sources.IdsInstalledBy(100, 2000).OrderBy(id => id));

            // Updated from a newer file, the code part no longer counts for the old one
            sources.RecordInstall("Author.Code", 100, 2002, "1.1.0", null, Now);
            Assert.Equal(new[] { "Author.CP" }, sources.IdsInstalledBy(100, 2000));
        }

        [Fact]
        public void BadInputIsIgnored()
        {
            var sources = Load();

            Assert.False(sources.RecordInstall("", 100, 2000, "1.0", null, Now));
            Assert.False(sources.RecordInstall("Author.Mod", 0, 2000, "1.0", null, Now));
            Assert.False(sources.RecordInstall("Author.Mod", 100, 0, "1.0", null, Now));
        }

        [Fact]
        public void SmapiIoAnswersAreKeptAndForgotten()
        {
            var sources = Load();

            Assert.True(sources.SetNexusIds(new Dictionary<string, int?> { ["FlashShifter.SVECode"] = 3753, ["Content.Pack"] = null }));
            Assert.False(sources.SetNexusIds(new Dictionary<string, int?> { ["flashshifter.svecode"] = 3753 }));
            sources.Save();

            var again = Load();
            Assert.Equal(new[] { 3753 }, again.NexusIdsOf(Mod("FlashShifter.SVECode")));

            // A later answer without one drops it; a check that didn't ask leaves it
            Assert.False(again.SetNexusIds(new Dictionary<string, int?> { ["Other.Mod"] = null }));
            Assert.True(again.SetNexusIds(new Dictionary<string, int?> { ["FlashShifter.SVECode"] = null }));
            Assert.Empty(again.NexusIdsOf(Mod("FlashShifter.SVECode")));
        }

        [Fact]
        public void AModBelongsToEveryNexusModAnySourceNames()
        {
            var sources = Load();
            sources.RecordInstall("Author.Mod", 200, 2000, "1.0.0", null, Now);
            sources.SetNexusIds(new Dictionary<string, int?> { ["Author.Mod"] = 300 });

            Assert.Equal(new[] { 100, 200, 300 }, sources.NexusIdsOf(Mod("Author.Mod", null, "Nexus:100")).OrderBy(id => id));

            // A collection's copy wasn't installed by DewDrop, so the record doesn't speak for it
            Assert.Equal(new[] { 100, 300 }, sources.NexusIdsOf(Mod("Author.Mod", "collection", "Nexus:100")).OrderBy(id => id));
        }

        [Fact]
        public void UnusableUpdateKeysDontBreakTheLookup()
        {
            var sources = Load();
            var mod = new InstalledMod(new Manifest { UniqueID = "Author.Mod", UpdateKeys = null! }, new DirectoryInfo(Path.GetTempPath()), null);

            Assert.Empty(sources.NexusIdsOf(mod));
            Assert.Empty(sources.NexusIdsOf(Mod("Author.Mod", null, "Nexus:???")));
        }

        [Fact]
        public void ADamagedFileIsSetAsideAndStartedAgain()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, "{ not json");

            var sources = Load();

            Assert.Empty(sources.Data.Installs);
            Assert.True(File.Exists(FilePath + ".bak"));
            Assert.Single(_log.Warnings);
        }

        [Fact]
        public void NullsInAHandEditedFileAreDropped()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, "{ \"Installs\": { \"Author.Mod\": null, \"Good.Mod\": { \"ModId\": 5, \"FileId\": 6, \"Version\": \"1.0\" } }, \"NexusIds\": null }");

            var sources = Load();

            Assert.Null(sources.InstallOf("Author.Mod"));
            Assert.Equal(5, sources.InstallOf("good.mod")!.ModId);
            Assert.Empty(sources.Data.NexusIds);
        }
    }
}
