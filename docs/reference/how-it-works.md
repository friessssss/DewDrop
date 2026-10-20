---
description: "What happens behind the Mods button: profiles, linked mods, Apply & Restart, starting in your profile, DewDrop's Steam launcher, updates, uninstalls and the button art."
icon: gears
---

# How DewDrop works

You don't need this page to use DewDrop. It explains what goes on behind the scenes, which helps when something surprises you, or when you're curious.

<figure><img src="../.gitbook/assets/diagram-how-it-works.png" alt="Diagram in four steps: your profile lists the mods you turned on; Apply &#x26; Restart puts one link per mod into the Selected Mods folder; SMAPI starts with --mods-path pointing at that folder; the game loads only those mods."><figcaption><p>From profile to game: your profile becomes links in Selected Mods, and SMAPI loads only that folder.</p></figcaption></figure>

## The short version

1. Your mods stay in your `Mods` folder. DewDrop only moves one out when you uninstall it.
2. A profile is a list of the mods you want turned on.
3. **Apply & Restart** puts one link per turned-on mod into a folder called `Selected Mods`.
4. DewDrop closes the game and starts SMAPI again, telling it to load mods from `Selected Mods` instead of `Mods`.
5. SMAPI loads only the mods in your profile.

Why restart at all? Mods that contain code can't be loaded or unloaded safely while the game is running. A fresh start is the only reliable way to change which mods are loaded.

**Start in my profile from Steam** saves you that restart when you start the game: Steam's **Play** button starts SMAPI on `Selected Mods` from the beginning. It works with the Steam version on Windows, and on Linux and the Steam Deck with [DewDrop's Steam launcher](#dewdrops-steam-launcher). See [Starting in your profile](#starting-in-your-profile).

## Your mods stay where they are

DewDrop finds your mods by looking through your mods folder, which is your game's `Mods` folder unless Stardrop is set to use another one. Any folder with a `manifest.json` file is a mod. Folders without one are searched too, so grouping folders such as `Mods/Farming/` work. Folders whose names start with a dot are skipped, the same way SMAPI skips them.

Turning a mod off only changes your profile. The mod's folder isn't moved, renamed or deleted. That's why a normal launch, which loads the whole `Mods` folder, still loads mods you turned off. [Starting in your profile](#starting-in-your-profile) explains how DewDrop gets you into your profile anyway.

DewDrop reads the folder when the game starts, after it installs something, and when you click **Rescan** on the **Installed** tab.

Two folders holding the same mod (the same unique ID) are a problem for SMAPI: when it loads the whole `Mods` folder, it loads neither. The **Installed** tab marks both **Installed twice**.

### Mods you copy in by hand

DewDrop keeps a list of the mods it has already seen, in `startup.json`. A mod in your mods folder that isn't on that list is new: you copied it in yourself.

When the game starts, and when you click **Rescan**, DewDrop turns new mods on in your active profile, the same way it turns on mods it downloads. It does this only while **Turn on mods when installed** (**Account** tab > **Settings** panel) is on. This way, a mod you drag into `Mods` still loads when the game starts in your profile.

* The next time you open the **Mods** menu after a start, a message names them: "Turned on *names*, new in your Mods folder."
* If they didn't load in this session, for example because the game started in your profile, which had no link for them yet, their rows on the **Installed** tab say **On after restart**.
* DewDrop goes by each mod's unique ID, not its folder. A mod you move or extract again keeps whatever you chose for it.

Only mods DewDrop has never seen count as new. The first time DewDrop runs, every mod already installed counts as seen. So does every mod that's on in any profile, every mod you've turned on or off, and every mod installed through DewDrop. If you delete `startup.json` while the game is closed, every mod installed at the next start counts as seen.

Mods from Stardrop collections are never turned on this way, and nothing is turned on while your active profile is a Stardrop collection profile, which DewDrop can't change.

### Uninstalling a mod

The trash can on the **Installed** tab, and **Uninstall** on a mod's page, don't delete anything straight away. The game may have the mod loaded, and mods read their files while you play, such as Content Patcher packs when a save loads. So DewDrop:

1. Turns the mod off in your active profile, unless another loose copy of it stays installed or the profile is read-only. Other profiles aren't changed.
2. Lists the mod's folder in `pending-updates.json`, in `Stardrop/Data/Cache/DewDrop`, and throws away any update waiting for that folder. From then on, DewDrop treats the mod as gone, apart from its row on the **Installed** tab, which offers **Undo**.
3. As the game closes (on every system, after **Apply & Restart** too), removes the mod's link from `Selected Mods` and moves its folder into the hidden `.dewdrop-staging` folder beside it, under a new name that ends in `-removed`. A single rename can't leave half a mod behind.
4. Deletes it the next time the game starts.

Before it moves or deletes anything, DewDrop checks that the folder still holds the mod you uninstalled. If the folder is gone, or now holds another mod, it's left alone.

On Windows, the move fails while a file in the folder is still open. Then the restart script, `relaunch.ps1`, tries again once the game has closed, and DewDrop tries again at each start, except in a session that loaded the mod again. After three starts it gives up, and the menu says "Couldn't delete '*folder*' (uninstalled): a file stayed open. Delete it yourself."

**Undo** takes the folder off the list, and turns the mod back on in the profile that uninstalling turned it off in. DewDrop only remembers that profile until the game closes. Installing the same mod again into that folder takes it off the list too.

[Uninstall a mod](../guides/installed.md#uninstall-a-mod) covers the buttons and messages.

## Profiles

A profile is a small `.json` file in the `Profiles` folder of the data folder DewDrop shares with Stardrop. It lists the unique IDs of the mods that are turned on. The format is Stardrop's, so both tools read and write the same files. See [Profiles](../guides/profiles.md) and [Where DewDrop keeps files](file-locations.md).

* **The Default profile.** If there's no `Default` profile yet, DewDrop creates one from the mods SMAPI loaded in that session. That way, your first **Apply & Restart** keeps the mods you already play with. See [Your first launch](../getting-started/first-launch.md).
* **DewDrop keeps itself on.** Each time the game starts, DewDrop adds itself to every profile it's allowed to edit. It also always links its own folder whenever it links your profile, wherever it's installed, so the **Mods** button can't disappear. This even covers Stardrop's collection profiles, which never list DewDrop.

## The Selected Mods folder

`Selected Mods` is a folder in the shared data folder. Stardrop uses the same folder in the same way. It doesn't hold copies of your mods, only **links**: entries that point to the real mod folders. To SMAPI, each link looks like an ordinary mod folder.

DewDrop rebuilds the folder when you click **Apply & Restart**, or **Restart now** in the **Load your profile?** box. With **Start in my profile from Steam** on, it also rebuilds it when you quit the game, if anything changed (see [When you quit the game](#when-you-quit-the-game)). Each time, it:

1. Removes the old links. It only ever removes links. If there's a real folder in there (put there by hand, for example), DewDrop leaves it alone and notes it in the SMAPI log, and SMAPI still loads it.
2. Creates one link for each turned-on mod, named after the mod's folder. If two mods have folders with the same name, the second link gets a suffix, such as `Name (local)`.
3. Links only one copy of a mod that's installed twice. For a collection profile, that's the collection's own copy.
4. Checks that every link leads to a `manifest.json`.

If any link didn't work, DewDrop cancels the restart and keeps the game running, with the message "Didn't restart: DewDrop couldn't link …". The old links are usually gone by then, so `Selected Mods` only holds part of your profile until you fix the problem and click **Apply & Restart** again. See [If it doesn't restart](../guides/apply-and-restart.md#if-it-doesnt-restart).

When a link fails as you quit the game, DewDrop notes it in the SMAPI log. If Steam already starts the game in your profile, the next launch only loads the mods that were linked. On Windows, if DewDrop was about to turn **Start in my profile from Steam** on, it leaves Steam alone that time and tries again the next time you quit.

When the active profile uses Stardrop collection mods, DewDrop also writes a small `SMAPI-config.json` into the folder. It tells SMAPI not to check those mods for updates, the same way Stardrop does.

### Links: junctions and symlinks

{% tabs %}
{% tab title="Windows" %}
DewDrop makes **directory junctions**, using Windows' built-in `mklink /J` command. Junctions don't need administrator rights or Developer Mode.

A junction can only point to a folder on a drive that's part of your computer. If your mods are on a network share or a mapped network drive, linking fails.
{% endtab %}

{% tab title="macOS, Linux and Steam Deck" %}
DewDrop makes **symbolic links**. They don't need any special permission. This hasn't been tested yet.
{% endtab %}
{% endtabs %}

## Which session you're in

SMAPI loads mods from one folder per session. When the game starts, DewDrop asks SMAPI which folder that is. The [`dewdrop_status`](console-commands.md) command shows it on its `Session` line.

| Session | SMAPI loaded | Usually started by | What DewDrop does |
| --- | --- | --- | --- |
| `Profile` | `Selected Mods`, or a folder inside it | A DewDrop restart such as **Apply & Restart**, Steam with **Start in my profile from Steam** on, Stardrop, or a launch option you set yourself. | Only your profile's mods are loaded. **Apply & Restart** lights up only when your profile no longer matches what's loaded, an update is waiting, or a mod you uninstalled is still loaded. |
| `Plain` | Your game's `Mods` folder, or the mods folder Stardrop is set to use | Steam without **Start in my profile from Steam**, a shortcut, the Xbox app or `StardewModdingAPI`. | SMAPI loaded every mod in that folder. DewDrop compares them with your profile (see [What counts as a difference](#what-counts-as-a-difference)), and the bottom bar says what differs. |
| `Custom` | Any other folder | SMAPI started with `--mods-path` pointing at a folder DewDrop doesn't manage, or SMAPI didn't say which folder it used. | DewDrop can't compare that with your profile, so **Apply & Restart** is available straight away. |

The menu's bottom bar says which mods differ, or that the loaded mods match. [The bottom bar](../guides/the-menu.md#the-bottom-bar) lists every message, and [How launching works](../getting-started/launching.md) explains what this means day to day.

## What Apply & Restart does

1. DewDrop checks it's safe to restart. It won't if it couldn't read `Settings.json` or your active profile's file, if SMAPI isn't in your game folder, or while a mod is still installing.
2. It asks you to confirm. The dialog lists mods that are loaded now but will be off, mods you uninstalled, which it deletes, and downloads that will be cancelled.
3. It rebuilds `Selected Mods` from your active profile, as described above.
4. It notes the restart in `startup.json`, so the next launch can tell whether the restart came back. See [The restart note](#the-restart-note).
5. It starts a small helper that waits in the background.
6. It cancels any running downloads, plays a sound and closes the game. As the game closes, DewDrop moves the mods you uninstalled out of the way (see [Uninstalling a mod](#uninstalling-a-mod)).
7. The helper waits until the game has fully closed, then starts it again in your profile. How it does that depends on your system; see below.

The new session is a clean start, so every mod in your profile loads fresh. It also skips the title intro and goes straight to the title buttons.

**Restart now** in the **Load your profile?** box uses these same steps, without the confirmation in step 2.

**Apply & Restart** doesn't change Steam. On Windows, if **Start in my profile from Steam** is waiting to turn on or off, that happens when you quit the restarted game.

### The restart helper on each system

{% tabs %}
{% tab title="Windows" %}
1. DewDrop writes `relaunch.ps1` into `Stardrop\Data\Cache\DewDrop` and runs it in a hidden PowerShell window. It runs with `-ExecutionPolicy Bypass`, which applies to that one script only and doesn't change any Windows setting.
2. Half a second after the game closes, the script finishes waiting updates and uninstalls from `pending-updates.json`.
3. It starts `StardewModdingAPI.exe` from your game folder with `--mods-path` pointing at `Selected Mods`, and hands the new game the restart's one-time ID in the `DEWDROP_LAUNCH` environment variable. A new SMAPI console window opens as usual. SMAPI is started directly, not through Steam or the Xbox app.

The script writes to `relaunch.log` next to it only when it finished updates or uninstalls, changed Steam's launch option or something went wrong. If SMAPI can't start, the game doesn't come back, and `relaunch.log` says why.

The same script also runs when you quit the game, if an update is waiting, an uninstall is held up by an open file, or Steam's launch option needs to change. Then it doesn't start the game. See [When you quit the game](#when-you-quit-the-game).
{% endtab %}

{% tab title="macOS" %}
1. A small background shell command checks every half second whether the game has closed.
2. It then starts `StardewModdingAPI` from your game folder (`Contents/MacOS`), with `--mods-path`, the `SMAPI_MODS_PATH` variable and `DEWDROP_LAUNCH` set.
3. Its output is thrown away, so you probably won't get a Terminal window for SMAPI.

There's no update step, because macOS lets DewDrop replace mod folders while the game is running. This hasn't been tested yet.
{% endtab %}

{% tab title="Linux" %}
This is for Linux, or a Steam Deck, without DewDrop's Steam launcher. With the launcher, see the **Steam Deck** tab.

1. A small background shell command checks every half second whether the game has closed.
2. It then runs `StardewValley` from your game folder, with `--mods-path`, the `SMAPI_MODS_PATH` variable and `DEWDROP_LAUNCH` set. SMAPI's installer replaces that file with SMAPI's own launcher, which starts SMAPI, usually in a terminal window. In Game Mode on a Steam Deck, DewDrop also sets `SMAPI_NO_TERMINAL`, since Game Mode can't show one.

If a game update has put the original `StardewValley` back, the restart starts the game without SMAPI, and so without mods. Run SMAPI's own installer again to fix it, or let the DewDrop installer set up its Steam launcher with `--steam-setup`.

There's no update step, because Linux lets DewDrop replace mod folders while the game is running. This hasn't been tested yet.
{% endtab %}

{% tab title="Steam Deck" %}
This is for [DewDrop's Steam launcher](#dewdrops-steam-launcher), which the installer sets up by itself on a Steam Deck, and with `--steam-setup` on other Linux systems. Without it, see the **Linux** tab.

1. DewDrop writes the restart's ID into a `restart` file next to the launcher, in `Stardrop/Data/Cache/DewDrop`.
2. A small background shell command waits for the game to close, then runs the launcher again with your game's `StardewValley`, the way Steam does.
3. The launcher takes the `restart` file, hands the ID on in `DEWDROP_LAUNCH`, and starts `StardewModdingAPI` directly with `--no-terminal` and `--mods-path` pointing at `Selected Mods`.

The restart stays in the game's Steam session, which Game Mode should keep showing, and `steam-launch.log` records it.

This is new and being tested on a real Steam Deck. If the game doesn't come back in Game Mode, press **Play**. That launch starts in your profile if the restart never got going (its `restart` file is still there), or if **Start in my profile from Steam** is on.
{% endtab %}
{% endtabs %}

## Starting in your profile

SMAPI picks its mods folder as the game starts, before any mod runs, DewDrop included. So DewDrop can't switch a normal launch into your profile once it has started. Two settings in the **Account** tab > **Settings** panel close that gap:

* **Start in my profile from Steam** has Steam's **Play** start SMAPI on `Selected Mods`, so the game starts in your profile.
* **Ask to load my profile** (on by default) shows the **Load your profile?** box when a normal launch loaded different mods than your profile, and offers a restart.

[Start in your profile](../guides/start-in-your-profile.md) covers both from the player's side. This section explains what happens behind them.

### Start in my profile from Steam

How it works depends on your system:

{% tabs %}
{% tab title="Windows" %}
Steam keeps each game's launch options in a settings file, `localconfig.vdf`, in `<Steam folder>\userdata\<number>\config\`, one per Steam account. With the setting on, DewDrop adds SMAPI's `--mods-path` option to Stardew Valley's launch option, pointing at `Selected Mods`:

```text
"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" --mods-path "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods" %command%
```

Everything else in the launch option is kept, and an older `--mods-path` is replaced. If the launch option was empty, DewDrop sets SMAPI's usual option with `--mods-path` added. Turning the setting off takes out only the `--mods-path` part, so **Play** still starts SMAPI.

Steam has to be closed for the change, so it happens after you quit the game. See [How DewDrop changes Steam's launch option](#how-dewdrop-changes-steams-launch-option).

It isn't available when Stardew Valley's launch option starts some other program than SMAPI, because DewDrop can't know what that program does with `--mods-path`.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
Steam's launch option runs [DewDrop's Steam launcher](#dewdrops-steam-launcher), which the installer sets up once: by itself on a Steam Deck, and with `--steam-setup` on other Linux systems.

The setting is then a `start-in-profile` file next to the launcher, which it checks at each launch. Ticking or unticking the row changes the file at once, and Steam never has to close.

A `--mods-path` you add to Steam's launch options yourself doesn't count here.
{% endtab %}
{% endtabs %}

Either way:

* **Before you've chosen,** DewDrop takes what Steam already does as your choice, the first time the game starts with the setting available. On Windows, that's whether the launch option already points at `Selected Mods`, for example because you added it yourself; DewDrop then leaves it alone. On Linux and the Steam Deck, it's whether the `start-in-profile` file is there, which the installer writes when you answer its question. In `config.json`, `StartInProfileFromSteam` is `null` until then.
* **Only the signed-in Steam account** is used: the one Steam is running as, or else the one that signed in most recently. Other accounts on the computer are left alone.
* **It isn't available**, and the row is grayed out, on macOS, on Linux and the Steam Deck without DewDrop's launcher, when the game runs through Proton or Wine or isn't the Steam version, or when DewDrop can't find Steam, read your account's settings or update its launcher. Hovering over the row says which; see [When it isn't available](../guides/start-in-your-profile.md#when-it-isnt-available).
* **Only Steam's Play button** gets it. A desktop shortcut, or starting `StardewModdingAPI` yourself, still loads the whole `Mods` folder.
* **Stardrop rebuilds `Selected Mods`** whenever it starts the game, and its links for a Stardrop collection profile leave DewDrop out. On Windows, the next **Play** in Steam then starts without the **Mods** button. DewDrop's Steam launcher checks for DewDrop first, and starts the whole `Mods` folder instead. The Stardrop note under [When it isn't available](../guides/start-in-your-profile.md#when-it-isnt-available) says how to get the button back.

### How DewDrop changes Steam's launch option

This is for Windows. On Linux and the Steam Deck, only the installer changes Steam's settings, once; see [DewDrop's Steam launcher](#dewdrops-steam-launcher).

Steam writes its settings file over when it closes, so a change made while Steam runs would be lost. That's why DewDrop only reads the file while the game is running, and makes the change after you quit:

1. When the game starts, DewDrop reads Stardew Valley's launch option from the file.
2. When you quit, if the setting and the launch option don't agree, DewDrop writes the old and new launch option into `steam-job.json` and starts `relaunch.ps1`, the same script **Apply & Restart** uses.
3. The script waits for the game to close. If Steam is running, it waits 5 more seconds, so Steam can finish with the game (cloud saves and playtime), then asks Steam to close and waits up to 90 seconds for it. If Steam isn't running, the script skips closing and reopening it.
4. It reads the file again and checks that Stardew Valley's launch option is still the one DewDrop read when the game started. If you changed it in Steam while you played, DewDrop leaves it alone, with the message "Stardew Valley's launch options changed in Steam while the game was running, so DewDrop left them alone."
5. It checks that the edited file reads back with the new launch option, saves a copy of the old file as `localconfig.vdf.dewdrop-backup` next to it (replacing the previous copy), and writes the new file.
6. Before it opens Steam again, it clears the environment variables it inherited from the game, such as `SMAPI_MODS_PATH`, `DEWDROP_LAUNCH` and Steam's own `SteamAppId`, so Steam doesn't hand them to the games it starts next. Steam opens minimized to the system tray.
7. It writes how it went into `steam-result.json`. At the next start, DewDrop reads and removes it. When you open the **Mods** menu, a message says "Steam now starts the game in your profile.", "Steam no longer starts the game in your profile." or "Steam's launch option wasn't changed:" followed by the reason. `relaunch.log` gets a line too.

Turning the setting on also needs the links in `Selected Mods` to be up to date, or the next launch would start with no mods and no **Mods** button. So DewDrop only adds `--mods-path` once it has linked your profile, either as you quit (see the next section) or because the game already started in your profile with nothing changed. If the links can't be made, Steam is left alone and DewDrop tries again the next time you quit.

### When you quit the game

On every system, and after **Apply & Restart** too, DewDrop first moves the mods you uninstalled out of your `Mods` folder (see [Uninstalling a mod](#uninstalling-a-mod)).

On Windows, and on Linux and the Steam Deck with DewDrop's Steam launcher, DewDrop then does a little more as the game closes. It skips this after **Apply & Restart** or **Restart now**, which do their own.

1. If **Start in my profile from Steam** is on, or waiting to turn on, and your profile has changed since the game started, DewDrop rebuilds `Selected Mods`. The same goes for a session that didn't start in your profile. The next **Play** in Steam then loads your changes.
2. On Windows, if an update is waiting, an uninstalled mod couldn't be moved because a file was open, or Steam's launch option needs to change, DewDrop starts `relaunch.ps1` without restarting the game. Once the game has closed, the script finishes the updates and uninstalls first, then changes Steam's launch option as described above.

If the game doesn't close normally, for example because it crashed, this may not happen. Waiting updates and the change to Steam then wait until the next time you quit. DewDrop deletes the mods you uninstalled at the next start instead, unless that session loads them again.

### The Load your profile? box

On the first title screen of a normal launch, DewDrop compares the mods that loaded with your profile. If they differ and **Ask to load my profile** is on, it skips the title intro and opens the **Load your profile?** box. It names the mods that differ and offers **Restart now**, which runs the same steps as **Apply & Restart** without asking to confirm. [The "Load your profile?" box](../guides/start-in-your-profile.md#the-load-your-profile-box) describes its buttons and checkbox.

While **Start in my profile from Steam** is off but available, the box adds a **Start in my profile from Steam from now on** checkbox, ticked at first. If it's ticked when you click **Restart now**, the setting turns on. On Windows, Steam's launch option then changes when you quit the restarted game. With DewDrop's Steam launcher, it turns on at once.

If the restart can't start, the box changes to **Couldn't load your profile**, says why, and adds "You can keep playing with the mods loaded right now."

The box doesn't open when:

* the mods match, or the game started in a profile or on a folder DewDrop doesn't manage;
* **Ask to load my profile** is off;
* the game runs under Proton or Wine, or started from a Steam invite;
* DewDrop couldn't read `Settings.json` or your profile, or SMAPI's launcher is missing.

DewDrop checks only once per launch. It also gives up if you open **Load**, **Co-op** or another title menu first, if a save loads, or if no title screen comes up within about 3 minutes of the game running. The [`dewdrop_status`](console-commands.md) command, or the `Load your profile:` line in the SMAPI log, shows what happened.

### What counts as a difference

DewDrop compares the mods SMAPI loaded with the mods a restart into your active profile would load. They match when:

* every mod that loaded is on in your profile,
* every mod that's on in your profile loaded, and
* each one loaded from the copy your profile would link. This only matters when the same mod is installed twice, such as a loose copy and a copy in a Stardrop collection.

These don't count as a difference:

* **DewDrop itself.** It always loads, and it's always linked.
* **Mods you uninstalled,** unless another copy of them stays installed. While one is still loaded, the bottom bar says so on its own line instead, such as "Some Mod, which you uninstalled, is still loaded. Restart to finish uninstalling."
* **Mods SMAPI refused to load.** If a mod is on in your profile but SMAPI skipped it as the game started, for example because it's broken, needs a mod you don't have or needs another game version, a restart wouldn't load it either. A mod you copy in or install after the game started is different: SMAPI hasn't had a chance to load it yet, so it counts, and the **Installed** tab says **On after restart**.

These do count:

* **Mods from outside DewDrop's mods folder.** If SMAPI loaded mods from your game's `Mods` folder while Stardrop is set to use another mods folder, nothing links those mods, so a restart leaves them out. They count as off.
* **Two copies of one mod in your `Mods` folder.** SMAPI loads neither, but a restart links only one. If the mod is on in your profile, it counts as one that didn't load. The **Installed** tab marks both copies **Installed twice**.

A `Mods` folder moved to another drive with a junction or symbolic link counts as the same folder, so a launch from it is still a normal launch.

So if every mod in your `Mods` folder is on in your profile, a normal launch already matches, and DewDrop doesn't ask. The same comparison sets the bottom bar's status line and the **On after restart** labels during a normal launch.

### The restart note

Every DewDrop restart, from **Apply & Restart** or **Restart now**, leaves a note in `startup.json`, in `Stardrop/Data/Cache/DewDrop`. The note holds a one-time ID, the profile's name and when the restart started. The new game gets the same ID in the `DEWDROP_LAUNCH` environment variable.

When the new game starts in `Selected Mods` with that ID, DewDrop knows it's the restart it planned, and marks the note as arrived. Once that game reaches the title screen, DewDrop removes the note and skips the title intro.

If the note can't be saved, the restart still happens. DewDrop just can't tell afterwards whether it came back.

### If a restart doesn't come back

Sometimes the next launch isn't the restart DewDrop planned. The restart may never have come back, or the game may have closed while your profile was loading. Either way, the note is still in `startup.json` when the game starts. DewDrop then:

* Removes the note, whatever kind of launch comes next, even one from Stardrop.
* Writes a warning in the SMAPI log.
* On a normal launch whose mods differ from your profile, shows the reason in the **Load your profile?** box instead of the list of mods: "Last time DewDrop restarted the game, it didn't come back in your profile." or "Last time, the game closed while your profile was loading." The **Start in my profile from Steam from now on** checkbox starts unticked that time.

DewDrop never restarts the game unless you click **Apply & Restart** or **Restart now**, so a restart that fails can't repeat by itself.

## DewDrop's Steam launcher

On Linux and the Steam Deck, Steam starts the game through a small script, DewDrop's Steam launcher. It's new, and being tested on a real Steam Deck.

**Why it's there.** On Linux, SMAPI's installer replaces the game's `StardewValley` file with a launcher that opens SMAPI in a terminal window. Game Mode on a Steam Deck isn't made for that window, so you can end up on a black screen or a terminal window instead of the game. A game update also puts the original `StardewValley` back, which quietly turns mods off.

**How it's set up.** The launcher ships in DewDrop's mod folder as `assets/steam-launch.sh`. The installer copies it to `Stardrop/Data/Cache/DewDrop/steam-launch.sh` and sets Stardew Valley's launch option in Steam to run it: by itself on a Steam Deck, and with `--steam-setup` on other Linux systems. The launch option looks like this, with your own data folder:

```text
"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
```

Steam replaces `%command%` with the command it was going to run, and hands it to the launcher. Each time the game starts while Steam's launch option uses the launcher, DewDrop checks its copy against the one in its mod folder and writes it again if it differs. It writes a temporary copy, makes it runnable, then renames it into place, so Steam never finds half a file. Edits you make to the copy don't last. If DewDrop can't update it, **Start in my profile from Steam** is grayed out with "DewDrop's Steam launcher couldn't be updated. The SMAPI log says why."

**What it does at each launch:**

1. It looks in Steam's command for the game's `StardewValley`, next to `Stardew Valley.dll`. If there's none, for example because Steam runs the Windows version through Proton, or if SMAPI isn't in the game folder, it runs Steam's command unchanged.
2. It decides which folder SMAPI loads. Your profile's `Selected Mods` when a `restart` file is next to it (an **Apply & Restart**; the file is taken, and its ID goes to the game in `DEWDROP_LAUNCH`), or when the `start-in-profile` file is there (**Start in my profile from Steam** is on). Otherwise, the `Mods` folder.
3. It only uses `Selected Mods` if DewDrop is linked in it, so you're never left without the **Mods** button. If it isn't, it starts the `Mods` folder instead.
4. It puts `StardewModdingAPI --no-terminal` in place of `StardewValley`, adds `--mods-path "<data folder>/Selected Mods"` when it's using your profile, and runs the rest of Steam's command unchanged, so the game still starts inside Steam's runtime.
5. It writes a line to `steam-launch.log` saying which folder it chose and why. See [Logs](file-locations.md#logs).

What this means for you:

* **No console window,** in Game Mode or Desktop Mode, restarts included. Console commands such as `dewdrop_status` can't be typed; the SMAPI log has most of what it would show. See [Console commands](console-commands.md#no-console-use-the-smapi-log).
* **A game update doesn't turn mods off,** because the launcher starts SMAPI itself.
* **Steam's settings never change again** after the installer: **Start in my profile from Steam** is the `start-in-profile` file, and **Apply & Restart** goes through the launcher (see [The restart helper on each system](#the-restart-helper-on-each-system)).

To stop using it, delete only the launcher's path from Stardew Valley's launch options in Steam and keep the rest. If only `%command%` is left, clear the box. See [Uninstalling DewDrop](../getting-started/uninstalling.md).

## Game Mode on the Steam Deck

Game Mode shows only Steam and the game: there's no desktop browser, terminal or file manager, and typing needs an on-screen keyboard. DewDrop notices Game Mode, and works around it:

* **Web pages** (**Open on Nexus**, **Get my API key** and the like) open in Steam's overlay browser, over the game. If the overlay is turned off or can't open, DewDrop asks Steam to open the page in its own browser instead.
* **Text boxes** open Steam's on-screen keyboard when you tap or click them, or the game's own on-screen keyboard if Steam's can't open.
* **Open mods folder** on the **Downloads** tab is grayed out, since there's no file manager. It works in Desktop Mode.

DewDrop counts the game as in Game Mode when it runs under Game Mode's gamescope session. It counts the computer as a Steam Deck (or another SteamOS device) when Steam says so, when the hardware is a Valve Steam Deck (LCD or OLED), or when the operating system is SteamOS. Each start, the SMAPI log gets a line such as `Linux: SteamOS True, Game Mode True, DewDrop's Steam launcher in use.`

See [DewDrop on the Steam Deck](../getting-started/steam-deck.md) for the setup and day-to-day use.

## Updates

### Finding updates

DewDrop checks for mod updates with smapi.io, the same service SMAPI uses for its own update alerts. It checks when the game starts (unless you turned off **Check for updates at startup**), and whenever you click **Check for updates** on the **Installed** tab.

Only mods whose `manifest.json` lists update keys and a valid version are offered updates. Mods from Stardrop collections are never checked, because the collection decides their version. smapi.io gets each mod's ID, version and update keys, plus your SMAPI version, game version and operating system.

DewDrop also asks about loose mods without update keys, but only to learn which Nexus mod smapi.io's list says each one is. It never offers updates from those answers. It keeps the Nexus IDs in `mod-sources.json`, for [Knowing what you have](#knowing-what-you-have).

DewDrop checks for updates to itself the same way: its own manifest's update key points at its GitHub releases. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

### Installing an update

When you click **Update to** *version*:

* If the mod comes from Nexus Mods, the update is there, and you're logged in, DewDrop looks for the file with that version on the mod's page, preferring the main file. Premium accounts download it straight into the game. Free accounts get the file's page in the browser, where they click **Slow download** to send it to DewDrop. That only works on Windows for now; elsewhere, the message says to add the mod to your `Mods` folder yourself. If no file has that version, DewDrop opens the mod's files page instead.
* Otherwise, for example when you're logged out or the update is on another site, DewDrop opens the update's page in your browser.

See [Updating mods](../guides/updating-mods.md) for the step-by-step version.

### Updates that wait for a restart

An update replaces a mod's whole folder. DewDrop does it carefully:

1. It unpacks the new version into a hidden `.dewdrop-staging` folder next to the mod's folder. SMAPI and DewDrop both skip dot-folders, so a half-unpacked mod never loads.
2. It copies your own files from the old version into the new one: every `config.json`, files in the mod's `data` folder, and `.json` files at the top of the mod's folder that the new version doesn't include. That's where mods usually keep settings and per-save data. Other leftover files from the old version aren't kept.
3. It renames the old folder out of the way, moves the new one into its place, then deletes the old one. If anything fails, the old folder is put back.

On Windows, step 3 often fails while the game is running, because SMAPI keeps files of loaded mods open, especially mods that contain code. Then:

* The new copy stays in `.dewdrop-staging`, and the update is listed in `pending-updates.json` in `Stardrop/Data/Cache/DewDrop`.
* The mod shows **Update ready, restart to finish** on the **Installed** tab, and the download shows "Update ready, finishes when the game restarts".
* **Apply & Restart** finishes it: the helper swaps the folders after the game has closed and before SMAPI starts.
* **Quitting the game** finishes it too. DewDrop starts the same script as you quit, and it swaps the folders once the game has closed, without starting the game again. See [When you quit the game](#when-you-quit-the-game).
* If an update is still waiting when the game starts, for example because the game didn't close normally, DewDrop tries again then. That only works for mods SMAPI didn't load in that session.
* If you download a newer version while one is waiting, the newer one replaces it.

When an update does go in while the game is running (on macOS, Linux and the Steam Deck, and on Windows for content packs and other mods whose files aren't held open), the game keeps using the old version until the next restart. DewDrop remembers this, so **Apply & Restart** lights up.

## Downloads and installs

* Downloads go into `Stardrop/Data/Cache/DewDrop/downloads`, one at a time. The file is deleted after it's installed, whether or not the install worked.
* DewDrop opens `.zip`, `.7z` and `.rar` files. Every folder in the download that has a `manifest.json` is installed as its own folder in your install folder, usually `Mods`.
* If a download holds several versions of the same mod to choose from, DewDrop only installs one when it's updating a copy you already have and can tell which version that is. Otherwise it installs none of them, and the download says so.
* A mod you already have is updated in its existing folder, which keeps its location and name.
* A new mod never overwrites another mod's folder. If the name is taken, it gets one like `Name (2)`.
* DewDrop doesn't download required mods for you. When it turns a new mod on, it also turns on required mods you already have.
* Installing a mod into a folder that's waiting to be uninstalled cancels the uninstall.

See [A mod's page and installing files](../guides/mod-page.md) and [Downloads](../guides/downloads.md).

### Knowing what you have

The **Installed** and **Update** tags on Browse's cards, and the **Installed** marks on a mod's page, need to know which Nexus mod each of your mods is. DewDrop counts a mod as belonging to a Nexus mod when any of these says so:

* **The Nexus update key** in the mod's `manifest.json`.
* **DewDrop's install record.** Each time DewDrop installs a download, it notes in `mod-sources.json` which Nexus mod and file every mod in it came from, and the version that file put in. A mod whose own update key names a different Nexus mod, such as a framework bundled in another mod's download, is left to its own page.
* **smapi.io's mod list,** from the update check (see [Finding updates](#finding-updates)). It covers code mods, not content packs.

A content pack you installed by hand, without a Nexus update key, can't be recognized yet.

On a mod's page, a file counts as installed when DewDrop's record names it and you still have that version, as long as every mod that file installed is still there. If one is gone, no file is marked for that copy, so the file can be installed again. A copy without a record, or whose version changed since, is compared by version instead: the one file with your copy's version, or, when several share it, the single **MAIN** one. If DewDrop can't tell, it marks none. An update downloaded and waiting for a restart counts as installed.

A Premium download of a file the record says you have doesn't start again: the message says "*title* is already installed." A website link for it still installs it.

Mods you uninstalled don't count, even before the game closes.

## Website download links on Windows

Free Nexus Mods accounts download through the website's **Slow download** button, which sends an `nxm://` link to your mod manager. **Use DewDrop for these links** on the **Account** tab registers a small script to receive those links, for your Windows user only.

The script saves each link as a file in `Stardrop/Data/Cache/DewDrop/nxm`. The game checks that folder about once a second and starts the download if you're logged in. Links that arrive while the game is closed, or while you're logged out, wait there until you are. Links to collections, and expired links, are refused with a message.

This only exists on Windows for now. Elsewhere, where there's a SMAPI console, the [`dewdrop_nxm`](console-commands.md) command can take a link instead. See ["Mod manager download" links](../nexus-mods/mod-manager-links.md) and [Platform support](platform-support.md).

## The Mods button

The **Mods** button sits on the title screen between **Co-op** and **Exit**. DewDrop doesn't ship any of the game's art. It draws the button when the game starts, from the game's own title-button picture:

* The wooden frame and shading come from the game's own buttons, and the middle is filled with their parchment color.
* "MODS" is hand-lettered in the same colors as the game's lettering, with a dew drop on a leaf where the other buttons have an icon.
* Because it's built from whatever title buttons the game has loaded, mods that restyle the title screen carry over to the **Mods** button. If a content pack changes the title buttons after the game starts, DewDrop draws the button again.
* If the window is too narrow for five full-size buttons, a small **Mods** button appears in the bottom-right corner instead, above the small language and **?** (About) buttons. It's made from that button's frame, with a small dew drop in place of the question mark.

Clicking it slides the title screen away, the same way **Load** does, and opens the DewDrop menu as part of the title screen. That's why the game's back button and the drifting clouds keep working. The menu only opens from the title screen, not during a save.

The [`dewdrop_export_title`](console-commands.md) command saves the built button as a PNG, for anyone working on its art.

## What DewDrop connects to

| Service | What for | What's sent |
| --- | --- | --- |
| `api.nexusmods.com` | Browsing and searching, checking your login, getting download links. | Your searches and filters. When you're logged in, your API key goes with each request. |
| Nexus Mods' file servers | Downloading mod files and pictures. | Only the file request; no API key. |
| `smapi.io` | Checking for mod updates, and which Nexus mod each of your mods is. | Mod IDs, versions and update keys, plus your SMAPI version, game version and operating system. Nothing about your Nexus Mods account. |

That's the full list. Every request says it comes from DewDrop and which version.

## What DewDrop leaves alone

* **Your game's files and SMAPI's files.** DewDrop doesn't change either, or SMAPI's settings.
* **Steam,** with two exceptions:
  * On Windows, when you turn on **Start in my profile from Steam**, DewDrop changes only Stardew Valley's launch option, for the Steam account that's signed in, when you quit the game. See [How DewDrop changes Steam's launch option](#how-dewdrop-changes-steams-launch-option).
  * The installer can set the launch option too: on Windows only if you say yes (see [Install with the installer](../getting-started/installing/installer.md)), and on a Steam Deck, or on Linux with `--steam-setup`, to DewDrop's Steam launcher (see [Steam on Linux and the Steam Deck](installer-options.md#steam-on-linux-and-the-steam-deck)).
* **Your mod folders**, except when you update a mod (the folder is replaced, keeping your settings), install a new one, or uninstall one (the folder is deleted once the game closes).
* **Stardrop's other settings.** DewDrop shares `Settings.json` but only changes the parts it uses. See [config.json options](config.md#settings-dewdrop-reads-from-stardrop).
