---
description: "A checklist for trying DewDrop's Steam Deck support on a real Deck, and what to send back."
icon: list-check
---

# Testing on a Steam Deck

DewDrop's Steam Deck support ([pull request #11](https://github.com/friessssss/DewDrop/pull/11)) was written against SMAPI's and Steam's source code and tested with stand-ins, not on a Deck. A first try on a real Deck covered installing. This checklist walks through everything else that needs a real one. Note what happens at each step, and keep the files listed in [What to send back](#what-to-send-back).

The two open questions are whether the game comes back in Game Mode after **Apply & Restart**, and whether Steam's overlay browser and keyboard open from the game.

## How it's meant to work

* **Setup, once, in Desktop Mode.** The installer installs SMAPI and DewDrop and asks for a Nexus Mods API key, unless one is saved already. It copies DewDrop's launcher, `steam-launch.sh`, into `~/.config/Stardrop/Data/Cache/DewDrop/`, and sets Stardew Valley's launch option in Steam to `"<launcher>" %command%`. Steam has to be closed for that, so after every question has been answered, the installer closes Steam and starts it again straight after the change. If Steam comes back by itself, the installer stops its systemd user service instead, and starts that again afterwards.
* **Every launch.** Steam runs the launcher with the command it would have run. The launcher swaps the game's `StardewValley` (which SMAPI's installer turns into a launcher that opens a terminal) for `StardewModdingAPI --no-terminal`. It adds `--mods-path "<Selected Mods>"` when the game should start in the profile, and runs the rest of Steam's command unchanged, so the game stays inside Steam's runtime. There's never a SMAPI console window, in Game Mode or Desktop Mode, so console commands can't be typed: the SMAPI log is where to look.
* **Apply & Restart.** DewDrop writes a `restart` file with the restart's ID and starts a small waiter. Once the game has closed, the waiter runs the launcher again. It stays inside Steam's process tree, so Steam should keep counting the game as running, and Game Mode should show the new window.
* **Start in my profile from Steam.** The setting is a `start-in-profile` file beside the launcher. Changing it in the game takes effect at once, and Steam never closes.
* **Game Mode.** Web pages open in Steam's overlay (Steamworks' `ActivateGameOverlayToWebPage`), and text boxes open Steam's floating keyboard (`ShowFloatingGamepadTextInput`). Both are called by reflection, with fallbacks.

## Get a test build onto the Deck

Until DewDrop 0.2.0 is released, the one-line installer doesn't work yet, and the 0.1.0 release has no Steam Deck support. A newer installer without its own copy of DewDrop downloads 0.1.0 and warns "This DewDrop has no Steam launcher (assets/steam-launch.sh); it needs a newer DewDrop." Use a build of the pull request instead, which bundles its own DewDrop:

1. Do this on the Deck, in Desktop Mode. On GitHub, open the pull request's **Checks** tab and the latest **Build** run. To make a fresh build of a branch, open **Actions** > **Build** > **Run workflow** and pick the branch.
2. On the run's summary page, download the **DewDrop-zips** artifact. You need to be signed in to GitHub.
3. Extract it. Inside a folder named after the version, extract `DewDrop-<version>-installer.zip`.
4. Double-click `Install on Linux.sh` in the `DewDrop <version> installer` folder. It installs the DewDrop bundled with it.

Until the version is bumped for 0.2.0, a test build still calls itself 0.1.0. That's expected: what matters is that the installer uses its bundled copy and doesn't say "No bundled copy of DewDrop".

From 0.2.0 on, the one-line installer is enough, as [DewDrop on the Steam Deck](../getting-started/steam-deck.md) shows.

## The checklist

### Setup in Desktop Mode

- [ ] The installer finds the game, including on a microSD card if that's where it is.
- [ ] Every question, including the Nexus key, comes **before** Steam closes, and nothing waits for an answer while Steam is closed. It doesn't offer to start the game at the end.
- [ ] It sets the launch option, and **Steam closes and opens again within a few seconds**, bringing back the controls and the on-screen keyboard. Note if Steam started again by itself straight away: the installer then says it's stopping Steam's service instead.
- [ ] In Steam, Stardew Valley's **Properties** > **General** > **Launch Options** shows:

  ```text
  "/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
  ```
- [ ] Pasting a Nexus API key into Konsole works (**Ctrl+Shift+V**).
- [ ] Run the installer again. It says "Steam already starts the game with DewDrop's launcher." and doesn't close Steam. Once the game has saved a Nexus key, it doesn't ask for one again.

### First start in Game Mode

- [ ] The game starts and shows the title screen, **with no terminal window and no black screen**.
- [ ] The **Mods** button is there.
- [ ] If you gave the installer a key, a message says "Logged in to Nexus Mods as *name* with the key from the installer."
- [ ] `steam-launch.log` says "Starting SMAPI on the Mods folder". The profile isn't linked yet on the first start.
- [ ] The SMAPI log has the line `Linux: SteamOS True, Game Mode True, DewDrop's Steam launcher in use.`

### The menu with the Deck's controls

- [ ] The left stick moves the cursor, **A** clicks, **L1** and **R1** switch tabs, and **B** closes the menu.
- [ ] The right trackpad works as a mouse, with Stardew Valley's default controller layout.
- [ ] Clicking the search box on **Installed** or **Browse** opens **Steam's** keyboard, and typing goes into the box. If the game's own keyboard opens instead, note that.
- [ ] The menu fits the screen at 1280×800, and nothing is cut off on any tab.

### Installing and switching mods

- [ ] Browse Nexus Mods, open a mod, and install a file (with a Premium account), or turn a mod off on **Installed**.
- [ ] **Open on Nexus** opens the page in Steam's overlay browser, and closing the overlay goes back to the game.
- [ ] **Open mods folder** on **Downloads** is grayed out, and its tooltip says "Not in Game Mode: there's no file manager. The folder opens from Desktop Mode."

### Apply & Restart (the important one)

- [ ] Click **Apply & Restart** and confirm. The game closes and **comes back by itself**, still in Game Mode.
- [ ] Note what shows while it restarts: Steam's library, a black screen, the old frame? How long does it take?
- [ ] Back on the title screen, the mods you turned off aren't loaded and the ones you installed are.
- [ ] `steam-launch.log` says "Starting SMAPI on .../Selected Mods (restarting into the profile)".
- [ ] Steam still shows the game as running, and the Steam button's overlay works in the restarted game.
- [ ] Quit the game normally. Steam shows it as closed, with no "Running" left over.

If the game doesn't come back by itself, press **Play**: the restart is still waiting, so that launch opens in your profile. Note this, since it's what the restart should have done.

### Start in my profile from Steam

- [ ] On **Account**, the setting isn't grayed out, and changing it says nothing about Steam closing.
- [ ] With it on: turn a mod off, quit, and press **Play**. The game starts in your profile (that mod isn't loaded), with no restart.
- [ ] With it off: **Play** loads the whole `Mods` folder, and the **Load your profile?** box offers a restart if your profile differs. The box has a ticked **Start in my profile from Steam from now on** checkbox, with the note "With that ticked, Steam's Play button starts the game in your profile from now on."
- [ ] With it off, run the installer again in Desktop Mode. It doesn't ask about starting in your profile, and the setting stays off.

### Other things worth a try

- [ ] **Desktop Mode:** start the game from Steam in Desktop Mode. Does it run, with no SMAPI terminal window, as in Game Mode? Does **Apply & Restart** work there too?
- [ ] **Launch options you already had:** before running the installer, set Stardew Valley's launch options to `gamemoderun %command%`, or to one that starts SMAPI directly with `--mods-path`. The installer shows what they become and asks first. It keeps `gamemoderun`, and names the SMAPI options it drops.
- [ ] **The installer's Nexus key, offline:** give the installer a key, then start the game with the Deck offline. A message says DewDrop tries the key from the installer again the next time the game starts, and the next start online logs in.
- [ ] **A game update**, or Steam's **Verify integrity of game files**, which puts the original `StardewValley` back: mods still load from **Play**.
- [ ] **Exit game** from the Steam button's menu while DewDrop's menu is open: the game closes cleanly, and the next **Play** starts in your profile if that's on.

## What to send back

Post what you found on [pull request #11](https://github.com/friessssss/DewDrop/pull/11), or in a [new issue](https://github.com/friessssss/DewDrop/issues). Include:

* **`steam-launch.log`**, from `/home/deck/.config/Stardrop/Data/Cache/DewDrop/`. It lists every launch the launcher handled, Steam's and DewDrop's restarts, and what it chose. In **Dolphin**, press **Ctrl+H** to show hidden folders like `.config`. Don't send anything else from that folder: `nexus-key.txt`, if it's still there, holds your Nexus API key.
* **The SMAPI log**, `/home/deck/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`, uploaded at [smapi.io/log](https://smapi.io/log). After a restart, this is the restarted game's log. If the game crashed, `SMAPI-crash.txt` is beside it.
* **The installer's log**, `/tmp/DewDrop-installer.log`.
* **For each unticked box,** what you saw instead. A photo of the screen helps for anything visual.
