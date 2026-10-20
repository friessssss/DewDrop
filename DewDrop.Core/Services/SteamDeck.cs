namespace DewDrop.Core.Services
{
    /// <summary>
    /// Tells a Steam Deck, and Game Mode on it, from a desktop. In Game Mode only the game and Steam show: there's no
    /// terminal, file manager or desktop browser, so DewDrop leaves out what would need one.
    /// </summary>
    public static class SteamDeck
    {
        /// <summary>
        /// Whether this is SteamOS (a Steam Deck, or another device running it), from what it's given: Steam's SteamDeck
        /// variable, the computer's os-release text, and its maker and model as /sys reports them.
        /// </summary>
        public static bool IsSteamOS(Func<string, string?> environment, string? osRelease, string? vendor, string? product)
        {
            if (environment("SteamDeck") == "1")
            {
                return true;
            }

            // Jupiter is the LCD Steam Deck, Galileo the OLED one
            if (vendor?.Trim() == "Valve" && product?.Trim() is "Jupiter" or "Galileo")
            {
                return true;
            }

            return osRelease is not null && osRelease
                .Split('\n')
                .Select(line => line.Trim())
                .Any(line => line.StartsWith("ID=", StringComparison.Ordinal) && line.Substring(3).Trim('"', '\'').Equals("steamos", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Whether this session is Game Mode (or another gamescope session), where no desktop windows can show.</summary>
        public static bool IsGameMode(Func<string, string?> environment)
        {
            return string.Equals(environment("XDG_CURRENT_DESKTOP"), "gamescope", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(environment("GAMESCOPE_WAYLAND_DISPLAY"));
        }

        private static bool? _isSteamOS;
        private static bool? _isGameMode;

        /// <summary>Whether this computer runs SteamOS, read once.</summary>
        public static bool Current => _isSteamOS ??= OperatingSystem.IsLinux() && IsSteamOS(
            Environment.GetEnvironmentVariable,
            // Inside Steam's runtime (where Steam starts native games), /etc is the runtime's and the computer's is under /run/host
            ReadFile("/run/host/os-release") ?? ReadFile("/etc/os-release"),
            ReadFile("/sys/devices/virtual/dmi/id/sys_vendor"),
            ReadFile("/sys/devices/virtual/dmi/id/product_name"));

        /// <summary>Whether the game is running in Game Mode, read once.</summary>
        public static bool InGameMode => _isGameMode ??= OperatingSystem.IsLinux() && IsGameMode(Environment.GetEnvironmentVariable);

        private static string? ReadFile(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
