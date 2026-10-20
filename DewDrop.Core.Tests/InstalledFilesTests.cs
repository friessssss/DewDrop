using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>Which of a Nexus mod's files a mod page marks Installed, instead of offering to install it again.</summary>
    public class InstalledFilesTests
    {
        private const int Page = 100;

        private static NexusModFile File(int id, string version, string category = "MAIN") => new() { FileId = id, Version = version, Category = category, Name = $"File {id}" };

        private static InstallSource Source(int modId, int fileId, string version) => new() { ModId = modId, FileId = fileId, Version = version };

        [Fact]
        public void AnInstallRecordNamesItsFile()
        {
            var files = new[] { File(1, "1.0.0"), File(2, "1.0.0", "OPTIONAL") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Addon", "1.0.0", Source(Page, 2, "1.0.0")) });

            Assert.Equal(new[] { 2 }, installed);
        }

        [Fact]
        public void AFileWithAPartUninstalledIsntMarked()
        {
            // The code part is still there, but the [CP] part from the same download was uninstalled
            var files = new[] { File(1, "1.0.0") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Code", "1.0.0", Source(Page, 1, "1.0.0")) }, isComplete: _ => false);

            Assert.Empty(installed);
        }

        [Fact]
        public void ARecordForAnotherModFallsBackToTheVersion()
        {
            // Bundled in mod 555's download, but this is its own page
            var files = new[] { File(1, "2.0"), File(2, "1.0") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "2.0.0", Source(555, 9, "2.0.0")) });

            Assert.Equal(new[] { 1 }, installed);
        }

        [Fact]
        public void ARecordForAVersionNoLongerInstalledFallsBackToTheVersion()
        {
            // Updated by hand since DewDrop installed file 1
            var files = new[] { File(1, "1.0"), File(2, "1.1") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "1.1", Source(Page, 1, "1.0")) });

            Assert.Equal(new[] { 2 }, installed);
        }

        [Fact]
        public void ARecordForAFileNotListedFallsBackToTheVersion()
        {
            var files = new[] { File(5, "1.0") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "1.0", Source(Page, 1, "1.0")) });

            Assert.Equal(new[] { 5 }, installed);
        }

        [Fact]
        public void WithoutARecordTheOnlyFileAtThatVersionMatches()
        {
            var files = new[] { File(1, "v1.2.0"), File(2, "1.1", "OLD_VERSION") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "1.2", null) });

            Assert.Equal(new[] { 1 }, installed);
        }

        [Fact]
        public void SeveralFilesAtThatVersionMatchOnlyTheirOneMainFile()
        {
            var files = new[] { File(1, "1.2"), File(2, "1.2", "OPTIONAL"), File(3, "1.2", "MISCELLANEOUS") };

            var installed = InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "1.2", null) });

            Assert.Equal(new[] { 1 }, installed);
        }

        [Fact]
        public void AlternativeMainFilesAreNeverGuessed()
        {
            var files = new[] { File(1, "1.2"), File(2, "1.2") };

            Assert.Empty(InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "1.2", null) }));
        }

        [Fact]
        public void NothingMatchesWithoutAVersion()
        {
            var files = new[] { File(1, ""), File(2, "1.0") };

            Assert.Empty(InstalledFiles.Find(Page, files, new[] { new InstalledCopy("Author.Mod", "", null) }));
        }

        [Theory]
        [InlineData("1.2", "1.2", true)]
        [InlineData("v1.2", "1.2", true)]
        [InlineData("V1.2.0", "1.2", true)]
        [InlineData("1.2.0.0", "1.2", true)]
        [InlineData(" 1.2 ", "1.2.0", true)]
        [InlineData("1.0", "1", true)]
        [InlineData("1.2.0-beta.1", "1.2-BETA.1", true)]
        [InlineData("1.2-beta", "1.2", false)]
        [InlineData("1.2.1", "1.2", false)]
        [InlineData("1.20", "1.2", false)]
        [InlineData("version", "1.2", false)]
        [InlineData("", "", false)]
        [InlineData(null, "1.0", false)]
        public void VersionsCompareTheWayPeopleWriteThem(string? a, string b, bool same)
        {
            Assert.Equal(same, InstalledFiles.SameVersion(a, b));
            Assert.Equal(same, InstalledFiles.SameVersion(b, a));
        }
    }
}
