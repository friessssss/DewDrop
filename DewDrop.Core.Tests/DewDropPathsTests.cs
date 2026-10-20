using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    public class DewDropPathsTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dewdrop-tests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (!Directory.Exists(_folder))
            {
                return;
            }

            // Links go first, on their own, so deleting never reaches through one
            foreach (var folder in new DirectoryInfo(_folder).GetDirectories().Where(d => d.LinkTarget is not null))
            {
                folder.Delete();
            }
            Directory.Delete(_folder, recursive: true);
        }

        [Fact]
        public void IsSameIgnoresATrailingSeparator()
        {
            var path = Path.Combine(_folder, "Mods");

            Assert.True(DewDropPaths.IsSame(path, path + Path.DirectorySeparatorChar));
            Assert.False(DewDropPaths.IsSame(path, path + "2"));
        }

        [Fact]
        public void CaseCountsOnlyOnLinux()
        {
            // As the file systems compare names: Linux's are case-sensitive, Windows' and macOS's (by default) aren't
            var path = Path.Combine(_folder, "Mods");

            Assert.Equal(!OperatingSystem.IsLinux(), DewDropPaths.IsSame(path, path.ToUpperInvariant() + Path.DirectorySeparatorChar));
            Assert.Equal(!OperatingSystem.IsLinux(), DewDropPaths.IsSameOrUnder(Path.Combine(path.ToUpperInvariant(), "A"), path));
        }

        [Fact]
        public void IsSameOrUnderNeedsAWholeSegment()
        {
            var mods = Path.Combine(_folder, "Mods");

            Assert.True(DewDropPaths.IsSameOrUnder(Path.Combine(mods, "A"), mods));
            Assert.True(DewDropPaths.IsSameOrUnder(mods, mods));
            Assert.False(DewDropPaths.IsSameOrUnder(mods + "2", mods));
        }

        [Fact]
        public void CanonicalLeavesAPlainFolderAlone()
        {
            var mods = Directory.CreateDirectory(Path.Combine(_folder, "Mods")).FullName;

            Assert.True(DewDropPaths.IsSame(mods, DewDropPaths.Canonical(mods)));
            Assert.True(DewDropPaths.IsSame(Path.Combine(mods, "Missing", "Mod"), DewDropPaths.Canonical(Path.Combine(mods, "Missing", "Mod"))));
        }

        [Fact]
        public void CanonicalResolvesALinkedFolder()
        {
            var real = Directory.CreateDirectory(Path.Combine(_folder, "Real")).FullName;
            Directory.CreateDirectory(Path.Combine(real, "Inner"));
            var link = Path.Combine(_folder, "Link");
            try
            {
                Directory.CreateSymbolicLink(link, real);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Windows without Developer Mode can't make symbolic links; nothing to check there
                return;
            }

            Assert.False(DewDropPaths.IsSame(link, real));
            Assert.True(DewDropPaths.IsSame(DewDropPaths.Canonical(link), DewDropPaths.Canonical(real)));
            Assert.True(DewDropPaths.IsSame(DewDropPaths.Canonical(Path.Combine(link, "Inner")), DewDropPaths.Canonical(Path.Combine(real, "Inner"))));
        }

        [Fact]
        public void CanonicalResolvesAJunction()
        {
            // How people move a Mods folder to another drive on Windows; junctions need no special rights
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var real = Directory.CreateDirectory(Path.Combine(_folder, "Real")).FullName;
            Directory.CreateDirectory(Path.Combine(real, "Inner"));
            var link = Path.Combine(_folder, "Junction");
            using (var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{real}\"") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true })!)
            {
                mklink.WaitForExit();
                Assert.Equal(0, mklink.ExitCode);
            }

            Assert.True(DewDropPaths.IsSame(DewDropPaths.Canonical(Path.Combine(link, "Inner")), DewDropPaths.Canonical(Path.Combine(real, "Inner"))));
        }
    }
}
