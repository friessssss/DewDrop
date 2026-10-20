---
description: "What works on Windows, macOS, Linux, Steam Deck and the Xbox app, and what hasn't been tested yet."
icon: desktop
---

# Platform support

DewDrop is built and tested on Windows. Its code is written to work on macOS and Linux too, but nobody has tested the mod there yet, and a few features are Windows only. Steam Deck support is new in DewDrop 0.2.0 and being tested on a real Deck. This page lists what's tested, what's untested, and what doesn't work.

## Requirements

* **Stardew Valley 1.6 or later**, the PC version.
* **SMAPI 4.0 or later.** DewDrop has been tested with SMAPI 4.5.2 and Stardew Valley 1.6.15. Newer SMAPI versions need newer game versions (SMAPI 4.5.2 needs Stardew Valley 1.6.14 or later), so keep both up to date.
* DewDrop is a SMAPI mod, so it only runs where SMAPI runs: on Windows, macOS and Linux computers, including the Steam Deck. Consoles and phones aren't supported.
* The menus are in English only for now.

See [What you need](../getting-started/requirements.md) for the beginner-friendly version.

## At a glance

| System | Status |
| --- | --- |
| Windows | **Supported.** This is where DewDrop is built and tested. |
| Xbox app (PC Game Pass) | **Supported**, using SMAPI's exe swap, which the installer can do for you. It runs the Windows version of the game, so DewDrop behaves as on Windows. The Xbox setup itself has had less testing. |
| macOS | **Untested.** The installer works. The mod is built to work, except website download links for free Nexus Mods accounts and **Start in my profile from Steam**. |
| Linux | **Untested.** The installer works. The mod is built to work, except website download links for free Nexus Mods accounts. **Start in my profile from Steam** needs DewDrop's Steam launcher (installer option `--steam-setup`). |
| Steam Deck | **New, being tested.** Set up once in Desktop Mode with the installer, then everything happens in Game Mode. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md). |
| Windows version through Proton or Wine | **Not supported.** The installer warns you if your game is set up this way. |

## Feature by feature

In this table, **Yes** means it works and has been tested. **Untested** means the code supports it but nobody has confirmed it on that system yet. **No** means it isn't available.

| Feature | Windows | macOS | Linux | Steam Deck | Xbox app |
| --- | --- | --- | --- | --- | --- |
| [Installer](../getting-started/installing/installer.md) | Yes | Yes (tested on an Apple Silicon Mac) | Yes | Being tested; also sets up DewDrop's Steam launcher | Untested |
| Starting the game with SMAPI | Installer sets the Steam launch option, or adds a desktop shortcut | SMAPI's installer sets it up; nothing to do | SMAPI's installer sets it up; nothing to do (or DewDrop's Steam launcher, with `--steam-setup`) | Installer sets the Steam launch option to DewDrop's launcher, which starts SMAPI with no terminal window | Installer offers SMAPI's exe swap |
| **Mods** button, menu and profiles | Yes | Untested | Untested | Being tested; Steam's keyboard for typing in Game Mode | Same as Windows |
| Linking mods for **Apply & Restart** | Yes (junctions) | Untested (symbolic links) | Untested (symbolic links) | Untested | Same as Windows |
| Restarting the game after **Apply & Restart** | Yes | Untested | Untested | Being tested: through DewDrop's launcher, staying in Game Mode | Untested |
| [Start in my profile from Steam](../guides/start-in-your-profile.md) | Yes, for the Steam version | No | Untested, with DewDrop's Steam launcher | Being tested | No |
| [**Load your profile?** box](../guides/start-in-your-profile.md#the-load-your-profile-box) | Yes | Untested | Untested | Being tested | Same as Windows; the restart is untested |
| Browsing Nexus Mods | Yes | Untested | Untested | Untested | Same as Windows |
| Premium: **Install** downloads in the game | Yes | Untested | Untested | Untested | Same as Windows |
| Free: website download links (**Use DewDrop for these links**) | Yes | No | No | No | Same as Windows |
| [`dewdrop_nxm`](console-commands.md) console command | Yes | Untested | Untested; not with DewDrop's Steam launcher, which has no console | No: DewDrop's launcher has no console | Same as Windows |
| Mod update checks | Yes | Untested | Untested | Untested | Same as Windows |
| Updating a mod the game has loaded | Finishes on **Apply & Restart**, or when you quit the game | Goes in straight away (untested) | Goes in straight away (untested) | Goes in straight away (untested) | Same as Windows |
| [Uninstalling a mod](../guides/installed.md#uninstall-a-mod) | Yes | Untested | Untested | Untested | Same as Windows |
| **Open mods folder** and website buttons | Yes | Untested | Untested (needs `xdg-open`) | Websites open in Steam's overlay in Game Mode; **Open mods folder** only in Desktop Mode | Same as Windows |
| Sharing profiles and login with Stardrop | Yes | Untested | Untested | Untested | Same as Windows |

## Windows

All of DewDrop's features are available on Windows, and it's where DewDrop is tested.

* **Steam:** the installer can set Steam's launch option so Steam starts SMAPI. It changes only the Steam account that's signed in, closes Steam first and backs up Steam's settings file. If you'd rather do it yourself, it copies the option for you and shows the steps. It also asks whether Steam should start the game in your DewDrop profile.
* **GOG Galaxy:** the installer can add a "Stardew Valley (SMAPI)" desktop shortcut. To make Galaxy's **Play** button start SMAPI, follow [SMAPI's guide for GOG Galaxy](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#GOG_Galaxy).
* **A game under Program Files** (some GOG installs): Windows protects that folder, so the installer offers to restart itself as administrator. Windows asks for permission, and the installer carries on in a new window.
* **Start in my profile from Steam** (Steam version only) makes Steam's **Play** button start the game in your profile. DewDrop changes the launch option when you quit the game, which closes Steam and opens it again once. With GOG Galaxy and the Xbox app, the **Load your profile?** box and **Apply & Restart** get you into your profile instead. See [Start in your profile](../guides/start-in-your-profile.md).
* **Only on Windows:** website download links for free accounts, the `relaunch.log` restart log, and updates that wait for **Apply & Restart** or for you to quit the game (other systems don't need them).

Not tested yet: **Apply & Restart** and **Restart now** start SMAPI directly rather than through Steam. Whether Steam counts playtime, shows its overlay or awards achievements in a session DewDrop restarted hasn't been checked. With **Start in my profile from Steam**, Steam itself starts the game, so this doesn't apply.

## Xbox app

The Xbox app (and PC Game Pass) always starts `Stardew Valley.exe`. SMAPI's guide for the Xbox app gets around this with an **exe swap**: the real `Stardew Valley.exe` is renamed `Stardew Valley original.exe`, and a copy of SMAPI takes its name. The installer offers to do this for you.

* **Redo it after every game update**, because the update replaces SMAPI's copy with the new game. Run the installer again: the updated `Stardew Valley.exe` becomes the new `Stardew Valley original.exe`, and SMAPI's copy takes its name again.
* The game folder is usually `C:\XboxGames\Stardew Valley\Content`.
* **Apply & Restart** and **Restart now** start `StardewModdingAPI.exe` from that folder directly, not through the Xbox app. This hasn't been tested.
* **Start in my profile from Steam** is only for the Steam version, so its row on the **Account** tab is grayed out.
* Windows can refuse changes to files in the Xbox app's game folder. If the installer says Windows didn't allow the exe swap, follow [SMAPI's Xbox app guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app) to do it by hand.

## macOS

DewDrop's code supports macOS, but the mod hasn't been tested there yet. The installer has, on a Mac with an Apple chip.

* **Free Nexus Mods accounts can't use website download links.** **Get on Nexus** opens the file on the website, and DewDrop tells you to add the mod to your `Mods` folder yourself. See the warning below.
* **Apply & Restart** starts `StardewModdingAPI` in your game folder directly. You probably won't get a Terminal window for SMAPI in that session, so you can't type [console commands](console-commands.md) until you start the game the usual way. Whether macOS shows security prompts for a game started this way hasn't been tested.
* The **Load your profile?** box works as on Windows, and its **Restart now** uses the same untested restart as **Apply & Restart**.
* **Start in my profile from Steam isn't available.** SMAPI's macOS launcher doesn't pass Steam's launch options on to SMAPI, so DewDrop can't use them. The row on the **Account** tab is grayed out. (On Linux, DewDrop's Steam launcher gets around this; there's no macOS version of it.)
* **The downloaded installer** may be blocked by Gatekeeper, macOS's check for apps from the internet, the first time you open it. [The one line](../getting-started/installing/installer.md#the-quick-way-one-line) pasted into Terminal isn't blocked; [Install with the installer](../getting-started/installing/installer.md) also shows how to allow the download.
* **Macs with an Apple chip (M1 or later) need Rosetta**, Apple's free add-on for Intel apps, because SMAPI's installer is made for Intel Macs. The installer offers to install it ("Install Rosetta now?"), which asks for your Mac password. To install it yourself, run this in Terminal:

  ```bash
  softwareupdate --install-rosetta
  ```

* SMAPI needs macOS 10.15 Catalina or later.
* DewDrop's data folder is `~/.config/Stardrop/Data`. See [Where DewDrop keeps files](file-locations.md).

## Linux

DewDrop's code supports Linux, but the mod hasn't been tested there yet. The installer has.

* **Free Nexus Mods accounts can't use website download links**, the same as on macOS.
* **DewDrop's Steam launcher** is optional on Linux. Run the installer with `--steam-setup` and it sets Steam to start the game through the launcher, as on a [Steam Deck](#steam-deck). See [Steam on Linux and the Steam Deck](installer-options.md#steam-on-linux-and-the-steam-deck). With it, SMAPI has no terminal window, a game update doesn't turn mods off, and **Start in my profile from Steam** works. This hasn't been tested outside SteamOS.
* **Apply & Restart** with the launcher starts the game again through it, and `steam-launch.log` records each restart. Without it, the restart runs the game's `StardewValley` file, which SMAPI's installer replaced with SMAPI's own launcher. If a game update puts the original back, the restart starts the game without SMAPI, and so without mods. Run SMAPI's own installer again to fix it; the DewDrop installer skips SMAPI when it's already up to date. See [About SMAPI](../getting-started/installing/smapi.md#macos-and-linux).
* **Start in my profile from Steam** needs the launcher. Without it, the row on the **Account** tab is grayed out, and hovering over it usually says to run the installer again with `--steam-setup`. With it, the setting changes at once, and Steam never closes for it. A `--mods-path` you add to Steam's launch options yourself doesn't count.
* The **Load your profile?** box works as on macOS. With the launcher, it also offers **Start in my profile from Steam**, as on Windows.
* **With the launcher, there's no SMAPI console**, so you can't type [console commands](console-commands.md). The SMAPI log, `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`, has what SMAPI and DewDrop said.
* The **Open mods folder** button and the buttons that open websites use your desktop's default apps, through `xdg-open`.
* DewDrop's data folder is `~/.config/Stardrop/Data`.

<details>

<summary>A rare Linux restart problem (untested)</summary>

SMAPI's launcher opens a terminal window to run in. With a few terminal programs, the generic `terminal` command and `termite`, it joins the restart's settings into one line of text. The space in the `Selected Mods` folder name could then break the restart. If **Apply & Restart** starts the game without your profile on Linux, this may be why. Most desktops use other terminals and aren't affected, and DewDrop's Steam launcher doesn't use a terminal at all.

</details>

{% hint style="warning" %}
**macOS and Linux, free accounts:** install mods by hand, or paste the website's `nxm://` link into the SMAPI console with `dewdrop_nxm`. When Steam starts the game with DewDrop's Steam launcher (on a Steam Deck, or on Linux after `--steam-setup`), there's no SMAPI console, so install by hand. [Free accounts on macOS and Linux](../nexus-mods/free-vs-premium.md#free-accounts-on-macos-and-linux) has the steps. Premium accounts don't need this; **Install** works directly.
{% endhint %}

## Steam Deck

Steam Deck support is new in DewDrop 0.2.0, and being tested on a real Deck now. [DewDrop on the Steam Deck](../getting-started/steam-deck.md) has the setup steps.

* **Set up once, in Desktop Mode.** The installer installs SMAPI and DewDrop, can take your Nexus Mods API key, and sets Stardew Valley's launch option in Steam to DewDrop's launcher. After that, everything happens in Game Mode.
* **DewDrop's launcher** starts SMAPI in place of the game's own launcher, which would open a terminal window Game Mode can't show. It keeps the game inside Steam's runtime, and starts it in your profile when you've asked for that. Because it starts SMAPI directly, a game update doesn't turn mods off.
* **There's no SMAPI console window**, in Game Mode or Desktop Mode, so [console commands](console-commands.md) such as `dewdrop_nxm` can't be typed. The SMAPI log has what SMAPI and DewDrop said.
* **Apply & Restart** and **Restart now** start the game again through the same launcher, so the restart is built to stay in Game Mode and in the same Steam session. Whether the game window comes back in Game Mode is still being checked.
* **Start in my profile from Steam** works, and changes at once: Steam doesn't have to close.
* **In Game Mode**, web pages open in Steam's overlay, text boxes bring up Steam's keyboard, and **Open mods folder** is grayed out. Whether the overlay and keyboard open on a real Deck is still being checked.
* **Free Nexus Mods accounts** can't use website download links on the Deck yet, as on Linux, and there's no console for `dewdrop_nxm`: install those mods by hand, in Desktop Mode.
* The Deck runs the Linux version of the game. In Desktop Mode, DewDrop behaves as on Linux, with the launcher.

## The Windows version through Proton or Wine

Not supported. Proton is Steam's tool for running Windows games on Linux. SMAPI for Linux can't load mods into the Windows version, and **Apply & Restart** relies on Windows' PowerShell and `mklink` command, which probably don't work under Proton or Wine. For the same reason, the **Load your profile?** box doesn't appear there, and **Start in my profile from Steam** is grayed out. On Linux and the Steam Deck, use the Linux version of the game instead.

If your game folder holds the Windows version (`Stardew Valley.exe` and no Linux launcher), the installer warns you and stops unless you answer **Y**. To switch to the Linux version:

1. In Steam, right-click Stardew Valley and choose **Properties**.
2. Open **Compatibility** and untick **Force the use of a specific Steam Play compatibility tool**.
3. Let Steam download the Linux version, then run the installer again.

{% hint style="info" %}
Using DewDrop on macOS, Linux or a Steam Deck? Reports help, whether it works or not. See [Reporting issues](../help/reporting-issues.md).
{% endhint %}
