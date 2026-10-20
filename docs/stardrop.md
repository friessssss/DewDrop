---
description: "DewDrop and Stardrop share profiles, settings and your Nexus login, so you can use them side by side."
icon: handshake
---

# Using DewDrop with Stardrop

[Stardrop](https://github.com/Floogen/Stardrop) is a popular Stardew Valley mod manager that runs outside the game. DewDrop began as a fork of Stardrop's code (both are open source under GPL-3.0) and uses Stardrop's data folder on purpose, so you can use both side by side and switch between them whenever you like.

DewDrop is a separate project. It isn't affiliated with or endorsed by Stardrop's authors. Many thanks to [Floogen](https://github.com/Floogen) and the Stardrop contributors, and to [Pathoschild](https://github.com/Pathoschild) for [SMAPI](https://smapi.io).

You don't need Stardrop to use DewDrop. If you've never installed it, you can skip this page. You'll still see a `Stardrop` folder in your AppData folder (`~/.config` on macOS and Linux), because that's where DewDrop keeps its data too.

## What the two share

DewDrop reads and writes Stardrop's data folder: `C:\Users\<you>\AppData\Roaming\Stardrop\Data` on Windows, or `~/.config/Stardrop/Data` on macOS and Linux.

| Shared | What that means for you |
| --- | --- |
| Profiles | Both show the same profiles. A change in one shows up in the other the next time that one starts. Deleting a profile in either one deletes it in both. |
| The selected profile | Switching profiles in one switches the other too. Stardrop only saves its choice when you close it, and each one picks up a change the next time it starts. |
| Mod folder settings | DewDrop lists the mods in Stardrop's **Mod Folder Path** and installs downloads into its **Stardrop Installed Mods Path**. |
| Nexus Mods login | Log in or out in either one, and it changes for both. See [Connecting your Nexus account](nexus-mods/connecting.md). |
| The `Selected Mods` folder | Both build this folder of links to your profile's mods before they start the game. DewDrop's **Start in my profile from Steam** points Steam at this same folder. |
| Collections | Collections you installed with Stardrop show up in DewDrop. |

DewDrop keeps its own settings in its own `config.json` (see [Settings](guides/settings.md)), and its downloads and other working files in `Cache\DewDrop`. Stardrop's own options stay Stardrop's. [Where DewDrop keeps files](reference/file-locations.md) has the full list.

<figure><img src=".gitbook/assets/shot-profiles.png" alt="DewDrop's Profiles tab, with a list of profiles on the left and the selected profile's mods on the right."><figcaption><p>The Profiles tab shows the same profiles Stardrop does. Stardrop's mod notes appear when you hover over a mod.</p></figcaption></figure>

## First-time setup if you already use Stardrop

Install DewDrop the usual way, with the [installer](getting-started/installing/installer.md) or [by hand](getting-started/installing/manual.md). Then there's one extra step.

Stardrop only starts the mods that are ticked in your profile, and a newly added mod isn't ticked yet. So the first time, DewDrop needs a hand. Pick one of these:

{% tabs %}
{% tab title="Start once from Steam" %}
1. Start Stardew Valley from Steam, or from the SMAPI shortcut, not from Stardrop. This one launch loads everything in your `Mods` folder, including mods your profile has turned off. That's fine for once.
2. DewDrop adds itself to every Stardrop profile it can change (all of them except collection profiles).
3. If the mods that loaded differ from your Stardrop profile, DewDrop asks **Load your profile?** on the title screen. Click **Restart now** to switch to your Stardrop profile, or click **Not now**, quit, and start the game from Stardrop.
{% endtab %}

{% tab title="Tick it in Stardrop" %}
1. Open Stardrop. If it was already open while you installed DewDrop, click **Tools** > **Refresh Mod List** so it finds DewDrop.
2. Tick **DewDrop** in your profile. If you turned off Stardrop's automatic profile saving, save the profile.
3. Start SMAPI from Stardrop as usual. DewDrop then adds itself to your other profiles too.
{% endtab %}
{% endtabs %}

The installer reminds you about this when it finds Stardrop. After this first time, DewDrop turns itself back on in every profile it can change, each time it starts.

Leave DewDrop ticked in Stardrop. If it's unticked, starting from Stardrop doesn't load it and the **Mods** button is missing. The same goes for a brand-new profile you make in Stardrop: DewDrop isn't in it until DewDrop next runs, so tick it there. Profiles you make in DewDrop always include it.

## Starting the game

Any of these works:

* **From Stardrop.** Stardrop links your profile's mods and starts SMAPI. DewDrop notices, and until you change something its bottom bar says "The mods loaded right now match this profile." The game already starts in your profile, so DewDrop's **Load your profile?** box doesn't appear. If you picked another profile in Stardrop since you last closed it, DewDrop still shows the old one (see the warning below); select the right one on DewDrop's **Profiles** tab and click **Use this profile**.
* **From DewDrop.** Change things in the menu and click **Apply & Restart**. DewDrop rebuilds the same `Selected Mods` folder and restarts the game itself, without Stardrop.
* **From a shortcut, or from Steam without Start in my profile from Steam.** This is a normal launch: it loads your whole `Mods` folder, not a profile. [How launching works](getting-started/launching.md) explains the difference. If the mods that load differ from your selected profile, the same one Stardrop uses, DewDrop offers on the title screen to restart into it.
* **From Steam, with Start in my profile from Steam on** (Windows). **Play** starts the game on `Selected Mods`, the folder Stardrop fills too. Stardrop links your profile again whenever it starts the game, and DewDrop does so as you quit the game if anything changed. So **Play** loads whatever was linked last, by either of them. See [Start in your profile](guides/start-in-your-profile.md).

{% hint style="warning" %}
**Stardrop saves its settings when you close it**, including which profile is selected and its copy of your Nexus login. Until then, DewDrop doesn't see a profile you just picked in Stardrop. And if Stardrop stays open while you switch profiles or log in or out in DewDrop, closing Stardrop later puts its own version back. Close Stardrop once the game has started, or check those settings again in Stardrop afterwards.
{% endhint %}

## What still needs Stardrop

**Installing collections.** DewDrop can't install Nexus collections. Install them in Stardrop (**Nexus Mods** > **Collections**). After that, DewDrop shows them:

* The collection's profile appears on the **Profiles** tab marked "collection, read-only", and its mods carry a "collection" tag.
* Collection mods have no trash can on the **Installed** tab, because the collection owns them. To remove them, remove the collection in Stardrop.
* You can switch to it and click **Apply & Restart**. DewDrop always links itself in, so the **Mods** button stays even though collection profiles don't list DewDrop.
* To change which mods are on, click **Duplicate** first. The copy is a normal profile you can edit.
* DewDrop doesn't offer updates for collection mods.
* Starting a collection profile from Stardrop doesn't load DewDrop, because DewDrop can't add itself to a read-only profile. Apply it from DewDrop instead.
* With **Start in my profile from Steam** on, starting a collection profile from Stardrop also leaves Stardrop's links in `Selected Mods`, so your next **Play** from Steam starts that collection without the **Mods** button. To get it back, start one of your own profiles from Stardrop once, or see [If Steam starts the game in your profile](help/troubleshooting.md#if-steam-starts-the-game-in-your-profile).

If DewDrop handles the website's [mod manager links](nexus-mods/mod-manager-links.md), a collection's download button on Nexus Mods sends the link to DewDrop, which answers "That was a Nexus collection. DewDrop installs single mods; collections install through Stardrop." Give the links to Stardrop first (in Stardrop, **View** > **Settings** > **Register NXM Association**).

**Profile-specific mod configs.** If you turned on Stardrop's **Enable Profile Specific Mod Configs**, Stardrop swaps each mod's `config.json` when you switch profiles. DewDrop keeps those saved configs in your profile files but doesn't swap them in. After **Apply & Restart** into another profile, or a **Play** from Steam with **Start in my profile from Steam** on, your mods keep the settings they had last. Starting from Stardrop swaps them as usual. [Uninstalling a mod](guides/installed.md#uninstall-a-mod) in DewDrop leaves the settings Stardrop saved for it in your profiles alone.

## If Stardrop uses its own mods folder

Most people leave Stardrop's **Mod Folder Path** on the game's `Mods` folder, and everything above works as described. If you pointed it somewhere else, there are three things to know about.

**Where DewDrop lives.** DewDrop lists the mods in Stardrop's folder, and Stardrop only links mods it finds there. The installer always puts DewDrop in the game's `Mods` folder, so:

* Starting from Steam loads DewDrop, and **Apply & Restart** works, because DewDrop always links its own folder wherever it is.
* Starting from Stardrop doesn't load DewDrop, because Stardrop can't see it.

To start from Stardrop instead, move the `DewDrop` folder into Stardrop's mods folder. Then a plain Steam launch no longer loads it, unless Stardrop's folder is itself inside the game's `Mods` folder. Keep only one copy, and [update it by hand](getting-started/updating-dewdrop.md), because the installer only looks in the game's `Mods` folder.

**Starting from Steam.** A normal Steam launch loads the game's `Mods` folder, not Stardrop's, so it usually doesn't match your profile. The **Load your profile?** box then offers to restart into your profile each time, unless **Start in my profile from Steam** already starts the game in it (see [Start in your profile](guides/start-in-your-profile.md)). Mods in the game's `Mods` folder that aren't in Stardrop's folder, such as SMAPI's **Console Commands** and **Save Backup**, can't be turned on in a profile, so starting in your profile leaves them out.

**Where downloads go.** DewDrop installs downloads into Stardrop's **Stardrop Installed Mods Path**. Stardrop normally insists that this folder is inside the **Mod Folder Path**. If it isn't, for example after editing `Settings.json` by hand, a download still says "Installed 1 mod", but the mod doesn't appear on the **Installed** tab and isn't turned on. To fix it, set **Stardrop Installed Mods Path** to a folder inside **Mod Folder Path** in Stardrop's **View** > **Settings**, and move the new mods there. The **Settings** panel on DewDrop's **Account** tab shows the folder DewDrop installs into as "Mods folder".

## Removing one of them

* **Removing DewDrop** doesn't affect Stardrop. Leave the `Stardrop\Data` folder alone. Your Stardrop profiles still list DewDrop; untick or remove it there if Stardrop mentions it. If you turned on **Start in my profile from Steam**, untick it and quit the game before you remove DewDrop, so Steam's **Play** button goes back to loading your whole `Mods` folder. See [Uninstalling DewDrop](getting-started/uninstalling.md).
* **Removing Stardrop** doesn't affect DewDrop. Don't delete the `Stardrop\Data` folder: DewDrop's profiles and Nexus login live there.
