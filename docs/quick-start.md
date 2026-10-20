---
description: "Install DewDrop, open the Mods menu and switch to your first profile in a few minutes."
icon: rocket
---

# Quick start

The installer puts DewDrop in your game and also sets up SMAPI, the free tool that loads Stardew mods, if you need it.

## Before you start

* You need **Stardew Valley 1.6 or later** and an internet connection. See [What you need](getting-started/requirements.md).
* Use Stardrop? Don't start your first launch from Stardrop. [Here's why](stardrop.md#first-time-setup-if-you-already-use-stardrop).
* DewDrop is only tested on Windows so far. Steam Deck support is new and being tested: to set up a Deck, follow [DewDrop on the Steam Deck](getting-started/steam-deck.md). See also [Platform support](reference/platform-support.md).

## Five steps

{% stepper %}
{% step %}
### Start the installer

Paste one line, and it downloads the installer and the latest DewDrop for you.

* **Windows:** open **PowerShell** (click **Start**, type `PowerShell` and press **Enter**), paste this and press **Enter**:

  ```powershell
  irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex
  ```
* **macOS or Linux:** open **Terminal** (on a Mac, press **Command + Space**, type `Terminal` and press **Return**), paste this and press **Return**:

  ```bash
  curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
  ```

<details>

<summary>Prefer to download the installer?</summary>

Download `DewDrop-<version>-installer.zip` from the [latest DewDrop release](https://github.com/friessssss/DewDrop/releases/latest) and extract it (on Windows, right-click it and choose **Extract All**; on macOS, Safari usually unzips it for you). Open the `DewDrop <version> installer` folder and double-click the file for your computer:

* **Windows:** **Install on Windows** (`Install on Windows.bat`). If you see **Windows protected your PC**, click **More info**, then **Run anyway**.
* **macOS:** `Install on macOS.command`. If macOS won't open it, click **Done** (not **Move to Trash**). Then go to **System Settings** > **Privacy & Security**, scroll down, click **Open Anyway** and enter your password. (Older macOS: right-click it and choose **Open**.)
* **Linux:** `Install on Linux.sh`. If it opens in a text editor, right-click an empty spot in the folder, choose **Open in Terminal**, type `bash "Install on Linux.sh"` and press **Enter**.

</details>
{% endstep %}

{% step %}
### Answer the questions

The installer runs in that window. For each question, press **Enter** to pick the recommended answer. On a Mac with an Apple chip, it may first offer to install Rosetta, which SMAPI's installer needs: press **Enter** and type your Mac password. On Windows, a second window (SMAPI's own installer) may open and close by itself.

With Steam on Windows, it offers to change one Steam setting, the game's launch option, so **Play** starts the game with mods. Steam closes for this and opens again when the installer starts the game. It also asks **Start the game in your DewDrop profile when you press Play in Steam?** Press **Enter** for yes. You'll see what that does in step 5.

On a Steam Deck, and on Linux if you run the installer with [`--steam-setup`](reference/installer-options.md#steam-on-linux-and-the-steam-deck), it sets that launch option to DewDrop's launcher instead and asks the same question. Steam closes for a few seconds to save it, then opens again. A Deck also asks for your Nexus Mods API key, and doesn't offer to start the game: that's for Game Mode.

When it says **All done!**, press **Enter** at **Start Stardew Valley now?**, then press **Enter** again to close the window. If it stops with a red message, follow what it says, or see [Troubleshooting](help/troubleshooting.md#the-installer).

<figure><img src=".gitbook/assets/installer-windows.png" alt="The DewDrop installer's window on Windows, finding the game, checking SMAPI and copying DewDrop"><figcaption><p>The installer on Windows.</p></figcaption></figure>
{% endstep %}

{% step %}
### Open the Mods menu

The game starts with SMAPI's window next to it (a Terminal window on macOS and Linux, and none at all when Steam starts the game with DewDrop's launcher, as on a Steam Deck). It lists what's loading, and yellow lines are normal. Leave it open: closing it closes the game. Game didn't start? See [Starting the game with mods](#starting-the-game-with-mods).

On the title screen, click the new **Mods** button between **Co-op** and **Exit**. In a small window, it's a little dew-drop button at the bottom right. Already use Stardrop? If a **Load your profile?** box opens first, click **Restart now**. The game restarts with your Stardrop profile's mods, and then you can click **Mods**.

<figure><img src=".gitbook/assets/shot-title.png" alt="The Stardew Valley title screen with DewDrop's Mods button between the Co-op and Exit buttons"><figcaption><p>The Mods button on the title screen.</p></figcaption></figure>

DewDrop opens on the **Installed** tab. It made a profile called **Default**. A profile is a saved list of which mods are on, and Default has every mod that loaded this time.

The line at the bottom says "The mods loaded right now match this profile." and **Apply & Restart** is grayed out: there's nothing to restart for yet.
{% endstep %}

{% step %}
### Turn a mod off

Besides DewDrop, you'll see **Console Commands** and **Save Backup**, two small mods that SMAPI installs. Keep **Save Backup** on: it backs up your saves. If those are the only mods listed, skip to step 5.

Click a mod's row to turn it off. It says **Off after restart**: it stays loaded until you restart. The line at the bottom turns orange, and **Apply & Restart** lights up. Only testing? Click it again to turn it back on.
{% endstep %}

{% step %}
### Apply & Restart

Click **Apply & Restart** at the bottom right, then **Restart**. The dialog lists any mods that will turn off. If you didn't turn anything off, the button stays grayed out: skip to **From now on** below.

<figure><img src=".gitbook/assets/shot-apply-restart.png" alt="DewDrop's restart dialog for the Default profile, with Restart and Cancel buttons"><figcaption><p>The restart dialog.</p></figcaption></figure>

The game closes and comes back with only your profile's mods. Open **Mods** again: the orange line is gone. On macOS, SMAPI's window usually doesn't come back after this restart. If the **Mods** button is there, your mods loaded. If the game doesn't come back within a minute, start it yourself and see [Troubleshooting](help/troubleshooting.md).

**From now on**, it depends on your setup:

* **Windows and Steam, and you said yes in the installer:** the first time you quit the game, Steam closes and opens again once, a few seconds after the game closes. That's how DewDrop saves Steam's new launch option. From then on, **Play** starts the game straight in your profile, with no restart. Change your mods in the menu, quit, and the next **Play** loads them.
* **Steam Deck, or Linux with DewDrop's Steam launcher, and you said yes in the installer:** **Play** starts the game straight in your profile from now on, with no restart, and Steam never closes. Change your mods in the menu, quit, and the next **Play** loads them.
* **Everyone else:** a normal launch loads every mod in your `Mods` folder, even ones you turned off. When those differ from your profile, a **Load your profile?** box opens on the title screen. Click **Restart now**, and the game restarts into your profile. On Windows with Steam, the box also has a **Start in my profile from Steam from now on** checkbox, ticked at first. Leave it ticked, and Steam closes and opens again once after you quit. From then on, **Play** starts in your profile. With DewDrop's Steam launcher (Steam Deck, or Linux after `--steam-setup`), the box has the checkbox too, and Steam doesn't close.

See [Start in your profile](guides/start-in-your-profile.md) for more.

If you never turn mods off, the mods already match and the box doesn't open. Mods you copy into `Mods` yourself are turned on in your profile when the game starts, like the ones DewDrop downloads (unless you turn off **Turn on mods when installed**).

That's it!
{% endstep %}
{% endstepper %}

## Starting the game with mods

* **Windows, Steam:** press **Play** if you let the installer change Steam. Otherwise, use the **Stardew Valley (SMAPI)** desktop shortcut. With **Start in my profile from Steam** on, **Play** starts the game in your profile, while the shortcut still loads your whole `Mods` folder.
* **Windows, GOG or other:** use the **Stardew Valley (SMAPI)** desktop shortcut. GOG Galaxy's **Play** button starts the game without mods unless you [set Galaxy up](getting-started/installing/smapi.md#gog-galaxy-on-windows).
* **Xbox app:** press **Play** in the Xbox app. Run the DewDrop installer again after each game update.
* **macOS and Linux:** start the game however you usually do, including Steam's **Play**. If SMAPI's window stops appearing after a game update, run [SMAPI's installer](getting-started/installing/smapi.md#macos-and-linux) again.
* **Steam Deck, or Linux with DewDrop's Steam launcher:** press **Play** in Steam. The launcher starts SMAPI itself, so a game update doesn't turn mods off.

If no SMAPI window opens, the game started without mods. The exceptions are macOS after DewDrop restarts the game, with **Apply & Restart** or **Restart now**, and DewDrop's Steam launcher, which never shows one. There, the **Mods** button on the title screen shows that mods loaded.

## Where to next

* [How launching works](getting-started/launching.md): why a normal launch and **Apply & Restart** load different mods, and the ways to start in your profile.
* [Connecting your Nexus account](nexus-mods/connecting.md): log in so you can install mods from **Browse**.
