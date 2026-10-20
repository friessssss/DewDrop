---
description: "Set DewDrop up once in Desktop Mode, then install, update and switch mods from the game in Game Mode."
icon: gamepad
---

# DewDrop on the Steam Deck

Modding Stardew Valley on a Steam Deck usually means going back to Desktop Mode every time you want to add or change a mod. With DewDrop, you go to Desktop Mode **once**, to run the installer. After that, you browse, install, update and turn mods on and off from the **Mods** button on the title screen, in Game Mode.

{% hint style="warning" %}
Steam Deck support is new and still being tested. If something doesn't work, the [logs below](#if-something-goes-wrong) say why. Please [report it](../help/reporting-issues.md), whether it works or not.
{% endhint %}

## Set up once, in Desktop Mode

{% stepper %}
{% step %}
### Start the game once without mods

If you've never played Stardew Valley on this Deck, start it once in Game Mode, wait for the title screen, then quit. SMAPI, the mod loader DewDrop runs on, asks for this.
{% endstep %}

{% step %}
### Switch to Desktop Mode

Press the **Steam** button, open **Power** and choose **Switch to Desktop**.
{% endstep %}

{% step %}
### Run the installer

Open **Konsole** from the app launcher in the bottom-left corner. Paste this line (right-click and choose **Paste**, or press **Ctrl + Shift + V**) and press **Enter**:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
```

You can also download `DewDrop-<version>-installer.zip` from the [latest release](https://github.com/friessssss/DewDrop/releases/latest), extract it in **Dolphin**, and double-click `Install on Linux.sh`.
{% endstep %}

{% step %}
### Answer the questions

Press **Enter** at a question to pick the recommended answer. On the Deck, the installer:

* installs SMAPI if you don't have it, and DewDrop;
* **sets Stardew Valley's launch option in Steam** to DewDrop's launcher. Steam closes for a moment while it does this, and opens again afterwards. See [What the installer changes](#what-the-installer-changes);
* asks whether **Play** should start the game in your DewDrop profile (recommended: yes);
* offers to take your **Nexus Mods API key**. Typing a key with the on-screen keyboard is slow, so paste it here instead. Open [your Nexus Mods account's API page](https://www.nexusmods.com/users/myaccount?tab=api) in the browser, copy the **Personal API Key** at the bottom, and paste it into Konsole. DewDrop logs in with it the next time the game starts. Press **Enter** without a key to skip this.
{% endstep %}

{% step %}
### Go back to Game Mode

Double-click **Return to Gaming Mode** on the desktop. Start Stardew Valley and press **Mods** on the title screen.
{% endstep %}
{% endstepper %}

## Playing in Game Mode

* **The Mods menu** works with the Deck's controls: the left stick moves the cursor (so does the right trackpad, if your controller layout uses it as a mouse), **A** clicks, **L1** and **R1** switch tabs, and **B** closes a dialog or the menu.
* **Typing**, in a search box or a profile name, brings up Steam's on-screen keyboard (or the game's own, if Steam's can't open).
* **Apply & Restart** closes the game and starts it again in your profile, without leaving Game Mode. It goes through the same launcher Steam uses, so Steam still counts it as the same game.
* **Start in my profile from Steam** (on the **Account** tab) makes **Play** start the game in your profile. On the Deck, changing it takes effect at once: Steam doesn't have to close.
* **Web pages**, such as **Open on Nexus** or **Get API key**, open in Steam's overlay browser over the game. Close it to go back to the game.
* **Open mods folder** is grayed out in Game Mode, which has no file manager. It works in Desktop Mode.
* **Game updates don't turn mods off.** Normally a Stardew Valley update undoes SMAPI's setup until you run SMAPI's installer again. DewDrop's launcher starts SMAPI directly, so mods keep loading. You still need a SMAPI version that supports the new game version, as on any system.

Free Nexus Mods accounts can't use the website's **Mod manager download** button with DewDrop on the Deck yet. Premium accounts install straight from the game. See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md).

## What the installer changes

* **Steam's launch option for Stardew Valley** becomes:

  ```
  "/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
  ```

  If you already had launch options that use `%command%`, such as `gamemoderun %command%`, the launcher is added just before `%command%` and the rest is kept. Steam's settings file is backed up first, next to it, as `localconfig.vdf.dewdrop-backup-<date>-<time>`. Only the Steam account that's signed in is changed.
* **DewDrop's launcher**, `steam-launch.sh`, goes in DewDrop's data folder. It starts SMAPI in place of the game's own launcher, which would try to open a terminal window that Game Mode can't show. It starts SMAPI in your profile when you asked for that and your profile is ready, and with your whole `Mods` folder otherwise. DewDrop keeps the launcher up to date itself.

To undo it, open Stardew Valley's **Properties** in Steam and clear **Launch Options**. Mods still load, through SMAPI's own launcher, but **Apply & Restart** and **Start in my profile from Steam** need DewDrop's launcher.

## If something goes wrong

These files are in `/home/deck/.config/Stardrop/Data/Cache/DewDrop/`. Open them from Desktop Mode, in **Dolphin** (press **Ctrl + H** to see hidden folders like `.config`):

* **`steam-launch.log`**: each time Steam (or a restart) starts the game, what the launcher did: which folder SMAPI loaded, and why.
* **SMAPI's log**, `/home/deck/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`: everything DewDrop and SMAPI reported. It includes a line saying whether DewDrop saw a Steam Deck, Game Mode and its launcher.

| What you see | What to try |
| --- | --- |
| The game shows a black screen or a terminal window in Game Mode | Steam isn't using DewDrop's launcher. Run the installer again in Desktop Mode, and check Stardew Valley's launch options in Steam. |
| **Start in my profile from Steam** is grayed out | Hover over it to see why. Usually the installer hasn't set Steam up yet: run it again in Desktop Mode. |
| The game doesn't come back after **Apply & Restart** | Press **Play** again. If the restart never got as far as starting the game, this launch opens in your profile instead. Then [report it](../help/reporting-issues.md) with `steam-launch.log` and the SMAPI log. |
| The installer says Steam didn't close | Close Steam yourself (right-click its icon in the taskbar and choose **Exit**), then run the installer again. |
