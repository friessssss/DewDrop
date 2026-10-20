---
description: "Every setting in DewDrop's config.json, what it changes, and how to edit it safely."
icon: sliders
---

# config.json options

DewDrop keeps its own settings in a small file called `config.json`. You don't need to open it: the five options most people change are on the **Account** tab > **Settings** panel, described in [Settings](../guides/settings.md).

This page lists everything in the file, for when you want to check a value or edit it by hand. At the end, it also covers the few settings DewDrop reads from Stardrop's `Settings.json`.

## Where the file is

`config.json` sits in DewDrop's own mod folder, next to `manifest.json` and `DewDrop.dll`.

{% tabs %}
{% tab title="Windows" %}
`<game folder>\Mods\DewDrop\config.json`

For a Steam install in the default place, that's `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\DewDrop\config.json`.
{% endtab %}

{% tab title="macOS" %}
`<game folder>/Mods/DewDrop/config.json`

For Steam, that's `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/DewDrop/config.json`.
{% endtab %}

{% tab title="Linux" %}
`<game folder>/Mods/DewDrop/config.json`

For Steam, that's usually `~/.local/share/Steam/steamapps/common/Stardew Valley/Mods/DewDrop/config.json`.
{% endtab %}
{% endtabs %}

If you keep DewDrop in a subfolder of `Mods`, the file is in that folder instead. [Where DewDrop keeps files](file-locations.md) lists the game folder for each store.

The download doesn't include `config.json`. SMAPI creates it with the default values the first time the game starts with DewDrop, and adds any missing setting each time the game starts after that. Because the download has no `config.json`, updating DewDrop with the installer, or by copying the new files over your existing folder, keeps your settings. If you delete the old folder first, keep a copy of `config.json` and put it back.

## The settings

| Key | Default | Option on the Account tab | In short |
| --- | --- | --- | --- |
| `StartInProfileFromSteam` | `null` | **Start in my profile from Steam** | Windows, Steam version: Steam's **Play** button starts the game in your active profile. |
| `AskToLoadProfile` | `true` | **Ask to load my profile** | When the game starts with different mods than your active profile, a box on the title screen offers to restart into it. |
| `EnableModsOnInstall` | `true` | **Turn on mods when installed** | Mods DewDrop installs or updates are turned on in your active profile. |
| `ShowAdultContent` | `false` | **Show adult content in Browse** | Browse includes mods Nexus Mods marks as adult content. |
| `CheckUpdatesOnLaunch` | `true` | **Check for updates at startup** | DewDrop checks smapi.io for mod updates each time the game starts. |
| `NexusSsoSlug` | `""` (empty) | None | Reserved for a browser login that isn't available yet. Leave it empty. |

### StartInProfileFromSteam

This is **Start in my profile from Steam**, for the Steam version of the game on Windows. When it's on, Steam's **Play** button starts the game straight in your profile, with no extra restart. See [Start in your profile](../guides/start-in-your-profile.md).

Unlike the other settings, it can also be `null`, with no quotes, which means it hasn't been set yet.

| Value | What it means |
| --- | --- |
| `null` | Not set yet. This is the default. DewDrop goes by what Steam already does: if Stardew Valley's launch option in Steam already points at `Selected Mods`, the setting counts as on and DewDrop leaves the option alone. The first time the game starts where the setting can be used, DewDrop saves what it found as `true` or `false`. |
| `true` | Steam starts the game in your profile. If Stardew Valley's launch option in Steam doesn't say so yet, DewDrop adds SMAPI's `--mods-path` option to it, pointing at `Selected Mods`. |
| `false` | Steam starts the game the usual way, with your whole `Mods` folder. If the launch option points at `Selected Mods`, DewDrop takes the `--mods-path` part out and keeps the SMAPI part. |

* **Steam only changes when you quit the game,** because Steam has to be closed to change its launch options. A few seconds after the game closes, Steam closes, the launch option changes, and Steam opens again, minimized to the system tray. Before it writes, DewDrop backs up Steam's settings file as `localconfig.vdf.dewdrop-backup`, in the same folder.
* **A hand edit works the same way.** Change the value with the game closed, then start the game. Steam's launch option changes the next time you quit it.
* **The rest of the launch option is kept.** If it was empty, DewDrop sets SMAPI's usual option with `--mods-path` added. With `true`, a `--mods-path` that points at some other folder is replaced. With `false`, it's left alone.
* **Changes you make in Steam win.** If Stardew Valley's launch options changed in Steam while the game was running, DewDrop leaves them alone, and a message says so the next time you open **Mods**.
* **It does nothing where it can't be used:** on macOS, on Linux and the Steam Deck until the installer has set up DewDrop's Steam launcher, when the game runs through Proton or Wine, for the GOG or Xbox app version, or when the launch option in Steam starts some other program than SMAPI. The menu row is grayed out there.
* **Other ways it gets set.** Ticking **Start in my profile from Steam from now on** in the **Load your profile?** box and clicking **Restart now** sets it to `true`. The Windows installer asks about it when it sets up Steam's launch option for SMAPI, and saves your answer here; see [Installer command-line options](installer-options.md).

### AskToLoadProfile

This is **Ask to load my profile**. When it's `true`, which is the default, and the game starts with different mods than your active profile, for example from a desktop shortcut, a **Load your profile?** box on the title screen offers to restart into your profile. When it's `false`, no box appears, and you use [**Apply & Restart**](../guides/apply-and-restart.md) to load your profile.

* **Don't ask again** in the **Load your profile?** box sets it to `false`.
* It doesn't matter when the game starts in your profile, such as after **Apply & Restart**, or from Steam with `StartInProfileFromSteam` on.
* When the game runs through Proton or Wine, the box never appears, whatever the value.

A change takes effect the next time the game starts. See [Start in your profile](../guides/start-in-your-profile.md) for how the box works.

### EnableModsOnInstall

When this is `true`, every mod DewDrop downloads and installs is turned on in your active profile straight away. Any required mods you already have installed are turned on with it. The mods load after your next [**Apply & Restart**](../guides/apply-and-restart.md).

* It covers updates as well. If you update a mod that was turned off, it's turned back on.
* It doesn't change a collection profile, because those are read-only. It also leaves alone a profile whose file DewDrop couldn't read.
* It doesn't download missing requirements. Only requirements you already have are turned on.
* It also covers mods you copy into your `Mods` folder yourself. DewDrop turns them on when the game starts, or when you click **Rescan** on the **Installed** tab. Only mods DewDrop has never seen before are turned on, so a mod you turned off stays off.

When it's `false`, new mods stay off until you turn them on in the [Installed tab](../guides/installed.md).

### ShowAdultContent

When this is `false`, [Browse](../guides/browse.md) asks Nexus Mods to leave out mods it marks as adult content. When it's `true`, they're included.

Changing the switch in the menu takes effect without a restart: Browse loads its results again when you go back to it. The setting only affects what Browse lists; it doesn't touch mods you already have.

### CheckUpdatesOnLaunch

When this is `true`, DewDrop asks smapi.io for newer versions of your mods each time the game starts. smapi.io is the same service SMAPI uses for its own update alerts. Mods with a newer version get an **Update to** *version* button on the **Installed** tab. See [Updating mods](../guides/updating-mods.md).

When it's `false`, nothing is checked at startup. You can still click **Check for updates** on the **Installed** tab whenever you like. Changing this setting takes effect the next time the game starts.

The check sends smapi.io your mods' IDs, versions and update keys, plus your SMAPI version, game version and operating system. It doesn't send anything about your Nexus Mods account. Mods from Stardrop collections are never checked.

### NexusSsoSlug

Leave this empty.

It's for a **Log in with browser** button. Nexus Mods gives each registered app its own value for this, and DewDrop doesn't have one yet. For now, you log in with your personal API key; see [Connecting your Nexus account](../nexus-mods/connecting.md).

## An example file

This is the file with every setting at its default. On Windows with the Steam version, `StartInProfileFromSteam` becomes `true` or `false` after the first start.

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

1. Close Stardew Valley. DewDrop reads `config.json` only when the game starts. Clicking a switch in the menu writes the whole file again, which would undo edits you made while the game was running.
2. Open `config.json` in a plain text editor, such as Notepad on Windows or TextEdit on macOS.
3. Change a `true` to `false`, or the other way round. `StartInProfileFromSteam` can also be `null`. Write these values without quotes, and leave the quotes, commas and braces around them as they are.
4. Save the file, then start the game.

{% hint style="warning" %}
If the file isn't valid JSON, SMAPI can't read it, and DewDrop doesn't start. You won't see the **Mods** button, and the SMAPI console shows an error that names `config.json` and says the mod "crashed on entry". A common cause is curly quotes (“like these”) from a word processor; JSON needs straight quotes.

To fix it, delete `config.json`. SMAPI makes a fresh one with the defaults the next time you start the game.
{% endhint %}

## Settings DewDrop reads from Stardrop

Some settings don't live in `config.json`. DewDrop shares Stardrop's data folder, so it reads them from Stardrop's `Settings.json` (see [Where DewDrop keeps files](file-locations.md) for its location). These are the only fields DewDrop uses. It changes only the ones marked as written and keeps every other part of the file as it is.

| Field | Used for | DewDrop writes it? |
| --- | --- | --- |
| `ModFolderPath` | The folder DewDrop searches for installed mods. | Only when it's empty, as on a first run without Stardrop: it's set to `<game folder>/Mods`. |
| `ModInstallPath` | The folder new mods are installed into. | Only when it's empty, as on a first run without Stardrop: it's set to the same folder as `ModFolderPath`. |
| `SMAPIFolderPath` | Stardrop's record of the game folder. | Only when it's empty, as on a first run without Stardrop: it's set to the game folder. |
| `LastSelectedProfileName` | Your active profile. | Yes, when you click **Use this profile** or rename your active profile. |
| `NexusDetails` | Your Nexus Mods login: user name, Premium or not, and your API key in encrypted form. | Yes, when you log in or out. |
| `IgnoreHiddenFolders` | When it's `true` (the default), folders whose names start with `.` or that are marked hidden aren't listed as mods. SMAPI skips dot-folders too. | No |
| `CollectionInstallPath` | Where Stardrop keeps the mods for its Nexus collections. | No |
| `CollectionsSkipSMAPIUpdateCheck` | When it's `true` (the default), SMAPI isn't asked to check collection mods for updates. | No |

DewDrop has no menu for the folder settings. If you use Stardrop, change them in Stardrop. If you edit `Settings.json` by hand, close both the game and Stardrop first.

{% hint style="info" %}
Keep `ModInstallPath` the same as `ModFolderPath`, or inside it. DewDrop only lists mods inside `ModFolderPath`, so a mod installed somewhere else doesn't appear on the **Installed** tab and isn't turned on, even though the download says it was installed.
{% endhint %}

If `Settings.json` is damaged, DewDrop keeps a copy named `Settings.json.bak-<date>-<time>` and starts from defaults. If another program has the file locked, DewDrop leaves it alone for that session and turns off **Apply & Restart** until you restart the game.
