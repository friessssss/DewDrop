---
description: "Have Steam's Play button start the game straight in your DewDrop profile, or let DewDrop offer a restart into it when the game starts with different mods."
icon: forward
---

# Start in your profile

A normal launch, from Steam's **Play** button or a shortcut, loads your whole `Mods` folder. Your profile only loads when SMAPI starts on `Selected Mods`, the folder DewDrop fills with links to your profile's mods. [How launching works](../getting-started/launching.md) explains why.

Two settings on the **Account** tab > **Settings** panel get you into your profile without thinking about it:

* **Start in my profile from Steam** makes Steam's **Play** button start the game straight in your profile.
* **Ask to load my profile** shows the **Load your profile?** box, which offers a restart when the game started with different mods.

[**Apply & Restart**](apply-and-restart.md) works either way.

## Start in my profile from Steam

This is for the Steam version of the game on Windows, and on Linux and the Steam Deck with DewDrop's Steam launcher.

### Turn it on

{% tabs %}
{% tab title="Windows" %}
1. Open **Mods**, then the **Account** tab.
2. In the **Settings** panel, tick **Start in my profile from Steam**.
3. Quit the game. A few seconds after it closes, Steam closes, DewDrop changes the game's launch option, and Steam opens again, minimized to the system tray.

The next time you open **Mods**, a message says "Steam now starts the game in your profile." If it didn't work, the message says why. See [Troubleshooting](../help/troubleshooting.md#steams-launch-option-wasnt-changed).

Behind the scenes, DewDrop adds SMAPI's `--mods-path` option, pointing at `Selected Mods`, to Stardew Valley's launch option in Steam. Steam saves over its settings file when it closes, so the change waits until you quit, once each time you turn the setting on or off. After **Apply & Restart**, it waits until you quit the restarted game. [How DewDrop works](../reference/how-it-works.md#how-dewdrop-changes-steams-launch-option) has the details, including the backup it makes.
{% endtab %}

{% tab title="Linux and Steam Deck" %}
1. Have the DewDrop installer set up Steam, if it hasn't yet. It does this by itself on a Steam Deck. On other Linux systems, run it with `--steam-setup`, for example:

   ```bash
   curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --steam-setup
   ```

   See [Steam on Linux and the Steam Deck](../reference/installer-options.md#steam-on-linux-and-the-steam-deck) for what it changes.
2. Open **Mods**, then the **Account** tab.
3. In the **Settings** panel, tick **Start in my profile from Steam**.

It takes effect at once, and Steam never closes for it. DewDrop links your profile as you quit, and from the next **Play**, the launcher starts the game in it. Until your profile is linked, the launcher loads your whole `Mods` folder, so you never start without the **Mods** button.
{% endtab %}
{% endtabs %}

The **Load your profile?** box and the [installer](../getting-started/installing/installer.md) offer this setting too.

### Playing with it on

Press **Play** in Steam, and the game starts with only your profile's mods. Changes you make in the menu are linked as you quit, so the next **Play** loads them. Until then, the status line says "Changes load the next time you start the game from Steam, or restart now." Click **Apply & Restart** to load them right away. On Windows, mod updates waiting for a restart also go in as you quit.

Only Steam's **Play** button does this. A desktop shortcut still loads the whole `Mods` folder, unless you [add your profile to it](#do-it-by-hand).

### Turn it off

Untick **Start in my profile from Steam**. On Windows, quit the game: Steam closes and opens again once, and DewDrop removes only the `--mods-path` part, so **Play** still starts SMAPI. With DewDrop's Steam launcher, unticking takes effect at once.

**Before you uninstall DewDrop** on Windows, turn this off and quit the game, or delete the `--mods-path "..."` part from Steam's launch options yourself. Otherwise SMAPI keeps loading your last profile, with no **Mods** button. With DewDrop's Steam launcher, take the launcher out of Steam's launch options instead (see [What the installer changes](../getting-started/steam-deck.md#what-the-installer-changes)). See [Uninstalling DewDrop](../getting-started/uninstalling.md).

### When it isn't available

The row is grayed out, and hovering over it says why. The usual reasons:

* You play on macOS, where SMAPI's launcher doesn't pass Steam's launch options on.
* The game isn't the Steam version, such as GOG or the Xbox app.
* On Linux or a Steam Deck, Steam doesn't start the game through DewDrop's Steam launcher yet. Run the installer: in Desktop Mode on a Deck, or with `--steam-setup` on other Linux systems.
* The game runs through Proton or Wine.
* DewDrop can't find Steam, can't find or read your Steam account's settings, or couldn't update its launcher.
* On Windows, the game's launch option in Steam starts a program other than SMAPI.

[Troubleshooting](../help/troubleshooting.md#you-cant-tick-start-in-my-profile-from-steam) lists every message and what to do about it.

{% hint style="warning" %}
**Using Stardrop too?** Stardrop rebuilds `Selected Mods` whenever it starts the game. If you start a Stardrop collection profile from Stardrop, its links leave DewDrop out, so on Windows the next **Play** in Steam starts without the **Mods** button. To get it back, start one of your own profiles from Stardrop once, or remove the `--mods-path` part from Steam's launch options. DewDrop's Steam launcher checks for DewDrop first, and starts your whole `Mods` folder instead.
{% endhint %}

## The "Load your profile?" box

When the game starts on your whole `Mods` folder and the mods that loaded differ from your profile, DewDrop skips the title intro and shows this box. **Ask to load my profile** on the **Account** tab turns it on or off. It's on by default.

<figure><img src="../.gitbook/assets/shot-title-prompt.png" alt="The Load your profile? box over the title screen. It says 20 mods that are off in the SVE playthrough profile loaded anyway, names three of them, and asks to restart into the profile. Below are a ticked Start in my profile from Steam from now on checkbox, a note that Steam closes and opens again once after you quit, and the Don't ask again, Restart now and Not now buttons."><figcaption><p>DewDrop asks when the mods that loaded don't match your profile.</p></figcaption></figure>

The box names the mods that are off in your profile but "loaded anyway", and the ones from your profile that "didn't load". Then you choose:

* **Restart now** (or **Enter**): the game closes and opens again in your profile, like **Apply & Restart**. It's grayed out while downloads are running, and the box says it's waiting for them.
* **Not now** (or **Esc**, or **B** on a controller): keep playing. DewDrop asks again next time.
* **Don't ask again**: turns **Ask to load my profile** off.

While **Start in my profile from Steam** is available but off, the box also has a **Start in my profile from Steam from now on** checkbox, ticked at first. If it's ticked when you click **Restart now**, the setting turns on:

* **On Windows**, the note under it says "With that ticked, Steam closes and opens again once after you quit, to save its new launch option." Steam changes when you quit the restarted game.
* **With DewDrop's Steam launcher**, the note says "With that ticked, Steam's Play button starts the game in your profile from now on." The setting changes at once.

### When it doesn't appear

The box doesn't open when the mods that loaded match your profile, when the game started in a profile or on a folder of your own, when **Ask to load my profile** is off, or when the game runs through Proton or Wine. It also stays away after a Steam multiplayer invite, or if you open **Load**, **Co-op** or another title menu first. [Troubleshooting](../help/troubleshooting.md#dewdrop-doesnt-ask-to-load-your-profile) lists every case and how to check which one it was.

### After a restart that didn't come back

If the last DewDrop restart didn't finish, the box says so instead of naming mods:

* "Last time DewDrop restarted the game, it didn't come back in your profile."
* "Last time, the game closed while your profile was loading."

The Steam checkbox then starts unticked. Click **Restart now** to try again.

If the restart can't start at all, the box changes to **Couldn't load your profile** and says why. Click **OK** and keep playing. See [If it doesn't restart](apply-and-restart.md#if-it-doesnt-restart).

## Do it by hand

On Windows, you can add `--mods-path` yourself: to a desktop shortcut, which the Steam setting never changes, or in Steam.

**Set this up only after your first Apply & Restart** (or **Restart now** in the box). Until then, `Selected Mods` doesn't contain DewDrop, so the game would start without the **Mods** button.

First, find your `Selected Mods` folder. Press **Win+R**, paste this and press **Enter**:

```text
%AppData%\Stardrop\Data\Selected Mods
```

Copy the full path from File Explorer's address bar. It looks like `C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods`.

<details>

<summary>Add it to a desktop shortcut</summary>

1. Right-click the **Stardew Valley (SMAPI)** shortcut on your desktop and choose **Properties**.
2. In the **Target** box, click after the closing quote of `...StardewModdingAPI.exe"`, type a space, then add `--mods-path` and your `Selected Mods` path in quotes:

   ```text
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

   ```text
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command%
   ```

3. Add `--mods-path` and your `Selected Mods` path, in quotes, between the closing quote and `%command%`:

   ```text
   "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" --mods-path "C:\Users\<you>\AppData\Roaming\Stardrop\Data\Selected Mods" %command%
   ```

   Keep your own game folder, and use your own Windows user name in place of `<you>`. Both sets of quotes matter, because both paths contain spaces.
4. Close the window and click **Play**.

Once the game starts, check that **Start in my profile from Steam** is ticked in **Mods** > **Account**. If it isn't, DewDrop takes the `--mods-path` part out again when you quit.

</details>

To undo either one, delete only the `--mods-path "..."` part.

On Linux and the Steam Deck, a `--mods-path` you add to Steam's launch options doesn't count as the setting. Use DewDrop's Steam launcher instead.

## Platform notes

| System | Start in my profile from Steam | **Load your profile?** box |
| --- | --- | --- |
| Windows, Steam | Yes | Yes |
| Windows, GOG or Xbox app | No | Yes |
| macOS | No | Yes, but the restart is untested |
| Linux | Untested, with DewDrop's Steam launcher (installer option `--steam-setup`) | Yes, but the restart is untested |
| Steam Deck | Yes, once the installer has set it up. Being tested | Yes. Being tested |
| Proton or Wine | No | No |

See [Platform support](../reference/platform-support.md) for everything else.
