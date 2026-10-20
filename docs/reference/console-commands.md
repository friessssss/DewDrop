---
description: "The three commands DewDrop adds to the SMAPI console, with examples, what their output means, and what to use where there's no console."
icon: terminal
---

# Console commands

SMAPI's console is the window of text that opens beside the game when you play with mods. Besides showing SMAPI's messages, it accepts typed commands. DewDrop adds three:

| Command | What it's for |
| --- | --- |
| `dewdrop_status` | Shows what DewDrop sees: where mods were loaded from, your profile, the **Load your profile?** box, **Start in my profile from Steam** and your Nexus Mods login. |
| `dewdrop_nxm` | Downloads a Nexus Mods file from an `nxm://` link you paste in. |
| `dewdrop_export_title` | Saves the title-screen button art as PNG files, for working on DewDrop's art. |

There are no commands to switch, apply or list profiles. Use the **Mods** menu for those.

## Where the console is

| System | Console |
| --- | --- |
| Windows | A black window full of text opens beside the game. |
| macOS | A Terminal window, when you start the game the usual way. After a DewDrop restart there's probably none (untested); start the game the usual way to get it back. |
| Linux | A terminal window, when SMAPI's own launcher starts the game. None when Steam starts the game through DewDrop's Steam launcher (after the installer's `--steam-setup`). |
| Steam Deck | None. DewDrop's Steam launcher, which the installer sets up on a Deck, starts SMAPI without a console, in Game Mode and Desktop Mode, restarts included. |

If DewDrop couldn't start, for example because it couldn't read its data folder, its commands aren't added. The SMAPI console shows an error from DewDrop while mods are loading instead.

### No console? Use the SMAPI log

Everything DewDrop prints goes to the [SMAPI log](../help/reporting-issues.md#find-your-smapi-log) too, so you can read it there. Even without typing `dewdrop_status`, DewDrop writes a few lines at each start that cover most of the same ground. Search the log for:

* `Start in my profile from Steam:` the setting, Stardew Valley's launch option in Steam, and whether it's up to date or why it isn't available.
* `Load your profile:` what happened with the **Load your profile?** box, in the same words as [the table below](#what-the-load-your-profile-part-says).
* `Linux: SteamOS` (Linux and the Steam Deck only): whether DewDrop detected SteamOS and Game Mode, and whether DewDrop's Steam launcher is in use.
* `SMAPI loaded mods from`: the game didn't start in your profile. The line names the folder SMAPI loaded instead.
* `Started in the`: a DewDrop restart came back in your profile, which the line names.
* `Logged in to Nexus Mods as`: your Nexus Mods login worked. The account name follows, with `(Premium)` for Premium accounts.

On Linux and the Steam Deck, `steam-launch.log` also says which folder each launch loaded. See [Where DewDrop keeps files](file-locations.md#logs).

## Running a command

1. Click the SMAPI console window to bring it to the front.
2. Type the command, then press **Enter**.
3. The answer appears as new lines in the console. It's also saved in your SMAPI log.

To paste into the console, right-click or press **Ctrl+V** on Windows, **Cmd+V** on macOS, and **Ctrl+Shift+V** in most Linux terminals.

To list every command SMAPI knows, or see one command's description:

```text
help
help dewdrop_status
```

## `dewdrop_status`

Shows what DewDrop sees right now. It's the quickest way to check whether your profile is loaded, and it's useful in bug reports.

```text
dewdrop_status
```

Example output from a game started from Steam on Windows:

```text
[14:02:31 INFO  DewDrop] Running mods path: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods (profile folder: False)
[14:02:31 INFO  DewDrop] Mods folder: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods; install folder: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods
[14:02:31 INFO  DewDrop] Active profile: Default; installed mods: 24; needs restart: True
[14:02:31 INFO  DewDrop] Session: Plain; ask to load my profile: True (asked)
[14:02:31 INFO  DewDrop] Start in my profile from Steam: off, Steam launch option: "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command%
[14:02:31 INFO  DewDrop] Nexus: not logged in
```

What each part means:

| Part | Meaning |
| --- | --- |
| `Running mods path` | The folder SMAPI loaded mods from in this session. |
| `profile folder: True` | This session started from your profile's `Selected Mods` folder, for example after **Apply & Restart**. Only the mods in your profile are loaded. |
| `profile folder: False` | This session didn't start from your profile's folder. Usually it started the normal way, from Steam, a shortcut or `StardewModdingAPI`, so SMAPI loaded everything in that folder. The `Session` line says more. |
| `Mods folder` | Where DewDrop looks for your installed mods. |
| `install folder` | Where DewDrop puts mods it downloads. Usually the same as the `Mods` folder. |
| `Active profile` | The profile DewDrop will apply. |
| `installed mods` | How many mods DewDrop found in your `Mods` folder, plus any Stardrop collection mods. Mods you uninstalled aren't counted, even before the game closes. |
| `needs restart: True` | **Apply & Restart** would change something: the mods loaded differ from your profile, an update is waiting, a mod you uninstalled is still loaded, or SMAPI loaded mods from a folder DewDrop can't compare with your profile. |
| `Session` | What kind of launch this was. `Profile`: SMAPI loaded your profile's `Selected Mods` folder. `Plain`: a normal launch that loaded your game's `Mods` folder, or the mods folder Stardrop is set to use. `Custom`: SMAPI was started on some other folder with `--mods-path`, or didn't say which folder it used. See [Which session you're in](how-it-works.md#which-session-youre-in) and [How launching works](../getting-started/launching.md). |
| `ask to load my profile` | The **Ask to load my profile** setting on the **Account** tab: `True` when it's on, `False` when it's off. The part in brackets after it says what happened with the **Load your profile?** box; see the next table. |
| `Start in my profile from Steam` | `on` or `off`, then Stardew Valley's launch option in Steam, as DewDrop read it when the game started. `(changes when the game closes)` at the end means the setting and Steam's launch option don't agree yet (Windows), so DewDrop changes the launch option when you quit. When the setting isn't available, the line says why instead, such as `Only for the Steam version of the game on Windows, Linux and the Steam Deck.` |
| `Nexus` | The Nexus Mods account you're logged in with, followed by `(Premium)` for Premium accounts, or `not logged in`. |

Both folders come from Stardrop's `Settings.json`, which DewDrop shares. See [config.json options](config.md#settings-dewdrop-reads-from-stardrop).

### What the Load your profile? part says

| In brackets | What it means |
| --- | --- |
| `waiting for the title screen` | DewDrop hasn't checked yet. |
| `asked` | It showed the **Load your profile?** box. |
| `the mods loaded already match the profile` | Nothing to do: the mods that loaded are the ones your profile would load. |
| `the game started in a profile` | This session started from `Selected Mods`, for example from Steam with **Start in my profile from Steam** on, or from Stardrop, so there was nothing to check. |
| `this session is DewDrop's restart` | This session is the restart DewDrop started. It reached the title screen, so the restart worked. |
| `SMAPI started on a folder DewDrop doesn't manage` | SMAPI was started on some other folder with `--mods-path`, so DewDrop can't compare it with your profile. |
| `turned off` | **Ask to load my profile** is off. |
| `another title menu opened first` | You opened **Load**, **Co-op** or another title menu before DewDrop asked, so it didn't ask. |
| `a save loaded, or no title screen came up in time` | It gave up for this session. |
| `started from a Steam invite` | The game started from a Steam multiplayer invite, so DewDrop didn't ask. |
| `SMAPI's launcher is missing` | SMAPI isn't in your game folder, so a restart couldn't work. |
| `the settings or profile couldn't be read` | DewDrop couldn't read Stardrop's `Settings.json` or your active profile's file. **Apply & Restart** is off then too. |
| `not available under Wine or Proton` | DewDrop can't restart the game on this system, so it never asks. See [Platform support](platform-support.md). |
| `stopped after an error` | Something went wrong. The SMAPI log says what. |

## `dewdrop_nxm`

Downloads and installs a Nexus Mods file from an `nxm://` link, the same way it would if the website's mod manager download had reached DewDrop. Type the command, a space, then paste the link:

```text
dewdrop_nxm nxm://stardewvalley/mods/<mod id>/files/<file id>?key=...&expires=...&user_id=...
```

You'll mostly want this on **macOS and Linux with a free Nexus Mods account**. There, the website's **Slow download** links can't reach DewDrop yet, so this is how you send one to it. It can also help on Windows when another app handles those links. Premium accounts don't need it: their **Install** button downloads directly. See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md).

It isn't available where there's no console: on a Steam Deck, or on Linux with DewDrop's Steam launcher (see [Where the console is](#where-the-console-is)).

To use it:

1. Log in to Nexus Mods in DewDrop first (**Account** tab). See [Connecting your Nexus account](../nexus-mods/connecting.md).
2. On the Nexus Mods website, start a mod manager download of the file. For a free account, that means clicking **Slow download** on the mod manager download page.
3. Copy the `nxm://` link the website gives your browser. How to get at it depends on your browser; the link always starts with `nxm://stardewvalley/mods/`.
4. In the SMAPI console, type `dewdrop_nxm`, a space, then paste the link. Press **Enter**.
5. The download appears on the [Downloads](../guides/downloads.md) tab. When it says it's installed, click **Apply & Restart** to load the mod.

Good to know:

* **Links expire quickly.** Nexus Mods only accepts them for a short time. If the download fails because the link expired, get a fresh one from the website.
* **Links belong to your account.** They carry a short-lived download key for you, so don't post them publicly.
* **Only single mod files work.** Links to Nexus collections, or to other games, are refused. Collections install through Stardrop.
* **Running the same link twice** while the first download is still going does nothing.
* **You can run it at the title screen or while playing.** Downloads keep going in the background, but messages only show in the **Mods** menu, which opens from the title screen.

| Message | What it means |
| --- | --- |
| `That isn't a Stardew Valley nxm:// mod file link.` (in the console) | The text after the command isn't a link to one Stardew Valley mod file. Check you copied the whole link. |
| `Log in to Nexus Mods first (Account tab).` (in the console and the menu) | You're not logged in. Log in on the **Account** tab, then run the command again. |

## `dewdrop_export_title`

Saves two PNG files, for anyone working on DewDrop's **Mods** button art:

* `TitleButtons.png`: the game's title-screen buttons, as currently loaded (including any texture mods you use).
* `ModsButton.png`: the **Mods** button DewDrop built from them. See [The Mods button](how-it-works.md#the-mods-button).

Run it while the title screen is showing. Without a folder, the files go into a `debug` folder inside DewDrop's mod folder, such as `Mods/DewDrop/debug`. To save them somewhere else, add a full path after the command:

```text
dewdrop_export_title
dewdrop_export_title C:\Users\<you>\Desktop\title-art
```

* The folder is optional, even though the command's help shows it as if it were required.
* When it works, the console says `Exported to` followed by the folder.
* If the title screen isn't showing, you'll see `Run this from the title screen.`

{% hint style="warning" %}
Both images are made from Stardew Valley's own art. Keep them on your computer for your own use; don't share them or commit them to the DewDrop repository. DewDrop itself ships no game art.
{% endhint %}
