---
description: "Log in to Nexus Mods with your personal API key, and learn what DewDrop does with it."
icon: key
---

# Connecting your Nexus account

To download mods inside the game, DewDrop needs to know who you are on Nexus Mods. You log in once with your **personal API key**, a long code from your Nexus Mods account page. A free account works. [Free vs Premium downloads](free-vs-premium.md) explains what Premium adds.

You need:

* A Nexus Mods account. You can [sign up for free](https://www.nexusmods.com/).
* A keyboard, to paste the key with **Ctrl+V**. On a Steam Deck, the installer can take the key for you instead. See [On a Steam Deck](#on-a-steam-deck).
* The game on the title screen. The DewDrop menu only opens from there.

## Log in with your API key

{% stepper %}
{% step %}
### Open the Account tab

On the title screen, click **Mods**, then the **Account** tab. You log in on the **Nexus Mods** panel on the left.
{% endstep %}

{% step %}
### Copy your key from Nexus Mods

Click **Get my API key**. Your browser opens the API page of your Nexus Mods account. Sign in to the website if it asks.

Copy your **Personal API Key** from that page. If you don't have one yet, the page lets you create it.
{% endstep %}

{% step %}
### Paste it into DewDrop

Go back to the game. Click the box that says **Paste your API key (Ctrl+V)** and press **Ctrl+V**. The key shows as stars, so nobody watching your screen can read it.
{% endstep %}

{% step %}
### Log in

Click **Log in**, or press **Enter**. The button says **Logging in...** while Nexus Mods checks the key.
{% endstep %}
{% endstepper %}

When it works, you hear a coin sound and see "Logged in to Nexus Mods as *name*." That's it!

## What you see when you're logged in

<figure><img src="../.gitbook/assets/shot-account.png" alt="DewDrop's Account tab with three panels side by side: Nexus Mods, Mod manager links and Settings."><figcaption><p>The Account tab. You log in on the Nexus Mods panel on the left.</p></figcaption></figure>

The **Nexus Mods** panel shows:

* "Logged in as *name*", with **Premium** and a gold star, or **Free account**.
* What that means for downloads: "Install buttons download directly." for Premium, or “Downloads go through the website's "Slow download" button.” for a free account.
* "API requests left today:" and a number. Nexus Mods limits how many requests an app can make for you each day.

DewDrop saves your login and logs you in again each time you start the game.

On Windows, a download you started on the website while you were logged out (with [mod manager links](mod-manager-links.md) turned on) waits for you. The panel says "1 download from the website is waiting for you to log in.", and the download starts as soon as you log in.

### Your login is shared with Stardrop

DewDrop saves the key where Stardrop does. That's why, before you log in, the panel says "DewDrop uses your personal Nexus Mods API key. It's shared with Stardrop."

* If you already logged in to Nexus Mods in Stardrop, DewDrop is logged in from the start.
* Logging in or out in one does the same in the other. Stardrop notices the next time it starts.

See [Using DewDrop with Stardrop](../stardrop.md).

{% hint style="info" %}
**Why not a "Log in with browser" button?** Nexus Mods' one-click browser login only works for apps that Nexus Mods has registered. DewDrop isn't registered yet, so for now you log in with your API key.
{% endhint %}

## On a Steam Deck

Typing a key this long on the on-screen keyboard is slow, so the installer offers to take it while you [set DewDrop up in Desktop Mode](../getting-started/steam-deck.md). When it asks, open [your Nexus Mods account's API page](https://www.nexusmods.com/users/myaccount?tab=api) in the browser, copy the **Personal API Key**, paste it into Konsole with **Ctrl+Shift+V** at "API key (or just press Enter to skip):", and press **Enter**. It doesn't ask if you're already logged in, for example through Stardrop.

The next time the game starts, DewDrop logs in with that key and saves it like one you typed in. A message in the **Mods** menu says how it went:

| Message | What it means |
| --- | --- |
| "Logged in to Nexus Mods as *name* with the key from the installer." | You're logged in. |
| "Nexus Mods didn't accept the key from the installer. Log in on the Account tab." | The key was wrong or only partly copied. DewDrop threw it away, and any login you had before still stands. |
| "Couldn't reach Nexus Mods: ... DewDrop tries the key from the installer again the next time the game starts." | Nexus Mods didn't answer, or was busy. DewDrop keeps the key and tries again. |
| "Logged in, but DewDrop couldn't save the key for next time. The SMAPI log says why." | You're logged in for now. DewDrop keeps the key from the installer and tries to save it again the next time the game starts. |

To log in later, run the installer again in Desktop Mode: while you're logged out, it asks for the key again. You can also log in on the **Account** tab in Game Mode. There, **Get my API key** opens the page in Steam's overlay, and tapping the key box brings up Steam's keyboard.

## If logging in doesn't work

| Message | What to do |
| --- | --- |
| "Paste your API key first. 'Get my API key' shows where to find it." | The box was empty. Paste your key, then click **Log in**. |
| "Nexus didn't accept that API key." | The key is wrong or only partly copied. Copy the whole key again. To start over with an empty box, close the DewDrop menu and open it again. |
| "Couldn't reach Nexus Mods: ..." | DewDrop didn't get an answer. Check your internet connection. If the message says Nexus Mods is limiting requests or didn't answer, wait a few minutes and try again. |
| "Logged in, but DewDrop couldn't save the key for next time. The SMAPI log says why." | You're logged in for now, but you'll have to log in again next time. [Reporting issues](../help/reporting-issues.md) shows how to find and share the SMAPI log. |
| "Stardrop's Settings.json couldn't be read, so your saved login didn't load and a new one won't be saved." | Another program may have had the file open. Close the game and start it again. If it keeps happening, see [Troubleshooting](../help/troubleshooting.md#a-red-message-at-the-bottom-of-the-menu). |

## Logging out

1. On the **Account** tab, click **Log out** in the **Nexus Mods** panel.
2. DewDrop asks "Log out of Nexus Mods as *name*?" and reminds you: "Stardrop uses the same saved key, so it's logged out too."
3. Click **Log out** to confirm, or **Cancel** to stay logged in.

DewDrop and Stardrop both forget the saved key. A key from the installer that's still waiting is deleted too. Logging out only removes the key from your computer: the key itself keeps working on Nexus Mods.

## How safe is my key?

Your API key lets an app use Nexus Mods as you, for example to download files. Here's exactly what DewDrop does with it.

**Where it's stored.** DewDrop stores the key the way Stardrop does, so the two can share it:

* An encrypted copy goes in `Settings.json`.
* The key that unlocks it sits right next to it, in `Cache/Notion.json`.

Both files are in DewDrop's data folder, which it shares with Stardrop: `C:\Users\<you>\AppData\Roaming\Stardrop\Data` on Windows, or `~/.config/Stardrop/Data` on macOS, Linux and the Steam Deck. See [Where DewDrop keeps files](../reference/file-locations.md).

DewDrop's own code calls this "obfuscation, not protection". It keeps the key out of plain sight, but anyone, or any program, that can read both files can get your key back. When you log out, the encrypted copy is removed from `Settings.json`. `Notion.json` stays, but it doesn't hold your key on its own.

**On a Steam Deck,** a key you give the installer waits in `Cache/DewDrop/nexus-key.txt`, as plain text that only your user account can read. DewDrop deletes the file once it has saved the key the usual way, or when Nexus Mods turns the key down. Logging out, or logging in with a different key, deletes it too.

**Where it's sent.** Only to Nexus Mods (`api.nexusmods.com`): to check it when you log in, to get download links, and with DewDrop's other Nexus Mods requests (searches, mod pages and file lists) while you're logged in. It isn't sent to smapi.io or anywhere else, and DewDrop doesn't write it to the SMAPI log.

{% hint style="warning" %}
**Treat your API key like a password.** Don't share `Settings.json`, `Notion.json` or `nexus-key.txt`, and don't zip up and post your whole `Stardrop` data folder when you ask for help. A SMAPI log is usually what helpers need (see [Reporting issues](../help/reporting-issues.md)). If you think your key has leaked, log out, then manage the key on your Nexus Mods account page (**Get my API key** opens it).
{% endhint %}
