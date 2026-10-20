---
description: "Control DewDrop's title-screen menu from a script and save screenshots. Debug builds only."
icon: robot
---

# The debug bridge

Debug builds of DewDrop include a small test bridge. A script drops a text file of commands into a folder, and DewDrop runs them in the game: open the menu, click, type, press keys and save screenshots. Your real mouse and keyboard aren't touched. The screenshots in this guide are made this way.

{% hint style="warning" %}
The bridge only exists in Debug builds. Any program on your computer can use it, including to quit the game. Never share a Debug build; package releases in Release. See [Debug vs Release](building.md#debug-vs-release).
{% endhint %}

## Turn it on

1. Close the game, then build Debug: `dotnet build DewDrop.sln`. This deploys the Debug build to your `Mods` folder. See [Building from source](building.md).
2. Start the game through SMAPI.
3. In the SMAPI console, look for `Debug bridge listening in <folder>`. If that line is missing, a Release build is loaded.

The bridge lives in a `dewdrop-bridge` folder inside your temp folder:

* **Windows:** `%TEMP%\dewdrop-bridge`, usually `C:\Users\<you>\AppData\Local\Temp\dewdrop-bridge`.
* **macOS and Linux:** `$TMPDIR/dewdrop-bridge`, or `/tmp/dewdrop-bridge` when `TMPDIR` isn't set. The bridge hasn't been tested on these systems.

It has two subfolders, `in` and `out`, which are created when the game starts.

When you've finished, build Release again (`dotnet build DewDrop.sln -c Release`) so the copy you play with leaves the bridge out.

## How it works

1. You write a text file into `in` with the extension `.cmd`, one command per line.
2. About four times a second (every 15 game ticks), if it isn't busy, the bridge takes the **first `.cmd` file by name**. It reads every non-blank line, trims it, creates or empties `out\<same name>.out`, and deletes the `.cmd` file. If the file is still locked, it tries again on the next check.
3. The commands run in order on the game's main thread. **All of them run in the same tick unless one is `wait`.**
4. After each command, the bridge appends `> <command>` and then the reply to the `.out` file. When the last command has run, it appends `DONE`. Lines end with LF.

Only one file runs at a time. The bridge doesn't read the next `.cmd` until every command in the current one has run. A `wait` holds back every command after it, even the ones in the next file. If a command throws an error, its reply is `ERROR <ExceptionType>: <message>`, and the remaining commands still run.

A finished reply file looks like this:

```
> skipintro
ok
> wait 2000
waiting
> status
menu=TitleMenu sub=none ui=1920x1080 zoom=1 profile=Default installed=36 loaded=11 restart=False kind=Profile ask=True askStatus="this session is DewDrop's restart" steamWanted=False steamOn=False steamPending=False steamProblem="none" marker=none failed=none nexus=no toasts=[]
> shot C:/dewdrop-shots/title.png
saved 1920x1080 to C:/dewdrop-shots/title.png
DONE
```

### Sending commands safely

* Write the file under another name first, such as `job1.tmp`, then rename it to `job1.cmd`. That way the bridge never reads a half-written file.
* Delete the old `out\job1.out` before you start. The bridge never cleans up `.out` files.
* Read `out\job1.out` repeatedly until one line is exactly `DONE`.
* Files run in name order, so start their names with a counter or timestamp if you queue several.

## Commands

The command name isn't case-sensitive. Everything after the first space is the argument, used as written. `X Y` means two whole numbers separated by spaces, in UI pixels (see [Coordinates and layout](#coordinates-and-layout)).

| Command | What it does | Reply |
| --- | --- | --- |
| `skipintro` | Skips the title screen's intro straight to the buttons. | `ok`, or `not on the title screen` |
| `open` | Skips the intro and presses the **Mods** button. This only starts the title sliding away; the menu appears a few seconds later. | `ok`, or `not on the title screen` |
| `back` | Presses the title screen's back button, which closes the DewDrop menu. Nothing happens while a confirm dialog is open, a text box has focus, the screen is moving, or the **Load your profile?** box is open. | `ok`, or `not on the title screen` |
| `click X Y` | Left-clicks and releases at that point. | `clicked X,Y`, even if nothing was there |
| `rclick X Y` | Right-clicks at that point. DewDrop's menu ignores right clicks. | `right-clicked X,Y` |
| `hover X Y` | Hovers at that point, which sets the tooltip text. | `hovered X,Y` |
| `key NAME` | Presses a key, by its XNA name: `Enter`, `Escape`, `Up`, `Down`, `Left`, `Right`, `Back`, `Tab`, `A`, `D1` and so on. | `ok`. An unknown name gives `ERROR ArgumentException: …` |
| `type TEXT` | Types the whole text into the text box that has focus. | `typed`, or `no text box has focus` |
| `scroll N` | Turns the mouse wheel. Positive scrolls up, negative scrolls down. | `ok` |
| `wait MS` | Pauses the rest of the commands for that many milliseconds. | `waiting` |
| `shot PATH` | Saves a PNG of the current menu to `PATH`. | `saved WxH to PATH`, or `no menu to draw` |
| `status` | Reports the current state on one line (see below). | the status line |
| `window W H` | Switches to windowed mode at that size. On Windows, the game won't go below 1280×720. | `windowed WxH` |
| `borderless` | Switches to windowed borderless mode. | `borderless` |
| `uiscale F` | Sets the UI scale, for example `1.0`. | `ui scale F` |
| `quit` | Closes the game. | `bye` |
| `ask on` or `ask off` | Turns **Ask to load my profile** on or off and saves it in `config.json`. Any word other than `on` means off. | `ask True` or `ask False` |
| `steam on` or `steam off` | Ticks or unticks **Start in my profile from Steam** and saves it in `config.json`. This only records the choice: Steam itself changes when the game closes. Any word other than `on` means off. | `steam wanted True, pending True`: the setting, and whether Steam will change when the game closes |
| `steamcheck` | Reads Stardew Valley's launch option from Steam's settings again, for example after you changed it while the game runs. | `problem=… option=… on=… pending=…` (see below) |
| `marker STAGE` | Leaves a restart note in `startup.json` for the next launch to find, as if a restart were in progress. `STAGE` is `restarting` or `arrived`. `marker clear` (or any other word) removes the note. | `marker Restarting`, `marker Arrived` or `marker cleared`, or `couldn't save` |
| `unsee ID` | Takes mods off the list of mods DewDrop has seen, so they count as new in your `Mods` folder at the next launch. Give one or more unique IDs, separated by spaces. | `forgotten`, even for an ID that wasn't on the list, or `couldn't save` |
| `diff` | Reports how the mods SMAPI loaded differ from the active profile (see below). | the difference line |
| `uninstall ID` | Uninstalls the first loose copy of the mod with that unique ID, as its trash can on the **Installed** tab does, without the dialog. The folder really is deleted when the game closes, unless you send `undo` first. | `uninstalled <folder>` with the folder's full path, `refused: <reason>` with the reason the trash can would give, or `not installed` (also for a mod that's already uninstalled, or only in a collection) |
| `undo ID` | Keeps an uninstalled mod, as its **Undo** button does. | `kept <folder>: <message>`, with the message **Undo** shows. Otherwise the message on its own, such as `<name> was already deleted.`, or `not uninstalled` |
| `removals` | Reports the uninstalls waiting for the game to close. | `removing=[ids] stillLoaded=[ids] queued=[id@folder, ...]` (see below) |
| `sources ID` | Reports what DewDrop knows about where a mod came from on Nexus Mods. | `nexus=[ids] install=<mod>/<file> v<version> copies=<n>` (see below), or `not installed` |
| anything else |  | `unknown command` |

The comment at the top of `DebugBridge.cs` doesn't list `window`, `borderless` or `uiscale`, but all three work.

### The status line

| Field | Meaning |
| --- | --- |
| `menu` | The type of the active menu, for example `TitleMenu`. |
| `sub` | The title screen's sub-menu. It's `DewDropMenu` while DewDrop is open, and `TitlePrompt` while the **Load your profile?** box is open. |
| `ui` | The UI size, `WxH`. Clicks, hovers and screenshots all use this coordinate space. |
| `zoom` | The UI scale in effect. |
| `profile` | The active profile's name. |
| `installed` | How many mods DewDrop found. |
| `loaded` | How many mods SMAPI loaded this session. |
| `restart` | `True` when **Apply & Restart** is needed to match the profile. |
| `kind` | How this session started: `Plain` for a normal launch (SMAPI loaded the game's `Mods` folder, or the mods folder set in Stardrop), `Profile` for a launch in a profile, or `Custom` for any other folder. |
| `ask` | The **Ask to load my profile** setting: `True` or `False`. |
| `askStatus` | What happened about the **Load your profile?** box this session, in double quotes. See the next table. |
| `steamWanted` | Whether **Start in my profile from Steam** is ticked. Until you've chosen, it follows what Steam's launch option already does. |
| `steamOn` | `True` when Stardew Valley's launch option in Steam, as last read, has `--mods-path` pointing at `Selected Mods`. DewDrop reads it as the game starts and on `steamcheck`. |
| `steamPending` | `True` when Steam's launch option will change as the game closes: the setting can be used here, and `steamWanted` differs from `steamOn`. |
| `steamProblem` | Why **Start in my profile from Steam** can't be used on this PC, in double quotes, as the row's hover text says it. `"none"` when it can. |
| `marker` | The restart note in `startup.json` right now: `Restarting`, `Arrived` or `none`. The note is cleared when the restarted game reaches its title screen, so it's usually `none`. |
| `failed` | On a launch that isn't in a profile, the note it found from a restart that didn't come back: `Restarting` if the restart never came back, `Arrived` if the game closed while the profile was loading. Otherwise `none`. |
| `nexus` | Your Nexus Mods user name, with `+premium` for Premium accounts, or `no` when logged out. |
| `toasts` | The messages on screen, inside square brackets and separated by vertical bars. |

`askStatus` is one of these:

| Value | What it means |
| --- | --- |
| `waiting for the title screen` | Nothing is decided yet. |
| `asked` | The **Load your profile?** box opened. |
| `this session is DewDrop's restart` | This game is a DewDrop restart that came back in its profile. |
| `the game started in a profile` | A launch in a profile that isn't a DewDrop restart, such as one from Stardrop, from Steam with **Start in my profile from Steam** on, or with a `--mods-path` launch option. |
| `SMAPI started on a folder DewDrop doesn't manage` | The session is `Custom`. |
| `turned off` | **Ask to load my profile** is off. |
| `not available on Steam Deck or under Wine or Proton` | The box is off on this system. |
| `the settings or profile couldn't be read` | Stardrop's `Settings.json` or the active profile file couldn't be read. |
| `started from a Steam invite` | The game started from a Steam multiplayer invite. |
| `SMAPI's launcher is missing` | SMAPI's launcher isn't in the game folder, so a restart couldn't start. |
| `the mods loaded already match the profile` | Nothing to do. |
| `another title menu opened first` | **Load**, **Co-op** or another title menu opened before the box could. |
| `a save loaded, or no title screen came up in time` | A save loaded, or no title screen came up within 3 minutes of the game running. |
| `stopped after an error` | Something went wrong. The SMAPI log has the error. |

### The difference line

`diff` replies with one line, for example:

```
matches=False turnedOff=[Author.ModA, Author.ModB] notLoaded=[] otherCopy=[] rejected=[]
```

Each list holds unique IDs. DewDrop itself never counts. The comparison is only meaningful on a normal launch (`kind=Plain`).

| Field | Meaning |
| --- | --- |
| `matches` | `True` when the mods that loaded match the profile, so a normal launch doesn't ask. |
| `turnedOff` | Mods that loaded but are off in the profile, or aren't in DewDrop's mods folder at all. A restart into the profile leaves them out. |
| `notLoaded` | Mods that are on in the profile but didn't load. A restart would load them. |
| `otherCopy` | Mods that loaded from a different copy than the one the profile uses, such as a loose copy instead of a Stardrop collection copy. |
| `rejected` | Mods that are on in the profile but SMAPI refused to load as the game started. A restart wouldn't load them either, so they don't count as a difference. |

### The steamcheck line

`steamcheck` replies with one line, for example:

```
problem=none option="C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe" %command% on=False pending=True
```

| Field | Meaning |
| --- | --- |
| `problem` | Why **Start in my profile from Steam** can't be used here, or `none`. Unlike `steamProblem` in the status line, it isn't in quotes. |
| `option` | Stardew Valley's launch option, exactly as Steam stores it. It's empty when the game has no launch option, and `(unknown)` when DewDrop couldn't read it. |
| `on` and `pending` | The same as `steamOn` and `steamPending` in the status line. |

### The removals line

`removals` replies with one line, for example:

```
removing=[Author.ModA] stillLoaded=[Author.ModA] queued=[Author.ModA@C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\ModA]
```

| Field | Meaning |
| --- | --- |
| `removing` | Unique IDs of the mods uninstalled and waiting for the game to close: the rows that say **Deleted when the game closes**. |
| `stillLoaded` | The ones of those the game has loaded, which make the bottom bar say "... still loaded. Restart to finish uninstalling." |
| `queued` | The uninstalls listed in `pending-updates.json`, each as the unique ID, `@`, and the mod's folder. Usually the same mods as `removing`. |

### The sources line

`sources` replies with one line, for example:

```
nexus=[1915] install=1915/12345 v2.5.3 copies=1
```

| Field | Meaning |
| --- | --- |
| `nexus` | The Nexus mod IDs the mod belongs to, from its update key, DewDrop's install record and smapi.io's list. Browse tags these mods' cards. See [How DewDrop knows you have a mod](../guides/browse.md#how-dewdrop-knows-you-have-a-mod). |
| `install` | The Nexus mod and file DewDrop last installed it from, and the version that file put in, from `mod-sources.json`. `none` when DewDrop didn't install it. |
| `copies` | How many loose copies of the mod are installed. It's 1 for a mod that's only in a collection. More than 1 means the **Installed** tab says **Installed twice**. |

### Screenshots

`shot` draws the current menu into an image the size of the UI and saves it as a PNG. It creates the folder if needed and overwrites an existing file. Use a full path; forward slashes and spaces are fine.

On the title screen, the picture shows the whole title screen: sky, clouds, the DewDrop menu, its messages and its dialogs, and the **Load your profile?** box. It doesn't capture the farm or the in-game HUD. The picture **does** include the mouse pointer at the real mouse position, so move your real mouse into a corner first.

## Coordinates and layout

Clicks, hovers and screenshots share one coordinate space: the UI size that `status` reports as `ui=WxH`. At UI scale 1.0 this matches the window size. For predictable numbers, start each session with `window 1920 1080` and `uiscale 1.0`.

DewDrop lays out its menu from the UI width `W` and height `H` (`DewDropMenu.cs`). All divisions round down.

* **Frame:** width `fw = min(1600, W − 128)`. Let `avail = max(360, H − 232)`; height `fh = min(1150, avail)`. The frame sits at `x = (W − fw) / 2`, `y = 112 + (avail − fh) / 2`.
* **Tabs:** one row at `frame.y + 28`, 60 pixels tall, so its middle is at `frame.y + 58`. The first tab starts at `frame.x + 32`. Each tab is its label's width plus 48, with 8 pixels between tabs, in this order: **Installed**, **Browse**, **Profiles**, **Downloads**, **Account**.
* **Content area:** `x = frame.x + 32`, width `fw − 64`. The top is `frame.y + 88 + 28` and the bottom is `frame.bottom − 116 − 44`. In a compact frame (`fh < 640`), use 16 instead of 28 and 16 instead of 44.
* **Apply & Restart:** 72 pixels tall, with its right edge at `frame.right − 32` and its top at `frame.bottom − 110`.

The **Installed** and **Downloads** tabs can show a count, like `(2 new)` on **Installed** or `(3)` on **Downloads**, which changes how wide they are. Measure tab positions from a screenshot. There's no command to switch tabs, so click them.

At a UI size of 1920×1080 the numbers work out like this:

| Part | Where it is |
| --- | --- |
| Frame | x 160, y 112, 1600 × 848 |
| Tab row | y 140 to 200 (middle 170) |
| Content area | x 192, y 228, 1536 × 572 |
| **Apply & Restart** | y 850 to 922, right edge at x 1728 |
| **Profiles** tab: name box | x 192, y 232, 48 tall (middle y 256) |
| **Profiles** tab: **Create** button | right edge at x 806, y 228 to 284 |
| **Profiles** tab: profile list | x 192, y 300, 614 × 500 |

At 3840×2160 the frame is at x 1120, y 501, 1600 × 1150.

## Timing and gotchas

**Opening and resizing**

* `open` only starts the slide. The menu appears once the title has moved off screen. Wait at least the UI width in milliseconds plus about a second (`wait 4000` is plenty at 1920 wide), then check that `status` shows `sub=DewDropMenu`.
* After `window`, wait about a second before you check `status`.

**Text boxes**

* Click the box first, then `type`.
* You can't delete text through the bridge. To get empty boxes, close the menu and open it again.
* `key Enter` and `key Back` never reach a text box. To submit, click the button next to it, such as **Create** on the **Profiles** tab.

**Dialogs and keys**

* `key Enter` only accepts a confirm dialog on a later tick than the one that opened it. Put a short `wait` (say `wait 200`) between the click that opens a dialog and `key Enter`.
* `key Escape` cancels a dialog, and so does clicking outside it.
* Accepting **Apply & Restart** really closes and restarts the game.

**Scrolling and hovering**

* `scroll` only moves a list when your real mouse is over it. Use `key Down` and `key Up` instead.
* The tooltip box and the tab highlight are drawn at your real mouse position. The game also re-hovers at the real mouse every tick, so put `hover` and `shot` in the same file with no `wait` between them.

**Clean screenshots**

* Info messages stay for 5 seconds and error messages for 8. Wait for them to clear, or click one to dismiss it.
* Pictures on the **Browse** tab load in the background. Allow several seconds.

**Reply files**

* A `.cmd` file with only blank lines creates an empty `.out` that never gets `DONE`.
* A `wait` on the last line doesn't hold back `DONE`.
* A line starting with `#` isn't a comment. It gets `unknown command`.

**Not tested yet:** whether `uiscale` is saved between sessions, how the bridge behaves while the window is minimized or in the background, and sending `open` while the menu is already open.

## Testing starting in your profile

DewDrop decides once per launch, on the first title screen, whether to ask **Load your profile?**, and it changes Steam's launch option only as the game closes. [Start in your profile](../guides/start-in-your-profile.md) explains both. So most of these tests set up the **next** launch: send the commands, close the game with `quit`, then start it again yourself. The bridge can't start the game for you.

Most tests need a normal launch, one where SMAPI loads your whole `Mods` folder. On Windows, running `StardewModdingAPI.exe` from the game folder always does. Steam's **Play** button does too, unless **Start in my profile from Steam** is on.

{% hint style="warning" %}
While **Start in my profile from Steam** is ticked, quitting the game can close the real Steam and change its settings file. `steam on` ticks it, and so does **Restart now** while the box's Steam checkbox is ticked. Test that part with a [fake Steam folder](#testing-with-a-fake-steam-folder) instead.
{% endhint %}

### The Load your profile? box

1. **Make the mods differ.** On the **Installed** tab, turn off a mod that's in your `Mods` folder. Then `quit` and start the game normally, which loads that mod anyway.
2. **Check the box.** On the title screen, send `status` and `diff`. Look for `kind=Plain`, `askStatus="asked"`, and the mod's ID in `turnedOff`. `shot` captures the **Load your profile?** box. The box ignores clicks and keys for its first half second. After that, `key Escape` answers **Not now**, and `key Enter` answers **Restart now**, which really restarts the game.
3. **Mind the Steam checkbox.** On Windows with the Steam version of the game, while **Start in my profile from Steam** is off and can be used, the box also shows **Start in my profile from Steam from now on**, ticked. **Restart now** then turns the setting on, and Steam changes when you quit the restarted game. Click the checkbox to untick it before you answer, unless you're testing with a fake Steam folder.
4. **Check the restart.** The restarted game skips its title intro, and `status` shows `kind=Profile` and `askStatus="this session is DewDrop's restart"`.
5. **Turn the question off.** Send `ask off`, then `quit` and start normally. No box opens, and `status` shows `askStatus="turned off"`. **Don't ask again** in the box does the same. Send `ask on` to turn it back on.
6. **Try a new mod.** Send `unsee` with the unique ID of a mod that's in your `Mods` folder and off in every profile. Then `quit` and start normally. While **Turn on mods when installed** is on, DewDrop turns that mod on in the active profile as the game starts, and when you open the menu a message says "Turned on *name*, new in your Mods folder." A new mod doesn't count as a difference, so it never opens the box by itself. Each launch marks every mod that's on in any profile as seen again, so `unsee` only lasts for mods that are off everywhere.
7. **Try a restart that didn't come back.** Send `marker restarting`, or `marker arrived` for a game that closed while the profile was loading. Then `quit` and start normally, with the mods still different. The box says why it's asking: "Last time DewDrop restarted the game, it didn't come back in your profile." or "Last time, the game closed while your profile was loading." `status` shows `failed=Restarting` or `failed=Arrived`, and the Steam checkbox, if the box shows it, starts unticked.
8. **Clean up.** Send `ask on` if you turned it off, and turn your mod back on. If `status` shows a `marker` other than `none`, send `marker clear`.

The note that `marker` leaves has the ID `bridge`. A launch in your profile treats it as a restart that didn't come back and clears it, without asking. The one exception is a `marker restarting` note when the game starts in your profile with the environment variable `DEWDROP_LAUNCH` set to `bridge`. That launch counts as the restart arriving: the title intro is skipped, and `askStatus` is `"this session is DewDrop's restart"`.

### Testing with a fake Steam folder

Debug builds read the environment variable `DEWDROP_FAKE_STEAM` (see `ProfileStart.cs`). When it names a folder, DewDrop treats that folder as Steam's folder, and the `localconfig.vdf` file in it as your Steam account's settings. As the game closes, DewDrop changes that file instead. It looks for Steam as `steam.exe` in the fake folder, which isn't running, so the real Steam is never closed and its settings are never touched. A restart that DewDrop starts keeps the variable, because the game starts it.

Two things still happen for real:

* `steam on` and `steam off` save the setting in `config.json`, as ticking the row does.
* With the setting on, DewDrop links your profile into `Selected Mods` as the game closes, as **Apply & Restart** does.

You need Windows and the Steam copy of the game: the game's folder path has to contain `steamapps`. Otherwise `steamProblem` says why the setting can't be used.

1. **Note your setting.** Open `Mods/DewDrop/config.json` and write down the value of `StartInProfileFromSteam`, so you can put it back at the end. DewDrop may change it as soon as the game starts with the fake folder.
2. **Make the fake folder.** In PowerShell:

   ```powershell
   $fake = 'C:\dewdrop-fake-steam'
   New-Item -ItemType Directory -Force $fake | Out-Null
   Set-Content -LiteralPath "$fake\localconfig.vdf" -Value '"UserLocalConfigStore" { "Software" { "Valve" { "Steam" { "apps" { } } } } }'
   ```

   An empty `apps` block reads as a game with no launch option yet. DewDrop adds the game's entry when it writes the file.
3. **Start the game from the same window,** with the variable set. Use your own game folder:

   ```powershell
   $env:DEWDROP_FAKE_STEAM = $fake
   & 'C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\StardewModdingAPI.exe'
   ```

   The variable only lasts while that PowerShell window is open, so start the game from it every time.
4. **Turn it on.** On the title screen, send `steamcheck`. The reply starts with `problem=none`. Then send `steam on`, which replies `steam wanted True, pending True`, and `quit`.
5. **Check the file.** A moment after the game closes, the fake `localconfig.vdf` has a `LaunchOptions` line that starts SMAPI with `--mods-path` pointing at `Selected Mods`. The old file is saved next to it as `localconfig.vdf.dewdrop-backup`. `relaunch.log`, in `%AppData%\Stardrop\Data\Cache\DewDrop`, says what the script did.
6. **Start the game again** from the same window and open the **Mods** menu. A message says "Steam now starts the game in your profile.", and `steamcheck` shows `on=True pending=False`. You started `StardewModdingAPI.exe` yourself, so this launch still loads your whole `Mods` folder. To try a launch in your profile, add the part Steam would add, `--mods-path "$env:APPDATA\Stardrop\Data\Selected Mods"`, after the `.exe` path. Then `status` shows `kind=Profile`.
7. **Turn it off.** Send `steam off` and `quit`. The `--mods-path` part goes from the fake file, and the rest stays. At the next start, the message says "Steam no longer starts the game in your profile."
8. **Try a change made meanwhile.** Send `steam on`. While the game still runs, change the text after `LaunchOptions` in the fake file with a text editor, then `quit`. DewDrop leaves the file alone, and at the next start the menu says "Steam's launch option wasn't changed: Stardew Valley's launch options changed in Steam while the game was running, so DewDrop left them alone."
9. **Try an option DewDrop can't add to.** Change the `LaunchOptions` value in the fake file to `-novid %command%` and send `steamcheck`. `problem=` says the launch option starts another program, and the row on the **Account** tab is grayed out.
10. **Clean up.** Close the game and the PowerShell window. Set `StartInProfileFromSteam` in `config.json` back to the value you noted. `null` is also safe: DewDrop then follows what the real Steam already does. Otherwise, the next time you quit the game without the fake folder, DewDrop may close the real Steam and change its launch option to match the setting.

The SMAPI console command `dewdrop_status` prints the session kind, the **Ask to load my profile** setting and what happened on the title screen, and the state of **Start in my profile from Steam** with Steam's launch option. It works in Release builds too. See [Console commands](../reference/console-commands.md).

## Example drivers

{% tabs %}
{% tab title="Windows (PowerShell)" %}
```powershell
$bridge = Join-Path ([IO.Path]::GetTempPath()) 'dewdrop-bridge'

function Invoke-DewDrop([string[]]$Commands, [string]$Name = ('job-' + (Get-Date -Format 'HHmmssfff')), [int]$TimeoutSec = 90) {
  $in = Join-Path $bridge 'in'; $out = Join-Path $bridge 'out'
  New-Item -ItemType Directory -Force $in, $out | Out-Null
  $reply = Join-Path $out "$Name.out"; Remove-Item $reply -ErrorAction SilentlyContinue
  $tmp = Join-Path $in "$Name.tmp"
  [IO.File]::WriteAllLines($tmp, $Commands)
  Move-Item $tmp (Join-Path $in "$Name.cmd")          # hand the file over in one step
  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $deadline) {
    if ((Test-Path $reply) -and ((Get-Content $reply) -contains 'DONE')) { return Get-Content $reply }
    Start-Sleep -Milliseconds 200
  }
  throw "Timed out waiting for $reply"
}

# A typical screenshot session at a 1920x1080 UI:
Invoke-DewDrop @('skipintro', 'window 1920 1080', 'uiscale 1.0', 'wait 3000', 'status')
Invoke-DewDrop @('open', 'wait 4000', 'status', 'shot C:/dewdrop-shots/installed.png')
# Click tabs and buttons using positions measured from installed.png, for example:
Invoke-DewDrop @('click <profilesTabX> 170', 'wait 500', 'shot C:/dewdrop-shots/profiles.png')
Invoke-DewDrop @('click 400 256', 'type My Profile', 'click <createX> 256', 'wait 500', 'shot C:/dewdrop-shots/profile-created.png')
Invoke-DewDrop @('back', 'wait 3000', 'quit')
```

Replace `<profilesTabX>` and `<createX>` with x positions you measured from a screenshot.
{% endtab %}

{% tab title="macOS and Linux (bash)" %}
```bash
bridge="${TMPDIR:-/tmp}/dewdrop-bridge"

# Usage: dewdrop NAME COMMAND...
dewdrop() {
  local name="$1"; shift
  mkdir -p "$bridge/in" "$bridge/out"
  rm -f "$bridge/out/$name.out"
  printf '%s\n' "$@" > "$bridge/in/$name.tmp"
  mv "$bridge/in/$name.tmp" "$bridge/in/$name.cmd"   # hand the file over in one step
  until grep -qx 'DONE' "$bridge/out/$name.out" 2>/dev/null; do sleep 0.2; done
  cat "$bridge/out/$name.out"
}

dewdrop job1 skipintro 'window 1920 1080' 'uiscale 1.0' 'wait 3000' status
```

This version waits forever if the game isn't running; press **Ctrl+C** to stop it. The bridge hasn't been tested on macOS or Linux.
{% endtab %}
{% endtabs %}

Screenshots for these docs go through one more step: the art pipeline puts each one in the wooden frame you see throughout this guide. See [Docs and art style](docs-and-art.md#screenshots).
