using System.Diagnostics;
using System.Text.Json;
using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    /// <summary>
    /// Runs the relauncher's PowerShell script (Windows only) with -NoRelaunch and a Steam job against a scratch
    /// localconfig.vdf. The job names a steam.exe that isn't running, so no real Steam is ever closed.
    /// </summary>
    public class RelauncherScriptTests : IDisposable
    {
        private const string Smapi = @"C:\Games\Stardew Valley\StardewModdingAPI.exe";
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "dewdrop-tests", Guid.NewGuid().ToString("N"));

        public RelauncherScriptTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            Directory.Delete(_folder, recursive: true);
        }

        private static string Config(string appBlock) =>
            "\"UserLocalConfigStore\"\n{\n\t\"Software\"\n\t{\n\t\t\"Valve\"\n\t\t{\n\t\t\t\"Steam\"\n\t\t\t{\n\t\t\t\t\"apps\"\n\t\t\t\t{\n" +
            appBlock +
            "\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\n}\n";

        private static string App(string option) =>
            "\t\t\t\t\t\"413150\"\n\t\t\t\t\t{\n\t\t\t\t\t\t\"LaunchOptions\"\t\t\"" + option.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"\n\t\t\t\t\t}\n";

        /// <summary>Runs the script on a config file and returns what it reported.</summary>
        private Relauncher.SteamJobResult? Run(string configText, string old, string wanted, out string configAfter)
        {
            var config = Path.Combine(_folder, "localconfig.vdf");
            File.WriteAllText(config, configText);
            var result = Path.Combine(_folder, "result.json");
            var job = Path.Combine(_folder, "job.json");
            File.WriteAllText(job, JsonSerializer.Serialize(new Relauncher.SteamJob(Path.Combine(_folder, "not-steam", "steam.exe"), config, old, wanted, result)));
            var script = Path.Combine(_folder, "relaunch.ps1");
            File.WriteAllText(script, Relauncher.WindowsScript);

            var info = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-GamePid", "0", "-GamePath", _folder, "-ModsPath", _folder, "-PendingFile", Path.Combine(_folder, "none.json"), "-NoRelaunch", "-SteamJob", job })
            {
                info.ArgumentList.Add(arg);
            }

            using (var process = Process.Start(info)!)
            {
                Assert.True(process.WaitForExit(60_000), "the script didn't finish");
            }

            configAfter = File.ReadAllText(config);
            Assert.False(File.Exists(job), "the job file should be removed");
            return File.Exists(result) ? JsonSerializer.Deserialize<Relauncher.SteamJobResult>(File.ReadAllText(result)) : null;
        }

        [Fact]
        public void ReplacesTheOptionAndKeepsABackup()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var old = $"\"{Smapi}\" %command%";
            var wanted = $"\"{Smapi}\" --mods-path \"C:\\Profile Links\" %command%";
            var original = Config(App(old));

            var result = Run(original, old, wanted, out var after);

            Assert.NotNull(result);
            Assert.True(result!.Ok, result.Message);
            Assert.Equal(wanted, SteamLaunchOption.Read(after));
            Assert.Equal(original, File.ReadAllText(Path.Combine(_folder, "localconfig.vdf.dewdrop-backup")));
        }

        [Fact]
        public void LeavesAnOptionThatChangedMeanwhileAlone()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var original = Config(App("-something-else %command%"));

            var result = Run(original, $"\"{Smapi}\" %command%", $"\"{Smapi}\" --mods-path \"C:\\X\" %command%", out var after);

            Assert.NotNull(result);
            Assert.False(result!.Ok);
            Assert.Contains("changed", result.Message);
            Assert.Equal(original, after);
        }

        [Fact]
        public void AddsTheGameWhenSteamHasNoEntryForIt()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var wanted = $"\"{Smapi}\" --mods-path \"C:\\Profile Links\" %command%";

            var result = Run(Config(""), "", wanted, out var after);

            Assert.True(result!.Ok, result.Message);
            Assert.Equal(wanted, SteamLaunchOption.Read(after));
        }
    }
}
