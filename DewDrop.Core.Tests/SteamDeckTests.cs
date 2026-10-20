using DewDrop.Core.Services;

namespace DewDrop.Core.Tests
{
    public class SteamDeckTests
    {
        private static Func<string, string?> Env(params (string Name, string Value)[] values) =>
            name => values.FirstOrDefault(v => v.Name == name).Value;

        private const string SteamOS = "NAME=\"SteamOS\"\nPRETTY_NAME=\"SteamOS\"\nID=steamos\nID_LIKE=arch\nVARIANT_ID=steamdeck\nVERSION_ID=3.7.16\n";

        // What a game sees inside Steam's runtime when it reads /etc/os-release
        private const string SteamRuntime = "NAME=\"Steam Runtime\"\nID=steamrt\nID_LIKE=debian\nVERSION_ID=\"1\"\n";

        [Fact]
        public void SteamOSIsToldByItsOsRelease()
        {
            Assert.True(SteamDeck.IsSteamOS(Env(), SteamOS, null, null));
            Assert.True(SteamDeck.IsSteamOS(Env(), "ID=\"steamos\"\n", null, null));
            Assert.False(SteamDeck.IsSteamOS(Env(), SteamRuntime, null, null));
            Assert.False(SteamDeck.IsSteamOS(Env(), "ID=arch\nID_LIKE=steamos\n", null, null));
            Assert.False(SteamDeck.IsSteamOS(Env(), null, null, null));
        }

        [Fact]
        public void ADeckIsToldByItsHardwareOrSteam()
        {
            Assert.True(SteamDeck.IsSteamOS(Env(), SteamRuntime, "Valve\n", "Jupiter\n"));
            Assert.True(SteamDeck.IsSteamOS(Env(), SteamRuntime, "Valve\n", "Galileo\n"));
            Assert.False(SteamDeck.IsSteamOS(Env(), SteamRuntime, "LENOVO\n", "83E1\n"));
            Assert.True(SteamDeck.IsSteamOS(Env(("SteamDeck", "1")), SteamRuntime, null, null));
            Assert.False(SteamDeck.IsSteamOS(Env(("SteamDeck", "0")), SteamRuntime, null, null));
        }

        [Fact]
        public void GameModeIsAGamescopeSession()
        {
            Assert.True(SteamDeck.IsGameMode(Env(("XDG_CURRENT_DESKTOP", "gamescope"))));
            Assert.True(SteamDeck.IsGameMode(Env(("GAMESCOPE_WAYLAND_DISPLAY", "gamescope-0"))));
            Assert.False(SteamDeck.IsGameMode(Env(("XDG_CURRENT_DESKTOP", "KDE"))));
            Assert.False(SteamDeck.IsGameMode(Env()));
        }
    }
}
