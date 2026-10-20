using System.Diagnostics;
using System.Text.Json;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// The relauncher's PowerShell script finishes uninstalls the game held open as it closed (Windows only), with the
    /// same checks as PendingUpdates.TryRemove, and removes the profile's link to the mod. Also ModLinker.RemoveLinksTo,
    /// which does the same from the game as it closes.
    /// </summary>
    public class RelauncherRemovalTests : IDisposable
    {
        private readonly ScratchFolder _scratch = new();
        private readonly TestLog _log = new();
        private readonly PendingUpdates _pending;
        private readonly string _links;

        public RelauncherRemovalTests()
        {
            _pending = new PendingUpdates(new DewDropPaths(Path.Combine(_scratch.Path, "home")), _log);
            _links = Path.Combine(_scratch.Path, "Selected Mods");
            Directory.CreateDirectory(_links);
        }

        public void Dispose() => _scratch.Dispose();

        private string Mod(string folder, string id) => _scratch.Mod(Path.Combine("Mods", folder), id);

        /// <summary>A junction on Windows (as ModLinker makes them), a symlink elsewhere.</summary>
        private string Link(string name, string target)
        {
            var link = Path.Combine(_links, name);
            if (OperatingSystem.IsWindows())
            {
                using var process = Process.Start(new ProcessStartInfo("cmd", $"/C mklink /J \"{link}\" \"{target}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })!;
                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            else
            {
                Directory.CreateSymbolicLink(link, target);
            }

            Assert.True(File.Exists(Path.Combine(link, "manifest.json")), "the test link wasn't made");
            return link;
        }

        private void RunScript()
        {
            var script = Path.Combine(_scratch.Path, "relaunch.ps1");
            File.WriteAllText(script, Relauncher.WindowsScript);
            var info = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-GamePid", "0", "-GamePath", _scratch.Path, "-ModsPath", _links, "-PendingFile", _pending.FilePath, "-NoRelaunch" })
            {
                info.ArgumentList.Add(arg);
            }

            using var process = Process.Start(info)!;
            Assert.True(process.WaitForExit(60_000), "the script didn't finish");
        }

        [Fact]
        public void TheScriptUninstallsAndRemovesTheLink()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var gone = Mod("Gone", "Author.Gone");
            var kept = Mod("Kept", "Author.Kept");
            var goneLink = Link("Gone", gone);
            var keptLink = Link("Kept", kept);
            var swap = _pending.AddRemoval("Author.Gone", gone);

            RunScript();

            Assert.False(Directory.Exists(gone));
            Assert.False(Directory.Exists(swap.Staged), "the moved copy is deleted once the game has closed");
            Assert.False(Directory.Exists(goneLink), "a link to the uninstalled mod would lead nowhere");
            Assert.True(File.Exists(Path.Combine(keptLink, "manifest.json")));
            Assert.False(File.Exists(_pending.FilePath), "nothing is left waiting");
        }

        [Fact]
        public void TheScriptLeavesAFolderThatNowHoldsAnotherMod()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var folder = Mod("Some Mod", "Author.SomeMod");
            _pending.AddRemoval("Author.SomeMod", folder);
            Mod("Some Mod", "Author.SomethingElse");

            RunScript();

            // Kept for DewDrop's own manifest reader to settle at the next start, which drops it
            Assert.True(File.Exists(Path.Combine(folder, "manifest.json")));
            Assert.Single(_pending.Removals());
            _pending.ApplyAll();
            Assert.True(File.Exists(Path.Combine(folder, "manifest.json")));
            Assert.Empty(_pending.Load());
        }

        [Fact]
        public void TheScriptReadsTheModsOwnIdNotItsFrameworks()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            // A content pack names its framework's UniqueID before its own
            var folder = Path.Combine(_scratch.Path, "Mods", "[CP] Pack");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "manifest.json"), "{ \"Name\": \"Pack\", \"ContentPackFor\": { \"UniqueID\": \"Pathoschild.ContentPatcher\" }, \"UniqueID\": \"Me.Pack\", \"Version\": \"1.0.0\" }");
            _pending.AddRemoval("Me.Pack", folder);

            RunScript();

            Assert.False(Directory.Exists(folder));
            Assert.False(File.Exists(_pending.FilePath));
        }

        [Fact]
        public void TheScriptLeavesAManifestItCantReadForDewDrop()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            // SMAPI allows comments and trailing commas, which Windows PowerShell's JSON reader doesn't
            var folder = Path.Combine(_scratch.Path, "Mods", "Commented");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "manifest.json"), "{\n  // the mod\n  \"Name\": \"Commented\",\n  \"UniqueID\": \"Me.Commented\",\n  \"Version\": \"1.0.0\",\n}");
            _pending.AddRemoval("Me.Commented", folder);

            RunScript();

            Assert.True(Directory.Exists(folder));
            Assert.Single(_pending.Removals());

            // The next start reads it properly and finishes the job
            _pending.ApplyAll();
            Assert.False(Directory.Exists(folder));
        }

        [Fact]
        public void TheScriptRefusesARecordThatMovesTheFolderElsewhere()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var folder = Mod("Some Mod", "Author.SomeMod");
            var elsewhere = Path.Combine(_scratch.Path, "elsewhere");
            Directory.CreateDirectory(Path.GetDirectoryName(_pending.FilePath)!);
            File.WriteAllText(_pending.FilePath, JsonSerializer.Serialize(new[] { new PendingUpdates.Swap("Author.SomeMod", folder, elsewhere, Remove: true) }));

            RunScript();

            Assert.True(Directory.Exists(folder));
            Assert.False(Directory.Exists(elsewhere));
        }

        [Fact]
        public void TheScriptKeepsABlockedUninstallAndStillAppliesUpdates()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var blocked = Mod("Blocked", "Author.Blocked");
            var updated = Mod("Updated", "Author.Updated");
            var staged = PendingUpdates.CreateStagingFolder(updated);
            File.WriteAllText(Path.Combine(staged, "manifest.json"), "{ \"UniqueID\": \"Author.Updated\", \"Version\": \"2.0.0\" }");
            _pending.Add(new PendingUpdates.Swap("Author.Updated", updated, staged));
            _pending.AddRemoval("Author.Blocked", blocked);

            using (new FileStream(Path.Combine(blocked, "config.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                RunScript();
            }

            Assert.True(File.Exists(Path.Combine(blocked, "manifest.json")), "a blocked move leaves the whole mod in place");
            Assert.Contains("2.0.0", File.ReadAllText(Path.Combine(updated, "manifest.json")));
            var left = Assert.Single(_pending.Load());
            Assert.True(left.Remove);
            Assert.Equal("Author.Blocked", left.UniqueId);
        }

        [Fact]
        public void TheLinkerRemovesOnlyLinksToTheGivenFolders()
        {
            // Symlinks on Unix may need permissions the test runner doesn't have; junctions on Windows never do
            var gone = Mod("Gone", "Author.Gone");
            var kept = Mod("Kept", "Author.Kept");
            string goneLink, keptLink;
            try
            {
                goneLink = Link("Gone (local)", gone);
                keptLink = Link("Kept", kept);
            }
            catch (UnauthorizedAccessException) when (!OperatingSystem.IsWindows())
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine(_links, "Real Folder"));

            new ModLinker(_log).RemoveLinksTo(_links, new[] { gone + Path.DirectorySeparatorChar });

            Assert.False(Directory.Exists(goneLink));
            Assert.True(Directory.Exists(gone), "only the link goes, never the mod");
            Assert.True(File.Exists(Path.Combine(keptLink, "manifest.json")));
            Assert.True(Directory.Exists(Path.Combine(_links, "Real Folder")));
        }

        [Fact]
        public void TheLinkerRemovesALinkWhoseModHasAlreadyMoved()
        {
            var gone = Mod("Gone", "Author.Gone");
            try
            {
                Link("Gone", gone);
            }
            catch (UnauthorizedAccessException) when (!OperatingSystem.IsWindows())
            {
                return;
            }
            Directory.Move(gone, gone + "-moved");

            new ModLinker(_log).RemoveLinksTo(_links, new[] { gone });

            Assert.Empty(new DirectoryInfo(_links).EnumerateFileSystemInfos());
            Assert.True(Directory.Exists(gone + "-moved"));
        }
    }
}
