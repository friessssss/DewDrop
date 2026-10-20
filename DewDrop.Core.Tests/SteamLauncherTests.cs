using System.Diagnostics;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// DewDrop's Steam launcher for Linux and the Steam Deck (DewDrop.Mod/assets/steam-launch.sh), run (on Linux and
    /// macOS) the way Steam runs it, against a scratch data folder and a game folder whose SMAPI, game launcher and
    /// "runtime" are small scripts that write down how they were started.
    /// </summary>
    public class SteamLauncherTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dewdrop-tests", Guid.NewGuid().ToString("N"));
        private readonly string _data;
        private readonly string _scripts;
        private readonly string _launcher;
        private readonly string _selected;
        private readonly string _game;
        private readonly string _out;
        private readonly NullLog _log = new();

        public SteamLauncherTests()
        {
            _data = Path.Combine(_folder, "Stardrop", "Data");
            _scripts = Path.Combine(_data, "Cache", "DewDrop");
            _launcher = Path.Combine(_scripts, SteamLauncher.ScriptName);
            _selected = Path.Combine(_data, "Selected Mods");
            _game = Path.Combine(_folder, "Stardew Valley");
            _out = Path.Combine(_folder, "out.txt");
            Directory.CreateDirectory(_selected);
            Directory.CreateDirectory(_game);
        }

        public void Dispose()
        {
            Directory.Delete(_folder, recursive: true);
        }

        private static string Shipped => Path.Combine(AppContext.BaseDirectory, SteamLauncher.ScriptName);

        /// <summary>The launcher in the data folder, and a game folder with SMAPI, the game's launcher and a stand-in for Steam's runtime.</summary>
        private void SetUp(bool smapi = true)
        {
            Assert.True(SteamLauncher.Install(Shipped, _launcher, _log));
            File.WriteAllText(Path.Combine(_game, "Stardew Valley.dll"), "");
            Script(Path.Combine(_game, "StardewValley"), "echo vanilla >> \"$OUT\"");
            if (smapi)
            {
                Script(Path.Combine(_game, "StardewModdingAPI"),
                    "{ echo smapi; for a in \"$@\"; do echo \"arg:$a\"; done; echo \"launch:${DEWDROP_LAUNCH:-}\"; echo \"modspath:${SMAPI_MODS_PATH:-}\"; echo \"cwd:$PWD\"; } >> \"$OUT\"");
            }

            // Like Steam's runtime: runs the command after "--"
            Script(Path.Combine(_folder, "runtime"), "echo runtime >> \"$OUT\"; while [ \"$1\" != \"--\" ]; do shift; done; shift; exec \"$@\"");
        }

        private void Script(string path, string body)
        {
            File.WriteAllText(path, $"#!/bin/sh\nOUT='{_out}'\n{body}\n");
            Run("chmod", "755", path);
        }

        private void LinkDewDropIntoProfile()
        {
            var mod = Path.Combine(_folder, "Mods", "DewDrop");
            Directory.CreateDirectory(mod);
            File.WriteAllText(Path.Combine(mod, "manifest.json"), "{\n  \"Name\": \"DewDrop\",\n  \"UniqueID\": \"friessssss.DewDrop\"\n}");
            Directory.CreateSymbolicLink(Path.Combine(_selected, "DewDrop"), mod);
        }

        /// <summary>Runs the launcher with Steam's command and returns what was started, one line each.</summary>
        private List<string> Launch(params string[] command)
        {
            Run(_launcher, command);
            return File.Exists(_out) ? File.ReadAllLines(_out).ToList() : new List<string>();
        }

        private static void Run(string program, params string[] args)
        {
            var info = new ProcessStartInfo(program) { UseShellExecute = false };
            foreach (var arg in args)
            {
                info.ArgumentList.Add(arg);
            }

            // Steam's own variable must never point SMAPI anywhere
            info.Environment["SMAPI_MODS_PATH"] = "/somewhere/else";
            using var process = Process.Start(info)!;
            Assert.True(process.WaitForExit(30_000), $"{program} didn't finish");
        }

        private string[] SteamCommand(params string[] extra) =>
            new[] { Path.Combine(_folder, "runtime"), "--verb=waitforexitandrun", "--", Path.Combine(_game, "StardewValley") }.Concat(extra).ToArray();

        [Fact]
        public void IsUsedByLooksForThisLauncher()
        {
            const string script = "/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh";

            Assert.True(SteamLauncher.IsUsedBy($"\"{script}\" %command%", script));
            Assert.True(SteamLauncher.IsUsedBy($"gamemoderun \"{script}\" %command% -novid", script));
            Assert.False(SteamLauncher.IsUsedBy("\"/home/deck/.local/share/Steam/steamapps/common/Stardew Valley/StardewModdingAPI\" %command%", script));
            Assert.False(SteamLauncher.IsUsedBy("", script));
            Assert.False(SteamLauncher.IsUsedBy(null, script));
        }

        [Fact]
        public void FindsSteamWhereItKeepsAccounts()
        {
            var home = Path.Combine(_folder, "home");
            Assert.Null(SteamLauncher.FindSteamRoot(home));

            var steam = Path.Combine(home, ".local", "share", "Steam");
            Directory.CreateDirectory(Path.Combine(steam, "userdata"));
            Assert.Equal(steam, SteamLauncher.FindSteamRoot(home));
        }

        [Fact]
        public void SmapiTakesTheGameLaunchersPlaceInSteamsCommand()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            SetUp();
            var lines = Launch(SteamCommand("+connect_lobby", "5"));

            // Still inside the runtime, with Steam's own arguments for the game kept
            Assert.Equal(new[] { "runtime", "smapi", "arg:--no-terminal", "arg:+connect_lobby", "arg:5", "launch:", "modspath:", $"cwd:{_game}" }, lines);
            Assert.Contains("on the Mods folder", File.ReadAllText(Path.Combine(_scripts, "steam-launch.log")));
        }

        [Fact]
        public void StartsInTheProfileWhenThatsOnAndTheProfileHasDewDrop()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            SetUp();
            File.WriteAllText(Path.Combine(_scripts, SteamLauncher.StartInProfileName), "");
            LinkDewDropIntoProfile();
            var lines = Launch(SteamCommand());

            Assert.Equal(new[] { "runtime", "smapi", "arg:--no-terminal", "arg:--mods-path", $"arg:{_selected}", "launch:", "modspath:", $"cwd:{_game}" }, lines);
        }

        [Fact]
        public void AProfileWithoutDewDropLoadsTheModsFolderInstead()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            // Not linked yet: starting there would leave no Mods button to fix it with
            SetUp();
            File.WriteAllText(Path.Combine(_scripts, SteamLauncher.StartInProfileName), "");
            var lines = Launch(SteamCommand());

            Assert.DoesNotContain("arg:--mods-path", lines);
            Assert.Contains("has no DewDrop in it yet", File.ReadAllText(Path.Combine(_scripts, "steam-launch.log")));
        }

        [Fact]
        public void ARestartStartsInTheProfileOnceWithItsId()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            SetUp();
            LinkDewDropIntoProfile();
            File.WriteAllText(Path.Combine(_scripts, SteamLauncher.RestartName), "abc123");
            var lines = Launch(SteamCommand());

            Assert.Contains("arg:--mods-path", lines);
            Assert.Contains("launch:abc123", lines);
            Assert.False(File.Exists(Path.Combine(_scripts, SteamLauncher.RestartName)));

            // Taken: the next launch is an ordinary one again
            File.Delete(_out);
            Assert.DoesNotContain("arg:--mods-path", Launch(SteamCommand()));
        }

        [Fact]
        public void WithoutSmapiSteamsCommandRunsUnchanged()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            SetUp(smapi: false);
            Assert.Equal(new[] { "runtime", "vanilla" }, Launch(SteamCommand()));
        }

        [Fact]
        public void ACommandWithoutTheGamesLauncherRunsUnchanged()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            // The Windows version through Proton, say
            SetUp();
            Script(Path.Combine(_folder, "proton"), "echo proton >> \"$OUT\"");
            Assert.Equal(new[] { "runtime", "proton" }, Launch(Path.Combine(_folder, "runtime"), "--", Path.Combine(_folder, "proton"), Path.Combine(_game, "Stardew Valley.exe")));
        }

        [Fact]
        public void InstallKeepsTheCopyCurrentAndRunnable()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.True(SteamLauncher.Install(Shipped, _launcher, _log));
            Assert.Equal(File.ReadAllText(Shipped).Replace("\r\n", "\n"), File.ReadAllText(_launcher));

            File.WriteAllText(_launcher, "old");
            Run("chmod", "644", _launcher);
            Assert.True(SteamLauncher.Install(Shipped, _launcher, _log));
            Assert.Equal(File.ReadAllText(Shipped).Replace("\r\n", "\n"), File.ReadAllText(_launcher));

            // Runnable again: Steam starts it directly
            SetUp();
            Assert.Contains("smapi", Launch(SteamCommand()));
        }

        [Fact]
        public void ARestartWaitsForTheGameThenRunsTheLauncher()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            SetUp();
            LinkDewDropIntoProfile();
            using var game = Process.Start(new ProcessStartInfo("sleep", "1") { UseShellExecute = false })!;
            Relauncher.ScheduleThroughLauncher(_launcher, _game, _scripts, "restart-1", _log, game.Id);

            Assert.True(File.Exists(Path.Combine(_scripts, SteamLauncher.RestartName)), "the restart file is written right away");
            Assert.False(File.Exists(_out), "nothing starts while the game is still running");

            var deadline = DateTime.Now.AddSeconds(20);
            while (!(File.Exists(_out) && File.ReadAllText(_out).Contains("cwd:")) && DateTime.Now < deadline)
            {
                Thread.Sleep(100);
            }

            var lines = File.ReadAllLines(_out);
            Assert.Equal(new[] { "smapi", "arg:--no-terminal", "arg:--mods-path", $"arg:{_selected}", "launch:restart-1", "modspath:", $"cwd:{_game}" }, lines);
            Assert.False(File.Exists(Path.Combine(_scripts, SteamLauncher.RestartName)));
        }

        private sealed class NullLog : ILog
        {
            public void Trace(string message) { }
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string message) { }
        }
    }
}
