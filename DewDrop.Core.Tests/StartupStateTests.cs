using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    public class StartupStateTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dewdrop-tests", Guid.NewGuid().ToString("N"));
        private readonly ListLog _log = new();

        private string FilePath => Path.Combine(_folder, "DewDrop", "startup.json");

        public void Dispose()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }

        private StartupState Load()
        {
            var state = new StartupState(FilePath, _log);
            state.Load();
            return state;
        }

        [Fact]
        public void MissingFileStartsEmpty()
        {
            var state = Load();

            Assert.False(state.Existed);
            Assert.Null(state.Data.Restart);
            Assert.Empty(state.Data.SeenMods);
        }

        [Fact]
        public void SaveCreatesTheFolderAndRoundTrips()
        {
            var state = Load();
            state.Data.Restart = new RestartRecord { Id = "abc", Profile = "Main", AtUtc = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc) };
            state.MarkSeen(new[] { "B", "a" });

            Assert.True(state.Save());
            Assert.True(state.Existed);
            Assert.False(File.Exists(FilePath + ".tmp"));

            var again = Load();
            Assert.True(again.Existed);
            Assert.Equal("abc", again.Data.Restart!.Id);
            Assert.Equal(RestartRecord.StageRestarting, again.Data.Restart.Stage);
            Assert.Equal("Main", again.Data.Restart.Profile);
            Assert.Equal(new[] { "a", "B" }, again.Data.SeenMods);
        }

        [Fact]
        public void NoRestartIsLeftOutOfTheFile()
        {
            var state = Load();
            Assert.True(state.Save());

            Assert.DoesNotContain("Restart", File.ReadAllText(FilePath));
        }

        [Fact]
        public void SeenModsIgnoreCaseAndBlanks()
        {
            var state = Load();

            Assert.True(state.MarkSeen(new[] { "Author.Mod", "", null!, "author.mod" }));
            Assert.False(state.MarkSeen(new[] { "AUTHOR.MOD" }));
            Assert.True(state.HasSeen("author.MOD"));
            Assert.False(state.HasSeen("Other"));
        }

        [Fact]
        public void ForgetMakesAModNewAgain()
        {
            var state = Load();
            state.MarkSeen(new[] { "A", "B" });
            state.Forget(new[] { "a" });
            state.Save();

            var again = Load();
            Assert.False(again.HasSeen("A"));
            Assert.True(again.HasSeen("B"));
        }

        [Fact]
        public void DamagedFileIsSetAsideAndTreatedAsMissing()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, "{ not json");

            var state = Load();

            Assert.False(state.Existed);
            Assert.Empty(state.Data.SeenMods);
            Assert.False(File.Exists(FilePath));
            Assert.Equal("{ not json", File.ReadAllText(FilePath + ".bak"));
            Assert.Single(_log.Warnings);
        }

        [Fact]
        public void NullSeenListInTheFileIsReadAsEmpty()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, "{ \"Proven\": true, \"SeenMods\": null }");

            var state = Load();

            Assert.True(state.Existed);
            Assert.Empty(state.Data.SeenMods);
            Assert.False(state.HasSeen("A"));
        }

        [Fact]
        public void SaveReportsFailure()
        {
            // A file where the folder should be makes the save fail
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "DewDrop"), "");

            var state = Load();

            Assert.False(state.Save());
            Assert.Single(_log.Warnings);
        }

        private sealed class ListLog : ILog
        {
            public List<string> Warnings { get; } = new();

            public void Trace(string message)
            {
            }

            public void Info(string message)
            {
            }

            public void Warn(string message) => Warnings.Add(message);

            public void Error(string message) => Warnings.Add(message);
        }
    }
}
