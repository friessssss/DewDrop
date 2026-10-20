---
description: "Something not working, or not sure how something works? Start here."
icon: life-ring
---

# Help

Most problems with DewDrop come down to a handful of causes, and they're all fixable. This section helps you find yours, answers common questions, and shows you how to ask for help when you're stuck.

## Pages in this section

* <img src="../.gitbook/assets/icons/wrench.png" alt="" data-size="line"> [Troubleshooting](troubleshooting.md): a list of problems in plain words, each linking to its fix, from an installer that won't start to a missing Mods button or downloads that never arrive.
* <img src="../.gitbook/assets/icons/question.png" alt="" data-size="line"> [FAQ](faq.md): short answers to common questions, like what the black window next to your game is, how to delete a mod, or whether you need Nexus Mods Premium.
* <img src="../.gitbook/assets/icons/bug.png" alt="" data-size="line"> [Reporting issues](reporting-issues.md): where your logs are, how to share them, and how to open an issue on GitHub.

## Quick checks

These cover most problems:

1. **No Mods button?** Make sure the game started with SMAPI. Usually a SMAPI window opens next to the game when it does. (Not on a Steam Deck, or on Linux with DewDrop's Steam launcher: check the SMAPI log there.) See [No Mods button on the title screen](troubleshooting.md#no-mods-button-on-the-title-screen).
2. **Mods you turned off are back?** A normal launch from Steam or a shortcut loads your whole `Mods` folder. Click **Restart now** when DewDrop asks **Load your profile?**, or open **Mods** and click **Apply & Restart**. With the Steam version of the game, **Start in my profile from Steam** on the **Account** tab makes **Play** start in your profile (on Windows, and on Linux and the Steam Deck with DewDrop's Steam launcher). See [Mods you turned off came back](troubleshooting.md#mods-you-turned-off-came-back).
3. **A new mod is missing?** If you copied it in while the game was running, click **Rescan** on the **Installed** tab. With **Turn on mods when installed** on (the default), DewDrop turns new mods on in your profile, and **Apply & Restart** loads them. See [A mod you copied in is missing or turned off](troubleshooting.md#a-mod-you-copied-in-is-missing-or-turned-off).
4. **A black screen or a terminal window in Game Mode on a Steam Deck?** Steam isn't starting the game with DewDrop's launcher. Run the installer again in Desktop Mode. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md#if-something-goes-wrong).
5. **Looking for DewDrop during a save?** The menu only opens from the title screen.

{% hint style="info" %}
When you ask for help, include your SMAPI log. [Reporting issues](reporting-issues.md#find-your-smapi-log) shows where it is and how to share it.
{% endhint %}
