---
description: "Drag and drop DewDrop into your Mods folder yourself, the same way you'd install any SMAPI mod."
icon: hand-pointer
---

# Install by hand

DewDrop is a normal SMAPI mod, so you can install it like any other: put its folder in your game's `Mods` folder. This page shows you where that folder is on each system, and the mistakes to avoid.

Prefer to have it done for you? Use the [installer](installer.md) instead. It also installs SMAPI if you need it.

## Before you start: install SMAPI

DewDrop needs **SMAPI 4.0 or later**. If you've used mods before, you probably have it already.

If you don't, download it from [smapi.io](https://smapi.io) and follow SMAPI's install guide for [Windows](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows), [macOS](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac) or [Linux](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux). [About SMAPI](smapi.md) explains what it is and how to check which version you have.

## Download DewDrop

Open the [latest DewDrop release](https://github.com/friessssss/DewDrop/releases/latest). Under **Assets**, click `DewDrop-<version>.zip` (for example, `DewDrop-0.1.0.zip`).

Make sure you pick the one **without** `-installer` in its name. Inside it is a single folder called `DewDrop`.

## Find your Mods folder

The `Mods` folder is inside your game folder. SMAPI creates it when you install SMAPI.

{% tabs %}
{% tab title="Windows" %}
**Steam**

1. In Steam, right-click **Stardew Valley** and choose **Manage** > **Browse local files**.
2. File Explorer opens your game folder. Open the `Mods` folder inside it.

By default, it's `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods`.

**GOG Galaxy**

1. In GOG Galaxy, right-click **Stardew Valley** in your library and choose **Manage installation** > **Show folder**.
2. Open the `Mods` folder inside it.

By default, it's `C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley\Mods`.

**Xbox app (PC Game Pass)**

1. In the Xbox app, open **Stardew Valley**, click the **...** button and choose **Manage**.
2. On the **Files** tab, click **Browse**.
3. Open the `Content` folder, then the `Mods` folder inside it.

By default, it's `C:\XboxGames\Stardew Valley\Content\Mods`. The Xbox app also needs an extra step before it starts SMAPI. See [About SMAPI](smapi.md#xbox-app).
{% endtab %}

{% tab title="macOS" %}
On a Mac, the game folder is `Contents/MacOS`, deep inside the game's app. That's where `Mods` is.

**Steam**

1. In Steam, right-click **Stardew Valley** and choose **Manage** > **Browse local files**.
2. In the Finder window that opens, open `Contents`, then `MacOS`, then `Mods`. If you see `Stardew Valley.app` instead, right-click it and choose **Show Package Contents** first.

By default, it's `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods`.

**GOG**

1. In Finder, open **Applications**.
2. Right-click **Stardew Valley** and choose **Show Package Contents**.
3. Open `Contents`, then `MacOS`, then `Mods`.

By default, it's `/Applications/Stardew Valley.app/Contents/MacOS/Mods`.

**Tip:** the `Library` folder is hidden in Finder. To jump to a path, choose **Go** > **Go to Folder...** in Finder (or press **Shift+Cmd+G**), paste the path and press **Return**.
{% endtab %}

{% tab title="Linux" %}
**Steam**

1. In Steam, right-click **Stardew Valley** and choose **Manage** > **Browse local files**.
2. Open the `Mods` folder inside the game folder.

By default, it's one of these:

* `~/.local/share/Steam/steamapps/common/Stardew Valley/Mods`
* `~/.steam/steam/steamapps/common/Stardew Valley/Mods`
* Steam installed as a Flatpak: `~/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/Stardew Valley/Mods`

**GOG**

By default, it's `~/GOG Games/Stardew Valley/game/Mods`. Note the extra `game` folder.

**Tip:** folders whose names start with a `.`, like `.local`, are hidden. Press **Ctrl+H** in most file managers to show them.
{% endtab %}
{% endtabs %}

**No `Mods` folder?** Then SMAPI probably isn't installed in this copy of the game yet. Install SMAPI first; it creates the folder.

## Put the DewDrop folder in Mods

Close the game if it's running. Then:

* **Windows:** double-click `DewDrop-<version>.zip` to open it. Drag the `DewDrop` folder from it into your `Mods` folder.
* **macOS:** double-click the zip to extract it. Drag the `DewDrop` folder into your `Mods` folder.
* **Linux:** extract the zip with your file manager or `unzip`. Move the `DewDrop` folder into your `Mods` folder.

When you're done, it should look like this:

```
Mods/
  DewDrop/
    manifest.json
    DewDrop.dll
    DewDrop.Core.dll
    SharpCompress.dll
    ZstdSharp.dll
    ...
```

## Start the game

Start the game with SMAPI. A second window, the SMAPI console, opens next to the game.

* **Windows with Steam:** Steam's **Play** button only starts SMAPI if you've set the SMAPI launch option. If you haven't, follow the steps in [About SMAPI](smapi.md#steam-on-windows), or start `StardewModdingAPI.exe` in your game folder.
* **Everyone else:** start the game the way you usually play with mods. See [About SMAPI](smapi.md#starting-the-game-with-smapi) if you're not sure.

On the title screen, click the new **Mods** button between **Co-op** and **Exit**. That's it! Next, read [Your first launch](../first-launch.md).

{% hint style="info" %}
**Using Stardrop?** The first time, start the game from Steam or `StardewModdingAPI.exe` (on macOS and Linux, the game's normal launcher), not from Stardrop. Stardrop only loads the mods in its profile, and DewDrop isn't in it yet. On that first launch, DewDrop adds itself to your Stardrop profiles. See [Using DewDrop with Stardrop](../../stardrop.md).
{% endhint %}

## Check the folder layout

If the **Mods** button doesn't appear, the folder is usually in the wrong place. Aim for `Mods/DewDrop/manifest.json`. Look out for these:

* **The zip itself is in `Mods`.** SMAPI can't read zip files. Take the `DewDrop` folder out of the zip and put that in `Mods` instead.
* **The files are loose in `Mods`.** If `manifest.json` and the `.dll` files sit straight in `Mods`, SMAPI won't load them. Make a `DewDrop` folder in `Mods` and move them into it.
* **A folder inside a folder**, like `Mods/DewDrop/DewDrop/manifest.json` or `Mods/DewDrop-0.1.0/DewDrop/manifest.json`. This happens when you extract the zip into a new folder and then copy that folder. If yours is one level deeper like this, move the inner `DewDrop` folder straight into `Mods` and delete the empty outer one.
* **Two copies of DewDrop.** If SMAPI finds the same mod twice, it loads neither, and says "you have multiple copies of this mod installed" in its console. Keep one copy and delete the other.
* **The wrong game.** If you have the game from more than one store, make sure you're using the `Mods` folder of the copy you play.

Still stuck? See [Troubleshooting](../../help/troubleshooting.md).

## Updating by hand

To update DewDrop yourself, close the game and replace the old `DewDrop` folder with the new one. Copy your `config.json` out of the old folder first and put it back afterwards. That file holds your DewDrop settings. [Updating DewDrop](../updating-dewdrop.md) has the details. Running the [installer](installer.md) does the same thing for you.
