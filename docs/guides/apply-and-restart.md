---
description: "Restart the game with exactly the mods your profile turns on, and what to do if it doesn't work."
icon: rotate-right
---

# Apply & Restart

**Apply & Restart** is the large button in the bottom-right corner of the DewDrop menu. It closes Stardew Valley and starts it again with only the mods your active profile turns on. On the way, it finishes any mod updates and uninstalls that are waiting for a restart.

You need it because mods load once, when the game starts. Ticking a mod on the **Installed** tab or switching profiles changes your profile straight away, but the mods already loaded stay as they are until the game restarts.

## When to use it

Use it when the button is lit. The line under **Profile:** at the bottom of the menu turns orange whenever a restart would change something, for example when:

* you turned mods on or off, switched profiles, or installed or updated a mod;
* you started the game from a shortcut, or from Steam without **Start in my profile from Steam**, and mods your profile turns off loaded anyway;
* a mod you [uninstalled](installed.md#uninstall-a-mod) is still loaded.

When the line is gray and says "The mods loaded right now match this profile.", there's nothing to restart for and the button is grayed out. [The bottom bar](the-menu.md#the-bottom-bar) lists every status line and what it means.

You don't always have to click the button yourself:

* **Start in my profile from Steam** makes Steam's **Play** button start the game in your profile every time. It's for the Steam version on Windows, and on Linux and the Steam Deck with DewDrop's Steam launcher.
* When a normal launch loads different mods than your profile, the **Load your profile?** box on the title screen offers this same restart.

See [Start in your profile](start-in-your-profile.md) for both.

<figure><img src="../.gitbook/assets/diagram-launching.png" alt="Diagram: starting from Steam or a shortcut loads the whole Mods folder, while Apply &#x26; Restart loads only the mods in your profile"><figcaption><p>A normal launch loads your whole Mods folder. Apply &#x26; Restart loads your profile.</p></figcaption></figure>

## What happens

1. Click **Apply & Restart**.
2. A dialog asks "Restart Stardew Valley now with the '*profile*' profile?" It can add:
   * "This turns off *N* mods loaded right now: *names*." These are mods you're playing with now that your profile leaves out. They stay installed but won't load. Turning off a mod your save uses can cause problems in that save, so check the names before you go on.
   * "This also deletes the mod you uninstalled: *name*." (or "the *N* mods you uninstalled"). To keep one, click **Cancel**, then click **Undo** on its row on the **Installed** tab.
   * "*N* downloads are still running and will be cancelled."
3. Click **Restart**, or press **Enter**. To back out, click **Cancel**, click outside the box, or press **Esc** (**B** on a controller).
4. DewDrop links your profile's mods, and the game closes. As it closes, DewDrop moves the mods you uninstalled out of your `Mods` folder.
5. A moment later, the game starts again through SMAPI, after DewDrop finishes any [mod updates](updating-mods.md) that were waiting.
6. The restarted game deletes the mods you uninstalled, skips the title intro, and goes straight to the title screen's buttons. Open **Mods**, and the bottom bar says "The mods loaded right now match this profile."

<figure><img src="../.gitbook/assets/shot-apply-restart.png" alt="The DewDrop restart dialog asking to restart Stardew Valley now with the chosen profile, with Restart and Cancel buttons"><figcaption><p>The restart dialog.</p></figcaption></figure>

The menu only opens on the title screen, so no farm is open when the game restarts and you don't lose any progress.

Apply & Restart doesn't move or delete your mod folders, apart from the mods you uninstalled. DewDrop puts a link to each of your profile's mods in a `Selected Mods` folder, shared with Stardrop, and starts SMAPI on that folder. DewDrop itself is always included, so the **Mods** button comes back. See [How DewDrop works](../reference/how-it-works.md).

### On each platform

{% tabs %}
{% tab title="Windows" %}
Tested. A hidden helper waits for the game to close, finishes waiting updates and any uninstall a still-open file held up, then starts SMAPI (`StardewModdingAPI.exe` in your game folder) directly, not through Steam or the Xbox app. A new SMAPI console window opens with the game.

The same helper also runs when you quit the game without restarting, if there's something to finish: waiting updates, held-up uninstalls, or a change to Steam's launch option for **Start in my profile from Steam**. So on Windows, waiting updates also go in when you quit.
{% endtab %}

{% tab title="macOS" %}
Not tested yet. DewDrop waits for the game to close, then starts `StardewModdingAPI` in your game folder directly, so you probably won't see a Terminal window for SMAPI.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
Not tested yet.

* **With DewDrop's Steam launcher** (on a Steam Deck, or on Linux after the installer's `--steam-setup`), DewDrop runs the launcher again once the game has closed, the way Steam does. It starts SMAPI directly, with no terminal window, and stays in the same Steam session, so on a Steam Deck it's built to stay in Game Mode. That's being tested now. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md).
* **Without it**, DewDrop runs the game's `StardewValley` launcher, which SMAPI's installer replaces so it starts SMAPI. If that launcher isn't SMAPI's, because a game update put the original back or you set SMAPI up another way, the restart starts the game without SMAPI, and so without mods. Run SMAPI's installer again to fix it.
{% endtab %}
{% endtabs %}

## If it doesn't restart

When DewDrop can't restart, the game keeps running as it is and a message says why. If the restart came from the **Load your profile?** box on the title screen, the box shows the same message under **Couldn't load your profile**.

| Message | What to do |
| --- | --- |
| "Didn't restart: SMAPI isn't in your game folder. Run the DewDrop installer (or SMAPI's) to put it back." | Without SMAPI the game couldn't start again, so DewDrop checks before it closes the game. Reinstall SMAPI; see [About SMAPI](../getting-started/installing/smapi.md). |
| "*name* is still installing. Restart when it's done." | A download is being installed. Wait until it shows **Done** on the [Downloads](downloads.md) tab, then click **Apply & Restart** again. |
| "Didn't restart: DewDrop couldn't link ..., so it wouldn't load. The SMAPI log says why." | DewDrop couldn't make a link to that mod's folder. Usually the folder was moved or deleted while the game was running, or your mods are on a network drive, which links can't point to. Close the game, start it again so DewDrop sees your mods as they are now, and try again. |
| "Didn't restart: DewDrop couldn't set up the restart (...). The SMAPI log says why. If you use Stardrop, starting the game from Stardrop loads this profile." | DewDrop couldn't start its restart helper. If you use Stardrop, start the game from Stardrop. If you don't, close the game, start it the usual way, and try **Apply & Restart** again. |

If the button is grayed out and the bottom bar shows a red message that `Settings.json` or a profile file "couldn't be read", hover over the button for the reason. Close the game and start it again, or for a profile file, pick another profile. If it keeps happening, see [Troubleshooting](../help/troubleshooting.md#a-red-message-at-the-bottom-of-the-menu).

### The game closed but didn't come back

The restart happens after the game has closed, so DewDrop can't show you an error. Start the game the usual way. DewDrop remembers that the restart didn't come back: if the **Load your profile?** box comes up, it says "Last time DewDrop restarted the game, it didn't come back in your profile." Click **Restart now** to try again, or **Not now** to keep playing. Otherwise, open **Mods** and try **Apply & Restart** again. Your profile is still selected.

Then check the restart's log, and include it with your SMAPI log if you [report the problem](../help/reporting-issues.md):

{% tabs %}
{% tab title="Windows" %}
The restart helper writes `relaunch.log`. To open its folder, press **Win+R**, paste this and press **Enter**:

```text
%AppData%\Stardrop\Data\Cache\DewDrop
```

The log is emptied each time the helper runs, after a restart or after you quit the game. It only gets lines when updates or uninstalls were finished, Steam's launch option changed, or something went wrong:

* "Could not start SMAPI: ..." means SMAPI is missing from your game folder or couldn't start. Reinstall SMAPI; see [About SMAPI](../getting-started/installing/smapi.md).
* "Could not update ... yet: ..." means an update couldn't be swapped in. It keeps waiting, and DewDrop tries again next time.
* "Could not uninstall ... yet: ..." means a file in a mod you uninstalled was still open. DewDrop tries again at the next start. See [When the folder is deleted](installed.md#when-the-folder-is-deleted).
* "Could not change the Steam launch option: ..." means **Start in my profile from Steam** couldn't be saved in Steam. The next time you open **Mods**, a message gives the same reason.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
With DewDrop's Steam launcher, `steam-launch.log` records each start, restarts included: which folder SMAPI loaded, and why. It's in this folder:

```text
~/.config/Stardrop/Data/Cache/DewDrop
```

See [DewDrop on the Steam Deck](../getting-started/steam-deck.md#if-something-goes-wrong). Without the launcher, there's no restart log.
{% endtab %}

{% tab title="macOS" %}
There's no restart log. Your SMAPI log is the place to look; see [Reporting issues](../help/reporting-issues.md).
{% endtab %}
{% endtabs %}

[Troubleshooting](../help/troubleshooting.md#the-game-closed-and-never-came-back) has more on what each log means.

<details>

<summary>A mod in my profile still doesn't load after Apply &#x26; Restart. Why?</summary>

Apply & Restart only decides which mods SMAPI tries to load. If SMAPI skips a mod, for example because it needs a newer SMAPI or a mod you don't have, the SMAPI log says why, and so does the SMAPI console window, if there is one. DewDrop doesn't keep asking you to restart for a mod that failed to load, unless you update or reinstall it.

</details>
