---
description: "Log in to Nexus Mods with your personal API key, and learn what DewDrop does with it."
icon: key
---

# Connecting your Nexus account

To download mods inside the game, DewDrop needs to know who you are on Nexus Mods. You log in once with your **personal API key**, a long code from your Nexus Mods account page. A free account works fine. [Free vs Premium downloads](free-vs-premium.md) explains what changes with Premium.

Before you start, you need:

* A Nexus Mods account. If you don't have one, you can sign up for free at [nexusmods.com](https://www.nexusmods.com/).
* A keyboard. The key is too long to type, so you paste it with **Ctrl+V**.
* The game open on the title screen. The DewDrop menu only opens from there.

## Log in with your API key

{% stepper %}
{% step %}
### Open the Account tab

On the title screen, click **Mods**, then the **Account** tab. The **Nexus Mods** panel is on the left.
{% endstep %}

{% step %}
### Copy your key from Nexus Mods

Click **Get my API key**. Your browser opens the API page of your Nexus Mods account. Sign in to the website if it asks.

Find your **Personal API Key** on that page and copy it. If you don't have one yet, the page lets you create it.
{% endstep %}

{% step %}
### Paste it into DewDrop

Go back to the game. Click the box that says **Paste your API key (Ctrl+V)** and press **Ctrl+V**. The key shows as stars, so nobody watching your screen can read it.
{% endstep %}

{% step %}
### Log in

Click **Log in**, or press **Enter**. The button reads **Logging in...** while DewDrop checks the key with Nexus Mods.
{% endstep %}
{% endstepper %}

When it works, you hear a coin sound and see "Logged in to Nexus Mods as" followed by your user name. That's it!

## What you see when you're logged in

<figure><img src="../.gitbook/assets/shot-account.png" alt="DewDrop's Account tab with three panels side by side: Nexus Mods, Mod manager links and Settings."><figcaption><p>The Account tab. You log in on the Nexus Mods panel on the left.</p></figcaption></figure>

The **Nexus Mods** panel now shows:

* "Logged in as" and your Nexus Mods user name.
* **Premium** with a gold star, or **Free account**.
* What that means for downloads: "Install buttons download directly." for Premium, or “Downloads go through the website's "Slow download" button.” for a free account.
* "API requests left today:" and a number. Nexus Mods limits how many requests an app can make for you each day, and this is how many you have left.

DewDrop saves your login. Each time you start the game, it logs you in again on its own.

On Windows, if you clicked a website download button while you were logged out (with [mod manager links](mod-manager-links.md) turned on), DewDrop kept it waiting. It starts as soon as you log in.

### Your login is shared with Stardrop

DewDrop saves the key in the same place as Stardrop, so before you log in the panel says "DewDrop uses your personal Nexus Mods API key. It's shared with Stardrop." Logging in or out in one also logs the other in or out; Stardrop notices the next time it starts. If you already logged in to Nexus Mods in Stardrop, DewDrop is logged in from the start. See [Using DewDrop with Stardrop](../stardrop.md).

{% hint style="info" %}
**Why not a "Log in with browser" button?** Nexus Mods' one-click browser login only works for apps that Nexus Mods has registered. DewDrop isn't registered yet, so pasting your API key is the only way to log in for now.
{% endhint %}

## If logging in doesn't work

| Message | What to do |
| --- | --- |
| "Paste your API key first. 'Get my API key' shows where to find it." | The box was empty. Paste your key, then click **Log in**. |
| "Nexus didn't accept that API key." | The key is wrong or only partly copied. Copy the whole key again. To start over with an empty box, close the DewDrop menu and open it again. |
| "Couldn't reach Nexus Mods: ..." | DewDrop didn't get an answer. Check your internet connection. If the message says Nexus Mods is limiting requests or didn't answer, wait a few minutes and try again. |
| "Logged in, but DewDrop couldn't save the key for next time. The SMAPI log says why." | You're logged in for now, but you'll have to log in again next time. [Reporting issues](../help/reporting-issues.md) shows how to find and share the SMAPI log. |
| "Stardrop's Settings.json couldn't be read, so your saved login didn't load and a new one won't be saved." | Another program may have had the file open. Close the game and start it again. If it keeps happening, see [Troubleshooting](../help/troubleshooting.md). |

## Logging out

1. On the **Account** tab, click **Log out** in the **Nexus Mods** panel.
2. DewDrop asks "Log out of Nexus Mods as" your name, and reminds you: "Stardrop uses the same saved key, so it's logged out too."
3. Click **Log out** to confirm, or **Cancel** to stay logged in.

DewDrop forgets the saved key, and Stardrop does too, because they share it. Logging out only removes the key from your computer. The key itself keeps working on Nexus Mods.

## How safe is my key?

Your API key lets an app use Nexus Mods as you, for example to download files. Here's exactly what DewDrop does with it.

**Where it's stored.** DewDrop stores the key the same way Stardrop does, so the two can share it:

* An encrypted copy goes in `Settings.json`.
* The key that unlocks it sits right next to it, in `Cache/Notion.json`.

Both files are in Stardrop's data folder: `C:\Users\<you>\AppData\Roaming\Stardrop\Data` on Windows, or `~/.config/Stardrop/Data` on macOS and Linux. See [Where DewDrop keeps files](../reference/file-locations.md).

DewDrop's own code calls this "obfuscation, not protection". It keeps the key out of plain sight, but anyone, or any program, that can read both files can get your key back. When you log out, the encrypted copy is removed from `Settings.json`. `Notion.json` stays, but it doesn't hold your key on its own.

**Where it's sent.** Only to Nexus Mods (`api.nexusmods.com`): to check it when you log in, to get download links, and with DewDrop's other Nexus Mods requests (searches, mod pages and file lists) while you're logged in. It isn't sent to smapi.io or anywhere else, and DewDrop doesn't write it to the SMAPI log.

{% hint style="warning" %}
**Treat your API key like a password.** Don't share `Settings.json` or `Notion.json`, and don't zip up and post your whole `Stardrop\Data` folder when you ask for help. A SMAPI log is usually what helpers need (see [Reporting issues](../help/reporting-issues.md)). If you think your key has leaked, log out and manage the key from your Nexus Mods account page (**Get my API key** opens it).
{% endhint %}
