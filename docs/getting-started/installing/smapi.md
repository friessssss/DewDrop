---
description: "What SMAPI is, how to install or update it yourself, and how to make Steam, GOG Galaxy or the Xbox app start it."
icon: puzzle-piece
---

# About SMAPI

SMAPI is the mod loader for Stardew Valley, and DewDrop runs on it. This page explains what it is, how to install or update it yourself, and how to make your game launcher start it. If you use the DewDrop [installer](installer.md), it handles most of this for you.

## What is SMAPI?

SMAPI (short for Stardew Modding API) starts Stardew Valley with mods loaded. It's free, open source and made by Pathoschild. Almost every Stardew Valley mod needs it. DewDrop is a SMAPI mod too.

A few things to know:

* **Mods live in the `Mods` folder** inside your game folder. SMAPI creates it when you install SMAPI.
* **SMAPI opens a second window** next to the game: the SMAPI console. It shows what SMAPI is loading and any errors. Leave it open while you play. Closing it closes the game.
* **SMAPI has to be started instead of the plain game.** Installing it doesn't change what Steam's **Play** button does on Windows. See [Starting the game with SMAPI](#starting-the-game-with-smapi).

The official site is [smapi.io](https://smapi.io), and the Stardew Valley wiki has a [player's guide to getting started with mods](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started).

## Do I already have it?

Look in your game folder (see [Find your Mods folder](manual.md#find-your-mods-folder) for where it is):

* **Windows:** if there's a `StardewModdingAPI.exe` file, SMAPI is installed.
* **macOS and Linux:** look for a file called `StardewModdingAPI`.

The SMAPI console also shows its version near the top when the game starts. DewDrop needs **SMAPI 4.0 or later**, and the DewDrop installer checks this for you.

## Install or update SMAPI yourself

The easiest way is the DewDrop [installer](installer.md). It installs SMAPI if it's missing and offers an update when a newer version is out.

To do it by hand:

1. If you've never played on this computer, start the game once without mods and close it at the title screen.
2. Download the latest SMAPI from [smapi.io](https://smapi.io).
3. Extract the zip somewhere that isn't your game folder. Your Downloads folder is fine.
4. In the extracted folder, run the installer for your computer: `install on Windows.bat`, `install on macOS.command` or `install on Linux.sh`. Follow the instructions on screen.
5. On Windows, set up your game launcher so it starts SMAPI. See below.

SMAPI's own guides have pictures and fixes for common problems: [Windows](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows), [macOS](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac), [Linux](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux).

**To update SMAPI**, install the new version the same way. SMAPI's installer replaces the old version, and you don't need to change your Steam or GOG Galaxy settings again.

## Starting the game with SMAPI

### Steam on Windows

On Windows, Steam's **Play** button starts the plain game unless you tell it to start SMAPI. Starting SMAPI through Steam keeps the Steam overlay, achievements and playtime working. (Games that DewDrop restarts, with **Apply & Restart** or **Restart now** in the **Load your profile?** box, start outside Steam, and whether Steam counts those hasn't been tested yet.)

The DewDrop installer offers to set this for you, for the Steam account that's signed in. To do it yourself:

1. In Steam, right-click **Stardew Valley** and choose **Properties**.
2. On the **General** page, find the **Launch Options** box.
3. If the box already has text, copy it into Notepad first so you can put it back. Then replace it with this line, changed to match your game folder:

   ```
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command%
   ```

   Keep both quote marks and the `%command%` part. They're both needed.
4. Close the window. From now on, **Play** starts SMAPI.

Not sure where your game folder is? Right-click **Stardew Valley** and choose **Manage** > **Browse local files**. SMAPI's installer also prints the exact line for your computer when it finishes.

DewDrop's **Apply & Restart** works with this setup.

DewDrop can also add SMAPI's `--mods-path` option to this line, so **Play** starts the game straight in your DewDrop profile. It only does that if you turn on **Start in my profile from Steam** on the **Account** tab, or say yes when the installer asks. The change happens when you quit the game, and Steam closes and opens again once for it. The line then looks like this:

```
"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" --mods-path "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods" %command%
```

So if you find a `--mods-path` part pointing at `Selected Mods` in the box, DewDrop probably put it there. To take it out, untick the setting and quit the game. If you delete it by hand while the setting is on, DewDrop adds it back the next time you quit. See [Start in your profile](../../guides/start-in-your-profile.md).

### GOG Galaxy on Windows

The simplest way is to start `StardewModdingAPI.exe` in your game folder, or the **Stardew Valley (SMAPI)** desktop shortcut that the DewDrop installer offers.

To make Galaxy's **Play** button start SMAPI, and keep Galaxy's playtime tracking, follow the GOG Galaxy steps in [SMAPI's Windows guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#GOG_Galaxy). In short, you make a small `start.bat` file in your game folder and add it in Galaxy under **Manage installation** > **Configure** > **Custom executables / arguments**.

### Xbox app

The Xbox app (PC Game Pass) always starts `Stardew Valley.exe`, so SMAPI's guide swaps in a copy of SMAPI under that name:

1. Install SMAPI into the game's `Content` folder: by default, `C:\XboxGames\Stardew Valley\Content`. To open it, click **...** > **Manage** in the Xbox app, then **Files** > **Browse**.
2. In that folder, rename `Stardew Valley.exe` to `Stardew Valley original.exe`.
3. Make a copy of `StardewModdingAPI.exe` and name the copy `Stardew Valley.exe`.

Now start the game from the Xbox app as usual. The DewDrop installer offers to do steps 2 and 3 for you.

{% hint style="warning" %}
After every game update, you need to do these steps again, or the game starts without mods. SMAPI's guide says to run its installer again too, not only to redo the swap. Running the DewDrop installer again redoes the swap for you: the updated game becomes the new `Stardew Valley original.exe`, and SMAPI's copy goes back in its place. It only reinstalls SMAPI if it's missing or out of date. SMAPI's guide also notes that Game Pass folder permissions can get in the way. See [SMAPI's Windows guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app).
{% endhint %}

### macOS and Linux

There's nothing to set up. SMAPI's installer renames the game's own launcher to `StardewValley-original` and puts a SMAPI launcher in its place. So the game starts SMAPI however you start it, including Steam's **Play** button. You don't need launch options; those are only for Windows. If you play on Steam, SMAPI's macOS guide recommends starting the game through the Steam client.

{% hint style="info" %}
**After a game update**, the game's launcher can be replaced and the game starts without SMAPI (no SMAPI window appears). To fix it, run SMAPI's own installer again. The DewDrop installer only reinstalls SMAPI when it's missing or out of date, so it won't repair this.
{% endhint %}

On macOS, SMAPI needs macOS 10.15 Catalina or later. If macOS blocks SMAPI or calls it malware, follow the "Enable macOS security permissions" part of [SMAPI's macOS guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac).

## How DewDrop starts SMAPI

When you click **Apply & Restart**, or **Restart now** when DewDrop asks to load your profile, DewDrop closes the game and starts SMAPI again itself, not through Steam, and tells it to load only the mods in your profile. With **Start in my profile from Steam** on, Steam's **Play** button tells SMAPI the same thing, so the game starts in your profile without that restart. [How launching works](../launching.md) explains why that matters, and [How DewDrop works](../../reference/how-it-works.md) has the technical details.

## Problems with SMAPI

* **The game starts without the SMAPI window:** your launcher isn't starting SMAPI. Check the section for your launcher above. (On macOS, a game that DewDrop restarted with **Apply & Restart** runs SMAPI without opening a window, so the missing window isn't a problem there.)
* **SMAPI shows errors:** open [smapi.io/log](https://smapi.io/log) to upload your SMAPI log and read what went wrong. The wiki's [troubleshooting guide](https://stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting) covers common SMAPI problems.
* **Something's wrong with DewDrop:** see [Troubleshooting](../../help/troubleshooting.md) and [Reporting issues](../../help/reporting-issues.md).

To remove SMAPI, run SMAPI's installer again and choose uninstall, then clear the Steam launch option or undo your launcher changes. (For the Xbox app, SMAPI's Windows guide explains how to undo the swap instead.) Without SMAPI, no mods load, DewDrop included. To remove only DewDrop, see [Uninstalling DewDrop](../uninstalling.md).
