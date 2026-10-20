---
description: "Set DewDrop up once in Desktop Mode, then install, update and switch mods from the game in Game Mode."
icon: gamepad
---

# DewDrop on the Steam Deck

Modding Stardew Valley on a Steam Deck usually means going back to Desktop Mode every time you want to add or change a mod. With DewDrop, you go to Desktop Mode **once**, to run the installer. After that, you browse, install, update and turn mods on and off from the **Mods** button on the title screen, in Game Mode.

{% hint style="warning" %}
**New, being tested.** Steam Deck support is new in DewDrop 0.2.0. Still being checked on a real Deck: whether the game comes back in Game Mode after **Apply & Restart**, and whether Steam's overlay browser and keyboard open from the game. If something doesn't work, the [logs below](#if-something-goes-wrong) say why. Please [report how it went](../help/reporting-issues.md), whether it works or not. Want to help test? [Testing on a Steam Deck](../contributing/steam-deck-testing.md) has the checklist.
{% endhint %}

## Set up once, in Desktop Mode

{% stepper %}
{% step %}
### Start the game once without mods

If you've never played Stardew Valley on this Deck, start it once in Game Mode, wait for the title screen, then quit. SMAPI, the mod loader DewDrop runs on, recommends this first start so the game can finish its own setup.
{% endstep %}

{% step %}
### Switch to Desktop Mode

Press the **Steam** button, open **Power** and choose **Switch to Desktop**.
{% endstep %}

{% step %}
### Run the installer

Open **Konsole** from the app launcher in the bottom-left corner. Paste this line (right-click and choose **Paste**, or press **Ctrl+Shift+V**) and press **Enter**:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
```

Rather download it? Get `DewDrop-<version>-installer.zip` from the [latest release](https://github.com/friessssss/DewDrop/releases/latest), extract it in **Dolphin**, and double-click `Install on Linux.sh`.
{% endstep %}

{% step %}
### Answer the questions

Press **Enter** at a question to take the recommended answer. On the Deck, the installer:

1. Installs SMAPI, if you don't have it, and DewDrop.
2. Offers to take your Nexus Mods API key, so you don't have to type it later with the on-screen keyboard. Open [your Nexus Mods API key page](https://www.nexusmods.com/users/myaccount?tab=api) in a browser, copy the **Personal API Key** at the bottom, and paste it at "API key (or just press Enter to skip):". DewDrop logs in with it the next time the game starts, and if it can't reach Nexus Mods then, it tries again at the next start. It isn't asked if you're already logged in.
3. If the game already has launch options in Steam, shows what they'll become with DewDrop's launcher and asks first. See [What the installer changes](#what-the-installer-changes).
4. Asks "Start the game in your DewDrop profile when you press Play in Steam?" (recommended: yes). If you already chose, here or in the game, it keeps that and doesn't ask.
5. Closes Steam for a few seconds, sets Stardew Valley's launch option to DewDrop's launcher, then starts Steam again. The Deck's controls and on-screen keyboard stop while Steam is closed, so every question comes before this.

It doesn't offer to start the game: on the Deck, that's for Game Mode.
{% endstep %}

{% step %}
### Go back to Game Mode

When the installer says to switch back to Game Mode, double-click **Return to Gaming Mode** on the desktop. Start Stardew Valley and press **Mods** on the title screen.

If it says instead that Steam isn't set to start the game with DewDrop's launcher yet, read the messages above that, then run the installer again.
{% endstep %}
{% endstepper %}

## Playing in Game Mode

* **The Mods menu** works with the Deck's controls. The left stick moves the cursor (so does the right trackpad, if your controller layout uses it as a mouse), **A** clicks, **L1** and **R1** (the bumpers) switch tabs, and **B** closes a dialog or the menu.
* **Typing** in a search box or a profile name brings up Steam's on-screen keyboard, or the game's own if Steam's can't open.
* **Web pages**, such as **Open on Nexus** or **Get my API key**, open in Steam's overlay browser over the game. Close it to go back to the game.
* **Open mods folder** on the **Downloads** tab is grayed out, because Game Mode has no file manager. It works in Desktop Mode.
* **Apply & Restart** closes the game and starts it again in your profile, through the same launcher Steam uses. It's built to stay in Game Mode and in the same Steam session.
* **Start in my profile from Steam** (on the **Account** tab) makes **Play** start the game in your profile. It takes effect as soon as you change it: Steam doesn't have to close. If you said yes in the installer, the first start still loads your whole `Mods` folder, because your profile isn't linked yet. After you quit once, **Play** starts in your profile. While the setting is off, the **Load your profile?** box offers it too, as a **Start in my profile from Steam from now on** checkbox.
* **Game updates don't turn mods off.** Normally a Stardew Valley update undoes SMAPI's setup until you run SMAPI's installer again. DewDrop's launcher starts SMAPI directly, so mods keep loading. You still need a SMAPI version that supports the new game version, as on any system.

Free Nexus Mods accounts can't use the website's **Mod manager download** button with DewDrop on the Deck yet, and there's no SMAPI console to paste its links into. Install those mods by hand, in Desktop Mode. Premium accounts install straight from the game. See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md#free-accounts-on-macos-and-linux).

## Updating DewDrop

Switch to Desktop Mode and run the installer again, the same way. It updates DewDrop and keeps your settings, and since Steam is already set up, it doesn't close Steam this time. [Updating DewDrop](updating-dewdrop.md#on-a-steam-deck) has the steps.

## What the installer changes

**Steam's launch option for Stardew Valley** becomes:

```text
"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
```

* Launch options that use `%command%`, such as `gamemoderun %command%`, are kept: the launcher goes just before `%command%`. The installer shows what they'll become and asks first.
* Launch options that start SMAPI directly get the launcher in SMAPI's place. What comes before SMAPI (such as `gamemoderun`) and after `%command%` is kept, but SMAPI's own options, such as `--mods-path`, go. The installer shows what they'll become, names any SMAPI options that go, and asks first. An option that only starts SMAPI is replaced without asking.
* Other launch options are only replaced if you say so (the recommended answer is no).
* Steam's settings file is backed up first, next to it, as `localconfig.vdf.dewdrop-backup-<date>-<time>`. Only the Steam account that's signed in is changed.

**DewDrop's launcher**, `steam-launch.sh`, goes in DewDrop's data folder. Steam runs it in place of the game's own launcher, which would try to open a terminal window that Game Mode can't show. The launcher starts SMAPI with no window, on your profile when you've asked for that and your profile is linked, and on your whole `Mods` folder otherwise. DewDrop keeps the launcher up to date itself.

Because SMAPI has no window, there's no console to type [console commands](../reference/console-commands.md) in, in Game Mode or Desktop Mode. The [SMAPI log](#if-something-goes-wrong) has what SMAPI and DewDrop said.

To undo it:

1. In Steam, open Stardew Valley's **Properties** and find **Launch Options** on the **General** page.
2. Delete `"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh"` and keep anything else, such as `gamemoderun`. If only `%command%` is left, clear the box.

The game then starts through SMAPI's own launcher, which may show a black screen or a terminal window in Game Mode. **Start in my profile from Steam** stops working, and **Apply & Restart** restarts the game through SMAPI's launcher instead.

## If something goes wrong

Two logs say what happened. Open them from Desktop Mode, in **Dolphin**. Press **Ctrl+H** to see hidden folders such as `.config`.

* **`steam-launch.log`**, in `/home/deck/.config/Stardrop/Data/Cache/DewDrop/`: each time Steam or a restart starts the game, which folder the launcher had SMAPI load, and why. [Where DewDrop keeps files](../reference/file-locations.md#logs) explains its lines.
* **SMAPI's log**, `/home/deck/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`: everything DewDrop and SMAPI reported. DewDrop's line with `Linux: SteamOS` in it says whether DewDrop saw a Steam Deck, Game Mode and its launcher.

| What you see | What to try |
| --- | --- |
| The game shows a black screen or a terminal window in Game Mode | Steam isn't using DewDrop's launcher. Run the installer again in Desktop Mode, and check Stardew Valley's launch options in Steam. |
| **Start in my profile from Steam** is grayed out | Hover over it to see why. Usually the installer hasn't set Steam up yet: run it again in Desktop Mode. |
| The game doesn't come back after **Apply & Restart** | Press **Play** again. If the restart never got as far as starting the game, this launch opens in your profile instead. Then [report it](../help/reporting-issues.md) with `steam-launch.log` and the SMAPI log. |
| Typing opens the game's own keyboard, not Steam's | That's DewDrop's fallback, and typing still works. Please [report it](../help/reporting-issues.md). |
| The installer says "Steam didn't close." | Close Steam yourself (right-click its icon in the taskbar and choose **Exit**), then run the installer again. |
| The installer says "Steam saved different launch options as it closed" | Steam changed the game's launch options as it closed, so the installer left them alone. Run the installer again to add DewDrop's launcher to them. |
