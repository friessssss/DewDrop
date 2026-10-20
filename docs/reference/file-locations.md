---
description: "Every folder and file DewDrop uses on Windows, macOS and Linux, and which ones are safe to delete."
icon: folder-open
---

# Where DewDrop keeps files

DewDrop uses three main places on your computer:

1. **Its own mod folder**, inside your game's `Mods` folder.
2. **A data folder it shares with Stardrop**, for profiles, settings, your Nexus Mods login and the `Selected Mods` folder SMAPI loads your profile from.
3. **Your SMAPI log**, where everything DewDrop reports ends up. Apart from a small restart log on Windows, and its Steam launcher's log on Linux and the Steam Deck, DewDrop has no log file of its own.

## Quick list for your system

{% tabs %}
{% tab title="Windows" %}

| What | Where |
| --- | --- |
| Game folder, Steam (default library) | `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley` |
| Game folder, GOG Galaxy | `C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley` |
| Game folder, Xbox app | `C:\XboxGames\Stardew Valley\Content` |
| DewDrop's mod folder | `<game folder>\Mods\DewDrop` |
| Shared data folder | `%AppData%\Stardrop\Data`, which is `C:\Users\<you>\AppData\Roaming\Stardrop\Data` |
| SMAPI log | `%AppData%\StardewValley\ErrorLogs\SMAPI-latest.txt` |
| Restart log | `%AppData%\Stardrop\Data\Cache\DewDrop\relaunch.log` |
| Installer log | `%TEMP%\DewDrop-installer.log` |

To open a folder that starts with `%AppData%` or `%TEMP%`, press **Win+R**, paste the path and press **Enter**.
{% endtab %}

{% tab title="macOS" %}

| What | Where |
| --- | --- |
| Game folder, Steam | `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS` |
| Game folder, GOG | `/Applications/Stardew Valley.app/Contents/MacOS` |
| DewDrop's mod folder | `<game folder>/Mods/DewDrop` |
| Shared data folder | `~/.config/Stardrop/Data` |
| SMAPI log | `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` |
| Installer log | `$TMPDIR/DewDrop-installer.log` |

Folders starting with `~/.config` and `~/Library` are hidden in Finder. To open one, choose **Go** > **Go to Folder...** in Finder (**Shift+Cmd+G**), paste the path and press **Return**. To open the folder that holds the installer log, type `open "$TMPDIR"` in Terminal.
{% endtab %}

{% tab title="Linux" %}

| What | Where |
| --- | --- |
| Game folder, Steam | `~/.local/share/Steam/steamapps/common/Stardew Valley` (on some systems `~/.steam/steam/steamapps/common/Stardew Valley`) |
| Game folder, GOG | `~/GOG Games/Stardew Valley/game` |
| DewDrop's mod folder | `<game folder>/Mods/DewDrop` |
| Shared data folder | `~/.config/Stardrop/Data`, or `$XDG_CONFIG_HOME/Stardrop/Data` if you've set `XDG_CONFIG_HOME` |
| SMAPI log | `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` |
| Steam launcher log (Steam Deck, or Linux after `--steam-setup`) | `~/.config/Stardrop/Data/Cache/DewDrop/steam-launch.log` |
| Installer log | `/tmp/DewDrop-installer.log`, or `$TMPDIR/DewDrop-installer.log` if you've set `TMPDIR` |

Folders starting with a dot, like `~/.config`, are hidden. In most file managers, **Ctrl+H** shows them.

Using the Flatpak or Snap version of Steam? The game runs in Steam's sandbox, so in the `~/.config` and `~/.local/share` paths above, `~` becomes `~/.var/app/com.valvesoftware.Steam` (Flatpak) or `~/snap/steam/common` (Snap).
{% endtab %}
{% endtabs %}

Can't find your game folder? In Steam, right-click Stardew Valley and choose **Manage** > **Browse local files**.

## DewDrop's mod folder

This is the folder you installed, normally `Mods/DewDrop` in your game folder.

| File | What it is |
| --- | --- |
| `manifest.json` | Tells SMAPI what the mod is: its name, version and unique ID, `friessssss.DewDrop`. |
| `DewDrop.dll`, `DewDrop.Core.dll` | DewDrop itself. |
| `SharpCompress.dll`, `ZstdSharp.dll` | Libraries DewDrop uses to open `.zip`, `.7z` and `.rar` downloads. |
| `DewDrop.Core.pdb` | Debugging information. Harmless. |
| `config.json` | Your settings. Created the first time the game starts with DewDrop. See [config.json options](config.md). |
| `debug/` | Only there if you ran [`dewdrop_export_title`](console-commands.md) without naming a folder. |

DewDrop doesn't have to be directly in `Mods`. It also works in a subfolder, such as `Mods/Tools/DewDrop`, and finds itself wherever it is. Keep only one copy, though: SMAPI won't load DewDrop if it finds two.

## The shared data folder

DewDrop deliberately uses Stardrop's data folder, so the two can share profiles, settings and your Nexus Mods login. The folder is created the first time DewDrop starts, even if you've never installed Stardrop. That's why a folder called `Stardrop` appears. See [Using DewDrop with Stardrop](../stardrop.md).

These are the files DewDrop uses inside `Stardrop/Data`:

| Path | What it holds | Safe to delete? |
| --- | --- | --- |
| `Settings.json` | Stardrop's settings, which DewDrop shares: the path of your `Mods` folder, your active profile and your Nexus Mods login. DewDrop only changes the parts it uses. | No. You'd lose your login, your active profile and Stardrop's settings. |
| `Profiles/` | One `.json` file per profile, such as `Default.json`. | No. Deleting a file deletes that profile, for Stardrop too. |
| `Selected Mods/` | Links to the mods in your active profile. DewDrop rebuilds it each time it restarts the game into your profile, such as when you click **Apply & Restart**. With **Start in my profile from Steam** on (on Windows, or on Linux and the Steam Deck with DewDrop's Steam launcher), it also brings the links up to date when you quit the game. Stardrop uses it too. | Leave it to DewDrop and Stardrop. See the warning below. |
| `Cache/Notion.json` | The key that unlocks the Nexus Mods API key saved in `Settings.json`. | No. Your saved login would stop working in DewDrop and Stardrop. |
| `Thumbnails/Nexus/` | Pictures from Browse. There's no size limit, so it can grow over time. | Yes, while the game is closed. DewDrop downloads pictures again when it needs them. |
| `Collections/Mods/` | Mods from Stardrop's Nexus collections, if you use them. | No, if you use Stardrop collections. |
| `Cache/DewDrop/downloads/` | Downloads waiting to be installed. Emptied every time the game starts. | Yes, while the game is closed. |
| `Cache/DewDrop/nxm/` | Download links from the website that haven't started yet (Windows). | Yes, but those downloads won't start. |
| `Cache/DewDrop/nxm-handler.ps1` | The small script Windows runs for website download links, once you've clicked **Use DewDrop for these links**. | No, while DewDrop handles those links. |
| `Cache/DewDrop/pending-updates.json` | Updates waiting for a restart, or on Windows for you to quit the game. | No, while an update shows **Update ready, restart to finish**. |
| `Cache/DewDrop/relaunch.ps1`, `relaunch.log` | The script that runs after the game closes, and its log (Windows). It restarts the game for **Apply & Restart**, finishes waiting updates, and changes Steam's launch option for **Start in my profile from Steam**. | Yes, while the game is closed. DewDrop writes them again when it needs them. |
| `Cache/DewDrop/steam-job.json`, `steam-result.json` | A change to Steam's launch option, and how it went (Windows). `steam-job.json` only exists for a moment after you quit the game. `steam-result.json` stays until the next start, when DewDrop reads it, removes it and tells you how the change went. | Yes. If the change didn't happen, DewDrop tries again the next time you quit. You only miss the message about it. |
| `Cache/DewDrop/steam-launch.sh`, `steam-launch.log` | Linux and Steam Deck: DewDrop's Steam launcher, which Stardew Valley's launch option in Steam runs, and its record of what it did at each launch, DewDrop's restarts included. DewDrop writes the launcher again when it changes. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md). | Not the launcher while Steam's launch option uses it: Steam couldn't start the game. Run the installer again to put it back. The log is safe to delete. |
| `Cache/DewDrop/start-in-profile`, `restart` | Linux and Steam Deck: `start-in-profile` is there while **Start in my profile from Steam** is on. `restart` holds the ID of an **Apply & Restart** until the launcher starts the restarted game, and is gone straight after. | `start-in-profile`: deleting it turns the setting off until DewDrop writes it again. `restart`: yes. |
| `Cache/DewDrop/nexus-key.txt` | Steam Deck: a Nexus Mods API key you gave the installer. DewDrop logs in with it at the next start and saves it like a key you typed in. It deletes this file once the key is saved, or if Nexus Mods turns the key down. If Nexus Mods can't be reached, or the key couldn't be saved, the file stays and DewDrop tries again at the next start. Logging out, or logging in with another key, deletes it too. Only you can read it. | Yes. You then log in on the **Account** tab instead. |
| `Cache/DewDrop/startup.json` | What DewDrop remembers between launches: a restart that's in progress, if any, and the mods DewDrop has already seen, so it can tell which ones you copied in by hand. See [How DewDrop works](how-it-works.md#mods-you-copy-in-by-hand). | Yes, while the game is closed. DewDrop starts a new one, and every mod installed then counts as seen. |

You may also see backup copies:

* `Settings.json.bak-<date>-<time>`: DewDrop found `Settings.json` damaged, kept this copy and started again from defaults.
* `Profiles/Default.json.bak-<date>-<time>`: the same, for a damaged `Default` profile.
* `Cache/DewDrop/startup.json.bak`: DewDrop found `startup.json` damaged, kept this copy and started a new one. Safe to delete.

{% hint style="warning" %}
`Selected Mods` holds **links**, not copies. Opening it looks as if your mods are inside, but they really live in your `Mods` folder. Some tools follow links when they delete, and would delete your real mods. For example, Windows PowerShell's `Remove-Item -Recurse` can do this. Don't clean the folder out with scripts or cleanup tools; DewDrop clears and rebuilds it safely itself.
{% endhint %}

{% hint style="warning" %}
`Settings.json` and `Cache/Notion.json` together are enough to recover your Nexus Mods API key. The key is only scrambled, not truly protected. Don't share these two files, and don't attach them to bug reports.
{% endhint %}

## Inside your Mods folder

Besides the mods themselves, you might notice these folders. Their names start with a dot, which tells SMAPI to skip them.

* **`.dewdrop-staging`**: holds a mod's new copy, next to the mod's folder, while DewDrop installs or updates it. On Windows, an update waiting for a restart stays here until **Apply & Restart**, or quitting the game, finishes it, so don't delete it while a mod shows **Update ready, restart to finish**. DewDrop removes leftovers when the game starts. See [How DewDrop works](how-it-works.md#updates-that-wait-for-a-restart).
* **A folder name with a dot in front and `-duplicate` at the end**, such as `.DewDrop-duplicate`: an extra copy of DewDrop that the installer turned off for you. Safe to delete.
* **`.dewdrop-installer-…`** and **`.dewdrop-old-…`**: temporary folders the installer uses while it swaps in a new DewDrop. They're removed when it finishes. If one is left behind after an interrupted install, you can delete it.

## Logs

* **SMAPI log** (`SMAPI-latest.txt`): everything DewDrop reports, including errors, goes here, together with SMAPI's own messages. It's rewritten each time the game starts. To share it, upload it at [smapi.io/log](https://smapi.io/log). See [Reporting issues](../help/reporting-issues.md).
* **`relaunch.log`** (Windows only): written by the script that runs after the game closes, for **Apply & Restart**, waiting updates and **Start in my profile from Steam**. It's emptied each time that script runs, and only gets lines when it finished updates, changed Steam's launch option or something went wrong. macOS, and Linux without DewDrop's Steam launcher, have no restart log.
* **`steam-launch.log`** (Linux and the Steam Deck, with DewDrop's Steam launcher): a line each time Steam or a DewDrop restart starts the game through the launcher, saying which folder SMAPI loaded and why. Only the latest launches are kept. Since the launcher starts SMAPI without a console window, this and the SMAPI log are where to look.
* **`DewDrop-installer.log`**: a record of the installer's last run, replaced each time you run it. On Windows, it holds everything the installer printed. On macOS and Linux, it holds the installer's steps, warnings and errors, but not your answers to its questions or the output of SMAPI's own installer.

## Other changes on Windows

Some actions change things outside these folders. Each one only happens when you choose it.

| Change | When | Where |
| --- | --- | --- |
| Website download links point to DewDrop | You click **Use DewDrop for these links** on the **Account** tab. | Registry: `HKEY_CURRENT_USER\Software\Classes\nxm`, for your Windows user only. DewDrop has no button to undo it; see ["Mod manager download" links](../nexus-mods/mod-manager-links.md). |
| Steam starts the game with SMAPI | You say yes when the installer offers to set the launch option. | Steam's `localconfig.vdf` in `<Steam folder>\userdata\<number>\config\`, for the Steam account that's signed in (or was signed in last). Other Steam accounts on the PC aren't changed. The installer saves a backup next to the file first, named `localconfig.vdf.dewdrop-backup-<date>-<time>`. |
| Steam starts the game in your profile | You turn on **Start in my profile from Steam** on the **Account** tab, or say yes when the installer or the **Load your profile?** box offers it. The change happens when you quit the game. | The same `localconfig.vdf`, for the same Steam account. DewDrop adds `--mods-path` to Stardew Valley's launch option, and takes it out again when you turn the setting off. Before each change, it saves a backup next to the file named `localconfig.vdf.dewdrop-backup`, replacing the previous one. See [Start in your profile](../guides/start-in-your-profile.md). |
| Desktop shortcut | You say yes when the installer offers it. | `Stardew Valley (SMAPI)` on your desktop. If the installer ran as administrator, it goes on the desktop all users share (`C:\Users\Public\Desktop`). |
| Xbox app starts SMAPI | You say yes when the installer offers SMAPI's exe swap. | In the game folder, `Stardew Valley.exe` is renamed `Stardew Valley original.exe`, and a copy of SMAPI takes its name. When you run the installer again after a game update, the updated `Stardew Valley.exe` replaces the older `Stardew Valley original.exe`, and SMAPI's copy takes its name again. |

On Linux and the Steam Deck, the installer changes Stardew Valley's launch option in Steam's `localconfig.vdf` (in `userdata/<number>/config` in Steam's folder, for the Steam account that's signed in) to run DewDrop's Steam launcher, and saves a backup named `localconfig.vdf.dewdrop-backup-<date>-<time>` next to it first. **Start in my profile from Steam** doesn't change Steam there: it's the `start-in-profile` file. See [Steam on Linux and the Steam Deck](installer-options.md#steam-on-linux-and-the-steam-deck).

SMAPI's own files in your game folder, such as `StardewModdingAPI.exe` and the `smapi-internal` folder, belong to SMAPI. See [About SMAPI](../getting-started/installing/smapi.md).

Developers using a Debug build of DewDrop also get a `dewdrop-bridge` folder in their temp folder. See [The debug bridge](../contributing/debug-bridge.md).

To remove DewDrop and clean up after it, see [Uninstalling DewDrop](../getting-started/uninstalling.md).
