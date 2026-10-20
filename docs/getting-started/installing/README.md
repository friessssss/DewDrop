---
description: "Two ways to install DewDrop: an installer that does everything for you, or drag and drop."
icon: download
---

# Installing DewDrop

There are two ways to install DewDrop. Both end in the same place: SMAPI installed, and a `DewDrop` folder inside your game's `Mods` folder.

<figure><img src="../../.gitbook/assets/diagram-install-paths.png" alt="The two ways to install DewDrop side by side: the installer, which sets up SMAPI and DewDrop for you, and drag and drop, where you put the DewDrop folder into the Mods folder yourself"><figcaption><p>Two ways in, same result.</p></figcaption></figure>

## Which one should I pick?

**New to mods? Use the installer.** If you already have SMAPI and know where your `Mods` folder is, dragging DewDrop in yourself takes a minute.

| | The installer | By hand |
| --- | --- | --- |
| What you download | `DewDrop-<version>-installer.zip` | `DewDrop-<version>.zip` |
| Installs or updates SMAPI | Yes, if it's missing or out of date | No. Install SMAPI yourself first. |
| Finds your `Mods` folder | Yes | You find it |
| Sets up Steam to start SMAPI (Windows) | Offers to do it for you | You do it yourself, once |
| Keeps your DewDrop settings when updating | Yes | Yes, if you keep `config.json` |
| Good for | First-timers, and anyone who wants it done for them | Players who already use mods |

Both downloads are on the [latest release page](https://github.com/friessssss/DewDrop/releases/latest), under **Assets**. You don't need the **Source code** downloads. They're for developers.

## Pages in this section

* [Install with the installer](installer.md): step by step, and what to do if your computer warns you about the file.
* [Install by hand](manual.md): where to find your `Mods` folder on each system, and the mistakes to avoid.
* [About SMAPI](smapi.md): what SMAPI is, how to install or update it yourself, and how to make Steam, GOG Galaxy or the Xbox app start it.

{% hint style="info" %}
Keep exactly one copy of DewDrop in your `Mods` folder. If SMAPI finds two mods with the same ID, it loads neither of them, and the **Mods** button disappears. The installer finds old copies and offers to turn them off for you.
{% endhint %}

## After you install

* Go to [Your first launch](../first-launch.md) to open the **Mods** menu for the first time.
* Already have DewDrop? Installing a newer version works the same way. See [Updating DewDrop](../updating-dewdrop.md).
* Use Stardrop too? Read [Using DewDrop with Stardrop](../../stardrop.md) before your first launch.
