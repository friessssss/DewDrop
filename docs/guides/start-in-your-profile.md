---
description: "Have Steam's Play button start the game straight in your DewDrop profile, or let DewDrop offer a restart into it when the game starts with different mods."
icon: forward
---

# Start in your profile

SMAPI picks the folder it loads mods from as the game starts. A normal launch, from Steam's **Play** button or a shortcut, loads your whole `Mods` folder. Your profile only loads when SMAPI is pointed at `Selected Mods`, the folder DewDrop fills with links to your profile's mods. [How launching works](../getting-started/launching.md) explains more.

On Windows with the Steam version, **Start in my profile from Steam** makes **Play** start straight in your profile. Otherwise, the **Load your profile?** box offers a restart. [**Apply & Restart**](apply-and-restart.md) works as before.

## Start in my profile from Steam

This setting adds SMAPI's `--mods-path` option, pointing at `Selected Mods`, to Stardew Valley's launch options in Steam. The rest of the launch option is kept.

### Turn it on

1. Open **Mods**, then the **Account** tab.
2. In the **Settings** panel, tick **Start in my profile from Steam**.
3. Quit the game.

The **Load your profile?** box and the Windows [installer](../getting-started/installing/installer.md) offer it too.

A few seconds after the game closes, Steam closes, DewDrop changes the launch option, and Steam opens again, minimized to the system tray. This happens once each time you turn the setting on or off. If you click **Apply & Restart** instead, the change waits until you quit the restarted game.

{% hint style="info" %}
**Why does Steam close?** Steam saves over its settings file when it closes, so a change made while it runs would be lost. DewDrop changes only the signed-in account's settings, and backs the file up first as `localconfig.vdf.dewdrop-backup`.
{% endhint %}

The next time you open **Mods**, a message says "Steam now starts the game in your profile." If the change didn't work, the message says why.

### Playing with it on

Press **Play** in Steam, and the game starts with only your profile's mods. Changes you make in the menu are linked as you quit, so the next **Play** loads them. Until then, the status line says "Changes load the next time you start the game from Steam, or restart now." Click **Apply & Restart** to load them right away. Waiting mod updates also finish when you quit.

Only **Play** in Steam does this. A desktop shortcut still loads the whole `Mods` folder.

### Turn it off

Untick **Start in my profile from Steam** and quit the game. Steam closes and opens again once, and DewDrop removes only the `--mods-path` part, so **Play** still starts SMAPI.

**Before you uninstall DewDrop,** turn this off and quit the game, or delete the `--mods-path "..."` part from Steam's launch options yourself. Otherwise SMAPI keeps loading your last profile, with no **Mods** button. See [Uninstalling DewDrop](../getting-started/uninstalling.md).

### When it isn't available

The row is grayed out, and hovering over it says why, when:

* you're not on Windows, or the game runs through Proton or Wine;
* the game isn't the Steam version, such as GOG or the Xbox app;
* DewDrop can't find Steam, or can't find or read your Steam account's settings;
* the game's launch option in Steam starts a program other than SMAPI.

{% hint style="warning" %}
**Using Stardrop too?** Stardrop rebuilds `Selected Mods` whenever it starts the game. If you start a Stardrop collection profile from Stardrop, its links leave DewDrop out, so the next **Play** in Steam starts without the **Mods** button. To get it back, start one of your own profiles from Stardrop once, or remove the `--mods-path` part from Steam's launch options.
{% endhint %}

## The "Load your profile?" box

When the game starts on your whole `Mods` folder and the mods that loaded differ from your profile, DewDrop skips the title intro and shows this box. The **Ask to load my profile** setting on the **Account** tab turns it on or off. It's on by default.

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>DewDrop asks when the mods that loaded don't match your profile.</p></figcaption></figure>

The box names the mods that are off in your profile but "loaded anyway", and the ones from your profile that "didn't load". Then you choose:

* **Restart now** (or **Enter**): the game closes and opens again in your profile, like **Apply & Restart**. It's grayed out while downloads are running.
* **Not now** (or **Esc**, or **B** on a controller): keep playing. DewDrop asks again next time.
* **Don't ask again**: turns **Ask to load my profile** off.
* **Start in my profile from Steam from now on** (Windows, Steam version, while that setting is off): ticked at first. If it's ticked when you click **Restart now**, the setting turns on, and Steam's launch option changes when you quit the restarted game.

### When it doesn't appear

DewDrop shows nothing when:

* the mods that loaded match your profile;
* SMAPI started on a folder other than `Mods`, as it does from **Play** with the Steam setting on, after **Apply & Restart**, from Stardrop, or with your own `--mods-path` option;
* **Ask to load my profile** is off;
* you're on Steam Deck, or the game runs through Proton or Wine;
* you started the game from a Steam multiplayer invite;
* DewDrop couldn't read Stardrop's `Settings.json` or your profile file, or SMAPI's launcher is missing;
* you opened **Load**, **Co-op** or another title menu first.

### After a restart that didn't come back

If the last DewDrop restart didn't come back in your profile, the box says so instead of naming mods: "Last time DewDrop restarted the game, it didn't come back in your profile." The Steam checkbox then starts unticked. Click **Restart now** to try again.

If the restart can't start at all, the box changes to **Couldn't load your profile** and says why. Click **OK** and keep playing. See [Troubleshooting](../help/troubleshooting.md).

## Do it by hand

You can add `--mods-path` yourself: on a desktop shortcut, which the Steam setting never changes, or in Steam. First find your `Selected Mods` folder: press **Win+R**, paste `%AppData%\Stardrop\Data\Selected Mods` and press **Enter**. Copy the full path from File Explorer's address bar. It looks like `C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods`.

**Set this up only after your first Apply & Restart** (or **Restart now** in the box). Until then, `Selected Mods` doesn't contain DewDrop, so the game would start without the **Mods** button.

<details>

<summary>Add it to a desktop shortcut</summary>

1. Right-click the **Stardew Valley (SMAPI)** shortcut on your desktop and choose **Properties**.
2. In the **Target** box, click after the closing quote of `...StardewModdingAPI.exe"`, type a space, then add `--mods-path` and your `Selected Mods` path in quotes:

   ```
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" --mods-path "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods"
   ```

   Keep your own game folder, and use your own Windows user name in place of `<you>`.
3. Click **OK** and start the game from the shortcut.

The shortcut loads what DewDrop last linked: on **Apply & Restart**, or as you quit when the Steam setting is on.

</details>

<details>

<summary>Add it in Steam yourself</summary>

1. In Steam, right-click **Stardew Valley** and choose **Properties**.
2. On the **General** page, find **Launch Options**. SMAPI's option should already be there, with your own game folder. (If the box is empty, see [About SMAPI](../getting-started/installing/smapi.md) first.)

   ```
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command%
   ```

3. Add `--mods-path` and your `Selected Mods` path, in quotes, between the closing quote and `%command%`:

   ```
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" --mods-path "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods" %command%
   ```

   Keep your own game folder, and use your own Windows user name in place of `<you>`. Both sets of quotes matter, because both paths contain spaces.
4. Close the window and click **Play**.

Once the game starts, check that **Start in my profile from Steam** is ticked in **Mods** > **Account**. If it isn't, DewDrop takes the `--mods-path` part out again when you quit.

</details>

To undo either one, delete only the `--mods-path "..."` part.

## Platform notes

* **Windows, Steam version:** everything on this page.
* **Windows, GOG or Xbox app:** the box and **Apply & Restart**. The Xbox app has nowhere to add launch options.
* **macOS and Linux:** the box and **Apply & Restart**, though the restart hasn't been tested there yet. There's no Steam setting, because SMAPI's launcher there doesn't pass Steam's launch options on.
* **Steam Deck, Proton and Wine:** neither the box nor the Steam setting. See [Platform support](../reference/platform-support.md).
