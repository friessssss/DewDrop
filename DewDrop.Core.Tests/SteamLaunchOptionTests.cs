using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    public class SteamLaunchOptionTests
    {
        private const string Smapi = @"C:\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe";
        private const string Selected = @"C:\Users\me\AppData\Roaming\Stardrop\Data\Selected Mods";

        // A trimmed localconfig.vdf with another game's launch option first
        private static string Config(string appBlock) =>
            "\"UserLocalConfigStore\"\n{\n\t\"Software\"\n\t{\n\t\t\"Valve\"\n\t\t{\n\t\t\t\"Steam\"\n\t\t\t{\n\t\t\t\t\"apps\"\n\t\t\t\t{\n" +
            "\t\t\t\t\t\"105600\"\n\t\t\t\t\t{\n\t\t\t\t\t\t\"LaunchOptions\"\t\t\"-other\"\n\t\t\t\t\t}\n" +
            appBlock + "\n" +
            "\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\n}\n";

        [Fact]
        public void ReadsStardewValleysOptionOnly()
        {
            var text = Config("\t\t\t\t\t\"413150\"\n\t\t\t\t\t{\n\t\t\t\t\t\t\"LastPlayed\"\t\t\"1\"\n\t\t\t\t\t\t\"LaunchOptions\"\t\t\"\\\"C:\\\\Steam\\\\StardewModdingAPI.exe\\\" %command%\"\n\t\t\t\t\t}");

            Assert.Equal("\"C:\\Steam\\StardewModdingAPI.exe\" %command%", SteamLaunchOption.Read(text));
        }

        [Fact]
        public void MissingOptionReadsEmptyAndMissingAppsReadsNull()
        {
            Assert.Equal("", SteamLaunchOption.Read(Config("")));
            Assert.Equal("", SteamLaunchOption.Read(Config("\t\t\t\t\t\"413150\"\n\t\t\t\t\t{\n\t\t\t\t\t\t\"LastPlayed\"\t\t\"1\"\n\t\t\t\t\t}")));
            Assert.Null(SteamLaunchOption.Read("\"UserLocalConfigStore\"\n{\n}"));
        }

        [Fact]
        public void LaunchOptionsOutsideTheAppBlockDontCount()
        {
            // A key with the same name nested deeper, inside the app's block, isn't the app's own option
            var text = Config("\t\t\t\t\t\"413150\"\n\t\t\t\t\t{\n\t\t\t\t\t\t\"cloud\"\n\t\t\t\t\t\t{\n\t\t\t\t\t\t\t\"LaunchOptions\"\t\t\"nested\"\n\t\t\t\t\t\t}\n\t\t\t\t\t}");

            Assert.Equal("", SteamLaunchOption.Read(text));
        }

        [Fact]
        public void EmptyOptionGetsTheWholeSmapiOption()
        {
            Assert.Equal($"\"{Smapi}\" --mods-path \"{Selected}\" %command%", SteamLaunchOption.WithModsPath("", Smapi, Selected));
            Assert.Equal($"\"{Smapi}\" --mods-path \"{Selected}\" %command%", SteamLaunchOption.WithModsPath(null, Smapi, Selected + "\\"));
        }

        [Fact]
        public void SmapiOptionKeepsItsOtherParts()
        {
            var option = $"\"{Smapi}\" --some-flag %command% -novid";

            Assert.Equal($"\"{Smapi}\" --mods-path \"{Selected}\" --some-flag %command% -novid", SteamLaunchOption.WithModsPath(option, Smapi, Selected));
        }

        [Fact]
        public void AnExistingModsPathIsReplaced()
        {
            var option = $"\"{Smapi}\" --mods-path \"D:\\Old Place\" %command%";

            var result = SteamLaunchOption.WithModsPath(option, Smapi, Selected);

            Assert.Equal($"\"{Smapi}\" --mods-path \"{Selected}\" %command%", result);
            Assert.True(SteamLaunchOption.PointsAt(result, Selected));
            Assert.False(SteamLaunchOption.PointsAt(option, Selected));
        }

        [Fact]
        public void CommandIsAddedWhenMissing()
        {
            Assert.Equal($"\"{Smapi}\" --mods-path \"{Selected}\" %command%", SteamLaunchOption.WithModsPath($"\"{Smapi}\"", Smapi, Selected));
        }

        [Fact]
        public void AnotherProgramIsLeftAlone()
        {
            Assert.Null(SteamLaunchOption.WithModsPath("\"C:\\Tools\\launcher.exe\" %command%", Smapi, Selected));
            Assert.Null(SteamLaunchOption.WithModsPath("-novid %command%", Smapi, Selected));
        }

        [Fact]
        public void WithoutRemovesOnlyTheModsPath()
        {
            Assert.Equal($"\"{Smapi}\" %command%", SteamLaunchOption.Without($"\"{Smapi}\" --mods-path \"{Selected}\" %command%"));
            Assert.Equal($"\"{Smapi}\" %command%", SteamLaunchOption.Without($"\"{Smapi}\" --mods-path C:\\Mods %command%"));
            Assert.Equal($"\"{Smapi}\" %command%", SteamLaunchOption.Without($"\"{Smapi}\" %command%"));
            Assert.Equal("", SteamLaunchOption.Without(null));
        }

        [Fact]
        public void ModsPathReadsQuotedAndBareValues()
        {
            Assert.Equal(Selected, SteamLaunchOption.ModsPath($"\"{Smapi}\" --mods-path \"{Selected}\" %command%"));
            Assert.Equal(@"C:\Mods", SteamLaunchOption.ModsPath($"\"{Smapi}\" --mods-path C:\\Mods %command%"));
            Assert.Null(SteamLaunchOption.ModsPath($"\"{Smapi}\" %command%"));
            Assert.True(SteamLaunchOption.PointsAt($"\"{Smapi}\" --mods-path \"{Selected.ToUpperInvariant()}\\\" %command%", Selected));
        }

        [Fact]
        public void ModsPathIsReadTheWaySmapiReadsIt()
        {
            // The last one wins, "--mods-path=X" isn't an option SMAPI knows, and an empty value isn't a folder
            Assert.Equal(@"C:\Second", SteamLaunchOption.ModsPath($"\"{Smapi}\" --mods-path \"C:\\First\" --mods-path \"C:\\Second\" %command%"));
            Assert.Null(SteamLaunchOption.ModsPath($"\"{Smapi}\" --mods-path=C:\\Mods %command%"));
            Assert.Null(SteamLaunchOption.ModsPath($"\"{Smapi}\" --mods-path \"\" %command%"));
            Assert.False(SteamLaunchOption.PointsAt($"\"{Smapi}\" --mods-path \"\" %command%", Selected));
            Assert.False(SteamLaunchOption.PointsAt($"\"{Smapi}\" --mods-path \"C:\\bad|name\" %command%", Selected));
        }

        [Fact]
        public void SplitProgramHandlesQuotesAndBareWords()
        {
            Assert.Equal((Smapi, " %command%"), SteamLaunchOption.SplitProgram($"\"{Smapi}\" %command%"));
            Assert.Equal(("smapi.exe", " %command%"), SteamLaunchOption.SplitProgram("smapi.exe %command%"));
            Assert.Equal(("smapi.exe", ""), SteamLaunchOption.SplitProgram("smapi.exe"));
        }
    }
}
