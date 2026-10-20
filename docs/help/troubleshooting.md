---
description: "Fixes for common DewDrop problems, sorted by what you see."
icon: wrench
---

# Troubleshooting

Find what you're seeing in this list and click it. If nothing here helps, [Reporting issues](reporting-issues.md) explains how to get help.

**While installing**

* [The one line says "404" or "Not Found"](#the-installer-fails-to-start)
* [Windows says "Windows protected your PC"](#the-installer-fails-to-start)
* [macOS says the installer can't be opened](#the-installer-fails-to-start)
* [The installer window says "Oops!"](#the-installer-fails-to-start)
* [The installer couldn't find my game](#the-installer-cant-find-stardew-valley)
* [The installer wants to restart as administrator](#windows-wont-let-the-installer-change-files)
* [SMAPI didn't install, or the installer stopped after 3 minutes](#smapi-didnt-install)
* [macOS: the installer asks about Rosetta, or says "Bad CPU type in executable"](#smapi-didnt-install)
* [Linux: the installer says I have the Windows version](#linux-the-windows-version-of-the-game)
* [The installer asked about Steam's launch options, or about starting in my DewDrop profile](#the-installer-and-steams-launch-option)

**On a Steam Deck**

* [The game shows a black screen or a terminal window in Game Mode](../getting-started/steam-deck.md#if-something-goes-wrong)
* [The installer says Steam isn't set to start the game with DewDrop's launcher yet](#the-installer-and-steams-launch-option)
* [The game didn't come back after Apply & Restart](#the-game-closed-and-never-came-back)
* [Start in my profile from Steam is grayed out](#you-cant-tick-start-in-my-profile-from-steam)
* [There's no SMAPI window to type commands in](#check-the-smapi-log)
* [Nexus Mods didn't accept the key from the installer](#logging-in-fails)

**While playing**

* [There's no Mods button](#no-mods-button-on-the-title-screen)
* [The game starts without the Mods button when I press Play in Steam](#if-steam-starts-the-game-in-your-profile)
* [No SMAPI window opens, or Steam's Play button starts the game without mods](#check-that-smapi-is-running)
* [The game closed and didn't come back](#the-game-closed-and-never-came-back)
* [Apply & Restart says "Didn't restart"](#didnt-restart-because-dewdrop-couldnt-link-a-mod)
* [Steam closed and opened again by itself](#steam-closed-and-opened-again-by-itself)
* [A message says "Steam's launch option wasn't changed"](#steams-launch-option-wasnt-changed)
* [I can't tick Start in my profile from Steam](#you-cant-tick-start-in-my-profile-from-steam)
* [DewDrop asks to load my profile every time](#dewdrop-asks-to-load-your-profile-every-time)
* [The box says the last restart didn't come back](#the-box-says-the-last-restart-didnt-come-back)
* [I don't see the "Load your profile?" box](#dewdrop-doesnt-ask-to-load-your-profile)
* [The box says "Couldn't load your profile"](#couldnt-load-your-profile)
* [Mods I turned off came back](#mods-you-turned-off-came-back)
* [A mod I added myself is missing or didn't load](#a-mod-you-copied-in-is-missing-or-turned-off)
* [A mod says "Needs ..."](#a-mod-says-needs)
* [A mod update says "restart to finish"](#an-update-says-restart-to-finish)
* [A mod says "Installed twice"](#a-mod-says-installed-twice)
* [A message says "Couldn't delete ... (uninstalled)"](#an-uninstalled-mod-wasnt-deleted)
* [I can't log in to Nexus Mods, or my downloads never arrive](#nexus-mods-problems)
* [There's a red message at the bottom of the menu](#a-red-message-at-the-bottom-of-the-menu)

## The installer

Yellow `[!]` lines are warnings, and the installer carries on after them. If it has to stop, it says why in a red `[x]` line. At the end, it says where it saved its log. [The installer log](reporting-issues.md#the-installer-log) explains how to find it.

### The installer fails to start

**The one line says "404" or "Not Found"**

The one-line installer comes with DewDrop 0.2.0 and works once 0.2.0 is released. Until then, download `DewDrop-<version>-installer.zip` from the [latest release](https://github.com/friessssss/DewDrop/releases/latest) and follow [Install with the installer](../getting-started/installing/installer.md). On a Steam Deck, wait for 0.2.0: older installers can't set up Steam there.

If it says "Couldn't download the DewDrop installer from ...", check your internet connection and paste the line again.

{% tabs %}
{% tab title="Windows" %}
**"Windows protected your PC"**

This is Windows SmartScreen, which warns about new downloads. Click **More info**, then **Run anyway**. Or skip the download: open **PowerShell** (click **Start**, type `PowerShell` and press **Enter**), paste this line and press **Enter**:

```powershell
irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex
```

**"Oops! It looks like you opened this from inside the zip file."**

Right-click `DewDrop-<version>-installer.zip` and choose **Extract All...**. In the folder that opens, open `DewDrop <version> installer` and double-click `Install on Windows.bat`. Still see this after extracting? The files went into a temporary folder: extract them to your Downloads folder or desktop instead.

**"Oops! The installer is missing internal\install.ps1."**

Only part of the zip was extracted, or your antivirus removed a file. Check your antivirus's quarantine list, then download the installer again and extract all of it.

**"Oops! The installer couldn't start (error ...)"**

PowerShell couldn't run the installer. The reason is usually just above this line:

* **Your antivirus blocked it.** Allow the installer in your antivirus, and try again.
* **Your PC doesn't allow PowerShell scripts.** The message mentions scripts being disabled, a policy or a digital signature. This is common on work and school computers. Delete the extracted folder, right-click the zip, choose **Properties**, tick **Unblock**, click **OK**, and extract it again. If it's still blocked, [install DewDrop by hand](../getting-started/installing/manual.md). **Apply & Restart** also uses a small PowerShell script, so it may not work on that PC either.
{% endtab %}

{% tab title="macOS" %}
**macOS says the file can't be opened, or "Not Opened"**

macOS shows this for scripts from the internet that Apple hasn't checked. The easiest way round it is to install from **Terminal** instead. Paste this line and press **Return**:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
```

To open the downloaded file anyway, click **Done** (not **Move to Trash**). Open **System Settings** > **Privacy & Security**, scroll down, click **Open Anyway**, and enter your password. On older macOS, right-click the file, choose **Open**, then click **Open** again.

**"...you do not have appropriate access privileges", or double-clicking does nothing**

1. Open **Terminal**.
2. Type `bash` followed by a space.
3. Drag `Install on macOS.command` into the Terminal window, and press **Return**.

If the installer still gives you trouble, [install DewDrop by hand](../getting-started/installing/manual.md) and please [tell us what happened](reporting-issues.md).
{% endtab %}

{% tab title="Linux" %}
**Double-clicking opens a text editor, or nothing happens**

Some file managers don't run scripts on a double-click. Right-click an empty spot in the `DewDrop <version> installer` folder and choose **Open in Terminal**. Type this and press **Enter**:

```bash
bash "Install on Linux.sh"
```

Do the same if you see "Couldn't find a terminal to run the DewDrop installer in." Or skip the download and use [the one line](../getting-started/installing/installer.md#the-quick-way-one-line) in a terminal.

**"Couldn't unzip SMAPI: install 'unzip' (or python3) and try again."**

Install the `unzip` package with your distribution's package manager, then run the installer again. For example:

```bash
sudo apt install unzip      # Debian, Ubuntu, Linux Mint
sudo dnf install unzip      # Fedora
sudo pacman -S unzip        # Arch
```
{% endtab %}
{% endtabs %}

### The installer can't find Stardew Valley

If the installer says "I couldn't find Stardew Valley automatically.", it asks you to show it your game folder:

* **Windows:** a folder picker opens. Pick the folder with the Stardew Valley game icon in it (`Stardew Valley.exe`). If you close the picker, paste the folder's path and press **Enter**.
* **macOS:** drag your Stardew Valley app, or its game folder, into the Terminal window and press **Return**.
* **Linux:** type or paste the path of the folder that contains `Stardew Valley.dll`, and press **Enter**.

To find your game folder:

* **Steam:** in your library, right-click **Stardew Valley** > **Manage** > **Browse local files**.
* **GOG Galaxy:** open the game's **Manage installation** menu and choose **Show folder**.
* **Xbox app:** it's usually `C:\XboxGames\Stardew Valley\Content`.

"That folder doesn't contain Stardew Valley.dll. Try again." means you picked the wrong folder. `Stardew Valley.dll` sits in the same folder as the game icon, so on Windows, look for the game icon again.

You can also give the installer your game folder as an option. See [Installer command-line options](../reference/installer-options.md).

### Windows won't let the installer change files

"Windows won't let me change files in ..." usually means the game is in a protected folder such as `Program Files`, where some GOG installs are. "Windows refused access to a file" has the same fix.

* **Answer Y** to "Restart the installer as administrator?", then click **Yes** when Windows asks. A new installer window carries on, and you can close the old one. It still starts Steam and the game as you, and puts the **Stardew Valley (SMAPI)** shortcut on the desktop for every user of the PC.
* **Answered N, or Windows didn't start it?** Close the game, then run the installer as administrator yourself:
  * **From the zip:** right-click `Install on Windows.bat` and choose **Run as administrator**.
  * **With the one line:** right-click **Start**, choose **Terminal (Admin)** or **Windows PowerShell (Admin)**, paste the line again and press **Enter**.
* **"That happens even as administrator."** Check that the game folder isn't read-only, and that your antivirus isn't blocking it.

### SMAPI didn't install

The installer downloads SMAPI's official installer from GitHub and runs it for you. On Windows, it opens in its own window and closes by itself. If that window stays open, it's showing an error: read it or take a screenshot, because after 3 minutes the DewDrop installer closes it and stops.

| The installer says | What to do |
| --- | --- |
| "Couldn't download SMAPI automatically." | The installer couldn't reach GitHub. Check your internet connection. If you've run the installer many times in a row, GitHub may be limiting you: wait an hour. |
| "The SMAPI download failed..." (macOS and Linux), or "Something went wrong: ..." (Windows) | Check your connection and run the installer again. |
| "The SMAPI download didn't contain ... SMAPI.Installer ..." | Your antivirus may have removed it. Check its quarantine list. |
| "The SMAPI installer didn't finish within 3 minutes, so I stopped it." | It usually takes seconds, so it got stuck, most often on an old SMAPI file it couldn't replace. Close the game and any SMAPI window. On Windows, [run the installer as administrator](#windows-wont-let-the-installer-change-files). |
| "The SMAPI installer finished, but SMAPI still looks missing or out of date." | On macOS and Linux, SMAPI's own messages are just above. On Windows, run the installer as administrator. |
| "The SMAPI installer couldn't start (see the message above)." (macOS and Linux) | The reason is just above it. On a Mac, it's usually Rosetta: see below. |
| "DewDrop needs SMAPI. Install it from ..." | You answered **N** when the installer offered to install SMAPI. DewDrop can't run without it. |

**Macs with an Apple chip need Rosetta**

SMAPI's installer is made for Intel Macs, so a Mac with an Apple chip (M1 or later) needs Rosetta, Apple's free add-on for running Intel apps. When it's missing, the installer says "...this Mac needs Rosetta to run it." Answer **Y** to "Install Rosetta now?" and enter your Mac password.

If you answered **N**, if it says "Rosetta didn't install.", or if you see "Bad CPU type in executable", install Rosetta yourself. Paste this into **Terminal**, press **Return** and follow what it asks, then run the DewDrop installer again:

```bash
softwareupdate --install-rosetta
```

**"I can't change files in ..." (macOS and Linux)**

Some game files belong to another user, usually because SMAPI was once installed with `sudo`. The installer prints a command that fixes it, with your user name and game folder already in it. Paste that line into Terminal, press **Return**, enter your password, then run the installer again. It looks like this:

```bash
sudo chown -R "yourname" "/home/yourname/.local/share/Steam/steamapps/common/Stardew Valley"
```

If SMAPI still won't install, [install or update SMAPI yourself](../getting-started/installing/smapi.md#install-or-update-smapi-yourself), then run the DewDrop installer again. It sees that SMAPI is there and moves on to DewDrop. If it offers a newer SMAPI, that's optional: answer **N** to keep yours.

### Linux: the Windows version of the game

The installer says "This is the Windows version of the game, which Steam runs through Proton." Proton is Steam's tool for running Windows games on Linux. SMAPI for Linux can't load mods into the Windows version, so the installer stops unless you answer **Y**. To switch to the Linux version:

1. In Steam, right-click **Stardew Valley** and choose **Properties**, then **Compatibility**.
2. Untick **Force the use of a specific Steam Play compatibility tool**.
3. Let Steam download the Linux version of the game.
4. Run the installer again.

### The installer and Steam's launch option

The installer can set Stardew Valley's launch option in Steam, so **Play** starts the game with mods. It only changes the Steam account that's signed in (or was most recently), closes Steam for the change, and backs up Steam's settings file, `localconfig.vdf`, first.

{% tabs %}
{% tab title="Windows" %}
On Windows, the launch option starts SMAPI.

| The installer says | What it means |
| --- | --- |
| "Steam's launch option starts SMAPI from somewhere else" | You may have moved the game to another drive. Answer **Y** (the default) to point it at this game's SMAPI. Other options you added are kept. |
| "Steam already has launch options for Stardew Valley" | They're only replaced if you answer **Y**. With **N** (the default), the installer copies SMAPI's launch option for you to paste yourself, and offers a desktop shortcut. See [About SMAPI](../getting-started/installing/smapi.md#steam-on-windows). |
| "Start the game in your DewDrop profile when you press Play in Steam?" | The same as **Start in my profile from Steam** in the game (default **Y**). DewDrop finishes the job the next time you quit the game: Steam closes and opens again once. If Steam already starts the game in your profile, the installer doesn't ask. |
| "Steam didn't close.", "Couldn't find Steam's folder." or another message about Steam's settings | The installer left Steam alone, and shows how to set the launch option yourself. |

Play on another Steam account on this PC? Sign in to it in Steam, then run the installer again.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
On a Steam Deck, and on Linux with the installer's `--steam-setup` option, the launch option runs DewDrop's Steam launcher. The installer asks all its questions first, then closes Steam for a moment. See [Steam on Linux and the Steam Deck](../reference/installer-options.md#steam-on-linux-and-the-steam-deck).

| The installer says | What it means |
| --- | --- |
| "Steam starts SMAPI directly right now" | The launcher takes SMAPI's place. Anything before SMAPI, such as `gamemoderun`, and anything after `%command%` is kept. SMAPI's own options, such as `--mods-path`, go: the installer names them and asks first (default **Y**). |
| "Stardew Valley has launch options in Steam already" | If they contain `%command%`, the launcher goes just before it and the rest is kept (default **Y**). Other launch options are only replaced if you answer **Y**. |
| "Start the game in your DewDrop profile when you press Play in Steam?" | Your answer counts at once, and Steam doesn't close again for it. DewDrop links your profile as you quit the game, so the first **Play** may still load your whole `Mods` folder. A choice you already made, here or in the game, is kept without asking. |
| "Steam saved different launch options as it closed (...), so I left them alone." | Run the installer again to add DewDrop's launcher to them. |
| "Steam didn't close." | Enter the launch option the installer shows (see below). Or close Steam yourself (right-click its icon in the system tray and choose **Exit**), then run the installer again. |
| "Couldn't find Steam's folder (Flatpak and Snap Steam aren't supported for this yet)." | DewDrop's launcher needs Steam installed the regular way, not from Flatpak or Snap. |
| "Couldn't find your Steam account's settings." | Start Steam and sign in once, then run the installer again. |
| "This DewDrop has no Steam launcher" | The DewDrop the installer used is too old. Run the installer from the latest release. |
| "Only the Steam version of the game can be set up to start with DewDrop's launcher." | Your copy isn't from Steam. Start the game the usual way. |
| "Steam isn't set to start the game with DewDrop's launcher yet" (Steam Deck, at the end) | Game Mode may show a black screen or a terminal window. The yellow lines above say why. Fix that, then run the installer again in Desktop Mode. |

When the installer can't make the change, it shows the launch option to enter yourself: in Steam, open Stardew Valley's **Properties** and find **Launch Options** on the **General** page. On a Steam Deck, it's:

```text
"/home/deck/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh" %command%
```
{% endtab %}
{% endtabs %}

### Other installer messages

| The installer says | What it means |
| --- | --- |
| "This is Stardew Valley ... DewDrop and SMAPI 4 need Stardew Valley 1.6 or later; update the game first." (Windows) | Update the game, then run the installer again. |
| "It looks like Stardew Valley has never been started..." | SMAPI recommends starting the game once without mods first. The installer's default (**Enter**) is to carry on, and that usually works. To follow SMAPI's advice instead, type **N**, start the game once, close it at the title screen, and run the installer again. |
| "Stardew Valley is running. Please close the game..." | Files can't be replaced while the game uses them. Close the game (and on Windows, the SMAPI window), then press **Enter**. |
| "Couldn't reach GitHub..." or "The DewDrop download failed." | With no DewDrop next to it, as with the one line, the installer downloads the latest release. Check your connection and try again, or use the installer zip, which has DewDrop inside. |
| "There's already a folder at ... that isn't DewDrop." | Rename that folder or move it out of `Mods`, then run the installer again. |
| "There are other copies of DewDrop in your Mods folder." | SMAPI won't load a mod that's installed twice. Answer **Y** and the installer puts a `.` in front of the extra folders' names, so SMAPI skips them. You can delete them later. |
| "Windows didn't allow it" (Xbox app) | Windows blocked the file swap that makes the Xbox app start SMAPI. Follow SMAPI's [Xbox app guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app). |

## No Mods button on the title screen

<figure><img src="../.gitbook/assets/shot-title.png" alt="Stardew Valley title screen with a wooden Mods button between the Co-op and Exit buttons"><figcaption><p>The Mods button sits between Co-op and Exit.</p></figcaption></figure>

Work through these checks in order.

### Look in the right place

The **Mods** button only appears on the title screen, once the **New**, **Load**, **Co-op** and **Exit** buttons appear. If your window is too narrow for five big buttons, DewDrop shows a small button with a dew drop instead, in the bottom-right corner above the small language and **?** (About) buttons. You can't open DewDrop while you're playing a save.

### Check that SMAPI is running

When the game starts with SMAPI, a separate text window usually opens next to it: the SMAPI console. On Windows it's a black window; on macOS and Linux it's a Terminal window. If none opens, the game probably started without mods. There are two exceptions, where mods load with no SMAPI window: after **Apply & Restart** or **Restart now** on macOS, and whenever Steam starts the game through DewDrop's Steam launcher (on a Steam Deck, or on Linux after the installer's `--steam-setup`).

{% tabs %}
{% tab title="Windows (Steam)" %}
Steam starts SMAPI only if Stardew Valley has SMAPI's launch option. Run the DewDrop installer again and answer **Y** when it offers to set it, or use the **Stardew Valley (SMAPI)** desktop shortcut. If you moved the game to another drive, the option may still point at the old folder: the installer notices and offers to fix it. To set it yourself, see [About SMAPI](../getting-started/installing/smapi.md#steam-on-windows).
{% endtab %}

{% tab title="Windows (GOG and others)" %}
Start the game with the **Stardew Valley (SMAPI)** desktop shortcut the installer offered, or with `StardewModdingAPI.exe` in your game folder. GOG Galaxy's **Play** button starts the game without mods unless you set Galaxy up; see [GOG Galaxy on Windows](../getting-started/installing/smapi.md#gog-galaxy-on-windows).
{% endtab %}

{% tab title="Xbox app" %}
After a game update, the Xbox app starts the plain game again. Run the DewDrop installer again: it notices, and offers to redo the swap that makes the Xbox app start SMAPI.
{% endtab %}

{% tab title="macOS and Linux" %}
SMAPI's installer replaces the game's launcher, so starting the game the usual way starts SMAPI. A game update can put the original launcher back. To fix it, run **SMAPI's own installer** again, from [smapi.io](https://smapi.io). The DewDrop installer skips SMAPI when it's up to date, so it won't fix this.

On Linux, also check that you have [the Linux version of the game](#linux-the-windows-version-of-the-game).
{% endtab %}

{% tab title="Steam Deck" %}
This also applies on Linux after the installer's `--steam-setup`.

DewDrop's Steam launcher starts SMAPI itself, so a game update doesn't turn mods off. If **Play** starts the game without mods, `steam-launch.log` says why. "SMAPI isn't in ..., so the game starts without mods" means you should run the DewDrop installer again, which installs SMAPI.

A black screen or a terminal window in Game Mode means Steam isn't using DewDrop's launcher. [DewDrop on the Steam Deck](../getting-started/steam-deck.md#if-something-goes-wrong) explains the fix, and where `steam-launch.log` is.
{% endtab %}
{% endtabs %}

### Check where DewDrop is installed

Inside your game folder, look for the file `Mods/DewDrop/manifest.json`. Common mistakes:

* **The zip itself is in `Mods`.** SMAPI can't load mods from a zip. Extract it.
* **DewDrop's files are loose in `Mods`,** instead of inside a `DewDrop` folder.
* **The `DewDrop` folder went into the game folder** instead of its `Mods` folder.
* **You extracted the installer zip into `Mods`.** The installer isn't the mod. Run the installer instead, or use the plain `DewDrop-<version>.zip`. See [Installing DewDrop](../getting-started/installing/README.md).

Aim for `Mods/DewDrop/manifest.json`. If yours is one level deeper, move the inner `DewDrop` folder straight into `Mods` and delete the empty outer one. Folders whose names start with a `.` are skipped.

### Check for a second copy

SMAPI won't load a mod that's installed twice, so two copies of DewDrop means no **Mods** button at all. Look through your `Mods` folder, including subfolders, for another folder with DewDrop's files, and delete the older one. Running the installer again also finds extra copies and offers to turn them off.

### If you start the game from Stardrop

A freshly installed DewDrop isn't in your Stardrop profiles yet, so Stardrop doesn't load it. Start the game once from Steam or your SMAPI shortcut instead, and DewDrop adds itself to your Stardrop profiles. See [Using DewDrop with Stardrop](../stardrop.md#first-time-setup-if-you-already-use-stardrop).

### If Steam starts the game in your profile

On Windows, with **Start in my profile from Steam** on, or a `--mods-path` launch option you added yourself, Steam's **Play** button loads only the mods linked in `Selected Mods`. If DewDrop isn't linked there, the game starts without the **Mods** button. That can happen when:

* **You started a Stardrop collection profile from Stardrop.** Stardrop refills `Selected Mods` whenever it starts the game, and collection profiles never list DewDrop. Start one of your own profiles from Stardrop once, or follow the steps below.
* **The links are broken or missing,** for example because you moved DewDrop's folder, or a restart couldn't link a mod.

DewDrop's Steam launcher on Linux and the Steam Deck avoids this: it loads your whole `Mods` folder whenever `Selected Mods` has no DewDrop in it.

To get the **Mods** button back on Windows:

1. In Steam, right-click **Stardew Valley** and choose **Properties**.
2. On the **General** page, find **Launch Options** and delete only the `--mods-path "..."` part. Leave SMAPI's part, with your own game folder:

   ```text
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command%
   ```

3. Click **Play**. This normal launch loads your whole `Mods` folder, DewDrop included.
4. Quit the game. **Start in my profile from Steam** is still ticked, so DewDrop links your profile again as the game closes and puts the `--mods-path` part back. Steam closes and opens again once to save it.

If you'd rather stop using it, untick **Start in my profile from Steam** on the **Account** tab before you quit in step 4. If you added `--mods-path` to a desktop shortcut yourself, remove it from the shortcut's **Target** box the same way.

### Check the SMAPI log

Open your [SMAPI log](reporting-issues.md#find-your-smapi-log) and search for "DewDrop". Look for:

* **SMAPI skipped DewDrop.** The log says why, for example that it needs a newer SMAPI. DewDrop needs SMAPI 4.0 or later.
* **"DewDrop couldn't read the mod and profile folders, so it's turned off this session"** DewDrop couldn't create or read its data folder: `%AppData%\Stardrop\Data` on Windows, `~/.config/Stardrop/Data` on macOS and Linux. Make sure you can open that folder, and that security software isn't blocking it. See [Where DewDrop keeps files](../reference/file-locations.md).

While DewDrop is running, you can ask it what it sees. Type this in the SMAPI console window and press **Enter**:

```text
dewdrop_status
```

It shows which folder SMAPI loaded mods from, your `Mods` folder, the active profile, whether a restart is needed, whether DewDrop asked to load your profile, the **Start in my profile from Steam** setting, and your Nexus login. See [Console commands](../reference/console-commands.md).

With DewDrop's Steam launcher (on a Steam Deck, or on Linux after `--steam-setup`), SMAPI has no console window. Search the SMAPI log for "Start in my profile from Steam:" and "Load your profile:" instead. A line starting with "Linux: SteamOS" says whether DewDrop saw a Steam Deck, Game Mode and its launcher.

## Problems with Apply & Restart

When **Apply & Restart** can't restart, a message says why and the game keeps running. The "Didn't restart" sections below explain those messages. If it says "*name* is still installing. Restart when it's done.", wait for that download to finish installing, then click **Apply & Restart** again.

### The game closed and never came back

After you click **Restart**, or **Restart now** in the **Load your profile?** box, the game closes and DewDrop starts it again through SMAPI a few seconds later. If it doesn't come back, start the game yourself: the **Load your profile?** box may [say what happened](#the-box-says-the-last-restart-didnt-come-back).

{% tabs %}
{% tab title="Windows" %}
1. Press **Win+R**, paste this and press **Enter**:

   ```text
   %AppData%\Stardrop\Data\Cache\DewDrop
   ```

2. Open `relaunch.log`. DewDrop empties it each time its helper runs after the game closes, and only writes to it when something went wrong, an update or uninstall finished, or Steam's launch option changed.
   * **"Could not start SMAPI: ..."** `StardewModdingAPI.exe` is missing or couldn't start, often because your antivirus removed it. Run the DewDrop installer again to put it back.
   * **"Could not update ... yet: ..."** See [An update says restart to finish](#an-update-says-restart-to-finish).
   * **"Could not uninstall ... yet: ..."** See [An uninstalled mod wasn't deleted](#an-uninstalled-mod-wasnt-deleted).
   * **"Could not change the Steam launch option: ..."** See [Steam's launch option wasn't changed](#steams-launch-option-wasnt-changed).
   * **Empty, or only "Updated ..." and "Uninstalled ..." lines, with a "Date modified" that matches your restart.** SMAPI was started, so SMAPI or the game stopped on its own. Check your [SMAPI log](reporting-issues.md#find-your-smapi-log).
   * **Missing, or older than your restart.** The restart helper never ran, most likely because security software or a work or school policy blocks PowerShell scripts.
3. Start the game the usual way, then try **Apply & Restart** again, or [report the problem](reporting-issues.md) with your `relaunch.log`.
{% endtab %}

{% tab title="macOS and Linux" %}
**Apply & Restart** hasn't been tested on macOS and Linux yet. **Restart now** in the **Load your profile?** box uses the same restart.

* **macOS:** DewDrop starts SMAPI directly, without a Terminal window, so the game probably comes back with no SMAPI window. That's expected.
* **Linux:** DewDrop starts the game's `StardewValley` launcher, which SMAPI's installer replaced so it starts SMAPI. If a game update put the original launcher back, the game comes back without mods and without the **Mods** button. Run SMAPI's installer again.

There's no restart log on macOS, or on Linux without DewDrop's Steam launcher. If the game doesn't come back at all, start it the usual way and [report the problem](reporting-issues.md).
{% endtab %}

{% tab title="Steam Deck" %}
This also applies on Linux after the installer's `--steam-setup`.

Once the game has closed, DewDrop runs its Steam launcher again, which starts SMAPI in your profile with no window. The restart should stay in Game Mode and in the same Steam session, but that's still being checked on a real Steam Deck.

* **The game doesn't come back:** press **Play**. If the restart never got as far as starting the game, this launch opens in your profile instead. It also does when **Start in my profile from Steam** is on.
* **`steam-launch.log`**, in `~/.config/Stardrop/Data/Cache/DewDrop`, records each start, restarts included. For a restart, it says "(restarting into the profile)".

Please [report it](reporting-issues.md) with `steam-launch.log` and your SMAPI log.
{% endtab %}
{% endtabs %}

### Didn't restart because DewDrop couldn't link a mod

The message reads "Didn't restart: DewDrop couldn't link ..., so it wouldn't load. The SMAPI log says why." To see why, search your SMAPI log for "The link for", "Couldn't link" or "Linking reported". Common causes:

* **The mod's folder was moved or deleted while the game was running.** Close the game and start it again, so DewDrop sees your mods as they are now.
* **Your mods are on a network drive or network share.** Links can't point there. Move your mods to a drive inside your computer.
* **Security software blocked the command that makes the links.**

Fix the cause, then click **Apply & Restart** again.

{% hint style="warning" %}
By the time this message appears, DewDrop may already have emptied `Selected Mods`. A normal start isn't affected. But if Steam starts the game in your profile on Windows, with **Start in my profile from Steam** or a `--mods-path` option you added, your next start may have no mods and no **Mods** button. See [If Steam starts the game in your profile](#if-steam-starts-the-game-in-your-profile).
{% endhint %}

### Didn't restart because the relaunch couldn't be set up

The message reads "Didn't restart: DewDrop couldn't set up the restart (...). The SMAPI log says why. If you use Stardrop, starting the game from Stardrop loads this profile." DewDrop couldn't start the helper that brings the game back.

If you use Stardrop, start the game from Stardrop. If not, close the game, start it the usual way, and try **Apply & Restart** again. If it keeps happening, search the SMAPI log for "Couldn't schedule the relaunch" and [report the problem](reporting-issues.md).

### Didn't restart because SMAPI is missing

The message reads "Didn't restart: SMAPI isn't in your game folder. Run the DewDrop installer (or SMAPI's) to put it back." DewDrop checks for SMAPI's launcher (`StardewModdingAPI.exe` on Windows) before it closes the game, because without it the game wouldn't come back. Your antivirus may have removed it. Close the game and run the DewDrop installer again: it notices that SMAPI is missing and installs it.

## Starting in your profile

A normal launch loads your whole `Mods` folder. Your profile only loads when SMAPI starts on `Selected Mods`, the folder of links DewDrop fills with your profile's mods. **Start in my profile from Steam** and the **Load your profile?** box get you there; [Start in your profile](../guides/start-in-your-profile.md) explains both.

### Steam closed and opened again by itself

On Windows, that's expected once after you turn **Start in my profile from Steam** on or off. Steam writes over its settings file while it runs, so a launch option can only be changed while Steam is closed. When you quit the game, DewDrop waits a few seconds so Steam can finish with the game, asks Steam to close, changes the launch option, and opens Steam again in the system tray.

It also happens once after you click **Restart now** with **Start in my profile from Steam from now on** ticked (when you quit the restarted game), and after you answer **Y** to the same question in the installer (when you next quit the game).

The next time you open **Mods**, a message says "Steam now starts the game in your profile." or "Steam no longer starts the game in your profile." If Steam closes every time you quit, the change isn't sticking: look for a message that starts with "Steam's launch option wasn't changed", described next.

On Linux and the Steam Deck, DewDrop's Steam launcher reads the setting each time it starts the game, so Steam never closes for it. Only the installer closes Steam, for a moment, when it sets up the launcher.

### Steam's launch option wasn't changed

On Windows, if DewDrop couldn't change Steam's launch option as the game closed, a message says so the next time you open **Mods**: "Steam's launch option wasn't changed:" followed by the reason. Steam's settings stay as they were, and DewDrop tries again the next time you quit the game.

| The reason | What to do |
| --- | --- |
| "Stardew Valley's launch options changed in Steam while the game was running, so DewDrop left them alone." | You, or another program, changed them while you played. DewDrop never writes over that, and tries again with the new options when you next quit. |
| "Steam didn't close, so its launch option wasn't changed." | Steam was still open 90 seconds after DewDrop asked it to close. If it keeps happening, quit the game, then close Steam yourself within a minute: right-click its icon in the system tray and choose **Exit**. If Steam doesn't open again afterward, start it yourself. |
| "Steam's settings file has an unexpected layout, so DewDrop left it alone." or "The change to Steam's settings didn't check out, so DewDrop left them alone." | DewDrop couldn't edit Steam's settings file safely. Leave the setting ticked and add the `--mods-path` part to Steam's launch options by hand, as [Start in your profile](../guides/start-in-your-profile.md#do-it-by-hand) shows. DewDrop sees that it's done and stops trying. |
| Anything else, such as access being denied | Something, such as security software, stopped DewDrop from writing Steam's settings file. Check that nothing blocks it, then quit the game once more. |

`relaunch.log` (see [The game closed and never came back](#the-game-closed-and-never-came-back)) notes each try: "Set the Steam launch option to: ..." or "Could not change the Steam launch option: ...". "Could not start Steam again" means you need to start Steam yourself.

No message at all, but hovering over the setting's row still says "Turns on when you quit the game"? DewDrop only points Steam at your profile once your profile's mods are linked, so if linking failed as the game closed, nothing changed. Click **Apply & Restart** once: if a mod can't be linked, it says which. A [red message at the bottom of the menu](#a-red-message-at-the-bottom-of-the-menu) can also stop it.

### You can't tick Start in my profile from Steam

The row is grayed out when DewDrop can't use it on your setup. Hover over it to see why:

| The row says | What to do |
| --- | --- |
| "Only for the Steam version of the game on Windows, Linux and the Steam Deck." | You play on macOS, where the setting isn't available. |
| "Set up by the DewDrop installer: run it once in Desktop Mode, and let it set up Steam." | Steam Deck: Steam doesn't use DewDrop's launcher yet. Do what it says. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md). |
| "Needs DewDrop's Steam launcher: in a terminal, run the DewDrop installer again with --steam-setup." | Linux: do what it says. See [Steam on Linux and the Steam Deck](../reference/installer-options.md#steam-on-linux-and-the-steam-deck). |
| "Couldn't find Steam's folder." | Linux and Steam Deck: DewDrop looks in `~/.steam/steam`, `~/.local/share/Steam` and `~/.steam/debian-installation`. Flatpak and Snap Steam aren't supported for this yet. |
| "DewDrop's Steam launcher couldn't be updated. The SMAPI log says why." | Linux and Steam Deck: search the SMAPI log for "steam-launch" to see why, then run the DewDrop installer again. |
| "Not available when the game runs through Proton or Wine." | Proton and Wine aren't supported. See [Platform support](../reference/platform-support.md). |
| "Only for the Steam version of the game. Use Apply & Restart to load your profile." | Your copy isn't from Steam, such as the GOG or Xbox app version. |
| "Couldn't find Steam on this PC." | Windows: DewDrop looks for Steam where Steam's installer records it, and didn't find it there. |
| "Couldn't find your Steam account's settings. Sign in to Steam, then start the game again." | Sign in to Steam, then start the game again. |
| "Couldn't read Steam's settings for the game." | Windows: Steam's settings file couldn't be read, or doesn't look the way DewDrop expects. Start the game again. |
| "Stardew Valley's launch option in Steam starts another program, so DewDrop can't add to it: ..." | Windows: DewDrop only adds to SMAPI's launch option. If you didn't mean to start another program, run the DewDrop installer again and let it [set SMAPI's launch option](#the-installer-and-steams-launch-option). |

DewDrop checks this as the game starts, so after you fix the cause, start the game again. Except under Proton or Wine, the **Load your profile?** box and **Apply & Restart** still get you into your profile.

### DewDrop asks to load your profile every time

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>The box names the mods that differ from your profile.</p></figcaption></figure>

A normal launch loads everything in your `Mods` folder. If any mod there is off in your profile, the two differ every time, so DewDrop asks every time. Pick what suits you:

* **Have Steam start the game in your profile** (the Steam version on Windows, or Linux and the Steam Deck with DewDrop's Steam launcher). Leave **Start in my profile from Steam from now on** ticked and click **Restart now**. From then on, **Play** starts the game in your profile and the box doesn't come back. On Windows, Steam closes and opens again once when you quit the restarted game.
* **Make a normal launch match.** Turn on the mods you want on the **Installed** tab. With the game closed, move mods you never use out of your `Mods` folder.
* **Stop asking.** Click **Don't ask again**. A normal launch then keeps your whole `Mods` folder, and you use **Apply & Restart** when you want your profile. You can tick **Ask to load my profile** again on the **Account** tab.

**Not now** keeps playing with the mods that loaded, and DewDrop asks again next time.

Still asked with **Start in my profile from Steam** on? Only Steam's **Play** button starts in your profile, not a desktop shortcut ([Start in your profile](../guides/start-in-your-profile.md#do-it-by-hand) shows how to add your profile to one). And the setting takes effect once you've quit the game with it ticked, because DewDrop links your profile as the game closes. So if you turned it on in the installer, your first **Play** still loads your whole `Mods` folder.

Some mods count as different every time:

* **Mods outside DewDrop's mods folder.** If Stardrop uses its own mods folder, a normal launch still loads the game's `Mods` folder. Mods only there aren't on DewDrop's list, so they can't be on in a profile. To keep one, move it into Stardrop's mods folder with the game closed, then turn it on. See [If Stardrop uses its own mods folder](../stardrop.md#if-stardrop-uses-its-own-mods-folder).
* **A mod that's in your `Mods` folder twice.** A normal launch skips it, but a restart into your profile loads one copy, so the box says it "didn't load". See [A mod says Installed twice](#a-mod-says-installed-twice).

### The box says the last restart didn't come back

DewDrop keeps a note of every restart, from **Apply & Restart** or **Restart now**, until the restarted game reaches its title screen. If the next start is a normal launch instead, that restart didn't finish. When the **Load your profile?** box opens, it says one of these in place of the list of mods:

* **"Last time DewDrop restarted the game, it didn't come back in your profile."** The game closed for the restart, but didn't come back in your profile. To find out why, see [The game closed and never came back](#the-game-closed-and-never-came-back).
* **"Last time, the game closed while your profile was loading."** The restart worked, but the game closed before it reached the title screen. If you closed it yourself, that's all it means. If not, a mod in your profile may have stopped it.

If the box offers **Start in my profile from Steam from now on**, that checkbox starts unticked this time, so Steam isn't changed until a restart has worked. Tick it if you want it anyway.

Click **Restart now** to try again. If the game closes again while your profile loads, grab your [SMAPI log](reporting-issues.md#find-your-smapi-log) before you start the game again: it usually says what went wrong.

### DewDrop doesn't ask to load your profile

The **Load your profile?** box only appears once per launch, on the first title screen after a normal launch, and only when the mods that loaded differ from your profile. It doesn't appear when:

* **The mods that loaded already match your profile.** There's nothing to restart for.
* **The game started in your profile:** after **Apply & Restart**, from Stardrop, from Steam with **Start in my profile from Steam** on, or with a `--mods-path` option you added.
* **SMAPI started on some other folder with `--mods-path`.** The bottom of the menu says "This session didn't start from a DewDrop profile. Restart to load this profile."
* **Ask to load my profile is unticked** on the **Account** tab. **Don't ask again** unticks it.
* **The game runs through Proton or Wine,** where DewDrop's restart can't bring the game back.
* **There's a [red message at the bottom of the menu](#a-red-message-at-the-bottom-of-the-menu).**
* **You started the game from a Steam multiplayer invite.** DewDrop lets you go straight to the invite.
* **SMAPI's launcher is missing** from your game folder (`StardewModdingAPI.exe` on Windows). Run the DewDrop installer again to put it back.
* **You opened Load, Co-op or another title menu first,** or a save loaded. It asks again next time.

To see why, run `dewdrop_status` in the SMAPI console (see [Check the SMAPI log](#check-the-smapi-log)). Its `Session:` line says what kind of launch this was and whether DewDrop asked, for example `Session: Plain; ask to load my profile: True (the mods loaded already match the profile)`. With DewDrop's Steam launcher there's no console: search the SMAPI log for "Load your profile:" instead. You can always load your profile with **Apply & Restart**.

### Couldn't load your profile

If DewDrop can't start the restart, the box changes to **Couldn't load your profile** and shows why. Click **OK**: the game keeps running with the mods loaded right now. The reasons are the same ones **Apply & Restart** gives, such as [a mod it couldn't link](#didnt-restart-because-dewdrop-couldnt-link-a-mod), [a restart it couldn't set up](#didnt-restart-because-the-relaunch-couldnt-be-set-up) or [SMAPI missing](#didnt-restart-because-smapi-is-missing).

While a download is still running, the box says "Waiting for 1 download to finish first..." instead, and **Restart now** works once it's done.

## Mods aren't on or off the way you set them

### Mods you turned off came back

<figure><img src="../.gitbook/assets/diagram-launching.png" alt="Diagram comparing two launches. Starting from Steam or a shortcut loads the whole Mods folder. Apply &#x26; Restart loads only the mods in your profile."><figcaption><p>A normal launch loads your whole Mods folder. Apply &#x26; Restart loads your profile.</p></figcaption></figure>

Your profile only applies when the game starts in it. A normal launch, from a shortcut or from Steam without **Start in my profile from Steam**, loads everything in your `Mods` folder, including mods you turned off. Your profile is fine: DewDrop only remembers which mods should load.

To get into your profile:

* **Steam version on Windows, or Linux and the Steam Deck with DewDrop's Steam launcher:** tick **Start in my profile from Steam** on the **Account** tab. After you next quit the game, **Play** starts the game in your profile. (On Windows, Steam closes and opens again once as you quit.)
* **Anywhere:** click **Restart now** when DewDrop asks **Load your profile?** on the title screen. Or open **Mods**: the bottom bar says how many mods are off but loaded anyway, such as "3 mods that are off in this profile loaded anyway. Restart to load this profile.", and those mods say **Off after restart** on the **Installed** tab. Click **Apply & Restart**.

See [Start in your profile](../guides/start-in-your-profile.md).

Mods still come back with **Start in my profile from Steam** on? Only Steam's **Play** button starts in your profile, not a desktop shortcut. And the setting takes effect once you've quit the game with it ticked, so if you turned it on in the installer, your first **Play** still loads your whole `Mods` folder. On Windows, until then, hovering over its row says "Turns on when you quit the game".

If mods come back even after **Apply & Restart**, look in `Selected Mods` (see [Where DewDrop keeps files](../reference/file-locations.md)) for a real mod folder, not a link. DewDrop leaves real folders there alone, so that mod loads every time. With the game closed, move it into your `Mods` folder.

### A mod you copied in is missing or turned off

DewDrop looks for mods when the game starts. If you copied a mod into `Mods` while the game was running, click **Rescan** on the **Installed** tab and it shows up.

With **Turn on mods when installed** on (the default), DewDrop turns a mod you copied in on in your active profile, when the game starts or when you click **Rescan**. A message names it, such as "Turned on AutoFish, new in your Mods folder." when you next open **Mods**, or "Found 1 new mod: AutoFish. Restart to load it." after **Rescan**. If it didn't load this time, its row says **On after restart**: click **Apply & Restart** to load it. With **Start in my profile from Steam** on, quitting the game also links it for your next **Play** from Steam.

A mod you copied in stays off when:

* **Turn on mods when installed is off** (**Account** tab > **Settings** panel).
* **DewDrop already knows the mod,** for example because it's on in another profile or you turned it off before. A mod you move or extract again keeps what you chose for it.
* **It failed to load the first time DewDrop ran,** for example because it was missing a mod it needs. DewDrop left it off in your **Default** profile.
* **It's a copy that belongs to a Stardrop collection,** or your active profile is a read-only collection profile. To change a collection profile, click **Duplicate** on the **Profiles** tab and use the copy.

To turn it on yourself, click it on the **Installed** tab, then click **Apply & Restart**.

Still not listed?

* **Check which folder DewDrop scans.** The "Mods folder" line of `dewdrop_status` (see [Check the SMAPI log](#check-the-smapi-log)) says. If Stardrop uses its own mods folder, DewDrop scans that one. See [If Stardrop uses its own mods folder](../stardrop.md#if-stardrop-uses-its-own-mods-folder).
* **Check that the mod's folder has a `manifest.json`,** directly inside or in a folder inside it. A mod still in its zip isn't listed.
* **Folders whose names start with a `.`, and hidden folders, are skipped.**

### A mod says Needs

A red **Needs ...** note means a mod you turned on requires another mod that isn't installed or is turned off. The **Show: Problems** filter on the **Installed** tab lists all of them.

If the required mod is installed, turn it on. (Turning a mod on also turns on the installed mods it needs.) If it isn't installed, find it on the **Browse** tab or on Nexus Mods and install it. DewDrop doesn't download required mods for you. See [Managing your mods](../guides/installed.md).

### An update says restart to finish

**Update ready, restart to finish** means the new version is downloaded and waits to be swapped in after the game closes. This happens on Windows, where the running game keeps its mods' files open. Click **Apply & Restart**, or quit the game: DewDrop finishes waiting updates a moment after the game closes.

If the update is still waiting after that, open `relaunch.log` (see [The game closed and never came back](#the-game-closed-and-never-came-back)) and look for "Could not update". Another program may have a file in that mod's folder open, such as a text editor, a File Explorer window or an antivirus scan. Close it, then restart or quit the game again. See [Updating mods](../guides/updating-mods.md).

### A mod says Installed twice

A red **Installed twice** note means two folders in your mods folder hold the same mod, with the same unique ID. When SMAPI loads the whole `Mods` folder, as a normal launch does, it loads neither copy. **Show: Problems** on the **Installed** tab lists them.

Hover over the row to see where the other copy is ("Also installed in: ..."), then click the trash can on the copy you don't want. The other copy stays installed, and stays on in your profile. See [Mods installed twice](../guides/installed.md#mods-installed-twice).

### An uninstalled mod wasn't deleted

DewDrop deletes a mod you uninstalled once the game has closed. On Windows, it can't while a program still has a file in the mod's folder open. It tries again a moment after the game closes, and at the next few starts. After three starts it gives up and says "Couldn't delete '...' (uninstalled): a file stayed open. Delete it yourself."

1. Find the folder. The message names it, and the SMAPI log has its full path: search for "Gave up removing".
2. Close the game, and any program that might be using a file in that folder, such as a text editor, a File Explorer window or an antivirus scan.
3. Delete the folder.

Until you do, the mod is listed on the **Installed** tab as an ordinary mod again. See [When the folder is deleted](../guides/installed.md#when-the-folder-is-deleted).

## Nexus Mods problems

### Logging in fails

[If logging in doesn't work](../nexus-mods/connecting.md#if-logging-in-doesnt-work) lists every login message. The most common ones:

* **"Nexus didn't accept that API key."** Click **Get my API key** and copy the whole **Personal API Key** from that page (not your password). Paste it with **Ctrl+V** and click **Log in**.
* **"Couldn't reach Nexus Mods: ..."** Check your internet connection. If it says Nexus Mods is limiting requests, wait a while: Nexus Mods limits how many requests each account can make per hour and per day.
* **"Nexus Mods didn't accept the key from the installer. Log in on the Account tab."** (Steam Deck) Nexus Mods turned down the key you pasted into the installer, so DewDrop deleted it. Log in on the **Account** tab with the whole **Personal API Key**. If the message ends "DewDrop tries the key from the installer again the next time the game starts.", Nexus Mods couldn't be reached: check your connection, and DewDrop tries again by itself. If it says "Logged in, but DewDrop couldn't save the key for next time.", you're logged in for now, and DewDrop tries the installer's key again at the next start. [On a Steam Deck](../nexus-mods/connecting.md#on-a-steam-deck) lists these messages.

Downloads have limits too. "You've reached Nexus Mods' download limit for now." means wait and try again later. "Nexus didn't accept your API key. Log in again on the Account tab." means log out, then log in with a fresh copy of your key.

### Website downloads never arrive

This is about free accounts, which download with the website's **Slow download** button. Premium accounts install straight from the **Install** button. See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md).

{% tabs %}
{% tab title="Windows" %}
1. **Check who handles the links.** On the **Account** tab, the **Mod manager links** panel should say "DewDrop handles these links." If it doesn't, click **Use DewDrop for these links**, then **Use DewDrop**. If the panel says Stardrop took the links back, or mentions Windows' Default apps setting, see [What the panel tells you](../nexus-mods/mod-manager-links.md#what-the-panel-tells-you).
2. **Allow the browser prompt.** When you click **Slow download**, your browser may ask whether to open the link with another app. Allow it.
3. **Check the messages and the Downloads tab.** A message tells you if you need to log in first, or if the link expired (click it on the website again). DewDrop can't install collections. A **Failed** row on the **Downloads** tab shows the reason.

Links you click while the game is closed aren't lost. DewDrop picks them up the next time you start the game, once you're logged in.
{% endtab %}

{% tab title="macOS and Linux" %}
Website downloads can't reach DewDrop on macOS and Linux yet. **Get on Nexus** opens the file on the website, and DewDrop says to add the mod to your `Mods` folder yourself. (The **Downloads** tab's hint still says to pick **Slow download** on the website, but on macOS and Linux that doesn't send the file to DewDrop.) Instead:

* **Download the mod yourself** from the website, and extract it into your `Mods` folder with the game closed. Start the game, turn the mod on in the **Installed** tab, and click **Apply & Restart**.
* **Paste the link into the console.** If you can copy the `nxm://` link your browser tries to open, type this in the SMAPI console window, with your link in place of `<link>`, and press **Enter**:

  ```text
  dewdrop_nxm <link>
  ```

  This doesn't work on a Steam Deck, or on Linux with DewDrop's Steam launcher: SMAPI has no console window there.

[Free accounts on macOS and Linux](../nexus-mods/free-vs-premium.md#free-accounts-on-macos-and-linux) has the steps.
{% endtab %}
{% endtabs %}

## A red message at the bottom of the menu

These messages are about Stardrop's `Settings.json` or a profile file. DewDrop shows them at the bottom of the menu (and on the **Installed** tab), and **Apply & Restart** stays off until they're sorted.

* **"Stardrop's Settings.json couldn't be read, so your profile and Nexus login didn't load and changes to them aren't saved. Close the game and start it again."** Another program probably had the file open when the game started, such as a backup or sync tool. Close the game and start it again.
* **"The '...' profile file couldn't be read, so DewDrop won't change or apply it. Pick another profile, or close the game and start it again."** On the **Profiles** tab, pick another profile and click **Use this profile**. DewDrop never writes over a profile file it can't read, so you can fix or delete that file yourself in the `Profiles` folder. See [Where DewDrop keeps files](../reference/file-locations.md).

If `Settings.json` is damaged rather than locked, there's no message. DewDrop keeps a `Settings.json.bak-<date>-<time>` copy and starts fresh, so your active profile goes back to **Default** and you need to log in to Nexus Mods again. A damaged `Default.json` is kept as a `.bak` copy too.
