namespace DewDrop
{
    public class ModConfig
    {
        /// <summary>Show mods Nexus flags as adult content in Browse.</summary>
        public bool ShowAdultContent { get; set; } = false;

        /// <summary>Enable newly installed mods in the active profile.</summary>
        public bool EnableModsOnInstall { get; set; } = true;

        /// <summary>Check smapi.io for mod updates when the game starts.</summary>
        public bool CheckUpdatesOnLaunch { get; set; } = true;

        /// <summary>
        /// Nexus SSO application slug. Nexus issues one per registered app; until DewDrop has its own, login
        /// uses a personal API key and this stays empty.
        /// </summary>
        public string NexusSsoSlug { get; set; } = "";
    }
}
