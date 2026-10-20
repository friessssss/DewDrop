---
description: "How to find your logs, share them, and report a problem on GitHub."
icon: bug
---

# Reporting issues

Found a bug, or stuck on something [Troubleshooting](troubleshooting.md) and the [FAQ](faq.md) don't cover? Open an issue on GitHub. A log from the session where things went wrong is the most useful thing you can send, so this page starts there.

## Find your SMAPI log

SMAPI keeps a log of everything that happens while the game runs, and DewDrop writes its own messages and errors there too. Apart from a small [restart log on Windows](#the-restart-log-on-windows), DewDrop has no log file of its own.

{% tabs %}
{% tab title="Windows" %}
1. Press **Win+R**.
2. Paste `%AppData%\StardewValley\ErrorLogs` and press **Enter**.

File Explorer opens `C:\Users\<you>\AppData\Roaming\StardewValley\ErrorLogs`.
{% endtab %}

{% tab title="macOS" %}
1. In Finder, choose **Go** > **Go to Folder...** (or press **Shift+Cmd+G**).
2. Type `~/.config/StardewValley/ErrorLogs` and press **Return**.
{% endtab %}

{% tab title="Linux" %}
Open `~/.config/StardewValley/ErrorLogs` in your file manager. The `.config` folder is hidden; in most file managers, **Ctrl+H** shows hidden folders.
{% endtab %}
{% endtabs %}

In that folder:

* `SMAPI-latest.txt` is the log of your most recent session.
* `SMAPI-crash.txt` is there if the game crashed. If you have it, send that one.

{% hint style="info" %}
SMAPI starts a fresh log every time the game starts, and that includes every restart by DewDrop, such as **Apply & Restart** or **Restart now**. So make the problem happen, close the game, and grab the log before you start the game again.
{% endhint %}

**Tip:** before you close the game, type `dewdrop_status` in the SMAPI console window and press **Enter**. What DewDrop prints (your mod folders, active profile, Steam launch option and Nexus login) goes into the log too. See [Console commands](../reference/console-commands.md).

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
Press **Win+R**, paste `%TEMP%`, and press **Enter**. Look for `DewDrop-installer.log`. It has everything the installer window printed.

SMAPI's own installer runs in a separate window, so its messages aren't in this log. If that window showed an error, take a screenshot of it. If the installer window said "Oops! The installer couldn't start", the installer never ran and there's no new log: take a screenshot of that window instead.
{% endtab %}

{% tab title="macOS" %}
The log is `DewDrop-installer.log` in your temporary folder. To open that folder, type `open "$TMPDIR"` in **Terminal** and press **Return**.
{% endtab %}

{% tab title="Linux" %}
The log is usually `/tmp/DewDrop-installer.log` (or `$TMPDIR/DewDrop-installer.log` if your system sets `TMPDIR`).
{% endtab %}
{% endtabs %}

On macOS and Linux, the log only records the installer's steps, warnings and errors. Your answers to its questions and SMAPI's own installer messages aren't in it, so before you close the window, copy all of its text (or take a screenshot) and send that too.

### The restart log on Windows

If the game closed after **Apply & Restart** and didn't come back, an update didn't finish, or Steam's launch option wasn't changed, send `relaunch.log` too. Press **Win+R**, paste `%AppData%\Stardrop\Data\Cache\DewDrop`, and press **Enter**.

DewDrop empties this file each time its helper runs after the game closes, and only writes to it when it finished an update, changed Steam's launch option, or something went wrong. It's short, so you can paste its text straight into your issue. macOS and Linux don't have a restart log. See [The game closed and never came back](troubleshooting.md#the-game-closed-and-never-came-back).

## Open an issue

1. Go to DewDrop's [issues page on GitHub](https://github.com/friessssss/DewDrop/issues). You'll need a free GitHub account to post.
2. Search the existing issues first. If someone has already reported your problem, add what you know there.
3. Otherwise, click **New issue**, give it a short title that says what went wrong, and fill it in.

### What to include

* **What happened, and what you expected** to happen instead.
* **Steps to make it happen again**, as exactly as you can.
* **Your system:** Windows, macOS or Linux, and where you got the game (Steam, GOG or the Xbox app).
* **How you started the game:** from Steam (and whether **Start in my profile from Steam** is on), a desktop shortcut, the Xbox app, Stardrop, or a restart by DewDrop (**Apply & Restart** or **Restart now**).
* **Your versions** of DewDrop, SMAPI and Stardew Valley. The SMAPI log lists all three, so a log link covers this.
* **Your SMAPI log link**, plus the installer log or `relaunch.log` if they're relevant.
* **Screenshots** of any message you saw.
* **Other tools you use,** such as Stardrop or Vortex.
* **Your Nexus Mods account type** (Free or Premium), if the problem is about downloads.

{% hint style="warning" %}
Never post your Nexus Mods API key, your `Settings.json`, or `Cache/Notion.json` from DewDrop's data folder. Together they hold your Nexus login.

The SMAPI log doesn't contain your API key, but file paths in it often include your computer user name, and it shows your Nexus Mods user name if you're logged in. If you used the `dewdrop_nxm` command, or DewDrop couldn't use a download link the website sent, the log also contains that link, including your Nexus user ID. The link itself expires on its own.
{% endhint %}

## Ideas and other feedback

Ideas for DewDrop are welcome too. Open an issue and describe what you'd like it to do. If you'd like to help with code, docs or art, see [Contributing](../contributing/README.md).
