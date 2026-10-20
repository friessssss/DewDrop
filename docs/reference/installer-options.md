---
description: "Every option the DewDrop installer accepts on Windows, macOS and Linux, what it answers for you with -Yes, and its exit codes."
icon: terminal
---

# Installer command-line options

You don't need any of this for a normal install: double-click the installer and answer its questions (see [Install with the installer](../getting-started/installing/installer.md)). Options are for choosing the game folder yourself, previewing what the installer would do, or installing without questions.

## Running the installer with options

{% tabs %}
{% tab title="Windows" %}
Open a Command Prompt in the extracted `DewDrop <version> installer` folder: click File Explorer's address bar, type `cmd` and press **Enter**. Then add your options after the file name:

```batch
"Install on Windows.bat" -DryRun
```

The `.bat` passes your options on to `internal\install.ps1`, and at the end it waits for you to press **Enter**. For scripts, run `install.ps1` directly instead:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\internal\install.ps1 -DryRun
```

`-ExecutionPolicy Bypass` lets PowerShell run this one script without changing your PC's settings.

Put folder paths in quotes, and don't end them with `\`. Windows reads `\"` at the end of a path as a quote mark, so the rest of the line becomes part of the path.
{% endtab %}

{% tab title="macOS and Linux" %}
Open a terminal in the extracted `DewDrop <version> installer` folder and add your options after the file name:

```bash
bash "Install on Linux.sh" --dry-run
bash "Install on macOS.command" --dry-run
```

Both pass your options on to `internal/install.sh`, which you can also run directly: `bash internal/install.sh --dry-run`.

With the one-line installer, put your options after `bash -s --`:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --dry-run
```
{% endtab %}
{% endtabs %}

## Options

| Windows | macOS and Linux | What it does |
| --- | --- | --- |
| `-GamePath "<folder>"` | `--game-path <folder>` | Use this game folder instead of searching. See [Choosing the game folder](#choosing-the-game-folder). |
| `-ModSource "<folder or zip>"` | `--mod-source <folder or zip>` | Install DewDrop from this folder or DewDrop release zip instead of the copy bundled with the installer. It looks for DewDrop's `manifest.json` inside, including in subfolders, and stops if there isn't one. |
| `-Yes` | `--yes` or `-y` | Pick the recommended answer to every question. See [Automatic answers](#automatic-answers). |
| `-DryRun` | `--dry-run` | Show what would happen without changing anything. Lines starting with `(dry run) would` describe each change. It still looks for the game and asks its questions. |
| `-SkipSmapi` | `--skip-smapi` | Don't install or update SMAPI. If SMAPI is missing or older than 4.0, the installer stops instead, because DewDrop can't run without it. |
| `-NoSteamLaunchOptions` | | Don't change Steam's launch options or show how to set them. The desktop shortcut is offered instead. |
| | `--steam-setup` | Linux: set Stardew Valley's launch option in Steam to DewDrop's launcher, as on a Steam Deck. See [Steam on Linux and the Steam Deck](#steam-on-linux-and-the-steam-deck). |
| | `--no-steam-setup` | Steam Deck: leave Steam's launch option alone. |
| `-StartInProfile` | `--start-in-profile` | Answer yes to starting the game in your DewDrop profile, without asking. See [Starting in your profile](#starting-in-your-profile). |
| `-NoStartInProfile` | `--no-start-in-profile` | Answer no to starting the game in your DewDrop profile, without asking. On Windows, this also takes DewDrop's `--mods-path` out of Steam's launch option if it's there. |
| `-NoShortcut` | | Don't offer the desktop shortcut. |
| `-NoLaunch` | `--no-launch` | Don't offer to start the game at the end. |
| | `--help` or `-h` | Show a short usage text and exit. |

A few differences between the two installers:

* **A `-ModSource` path that doesn't exist** stops the Windows installer with an error. The macOS and Linux installer ignores a `--mod-source` path that doesn't exist and uses its bundled copy, so check the path.
* **`--skip-smapi` with an unknown SMAPI version:** on macOS and Linux, if the installer can't tell which SMAPI you have (for example, a very old SMAPI 3), it carries on and reports "(unknown version)".
* **Steam options:** on Windows, the installer points Steam at SMAPI. On a Steam Deck (and on Linux with `--steam-setup`), it points Steam at DewDrop's launcher instead. On macOS it doesn't change Steam. The shortcut options are Windows only.
* **An option it doesn't know:** the macOS and Linux installer says "Unknown option" and stops. On Windows, PowerShell refuses to start the script (see [Exit codes](#exit-codes)).

`-FromLauncher`, `-Elevated` and `--from-launcher` are set by the installer's own launchers. You don't need them.

If you run `install.ps1` or `install.sh` on its own, without the rest of the installer folder, it downloads the latest DewDrop release from GitHub.

## Choosing the game folder

* **Windows:** `-GamePath` must be the folder that contains `Stardew Valley.dll`, the same folder as the Stardew Valley game icon. For the Xbox app, that's the `Content` folder, for example `C:\XboxGames\Stardew Valley\Content`. Unlike the folder picker, `-GamePath` doesn't look in subfolders.
* **macOS and Linux:** `--game-path` can be the folder with `Stardew Valley.dll` in it, or the folder above it: the `.app` or Steam's **Browse local files** folder on macOS (it looks in `Contents/MacOS`), the GOG folder above `game`, or a folder with a `Stardew Valley` folder inside. A path starting with `~` works too.

If the installer can't find `Stardew Valley.dll` there, it stops and says so.

## Automatic answers

With `-Yes` (Windows) or `--yes` (macOS and Linux), the installer answers its own questions like this:

| Question | Answer |
| --- | --- |
| Which copy of the game do you play? | The first one in the list. Add `-GamePath` (`--game-path`) if you have more than one. |
| The game wasn't found | It stops. Add `-GamePath` (`--game-path`). |
| The game has never been started (no game data folder, including Flatpak and Snap Steam's on Linux). Continue anyway? | Yes. |
| The game is running | It waits up to 5 minutes for the game to close, then stops. |
| Linux: this is the Windows version of the game (Proton). Continue anyway? | No, so it stops. |
| Install or update SMAPI? | Yes. |
| macOS and Linux: SMAPI's version is unknown. Install the latest over it? | Yes. |
| Your DewDrop is newer than the installer's. Replace it with the older one? | No. It keeps yours. |
| Turn the extra copies of DewDrop off? | Yes. |
| Windows: the game folder is protected. Restart as administrator? | Not offered. It stops and asks you to run it as administrator. |
| Windows, Steam: set the launch option? | Yes, which closes Steam. Add `-NoSteamLaunchOptions` to leave Steam alone. |
| Windows, Steam: the launch option starts SMAPI from another folder. Point it at this game's SMAPI? | Yes. Other options in it are kept; see [Starting in your profile](#starting-in-your-profile) for `--mods-path`. |
| Windows, Steam: replace other launch options? | No. It shows the manual steps, but doesn't copy the line to your clipboard. |
| Windows, Steam: start the game in your DewDrop profile when you press Play in Steam? | Yes. Add `-NoStartInProfile` for no. |
| Steam Deck: add DewDrop's launcher to launch options you already have (they use `%command%`)? | Yes. |
| Steam Deck: replace launch options you already have (they don't use `%command%`)? | No. It shows what to enter yourself. |
| Steam Deck: start the game in your DewDrop profile when you press Play in Steam? | Yes. Add `--no-start-in-profile` for no. |
| Steam Deck: your Nexus Mods API key? | Not asked: it's skipped. |
| Windows: add the desktop shortcut? | Yes. Add `-NoShortcut` if you don't want it. |
| Windows, Xbox app: swap in SMAPI's copy of the game's exe? | Yes. |
| Start Stardew Valley now? | Yes (no on a Steam Deck, where the game is for Game Mode). Add `-NoLaunch` (`--no-launch`) if you don't want that. |

On macOS and Linux, the installer also picks these answers when it isn't running in a terminal, for example when its input comes from another program.

The Steam launch option is only changed for the Steam account that's signed in (or was signed in most recently). Other accounts on the PC are left alone.

## Starting in your profile

This only applies to the Steam version on Windows. When the installer sets up Steam's launch option for SMAPI, it also asks:

```
Start the game in your DewDrop profile when you press Play in Steam? [Y/n]
```

Yes is the same as ticking **Start in my profile from Steam** on the **Account** tab in the game. You can change it there any time. See [Start in your profile](../guides/start-in-your-profile.md).

* **The installer never adds `--mods-path` itself.** It saves your answer in DewDrop's `config.json`, as `StartInProfileFromSteam`. The next time you quit the game, DewDrop links your profile, Steam closes and opens again once, and the launch option gets `--mods-path`. That way, Steam is never pointed at links that are missing or out of date. After a yes, the installer says: "DewDrop finishes this the next time you quit the game: Steam closes and opens again once, and from then on it starts the game in your DewDrop profile."
* **If the launch option already points at `Selected Mods`,** the installer doesn't ask, and keeps it. If `Selected Mods` doesn't have DewDrop's links yet, it takes the `--mods-path` part out for now, and the game adds it back the next time you quit.
* **A `--mods-path` that points at some other folder is yours,** so the installer keeps it. If you answer yes, though, DewDrop replaces it with its own the next time you quit the game. Answer no, or add `-NoStartInProfile`, to keep it.
* **`-StartInProfile`** answers yes without asking. **`-NoStartInProfile`** answers no and takes out a `--mods-path` that points at `Selected Mods`. With `-Yes`, the answer is yes.
* The path to SMAPI is kept the way you typed it, as long as it's this game's SMAPI.

The question only comes up when the installer sets up Steam's launch option, so `-NoSteamLaunchOptions` skips it too.

## Steam on Linux and the Steam Deck

On a Steam Deck (SteamOS), the installer sets Stardew Valley's launch option in Steam to DewDrop's launcher, so the game starts with SMAPI in Game Mode and DewDrop can restart it there. On other Linux systems it only does this with `--steam-setup`; `--no-steam-setup` skips it on a Deck. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md) for what the launcher does.

* **Steam has to be closed** to change its settings, so the installer closes it (`steam -shutdown`) and starts it again at the end. If Steam starts again by itself straight away, as a SteamOS service can, the installer stops that service instead, and starts it again at the end.
* **Only the signed-in Steam account** is changed (the one marked as most recent in Steam's `loginusers.vdf`), and Steam's settings file is backed up first.
* **Launch options you already have** that use `%command%`, such as `gamemoderun %command%`, are kept: the launcher goes just before `%command%`. Ones without `%command%` are only replaced if you say so. A launch option that starts SMAPI directly is replaced, since the launcher starts SMAPI itself.
* **Starting in your profile:** the answer is saved for the launcher at once. The launcher only starts the game in your profile once DewDrop has linked it, so the first start loads your whole `Mods` folder.
* **Flatpak and Snap Steam** aren't supported for this yet.

## Exit codes

**Windows (`install.ps1`):**

| Code | Meaning |
| --- | --- |
| `0` | Finished. Also used when it handed over to a new administrator window. |
| `10` | Stopped with a message (a red `[x]` line). |

`Install on Windows.bat` passes `0` and `10` on. When the installer is started from the `.bat`, it first waits at "Press Enter to close this window". The `.bat` exits with `1` if you opened it from inside the zip or `internal\install.ps1` is missing. Any other code means PowerShell couldn't run the script at all: for example, your antivirus blocked it, a policy on your PC blocks scripts, or an option was misspelled. Then the `.bat` keeps its window open with "Oops! The installer couldn't start (error N)" and exits with that code.

**macOS and Linux (`install.sh`):**

| Code | Meaning |
| --- | --- |
| `0` | Finished, or `--help` was shown. |
| `1` | Stopped with a message (a red `[x]` line), or an option it doesn't know. |

When it's started from `Install on macOS.command` or `Install on Linux.sh` in a terminal, it first waits at "Press Enter to close this window". `Install on Linux.sh` also exits with `1` if it can't find a terminal to open.

## What the launchers do

* **`Install on Windows.bat`** refuses to run from inside the zip (Windows would run it from a temporary folder without the other files), checks that `internal\install.ps1` is there, and runs it with Windows PowerShell.
* **`Install on macOS.command`** clears macOS's download warning flag from the installer folder, then runs `internal/install.sh`.
* **`install.sh`** (in the repository root, for `curl ... | bash`) downloads `internal/install.sh` from the `master` branch into a temporary folder and runs it, with its questions read from your terminal rather than the pipe. With no bundled copy next to it, `internal/install.sh` then downloads the latest DewDrop release. In Git Bash on Windows it downloads and runs `install.ps1` instead.
* **`Install on Linux.sh`** runs `internal/install.sh` in the terminal you started it from. When you double-click it, it opens the first terminal it finds out of: xdg-terminal-exec, Ptyxis, GNOME Console, GNOME Terminal, Konsole, Xfce Terminal, MATE Terminal, Terminator, Tilix, kitty, Alacritty, WezTerm, foot, x-terminal-emulator and xterm. Options are only passed on when you start it from a terminal.

## Files the installer uses

| What | Where |
| --- | --- |
| Log, replaced on each run | Windows: `%TEMP%\DewDrop-installer.log`, a full copy of everything the installer printed. macOS and Linux: `$TMPDIR/DewDrop-installer.log` (or `/tmp/DewDrop-installer.log`), with the installer's steps, warnings and errors, but not your answers or SMAPI's own output. |
| Work folder for downloads, deleted at the end | Windows: `%TEMP%\dewdrop-installer-<id>`. macOS and Linux: `~/.cache/dewdrop-installer.<id>` (or under `$XDG_CACHE_HOME`), not `/tmp`, because some systems don't allow running programs from `/tmp`. |
| Backup of Steam's settings (Windows) | `localconfig.vdf.dewdrop-backup-<date>-<time>`, next to `localconfig.vdf` in `Steam\userdata\<account>\config`. |
| Your answer about starting in your profile (Windows, Steam) | `StartInProfileFromSteam` in DewDrop's `config.json`, in its mod folder. Your other settings in the file are kept. |
| Desktop shortcut (Windows) | `Stardew Valley (SMAPI)` on your desktop. When the installer runs as administrator, it goes on the desktop for all users instead. |
| Turned-off copies of DewDrop | Renamed to `.<folder name>-duplicate` in the same place. |
| Temporary folders during an update | `.dewdrop-installer-<id>` and `.dewdrop-old-<id>` next to your `DewDrop` folder, removed once the new copy is in place. |

SMAPI skips folders whose names start with a `.`, so the last two never load as mods.

On macOS and Linux, the downloads use `curl` (Ubuntu's snap `curl` works) or, failing that, `wget`. Zips are extracted with `unzip`, `python3` or `bsdtar`.

## Examples

**Windows (Command Prompt, in the installer folder):**

```batch
REM Preview what the installer would do
"Install on Windows.bat" -DryRun

REM Install into a specific game folder without questions
"Install on Windows.bat" -GamePath "D:\Games\Stardew Valley" -Yes

REM Install without questions, but don't have Steam start the game in your DewDrop profile
"Install on Windows.bat" -Yes -NoStartInProfile

REM Update DewDrop only: leave SMAPI, Steam and the desktop alone, and don't start the game
"Install on Windows.bat" -SkipSmapi -NoSteamLaunchOptions -NoShortcut -NoLaunch
```

**macOS and Linux (Terminal, in the installer folder):**

```bash
# Preview what the installer would do
bash "Install on Linux.sh" --dry-run

# Install into the Steam copy on Linux without questions
bash "Install on Linux.sh" --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --yes

# Install into the GOG copy on macOS, and don't start the game
bash "Install on macOS.command" --game-path "/Applications/Stardew Valley.app" --yes --no-launch
```
