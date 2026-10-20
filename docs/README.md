---
description: "A mod manager that lives inside Stardew Valley, one click from the title screen."
icon: droplet
---

# Welcome to DewDrop

<figure><img src=".gitbook/assets/banner.png" alt="The DewDrop banner: the dew-drop logo and name over a pixel-art meadow"></figure>

DewDrop is a mod manager that lives inside Stardew Valley. It adds a **Mods** button to the title screen, where you can turn mods on and off, find and install mods from the Nexus Mods website, and keep different sets of mods in profiles. It runs on SMAPI, the free tool that loads Stardew Valley mods. The installer sets up both for you.

<figure><img src=".gitbook/assets/demo-tour.png" alt="Animation: the Mods button on the Stardew Valley title screen, then DewDrop's Installed, Browse, mod page, Profiles and Downloads tabs, then the Apply &#x26; Restart dialog"><figcaption><p>From the title screen to your mods in one click.</p></figcaption></figure>

## What you can do

* <img src=".gitbook/assets/icons/installed.png" alt="" data-size="line"> **Installed**: turn mods on and off, search your list, spot missing requirements, and update and uninstall mods. See [Managing your mods](guides/installed.md).
* <img src=".gitbook/assets/icons/browse.png" alt="" data-size="line"> **Browse**: search and sort Nexus Mods, see which mods you already have or can update, and install a file from a mod's page. Installing needs a free or Premium Nexus Mods account. See [Browsing Nexus Mods](guides/browse.md).
* <img src=".gitbook/assets/icons/profiles.png" alt="" data-size="line"> **Profiles**: create, switch, duplicate, rename and delete profiles. See [Profiles](guides/profiles.md).
* <img src=".gitbook/assets/icons/downloads.png" alt="" data-size="line"> **Downloads**: follow your downloads, cancel them, or try a failed one again. See [Downloads](guides/downloads.md).
* <img src=".gitbook/assets/icons/account.png" alt="" data-size="line"> **Account**: log in to Nexus Mods, let the website's download buttons send files to DewDrop (Windows only), and change settings. See [Connecting your Nexus account](nexus-mods/connecting.md).
* <img src=".gitbook/assets/icons/restart.png" alt="" data-size="line"> **Apply & Restart**: restart the game with exactly the mods in your profile. With the Steam version on Windows or a Steam Deck, Steam's **Play** button can start the game in your profile instead. See [Apply & Restart](guides/apply-and-restart.md).

{% hint style="info" %}
DewDrop is built and tested on Windows. It's built to work on macOS and Linux too, but it hasn't been tested much there yet, and some Nexus Mods download features are Windows only. Steam Deck support is new and being tested. See [Platform support](reference/platform-support.md).

Trying it is safe: the installer never deletes your mods, saves or settings, and you can [remove DewDrop](getting-started/uninstalling.md) again in a few steps. If you try it on macOS, Linux or a Steam Deck, please [tell us how it went](https://github.com/friessssss/DewDrop/issues).
{% endhint %}

## What's new in 0.2.0

* **Uninstall mods** with the trash can on the **Installed** tab, or the **Uninstall** button on a mod's page. Changed your mind? Click **Undo** any time before the game closes. See [Uninstall a mod](guides/installed.md#uninstall-a-mod).
* **See what you have while browsing.** **Browse** recognizes more of the mods you have and tags their cards **Installed** or **Update**, and a mod's page marks the file you have. See [Browsing Nexus Mods](guides/browse.md) and [A mod's page and installing files](guides/mod-page.md).
* **One-line installers.** Paste one line into PowerShell on Windows, or into Terminal on macOS and Linux. See [Install with the installer](getting-started/installing/installer.md).
* **Apple Silicon Macs.** The installer offers to install Rosetta, which SMAPI's installer needs. See [Running the installer on your computer](getting-started/installing/installer.md#running-the-installer-on-your-computer).
* **Steam Deck support,** new and being tested. Set DewDrop up once in Desktop Mode, then install, update and switch mods in Game Mode. See [DewDrop on the Steam Deck](getting-started/steam-deck.md).

{% hint style="info" %}
0.2.0 isn't released yet, so the latest download is still 0.1.0, which has none of these. Until 0.2.0 is out, the one-line installers show a 404 error: use the [installer download](getting-started/installing/installer.md#or-download-it-five-steps) instead. [DewDrop's GitHub page](https://github.com/friessssss/DewDrop#whats-new) shows where each change stands.
{% endhint %}

## Get started

<table data-view="cards">
  <thead>
    <tr>
      <th></th>
      <th></th>
      <th data-hidden data-card-target data-type="content-ref"></th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><strong>Start here</strong></td>
      <td>The quick start: install DewDrop, open the Mods menu and load your first profile in a few minutes.</td>
      <td><a href="quick-start.md">quick-start.md</a></td>
    </tr>
    <tr>
      <td><strong>Install</strong></td>
      <td>More detail on installing, with the installer or by dragging DewDrop into your Mods folder yourself.</td>
      <td><a href="getting-started/installing/README.md">getting-started/installing/README.md</a></td>
    </tr>
    <tr>
      <td><strong>Steam Deck</strong></td>
      <td>Set up once in Desktop Mode, then manage your mods from the title screen in Game Mode.</td>
      <td><a href="getting-started/steam-deck.md">getting-started/steam-deck.md</a></td>
    </tr>
    <tr>
      <td><strong>Help</strong></td>
      <td>Fixes for common problems, answers to common questions, and how to report a bug.</td>
      <td><a href="help/README.md">help/README.md</a></td>
    </tr>
  </tbody>
</table>

## About DewDrop

DewDrop is free and open source under the GPL-3.0 license. You can download it from the [latest release on GitHub](https://github.com/friessssss/DewDrop/releases/latest).

DewDrop shares its profiles and Nexus Mods login with [Stardrop](stardrop.md), another mod manager, so you can use both. You don't need Stardrop.

DewDrop began as a fork of Stardrop's code and is not affiliated with or endorsed by Stardrop's authors. Thank you to [Floogen](https://github.com/Floogen) and the Stardrop contributors, and to [Pathoschild](https://smapi.io/) for SMAPI.
