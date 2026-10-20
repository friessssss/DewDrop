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
        /// Windows, Steam version: have Steam start the game in the active profile, by adding SMAPI's --mods-path to
        /// Stardew Valley's launch option. Applied when the game closes, since Steam has to be closed to change it.
        /// Null until chosen: it then follows what Steam's launch option already says, so a --mods-path set by hand (or
        /// by an earlier install) is never taken out unasked.
        /// </summary>
        public bool? StartInProfileFromSteam { get; set; }

        /// <summary>When the game starts with different mods than the active profile, ask whether to restart into it.</summary>
        public bool AskToLoadProfile { get; set; } = true;

        /// <summary>
        /// Nexus SSO application slug. Nexus issues one per registered app; until DewDrop has its own, login
        /// uses a personal API key and this stays empty.
        /// </summary>
        public string NexusSsoSlug { get; set; } = "";
    }
}
