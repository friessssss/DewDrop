---
description: "Why your profile only applies to sessions that start in it, and the ways to start in it each time you play."
icon: play
---

# How launching works

This is the one idea that makes DewDrop make sense: **your profile only applies to sessions that start in it.** Once that clicks, the rest falls into place: the **Load your profile?** box on the title screen, the orange status line, the lit **Apply & Restart** button, the **Start in my profile from Steam** setting, and mods that seem to come back on by themselves.

## Two ways the game can start

SMAPI, the tool that loads your mods, picks the folder to load them from as the game starts. That happens before any mod runs, DewDrop included, so no mod can change it once the game is running. Which mods load depends on how the game was started.

| | How you start it | Which mods load |
| --- | --- | --- |
| **A normal launch** | A desktop shortcut, the Xbox app, Steam's **Play** button (unless **Start in my profile from Steam** is on), or however else you start the game with SMAPI | Everything in your `Mods` folder, whether it's on or off in DewDrop |
| **A profile launch** | DewDrop restarts the game, Steam's **Play** button with **Start in my profile from Steam** on, or Stardrop starts the game | Only the mods that are on in your active profile |

<figure><img src="../.gitbook/assets/diagram-launching.png" alt="Diagram comparing two launches. Starting from Steam or a shortcut loads the whole Mods folder. Apply &#x26; Restart loads only the mods in your profile."><figcaption><p>A normal launch loads your whole Mods folder. Apply &#x26; Restart loads your profile.</p></figcaption></figure>

A profile launch uses a folder called `Selected Mods`. DewDrop fills it with a link to each mod that's on in your profile. These are links, not copies: your mods stay where they are. SMAPI is then told to load mods from `Selected Mods` instead of `Mods`.

A session starts in your profile in one of three ways:

* **DewDrop restarts the game.** You click **Apply & Restart** in the menu, or **Restart now** in the **Load your profile?** box. DewDrop fills `Selected Mods`, closes the game, and starts SMAPI again on `Selected Mods`. See [Apply & Restart](../guides/apply-and-restart.md).
* **Steam's Play button, with Start in my profile from Steam on.** This is for the Steam version of the game on Windows, or on Linux and the Steam Deck with DewDrop's Steam launcher. Steam starts SMAPI on `Selected Mods` from the start, so there's no restart. See [Two settings that help](#two-settings-that-help) below.
* **Stardrop starts the game.** Stardrop fills the same `Selected Mods` folder with your profile's mods and starts SMAPI on it. See [Using DewDrop with Stardrop](../stardrop.md).

DewDrop never moves anything out of your `Mods` folder, so a normal launch still sees every mod. That's why a mod you turned off in DewDrop loads again the next time you start the game from a shortcut. It stays off in every session that starts in your profile. For the details, see [How DewDrop works](../reference/how-it-works.md).

## Two settings that help

A normal launch doesn't load your profile. Two settings on the **Account** tab > **Settings** panel get you into it anyway.

### Start in my profile from Steam

For the Steam version of the game. Tick it, and Steam's **Play** button starts the game straight in your profile, with no restart. On Windows, it works by adding SMAPI's `--mods-path` option to the game's launch options in Steam. (A launch option is extra text Steam adds when it starts the game. It's also how **Play** starts SMAPI in the first place.)

Steam has to be closed to change its launch options, so on Windows the change happens **when you quit the game**. A few seconds after the game closes, Steam closes, the launch option changes, and Steam opens again, minimized to the system tray. This happens once each time you turn the setting on or off. The next time you open the **Mods** menu, a message says "Steam now starts the game in your profile."

With it on, changes you make in the menu, such as mods turned on or off, another profile or new mods, are linked as you quit. The next **Play** loads them. **Apply & Restart** still loads them right away.

Only Steam's **Play** button starts in your profile. A desktop shortcut is still a normal launch. On Linux and the Steam Deck, it works once the installer has set up DewDrop's Steam launcher (by itself on a Steam Deck, and with `--steam-setup` on other Linux systems), and changes at once, without closing Steam (see [DewDrop on the Steam Deck](steam-deck.md)). On GOG, the Xbox app, macOS, Proton and Wine, the row is grayed out; hover over it to see why. The installer asks about this setting too, when it sets up Steam.

### Ask to load my profile

This one is on at first. When a normal launch loads different mods than your profile, a **Load your profile?** box opens on the title screen and names the mods that differ:

* **Restart now** (or **Enter**) restarts the game into your profile, the same way **Apply & Restart** does.
* **Not now** (or **Esc**, or **B** on a controller) keeps the mods that loaded. DewDrop asks again on your next normal launch.
* **Don't ask again** turns **Ask to load my profile** off.

On Windows with the Steam version, or on Linux and the Steam Deck with DewDrop's Steam launcher, while **Start in my profile from Steam** is off, the box also has a **Start in my profile from Steam from now on** checkbox, ticked at first. Leave it ticked when you click **Restart now**, and the setting turns on. On Windows, Steam closes and opens again once after you quit the restarted game; with the launcher, it's on at once. From then on **Play** starts in your profile.

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>The box DewDrop shows when the mods that loaded differ from your profile.</p></figcaption></figure>

The box doesn't open when the game runs through Proton or Wine. **Restart now** uses the same restart as **Apply & Restart**, and like it, hasn't been tested on macOS and Linux yet.

[Start in your profile](../guides/start-in-your-profile.md) covers both settings in full.

## Which kind of session am I in?

Open the **Mods** menu and read the line under **Profile:** at the bottom. After a normal launch, it says what differs from your profile. The numbers below are examples.

| Status line | What it means |
| --- | --- |
| **3 mods that are off in this profile loaded anyway. Restart to load this profile.** (orange) | A normal launch. Mods you turned off in DewDrop loaded, because they're in your `Mods` folder. |
| **2 mods in this profile didn't load. Restart to load this profile.** (orange) | A normal launch, and mods that are on in your profile didn't load. For example, you installed them after the game started. |
| **1 mod loaded from a different copy than this profile uses. Restart to load this profile.** (orange) | A normal launch. You have two copies of a mod, such as a loose one and one from a Stardrop collection, and the copy that loaded isn't the one your profile uses. |
| **Changes are ready. Restart the game to load them.** (orange) | A profile launch, but something changed since. For example, you turned a mod on or off, installed or updated a mod, or switched profiles. After a normal launch, it means a mod update needs a restart to go in. |
| **Changes load the next time you start the game from Steam, or restart now.** (orange) | The same, with **Start in my profile from Steam** on. Quit and press **Play** in Steam later, or click **Apply & Restart** to load them now. |
| **The mods loaded right now match this profile.** (gray) | Nothing to change, after a profile launch or a normal launch. **Apply & Restart** is grayed out. |
| **This session didn't start from a DewDrop profile. Restart to load this profile.** (orange) | SMAPI was started on some other folder with its `--mods-path` launch option, so DewDrop can't compare it with your profile. **Apply & Restart** is lit so you can switch into your profile. |

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
**Never turn mods off?** If every mod in your `Mods` folder is on in your profile, a normal launch already loads the same mods. DewDrop sees that, so no box opens and the status line says **The mods loaded right now match this profile.** When a mod says **Update ready, restart to finish**, click **Apply & Restart** to put the update in. On Windows, quitting the game puts it in too. See [Updating mods](../guides/updating-mods.md).
{% endhint %}

## A few more things

* **DewDrop's restart starts the game itself, not through Steam.** That goes for **Apply & Restart** and **Restart now**. Whether Steam's overlay and playtime tracking work in that session hasn't been tested. With **Start in my profile from Steam**, **Play** starts the game through Steam as usual. With DewDrop's Steam launcher (Steam Deck, or Linux after `--steam-setup`), the restart goes through that same launcher, so Steam still counts it as the same game.
* **The restart is tested on Windows.** On macOS and Linux it's built to work too, but it hasn't been tested yet. On macOS, the restarted game has no Terminal window; if the **Mods** button is there, your mods loaded. DewDrop's Steam launcher never opens a SMAPI window either, and `steam-launch.log` records each restart (see [DewDrop on the Steam Deck](steam-deck.md#if-something-goes-wrong)). If the game closes and doesn't come back, start it the normal way and see [Troubleshooting](../help/troubleshooting.md#the-game-closed-and-never-came-back). The next time the box opens, it says the last restart didn't come back.
* **Switching profiles doesn't restart the game.** **Use this profile** on the **Profiles** tab picks the profile, and **Apply & Restart** loads it. With **Start in my profile from Steam** on, the next **Play** loads it too. See [Profiles](../guides/profiles.md).
* **Stardrop and Steam share `Selected Mods`.** With **Start in my profile from Steam** on, **Play** starts whatever Stardrop or DewDrop linked last. On Windows, if you start a Stardrop collection profile from Stardrop, the next **Play** starts that profile without the **Mods** button. (DewDrop's Steam launcher starts your whole `Mods` folder instead.) [Start in your profile](../guides/start-in-your-profile.md) explains how to get it back.
* **Removing DewDrop?** On Windows, turn **Start in my profile from Steam** off and quit the game first. Otherwise Steam keeps starting your last profile, with no **Mods** button to change it. With DewDrop's Steam launcher, take the launcher out of Steam's launch options instead. See [Uninstalling DewDrop](uninstalling.md).
