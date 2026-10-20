---
description: "How DewDrop finds new versions of your mods, and how to install them."
icon: arrows-rotate
---

# Updating mods

DewDrop checks for new versions of your mods and puts an **Update to** button on each mod that has one. With a Nexus Mods account, it can download and install most updates for you.

## How DewDrop finds updates

DewDrop asks smapi.io, the same update service SMAPI itself uses. It checks:

* when the game starts, if **Check for updates at startup** is on. It's on by default; see [Settings](settings.md).
* when you click **Check for updates** on the **Installed** tab. The button reads **Checking...** while it works. Then a message says "Everything's up to date." or how many updates are available.

A mod can only be checked if its author said where its updates are published (its "update keys") and gave it a normal version number such as `1.2.3`. Mods from Stardrop collections are never checked, because the collection decides their version. DewDrop checks itself the same way; see [Updating DewDrop](../getting-started/updating-dewdrop.md).

DewDrop sends smapi.io only each mod's ID, version and update keys, plus your SMAPI version, game version and operating system. Nothing about your Nexus Mods account is sent. If a check you started fails, a red message says why, for example "Couldn't reach smapi.io to check for updates." Check your internet connection and try again later.

## See which mods have updates

<figure><img src="../.gitbook/assets/shot-installed.png" alt="The Installed tab of the DewDrop menu, with a search box, a filter button and a Check for updates button above a list of installed mods with checkboxes"><figcaption><p>Updates show on the Installed tab.</p></figcaption></figure>

* The tab's name shows a count, for example **Installed (2 new)**.
* Click the filter button until it reads **Show: Updates** to list only those mods.
* Each of them has a green **Update to** button with the new version number, for example **Update to 2.1.0**.
* On a mod's page in Browse, the bar at the top shows **Update available:** and the new version next to the version you have.

## Install an update

Click the mod's **Update to** button. What happens next depends on your Nexus Mods account. To log in, see [Connecting your Nexus account](../nexus-mods/connecting.md).

{% tabs %}
{% tab title="Premium" %}
1. A message says DewDrop is finding the update.
2. DewDrop picks the file for the new version from the mod's Nexus Mods page, downloads it and installs it. You can watch it on the [Downloads](downloads.md) tab.
3. When it's done, click **Apply & Restart** to load the new version. See [Finish the update](#finish-the-update) below.
{% endtab %}

{% tab title="Free" %}
Free accounts download from the Nexus Mods website. On Windows, DewDrop can pick up that download for you once it handles the website's "Mod manager download" links.

1. Once only: on the **Account** tab, in the **Mod manager links** panel, click **Use DewDrop for these links**. See ["Mod manager download" links](../nexus-mods/mod-manager-links.md).
2. Click **Update to**. Your browser opens the new file on Nexus Mods.
3. On the website, click **Slow download**. If your browser asks whether to open the link, allow it.
4. Within a second or so the download appears on DewDrop's [Downloads](downloads.md) tab, and DewDrop installs it.
5. Click **Apply & Restart** to load the new version.

On macOS and Linux, the website's download can't reach DewDrop, because the link handler is Windows only, and DewDrop's message says so. Use the website's manual download and [update the mod by hand](#update-a-mod-by-hand) instead. If you can copy the `nxm://` link the website opens, you can also paste it into the SMAPI console as `dewdrop_nxm <link>`; see [Console commands](../reference/console-commands.md).
{% endtab %}

{% tab title="Not logged in" %}
DewDrop opens the update's page in your browser. If the mod is on Nexus Mods, a message also says "Log in to Nexus Mods on the Account tab to update in-game."

Log in and click **Update to** again, or download the file yourself and [update the mod by hand](#update-a-mod-by-hand).
{% endtab %}
{% endtabs %}

Some updates aren't on Nexus Mods, for example a release on GitHub. DewDrop then opens that page instead. If the mod also has a Nexus Mods page, a message says "The *mod* update isn't on Nexus Mods; opening where it is." If DewDrop can't tell which Nexus file is the new version, it says so and opens the mod's files on the website. Either way, download the file yourself and [update the mod by hand](#update-a-mod-by-hand).

## Finish the update

On Windows, the game keeps a loaded mod's files open, so DewDrop usually can't replace a mod while you're playing with it. It keeps the new version ready instead. The mod's row on the **Installed** tab says **Update ready, restart to finish**, and the download says "Update ready, finishes when the game restarts".

An update marked **Update ready, restart to finish** goes in once the game has closed, in either of these ways:

* **Apply & Restart.** DewDrop swaps in the new version after the game closes and before it starts again. See [Apply & Restart](apply-and-restart.md).
* **Quitting the game.** A moment after the game closes, DewDrop swaps in the new version. It loads the next time you start the game.

If an update still can't go in, for example because one of its files is open in another program, it keeps waiting and DewDrop tries again next time. Nothing is lost: the note stays until the update is in.

Some updates go in straight away and show **Updated**, for example content packs (mods made only of files, with no code), and most updates on macOS and Linux. Either way, the new version only loads after a restart.

### What the update keeps

DewDrop copies every `config.json` from the old version into the new one, so your settings for the mod stay. It also keeps files the new version doesn't come with in the mod's `data` folder and `.json` files at the top of the mod folder, where many mods keep per-save data. Everything else comes from the new version.

If **Turn on mods when installed** is on, which is the default, updating a mod that was off in your active profile turns it on.

## Update a mod by hand

1. Close the game.
2. Download the new version of the mod.
3. In your `Mods` folder, replace the mod's old folder with the new one. To keep your settings for that mod, copy its old `config.json` into the new folder.
4. Start the game. DewDrop sees the new version. Your profiles remember mods by their ID, so the mod stays on wherever it was on.
