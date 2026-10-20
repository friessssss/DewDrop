---
description: "Why your profile only applies to sessions that start in it, and the ways to start in it each time you play."
icon: play
---

# How launching works

This is the one idea that makes DewDrop make sense: **your profile only applies to sessions that start in it.** It explains the **Load your profile?** box on the title screen, the orange line at the bottom of the menu, the lit **Apply & Restart** button, and why a mod you turned off can load again.

## Two ways the game can start

SMAPI, the tool that loads your mods, picks the folder it loads them from as the game starts. That happens before any mod runs, DewDrop included, so no mod can change it once the game is running.

| | How you start it | Which mods load |
| --- | --- | --- |
| **A normal launch** | A desktop shortcut, the Xbox app, Steam's **Play** button while **Start in my profile from Steam** is off, or any other way you start the game with SMAPI | Everything in your `Mods` folder, whether it's on or off in DewDrop |
| **A profile launch** | [**Apply & Restart**](../guides/apply-and-restart.md) or **Restart now**, Steam's **Play** button with [**Start in my profile from Steam**](#start-in-my-profile-from-steam) on, or [Stardrop](../stardrop.md) | Only the mods that are on in your active profile |

<figure><img src="../.gitbook/assets/diagram-launching.png" alt="Diagram comparing two launches. Starting from Steam or a shortcut loads the whole Mods folder. Apply &#x26; Restart loads only the mods in your profile."><figcaption><p>A normal launch loads your whole Mods folder. Apply &#x26; Restart loads your profile.</p></figcaption></figure>

A profile launch uses a folder called `Selected Mods`. DewDrop fills it with a link to each mod that's on in your profile, and SMAPI loads mods from there instead of `Mods`. These are links, not copies: your mods stay where they are.

Turning a mod off doesn't move it out of your `Mods` folder, so a normal launch still loads it. It stays off in every session that starts in your profile. [How DewDrop works](../reference/how-it-works.md) has the details.

## Two settings that help

A normal launch doesn't load your profile. Two settings on the **Account** tab > **Settings** panel get you into it anyway. [Start in your profile](../guides/start-in-your-profile.md) covers both in full.

### Start in my profile from Steam

Tick it, and Steam's **Play** button starts the game straight in your profile, with no restart. It's for the Steam version of the game.

{% tabs %}
{% tab title="Windows" %}
DewDrop adds SMAPI's `--mods-path` option to the game's launch options in Steam. (A launch option is extra text Steam adds when it starts the game. It's also how **Play** starts SMAPI in the first place.)

Steam has to be closed to change its launch options, so the change happens **when you quit the game**. A few seconds after the game closes, Steam closes, the launch option changes, and Steam opens again, minimized to the system tray. This happens once each time you turn the setting on or off. The next time you open the **Mods** menu, a message says "Steam now starts the game in your profile."
{% endtab %}

{% tab title="Linux and Steam Deck" %}
It works once Steam starts the game through DewDrop's Steam launcher. The installer sets that up: by itself on a Steam Deck, and with `--steam-setup` on other Linux systems (see [Steam on Linux and the Steam Deck](../reference/installer-options.md#steam-on-linux-and-the-steam-deck)). The setting then changes as soon as you tick it, and Steam never closes. See [DewDrop on the Steam Deck](steam-deck.md).
{% endtab %}
{% endtabs %}

With it on, changes you make in the menu (mods turned on or off, another profile, new mods) are linked as you quit, so the next **Play** loads them. **Apply & Restart** still loads them right away.

Only Steam's **Play** button starts in your profile. A desktop shortcut is still a normal launch. On macOS, GOG, the Xbox app, Proton and Wine, the row is grayed out; hover over it to see why. The installer asks about this setting too, when it sets up Steam.

### Ask to load my profile

This one is on at first. When a normal launch loads different mods than your profile, a **Load your profile?** box opens on the title screen and names the mods that differ:

* **Restart now** (or **Enter**) restarts the game into your profile, the same way **Apply & Restart** does.
* **Not now** (or **Esc**, or **B** on a controller) keeps the mods that loaded. DewDrop asks again on your next normal launch.
* **Don't ask again** turns **Ask to load my profile** off.

While **Start in my profile from Steam** is available but off, the box also has a **Start in my profile from Steam from now on** checkbox, ticked at first. Leave it ticked when you click **Restart now**, and the setting turns on. On Windows, Steam closes and opens again once after you quit the restarted game. With DewDrop's Steam launcher, it's on at once.

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>The box DewDrop shows when the mods that loaded differ from your profile.</p></figcaption></figure>

The box doesn't open when the game runs through Proton or Wine. [The "Load your profile?" box](../guides/start-in-your-profile.md#the-load-your-profile-box) covers the other times it stays away.

## Which kind of session am I in?

Open the **Mods** menu and read the line under **Profile:** at the bottom. Orange means a restart would change something, and **Apply & Restart** is lit. Gray means there's nothing to do.

| The line | What it tells you |
| --- | --- |
| Ends with "Restart to load this profile." (orange) | A normal launch, and the mods that loaded aren't your profile's. It says how many mods that are off in your profile loaded anyway, how many that are on didn't load, or how many loaded from a different copy than your profile uses. "This session didn't start from a DewDrop profile." means SMAPI was started on some other folder with its `--mods-path` launch option. |
| Ends with "Restart to finish uninstalling." (orange) | A mod you [uninstalled](../guides/installed.md#uninstall-a-mod) is still loaded. |
| "Changes are ready. Restart the game to load them." (orange) | Something changed since the game started: you turned a mod on or off, installed or updated a mod, or switched profiles. |
| "Changes load the next time you start the game from Steam, or restart now." (orange) | The same, with **Start in my profile from Steam** on. Quit and press **Play** in Steam later, or click **Apply & Restart** to load them now. |
| Starts with "The mods loaded right now match this profile." (gray) | Your profile is loaded, or a normal launch happened to load the same mods. Nothing to restart for. |

[The bottom bar](../guides/the-menu.md#the-bottom-bar) lists every line word for word, and which one shows when more than one applies.

A red line instead means DewDrop couldn't read Stardrop's `Settings.json` or your profile file. See [Troubleshooting](../help/troubleshooting.md#a-red-message-at-the-bottom-of-the-menu).

After a normal launch, the **Installed** tab shows the difference too. Mods that loaded but are off in your profile say **Off after restart**, and mods that are on in your profile but didn't load say **On after restart**.

## Each time you play

### With Start in my profile from Steam

1. Press **Play** in Steam. The game starts in your profile, and no box opens.
2. Play. If you change your mods in the menu, click **Apply & Restart** to load them now, or quit and they load the next time you press **Play**.

Started the game from a desktop shortcut instead? That's a normal launch, so the box may open. Click **Restart now**.

### Without it

1. Start the game as usual.
2. If the **Load your profile?** box opens on the title screen, click **Restart now**. The game closes and comes back with your profile's mods.
3. Play. If you change your mods in the menu, click **Apply & Restart** to load them.

It costs one extra restart when your mods differ from your profile, and there's nothing to set up.

### Either way

Copied mods into your `Mods` folder by hand? DewDrop turns them on in your active profile when the game starts, or when you click **Rescan** on the **Installed** tab, as long as **Turn on mods when installed** is on. If they didn't load this session, their rows say **On after restart**. With that setting off, they stay off until you turn them on.

{% hint style="info" %}
**Never turn mods off?** If every mod in your `Mods` folder is on in your profile, a normal launch already loads the same mods. DewDrop sees that, so no box opens and the status line says "The mods loaded right now match this profile." When a mod says **Update ready, restart to finish**, click **Apply & Restart** to put the update in. On Windows, quitting the game puts it in too. See [Updating mods](../guides/updating-mods.md).
{% endhint %}

## A few more things

* **The restart is tested on Windows.** On macOS, Linux and the Steam Deck it's built to work, but hasn't been confirmed yet. If the game closes and doesn't come back, start it the normal way and see [Troubleshooting](../help/troubleshooting.md#the-game-closed-and-never-came-back). The next time the box opens, it says the last restart didn't come back.
* **The restart and Steam.** Without DewDrop's Steam launcher, the restart starts SMAPI itself, not through Steam, and whether Steam's overlay and playtime tracking work in that session hasn't been tested. With the launcher (Steam Deck, or Linux after the installer's `--steam-setup`), the restart runs the launcher again, so it's built to stay in the same Steam session.
* **No SMAPI window?** That's normal after a restart on macOS, and always with DewDrop's Steam launcher. If the **Mods** button is there, your mods loaded.
* **Switching profiles doesn't restart the game.** **Use this profile** on the **Profiles** tab picks the profile. **Apply & Restart**, or the next **Play** with **Start in my profile from Steam** on, loads it. See [Profiles](../guides/profiles.md).
* **Stardrop and Steam share `Selected Mods`.** With **Start in my profile from Steam** on, **Play** starts whatever Stardrop or DewDrop linked last. [Start in your profile](../guides/start-in-your-profile.md) explains what that means for Stardrop collection profiles.
* **Removing DewDrop?** On Windows, turn **Start in my profile from Steam** off and quit the game first, or Steam keeps starting your last profile with no **Mods** button. With DewDrop's Steam launcher, take the launcher out of Steam's launch options. See [Uninstalling DewDrop](uninstalling.md).
