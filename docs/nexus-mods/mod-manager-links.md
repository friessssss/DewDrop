---
description: "Let the website's Mod manager download button send files to DewDrop on Windows, and undo it safely."
icon: link
---

# "Mod manager download" links

On the Nexus Mods website, the **Mod manager download** button, and the **Slow download** button that free accounts click after it, don't save a file in your browser. They open a special `nxm://` link, and Windows hands that link to whichever mod manager signed up for it. DewDrop can be that program, so a download you start on the website lands in your game.

You need this only if both are true:

* You have a **free** Nexus Mods account. Premium downloads go straight into the game without it (see [Free vs Premium downloads](free-vs-premium.md)).
* You play on **Windows**. On macOS, Linux and the Steam Deck, the **Mod manager links** panel says "Only available on Windows for now." See [Free accounts on macOS and Linux](free-vs-premium.md#free-accounts-on-macos-and-linux) for what to do instead.

## Turn it on

1. On the title screen, click **Mods**, then the **Account** tab.
2. Look at the middle panel, **Mod manager links**. It tells you which program opens these links right now.
3. Click **Use DewDrop for these links** (in a small window it says **Use DewDrop**).
4. DewDrop asks "Let DewDrop open the website's Mod Manager Download (nxm://) links?" and names the program it replaces, for example "This replaces Stardrop, which opens them now." Click **Use DewDrop**.
5. You see "DewDrop now opens Mod Manager Download links.", and the panel says "DewDrop handles these links." That's it!

If you have a free account and haven't done this yet, the **Downloads** tab shows a **Set up** button that brings you here.

## What the panel tells you

The panel checks again every time you open the **Account** tab.

| Status | What it means |
| --- | --- |
| "DewDrop handles these links." | You're all set. |
| "Nothing handles them yet." | No mod manager has the links. Click **Use DewDrop**. |
| "Stardrop handles them right now." (or Vortex, Mod Organizer, or another app) | Another mod manager has the links. **Use DewDrop** takes them over. |
| "Stardrop took these links back. It does that every time it starts while logged in to Nexus." | DewDrop had the links, and Stardrop took them back. See [below](#when-stardrop-or-vortex-takes-the-links-back). |
| "Windows' Default apps setting sends these links to ..., which 'Use DewDrop' can't change. ..." | A choice in Windows' settings wins over DewDrop. See [below](#if-the-windows-default-apps-setting-is-in-the-way). |
| "Couldn't check which app handles them." | DewDrop couldn't read the setting. The SMAPI log has the details. |

## What it changes on your PC

**Use DewDrop** changes two things, for your Windows user only. It doesn't need admin rights.

1. It writes a small script, `nxm-handler.ps1`, in `C:\Users\<you>\AppData\Roaming\Stardrop\Data\Cache\DewDrop\`.
2. It sets the registry key `HKEY_CURRENT_USER\Software\Classes\nxm`, so that Windows runs that script for `nxm://` links. The command it writes there is:

   ```text
   powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Cache\DewDrop\nxm-handler.ps1" "%1"
   ```

This replaces the command another mod manager, such as Stardrop or Vortex, had put there. DewDrop doesn't touch Stardrop's own `Stardrop.nxm` entry or your choices in Windows' Default apps settings.

When you click a link on the website, the script saves it as a small file in `Cache\DewDrop\nxm\`. The game checks that folder about once a second and starts the download.

* **Game closed?** The link waits, and DewDrop picks it up the next time you start the game.
* **Logged out?** The link waits until you log in, and DewDrop tells you a download is waiting.
* **Old link?** The website's links expire. An old one gives "The website's download link for Nexus mod ... expired. Click it on the website again."
* **Not a mod file?** DewDrop only accepts links to Stardew Valley mod files, and only downloads from Nexus Mods. A collection link gets "That was a Nexus collection. DewDrop installs single mods; collections install through Stardrop."

{% hint style="info" %}
**Premium? You don't need this.** Any web page can try to open an `nxm://` link, though your browser normally asks you first. With a free account, a link only works if Nexus Mods made it for you when you clicked **Slow download**. With Premium, a link doesn't need that, so DewDrop would download, install and (with the default settings) turn on the file without asking. If you have Premium, leave these links with another program, or with none.
{% endhint %}

## When Stardrop or Vortex takes the links back

Only one program can handle these links at a time, and other mod managers can take them back:

* **Stardrop** checks the links each time it starts while you're logged in to Nexus Mods. Your login is shared, so if DewDrop is logged in, Stardrop is too.
  * If Stardrop handled them before, it quietly takes them back. DewDrop's panel then says "Stardrop took these links back."
  * If Stardrop never handled them, it asks you first. Answer no to keep them with DewDrop.
* **Vortex, Mod Organizer** and other managers take them when you turn on their own link handling. DewDrop's panel then names that program.

To get them back, click **Use DewDrop** again.

**Using both DewDrop and Stardrop with a free account?** Decide which one gets the links:

* **Keep them with DewDrop:** in Stardrop, open **View** > **Settings** and click **Remove NXM Association**. This removes only Stardrop's own registration. Then click **Use DewDrop** in DewDrop. From then on, Stardrop asks each time it starts instead of taking the links quietly; answer no. (These are the menu names in recent versions of Stardrop.)
* **Give them to Stardrop:** do this if you install Nexus collections from the website. DewDrop can't install collections, so Stardrop needs the links for that. See [Using DewDrop with Stardrop](../stardrop.md).

## If the Windows Default apps setting is in the way

If an app was picked for NXM in Windows' Default apps settings, Windows always uses that app, and DewDrop can't change it for you. DewDrop's panel then says "Windows' Default apps setting sends these links to ..., which 'Use DewDrop' can't change."

To fix it:

1. Open Windows **Settings** > **Apps** > **Default apps**.
2. Choose defaults by link type (Windows 11) or by protocol (Windows 10).
3. Find **NXM** and change it. DewDrop doesn't register itself as a Windows app, so it may not be in that list.

If the app chosen there is Stardrop, Stardrop's **Remove NXM Association** button (see above) also tries to clear that choice. You can click **Use DewDrop** before or after: DewDrop is ready as soon as Windows stops sending the links elsewhere.

## Undo it

DewDrop has no button to stop handling these links, and [uninstalling DewDrop](../getting-started/uninstalling.md) doesn't undo it either. Pick one of these.

**Give the links to another mod manager (easiest).** In Stardrop, open **View** > **Settings** and click **Register NXM Association**. In Vortex or another manager, turn its link handling on again. Either one replaces DewDrop's command.

**Remove DewDrop's handler and leave the links with no program.** Open the Start menu, type `PowerShell`, open **Windows PowerShell**, and paste this:

```powershell
$cmd = 'HKCU:\Software\Classes\nxm\shell\open\command'
$value = if (Test-Path -LiteralPath $cmd) { (Get-Item -LiteralPath $cmd).GetValue('') } else { '' }
if ($value -like '*nxm-handler.ps1*') {
    Remove-Item -LiteralPath 'HKCU:\Software\Classes\nxm' -Recurse
    'Removed DewDrop as the nxm:// link handler.'
} else {
    "DewDrop isn't the nxm:// link handler, so nothing was changed."
}
```

It removes the `nxm` key only when it points at DewDrop's script. After that, you can also delete `nxm-handler.ps1` from `C:\Users\<you>\AppData\Roaming\Stardrop\Data\Cache\DewDrop\`. If Stardrop handled the links before DewDrop, it takes them back by itself the next time it starts while logged in.

{% hint style="warning" %}
Don't delete `HKEY_CURRENT_USER\Software\Classes\nxm` without checking who it belongs to first. If it belongs to Stardrop or Vortex, their website downloads stop working. The script above does that check for you.
{% endhint %}
