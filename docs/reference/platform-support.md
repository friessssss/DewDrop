---
description: "What works on Windows, macOS, Linux, Steam Deck and the Xbox app, and what hasn't been tested yet."
icon: desktop
---

# Platform support

DewDrop is built and tested on Windows. Its code is written to work on macOS and Linux too, but nobody has tested it there yet, and a few features are Windows only. This page lists what's tested, what's untested, and what doesn't work.

## Requirements

* **Stardew Valley 1.6 or later**, the PC version.
* **SMAPI 4.0 or later.** DewDrop has been tested with SMAPI 4.5.2 and Stardew Valley 1.6.15. Newer SMAPI versions need newer game versions (SMAPI 4.5.2 needs Stardew Valley 1.6.14 or later), so keep both up to date.
* DewDrop is a SMAPI mod, so it only runs where SMAPI runs: on Windows, macOS and Linux computers. Consoles and phones aren't supported.
* The menus are in English only for now.

See [What you need](../getting-started/requirements.md) for the beginner-friendly version.

## At a glance

| System | Status |
| --- | --- |
| Windows | **Supported.** This is where DewDrop is built and tested. |
| Xbox app (PC Game Pass) | **Supported**, using SMAPI's exe swap, which the installer can do for you. It runs the Windows version of the game, so DewDrop behaves as on Windows. The Xbox setup itself has had less testing. |
| macOS | **Untested.** The installer works; the mod is built to work, except website download links for free Nexus Mods accounts and **Start in my profile from Steam**. |
| Linux | **Untested.** Built to work, with the same exception as macOS. The installer has been tested on Linux. |
| Steam Deck | **Not supported.** |
| Windows version through Proton or Wine | **Not supported.** The installer warns you if your game is set up this way. |

## Feature by feature

In this table, **Yes** means it works and has been tested. **Untested** means the code supports it but nobody has confirmed it on that system yet. **No** means it isn't available.

| Feature | Windows | macOS | Linux | Steam Deck | Xbox app |
| --- | --- | --- | --- | --- | --- |
| [Installer](../getting-started/installing/installer.md) | Yes | Yes (tested on an Apple Silicon Mac) | Yes | Untested | Untested |
| Starting the game with SMAPI | Installer sets the Steam launch option, or adds a desktop shortcut | SMAPI's installer sets it up; nothing to do | SMAPI's installer sets it up; nothing to do | Untested | Installer offers SMAPI's exe swap |
| **Mods** button, menu and profiles | Yes | Untested | Untested | Untested | Same as Windows |
| Linking mods for **Apply & Restart** | Yes (junctions) | Untested (symbolic links) | Untested (symbolic links) | Untested | Same as Windows |
| Restarting the game after **Apply & Restart** | Yes | Untested | Untested | Untested, may fail in Game Mode | Untested |
| [Start in my profile from Steam](../guides/start-in-your-profile.md) | Yes, for the Steam version | No | No | No | No |
| [**Load your profile?** box](../guides/start-in-your-profile.md) | Yes | Untested | Untested | No | Same as Windows; the restart is untested |
| Browsing Nexus Mods | Yes | Untested | Untested | Untested | Same as Windows |
| Premium: **Install** downloads in the game | Yes | Untested | Untested | Untested | Same as Windows |
| Free: website download links (**Use DewDrop for these links**) | Yes | No | No | No | Same as Windows |
| [`dewdrop_nxm`](console-commands.md) console command | Yes | Untested | Untested | Untested | Same as Windows |
| Mod update checks | Yes | Untested | Untested | Untested | Same as Windows |
| Updating a mod the game has loaded | Finishes on **Apply & Restart**, or when you quit the game | Goes in straight away (untested) | Goes in straight away (untested) | Untested | Same as Windows |
| **Open mods folder** and website buttons | Yes | Untested | Untested (needs `xdg-open`) | Untested | Same as Windows |
| Sharing profiles and login with Stardrop | Yes | Untested | Untested | Untested | Same as Windows |

## Windows

All of DewDrop's features are available on Windows, and it's where DewDrop is tested.

* **Steam:** the installer can set Steam's launch option so Steam starts SMAPI. It changes only the Steam account that's signed in, closes Steam first and backs up Steam's settings file. If you'd rather do it yourself, it copies the option for you and shows the steps. It also asks whether Steam should start the game in your DewDrop profile.
* **GOG Galaxy:** the installer can add a "Stardew Valley (SMAPI)" desktop shortcut. To make Galaxy's **Play** button start SMAPI, follow [SMAPI's guide for GOG Galaxy](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#GOG_Galaxy).
* **A game under Program Files** (some GOG installs): Windows protects that folder, so the installer offers to restart itself as administrator. Windows asks for permission, and the installer carries on in a new window.
* **Only on Windows:** website download links for free accounts, **Start in my profile from Steam**, the `relaunch.log` restart log, and updates that wait for **Apply & Restart** or for you to quit the game (other systems don't need them).
* **Start in my profile from Steam** (Steam version only) makes Steam's **Play** button start the game in your profile. DewDrop changes the launch option when you quit the game, which closes Steam and opens it again once. With GOG Galaxy and the Xbox app, the **Load your profile?** box and **Apply & Restart** get you into your profile instead. See [Start in your profile](../guides/start-in-your-profile.md).

Not tested yet: **Apply & Restart**, and **Restart now** in the **Load your profile?** box, start SMAPI directly rather than through Steam. Whether Steam counts playtime, shows its overlay or awards achievements in a session DewDrop restarted hasn't been checked. With **Start in my profile from Steam**, Steam itself starts the game, so this doesn't apply.

## Xbox app

The Xbox app (and PC Game Pass) always starts `Stardew Valley.exe`. SMAPI's guide for the Xbox app gets around this with an **exe swap**: the real `Stardew Valley.exe` is renamed `Stardew Valley original.exe`, and a copy of SMAPI takes its name. The installer offers to do this for you.

* **Redo it after every game update**, because the update replaces SMAPI's copy with the new game. Run the installer again: the updated `Stardew Valley.exe` becomes the new `Stardew Valley original.exe`, and SMAPI's copy takes its name again.
* The game folder is usually `C:\XboxGames\Stardew Valley\Content`.
* **Apply & Restart**, and **Restart now** in the **Load your profile?** box, start `StardewModdingAPI.exe` from that folder directly, not through the Xbox app. This hasn't been tested.
* **Start in my profile from Steam** is only for the Steam version, so its row on the **Account** tab is grayed out.
* Windows can refuse changes to files in the Xbox app's game folder. If the installer says Windows didn't allow the exe swap, follow [SMAPI's Xbox app guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app) to do it by hand.

## macOS

DewDrop's code supports macOS, but it hasn't been tested yet. The installer has been, on a Mac with an Apple chip.

* **Free Nexus Mods accounts can't use website download links.** **Get on Nexus** opens the file on the website, and DewDrop tells you to add the mod to your `Mods` folder yourself. See the warning below.
* **Apply & Restart** starts `StardewModdingAPI` in your game folder directly. You probably won't get a Terminal window for SMAPI in that session, so you can't type [console commands](console-commands.md) until you start the game the usual way. Whether macOS shows security prompts for a game started this way hasn't been tested.
* The **Load your profile?** box works as on Windows, and its **Restart now** uses the same untested restart as **Apply & Restart**.
* **Start in my profile from Steam isn't available.** SMAPI's macOS and Linux launcher doesn't pass Steam's launch options on to SMAPI, so DewDrop can't use them. The row on the **Account** tab is grayed out.
* **The downloaded installer** may be blocked by Gatekeeper, macOS's check for apps from the internet, the first time you open it. [The one line](../getting-started/installing/installer.md#the-quick-way-one-line) pasted into Terminal isn't blocked; [Install with the installer](../getting-started/installing/installer.md) also shows how to allow the download.
* **Macs with an Apple chip (M1 or later) need Rosetta**, Apple's free add-on for Intel apps, because SMAPI's installer is made for Intel Macs. The installer offers to install it.
* SMAPI needs macOS 10.15 Catalina or later.
* DewDrop's data folder is `~/.config/Stardrop/Data`. See [Where DewDrop keeps files](file-locations.md).

## Linux

DewDrop's code supports Linux, but it hasn't been tested yet. The installer has been.

* **Free Nexus Mods accounts can't use website download links**, the same as on macOS.
* **Apply & Restart** runs the game's `StardewValley` file, which SMAPI's installer replaced with SMAPI's own launcher. If a game update puts the original back, the restart starts the game without SMAPI, and so without mods. Run SMAPI's own installer again to fix it; the DewDrop installer skips SMAPI when it's already up to date. See [About SMAPI](../getting-started/installing/smapi.md#macos-and-linux).
* The **Load your profile?** box works as on macOS, and **Start in my profile from Steam** isn't available, for the same reason.
* The **Open mods folder** button and the buttons that open websites use your desktop's default apps, through `xdg-open`.
* DewDrop's data folder is `~/.config/Stardrop/Data`.

<details>

<summary>A rare Linux restart problem (untested)</summary>

SMAPI's launcher opens a terminal window to run in. With a few terminal programs, the generic `terminal` command and `termite`, it joins the restart's settings into one line of text. The space in the `Selected Mods` folder name could then break the restart. If **Apply & Restart** starts the game without your profile on Linux, this may be why. Most desktops use other terminals and aren't affected.

</details>

{% hint style="warning" %}
**macOS and Linux, free accounts:** install mods by hand, or paste the website's `nxm://` link into the SMAPI console with `dewdrop_nxm`. [Free accounts on macOS and Linux](../nexus-mods/free-vs-premium.md#free-accounts-on-macos-and-linux) has the steps. Premium accounts don't need this; **Install** works directly.
{% endhint %}

## Steam Deck

The Steam Deck isn't supported, and DewDrop hasn't been tried on one.

* The Deck runs the Linux version of Stardew Valley. In Desktop Mode, DewDrop would probably behave as it does on Linux, including the missing website download links for free accounts.
* In Game Mode, Steam may close everything the game started when the game exits. That would stop **Apply & Restart** from bringing the game back.
* DewDrop never offers to restart into your profile on the Steam Deck: the **Load your profile?** box doesn't appear, and **Start in my profile from Steam** is grayed out.

## The Windows version through Proton or Wine

Not supported. Proton is Steam's tool for running Windows games on Linux. SMAPI for Linux can't load mods into the Windows version, and **Apply & Restart** relies on Windows' PowerShell and `mklink` command, which probably don't work under Proton or Wine. For the same reason, the **Load your profile?** box doesn't appear there, and **Start in my profile from Steam** is grayed out. On Linux and the Steam Deck, use the Linux version of the game instead.

If your game folder holds the Windows version (`Stardew Valley.exe` and no Linux launcher), the installer warns you and stops unless you answer **Y**. To switch to the Linux version:

1. In Steam, right-click Stardew Valley and choose **Properties**.
2. Open **Compatibility** and untick **Force the use of a specific Steam Play compatibility tool**.
3. Let Steam download the Linux version, then run the installer again.

{% hint style="info" %}
Using DewDrop on macOS, Linux or a Steam Deck? Reports help, whether it works or not. See [Reporting issues](../help/reporting-issues.md).
{% endhint %}
