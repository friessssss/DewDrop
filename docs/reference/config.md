---
description: "Every setting in DewDrop's config.json, what it changes, and how to edit it safely."
icon: sliders
---

# config.json options

DewDrop keeps its settings in a small file called `config.json`. You rarely need to open it: the five options most people change are on the **Account** tab > **Settings** panel, described in [Settings](../guides/settings.md).

This page lists everything in the file, for when you want to check a value or edit it by hand. It ends with the few settings DewDrop reads from Stardrop's `Settings.json`.

## Where the file is

`config.json` sits in DewDrop's own mod folder, next to `manifest.json` and `DewDrop.dll`: `<game folder>/Mods/DewDrop/config.json`. For a Steam install in the default place, that's:

{% tabs %}
{% tab title="Windows" %}
```text
C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\DewDrop\config.json
```
{% endtab %}

{% tab title="macOS" %}
```text
~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/DewDrop/config.json
```
{% endtab %}

{% tab title="Linux" %}
```text
~/.local/share/Steam/steamapps/common/Stardew Valley/Mods/DewDrop/config.json
```

On some systems, Steam's folder is `~/.steam/steam` instead.
{% endtab %}

{% tab title="Steam Deck" %}
```text
/home/deck/.local/share/Steam/steamapps/common/Stardew Valley/Mods/DewDrop/config.json
```

Open it from Desktop Mode. If the game is on a microSD card, the game folder is on the card instead: in Steam, choose **Manage** > **Browse local files** to find it.
{% endtab %}
{% endtabs %}

If you keep DewDrop in a subfolder of `Mods`, the file is in that folder instead. [Where DewDrop keeps files](file-locations.md) lists the game folder for each store.

**Updating keeps your settings.** The download doesn't include `config.json`. SMAPI creates it with the default values the first time the game starts with DewDrop, and adds any missing setting each time the game starts after that. (The Windows installer may create it a little earlier, holding only your answer about starting in your profile.) So updating DewDrop with the installer, or by copying the new files over your existing folder, keeps your settings. If you delete the old folder first, keep a copy of `config.json` and put it back.

## The settings

| Key | Default | Option on the Account tab | In short |
| --- | --- | --- | --- |
| `StartInProfileFromSteam` | `null` | **Start in my profile from Steam** | Steam's **Play** button starts the game in your active profile. |
| `AskToLoadProfile` | `true` | **Ask to load my profile** | When the game starts with different mods than your active profile, a box on the title screen offers to restart into it. |
| `EnableModsOnInstall` | `true` | **Turn on mods when installed** | Mods DewDrop installs or updates, and mods you copy in yourself, are turned on in your active profile. |
| `ShowAdultContent` | `false` | **Show adult content in Browse** | Browse includes mods Nexus Mods marks as adult content. |
| `CheckUpdatesOnLaunch` | `true` | **Check for updates at startup** | DewDrop checks smapi.io for mod updates each time the game starts. |
| `NexusSsoSlug` | `""` (empty) | None | Reserved for a browser login that isn't available yet. Leave it empty. |

### StartInProfileFromSteam

This is **Start in my profile from Steam**. When it's on, Steam's **Play** button starts the game straight in your profile, with no extra restart. It works with the Steam version of the game on Windows, and on Linux and the Steam Deck once the installer has set up DewDrop's Steam launcher. See [Start in your profile](../guides/start-in-your-profile.md).

Unlike the other settings, it can also be `null`, with no quotes.

| Value | What it means |
| --- | --- |
| `null` | Not chosen yet. This is the default. DewDrop goes by what Steam already does, and the first time the game starts where the setting can be used, it saves what it found as `true` or `false`. |
| `true` | Steam starts the game in your profile. |
| `false` | Steam starts the game the usual way, with your whole `Mods` folder. |

How the value reaches Steam depends on your system:

{% tabs %}
{% tab title="Windows" %}
DewDrop changes Stardew Valley's launch option in Steam:

* **`true`** adds SMAPI's `--mods-path` option, pointing at `Selected Mods`. If the launch option was empty, DewDrop sets SMAPI's usual option with `--mods-path` added. A `--mods-path` that points at some other folder is replaced.
* **`false`** takes out a `--mods-path` that points at `Selected Mods` and keeps the rest, so **Play** still starts SMAPI. A `--mods-path` that points at some other folder is left alone.
* **`null`** counts as on if the launch option already points at `Selected Mods`, and DewDrop leaves the option alone.

Things to know:

* **Steam only changes when you quit the game,** because Steam has to be closed to change its launch options. A few seconds after the game closes, Steam closes, the launch option changes, and Steam opens again, minimized to the system tray. DewDrop first backs up Steam's settings file as `localconfig.vdf.dewdrop-backup`, in the same folder.
* **A hand edit works the same way.** Change the value with the game closed, then start the game. Steam's launch option changes the next time you quit.
* **Changes you make in Steam win.** If Stardew Valley's launch options changed in Steam while the game was running, DewDrop leaves them alone, and a message says so the next time you open **Mods**.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
Steam's launch option runs DewDrop's Steam launcher, and never changes after the installer sets it. The setting is a file next to the launcher instead:

* **`true`** puts a `start-in-profile` file in `Stardrop/Data/Cache/DewDrop`, which tells the launcher to start SMAPI on `Selected Mods`.
* **`false`** removes that file.
* **`null`** goes by whether the file is there. The installer writes it when you answer its question about starting in your profile.

Ticking or unticking the menu row changes the file at once, and Steam never closes.

A hand edit to `config.json` reaches the file when the game next starts. The launcher has already picked the folder for that start by then, so the change shows from the launch after it. See [How DewDrop works](how-it-works.md#dewdrops-steam-launcher).
{% endtab %}
{% endtabs %}

**Other ways it gets set:**

* Ticking **Start in my profile from Steam from now on** in the **Load your profile?** box, then clicking **Restart now**, sets it to `true`.
* The installer asks about it when it sets up Steam. On Windows, it saves your answer here, creating `config.json` if there isn't one yet. On Linux and the Steam Deck, it writes or removes the `start-in-profile` file, and changes this setting too if `config.json` already has it.
* The installer doesn't ask when you've already chosen: on Windows when the launch option already points at `Selected Mods`, and on Linux and the Steam Deck when the `start-in-profile` file is there or this setting is `false`. Its `--start-in-profile` and `--no-start-in-profile` options (`-StartInProfile` and `-NoStartInProfile` on Windows) answer for you. See [Installer command-line options](installer-options.md#starting-in-your-profile).

**Where it does nothing:** on macOS, on Linux and the Steam Deck until the installer has set up DewDrop's Steam launcher, when the game runs through Proton or Wine, for the GOG or Xbox app version, and on Windows when the launch option in Steam starts some other program than SMAPI. The menu row is grayed out there, and hovering over it says why. See [When it isn't available](../guides/start-in-your-profile.md#when-it-isnt-available).

### AskToLoadProfile

This is **Ask to load my profile**. When it's `true` (the default) and the game starts with different mods than your active profile, for example from a desktop shortcut, a **Load your profile?** box on the title screen offers to restart into your profile. When it's `false`, no box appears, and you use [**Apply & Restart**](../guides/apply-and-restart.md) to load your profile.

* **Don't ask again** in the **Load your profile?** box sets it to `false`.
* It doesn't matter when the game already starts in your profile, such as after **Apply & Restart**, or from Steam with `StartInProfileFromSteam` on.
* When the game runs through Proton or Wine, the box never appears, whatever the value.

A change takes effect the next time the game starts. See [Start in your profile](../guides/start-in-your-profile.md) for how the box works.

### EnableModsOnInstall

When this is `true`, every mod DewDrop downloads and installs is turned on in your active profile straight away, along with any required mods you already have. The mods load after your next [**Apply & Restart**](../guides/apply-and-restart.md).

* **Updates count too.** If you update a mod that was turned off, it's turned back on.
* **Mods you copy into your `Mods` folder yourself count too.** DewDrop turns them on when the game starts, or when you click **Rescan** on the **Installed** tab. Only mods DewDrop has never seen before are turned on, so a mod you turned off stays off. See [Mods you copy in by hand](how-it-works.md#mods-you-copy-in-by-hand).
* **It doesn't download missing requirements.** Only requirements you already have are turned on.
* **It leaves some profiles alone:** a collection profile, which is read-only, and a profile whose file DewDrop couldn't read.

When it's `false`, new mods stay off until you turn them on in the [Installed tab](../guides/installed.md).

### ShowAdultContent

When this is `false`, [Browse](../guides/browse.md) asks Nexus Mods to leave out mods it marks as adult content. When it's `true`, they're included.

Changing the switch in the menu takes effect without a restart: Browse loads its results again when you go back to it. It only affects what Browse lists, not mods you already have.

### CheckUpdatesOnLaunch

When this is `true`, DewDrop asks smapi.io for newer versions of your mods each time the game starts. smapi.io is the same service SMAPI uses for its own update alerts. Mods with a newer version get an **Update to** *version* button on the **Installed** tab, and an orange **Update** tag on Browse. See [Updating mods](../guides/updating-mods.md).

When it's `false`, nothing is checked at startup. You can still click **Check for updates** on the **Installed** tab whenever you like. A change takes effect the next time the game starts.

The check sends smapi.io your mods' IDs, versions and update keys, plus your SMAPI version, game version and operating system. It sends nothing about your Nexus Mods account. Mods from Stardrop collections are never checked. Mods without update keys are asked about too, but only to learn which Nexus mod each one is, so Browse can tag it **Installed**. See [Knowing what you have](how-it-works.md#knowing-what-you-have).

### NexusSsoSlug

Leave this empty.

It's for a **Log in with browser** button. Nexus Mods gives each registered app its own value for this, and DewDrop doesn't have one yet. For now, you log in with your personal API key; see [Connecting your Nexus account](../nexus-mods/connecting.md).

## An example file

This is the file with every setting at its default. Where **Start in my profile from Steam** can be used, `StartInProfileFromSteam` becomes `true` or `false` after the first start.

{% code title="config.json" %}
```json
{
  "ShowAdultContent": false,
  "EnableModsOnInstall": true,
  "CheckUpdatesOnLaunch": true,
  "StartInProfileFromSteam": null,
  "AskToLoadProfile": true,
  "NexusSsoSlug": ""
}
```
{% endcode %}

## Editing the file by hand

1. Close Stardew Valley. DewDrop reads `config.json` only when the game starts, and clicking a switch in the menu writes the whole file again, which would undo edits you made while the game was running.
2. Open `config.json` in a plain text editor, such as Notepad on Windows or TextEdit on macOS. Don't use a word processor.
3. Change a `true` to `false`, or the other way round. `StartInProfileFromSteam` can also be `null`. Write these values without quotes, and leave the quotes, commas and braces around them as they are.
4. Save the file, then start the game.

{% hint style="warning" %}
If the file isn't valid JSON, SMAPI can't read it and DewDrop doesn't start: there's no **Mods** button, and the SMAPI log shows an error that names `config.json` and says the mod "crashed on entry". A common cause is curly quotes (“like these”) from a word processor; JSON needs straight quotes.

To fix it, delete `config.json`. SMAPI makes a fresh one with the defaults the next time you start the game.
{% endhint %}

## Settings DewDrop reads from Stardrop

Some settings don't live in `config.json`. DewDrop shares Stardrop's data folder, so it reads them from Stardrop's `Settings.json` (see [Where DewDrop keeps files](file-locations.md#the-shared-data-folder)). These are the only fields DewDrop uses. It changes only the ones marked as written, and keeps every other part of the file as it is.

| Field | Used for | DewDrop writes it? |
| --- | --- | --- |
| `ModFolderPath` | The folder DewDrop searches for installed mods. | Only when it's empty, as on a first run without Stardrop: it's set to `<game folder>/Mods`. |
| `ModInstallPath` | The folder new mods are installed into. | Only when it's empty: it's set to the same folder as `ModFolderPath`. |
| `SMAPIFolderPath` | Stardrop's record of the game folder. | Only when it's empty: it's set to the game folder. |
| `LastSelectedProfileName` | Your active profile. | Yes, when you click **Use this profile** or rename your active profile. |
| `NexusDetails` | Your Nexus Mods login: user name, Premium or not, and your API key in encrypted form. | Yes, when you log in or out. |
| `IgnoreHiddenFolders` | When it's `true` (the default), folders whose names start with `.` or that are marked hidden aren't listed as mods. SMAPI skips dot-folders too. | No |
| `CollectionInstallPath` | Where Stardrop keeps the mods for its Nexus collections. | No |
| `CollectionsSkipSMAPIUpdateCheck` | When it's `true` (the default), SMAPI isn't asked to check collection mods for updates. | No |

DewDrop has no menu for the folder settings. If you use Stardrop, change them there. If you edit `Settings.json` by hand, close both the game and Stardrop first.

{% hint style="info" %}
Keep `ModInstallPath` the same as `ModFolderPath`, or inside it. DewDrop only lists mods inside `ModFolderPath`, so a mod installed somewhere else doesn't appear on the **Installed** tab and isn't turned on, even though the download says it was installed.
{% endhint %}

If `Settings.json` is damaged, DewDrop keeps a copy named `Settings.json.bak-<date>-<time>` and starts from defaults. If another program has the file locked, DewDrop leaves it alone for that session and turns off **Apply & Restart** until you restart the game.
