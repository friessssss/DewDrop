---
description: "Short answers to the questions new DewDrop players ask most."
icon: circle-question
---

# FAQ

Click a question to see the answer. For step-by-step fixes, see [Troubleshooting](troubleshooting.md). To ask for help, see [Reporting issues](reporting-issues.md).

## The basics

<details>
<summary>What does DewDrop do?</summary>

DewDrop is a mod manager that lives inside Stardew Valley. It adds a **Mods** button to the title screen. From there you can turn mods on and off, keep different mod lists as profiles, browse and install mods from Nexus Mods, update your mods, and uninstall the ones you don't want. It's a mod itself, so it needs [SMAPI](../getting-started/installing/smapi.md), the free tool that loads Stardew Valley mods.

</details>

<details>
<summary>What's the black window next to my game?</summary>

That's SMAPI's console. It opens when the game starts with mods and lists what's loading. Yellow lines and messages about mod updates are normal. On macOS and Linux it's a Terminal window. Leave it open while you play: closing it closes the game.

Sometimes there's no console, and that's fine as long as the **Mods** button is there:

* On a Steam Deck, and on Linux after the installer's `--steam-setup`, DewDrop's Steam launcher starts SMAPI without one.
* On macOS, it usually doesn't come back after **Apply &#x26; Restart** or **Restart now**.

No console and no **Mods** button means the game started without mods. See [No Mods button on the title screen](troubleshooting.md#no-mods-button-on-the-title-screen).

</details>

<details>
<summary>What are Console Commands and Save Backup? I didn't install them.</summary>

They're two small mods that come with SMAPI, and SMAPI's installer added them. **Console Commands** adds commands you can type in the SMAPI window. **Save Backup** backs up your saves once a day, when you start the game. Keep **Save Backup** on, including in new profiles, which start with only DewDrop on.

</details>

<details>
<summary>What's a profile?</summary>

A saved list of which mods are on. DewDrop made one called **Default** the first time it ran, with every mod that loaded then. You can make more and switch between them with **Apply &#x26; Restart**. See [Profiles](../guides/profiles.md).

</details>

<details>
<summary>Do I need Stardrop installed?</summary>

No. DewDrop works on its own. It keeps its data in a folder named `Stardrop` so it can share profiles and your Nexus login with Stardrop if you ever use both. That folder appears even if you've never installed Stardrop.

</details>

<details>
<summary>Is DewDrop safe to use?</summary>

DewDrop is open source (GPL-3.0), so anyone can read its code on [GitHub](https://github.com/friessssss/DewDrop). It's careful with your things:

* **Your mods.** Turning a mod off never moves or deletes it. DewDrop deletes a mod's folder only when you uninstall that mod.
* **The installer** gets SMAPI only from SMAPI's official GitHub page, and never deletes your mods, saves or settings.
* **Steam's settings.** The installer and **Start in my profile from Steam** change them only for the Steam account you're signed in to, and back them up first. On Windows, the installer asks before it changes them. On a Steam Deck (or on Linux with `--steam-setup`), setting up Steam is part of the install, and it asks before it changes launch options you already have, except one that only starts SMAPI.
* **Start in my profile from Steam** is off until you or the installer turn it on. It never writes over launch options you changed while the game was running. On Linux and the Steam Deck, it doesn't change Steam's settings at all: DewDrop's Steam launcher reads it each time.
* **Your Nexus Mods API key** is only sent to Nexus Mods.

If you turn on "Mod manager download" links on Windows, any website can try to send DewDrop a Nexus Mods download, and with Premium, DewDrop installs it without asking. Your browser usually asks first, so only allow prompts you expect. See ["Mod manager download" links](../nexus-mods/mod-manager-links.md).

</details>

<details>
<summary>Does DewDrop move or delete my mods?</summary>

Only when you ask it to. Your mods stay in your `Mods` folder. Turning a mod off only changes your profile, and **Apply &#x26; Restart** loads your profile through links, not copies. Deleting a profile never deletes mods.

DewDrop replaces a mod's folder when you update that mod, and keeps the mod's `config.json`. It deletes a mod's folder only when you [uninstall the mod](../guides/installed.md#uninstall-a-mod), and only once the game has closed. See [How DewDrop works](../reference/how-it-works.md).

</details>

<details>
<summary>Will DewDrop affect my saves?</summary>

DewDrop doesn't read or change your save files. But turning off or uninstalling a mod that a save uses can still affect that save, the same as removing the mod by hand. For example, items a mod added may disappear. Keep **Save Backup** on so you have backups.

</details>

## Using DewDrop

<details>
<summary>Is there a hotkey to open DewDrop while I'm playing?</summary>

No. DewDrop only opens from the **Mods** button on the title screen, so exit your save to the title screen first.

</details>

<details>
<summary>Why does the game restart when I apply changes?</summary>

SMAPI loads mods only when the game starts, and mods with code can't be safely unloaded while it runs. So DewDrop saves your choices, closes the game, and starts it again with the right mods. See [Apply &#x26; Restart](../guides/apply-and-restart.md).

</details>

<details>
<summary>Why does it say mods "loaded anyway" or "didn't load"?</summary>

The game started the normal way: from a shortcut, another launcher, or Steam without **Start in my profile from Steam**. That loads everything in your `Mods` folder, so the orange line under **Profile:** says how that differs from your profile:

* "... off in this profile loaded anyway": mods you turned off are loaded right now.
* "... in this profile didn't load": mods that are on in your profile aren't loaded, such as one you installed since the game started.
* "... loaded from a different copy than this profile uses": you have a mod twice, and the game loaded the other copy.

Click **Apply &#x26; Restart** to load exactly your profile. By default, DewDrop also offers this restart with a **Load your profile?** box on the title screen.

If every mod in your `Mods` folder is on in your profile, a normal launch already loads the same mods, and the line says "The mods loaded right now match this profile." See [How launching works](../getting-started/launching.md).

If it says "This session didn't start from a DewDrop profile. Restart to load this profile.", SMAPI was started with a `--mods-path` launch option that points at some other folder. Click **Apply &#x26; Restart** to switch into your profile.

</details>

<details>
<summary>Can DewDrop start the game in my profile by itself?</summary>

With the Steam version of the game, yes: tick **Start in my profile from Steam** on the **Account** tab. From then on, Steam's **Play** button starts the game straight in your profile.

* **On Windows,** DewDrop adds SMAPI's `--mods-path` option to Stardew Valley's launch options in Steam. It makes the change when you quit the game, and Steam closes and opens again once to save it.
* **On Linux and the Steam Deck,** it works with DewDrop's Steam launcher, which the installer sets up (on Linux other than the Deck, with its [`--steam-setup`](../reference/installer-options.md#steam-on-linux-and-the-steam-deck) option). The launcher reads the setting each time it starts the game, so a change counts at once and Steam never closes.

It isn't available with GOG or the Xbox app (no Steam launch options), on macOS (SMAPI's launcher there doesn't pass them on), on Linux without DewDrop's Steam launcher, or with Proton and Wine. There, DewDrop asks **Load your profile?** on the title screen, and **Restart now** restarts the game into your profile. On Windows, you can also add `--mods-path` to a desktop shortcut by hand: see [Do it by hand](../guides/start-in-your-profile.md#do-it-by-hand).

</details>

<details>
<summary>Do I still need Apply &#x26; Restart?</summary>

To load a change right away, yes. When you turn mods on or off, switch profiles, or install, update or uninstall a mod while the game is running, **Apply &#x26; Restart** restarts the game with the change.

* **With Start in my profile from Steam on,** you can also wait. DewDrop links your changes as you quit the game, and your next **Play** from Steam loads them. The status line says "Changes load the next time you start the game from Steam, or restart now."
* **Without it,** a normal launch loads your whole `Mods` folder, so it takes a restart to get into your profile: **Restart now** in the **Load your profile?** box, or **Apply &#x26; Restart**.

Uninstalled mods are deleted when the game closes, however you quit. On Windows, a mod update that says **Update ready, restart to finish** also goes in when you quit.

</details>

<details>
<summary>Why does Steam close when I quit the game?</summary>

That's **Start in my profile from Steam** saving its change, on Windows. Steam writes over its settings file while it runs, so a game's launch options can only be changed while Steam is closed. When you quit the game, DewDrop waits a few seconds, closes Steam, changes the launch option, and opens Steam again in the system tray.

It happens once each time you turn the setting on or off, not every time you play. On Linux and the Steam Deck, DewDrop's Steam launcher reads the setting itself, so Steam stays open. If it happens every time, see [Troubleshooting](troubleshooting.md#steam-closed-and-opened-again-by-itself).

</details>

<details>
<summary>Can I undo Start in my profile from Steam?</summary>

Yes. Untick **Start in my profile from Steam** on the **Account** tab.

* **On Windows,** Steam closes and opens again once when you quit the game, and DewDrop removes the `--mods-path` part from the launch option. SMAPI's part stays, so **Play** still starts the game with mods, loading your whole `Mods` folder.
* **On Linux and the Steam Deck,** it counts at once, and Steam stays open. DewDrop's Steam launcher then starts the game with your whole `Mods` folder.

You can also delete the `--mods-path "..."` part from Steam's launch options by hand on Windows. If DewDrop is still installed, untick the setting too, or DewDrop puts the part back when you quit. See [Start in your profile](../guides/start-in-your-profile.md#turn-it-off).

</details>

<details>
<summary>Why can't I click Apply &#x26; Restart?</summary>

Hover over the button to see why. Usually it says "Nothing to apply: the loaded mods already match this profile.", which means there's nothing to restart for. If the bottom bar shows a red message about `Settings.json` or a profile file, see [Troubleshooting](troubleshooting.md#a-red-message-at-the-bottom-of-the-menu).

</details>

<details>
<summary>Why can't I turn DewDrop itself off?</summary>

If DewDrop were off after a restart, there'd be no **Mods** button to turn it back on. So it stays on in every profile, and it has no trash can on the **Installed** tab. To stop using it, [uninstall DewDrop](../getting-started/uninstalling.md).

</details>

<details>
<summary>Can I play without mods sometimes?</summary>

Yes, with a profile:

1. On the **Profiles** tab, type a name such as `Vanilla` in the **New profile name** box and click **Create**.
2. Click **Use this profile**.
3. On the **Installed** tab, turn on **Save Backup**, so your saves stay backed up.
4. Click **Apply &#x26; Restart**.

While `Vanilla` is your profile, **Play** in Steam starts it if **Start in my profile from Steam** is on. Otherwise DewDrop offers to load it whenever you start the game. DewDrop stays on, so you can still get back: pick your usual profile, click **Use this profile**, and **Apply &#x26; Restart**. See [Profiles](../guides/profiles.md).

</details>

<details>
<summary>Can I install a mod from a zip file I downloaded?</summary>

Not from the menu yet. Extract the zip into your `Mods` folder, so the mod's `manifest.json` ends up inside a folder in `Mods`. If the game is running, click **Rescan** on the **Installed** tab so the mod shows up.

With **Turn on mods when installed** on (the default), DewDrop turns the new mod on in your active profile, when the game starts or when you click **Rescan**. If it isn't loaded yet, its row says **On after restart**: click **Apply &#x26; Restart** to load it. See [Managing your mods](../guides/installed.md).

</details>

<details>
<summary>How do I delete a mod?</summary>

Uninstall it (DewDrop 0.2.0 and later):

* On the **Installed** tab, click the trash can at the right of the mod's row, then click **Uninstall**.
* Or, on the mod's page in **Browse**, click the red **Uninstall** button at the top.

DewDrop turns the mod off in your profile now, and deletes its folder, settings included, when the game closes. Until then, its row on the **Installed** tab has an **Undo** button. See [Uninstall a mod](../guides/installed.md#uninstall-a-mod).

If you might want the mod again, turning it off is enough: its folder and settings stay.

</details>

<details>
<summary>How can I tell which mods I already have while browsing?</summary>

Since DewDrop 0.2.0, Browse marks them. A card says **Installed** in green, or **Update** in orange when a newer version is out. On a mod's page, the top bar shows the version you have, and the file you installed says **Installed** instead of having an install button. See [Browsing Nexus Mods](../guides/browse.md) and [A mod's page](../guides/mod-page.md).

DewDrop can't recognize every mod. A content pack you added by hand, with no Nexus Mods update key in its `manifest.json`, may not get a tag. [How DewDrop knows you have a mod](../guides/browse.md#how-dewdrop-knows-you-have-a-mod) explains why.

</details>

<details>
<summary>Do my mod settings change when I switch profiles?</summary>

No. Each mod has one `config.json` in its own folder, and all your profiles share it. Stardrop's option to keep separate mod settings per profile isn't supported in DewDrop.

</details>

<details>
<summary>Where did my downloads list go?</summary>

The **Downloads** tab only lists downloads from the current session, and it's cleared when the game restarts. The mods themselves are installed: you'll find them on the **Installed** tab.

</details>

<details>
<summary>Can I use a controller?</summary>

Yes. Move the cursor with the left stick and press **A** to click. **LB** and **RB** (**L1** and **R1** on a Steam Deck) switch tabs, and **B** closes a dialog or the menu. Selecting a text box opens an on-screen keyboard: Steam's, in Game Mode on a Steam Deck. See [The DewDrop menu](../guides/the-menu.md).

</details>

<details>
<summary>Is DewDrop available in other languages?</summary>

Not yet. The menu is in English only.

</details>

## Nexus Mods

<details>
<summary>Do I need a Nexus Mods account?</summary>

No. Without one you can still turn mods on and off, use profiles, check for mod updates, and browse Nexus Mods. To download mods from inside the game, you need a Nexus Mods account (a free one works) and its API key. See [Connecting your Nexus account](../nexus-mods/connecting.md).

</details>

<details>
<summary>Do I need Nexus Mods Premium?</summary>

No, but it makes downloads quicker:

* **Premium:** a mod file's **Install** button downloads and installs it right in the game, on any system, including the Steam Deck.
* **Free:** **Get on Nexus** opens the file on the website, and **Slow download** there sends it back to DewDrop. This only works on Windows for now, after a one-time setup on the **Account** tab. On macOS, Linux and the Steam Deck, download the mod from the website, add it to `Mods` yourself, click **Rescan** on the **Installed** tab, and click **Apply &#x26; Restart**. [Install the mod by hand](../nexus-mods/free-vs-premium.md#install-the-mod-by-hand) has the steps.

See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md).

</details>

<details>
<summary>How do I log in to Nexus Mods on a Steam Deck?</summary>

The easiest way is to let the installer take your API key while you set DewDrop up in Desktop Mode: paste it into Konsole when it asks, and DewDrop logs in with it the next time the game starts. You can also log in on the **Account** tab in Game Mode, with Steam's on-screen keyboard. See [On a Steam Deck](../nexus-mods/connecting.md#on-a-steam-deck).

</details>

<details>
<summary>Can DewDrop install Nexus collections?</summary>

No. DewDrop installs single mods. Use Stardrop to install a collection. DewDrop can still use the collection profiles Stardrop makes: they're read-only, so click **Duplicate** on the **Profiles** tab to get a copy you can change.

</details>

<details>
<summary>Is my Nexus API key safe?</summary>

DewDrop only sends the key to Nexus Mods. It stores it the way Stardrop does, scrambled in `Settings.json` with what unscrambles it in `Cache/Notion.json`, so never share those two files. (On a Steam Deck, a key you give the installer waits as plain text in `Cache/DewDrop/nexus-key.txt` until DewDrop has logged in with it and saved it. Don't share that file either.) To remove it, click **Log out** on the **Account** tab; Stardrop is logged out too. See [How safe is my key?](../nexus-mods/connecting.md#how-safe-is-my-key)

</details>

## Compatibility

<details>
<summary>Can I use DewDrop with Stardrop?</summary>

Yes, side by side. They share profiles, settings and your Nexus login, so changes in one (including logging out or deleting a profile) show up in the other. The first time, start the game from Steam or your SMAPI shortcut rather than from Stardrop, so DewDrop can add itself to your Stardrop profiles. See [Using DewDrop with Stardrop](../stardrop.md).

</details>

<details>
<summary>Can I use DewDrop with Vortex or another mod manager?</summary>

They can share a `Mods` folder, but they don't know about each other, so use one tool to install, update and remove mods. Only one app at a time can open the website's "Mod manager download" links; see [When Stardrop or Vortex takes the links back](../nexus-mods/mod-manager-links.md#when-stardrop-or-vortex-takes-the-links-back).

</details>

<details>
<summary>Does DewDrop work on Mac and Linux?</summary>

It's built to, but so far only Windows has been fully tested. The installer has been tested on Linux and on a Mac with an Apple chip, where it offers to install Rosetta, Apple's add-on that SMAPI's installer needs. What's different:

* Website downloads for free Nexus Mods accounts don't reach DewDrop yet.
* **Apply &#x26; Restart** hasn't been tested yet.
* **Start in my profile from Steam** isn't available on macOS. On Linux, it needs DewDrop's Steam launcher.

On Linux, you need the Linux version of the game, not the Windows version running through Proton (Steam's tool for running Windows games on Linux). The installer warns you if it finds the Windows version, and [Troubleshooting](troubleshooting.md#linux-the-windows-version-of-the-game) shows how to switch. See [Platform support](../reference/platform-support.md) for the details.

</details>

<details>
<summary>Does DewDrop work on Steam Deck?</summary>

Yes, from DewDrop 0.2.0. It's new and still being tested. You run the installer once in Desktop Mode. It sets Stardew Valley up in Steam to start through DewDrop's launcher, and can take your Nexus Mods API key so you don't have to type it. After that, you install, update and switch mods from the **Mods** button in Game Mode.

* **Apply &#x26; Restart** is meant to bring the game back in Game Mode, which is still being checked on a real Deck. If the game doesn't come back, press **Play** again. If the restart never got as far as starting the game, that launch opens in your profile instead.
* There's no SMAPI console window, so console commands such as `dewdrop_nxm` can't be typed. The SMAPI log has what SMAPI and DewDrop report.
* Free Nexus Mods accounts install mods by hand, in Desktop Mode.
* The Windows version of the game through Proton isn't supported.

See [DewDrop on the Steam Deck](../getting-started/steam-deck.md).

</details>

<details>
<summary>Does DewDrop work with the Xbox app or GOG?</summary>

Yes. The Xbox app always starts the game's own `.exe`, so SMAPI's guide swaps in a copy of SMAPI under that name. The DewDrop installer can do this for you. A game update undoes it, so run the installer again after each update.

With GOG on Windows, start the game with the **Stardew Valley (SMAPI)** desktop shortcut the installer offers. GOG Galaxy's **Play** button starts the game without mods unless you set Galaxy up; see [GOG Galaxy on Windows](../getting-started/installing/smapi.md#gog-galaxy-on-windows).

**Start in my profile from Steam** is for the Steam version only. With the Xbox app or GOG, the **Load your profile?** box and **Apply &#x26; Restart** get you into your profile.

</details>

<details>
<summary>Does DewDrop work in multiplayer?</summary>

DewDrop only adds a menu to the title screen, so other players don't need it. Whether everyone needs the same mods depends on those mods, so check each mod's page. A profile with your friends' mods makes switching for co-op easy.

</details>

## Updating and removing DewDrop

<details>
<summary>How do I update DewDrop?</summary>

When a new DewDrop is out, SMAPI mentions it in its window and log as the game starts, and DewDrop's own row on the **Installed** tab gets an **Update to** button that opens the release page. You can also check the [Releases page](https://github.com/friessssss/DewDrop/releases/latest).

To update, close the game and run the installer again, for example by pasting the same install line you used the first time. It updates DewDrop in place and keeps your settings. On a Steam Deck, do this in Desktop Mode. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

</details>

<details>
<summary>How do I uninstall DewDrop?</summary>

1. **On Windows, if you turned on Start in my profile from Steam,** untick it first and quit the game, so DewDrop takes `--mods-path` out of Steam's launch options. Otherwise SMAPI keeps loading your last profile, with no **Mods** button to change it.
2. Close the game and delete the `DewDrop` folder from your `Mods` folder.

Your other mods stay where they are. A normal launch then loads all of them again, including any you'd turned off in DewDrop.

On Linux and the Steam Deck, Steam's launch option may run DewDrop's launcher from DewDrop's data folder. Take it out of Steam before you delete that folder. [Uninstalling DewDrop](../getting-started/uninstalling.md) has the details.

</details>
