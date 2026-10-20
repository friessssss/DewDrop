---
description: "Short answers to the questions new DewDrop players ask most."
icon: circle-question
---

# FAQ

Click a question to see the answer. For step-by-step fixes, see [Troubleshooting](troubleshooting.md). To ask for help, see [Reporting issues](reporting-issues.md).

## The basics

<details>
<summary>What does DewDrop do?</summary>

DewDrop is a mod manager that lives inside Stardew Valley. It adds a **Mods** button to the title screen. From there you can turn mods on and off, keep different mod lists as profiles, browse and install mods from Nexus Mods, and update the mods you have. It's a mod itself, so it needs [SMAPI](../getting-started/installing/smapi.md), the free tool that loads Stardew Valley mods.

</details>

<details>
<summary>What's the black window next to my game?</summary>

That's SMAPI's console. It opens when the game starts with mods and lists what's loading. Yellow lines and messages about mod updates are normal. On macOS and Linux it's a Terminal window.

Leave it open while you play: closing it closes the game. If it doesn't open at all, the game started without mods. (On macOS, it usually doesn't come back after **Apply &#x26; Restart** or **Restart now**. That's fine: if the **Mods** button is there, your mods loaded.)

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

DewDrop is open source (GPL-3.0), so anyone can read its code on [GitHub](https://github.com/friessssss/DewDrop). A few things it's careful about:

* Turning mods off never moves or deletes them.
* The installer gets SMAPI only from SMAPI's official GitHub page, and never deletes your mods, saves or settings.
* The installer asks before it changes Steam's settings, changes them only for the Steam account you're signed in to, and backs them up first.
* **Start in my profile from Steam** is the same: it's off until you or the installer turn it on, it only changes the signed-in account's settings, and it backs them up first. It never writes over launch options you changed while the game was running.
* Your Nexus Mods API key is only sent to Nexus Mods.

If you turn on "Mod manager download" links on Windows, any website can try to send DewDrop a Nexus Mods download, and with Premium, DewDrop installs it without asking. Your browser usually asks first, so only allow prompts you expect. See ["Mod manager download" links](../nexus-mods/mod-manager-links.md).

</details>

<details>
<summary>Does DewDrop move or delete my mods?</summary>

No. Your mods stay in your `Mods` folder. Turning a mod off only changes your profile, and **Apply &#x26; Restart** loads your profile through links, not copies. Deleting a profile never deletes mods. The one time DewDrop replaces a mod's folder is when you update that mod, and it keeps the mod's `config.json`. See [How DewDrop works](../reference/how-it-works.md).

</details>

<details>
<summary>Will DewDrop affect my saves?</summary>

DewDrop doesn't read or change your save files. But turning off a mod that a save uses can still affect that save, the same as removing the mod by hand. For example, items a mod added may disappear. Keep **Save Backup** on so you have backups.

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

Click **Apply &#x26; Restart** to load exactly your profile. By default, DewDrop also offers this restart with a **Load your profile?** box on the title screen. On Windows with the Steam version, **Start in my profile from Steam** makes **Play** load your profile from the start.

If every mod in your `Mods` folder is on in your profile, a normal launch already loads the same mods, and the line says "The mods loaded right now match this profile." See [How launching works](../getting-started/launching.md).

If it says "This session didn't start from a DewDrop profile. Restart to load this profile.", SMAPI was started with a `--mods-path` launch option that points at some other folder. Click **Apply &#x26; Restart** to switch into your profile.

</details>

<details>
<summary>Can DewDrop start the game in my profile by itself?</summary>

On Windows with the Steam version, yes: tick **Start in my profile from Steam** on the **Account** tab. SMAPI picks which mods to load as the game starts, before DewDrop runs, so DewDrop can't change what the current launch loads. Instead, it adds SMAPI's `--mods-path` option to Stardew Valley's launch options in Steam, pointing at your profile's links. From then on, **Play** starts the game straight in your profile. DewDrop makes the change when you quit the game, and Steam closes and opens again once to save it.

Everywhere else, and when you start the game from a shortcut, DewDrop asks **Load your profile?** on the title screen, and **Restart now** restarts the game into your profile. If every mod in your `Mods` folder is on in your profile, there's nothing to restart for, because a normal launch already matches. See [Start in your profile](../guides/start-in-your-profile.md).

</details>

<details>
<summary>Do I still need Apply &#x26; Restart?</summary>

To load a change right away, yes. When you turn mods on or off, switch profiles, or install or update a mod while the game is running, **Apply &#x26; Restart** restarts the game with the change.

* **With Start in my profile from Steam on,** you can also wait. DewDrop links your changes as you quit the game, and your next **Play** from Steam loads them. The status line says "Changes load the next time you start the game from Steam, or restart now."
* **Without it,** a normal launch loads your whole `Mods` folder, so it takes a restart to get into your profile: **Restart now** in the **Load your profile?** box, or **Apply &#x26; Restart**.

A mod update that says **Update ready, restart to finish** also goes in when you quit the game on Windows.

</details>

<details>
<summary>Why does Steam close when I quit the game?</summary>

That's **Start in my profile from Steam** saving its change. Steam writes over its settings file while it runs, so a game's launch options can only be changed while Steam is closed. When you quit the game, DewDrop waits a few seconds, closes Steam, changes the launch option, and opens Steam again in the system tray.

It happens once each time you turn the setting on or off, not every time you play. If it happens every time, see [Troubleshooting](troubleshooting.md#steam-closed-and-opened-again-by-itself).

</details>

<details>
<summary>Can I undo Start in my profile from Steam?</summary>

Yes. Untick **Start in my profile from Steam** on the **Account** tab. When you quit the game, Steam closes and opens again once, and DewDrop removes the `--mods-path` part from the launch option. SMAPI's part stays, so **Play** still starts the game with mods, loading your whole `Mods` folder.

You can also do it by hand: in Steam, right-click **Stardew Valley**, choose **Properties**, and on the **General** page delete only the `--mods-path "..."` part of **Launch Options**. If DewDrop is still installed, untick the setting too the next time you play, or DewDrop puts the part back when you quit.

</details>

<details>
<summary>Why can't I click Apply &#x26; Restart?</summary>

Hover over the button to see why. Usually it says "Nothing to apply: the loaded mods already match this profile.", which means there's nothing to restart for. If the bottom bar shows a red message about `Settings.json` or a profile file, see [Troubleshooting](troubleshooting.md#a-red-message-at-the-bottom-of-the-menu).

</details>

<details>
<summary>Why can't I turn DewDrop itself off?</summary>

If DewDrop were off after a restart, there'd be no Mods button to turn it back on. So it stays on in every profile. To stop using it, [uninstall it](../getting-started/uninstalling.md).

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

Not from the menu yet. Extract the zip into your `Mods` folder, so the mod's `manifest.json` ends up inside a folder in `Mods`. If the game is running, click **Rescan** on the **Installed** tab so the mod shows up. With **Turn on mods when installed** on (the default), DewDrop turns the new mod on in your active profile, when the game starts or when you click **Rescan**. If it isn't loaded yet, its row says **On after restart**: click **Apply &#x26; Restart** to load it. See [Managing your mods](../guides/installed.md).

</details>

<details>
<summary>How do I delete a mod?</summary>

There's no delete button yet. Turning a mod off is usually enough. To remove it for good, close the game and delete the mod's folder from your `Mods` folder. **Open mods folder** on the **Downloads** tab opens it.

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

Yes. Move the cursor with the left stick and press **A** to click. **LB** and **RB** switch tabs, and **B** closes a dialog or the menu. Selecting a text box opens the game's on-screen keyboard. See [The DewDrop menu](../guides/the-menu.md).

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

* **Premium:** a mod file's **Install** button downloads and installs it right in the game, on any system.
* **Free:** **Get on Nexus** opens the file on the website, and **Slow download** there sends it back to DewDrop. This only works on Windows for now, after a one-time setup on the **Account** tab. On macOS and Linux, download the mod from the website and add it to `Mods` yourself, then turn it on in the **Installed** tab and click **Apply &#x26; Restart**.

See [Free vs Premium downloads](../nexus-mods/free-vs-premium.md).

</details>

<details>
<summary>Can DewDrop install Nexus collections?</summary>

No. DewDrop installs single mods. Use Stardrop to install a collection. DewDrop can still use the collection profiles Stardrop makes: they're read-only, so click **Duplicate** on the **Profiles** tab to get a copy you can change.

</details>

<details>
<summary>Is my Nexus API key safe?</summary>

DewDrop only sends the key to Nexus Mods. It stores it the way Stardrop does, scrambled in `Settings.json` with what unscrambles it in `Cache/Notion.json`, so never share those two files. To remove it, click **Log out** on the **Account** tab; Stardrop is logged out too. See [How safe is my key?](../nexus-mods/connecting.md#how-safe-is-my-key)

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

It's built to, but it's only been fully tested on Windows. On macOS and Linux, website downloads for free Nexus accounts don't reach DewDrop yet, and **Apply &#x26; Restart** hasn't been tested. See [Platform support](../reference/platform-support.md).

On Linux, you need the Linux version of the game, not the Windows version running through Proton (Steam's tool for running Windows games on Linux). To check, in Steam right-click **Stardew Valley** > **Properties** > **Compatibility**. If **Force the use of a specific Steam Play compatibility tool** is ticked, untick it, let Steam download the Linux version, and run the installer again. The installer warns you if it finds the Windows version.

</details>

<details>
<summary>Why is Start in my profile from Steam only for Steam on Windows?</summary>

It works by changing Stardew Valley's launch options in Steam, so it needs the Steam version of the game, and somewhere DewDrop can safely add SMAPI's `--mods-path` option:

* **GOG and the Xbox app** don't have Steam's launch options for DewDrop to change.
* **macOS and Linux:** SMAPI's launcher there doesn't pass on the options Steam gives it, so a launch option can't point SMAPI at your profile.
* **Steam Deck, Proton and Wine** aren't supported.

Everywhere else, the **Load your profile?** box offers to restart into your profile, and **Apply &#x26; Restart** works as always. Both use the same restart, which hasn't been tested on macOS and Linux yet. On Windows, you can also add `--mods-path` to a desktop shortcut by hand: see [Start in your profile](../guides/start-in-your-profile.md).

</details>

<details>
<summary>Does DewDrop work on Steam Deck?</summary>

It isn't supported, and it hasn't been tested. In Game Mode, Steam may stop DewDrop's restart helper when the game closes, so **Apply &#x26; Restart** might not bring the game back. The Windows version of the game through Proton isn't supported either. On Steam Deck and under Proton, DewDrop doesn't show the **Load your profile?** box, and **Start in my profile from Steam** isn't available. See [Platform support](../reference/platform-support.md#steam-deck).

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
<summary>How do I know when DewDrop has an update?</summary>

When a new DewDrop is out, SMAPI's window mentions it as the game starts, and DewDrop's own row on the **Installed** tab gets an **Update to** button that opens the release page. You can also check the [Releases page](https://github.com/friessssss/DewDrop/releases/latest). To update, run the newer installer, which updates DewDrop in place and keeps your settings. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

</details>

<details>
<summary>How do I uninstall DewDrop?</summary>

If you turned on **Start in my profile from Steam**, untick it first and quit the game, so DewDrop takes `--mods-path` out of Steam's launch options. Otherwise SMAPI keeps loading your last profile, with no Mods button to change it. (You can also delete the `--mods-path "..."` part in Steam by hand afterward.)

Then close the game and delete the `DewDrop` folder from your `Mods` folder. Your other mods stay where they are. A normal launch then loads all of them again, including any you'd turned off in DewDrop. See [Uninstalling DewDrop](../getting-started/uninstalling.md).

</details>
