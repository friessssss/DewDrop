---
description: "Turn mods on and off for your profile, find mods fast, spot missing requirements and update mods from the Installed tab."
icon: list-check
---

# Managing your mods

The **Installed** tab lists every mod DewDrop found in your `Mods` folder, sorted by name. A tick in a mod's checkbox means it's on in your active profile. This is where the menu opens every time.

<figure><img src="../.gitbook/assets/shot-installed.png" alt="DewDrop's Installed tab: a search box, the Show: All filter button, Rescan and Check for updates along the top, then a list of mods, each with a checkbox, its name, author and version"><figcaption><p>The Installed tab.</p></figcaption></figure>

## Turn a mod on or off

1. Click anywhere on the mod's row. The checkbox ticks or unticks.
2. Do the same for any other mods you want to change.
3. When you're done, click **Apply & Restart** in the bottom-right corner to restart the game with your changes.

DewDrop saves each change to your profile straight away. Nothing is deleted when you turn a mod off: its folder stays where it is, and you can turn it back on any time.

When you turn a mod on, DewDrop also turns on any mods it needs that you already have installed. A message tells you which, for example "Also turned on what it needs: Content Patcher".

You can't turn DewDrop itself off. If you try, it reminds you: "DewDrop has to stay on so this menu is here next time."

{% hint style="info" %}
Your changes load the next time the game starts in your profile: after **Apply & Restart**, or, with **Start in my profile from Steam** on, from Steam's **Play** button. Any other launch, such as a desktop shortcut, loads every mod in your `Mods` folder, including the ones you turned off. DewDrop then offers on the title screen to restart into your profile, or you can click **Apply & Restart**. See [Start in your profile](start-in-your-profile.md) and [How launching works](../getting-started/launching.md).
{% endhint %}

## Find a mod

**Search.** Type in the **Search installed mods** box. The list filters as you type, and matches a mod's name, author or unique ID. Capital letters don't matter.

**Filter.** Click the **Show:** button to step through these views. The search and the filter work together.

| Button | Shows |
| --- | --- |
| **Show: All** | Every installed mod. |
| **Show: Enabled** | Mods that are on in your profile. |
| **Show: Disabled** | Mods that are off. |
| **Show: Updates** | Mods with a new update you haven't downloaded yet. |
| **Show: Problems** | Mods that are on but need a mod that's missing or turned off. |

Click again after **Show: Problems** to get back to **Show: All**. If nothing matches, the list says "No mods match."

When there's room, the toolbar also shows a count, such as "42 installed, 30 on".

## Added or removed a mod by hand?

DewDrop looks through your `Mods` folder when the game starts. A mod you copied in while the game was closed is turned **on** in your active profile then, as long as **Turn on mods when installed** is on, which it is by default. The next time you open the menu, a message lists them: "Turned on *mods*, new in your Mods folder." If a mod didn't load this time, for example because the game started in your profile, its row says **On after restart**.

If you copy a mod in (or delete one) while the game is running, click **Rescan**. New mods are turned on the same way, and a message says what changed, for example "Found 1 new mod: *name*. Restart to load it." If a new mod is still off, for example because **Turn on mods when installed** is off, the message says "Turn it on, then restart to load it." instead.

* Only mods DewDrop has never seen before are turned on. A mod you've turned on or off before keeps your choice, even if you move it to another folder or extract it again.
* Mods from Stardrop collections are never turned on this way, and a collection profile is never changed, because it's read-only.
* With **Turn on mods when installed** off, mods you add by hand stay off. Click them to turn them on, or they won't load the next time the game starts in your profile. See [Settings](settings.md).

**Rescan** waits if DewDrop is in the middle of installing something; try again when it's done.

## Read a mod's row

Each row has:

* **A checkbox.** Ticked means on. It's dimmed when you can't change it: DewDrop's own row, or every row when the profile is read-only or its file couldn't be read.
* **The name.** It's grayed out when the mod is off.
* **A details line** with the author and version, such as "by Pathoschild | v2.5.3". It also says "content pack" when the mod adds content through another mod, and "collection" when Stardrop installed it as part of a Nexus collection.
* **A status note** on the right, when there's something to know (see below).
* **An Update button,** when there's a new version (see [Updates](#updates)).

Hover over a row to see the mod's description, its unique ID, which mod it's a content pack for, and the name of its folder.

### Status notes

| Note | What it means |
| --- | --- |
| **Update ready, restart to finish** (green) | DewDrop downloaded an update, but the game is still using the old files. It goes in when you use **Apply & Restart**, or, on Windows, when you quit the game. See [Updating mods](updating-mods.md). |
| **Needs** followed by names (red) | The mod is on, but something it requires is missing or turned off. See below. |
| **Off after restart** (orange) | The mod is loaded right now, but it's off in your profile, so it won't load the next time the game starts in your profile, such as after **Apply & Restart**. |
| **On after restart** (orange) | The mod is on in your profile but isn't loaded yet, for example because you turned it on after the game started, or it's new in your `Mods` folder. It loads the next time the game starts in your profile, such as after **Apply & Restart**. When the game was started from your profile, this note also shows for a mod SMAPI skipped when this session started, for example a broken one or one that needs a newer SMAPI. Restarting won't load that one; the SMAPI console window says why. |

If the bottom bar shows a red problem, the two "after restart" notes are hidden until it's fixed.

## Missing requirements

Many mods need another mod to work, such as a framework like Content Patcher. When a mod that's on is missing one, its row says **Needs** and lists what's missing. Use **Show: Problems** to see all of them at once.

* **If the note shows a mod's name,** you have that mod but it's off. Click its row to turn it on.
* **If the note shows a unique ID** (like `Pathoschild.ContentPatcher`), that mod isn't installed. Search for it on the [Browse](browse.md) tab and install it, then turn it on here.

DewDrop doesn't download required mods for you. The mod's Nexus page usually lists what it needs.

## Updates

DewDrop checks for mod updates each time the game starts, using the same service SMAPI uses (smapi.io). You can turn that off in [Settings](settings.md). To check again, click **Check for updates**. It reads **Checking...** while it works, then a message says "Everything's up to date." or how many updates are available.

Mods with a new version get a green **Update to** button, such as **Update to 1.4.0**, and the tab reads **Installed (1 new)**. What clicking it does depends on your Nexus Mods account. Premium installs the update in the game and keeps the mod's settings. Free opens the file on the Nexus Mods website, where you click **Slow download**. If you're logged out, or the update isn't on Nexus Mods, it opens the update's page in your browser. [Updating mods](updating-mods.md) has the details, including what happens on macOS and Linux.

## Collection profiles

If your active profile came from a Nexus collection in Stardrop, it's read-only. The tab shows "This collection profile is read-only. Duplicate it on the Profiles tab to change it." To change which mods are on, make a copy on the [Profiles](profiles.md) tab and use the copy.

## What the Installed tab can't do yet

* **Remove a mod.** There's no uninstall button. Close the game, then delete the mod's folder from your `Mods` folder.
* **Install a mod from a file you downloaded.** Unzip it into your `Mods` folder yourself, then click **Rescan**. It shows up here, turned on if **Turn on mods when installed** is on. See [Added or removed a mod by hand?](#added-or-removed-a-mod-by-hand)
* **Sort the list.** It's always sorted by name.

DewDrop finds mods inside sub-folders of `Mods` too. Like SMAPI, it skips folders whose names start with a dot, so renaming a folder to `.MyMod` hides it from both.
