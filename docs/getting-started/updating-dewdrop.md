---
description: "Get the newest DewDrop with the installer or by replacing one folder. Your settings, profiles and login stay."
icon: circle-arrow-up
---

# Updating DewDrop

Updating DewDrop means swapping the files in its mod folder for newer ones. Your profiles and Nexus Mods login live outside that folder, in [DewDrop's data folder](../reference/file-locations.md#the-shared-data-folder), so updating doesn't touch them. Both ways below also keep your DewDrop settings (`config.json`).

## Is there a new version?

DewDrop checks for its own updates along with your other mods. When a new version is out, SMAPI's window mentions it as the game starts, and DewDrop's row on the **Installed** tab gets an **Update to** button that opens the [Releases page](https://github.com/friessssss/DewDrop/releases/latest). Download the new version there and follow one of the ways below.

To see which version you have, find **DewDrop** on the **Installed** tab. Its row shows the version, for example **v0.1.0**.

## Option 1: run the new installer (easiest)

1. Close the game. If you forget, the installer waits for you.
2. Paste the same line as the first time. It always gets the latest DewDrop:
   * **Windows** (PowerShell): `irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex`
   * **macOS or Linux** (Terminal): `curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash`

   Or download `DewDrop-<version>-installer.zip` from the [Releases page](https://github.com/friessssss/DewDrop/releases/latest), extract it, and double-click `Install on Windows.bat`, `Install on macOS.command` or `Install on Linux.sh`.
3. Answer the questions. At a yes/no question, pressing **Enter** picks the recommended answer.

The installer finds your current DewDrop wherever it is in your `Mods` folder, even inside a folder you use to group mods, and updates it in place. It keeps your `config.json`, and it removes the old copy only once the new one is in place, so an update that fails halfway leaves your old DewDrop working. Along the way:

* **It won't swap in an older DewDrop without asking.** If yours is newer than the one in the installer, pressing **Enter** keeps yours.
* **It checks SMAPI again** and offers to update it. That's optional unless your SMAPI is older than 4.0.
* **It checks Steam's launch option** (Windows). If Steam already starts the game in your DewDrop profile, it keeps that without asking. If Steam starts this game's SMAPI but not in your profile, it asks whether it should, the same question as on a first install, and DewDrop makes that change the next time you quit the game. See [Start in your profile](../guides/start-in-your-profile.md).
* **It offers to turn off extra copies of DewDrop** by putting a `.` in front of their folder names. SMAPI skips those folders.

More about what the installer does: [Install with the installer](installing/installer.md).

## Option 2: replace the folder by hand

1. Close the game. On Windows, the game keeps DewDrop's files locked while it runs.
2. Download `DewDrop-<version>.zip` from the [Releases page](https://github.com/friessssss/DewDrop/releases/latest) and extract it. You get a folder called `DewDrop`.
3. Open your game's `Mods` folder and find your current `DewDrop` folder. Not sure where `Mods` is? See [Install by hand](installing/manual.md).

Next, copy the new files over the old ones. The new zip doesn't include a `config.json`, so this keeps your settings.

{% tabs %}
{% tab title="Windows" %}
Drag the new `DewDrop` folder into the folder that holds your current one (usually `Mods`). When File Explorer asks, choose **Replace the files in the destination**. File Explorer merges the two folders, so your `config.json` stays.

If your current folder has a different name, such as `DewDrop 0.1.0`, open the new `DewDrop` folder and copy everything inside it into your current folder instead. Otherwise you end up with two copies.
{% endtab %}

{% tab title="macOS" %}
Don't drag the whole folder: Finder's **Replace** swaps out the entire folder, `config.json` included. Instead, open the new `DewDrop` folder, select everything inside it, and drag it into your old `DewDrop` folder. When Finder asks, tick **Apply to All** and choose **Replace**.
{% endtab %}

{% tab title="Linux" %}
Open the new `DewDrop` folder, select everything inside it, and copy it into your old `DewDrop` folder. When your file manager asks, choose to replace or overwrite the existing files.
{% endtab %}
{% endtabs %}

Finally, start the game and check the version on the **Installed** tab. That's it!

{% hint style="warning" %}
**Keep exactly one DewDrop.** If two folders in `Mods` both contain DewDrop (for example `DewDrop` and `DewDrop old`), SMAPI refuses to load either one, and the Mods button disappears. Move any backup copy out of `Mods`, or put a `.` at the start of its folder name.
{% endhint %}

## Good to know

* **Keep DewDrop in the same folder** when you update. If you do move it, the next **Apply & Restart** links the new place. If Steam starts the game in your profile, though, the next **Play** in Steam may start without the **Mods** button, because `Selected Mods` still links the old place. Start the game once from `StardewModdingAPI.exe` in your game folder (or the desktop shortcut) and quit it: DewDrop links the new place as the game closes. See [Start in your profile](../guides/start-in-your-profile.md).
* **Updating SMAPI** is a separate job. Running the DewDrop installer again offers it, or see [About SMAPI](installing/smapi.md).
* **Something went wrong?** The installer saves a log called `DewDrop-installer.log` in your temp folder. See [Troubleshooting](../help/troubleshooting.md#the-installer) and [Reporting issues](../help/reporting-issues.md).
