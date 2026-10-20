---
description: "Turn mods on and off for your profile, find mods fast, spot missing requirements, and update and uninstall mods from the Installed tab."
icon: list-check
---

# Managing your mods

The **Installed** tab lists every mod DewDrop found in your `Mods` folder, sorted by name. A tick in a mod's checkbox means it's on in your active profile. This is where the menu opens every time.

<figure><img src="../.gitbook/assets/shot-installed.png" alt="DewDrop's Installed tab: a search box, the Show: All filter button, Rescan and Check for updates along the top, then a list of mods, each with a checkbox, its name, author and version, and a trash can at the right of each row except DewDrop's own"><figcaption><p>The Installed tab.</p></figcaption></figure>

## Turn a mod on or off

1. Click anywhere on the mod's row. The checkbox ticks or unticks.
2. Do the same for any other mods you want to change.
3. When you're done, click **Apply & Restart** in the bottom-right corner to restart the game with your changes.

DewDrop saves each change to your profile straight away. Nothing is deleted when you turn a mod off: its folder stays where it is, and you can turn it back on any time. To get rid of a mod for good, [uninstall it](#uninstall-a-mod).

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
| **Show: Problems** | Mods that are on but need a mod that's missing or turned off, and mods [installed twice](#mods-installed-twice). |

Click again after **Show: Problems** to get back to **Show: All**. If nothing matches, the list says "No mods match."

Mods you've uninstalled stay in every **Show:** view until the game closes, and in a search when they match it. See [Uninstall a mod](#uninstall-a-mod).

When there's room, the toolbar also shows a count, such as "42 installed, 30 on". While mods you uninstalled are waiting to be deleted, it can add how many, such as "36 installed, 11 on, 1 to delete".

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
* **A trash can** at the right edge, the one from the game's inventory. Click it to [uninstall the mod](#uninstall-a-mod). Its lid tips open when you point at it. DewDrop's own row and copies from Stardrop collections don't have one.

Hover over a row to see the mod's description, its unique ID, which mod it's a content pack for, and the name of its folder. For a mod [installed twice](#mods-installed-twice), it also says where the other copy is.

### Status notes

| Note | What it means |
| --- | --- |
| **Update ready, restart to finish** (green) | DewDrop downloaded an update, but the game is still using the old files. It goes in when you use **Apply & Restart**, or, on Windows, when you quit the game. See [Updating mods](updating-mods.md). |
| **Installed twice** (red) | Your `Mods` folder has two copies of this mod, with the same unique ID. See [Mods installed twice](#mods-installed-twice). |
| **Needs** followed by names (red) | The mod is on, but something it requires is missing or turned off. See below. |
| **Off after restart** (orange) | The mod is loaded right now, but it's off in your profile, so it won't load the next time the game starts in your profile, such as after **Apply & Restart**. |
| **On after restart** (orange) | The mod is on in your profile but isn't loaded yet, for example because you turned it on after the game started, or it's new in your `Mods` folder. It loads the next time the game starts in your profile, such as after **Apply & Restart**. When the game was started from your profile, this note also shows for a mod SMAPI skipped when this session started, for example a broken one or one that needs a newer SMAPI. Restarting won't load that one; the SMAPI console window says why. |
| **Deleted when the game closes** (orange) | You uninstalled the mod. Its row has an **Undo** button instead of a checkbox. See [Uninstall a mod](#uninstall-a-mod). |

If the bottom bar shows a red problem, the two "after restart" notes are hidden until it's fixed.

## Missing requirements

Many mods need another mod to work, such as a framework like Content Patcher. When a mod that's on is missing one, its row says **Needs** and lists what's missing. Use **Show: Problems** to see all of them at once.

* **If the note shows a mod's name,** you have that mod but it's off. Click its row to turn it on.
* **If the note shows a unique ID** (like `Pathoschild.ContentPatcher`), that mod isn't installed. Search for it on the [Browse](browse.md) tab and install it, then turn it on here.

DewDrop doesn't download required mods for you. The mod's Nexus page usually lists what it needs.

## Updates

DewDrop checks for mod updates each time the game starts, using the same service SMAPI uses (smapi.io). You can turn that off in [Settings](settings.md). To check again, click **Check for updates**. It reads **Checking...** while it works, then a message says "Everything's up to date." or how many updates are available.

Mods with a new version get a green **Update to** button, such as **Update to 1.4.0**, and the tab reads **Installed (1 new)**. What clicking it does depends on your Nexus Mods account. Premium installs the update in the game and keeps the mod's settings. Free opens the file on the Nexus Mods website, where you click **Slow download**. If you're logged out, or the update isn't on Nexus Mods, it opens the update's page in your browser. [Updating mods](updating-mods.md) has the details, including what happens on macOS and Linux.

## Uninstall a mod

Uninstalling deletes a mod's folder from your `Mods` folder, settings included: its `config.json` and everything else in the folder go with it. DewDrop waits until the game closes to delete it, so you can change your mind until then.

1. Click the trash can at the right edge of the mod's row. Pointing at it says "Uninstall *name*. Its folder is deleted when the game closes."
2. A dialog asks "Uninstall *name*?" and says what will happen (see below).
3. Click **Uninstall**, or press **Enter**. To keep the mod, click **Cancel**.

A message says "Uninstalled *name*. It's deleted when the game closes."

Under the question, the dialog says one of these:

| Line | When you see it |
| --- | --- |
| "DewDrop turns it off now and deletes its folder, settings included, when the game closes. Until then you can undo it on the Installed tab." | The mod is on in your active profile. |
| "DewDrop deletes its folder, settings included, when the game closes. Until then you can undo it on the Installed tab." | The mod is off already, or uninstalling leaves your profile as it is (see [What happens to your profiles](#what-happens-to-your-profiles)). |

Then it adds any of these that apply:

| Line | When you see it |
| --- | --- |
| "*names* need it and won't work without it." | Mods that are on in your profile require this one, and no other copy of it is left. |
| "The update downloaded for it is thrown away too." | The mod has an update waiting (**Update ready, restart to finish**). |
| "*name* came with SMAPI, so installing SMAPI again puts it back." | It's one of the small mods SMAPI installs, **Console Commands** or **Save Backup**. |
| "Your other copy, in the '*folder*' folder, stays installed." | The mod is [installed twice](#mods-installed-twice). |

### Until the game closes

The mod's row stays on the list, so you can change your mind:

* It has no checkbox, its name is grayed out, and it says **Deleted when the game closes** in orange, next to an **Undo** button.
* It stays in every **Show:** view, and in a search when it matches.
* Clicking the row only reminds you: "*name* is uninstalled. Click Undo to keep it."

Everywhere else, DewDrop treats the mod as gone. For example, Browse and the mod's page no longer show it as installed.

The bottom bar says whether the game still has the mod loaded:

| Status | What it means |
| --- | --- |
| "*name*, which you uninstalled, is still loaded. Restart to finish uninstalling." (orange) | The game is running the mod right now. With more than one, it says "2 mods you uninstalled are still loaded." instead. **Apply & Restart** lights up. |
| "The mods loaded right now match this profile. Uninstalled mods are deleted when the game closes." (gray) | The game didn't load the mod, and nothing else needs a restart. Its folder goes when you quit. |

### Undo

Click **Undo** on the row to keep the mod. If uninstalling turned it off, Undo turns it back on, in the profile it was turned off in, even if you've switched profiles since. A message says what happened:

* "Kept *name*."
* "Kept *name* and turned it back on."
* "Kept *name* and turned it back on in the '*profile*' profile." You've switched to another profile since.
* "Kept *name*. Turn it back on yourself: the '*profile*' profile can't be changed now." For example, that profile was deleted or renamed, or its file couldn't be read.

Installing the same mod again before the game closes, for example from its page on **Browse**, keeps it too: the uninstall is cancelled.

### When the folder is deleted

Nothing is deleted straight away, because the game may have the mod loaded. Mods read their files while you play, not only when the game starts. Content Patcher packs, for example, read many of theirs when a save loads. So DewDrop waits:

1. When the game closes, whether you quit or click **Apply & Restart**, DewDrop moves each uninstalled mod's folder out of your `Mods` folder, into a hidden `.dewdrop-staging` folder beside it. It also removes the mod's link from `Selected Mods`, the folder your profile loads from.
2. The next time the game starts, DewDrop deletes them.

On Windows, a folder can't be moved while the game still has a file in it open. DewDrop's restart script then finishes the job a moment after the game has closed, or DewDrop does at the next start. If it still can't after three starts, DewDrop gives up and shows "Couldn't delete '*folder*' (uninstalled): a file stayed open. Delete it yourself." The SMAPI log has the folder's full path. Close the game, and any program that might have a file in that folder open, then delete it. Until you do, it's listed as an ordinary mod again.

If the game doesn't close normally, for example because it crashed, DewDrop deletes the folder the next time the game starts instead. If that session loads the mod again, it waits until that session closes. Until then the row still has **Undo**, but it doesn't turn the mod back on, so do that yourself.

### What happens to your profiles

* Uninstalling turns the mod off only in your active profile. Your other profiles still list it, which does no harm once it's gone.
* It doesn't turn the mod off at all when another copy of it stays in your `Mods` folder, or when your active profile is read-only.
* If you use Stardrop's profile-specific mod configs, the settings Stardrop saved for the mod in your profiles stay as they are.

### When you can't uninstall

If a mod can't be uninstalled right now, pointing at its trash can says why, and clicking it shows the same message.

| Message | What to do |
| --- | --- |
| "Wait for *title* to finish installing, then try again." | DewDrop is installing a download. Wait until it no longer says **Installing...** on the [Downloads](downloads.md) tab. |
| "*name* is downloading again. Cancel that on the Downloads tab, or wait for it." | A download of this mod is queued or running. Cancel it on the **Downloads** tab, or let it finish. |

DewDrop's own row has no trash can, because DewDrop can't uninstall itself. To remove it, see [Uninstalling DewDrop](../getting-started/uninstalling.md). Copies from a Stardrop collection have none either. The collection owns them, so remove the collection in Stardrop instead.

## Mods installed twice

If two folders in your `Mods` folder hold the same mod, with the same unique ID, both rows say **Installed twice** in red, and **Show: Problems** lists them. When SMAPI loads your `Mods` folder directly, as a normal launch does, it loads neither copy.

Hover over one of the rows to see where the other copy is, such as "Also installed in: ContentPatcher (v2.5.3)". Then click the trash can on the copy you don't want. The dialog says "Your other copy, in the '*folder*' folder, stays installed.", and the mod stays on in your profile, because the other copy is still there.

## Collection profiles

If your active profile came from a Nexus collection in Stardrop, it's read-only. The tab shows "This collection profile is read-only. Duplicate it on the Profiles tab to change it." To change which mods are on, make a copy on the [Profiles](profiles.md) tab and use the copy.

## What the Installed tab can't do yet

* **Install a mod from a file you downloaded.** Unzip it into your `Mods` folder yourself, then click **Rescan**. It shows up here, turned on if **Turn on mods when installed** is on. See [Added or removed a mod by hand?](#added-or-removed-a-mod-by-hand)
* **Sort the list.** It's always sorted by name.

DewDrop finds mods inside sub-folders of `Mods` too. Like SMAPI, it skips folders whose names start with a dot, so renaming a folder to `.MyMod` hides it from both.
