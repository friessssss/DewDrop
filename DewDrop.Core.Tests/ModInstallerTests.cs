using System.IO.Compression;
using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>The installer reports every folder it wrote, which the install record and Undo rely on.</summary>
    public class ModInstallerTests : IDisposable
    {
        private readonly ScratchFolder _scratch = new();
        private readonly TestLog _log = new();
        private readonly ModInstaller _installer;
        private readonly string _mods;

        public ModInstallerTests()
        {
            _installer = new ModInstaller(_log, new PendingUpdates(new DewDropPaths(Path.Combine(_scratch.Path, "home")), _log));
            _mods = Path.Combine(_scratch.Path, "Mods");
            Directory.CreateDirectory(_mods);
        }

        public void Dispose() => _scratch.Dispose();

        /// <summary>A zip holding one folder per (folder, id, version, keys).</summary>
        private string Archive(params (string Folder, string Id, string Version, string? Keys)[] mods)
        {
            var path = Path.Combine(_scratch.Path, Guid.NewGuid().ToString("N") + ".zip");
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (var (folder, id, version, keys) in mods)
            {
                var keyText = keys is null ? "" : $", \"UpdateKeys\": [\"{keys}\"]";
                using (var writer = new StreamWriter(zip.CreateEntry($"{folder}/manifest.json").Open()))
                {
                    writer.Write($"{{ \"Name\": \"{id}\", \"UniqueID\": \"{id}\", \"Version\": \"{version}\"{keyText} }}");
                }
                using (var writer = new StreamWriter(zip.CreateEntry($"{folder}/content.json").Open()))
                {
                    writer.Write("{}");
                }
            }
            return path;
        }

        [Fact]
        public void EveryInstalledFolderIsReportedWithItsVersion()
        {
            var archive = Archive(("Big Mod/[CP] Big Mod", "Author.BigCP", "2.0.0", null), ("Big Mod/Big Mod Code", "Author.BigCode", "2.0.0", "Nexus:100"));

            var result = _installer.Install(archive, _mods, Array.Empty<InstalledMod>());

            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Folders.Count);
            var code = result.Folders.Single(f => f.UniqueId == "Author.BigCode");
            Assert.Equal("2.0.0", code.Version);
            Assert.Equal(Path.Combine(_mods, "Big Mod Code"), code.Folder);
            Assert.Equal(100, code.NexusId);
            Assert.Null(result.Folders.Single(f => f.UniqueId == "Author.BigCP").NexusId);
            Assert.True(File.Exists(Path.Combine(code.Folder, "manifest.json")));
        }

        [Fact]
        public void AnUpdateReportsTheFolderItWentInto()
        {
            var existing = _scratch.Mod(Path.Combine("Mods", "My Folder"), "Author.Mod", "1.0.0");
            var installed = new List<InstalledMod> { new(Manifest.Parse(File.ReadAllText(Path.Combine(existing, "manifest.json")))!, new DirectoryInfo(existing), null) };

            var result = _installer.Install(Archive(("Mod", "Author.Mod", "1.1.0", null)), _mods, installed);

            var folder = Assert.Single(result.Folders);
            Assert.Equal(existing, folder.Folder);
            Assert.Equal("1.1.0", folder.Version);
            Assert.Equal(new[] { "Author.Mod" }, result.UpdatedIds.Concat(result.PendingIds));
        }

        [Fact]
        public void ReinstallingIntoAFolderWaitingToBeUninstalledKeepsIt()
        {
            // The mod list leaves out a mod being uninstalled, so the installer finds its folder by name and updates it
            var pending = new PendingUpdates(new DewDropPaths(Path.Combine(_scratch.Path, "home")), _log);
            var folder = _scratch.Mod(Path.Combine("Mods", "Mod"), "Author.Mod", "1.0.0");
            pending.AddRemoval("Author.Mod", folder);

            var result = _installer.Install(Archive(("Mod", "Author.Mod", "1.0.0", null)), _mods, Array.Empty<InstalledMod>());

            var installed = Assert.Single(result.Folders);
            Assert.Equal(folder, installed.Folder);
            Assert.True(File.Exists(Path.Combine(folder, "config.json")), "the player's settings are carried over");

            // Taken off the list by the install itself, so a game closing before the main thread hears of it keeps it
            Assert.Empty(pending.Removals());
        }
    }
}
