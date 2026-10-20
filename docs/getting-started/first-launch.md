---
description: "What you'll see the first time you start the game with DewDrop, and how you get into your profile after that."
icon: flag
---

# Your first launch

DewDrop is installed. Here's where the Mods button is, what DewDrop set up, and when you'll need a restart.

## Start the game

If the installer started the game for you, this is already your first launch. Otherwise, start it the way that fits your setup:

* **Windows, Steam:** click **Play**. If you said no when the installer offered to set up Steam, use the **Stardew Valley (SMAPI)** desktop shortcut instead.
* **Windows, GOG:** use the **Stardew Valley (SMAPI)** desktop shortcut. GOG Galaxy's **Play** button starts the game without mods, unless you've [set Galaxy up for SMAPI](installing/smapi.md#gog-galaxy-on-windows).
* **Xbox app:** click **Play** in the Xbox app.
* **macOS and Linux:** start the game however you usually do. Steam's **Play** button already starts SMAPI.
* **Steam Deck:** press **Play** in Game Mode. See [DewDrop on the Steam Deck](steam-deck.md).

Use Stardrop? Start this first launch one of the ways above, not from Stardrop. More in [Using DewDrop with Stardrop](../stardrop.md#first-time-setup-if-you-already-use-stardrop).

SMAPI's window opens next to the game: a black console window on Windows, a terminal window on macOS and Linux. It lists what's loading, and yellow lines are normal. Leave it open: closing it closes the game. If no SMAPI window opens, the game started without mods, unless Steam started it with DewDrop's launcher (on a Steam Deck, or on Linux after the installer's `--steam-setup`), which never shows one. Then the **Mods** button on the title screen shows that mods loaded.

## Find the Mods button

On the title screen, DewDrop adds a **Mods** button between **Co-op** and **Exit**. In a small game window, it's a little dew-drop button above the small buttons in the bottom-right corner instead.

<figure><img src="../.gitbook/assets/shot-title.png" alt="The Stardew Valley title screen with five buttons in a row: New, Load, Co-op, Mods and Exit."><figcaption><p>The Mods button sits between Co-op and Exit.</p></figcaption></figure>

Don't see it? See [No Mods button?](#no-mods-button) below. If a **Load your profile?** box opens over the title screen first, see [Next time you play](#next-time-you-play). That can happen on a first launch if you already use Stardrop.

Click **Mods** (or press **A** on a controller) to open the DewDrop menu on the **Installed** tab. The back button in the bottom-right corner (or **B**) closes it. The button is only on the title screen, not in a loaded save. [The DewDrop menu](../guides/the-menu.md) explains everything on screen.

## Your Default profile

A profile is a saved list of which mods are on. DewDrop made one called **Default** with every mod that loaded this time, so applying it changes nothing. (Already use Stardrop? You'll see your last Stardrop profile instead.) See [Profiles](../guides/profiles.md).

* **Console Commands** and **Save Backup** are two small mods that SMAPI installs. Keep **Save Backup** on: it backs up your saves.
* **Mods you copy into `Mods` yourself are turned on for you** when the game starts, as long as **Turn on mods when installed** is on (it is at first). If the game is already running, click **Rescan** on the **Installed** tab so they show up. Those say **On after restart** until the game starts again.

## The line at the bottom

Look at the bottom of the menu. You'll see **Profile: Default**, a gray line saying **The mods loaded right now match this profile.**, and a grayed-out **Apply & Restart** button.

You started the game the normal way, so SMAPI loaded everything in your `Mods` folder. Your profile only applies when DewDrop starts the game itself. But Default has every mod that loaded this time, so restarting wouldn't change anything yet.

Already use Stardrop? If your profile turns on different mods than the ones that loaded, the line is orange instead and says what's different. [How launching works](launching.md#which-kind-of-session-am-i-in) lists every line.

## Your first Apply & Restart

Mods load once, when the game starts. So when you change your mods, for example turn one off on the **Installed** tab, the change waits for a restart. The line at the bottom turns orange and says what's different, such as **1 mod that is off in this profile loaded anyway. Restart to load this profile.**, and **Apply & Restart** lights up. Then:

1. Click **Apply & Restart**.
2. A dialog asks **Restart Stardew Valley now with the 'Default' profile?** Click **Restart**.
3. The game closes, then starts again by itself after a few seconds, with a new SMAPI window. (On macOS, no Terminal window comes back, and with DewDrop's Steam launcher there's never one. That's fine: if the **Mods** button is there, your mods loaded.)
4. Click **Mods** again. The bottom line now says **The mods loaded right now match this profile.**, and **Apply & Restart** is grayed out.

<figure><img src="../.gitbook/assets/shot-apply-restart.png" alt="DewDrop's confirm dialog asking to restart Stardew Valley with the Default profile, with Restart and Cancel buttons."><figcaption><p>The confirm dialog names the profile that will load.</p></figcaption></figure>

{% hint style="warning" %}
If the dialog also has a line starting **This turns off**, read the mod names before you click **Restart**. Turning off a mod your save uses can cause problems in that save. To keep one, click **Cancel**, turn it on in the **Installed** tab, then try again.
{% endhint %}

If the game closes and doesn't come back, start it the normal way and see [Troubleshooting](../help/troubleshooting.md#the-game-closed-and-never-came-back).

## Next time you play

Starting the game the normal way loads your whole `Mods` folder again, including mods you turned off. DewDrop checks this on the title screen. If the mods that loaded differ from your profile, a **Load your profile?** box opens and names them:

* **Restart now** restarts the game into your profile, the same way **Apply & Restart** does.
* **Not now** keeps the mods that loaded. DewDrop asks again on your next normal launch.
* **Don't ask again** stops the box. To get it back, tick **Ask to load my profile** on the **Account** tab > **Settings** panel.

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>The box that opens when the mods that loaded differ from your profile.</p></figcaption></figure>

On Windows with the Steam version, and on Linux and the Steam Deck when Steam starts the game with DewDrop's launcher, the box also has a **Start in my profile from Steam from now on** checkbox, ticked at first. Leave it ticked when you click **Restart now**, and Steam's **Play** button will start the game in your profile from then on, with no box and no restart. Untick it if you'd rather be asked each time.

**Never turn mods off?** Then a normal launch already loads the same mods, and the box doesn't open.

## When you quit the game

This part is for Windows and the Steam version of the game. For Steam to start the game in your profile, DewDrop changes the game's launch option in Steam. (A launch option is extra text Steam adds when it starts the game.) Steam has to be closed for that, so DewDrop does it when you quit the game:

1. You quit the game.
2. A few seconds after the game closes, Steam closes, then opens again, minimized to the system tray.
3. The next time you open the **Mods** menu, a message says "Steam now starts the game in your profile."

This happens once, the first time you quit after **Start in my profile from Steam** turns on. It turns on when you leave the box's checkbox ticked, or if you said yes to the installer's question **Start the game in your DewDrop profile when you press Play in Steam?** You can turn it on or off any time on the **Account** tab > **Settings** panel, and each change happens the same way when you quit.

On Linux and the Steam Deck, Steam never closes for this. DewDrop's Steam launcher reads the setting each time the game starts, so turning it on or off takes effect at once.

On Windows, quitting the game also finishes any mod updates that say **Update ready, restart to finish**.

## From then on

* **Play in Steam, with Start in my profile from Steam on:** the game starts in your profile. If you change your mods in the menu, they load the next time you press **Play**, or click **Apply & Restart** to load them now.
* **Any other launch**, such as Steam without that setting, a desktop shortcut, GOG, the Xbox app, macOS, or Linux without DewDrop's Steam launcher: when the mods that loaded differ from your profile, the box opens. Click **Restart now**.

[How launching works](launching.md) explains why, and [Start in your profile](../guides/start-in-your-profile.md) covers both settings.

## No Mods button?

* **The game started without SMAPI.** No SMAPI window opened. See [Starting the game with SMAPI](installing/smapi.md#starting-the-game-with-smapi). DewDrop's Steam launcher never shows one, so there, check `steam-launch.log` instead (see [If something goes wrong](steam-deck.md#if-something-goes-wrong)).
* **DewDrop is still zipped, or one folder too deep.** Aim for `Mods/DewDrop/manifest.json`. If yours is one level deeper, move the inner `DewDrop` folder straight into `Mods` and delete the empty outer one.
* **There are two copies of DewDrop.** SMAPI loads neither, so keep one.
* **You started from Stardrop** before DewDrop was in your profile. Start once the normal way instead.

[Troubleshooting](../help/troubleshooting.md#no-mods-button-on-the-title-screen) has more checks.

## Next steps

* [How launching works](launching.md): read this next.
* [Managing your mods](../guides/installed.md): turn mods on and off.
* [Connecting your Nexus account](../nexus-mods/connecting.md): browse and install mods from inside the game.
