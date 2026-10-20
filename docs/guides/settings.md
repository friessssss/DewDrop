---
description: "DewDrop's own options on the Account tab, and where they're saved."
icon: gear
---

# Settings

DewDrop's options are on the **Account** tab, in the **Settings** panel on the right. Each option is a checkbox: click anywhere on its row to tick or untick it. Your choice is saved straight away.

The other two panels on the **Account** tab are for your Nexus Mods login and for website download links. See [Connecting your Nexus account](../nexus-mods/connecting.md) and ["Mod manager download" links](../nexus-mods/mod-manager-links.md).

<figure><img src="../.gitbook/assets/shot-account.png" alt="The Account tab of the DewDrop menu with three panels: Nexus Mods, Mod manager links, and Settings. In the Settings panel, Start in my profile from Steam, Ask to load my profile, Turn on mods when installed and Check for updates at startup are ticked, and Show adult content in Browse is not."><figcaption><p>The Settings panel is on the right of the Account tab.</p></figcaption></figure>

## The options

| Option | What it does | Default |
| --- | --- | --- |
| **Start in my profile from Steam** | Steam's **Play** button starts the game straight in your profile, so you don't need **Apply & Restart**. Steam version only, on Windows, Linux and the Steam Deck. | Off, unless Steam or the installer already set it up |
| **Ask to load my profile** | When the game starts with different mods than your profile, for example from a desktop shortcut, a **Load your profile?** box on the title screen offers to restart into it. | On |
| **Turn on mods when installed** | Mods you download through DewDrop, and new mods you copy into your `Mods` folder yourself, are turned on in your active profile. | On |
| **Show adult content in Browse** | Browse includes mods that Nexus Mods marks as adult content. | Off |
| **Check for updates at startup** | DewDrop looks for new versions of your mods each time the game starts. | On |

Point at a row to see its description. A grayed-out row can't be changed here, and pointing at it says why.

### Start in my profile from Steam

Normally, pressing **Play** in Steam loads everything in your `Mods` folder, and it takes a restart to switch into your profile. With this option on, **Play** starts the game straight in your profile. While it's on, DewDrop links your changes as you quit, so they load the next time you press **Play**. [**Apply & Restart**](apply-and-restart.md) still loads them right away.

How it works depends on your system:

{% tabs %}
{% tab title="Windows" icon="windows" %}
DewDrop adds SMAPI's `--mods-path` option to Stardew Valley's launch options in Steam. Steam has to be closed for that, so the change happens **when you quit the game**: Steam closes, the launch option changes, and Steam opens again, minimized to the system tray. This happens once each time you turn the option on or off. The next time you open **Mods**, a message says "Steam now starts the game in your profile." or "Steam no longer starts the game in your profile."

The row's description follows what's happening:

| State | Description |
| --- | --- |
| Off | "Have Steam start the game in your profile, so you don't need Apply & Restart. Steam closes and opens again once when you quit, to save it." |
| Turning on | "Turns on when you quit the game: Steam closes and opens again once to save it. After that, Steam starts the game in your profile." |
| On | "Steam starts the game in your profile. Changes you make load the next time you start it." |
| Turning off | "Turns off when you quit the game: Steam closes and opens again once to save it." |

**Before you uninstall DewDrop**, untick this row and quit the game. Otherwise SMAPI keeps loading your last profile, with no **Mods** button to change it. See [Uninstalling DewDrop](../getting-started/uninstalling.md).
{% endtab %}

{% tab title="Linux and Steam Deck" icon="linux" %}
Steam's launch option runs DewDrop's Steam launcher, which the installer sets up: by itself on a Steam Deck, and on other Linux systems when you run it with `--steam-setup`. The launcher starts SMAPI on your profile while this row is ticked. The change takes effect as soon as you click: Steam never closes, and no message appears.

The row's description reads "Have Steam start the game in your profile, so you don't need Apply & Restart." while it's off, and "Steam starts the game in your profile. Changes you make load the next time you start it." while it's on.

Until the launcher is set up, the row is grayed out. On Linux other than a Steam Deck, run the installer again in a terminal with `--steam-setup`:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --steam-setup
```

On a Steam Deck, run the installer in Desktop Mode and let it set up Steam. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md) and [Installer command-line options](../reference/installer-options.md).
{% endtab %}
{% endtabs %}

Good to know:

* **Only Steam's Play button starts in your profile.** A desktop shortcut, or starting SMAPI yourself, still loads the whole `Mods` folder. **Ask to load my profile** covers those launches.
* **At first, the row follows Steam.** The first time DewDrop checks Steam, the row takes on what Steam already does: if Steam's launch option already starts the game in your profile, for example because you added `--mods-path` yourself, the row is ticked and DewDrop leaves the option alone.
* **The installer asks about it too,** when it sets up Steam's launch option, and saves your answer here. On Linux and the Steam Deck, it doesn't ask again once you've chosen, in the installer or in the game: it keeps your choice.
* **It's grayed out** when it can't be used: on macOS; for the GOG or Xbox app version of the game; through Proton or Wine; when DewDrop can't find Steam, or can't find or read your Steam account's settings; on Windows, when the game's launch option in Steam starts a program other than SMAPI; and on Linux, until DewDrop's Steam launcher is set up, or when it couldn't be updated. Point at the row to see why.

See [Start in your profile](start-in-your-profile.md) for the whole feature.

### Ask to load my profile

When the game starts on your whole `Mods` folder, for example from a desktop shortcut, from Steam with **Start in my profile from Steam** off, or on macOS, DewDrop compares the mods that loaded with your profile. If they differ, a **Load your profile?** box on the title screen names the mods and offers to restart into your profile. Untick this row to stop the box. You can still use [**Apply & Restart**](apply-and-restart.md) whenever you like.

* If every mod in your `Mods` folder is on in your profile, the mods already match and the box doesn't appear.
* **Don't ask again** in the box unticks this row.
* A change here applies from the next time you start the game.
* When the game runs through Proton or Wine, the box never appears.

See [Start in your profile](start-in-your-profile.md) for what the box says and what each button does.

### Turn on mods when installed

When this is on, every mod DewDrop installs is ticked in your active profile, along with the installed mods it needs. Click [**Apply & Restart**](apply-and-restart.md) to load them.

* It covers updates too. Updating a mod that was off in your profile turns it on.
* It covers mods you copy into your `Mods` folder yourself. DewDrop turns them on when the game starts, or when you click **Rescan** on the [Installed](installed.md) tab. Only mods DewDrop has never seen before count, so a mod you turned off stays off.
* It doesn't change collection profiles, which are read-only, or a profile whose file couldn't be read.

When it's off, new mods stay off until you tick them on the [Installed](installed.md) tab.

### Show adult content in Browse

When you change this, [Browse](browse.md) reloads its results the next time you open it.

### Check for updates at startup

DewDrop asks smapi.io for updates in the background as the game starts. If it finds any, the **Installed** tab shows a count, such as **Installed (2 new)**. The same check tells DewDrop which Nexus mod some of your mods are, which helps Browse [mark the mods you have](browse.md#how-dewdrop-knows-you-have-a-mod).

If you turn this off, the change applies from your next launch, and you can still click **Check for updates** on the **Installed** tab whenever you like. See [Updating mods](updating-mods.md).

## The lines under the options

* **Mods folder:** the folder DewDrop installs new mods into. This is usually your game's `Mods` folder. If you use Stardrop, it's the install folder set in Stardrop.
* "Profiles are shared with Stardrop." See [Profiles](profiles.md) and [Using DewDrop with Stardrop](../stardrop.md).
* "This session started outside DewDrop's profile folder." (orange) means the game didn't start in your profile, for example because you started it from a desktop shortcut. The bottom bar says whether the mods that loaded match your profile. See [How launching works](../getting-started/launching.md).

## Where settings are saved

The five options are saved in `config.json` in DewDrop's own mod folder, usually `Stardew Valley/Mods/DewDrop/config.json`. SMAPI creates the file the first time the game starts with DewDrop. (The Windows installer may create it a little earlier, holding only your answer about starting in your profile.)

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

| Option in the menu | Key in `config.json` |
| --- | --- |
| **Start in my profile from Steam** | `StartInProfileFromSteam`: `true` or `false`, or `null` before it's set |
| **Ask to load my profile** | `AskToLoadProfile` |
| **Turn on mods when installed** | `EnableModsOnInstall` |
| **Show adult content in Browse** | `ShowAdultContent` |
| **Check for updates at startup** | `CheckUpdatesOnLaunch` |

`NexusSsoSlug` has no switch in the menu. It's reserved for a browser login that isn't available yet, so leave it empty. Every key is described in [config.json options](../reference/config.md).

{% hint style="info" %}
To edit `config.json` by hand, close the game first. DewDrop reads the file once when the game starts, and clicking an option in the menu writes the whole file again.
{% endhint %}

The installer and the manual update steps both keep your `config.json` when you update DewDrop. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

### What isn't in config.json

The path of your `Mods` folder, your active profile and your Nexus Mods login are saved in Stardrop's `Settings.json`, which DewDrop and Stardrop share. DewDrop has no option to change that path. See [Where DewDrop keeps files](../reference/file-locations.md).
