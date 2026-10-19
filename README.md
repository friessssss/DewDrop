# DewDrop

DewDrop is a mod manager that lives inside Stardew Valley. A **Mods** button sits on the title screen next to
New, Load, Co-op and Exit, and opens a menu drawn entirely with the game's own art where you can:

- **Installed**: turn mods on and off for the active profile, search and filter, see missing dependencies,
  and update mods that have new versions.
- **Browse**: search Nexus Mods (trending, top rated, most downloaded, updated, new), open a mod's page with
  its pictures, description and files, and install a file.
- **Profiles**: create, duplicate, rename, delete and switch profiles.
- **Downloads**: follow downloads and installs, cancel them, retry failures.
- **Account**: log in to Nexus Mods, set up "Mod manager download" links, change settings.

Changes apply with **Apply & Restart**: DewDrop relinks the profile's mods and restarts the game through SMAPI.
C# mods can't be loaded or unloaded safely while the game runs, so a restart is the only reliable way.

## Works alongside Stardrop

DewDrop shares Stardrop's data folder (`%AppData%\Stardrop\Data`): the same profiles, the same settings, the
same Nexus login and the same `Selected Mods` folder SMAPI loads from. You can switch between the two freely.
DewDrop keeps itself enabled in every profile, and always links its own folder on restart (collection
profiles never list it), so the Mods button is always there.

## How it works

| Piece | What it does |
| --- | --- |
| `DewDrop.Core` | Plain .NET 6 library, no game references: profiles, settings, mod scanning, linking, Nexus API, installing, update checks, the nxm:// bridge, the relauncher. |
| `DewDrop.Mod` | The SMAPI mod: the title button (Harmony patches on `TitleMenu`), the menu and its tabs, background downloads. |

- **Enable/disable**: the active profile lists the enabled mods. On restart the `Selected Mods` folder is
  rebuilt as one directory junction (symlink on macOS/Linux) per enabled mod, and SMAPI is started with
  `--mods-path` pointing at it.
- **Restart**: a hidden PowerShell (or `sh`) process waits for the game to exit and then starts SMAPI again.
- **Nexus downloads**: Premium accounts download any file directly. Free accounts click *Get on Nexus* and then
  *Slow download* on the website, which opens an `nxm://` link. When DewDrop is registered as the nxm handler
  (Account tab, Windows only), a small script drops the link into a queue folder that the running game picks up.
- **Updates**: installed mods are checked against smapi.io, the same service SMAPI uses.
- **Title button art**: built at runtime from the game's own title buttons, so no game art ships with the mod
  and texture-replacement mods carry over.

## Nexus login

DewDrop logs in with your personal API key (Account tab, then *Get my API key*), stored the same way Stardrop
stores it. Nexus's one-click SSO login needs an application slug that Nexus issues per registered app; once
DewDrop has one, set `NexusSsoSlug` in `config.json` and the *Log in with browser* button appears.

## Building

Requires the .NET 6 SDK or later and a Stardew Valley install with SMAPI 4.

```bash
dotnet build DewDrop.sln
```

The build copies the mod into the game's `Mods\DewDrop` folder (via Pathoschild.Stardew.ModBuildConfig). Set
`GamePath` if the game isn't in the default Steam folder. If SMAPI loads from Stardrop's `Selected Mods`
folder, DewDrop needs one link there the first time; after that it keeps itself enabled.

Debug builds include a test bridge: drop command files into `%TEMP%\dewdrop-bridge\in` to open the menu,
click, type and take screenshots without touching the real mouse and keyboard (see `Framework/DebugBridge.cs`).

## Config

`Mods\DewDrop\config.json`, created on first run:

| Setting | Default | Meaning |
| --- | --- | --- |
| `ShowAdultContent` | `false` | Show mods Nexus marks as adult in Browse. |
| `EnableModsOnInstall` | `true` | Turn on newly installed mods in the active profile. |
| `CheckUpdatesOnLaunch` | `true` | Check smapi.io for updates when the game starts. |
| `NexusSsoSlug` | `""` | Nexus SSO application slug, once DewDrop has one. |

## Console commands

- `dewdrop_status`: what DewDrop sees (mods path, profile, login).
- `dewdrop_nxm <link>`: download an nxm:// link as if the website had sent it.
- `dewdrop_export_title <folder>`: save the vanilla title buttons and the built Mods button as PNGs.

## Credits and license

DewDrop is an independent project, not affiliated with Stardrop. It works with Stardrop's data formats so the two
can share profiles and a Nexus login. Thanks to [Floogen](https://github.com/Floogen) and the
[Stardrop](https://github.com/Floogen/Stardrop) contributors, and to [SMAPI](https://smapi.io/).

DewDrop is licensed under the [GNU General Public License v3.0](LICENSE).
