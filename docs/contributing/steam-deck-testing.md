---
description: "A checklist for trying DewDrop's Steam Deck support on a real Deck, and what to send back."
icon: list-check
---

# Testing on a Steam Deck

DewDrop's Steam Deck support was written against SMAPI's and Steam's source code, not on a Deck. This checklist walks through everything that needs a real one. Note what happens at each step, and keep the files listed in [What to send back](#what-to-send-back).

## How it's meant to work

* **Setup (Desktop Mode, once):** the installer installs SMAPI and DewDrop, copies DewDrop's launcher (`steam-launch.sh`) into `~/.config/Stardrop/Data/Cache/DewDrop/`, and sets Stardew Valley's Steam launch option to `"<launcher>" %command%`. Steam has to be closed for that, so the installer closes it and starts it again.
* **Every launch:** Steam runs the launcher with the command it would have run. The launcher swaps the game's `StardewValley` (SMAPI's terminal-opening launcher, after SMAPI is installed) for `StardewModdingAPI --no-terminal`, adds `--mods-path "<Selected Mods>"` when the game should start in the profile, and runs the rest of Steam's command unchanged, so the game stays inside Steam's runtime.
* **Apply & Restart:** DewDrop writes a `restart` file with the restart's ID and starts a small waiter. When the game has closed, the waiter runs the launcher again. It stays inside Steam's process tree, so Steam should keep counting the game as running and Game Mode should show the new window.
* **Start in my profile from Steam:** a `start-in-profile` file beside the launcher. Changing the setting in the game takes effect at once.
* **Game Mode:** web pages open in Steam's overlay (Steamworks' `ActivateGameOverlayToWebPage`), and text boxes open Steam's floating keyboard (`ShowFloatingGamepadTextInput`). Both are called by reflection, with fallbacks.

## Getting a test build onto the Deck

Until this is in a release, the installer's download is the last release, which doesn't have the launcher. Use a build of the branch instead:

1. On GitHub, open **Actions** > **Build** > **Run workflow**, and pick the branch.
2. When it finishes, download the **DewDrop-zips** artifact on the Deck, in Desktop Mode (you need to be signed in to GitHub).
3. Extract it, then extract `DewDrop-<version>-installer.zip` inside it, and double-click `Install on Linux.sh` in the `DewDrop <version> installer` folder. It installs the DewDrop bundled with it.

## The checklist

### Setup in Desktop Mode

- [ ] The installer finds the game, including on a microSD card if that's where it is.
- [ ] Every question (including the Nexus key) comes **before** Steam closes, and the installer never waits for an answer while Steam is closed. It doesn't ask to start the game at the end.
- [ ] It sets the launch option, and **Steam closes and opens again within a few seconds**, bringing back the controls and the on-screen keyboard. Note if Steam started again by itself straight away: the installer then says it's stopping Steam's service instead.
- [ ] In Steam, Stardew Valley's **Properties** > **Launch Options** shows `"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%`.
- [ ] Pasting a Nexus API key into Konsole works (**Ctrl + Shift + V**).
- [ ] Run the installer again: it says Steam already starts the game with DewDrop's launcher, and doesn't close Steam.

### First start in Game Mode

- [ ] The game starts and shows the title screen, **with no terminal window and no black screen**.
- [ ] The **Mods** button is there.
- [ ] A message says you're logged in to Nexus Mods with the key from the installer (if you gave one).
- [ ] `steam-launch.log` says "Starting SMAPI on the Mods folder" (the profile isn't linked yet on the first start).
- [ ] The SMAPI log has the line `Linux: SteamOS True, Game Mode True, DewDrop's Steam launcher in use.`

### The menu with the Deck's controls

- [ ] The left stick moves the cursor, **A** clicks, **L1** / **R1** switch tabs, **B** closes the menu.
- [ ] The right trackpad works as a mouse (with Stardew Valley's default controller layout).
- [ ] Clicking the search box on **Installed** or **Browse** opens **Steam's** keyboard, and typing goes into the box. If the game's own keyboard opens instead, note that.
- [ ] The menu fits the screen at 1280×800; nothing is cut off on any tab.

### Installing and switching mods

- [ ] Browse Nexus Mods, open a mod, and install a file (Premium account), or turn a mod off on **Installed**.
- [ ] **Open on Nexus** opens the page in Steam's overlay browser, and closing it goes back to the game.
- [ ] **Open mods folder** on **Downloads** is grayed out, and its tooltip explains why.

### Apply & Restart (the important one)

- [ ] Click **Apply & Restart** and confirm. The game closes and **comes back by itself**, still in Game Mode.
- [ ] Note what shows while it restarts: Steam's library, a black screen, the old frame? How long does it take?
- [ ] Back on the title screen, the mods you turned off aren't loaded and the ones you installed are.
- [ ] `steam-launch.log` says "Starting SMAPI on .../Selected Mods (restarting into the profile)".
- [ ] Steam still shows the game as running, and the Steam button's overlay works in the restarted game.
- [ ] Quit the game normally. Steam shows it as closed (no "Running" left over).

### Start in my profile from Steam

- [ ] On **Account**, the setting isn't grayed out, and changing it shows no "Steam closes" message.
- [ ] With it on: turn a mod off, quit, press **Play**. The game starts in your profile (that mod isn't loaded), with no restart.
- [ ] With it off: **Play** loads the whole `Mods` folder, and the **Load your profile?** box offers a restart if your profile differs.

### Other things worth a try

- [ ] **Desktop Mode:** start the game from Steam in Desktop Mode. Does it run? Does **Apply & Restart** work there too?
- [ ] **A game update** (or Steam's **Verify integrity of game files**, which puts the original `StardewValley` back): mods still load from **Play**.
- [ ] **Exit game** from the Steam button's menu while DewDrop's menu is open: the game closes cleanly, and the next **Play** starts in your profile if that's on.

## What to send back

From `/home/deck/.config/Stardrop/Data/Cache/DewDrop/` (in **Dolphin**, press **Ctrl + H** to show hidden folders):

* `steam-launch.log`: every launch the launcher handled (Steam's and DewDrop's restarts), and what it chose.

And:

* The SMAPI log, `/home/deck/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`, uploaded at [smapi.io/log](https://smapi.io/log). After a restart, the log is the restarted game's; `SMAPI-crash.txt` beside it is there if the game crashed.
* `/tmp/DewDrop-installer.log` from the installer's run.
* For each unticked box, what you saw instead. A photo of the screen helps for anything visual.
