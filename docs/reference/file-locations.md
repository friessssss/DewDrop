---
description: "Every folder, file and log DewDrop uses on Windows, macOS, Linux and the Steam Deck, and which ones are safe to delete."
icon: folder-open
---

# Where DewDrop keeps files

DewDrop uses three main places on your computer:

1. **Its own mod folder**, inside your game's `Mods` folder.
2. **A data folder it shares with Stardrop**, for profiles, settings, your Nexus Mods login and the `Selected Mods` folder SMAPI loads your profile from.
3. **Your SMAPI log**, where everything DewDrop reports ends up. DewDrop has no log of its own, apart from a small restart log on Windows and its Steam launcher's log on Linux and the Steam Deck.

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
| The one-line installer's download | `%TEMP%\DewDrop-install.ps1`. Kept after it runs; safe to delete. |

To open a folder that starts with `%AppData%` or `%TEMP%`, press **Win+R**, paste the path and press **Enter**. For example, for the shared data folder:

```text
%AppData%\Stardrop\Data
```
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

Folders starting with `~/.config` and `~/Library` are hidden in Finder. To open one, choose **Go** > **Go to Folder...** in Finder (**Shift+Cmd+G**), paste the path and press **Return**.

To open the folder that holds the installer log, run this in Terminal:

```bash
open "$TMPDIR"
```
{% endtab %}

{% tab title="Linux" %}

| What | Where |
| --- | --- |
| Game folder, Steam | `~/.local/share/Steam/steamapps/common/Stardew Valley` (on some systems `~/.steam/steam/steamapps/common/Stardew Valley`) |
| Game folder, GOG | `~/GOG Games/Stardew Valley/game` |
| DewDrop's mod folder | `<game folder>/Mods/DewDrop` |
| Shared data folder | `~/.config/Stardrop/Data`, or `$XDG_CONFIG_HOME/Stardrop/Data` if you've set `XDG_CONFIG_HOME` |
| SMAPI log | `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` |
| Steam launcher log (after the installer's `--steam-setup`) | `~/.config/Stardrop/Data/Cache/DewDrop/steam-launch.log` |
| Installer log | `/tmp/DewDrop-installer.log`, or `$TMPDIR/DewDrop-installer.log` if you've set `TMPDIR` |

Folders starting with a dot, like `~/.config`, are hidden. In most file managers, **Ctrl+H** shows them.

Using the Flatpak or Snap version of Steam? The game runs in Steam's sandbox, so in the `~/.config` and `~/.local/share` paths above, `~` becomes `~/.var/app/com.valvesoftware.Steam` (Flatpak) or `~/snap/steam/common` (Snap). DewDrop's Steam launcher doesn't support these versions yet.
{% endtab %}

{% tab title="Steam Deck" %}

| What | Where |
| --- | --- |
| Game folder | `/home/deck/.local/share/Steam/steamapps/common/Stardew Valley` |
| DewDrop's mod folder | `<game folder>/Mods/DewDrop` |
| Shared data folder | `/home/deck/.config/Stardrop/Data` |
| DewDrop's Steam launcher | `/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh` |
| Steam launcher log | `/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.log` |
| SMAPI log | `/home/deck/.config/StardewValley/ErrorLogs/SMAPI-latest.txt` |
| Installer log | `/tmp/DewDrop-installer.log` |

Open these in Desktop Mode, with the **Dolphin** file manager. Folders starting with a dot, like `.config` and `.local`, are hidden: press **Ctrl+H** to show them. If the game is on a microSD card, its folder is on the card instead.
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
| `assets/steam-launch.sh` | DewDrop's Steam launcher, for Linux and the Steam Deck. The installer and DewDrop copy it into the data folder, and Steam runs that copy. Harmless on other systems. |
| `config.json` | Your settings. Created the first time the game starts with DewDrop. See [config.json options](config.md). |
| `debug/` | Only there if you ran [`dewdrop_export_title`](console-commands.md) without naming a folder. |

DewDrop doesn't have to be directly in `Mods`. It also works in a subfolder, such as `Mods/Tools/DewDrop`, and finds itself wherever it is. Keep only one copy, though: SMAPI won't load DewDrop if it finds two.

## The shared data folder

DewDrop deliberately uses Stardrop's data folder, so the two can share profiles, settings and your Nexus Mods login. The folder is created the first time DewDrop starts, even if you've never installed Stardrop. That's why a folder called `Stardrop` appears. See [Using DewDrop with Stardrop](../stardrop.md).

### Profiles, settings and login

These are shared with Stardrop.

| Path in `Stardrop/Data` | What it holds | Safe to delete? |
| --- | --- | --- |
| `Settings.json` | Stardrop's settings, which DewDrop shares: the path of your `Mods` folder, your active profile and your Nexus Mods login. DewDrop only changes the parts it uses. | No. You'd lose your login, your active profile and Stardrop's settings. |
| `Profiles/` | One `.json` file per profile, such as `Default.json`. | No. Deleting a file deletes that profile, for Stardrop too. |
| `Selected Mods/` | Links to the mods in your active profile, which SMAPI loads when the game starts in your profile. DewDrop rebuilds it whenever it restarts the game into your profile, and with **Start in my profile from Steam** on, when you quit the game. Stardrop uses it too. | Leave it to DewDrop and Stardrop. See the warning below. |
| `Cache/Notion.json` | The key that unlocks the Nexus Mods API key saved in `Settings.json`. | No. Your saved login would stop working in DewDrop and Stardrop. |
| `Thumbnails/Nexus/` | Pictures from Browse. There's no size limit, so it can grow over time. | Yes, while the game is closed. DewDrop downloads pictures again when it needs them. |
| `Collections/Mods/` | Mods from Stardrop's Nexus collections, if you use them. | No, if you use Stardrop collections. |

### DewDrop's own files

These are in `Stardrop/Data/Cache/DewDrop`, and only DewDrop uses them.

| File | What it holds | Safe to delete? |
| --- | --- | --- |
| `downloads/` | Downloads waiting to be installed. Emptied every time the game starts. | Yes, while the game is closed. |
| `pending-updates.json` | Updates waiting for a restart (Windows), and mods you uninstalled, waiting for the game to close. | No, while a mod shows **Update ready, restart to finish** or **Deleted when the game closes**. Without it, DewDrop forgets the update or the uninstall. |
| `mod-sources.json` | Which Nexus mod and file DewDrop installed each mod from, and which Nexus mod smapi.io says each of your mods is. Browse and a mod's page use it to show what you have installed. See [How DewDrop knows you have a mod](../guides/browse.md#how-dewdrop-knows-you-have-a-mod). | Yes. DewDrop fills it in again as you install mods and check for updates. Until then, some mods you have may not show as installed on Browse. |
| `startup.json` | What DewDrop remembers between launches: a restart in progress, if any, and the mods it has already seen, so it can tell which ones you copied in by hand. See [Mods you copy in by hand](how-it-works.md#mods-you-copy-in-by-hand). | Yes, while the game is closed. DewDrop starts a new one, and every mod installed then counts as seen. |

**Windows only:**

| File | What it holds | Safe to delete? |
| --- | --- | --- |
| `nxm/` | Download links from the website that haven't started yet. The empty folder is there on other systems too, but only Windows puts links in it. | Yes, but those downloads won't start. |
| `nxm-handler.ps1` | The small script Windows runs for website download links, once you've clicked **Use DewDrop for these links**. | No, while DewDrop handles those links. |
| `relaunch.ps1`, `relaunch.log` | The script that runs after the game closes, and its log. It restarts the game for **Apply & Restart**, finishes waiting updates and uninstalls, and changes Steam's launch option for **Start in my profile from Steam**. | Yes, while the game is closed. DewDrop writes them again when it needs them. |
| `steam-job.json`, `steam-result.json` | A change to Steam's launch option, and how it went. `steam-job.json` only exists for a moment after you quit the game. `steam-result.json` stays until the next start, when DewDrop reads it, removes it and tells you how the change went. | Yes. If the change didn't happen, DewDrop tries again the next time you quit. You only miss the message about it. |

**Linux and the Steam Deck:**

| File | What it holds | Safe to delete? |
| --- | --- | --- |
| `steam-launch.sh` | DewDrop's Steam launcher, which Stardew Valley's launch option in Steam runs. While Steam uses it, DewDrop writes it again at each start if it differs from the copy in its mod folder, so edits to it don't last. See [DewDrop's Steam launcher](how-it-works.md#dewdrops-steam-launcher). | No, while Steam's launch option uses it: Steam couldn't start the game. Run the installer again to put it back. |
| `steam-launch.log` | What the launcher did at each launch, DewDrop's restarts included. See [Logs](#logs). | Yes. |
| `start-in-profile` | There while **Start in my profile from Steam** is on. The launcher checks for it at each launch. | Yes, but it doesn't turn the setting off for long: the next **Play** loads your whole `Mods` folder, and DewDrop writes the file again as that game starts, from `config.json`. To turn the setting off, untick it on the **Account** tab. |
| `restart` | The ID of an **Apply & Restart**, from when you click it until the launcher starts the restarted game. Gone straight after. If the restart didn't come back, the next **Play** in Steam takes it and starts in your profile. | Yes. |
| `nexus-key.txt` | Steam Deck: a Nexus Mods API key you gave the installer. Only you can read it. DewDrop logs in with it at the next start, saves it like a key you typed in, and deletes the file. It also deletes it if Nexus Mods turns the key down, or when you log out or log in with another key. If Nexus Mods can't be reached, or the key couldn't be saved, the file stays and DewDrop tries again at the next start. | Yes. You then log in on the **Account** tab instead. |

### Backup copies

You may also see these:

* `Settings.json.bak-<date>-<time>`: DewDrop found `Settings.json` damaged, kept this copy and started again from defaults.
* `Profiles/Default.json.bak-<date>-<time>`: the same, for a damaged `Default` profile.
* `Cache/DewDrop/startup.json.bak`: DewDrop found `startup.json` damaged, kept this copy and started a new one. Safe to delete.
* `Cache/DewDrop/mod-sources.json.bak`: the same, for `mod-sources.json`. Safe to delete.

{% hint style="warning" %}
`Selected Mods` holds **links**, not copies. Opening it looks as if your mods are inside, but they really live in your `Mods` folder. Some tools follow links when they delete, and would delete your real mods; Windows PowerShell's `Remove-Item -Recurse` is one. Don't clean the folder out with scripts or cleanup tools. DewDrop clears and rebuilds it safely itself.
{% endhint %}

{% hint style="warning" %}
`Settings.json` and `Cache/Notion.json` together are enough to recover your Nexus Mods API key. The key is only scrambled, not truly protected. Don't share these two files, or `nexus-key.txt`, and don't attach them to bug reports.
{% endhint %}

## Inside your Mods folder

Besides the mods themselves, you might notice these folders. Their names start with a dot, which tells SMAPI to skip them.

* **`.dewdrop-staging`**: DewDrop's work folder, next to a mod's folder.
  * While DewDrop installs or updates a mod, the new copy is unpacked here first.
  * On Windows, an update waiting for a restart stays here until **Apply & Restart**, or quitting the game, finishes it. Don't delete it while a mod shows **Update ready, restart to finish**.
  * Mods you uninstall are moved in here as the game closes, into a folder whose name ends in `-removed`, and deleted the next time the game starts.
  * DewDrop removes leftovers when the game starts. See [Updates that wait for a restart](how-it-works.md#updates-that-wait-for-a-restart) and [Uninstalling a mod](how-it-works.md#uninstalling-a-mod).
* **A folder name with a dot in front and `-duplicate` at the end**, such as `.DewDrop-duplicate`: an extra copy of DewDrop that the installer turned off for you. Safe to delete.
* **`.dewdrop-installer-…`** and **`.dewdrop-old-…`**: temporary folders the installer uses while it swaps in a new DewDrop. They're removed when it finishes. If one is left behind after an interrupted install, you can delete it.

## Logs

* **SMAPI log** (`SMAPI-latest.txt`): everything DewDrop reports, including errors, goes here, with SMAPI's own messages. It's rewritten each time the game starts. To share it, upload it at [smapi.io/log](https://smapi.io/log). See [Reporting issues](../help/reporting-issues.md).
* **`relaunch.log`** (Windows only): written by the script that runs after the game closes. It's emptied each time the script runs, and only gets lines when the script finished updates or uninstalls, changed Steam's launch option, or something went wrong, such as `Uninstalled <mod ID>` or `Could not uninstall <mod ID> yet: ...`. macOS, and Linux without DewDrop's Steam launcher, have no restart log.
* **`steam-launch.log`** (Linux and the Steam Deck, with DewDrop's Steam launcher): one line each time Steam or a DewDrop restart starts the game through the launcher, saying which folder SMAPI loaded and why. Only the latest launches are kept. With no console window, this and the SMAPI log are where to look.
* **`DewDrop-installer.log`**: a record of the installer's last run, replaced each time you run it. On Windows, it holds everything the installer printed. On macOS and Linux, it holds the installer's steps, warnings and errors, but not your answers to its questions or the output of SMAPI's own installer.

Lines in `steam-launch.log` look like this:

{% code title="steam-launch.log" %}
```text
2026-10-20 18:02:11 Starting SMAPI on /home/deck/.config/Stardrop/Data/Selected Mods (Start in my profile from Steam is on)
2026-10-20 18:40:05 Starting SMAPI on /home/deck/.config/Stardrop/Data/Selected Mods (restarting into the profile)
2026-10-20 19:15:52 Starting SMAPI on the Mods folder (Start in my profile from Steam is off)
```
{% endcode %}

| The line says | What it means |
| --- | --- |
| `..., but <folder> has no DewDrop in it yet` | `Selected Mods` has no link to DewDrop, so the launcher started the whole `Mods` folder instead, to keep the **Mods** button. Usually your profile hasn't been linked yet, or Stardrop last linked a collection profile, which leaves DewDrop out. DewDrop links your profile when you quit the game with **Start in my profile from Steam** on, or when you click **Apply & Restart**. |
| `SMAPI isn't in <game folder>, so the game starts without mods` | SMAPI is missing. Run the installer again. |
| `No StardewValley in Steam's command, so it runs unchanged: ...` | Steam isn't starting the Linux version of the game, for example because it's set to run the Windows version through Proton, which DewDrop doesn't support. See [Platform support](platform-support.md#the-windows-version-through-proton-or-wine). |

## Changes outside these folders

A few changes happen outside these folders, each one because of a setting you turned on or an installer you ran.

{% tabs %}
{% tab title="Windows" %}

| Change | When | Where |
| --- | --- | --- |
| Website download links point to DewDrop | You click **Use DewDrop for these links** on the **Account** tab. | Registry: `HKEY_CURRENT_USER\Software\Classes\nxm`, for your Windows user only. DewDrop has no button to undo it; see ["Mod manager download" links](../nexus-mods/mod-manager-links.md). |
| Steam starts the game with SMAPI | You say yes when the installer offers to set the launch option. | Steam's `localconfig.vdf` in `<Steam folder>\userdata\<number>\config\`, for the Steam account that's signed in (or was signed in last). Other Steam accounts on the PC aren't changed. The installer first saves a backup next to it, named `localconfig.vdf.dewdrop-backup-<date>-<time>`. |
| Steam starts the game in your profile | You turn on **Start in my profile from Steam** on the **Account** tab, or say yes when the installer or the **Load your profile?** box offers it. The change happens when you quit the game. | The same `localconfig.vdf`. DewDrop adds `--mods-path` to Stardew Valley's launch option, and takes it out again when you turn the setting off. Before each change, it saves a backup next to the file named `localconfig.vdf.dewdrop-backup`, replacing the previous one. See [Start in your profile](../guides/start-in-your-profile.md). |
| Desktop shortcut | You say yes when the installer offers it. | `Stardew Valley (SMAPI)` on your desktop. If the installer ran as administrator, it goes on the desktop all users share (`C:\Users\Public\Desktop`). |
| Xbox app starts SMAPI | You say yes when the installer offers SMAPI's exe swap. | In the game folder, `Stardew Valley.exe` is renamed `Stardew Valley original.exe`, and a copy of SMAPI takes its name. When you run the installer again after a game update, the updated `Stardew Valley.exe` replaces the older `Stardew Valley original.exe`, and SMAPI's copy takes its name again. |
{% endtab %}

{% tab title="Linux and Steam Deck" %}
The installer sets Stardew Valley's launch option in Steam to run DewDrop's Steam launcher. It does this by itself on a Steam Deck, and with `--steam-setup` on other Linux systems. The launch option becomes this, with your own home folder in place of `/home/deck`:

```text
"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
```

Wrappers you already had, such as `gamemoderun`, are kept. If the launch option held anything other than SMAPI on its own, the installer asks first.

* **Where:** Steam's `localconfig.vdf`, in `userdata/<number>/config` in Steam's folder, for the Steam account that's signed in (or was signed in last).
* **Backup:** the installer saves `localconfig.vdf.dewdrop-backup-<date>-<time>` next to it first.
* **After that, Steam isn't changed again.** **Start in my profile from Steam** is the `start-in-profile` file instead, and **Apply & Restart** goes through the launcher.

To undo it, see [Uninstalling DewDrop](../getting-started/uninstalling.md). For the installer's side, see [Steam on Linux and the Steam Deck](installer-options.md#steam-on-linux-and-the-steam-deck).
{% endtab %}
{% endtabs %}

SMAPI's own files in your game folder, such as `StardewModdingAPI.exe` and the `smapi-internal` folder, belong to SMAPI. See [About SMAPI](../getting-started/installing/smapi.md).

Developers using a Debug build of DewDrop also get a `dewdrop-bridge` folder in their temp folder. See [The debug bridge](../contributing/debug-bridge.md).

To remove DewDrop and clean up after it, see [Uninstalling DewDrop](../getting-started/uninstalling.md).
