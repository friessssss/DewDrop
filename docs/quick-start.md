---
description: "Install DewDrop, open the Mods menu and load your first profile in a few minutes."
icon: rocket
---

# Quick start

Five steps take you from nothing to your first profile. The installer puts DewDrop in your game, and also sets up SMAPI, the free tool that loads Stardew Valley mods, if you need it.

## Before you start

* You need **Stardew Valley 1.6 or later** and an internet connection. See [What you need](getting-started/requirements.md).
* **On a Steam Deck?** Follow [DewDrop on the Steam Deck](getting-started/steam-deck.md) instead. You run the installer once in Desktop Mode.
* **Use Stardrop?** Don't start your first launch from Stardrop. [Here's why](stardrop.md#first-time-setup-if-you-already-use-stardrop).
* DewDrop is mainly tested on Windows so far. See [Platform support](reference/platform-support.md).

## Five steps

{% stepper %}
{% step %}
### Start the installer

Paste one line. It downloads the installer and the latest DewDrop, and runs the installer in that window.

* **Windows:** open **PowerShell** (click **Start**, type `PowerShell` and press **Enter**), paste this and press **Enter**:

  ```powershell
  irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | iex
  ```
* **macOS or Linux:** open **Terminal** (on a Mac, press **Command + Space**, type `Terminal` and press **Return**), paste this and press **Return**:

  ```bash
  curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
  ```

If the line shows an error, or you'd rather double-click a file, [download the installer](getting-started/installing/installer.md#or-download-it-five-steps) instead. It asks the same questions.
{% endstep %}

{% step %}
### Answer the questions

At each question, press **Enter** to pick the recommended answer. Along the way:

* **On a Mac with an Apple chip,** it may first offer to install Rosetta, which SMAPI's installer needs. Press **Enter**, then type your Mac password.
* **On Windows,** a second window (SMAPI's own installer) may open and close by itself.
* **With Steam on Windows,** it offers to change the game's launch option in Steam, so **Play** starts the game with mods. Steam closes for this, and opens again when the installer starts the game. It also asks **Start the game in your DewDrop profile when you press Play in Steam?** Press **Enter** for yes. Step 5 shows what that does.

When it says **All done!**, press **Enter** at **Start Stardew Valley now?**. If it stops with a red message, follow what it says, or see [Troubleshooting](help/troubleshooting.md#the-installer).

<figure><img src=".gitbook/assets/installer-windows.png" alt="The DewDrop installer's window on Windows, finding the game, checking SMAPI and copying DewDrop"><figcaption><p>The installer on Windows.</p></figcaption></figure>
{% endstep %}

{% step %}
### Open the Mods menu

The game starts with SMAPI's window next to it (a terminal window on macOS and Linux). It lists what's loading, and yellow lines are normal. Leave it open: closing it closes the game. Game didn't start? See [Starting the game with mods](#starting-the-game-with-mods).

On the title screen, click the new **Mods** button between **Co-op** and **Exit**. In a small game window, it's a little dew-drop button at the bottom right instead.

<figure><img src=".gitbook/assets/shot-title.png" alt="The Stardew Valley title screen with DewDrop's Mods button between the Co-op and Exit buttons"><figcaption><p>The Mods button on the title screen.</p></figcaption></figure>

DewDrop opens on the **Installed** tab. It made a profile called **Default**. A profile is a saved list of which mods are on, and Default starts with every mod that loaded this time. So the line at the bottom says "The mods loaded right now match this profile." and **Apply & Restart** is grayed out: there's nothing to restart for yet.

Already use Stardrop? If a **Load your profile?** box opens first, click **Restart now**. The game restarts with your Stardrop profile's mods, and then you can click **Mods**.
{% endstep %}

{% step %}
### Turn a mod off

Besides DewDrop, you'll see **Console Commands** and **Save Backup**, two small mods that come with SMAPI. Keep **Save Backup** on: it backs up your saves. If those are the only mods listed, skip to **From now on** in step 5.

Click a mod's row to turn it off. It says **Off after restart**, because it stays loaded until the game restarts. The line at the bottom turns orange, and **Apply & Restart** lights up. Only testing? Click the row again to turn the mod back on.
{% endstep %}

{% step %}
### Apply & Restart

Click **Apply & Restart** at the bottom right, then **Restart**. The dialog lists any mods that will turn off.

<figure><img src=".gitbook/assets/shot-apply-restart.png" alt="DewDrop's restart dialog for the Default profile, with Restart and Cancel buttons"><figcaption><p>The restart dialog.</p></figcaption></figure>

The game closes and comes back with only your profile's mods. Open **Mods** again: the orange line is gone. On macOS, SMAPI's window usually doesn't come back after a restart; if the **Mods** button is there, your mods loaded. If the game doesn't come back within a minute, start it yourself and see [Troubleshooting](help/troubleshooting.md#the-game-closed-and-never-came-back).

**From now on**, a normal launch loads every mod in your `Mods` folder, even the ones you turned off. DewDrop gets you back into your profile:

* **If you said yes to starting in your profile,** Steam's **Play** button starts the game straight in your profile. On Windows, the first time you quit the game, Steam closes and opens again once, a few seconds later, to save that.
* **Otherwise,** when the mods that loaded differ from your profile, a **Load your profile?** box opens on the title screen. Click **Restart now**. With the Steam version on Windows, or with DewDrop's Steam launcher, the box also has a **Start in my profile from Steam from now on** checkbox. Leave it ticked, and **Play** starts in your profile from then on.

Mods you copy into `Mods` yourself are turned on in your profile when the game starts, like the ones DewDrop downloads (unless you turn off **Turn on mods when installed**). [How launching works](getting-started/launching.md) explains the rest.

That's it!
{% endstep %}
{% endstepper %}

## Starting the game with mods

| Your setup | How to start the game with mods |
| --- | --- |
| Windows, Steam | Press **Play** if you let the installer change Steam. Otherwise, use the **Stardew Valley (SMAPI)** desktop shortcut. |
| Windows, GOG or another store | Use the **Stardew Valley (SMAPI)** desktop shortcut. GOG Galaxy's **Play** button starts the game without mods unless you [set Galaxy up](getting-started/installing/smapi.md#gog-galaxy-on-windows). |
| Xbox app | Press **Play** in the Xbox app. Run the DewDrop installer again after each game update. |
| macOS and Linux | Start the game however you usually do, including Steam's **Play**. If SMAPI's window stops appearing after a game update, run [SMAPI's installer](getting-started/installing/smapi.md#macos-and-linux) again. |
| Steam Deck, or Linux with DewDrop's Steam launcher | Press **Play** in Steam. The launcher starts SMAPI itself, so a game update doesn't turn mods off. |

If no SMAPI window opens, the game started without mods. There are two exceptions: DewDrop's Steam launcher never shows that window, and on macOS a game that DewDrop restarted doesn't either. There, the **Mods** button on the title screen tells you that mods loaded.

## Where to next

* [How launching works](getting-started/launching.md): why a normal launch and **Apply & Restart** load different mods.
* [Connecting your Nexus account](nexus-mods/connecting.md): log in so you can install mods from **Browse**.
* [Managing your mods](guides/installed.md): turn mods on and off, update them and uninstall them.
