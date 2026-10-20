---
description: "Remove DewDrop in a few steps. Cleaning up the leftovers afterwards is optional."
icon: trash-can
---

# Uninstalling DewDrop

Removing DewDrop mostly means deleting one folder. If Steam starts the game in your profile, there's one step before that. Your other mods, SMAPI and your saves stay as they are. Mods you installed through DewDrop are ordinary mod folders, so they stay too.

{% hint style="warning" %}
**Mods you turned off will load again.** Turning a mod off doesn't move it out of your `Mods` folder. It only changes your profile. Once DewDrop is gone, a normal launch loads everything in `Mods`, including mods you had turned off.

To see which mods those are, open the **Installed** tab before you uninstall and click the filter button until it says **Show: Disabled**. To keep a mod off without DewDrop, move its folder out of `Mods`, or put a `.` at the start of its folder name (SMAPI skips those folders).
{% endhint %}

## Remove DewDrop

1. **If Steam starts the game in your profile, turn that off first** (Windows). On the **Account** tab > **Settings** panel, untick **Start in my profile from Steam**, then quit the game. Steam closes and opens again once, and DewDrop takes the `--mods-path` part out of Stardew Valley's launch options. If you skip this, SMAPI keeps loading your last profile, with no **Mods** button to change it.

   Already removed DewDrop, or added `--mods-path` yourself? In Steam, right-click **Stardew Valley**, choose **Properties** and delete the `--mods-path "..."` part from **Launch Options**, keeping the rest. Do the same for a desktop shortcut you added it to. See [Start in your profile](../guides/start-in-your-profile.md).
2. Make sure the game is closed.
3. Open your game's `Mods` folder and delete the `DewDrop` folder (or wherever you put it). Its `config.json` goes with it.
4. If the installer turned off extra copies of DewDrop, you can also delete any folders named like `.DewDrop-duplicate`.

That's it! The next time you start the game, SMAPI loads your `Mods` folder without DewDrop.

## Optional: clean up leftovers

Everything below is harmless to leave behind. Open only what you want to clean up.

<details>

<summary>Hidden folders in your Mods folder</summary>

When DewDrop installs or updates a mod, it unpacks the new version into a hidden `.dewdrop-staging` folder next to that mod first. Some can stay behind, in `Mods` or in a folder you use to group mods. SMAPI ignores them.

To see them, show hidden files: in Windows 11 File Explorer choose **View** > **Show** > **Hidden items** (on Windows 10, tick **View** > **Hidden items**), in Finder press **Cmd+Shift+.**, and in most Linux file managers press **Ctrl+H**. Any folder whose name starts with `.dewdrop-` is safe to delete once DewDrop is gone.

If a mod said **Update ready, restart to finish** when you removed DewDrop, its new version is in `.dewdrop-staging` and won't go in now. That mod stays on its old version, so update it another way if you need to.

If a mod said **Deleted when the game closes**, DewDrop moved it into `.dewdrop-staging` when you quit, in a folder whose name ends in `-removed`. DewDrop would have deleted it at the next start, so it stays there now. SMAPI skips it, and you can delete it. If the mod's folder is still in `Mods`, delete that yourself.

</details>

<details>

<summary>DewDrop's data folder (profiles and Nexus Mods login)</summary>

DewDrop keeps its data in Stardrop's folder: `%AppData%\Stardrop\Data` on Windows. For macOS and Linux, see [Where DewDrop keeps files](../reference/file-locations.md).

**On Linux and the Steam Deck,** if the installer set up Steam (it does on a Deck), Steam's launch option runs DewDrop's launcher from this folder. First delete the launcher's path (`"…/steam-launch.sh"`) from Stardew Valley's **Launch Options** in Steam, and keep anything else there, such as `gamemoderun`. Otherwise **Play** can't start the game once the folder is gone. See [DewDrop on the Steam Deck](steam-deck.md#what-the-installer-changes).

**If you don't use Stardrop,** you can delete the whole `Stardrop` folder. On Windows, paste `%AppData%` into the File Explorer address bar to find it. Deleting it in File Explorer, Finder or a Linux file manager is safe. Don't use PowerShell's `Remove-Item -Recurse`: the `Selected Mods` folder inside holds links to your real mod folders, and Windows PowerShell can follow them and delete your actual mods.

Your DewDrop profiles and saved Nexus Mods login go with it. If you might try Stardrop later, keep the folder instead: Stardrop reads the same profiles.

**If you use Stardrop,** keep the data folder. Stardrop uses the same profiles, settings, Nexus Mods login and `Selected Mods` folder.

* You can delete `Cache\DewDrop` inside it: DewDrop's download queue, restart script and log, `startup.json`, `mod-sources.json` and link handler script. If DewDrop handles Nexus download links, sort that out first (next section).
* Your profiles still list DewDrop (`friessssss.DewDrop`) as turned on. That entry does nothing once the DewDrop folder is gone.
* Don't log out of Nexus Mods in DewDrop before you remove it. Stardrop uses the same saved key, so it would be logged out too.

</details>

<details>

<summary>The "Mod manager download" link handler (Windows)</summary>

Skip this unless you clicked **Use DewDrop for these links** on the **Account** tab. Uninstalling doesn't undo it, so once DewDrop is gone, the **Mod manager download** and **Slow download** buttons on the Nexus Mods website seem to do nothing. Pick whichever fits you:

* **You use Stardrop:** start Stardrop while you're logged in to Nexus Mods. If Stardrop handled these links before, it takes them back by itself. If it never did, it asks whether it should.
* **You use Vortex or another mod manager:** turn on its own setting for handling Nexus Mods links.
* **You don't use another mod manager:** remove DewDrop's handler with the short PowerShell script in ["Mod manager download" links](../nexus-mods/mod-manager-links.md#undo-it). It checks that the handler is DewDrop's before it changes anything.

</details>

<details>

<summary>Steam, the desktop shortcut, the Xbox app and SMAPI</summary>

These belong to SMAPI's setup, not DewDrop. Keep them if you still play with mods.

* **Steam's launch option** (`"...\StardewModdingAPI.exe" %command%`). If the installer set it, it changed only the Steam account you were signed in to, and first saved a backup of that account's Steam settings as `localconfig.vdf.dewdrop-backup-<date>-<time>`, in your Steam folder under `userdata\<number>\config`. If you used **Start in my profile from Steam**, the same folder also has `localconfig.vdf.dewdrop-backup`, the copy DewDrop saved before its last change. You can delete the backups. Don't copy one back over Steam's current file: it's an old copy of all your Steam settings.
* **The Stardew Valley (SMAPI) desktop shortcut** starts SMAPI. Keep it to play with mods, or delete it.
* **Xbox app:** if the installer did SMAPI's exe swap, `Stardew Valley.exe` is a copy of SMAPI and the game's own exe is `Stardew Valley original.exe`. If you ran the installer again after a game update, that's the updated game's exe.
* **The installer log**, `DewDrop-installer.log`, is in your temp folder (`%TEMP%` on Windows, `$TMPDIR` or `/tmp` on macOS and Linux). You can delete it.
* **SMAPI itself:** to remove it too, first empty Steam's **Launch Options** box, so **Play** starts the plain game. On the Xbox app, if the game still opens with a SMAPI window, delete `Stardew Valley.exe` and rename `Stardew Valley original.exe` back to `Stardew Valley.exe`. Then run SMAPI's own installer and choose to uninstall. See [About SMAPI](installing/smapi.md).

</details>

## Changed your mind?

You can install DewDrop again at any time. If you kept the data folder, your profiles and Nexus Mods login are still there. See [Installing DewDrop](installing/README.md).
