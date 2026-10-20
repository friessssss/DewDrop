---
description: "How to find your logs, share them, and report a problem on GitHub."
icon: bug
---

# Reporting issues

Found a bug, or stuck on something [Troubleshooting](troubleshooting.md) and the [FAQ](faq.md) don't cover? Open an issue on GitHub. A log from the session where things went wrong is the most useful thing you can send, so this page starts there.

## Find your SMAPI log

SMAPI keeps a log of everything that happens while the game runs, and DewDrop writes its own messages and errors there too. Apart from a few small helper logs ([below](#other-logs-that-can-help)), DewDrop has no log file of its own.

{% tabs %}
{% tab title="Windows" %}
1. Press **Win+R**.
2. Paste this and press **Enter**:

   ```text
   %AppData%\StardewValley\ErrorLogs
   ```

File Explorer opens `C:\Users\<you>\AppData\Roaming\StardewValley\ErrorLogs`.
{% endtab %}

{% tab title="macOS" %}
1. In Finder, choose **Go** > **Go to Folder...** (or press **Shift+Cmd+G**).
2. Paste this and press **Return**:

   ```text
   ~/.config/StardewValley/ErrorLogs
   ```
{% endtab %}

{% tab title="Linux and Steam Deck" %}
Open this folder in your file manager (on a Steam Deck, in Desktop Mode, with **Dolphin**):

```text
~/.config/StardewValley/ErrorLogs
```

The `.config` folder is hidden. In most file managers, including Dolphin, **Ctrl+H** shows hidden folders.
{% endtab %}
{% endtabs %}

In that folder:

* `SMAPI-latest.txt` is the log of your most recent session.
* `SMAPI-crash.txt` is there if the game crashed. If you have it, send that one.

{% hint style="info" %}
SMAPI starts a fresh log every time the game starts, and that includes every restart by DewDrop, such as **Apply & Restart** or **Restart now**. So make the problem happen, close the game, and grab the log before you start the game again.
{% endhint %}

**Tip:** if you have a SMAPI console window, type this in it before you close the game, and press **Enter**:

```text
dewdrop_status
```

What DewDrop prints (your mod folders, active profile, **Start in my profile from Steam** with Steam's launch option, and Nexus login) goes into the log too. See [Console commands](../reference/console-commands.md). On a Steam Deck, or on Linux with DewDrop's Steam launcher, there's no console to type in, but the log still says whether DewDrop saw a Steam Deck, Game Mode and its launcher.

## Share your SMAPI log

Logs are long, so share a link instead of pasting the text:

1. Go to [smapi.io/log](https://smapi.io/log).
2. Drop your log file onto the page, or paste its text, and upload it.
3. Copy the link it gives you. You'll paste it into your issue.

## Other logs that can help

### The installer log

If the problem is with the installer, send its log too. The installer replaces it every time it runs, and at the end it prints where it saved it ("Log saved to ...").

{% tabs %}
{% tab title="Windows" %}
Press **Win+R**, paste this and press **Enter**:

```text
%TEMP%
```

Look for `DewDrop-installer.log`. It has everything the installer window printed.

SMAPI's own installer runs in a separate window, so its messages aren't in this log. If that window showed an error, take a screenshot of it.
{% endtab %}

{% tab title="macOS" %}
The log is `DewDrop-installer.log` in your temporary folder. To open that folder, paste this into **Terminal** and press **Return**:

```bash
open "$TMPDIR"
```
{% endtab %}

{% tab title="Linux and Steam Deck" %}
The log is usually here:

```text
/tmp/DewDrop-installer.log
```

If your system sets `TMPDIR`, it's `$TMPDIR/DewDrop-installer.log` instead.
{% endtab %}
{% endtabs %}

On macOS, Linux and the Steam Deck, the log only records the installer's steps, warnings and errors. Your answers to its questions and SMAPI's own installer messages aren't in it, so before you close the window, copy all of its text (or take a screenshot) and send that too.

If the installer never got going, there's no new log. That's the case when the zip's installer window says "Oops! The installer couldn't start", or when the one-line install command says "Couldn't download the DewDrop installer from ...". Send a screenshot or a copy of that message instead.

### The restart log (Windows)

Send `relaunch.log` if the game closed after **Apply & Restart** and didn't come back, a mod update or uninstall didn't finish, or Steam's launch option wasn't changed. To open its folder, press **Win+R**, paste this and press **Enter**:

```text
%AppData%\Stardrop\Data\Cache\DewDrop
```

DewDrop's helper empties this file each time it runs after the game closes. It only writes to it when it finished a mod update, deleted an uninstalled mod, changed Steam's launch option, or something went wrong. It's short, so you can paste its text straight into your issue. See [The game closed and never came back](troubleshooting.md#the-game-closed-and-never-came-back).

### The Steam launcher log (Linux and Steam Deck)

When Steam starts the game with DewDrop's Steam launcher (on a Steam Deck, or on Linux after the installer's `--steam-setup`), the launcher writes `steam-launch.log`. Each time Steam or a restart starts the game, it adds a line saying which folder SMAPI loaded and why. Send it if the game started without your profile or without mods, or didn't come back after **Apply & Restart**. It's here:

```text
~/.config/Stardrop/Data/Cache/DewDrop/steam-launch.log
```

It keeps only the latest launches, so it's short enough to paste into your issue. [Where DewDrop keeps files](../reference/file-locations.md#logs) explains its lines, and [DewDrop on the Steam Deck](../getting-started/steam-deck.md#if-something-goes-wrong) has fixes.

macOS, and Linux without DewDrop's Steam launcher, have neither the restart log nor the Steam launcher log.

## Open an issue

1. Go to DewDrop's [issues page on GitHub](https://github.com/friessssss/DewDrop/issues). You'll need a free GitHub account to post.
2. Search the existing issues first. If someone has already reported your problem, add what you know there.
3. Otherwise, click **New issue**, choose **Bug report**, and fill in the form with a short title that says what went wrong.

### What to include

The form asks for most of this:

* **What happened, and what you expected** to happen instead.
* **Steps to make it happen again**, as exactly as you can.
* **Your system:** Windows, macOS, Linux or Steam Deck, and where you got the game (Steam, GOG or the Xbox app).
* **How you started the game:** from Steam (and whether **Start in my profile from Steam** is on), a desktop shortcut, the Xbox app, Stardrop, or a restart by DewDrop (**Apply & Restart** or **Restart now**).
* **Your versions** of DewDrop, SMAPI and Stardew Valley. The SMAPI log lists all three, so a log link covers this.
* **Your SMAPI log link**, plus the installer log, `relaunch.log` or `steam-launch.log` if they're relevant.
* **Screenshots** of any message you saw.
* **Other tools you use,** such as Stardrop or Vortex.
* **Your Nexus Mods account type** (Free or Premium), if the problem is about downloads.

{% hint style="warning" %}
Never post your Nexus Mods API key, or these files from DewDrop's data folder: `Settings.json` and `Cache/Notion.json`, which together hold your Nexus login, and `Cache/DewDrop/nexus-key.txt` (Steam Deck), which holds a key from the installer.

The SMAPI log doesn't contain your API key, but file paths in it often include your computer user name, and it shows your Nexus Mods user name if you're logged in. If you used the `dewdrop_nxm` command, or DewDrop couldn't use a download link the website sent, the log also contains that link, including your Nexus user ID. The link itself expires on its own.
{% endhint %}

## Ideas and other feedback

Ideas for DewDrop are welcome too. Click **New issue**, choose **Feature request**, and describe what you'd like it to do. If you'd like to help with code, docs or art, see [Contributing](../contributing/README.md).
