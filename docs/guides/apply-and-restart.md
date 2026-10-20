---
description: "Restart the game with exactly the mods your profile turns on, and what to do if it doesn't work."
icon: rotate-right
---

# Apply & Restart

**Apply & Restart** is the large button in the bottom-right corner of the DewDrop menu. It closes Stardew Valley and starts it again with only the mods your active profile turns on. On the way, it finishes any mod updates that are waiting for a restart.

You need it because mods load once, when the game starts. Ticking a mod on the **Installed** tab or switching profiles changes your profile straight away, but the mods already loaded stay as they are until the game restarts. **Apply & Restart** is the way to load your changes right away.

## When to use it

The bottom bar tells you. The line under **Profile:** says one of these:

| Status | What it means |
| --- | --- |
| "3 mods that are off in this profile loaded anyway. Restart to load this profile." (orange) | A normal launch: you started the game from a shortcut, from SMAPI, or from Steam without **Start in my profile from Steam**, so everything in your `Mods` folder loaded, including mods your profile turns off. Click **Apply & Restart** to switch to your profile. |
| "2 mods in this profile didn't load. Restart to load this profile." (orange) | Also a normal launch. Some mods that are on in your profile aren't loaded, for example ones you added after the game started. Click **Apply & Restart** to load them. |
| "1 mod loaded from a different copy than this profile uses. Restart to load this profile." (orange) | Also a normal launch. You have two copies of a mod, such as your own and one from a Stardrop collection, and the one that loaded isn't the one your profile uses. |
| "This session didn't start from a DewDrop profile. Restart to load this profile." (orange) | SMAPI was started with its `--mods-path` launch option on a folder DewDrop doesn't manage, so DewDrop can't tell what's different. Click **Apply & Restart** to switch to your profile. |
| "Changes are ready. Restart the game to load them." (orange) | You turned mods on or off, switched profiles, or installed or updated a mod. |
| "Changes load the next time you start the game from Steam, or restart now." (orange) | The same, with **Start in my profile from Steam** on. Your changes are linked when you quit the game, so they load the next time you press **Play** in Steam. Click **Apply & Restart** to load them now instead. |
| "The mods loaded right now match this profile." (gray) | Nothing to do. The button is grayed out. |

The numbers are examples. If more than one of the first three lines is true, the bar shows the one highest in the table.

[How launching works](../getting-started/launching.md) explains why a normal launch needs this restart. You don't always have to click the button yourself:

* On Windows with the Steam version, **Start in my profile from Steam** on the **Account** tab makes Steam's **Play** button start the game in your profile every time.
* When a normal launch loads different mods than your profile, the **Load your profile?** box on the title screen offers this same restart.

See [Start in your profile](start-in-your-profile.md) for both.

<figure><img src="../.gitbook/assets/diagram-launching.png" alt="Diagram: starting from Steam or a shortcut loads the whole Mods folder, while Apply &#x26; Restart loads only the mods in your profile"><figcaption><p>A normal launch loads your whole Mods folder. Apply &#x26; Restart loads your profile.</p></figcaption></figure>

## What happens

1. Click **Apply & Restart**.
2. A dialog asks "Restart Stardew Valley now with the '*profile*' profile?" It can add:
   * "This turns off ... mods loaded right now", with their names. These are mods you're playing with now that your profile leaves out. They stay installed but won't load. Turning off a mod your save uses can cause problems in that save, so check the names before you go on.
   * "... downloads are still running and will be cancelled."
3. Click **Restart**, or press **Enter**. To back out, click **Cancel**, click outside the box, or press **Esc** (**B** on a controller).
4. DewDrop prepares your profile's mods, and the game closes.
5. A moment later, the game starts again through SMAPI. Before it does, DewDrop finishes any [mod updates](updating-mods.md) that were waiting for a restart.
6. The restarted game skips the title intro and goes straight to the title screen's buttons. Open **Mods**, and the bottom bar says "The mods loaded right now match this profile."

<figure><img src="../.gitbook/assets/shot-apply-restart.png" alt="The DewDrop restart dialog asking to restart Stardew Valley now with the chosen profile, with Restart and Cancel buttons"><figcaption><p>The restart dialog.</p></figcaption></figure>

The menu only opens on the title screen, so no farm is open when the game restarts and you don't lose any progress.

Your mod folders are never moved or deleted. DewDrop puts a link to each of your profile's mods in a `Selected Mods` folder, shared with Stardrop, and starts SMAPI from there. DewDrop itself is always included, so the **Mods** button comes back. See [How DewDrop works](../reference/how-it-works.md).

### On each platform

{% tabs %}
{% tab title="Windows" %}
A hidden helper waits for the game to close, finishes any waiting updates, then starts SMAPI (`StardewModdingAPI.exe` in your game folder) directly, not through Steam or the Xbox app. A new SMAPI console window (the black window with SMAPI's messages) opens with the game. Windows is the tested platform.

The same helper also runs when you quit the game without restarting, if there's something to finish: it installs waiting updates, and changes Steam's launch option when you've switched **Start in my profile from Steam** on or off. So on Windows, waiting updates also go in when you quit the game.
{% endtab %}

{% tab title="macOS" %}
Not tested yet. DewDrop waits for the game to close, then starts `StardewModdingAPI` in your game folder directly, so you probably won't see a Terminal window for SMAPI.
{% endtab %}

{% tab title="Linux" %}
Not tested yet. DewDrop waits for the game to close, then runs the game's `StardewValley` launcher. SMAPI's installer replaces that launcher so it starts SMAPI. If you set SMAPI up another way, the restart may start the game without SMAPI, and so without mods.

On the Steam Deck, the restart stays in Game Mode: DewDrop starts the game again through its Steam launcher. This is new and being tested. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md).
{% endtab %}
{% endtabs %}

## If it doesn't restart

When DewDrop can't restart, the game keeps running as it is and a message says why. If the restart came from the **Load your profile?** box on the title screen, the box shows the same message under **Couldn't load your profile**.

| Message | What to do |
| --- | --- |
| "Didn't restart: SMAPI isn't in your game folder. Run the DewDrop installer (or SMAPI's) to put it back." | Without SMAPI the game couldn't start again, so DewDrop checks before it closes the game. Reinstall SMAPI; see [About SMAPI](../getting-started/installing/smapi.md). |
| "... is still installing. Restart when it's done." | A download is being installed. Wait until it shows **Done** on the [Downloads](downloads.md) tab, then click **Apply & Restart** again. |
| "Didn't restart: DewDrop couldn't link ..., so it wouldn't load. The SMAPI log says why." | DewDrop couldn't make a link to that mod's folder. Usually the folder was moved or deleted while the game was running, or your mods are on a network drive, which links can't point to. Close the game, start it again so DewDrop sees your mods as they are now, and try again. |
| "Didn't restart: DewDrop couldn't set up the restart (...). The SMAPI log says why. If you use Stardrop, starting the game from Stardrop loads this profile." | DewDrop couldn't start its restart helper. If you use Stardrop, start the game from Stardrop. If you don't, close the game, start it the usual way, and try **Apply & Restart** again. |

If the button is grayed out and the bottom bar shows a red message that `Settings.json` or a profile file "couldn't be read", hover over the button for the reason. Close the game and start it again, or for a profile file, pick another profile. If it keeps happening, see [Troubleshooting](../help/troubleshooting.md).

### The game closed but didn't come back

The restart happens after the game has closed, so DewDrop can't show you an error. Start the game the usual way. DewDrop remembers that the restart didn't come back. If the **Load your profile?** box comes up, it says "Last time DewDrop restarted the game, it didn't come back in your profile." Click **Restart now** to try again, or **Not now** to keep playing. On Windows with the Steam version, the box's **Start in my profile from Steam from now on** checkbox starts unticked this time. Otherwise, open **Mods** and try **Apply & Restart** again. Your profile is still selected. See [Start in your profile](start-in-your-profile.md).

On Windows, the restart helper writes a log at `C:\Users\<you>\AppData\Roaming\Stardrop\Data\Cache\DewDrop\relaunch.log`. To open the folder, press **Win+R**, paste `%AppData%\Stardrop\Data\Cache\DewDrop`, and press **Enter**. The log is emptied each time the helper runs, after a restart or after you quit the game, and only gets lines when updates were finished, Steam's launch option changed, or something went wrong:

* "Could not start SMAPI: ..." means SMAPI is missing from your game folder or couldn't start. Reinstall SMAPI; see [About SMAPI](../getting-started/installing/smapi.md).
* "Could not update ... yet: ..." means an update couldn't be swapped in. It keeps waiting, and DewDrop tries again next time.
* "Could not change the Steam launch option: ..." means **Start in my profile from Steam** couldn't be saved in Steam. The next time you open **Mods**, a message gives the same reason.

On macOS and Linux there's no restart log. When you report a problem, include your SMAPI log, plus `relaunch.log` on Windows; see [Reporting issues](../help/reporting-issues.md).

<details>

<summary>A mod in my profile still doesn't load after Apply &#x26; Restart. Why?</summary>

Apply & Restart only decides which mods SMAPI tries to load. If SMAPI skips a mod, for example because it needs a newer SMAPI or a mod you don't have, the SMAPI console window says why. DewDrop doesn't keep asking you to restart for a mod that failed to load, unless you update or reinstall it.

</details>
