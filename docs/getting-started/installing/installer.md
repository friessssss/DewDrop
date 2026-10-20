---
description: "The easiest way to install DewDrop: it finds your game, sets up SMAPI if needed and puts DewDrop in your Mods folder."
icon: box-open
---

# Install with the installer

The installer does the fiddly parts for you. It finds your game, installs or updates SMAPI (the mod loader DewDrop runs on) if you need it, and copies DewDrop into your `Mods` folder. It asks before it changes anything important.

Before you start, check [What you need](../requirements.md). The installer has been tested on Windows and Linux, but not on macOS yet (see [Platform support](../../reference/platform-support.md)).

## The quick way on macOS and Linux

Open **Terminal** (on a Mac, press **Command + Space**, type `Terminal` and press **Return**), paste this line and press **Return**:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
```

It checks whether you're on macOS or Linux, downloads the installer and the latest DewDrop release, and runs the installer in that window. macOS doesn't block it, so there's nothing to allow in **Privacy & Security**. Then [answer the questions](#answer-the-questions) and [start the game](#start-the-game) as in the steps below.

To pass [installer options](../../reference/installer-options.md), put them after `bash -s --`, for example `... | bash -s -- --dry-run`.

## Install in five steps

{% stepper %}
{% step %}
### Download the installer

Open the [latest DewDrop release](https://github.com/friessssss/DewDrop/releases/latest). Under **Assets**, click `DewDrop-<version>-installer.zip`. You don't need the **Source code** downloads.
{% endstep %}

{% step %}
### Extract the zip

The installer can't run from inside the zip, so extract it first.

* **Windows:** right-click the zip, choose **Extract All...**, then click **Extract**. You get a `DewDrop-<version>-installer` folder with a `DewDrop <version> installer` folder inside it.
* **macOS:** double-click the zip.
* **Linux:** right-click the zip and choose your file manager's extract option.
{% endstep %}

{% step %}
### Run the installer for your computer

Open the `DewDrop <version> installer` folder and double-click the file for your computer:

* **Windows:** `Install on Windows.bat`. Windows may hide the `.bat` part, so it can show as **Install on Windows**.
* **macOS:** `Install on macOS.command`.
* **Linux:** `Install on Linux.sh`.

A window full of text opens. That's the installer. If your computer warns you about it, see [Running the installer on your computer](#running-the-installer-on-your-computer).
{% endstep %}

{% step %}
### Answer the questions

At a yes/no question, the capital letter is the recommended answer (`[Y/n]` means yes, `[y/N]` means no). Press **Enter** to pick it, or type **Y** or **N** and press **Enter**. At a numbered list, type a number and press **Enter**.

On Windows with Steam, it offers to change one Steam setting so that **Play** starts SMAPI. Steam has to close for this, and opens again when the installer starts the game.

It also asks **Start the game in your DewDrop profile when you press Play in Steam?** Press **Enter** for yes. Then **Play** loads only the mods that are on in your profile, so you don't need **Apply & Restart** to get into it. The installer doesn't make this change itself: DewDrop finishes it the next time you quit the game, when Steam closes and opens again once. You can change it any time in **Mods** > **Account**.

<figure><img src="../../.gitbook/assets/installer-windows.png" alt="The DewDrop installer on Windows: it finds the game, checks SMAPI, installs DewDrop and ends with All done"><figcaption><p>The installer on Windows.</p></figcaption></figure>

When you see **All done!**, DewDrop and SMAPI are installed.
{% endstep %}

{% step %}
### Start the game

The installer asks **Start Stardew Valley now?** (not for the Xbox app). Press **Enter**.

SMAPI's window opens next to the game and lists what's loading. Leave it open: closing it closes the game. On the title screen, click the new **Mods** button. That's it!
{% endstep %}
{% endstepper %}

{% hint style="success" %}
You're done. Next, read [Your first launch](../first-launch.md). To learn when your profile's mods load, and when the game needs a restart, see [How launching works](../launching.md).
{% endhint %}

## How to start the game from now on

| Your setup | Start the game with mods |
| --- | --- |
| Windows, Steam, launch option set by the installer | Press **Play** in Steam. If you said yes to starting in your DewDrop profile, **Play** starts the game in it once you've quit the game for the first time. |
| Windows, Steam, you said no to the launch option | Use the **Stardew Valley (SMAPI)** desktop shortcut. |
| Windows, GOG or another setup | Use the **Stardew Valley (SMAPI)** desktop shortcut. GOG Galaxy's **Play** button starts the game **without** mods unless you [set Galaxy up](smapi.md#gog-galaxy-on-windows). |
| Windows, Xbox app | Press **Play** in the Xbox app. |
| macOS or Linux | Start it however you usually do. Steam's **Play** button works. |

If no SMAPI window opens, the game started without mods. One exception: on macOS, a game that DewDrop restarted with **Apply & Restart** doesn't open a new Terminal window. If the **Mods** button is on the title screen, your mods loaded.

## Running the installer on your computer

{% tabs %}
{% tab title="Windows" %}
**"Windows protected your PC":** click **More info**, then **Run anyway**. Windows shows this for new downloads it doesn't recognize yet.

**A message that starts with "Oops!":** the installer couldn't start, and the message says why. See [Troubleshooting](../../help/troubleshooting.md#the-installer-fails-to-start).

**It can't find your game:** in the folder picker, choose the folder with the Stardew Valley game icon in it (`Stardew Valley.exe`). To open it from Steam, right-click the game and choose **Manage** > **Browse local files**.

**"Windows won't let me change files":** the game is in a protected folder, such as Program Files. Let the installer restart itself as administrator, click **Yes** when Windows asks, and carry on in the new window. It still starts Steam and the game as you, not as administrator.
{% endtab %}

{% tab title="macOS" %}
**"Install on macOS.command" Not Opened:** macOS shows this for downloaded scripts that Apple hasn't checked. The easiest fix is to use [the Terminal line](#the-quick-way-on-macos-and-linux) instead. Click **Done** (not **Move to Trash**). Open **System Settings** > **Privacy & Security**, scroll down, click **Open Anyway** and enter your password. On older macOS, right-click the file and choose **Open** instead.

**Double-clicking does nothing:** open Terminal, type `bash` and a space, drag `Install on macOS.command` into the window, and press **Return**.

**"Install Rosetta now?":** SMAPI's installer is made for Intel Macs, so a Mac with an Apple chip (M1 or later) needs Apple's free Rosetta add-on to run it. Press **Enter**, then type your Mac password (it doesn't show as you type) and press **Return**.

**"Operation not permitted":** macOS is blocking Terminal from your game folder. Switch on **Terminal** in **System Settings** > **Privacy & Security** > **Full Disk Access**, then run the installer again. You can switch it off there afterwards.

**macOS later says SMAPI is malware:** it says this about apps Apple hasn't checked. SMAPI is the standard, open-source Stardew Valley mod loader. See "Enable macOS security permissions" in [SMAPI's macOS guide](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac).
{% endtab %}

{% tab title="Linux" %}
**Double-clicking opens a text editor, or nothing happens:** right-click an empty spot in the folder, choose **Open in Terminal**, type `bash "Install on Linux.sh"` and press **Enter**.

**"This is the Windows version of the game":** Steam is running it through Proton, Steam's tool for running Windows games on Linux. DewDrop needs the Linux version. In Steam, right-click **Stardew Valley** and choose **Properties** > **Compatibility**. Untick **Force the use of a specific Steam Play compatibility tool**, let Steam download the Linux version, then run the installer again.

**What it needs:** `curl` or `wget`, and `unzip` or `python3`. Most desktop Linux systems have them.
{% endtab %}
{% endtabs %}

## What the installer does

<figure><img src="../../.gitbook/assets/diagram-installer-steps.png" alt="The installer's steps: find the game, check SMAPI, install it if needed, copy DewDrop into Mods, set up Steam or a shortcut, play"><figcaption><p>The installer's steps.</p></figcaption></figure>

<details>

<summary>What the installer does, step by step</summary>

1. **Finds your game** (Steam on any drive, GOG, the Xbox app or the usual folders), and asks which one you play if there are several.
2. **Checks the game:** version 1.6 or later on Windows, the Linux version rather than Proton on Linux, and whether the game has ever been started.
3. **Waits for the game to close.**
4. **Checks SMAPI.** If it's missing or older than 4.0, it downloads the official installer from [SMAPI's GitHub releases](https://github.com/Pathoschild/SMAPI/releases) and runs it, stopping it if it's stuck after 3 minutes. If a newer SMAPI is out, it offers the update. On macOS and Linux, if some game files belong to another user (for example, after a `sudo` install), it stops first and shows the `sudo chown` command that fixes it.
5. **Copies DewDrop into `Mods/DewDrop`**, or updates your copy where it is and keeps your `config.json` settings. The old copy is only removed after the new one is in place. It doesn't swap a newer DewDrop for an older one unless you say yes. A mod that only needs DewDrop is never mistaken for it.
6. **Offers to turn off extra copies** of DewDrop by putting a `.` at the start of their folder names, so SMAPI skips them.
7. **Reminds Stardrop users** what to do on their first launch.
8. **Sets up how you start the game** (Windows only):
   * **Steam:** it sets the launch option `"<your game folder>\StardewModdingAPI.exe" %command%` for the signed-in Steam account, after closing Steam and backing up its `localconfig.vdf`. It replaces other launch options only if you say yes, and offers to fix one that starts SMAPI from another folder. It also asks whether Steam should start the game in your DewDrop profile, and saves your answer in DewDrop's `config.json`. If you say yes, DewDrop adds SMAPI's `--mods-path` option to the launch option the next time you quit the game. A `--mods-path` you set to another folder yourself is kept, unless you say yes. See [Installer command-line options](../../reference/installer-options.md#starting-in-your-profile).
   * **Xbox app:** it renames `Stardew Valley.exe` to `Stardew Valley original.exe` and puts a copy of SMAPI in its place. After a game update, run the installer again: the updated game becomes the new `Stardew Valley original.exe`.
   * **Anything else:** it offers a **Stardew Valley (SMAPI)** desktop shortcut.
9. **Offers to start the game.**

It never deletes your mods, saves or settings. SMAPI's installer adds or updates its two bundled mods, Console Commands and Save Backup.

</details>

## Running it again

Run the installer again whenever you like: to [update DewDrop](../updating-dewdrop.md) or SMAPI, to set up the Steam launch option or shortcut you skipped, or to redo the Xbox app setup after a game update.

If it says **Stardrop is installed too** and you don't use Stardrop, ignore it. DewDrop created the settings file the installer looks for.

## If something goes wrong

If the installer has to stop, it says why in a red `[x]` line. [Troubleshooting](../../help/troubleshooting.md#the-installer) explains the common messages.

It also saves a log, replacing the last one: `%TEMP%\DewDrop-installer.log` on Windows, or `DewDrop-installer.log` in `$TMPDIR` (or `/tmp`) on macOS and Linux. Attach it if you [ask for help](../../help/reporting-issues.md).

## Command-line options

To choose the game folder yourself, preview what the installer would do, or install without questions, see [Installer command-line options](../../reference/installer-options.md).
