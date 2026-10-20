---
description: "Every option the DewDrop installer accepts on Windows, macOS and Linux, what it answers for you with -Yes, how it sets up Steam on Linux and the Steam Deck, and its exit codes."
icon: terminal
---

# Installer command-line options

You don't need any of this for a normal install. Paste the one line, or double-click the downloaded installer, and answer its questions (see [Install with the installer](../getting-started/installing/installer.md)). Options are for choosing the game folder yourself, previewing what the installer would do, installing without questions, or setting up Steam on Linux.

## Running the installer with options

Each example below runs a preview with `-DryRun` (`--dry-run`). Swap in the [options](#options) you want.

{% tabs %}
{% tab title="Windows" %}
**With the one-line installer**, wrap it like this in PowerShell and put your options at the end:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1))) -DryRun
```

**With the downloaded installer**, open a Command Prompt in the extracted `DewDrop <version> installer` folder: click File Explorer's address bar, type `cmd` and press **Enter**. Then add your options after the file name:

```cmd
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
**With the one-line installer**, put your options after `bash -s --`:

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --dry-run
```

**With the downloaded installer**, open a terminal in the extracted `DewDrop <version> installer` folder and add your options after the file name:

```bash
bash "Install on Linux.sh" --dry-run
bash "Install on macOS.command" --dry-run
```

Both pass your options on to `internal/install.sh`, which you can also run directly:

```bash
bash internal/install.sh --dry-run
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
| | `--steam-setup` | Linux: set Stardew Valley's launch option in Steam to DewDrop's launcher, as on a Steam Deck. Ignored on macOS. See [Steam on Linux and the Steam Deck](#steam-on-linux-and-the-steam-deck). |
| | `--no-steam-setup` | Steam Deck: leave Steam's launch option alone. |
| `-StartInProfile` | `--start-in-profile` | Answer yes to starting the game in your DewDrop profile, without asking. See [Starting in your profile](#starting-in-your-profile). |
| `-NoStartInProfile` | `--no-start-in-profile` | Answer no to starting the game in your DewDrop profile, without asking. On Windows, this also takes DewDrop's `--mods-path` out of Steam's launch option if it's there. On Linux, it removes the launcher's `start-in-profile` file. |
| `-NoShortcut` | | Don't offer the desktop shortcut. |
| `-NoLaunch` | `--no-launch` | Don't offer to start the game at the end. |
| | `--help` or `-h` | Show a short usage text and exit. |

On macOS and Linux, the Steam options only matter when the installer sets up Steam: on a Steam Deck, or with `--steam-setup`.

A few differences between the two installers:

* **A `-ModSource` path that doesn't exist** stops the Windows installer with an error. The macOS and Linux installer ignores a `--mod-source` path that doesn't exist and uses its bundled copy, so check the path.
* **`--skip-smapi` with an unknown SMAPI version:** on macOS and Linux, if the installer can't tell which SMAPI you have (for example, a very old SMAPI 3), it carries on and reports "(unknown version)".
* **Steam:** on Windows, the installer points Steam at SMAPI. On a Steam Deck (and on Linux with `--steam-setup`), it points Steam at DewDrop's launcher instead. On macOS it doesn't change Steam. The shortcut options are Windows only.
* **An option it doesn't know:** the macOS and Linux installer says "Unknown option" and stops. On Windows, PowerShell refuses to start the script (see [Exit codes](#exit-codes)).

`-FromLauncher`, `-Elevated` and `--from-launcher` are set by the installer's own launchers. You don't need them.

If you run `install.ps1` or `install.sh` on its own, without the rest of the installer folder, it downloads the latest DewDrop release from GitHub. The one-line installer always works this way.

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
| macOS with an Apple chip: Install Rosetta now? | Yes. macOS still asks for your Mac password. |
| Your DewDrop is newer than the installer's. Replace it with the older one? | No. It keeps yours. |
| Turn the extra copies of DewDrop off? | Yes. |
| Windows: the game folder is protected. Restart as administrator? | Not offered. It stops and asks you to run it as administrator. |
| Windows, Steam: set the launch option? | Yes, which closes Steam. Add `-NoSteamLaunchOptions` to leave Steam alone. |
| Windows, Steam: the launch option starts SMAPI from another folder. Point it at this game's SMAPI? | Yes. Other options in it are kept; see [Starting in your profile](#starting-in-your-profile) for `--mods-path`. |
| Windows, Steam: replace other launch options? | No. It shows the manual steps, but doesn't copy the line to your clipboard. |
| Windows, Steam: start the game in your DewDrop profile when you press Play in Steam? | Yes. Add `-NoStartInProfile` for no. |
| Steam Deck: your Nexus Mods API key | Skipped, unless `DEWDROP_NEXUS_KEY` gives one (see [Steam on Linux and the Steam Deck](#steam-on-linux-and-the-steam-deck)). |
| Steam Deck or `--steam-setup`: add DewDrop's launcher to launch options you already have (they use `%command%`)? | Yes. |
| Steam Deck or `--steam-setup`: the launch option starts SMAPI directly. Use DewDrop's launcher instead? | Yes. SMAPI's own options in it, such as `--mods-path`, go; the rest is kept. |
| Steam Deck or `--steam-setup`: replace launch options you already have (they don't use `%command%`)? | No. It shows what to enter yourself. |
| Steam Deck or `--steam-setup`: start the game in your DewDrop profile when you press Play in Steam? | Yes, unless it's already on, or you turned it off in the game: then it keeps that without asking. Add `--no-start-in-profile` for no. |
| Windows: add the desktop shortcut? | Yes. Add `-NoShortcut` if you don't want it. |
| Windows, Xbox app: swap in SMAPI's copy of the game's exe? | Yes. |
| Start Stardew Valley now? | Yes. Add `-NoLaunch` (`--no-launch`) if you don't want that. On a Steam Deck it isn't asked: the game is for Game Mode. |

On macOS and Linux, the installer also picks these answers when it isn't running in a terminal, for example when its input comes from another program.

The Steam launch option is only changed for the Steam account that's signed in (or was signed in most recently). Other accounts on the PC are left alone.

## Starting in your profile

This section is about the Steam version on Windows. On Linux and the Steam Deck, the installer asks the same question, but the answer works through DewDrop's launcher; see [Steam on Linux and the Steam Deck](#steam-on-linux-and-the-steam-deck).

When the installer sets up Steam's launch option for SMAPI, it also asks:

```text
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

On a Steam Deck, the installer sets Stardew Valley's launch option in Steam to DewDrop's launcher, so the game starts with SMAPI in Game Mode and DewDrop can restart it there. It counts as a Steam Deck when the system is SteamOS, or the hardware is a Steam Deck running another Linux system. On other Linux systems it only does this with `--steam-setup`, and `--no-steam-setup` skips it on a Deck. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md) for what the launcher does.

The installer asks everything first, in this order:

1. **Your Nexus Mods API key** (Steam Deck only), so you don't have to type it later with the on-screen keyboard: "API key (or just press Enter to skip):". It isn't asked when a key is already saved. DewDrop logs in with it the next time the game starts; see [Where DewDrop keeps files](file-locations.md#the-shared-data-folder).
2. **Launch options you already have**, if any:
   * Ones that use `%command%`, such as `gamemoderun %command%`, are kept, with the launcher just before `%command%`. The installer asks "Add DewDrop's launcher to them? (they become: ...)".
   * One that starts SMAPI directly gets the launcher in SMAPI's place. What comes before SMAPI (such as `gamemoderun`) and after `%command%` is kept. SMAPI's own options, such as `--mods-path`, go, since the launcher picks the mods folder itself. The installer names them and asks "Use DewDrop's launcher instead? (they become: ...)". An option that's only SMAPI and `%command%` is replaced without asking.
   * Ones without `%command%` are only replaced if you answer yes to "Replace them with DewDrop's launcher?" (the recommended answer is no). Otherwise it shows what to enter yourself.
3. **Starting in your profile:** "Start the game in your DewDrop profile when you press Play in Steam?" If **Start in my profile from Steam** is already on, or you turned it off in the game, the installer keeps that and doesn't ask. The answer is saved for the launcher at once. The launcher only starts the game in your profile once DewDrop has linked it, so the first start loads your whole `Mods` folder.

Then it changes Steam:

* **Steam has to be closed** to change its settings, so the installer closes it (`steam -shutdown`), changes the launch option, and starts Steam again straight away. On a Steam Deck, the controls and the on-screen keyboard stop while Steam is closed, which is why every question comes first. If Steam starts again by itself, as a SteamOS service can, the installer stops that service instead, and starts it again afterwards.
* **Only the signed-in Steam account** is changed (the one marked as most recent in Steam's `loginusers.vdf`), and Steam's settings file is backed up first.
* **Launch options Steam saves as it closes** are left alone, since the installer can't ask about them while Steam is closed: "Steam saved different launch options as it closed (...), so I left them alone. Run the installer again to add DewDrop's launcher to them."
* **Running it again** is safe. When Steam already uses the launcher, the installer says "Steam already starts the game with DewDrop's launcher." and doesn't close Steam.
* **If Steam couldn't be set up on a Deck**, the installer's last message says so instead of sending you back to Game Mode. The messages above it say why; fix that and run the installer again.
* **Only the Steam version** of the game can use the launcher, and **Flatpak and Snap Steam** aren't supported for this yet.

With the launcher, SMAPI has no terminal window, in Game Mode or Desktop Mode, so you can't type [console commands](console-commands.md). The SMAPI log has what SMAPI and DewDrop said.

To hand the installer your API key without the question, put it in the `DEWDROP_NEXUS_KEY` environment variable. It's only used on a Steam Deck when no key is saved yet, and it works with `--yes` too. Your shell may keep the command in its history, so the question is the safer way.

```bash
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | DEWDROP_NEXUS_KEY="<your key>" bash
```

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

* **`install.sh`** (in the repository root, for `curl ... | bash`) downloads `internal/install.sh` from the `master` branch into a temporary folder and runs it, with its questions read from your terminal rather than the pipe. With no terminal at all, the installer takes the recommended answers. With no bundled copy next to it, `internal/install.sh` then downloads the latest DewDrop release. In Git Bash on Windows it downloads and runs `install.ps1` instead. If the download fails, it says "Couldn't download the DewDrop installer from ... Check your internet connection."
* **`install.ps1`** (in the repository root, for `irm ... | iex`) downloads `internal\install.ps1` from the `master` branch to `%TEMP%\DewDrop-install.ps1` and runs it with Windows PowerShell, even when pasted into PowerShell 7. It keeps that file afterwards, because the installer may restart itself from it as administrator. It never closes the PowerShell window it was pasted into. On macOS or Linux, it shows the `curl` line to use instead.
* **`Install on Windows.bat`** refuses to run from inside the zip (Windows would run it from a temporary folder without the other files), checks that `internal\install.ps1` is there, and runs it with Windows PowerShell.
* **`Install on macOS.command`** clears macOS's download warning flag from the installer folder, then runs `internal/install.sh`.
* **`Install on Linux.sh`** runs `internal/install.sh` in the terminal you started it from. When you double-click it, it opens the first terminal it finds out of: xdg-terminal-exec, Ptyxis, GNOME Console, GNOME Terminal, Konsole, Xfce Terminal, MATE Terminal, Terminator, Tilix, kitty, Alacritty, WezTerm, foot, x-terminal-emulator and xterm. Options are only passed on when you start it from a terminal.

## Files the installer uses

| What | Where |
| --- | --- |
| Log, replaced on each run | Windows: `%TEMP%\DewDrop-installer.log`, a full copy of everything the installer printed. macOS and Linux: `$TMPDIR/DewDrop-installer.log` (or `/tmp/DewDrop-installer.log`), with the installer's steps, warnings and errors, but not your answers or SMAPI's own output. |
| The installer the one line downloads | Windows: `%TEMP%\DewDrop-install.ps1`, kept. macOS and Linux: a `dewdrop-get.<id>` folder in `$TMPDIR` (or `/tmp`), deleted at the end. |
| Work folder for downloads, deleted at the end | Windows: `%TEMP%\dewdrop-installer-<id>`. macOS and Linux: `~/.cache/dewdrop-installer.<id>` (or under `$XDG_CACHE_HOME`), not `/tmp`, because some systems don't allow running programs from `/tmp`. |
| Backup of Steam's settings (Windows, and Linux when it sets up Steam) | `localconfig.vdf.dewdrop-backup-<date>-<time>`, next to `localconfig.vdf` in `Steam\userdata\<account>\config` (on Linux, `userdata/<account>/config` in Steam's folder). |
| Your answer about starting in your profile (Steam) | Windows: `StartInProfileFromSteam` in DewDrop's `config.json`, in its mod folder. Your other settings in the file are kept. Linux and the Steam Deck: the `start-in-profile` file next to DewDrop's launcher (there for a yes), and `StartInProfileFromSteam` in `config.json` when the file already has that setting; otherwise the game picks the answer up from the launcher's file at its first start. |
| DewDrop's Steam launcher (Steam Deck, or Linux with `--steam-setup`) | `steam-launch.sh` in `~/.config/Stardrop/Data/Cache/DewDrop`, copied from `assets/steam-launch.sh` in DewDrop's mod folder. See [Where DewDrop keeps files](file-locations.md#the-shared-data-folder). |
| Your Nexus Mods API key (Steam Deck) | `nexus-key.txt` in the same folder, until DewDrop has saved the key to your login, or Nexus Mods turns it down. Only you can read it. |
| Desktop shortcut (Windows) | `Stardew Valley (SMAPI)` on your desktop. When the installer runs as administrator, it goes on the desktop for all users instead. |
| Turned-off copies of DewDrop | Renamed to `.<folder name>-duplicate` in the same place. |
| Temporary folders during an update | `.dewdrop-installer-<id>` and `.dewdrop-old-<id>` next to your `DewDrop` folder, removed once the new copy is in place. |

SMAPI skips folders whose names start with a `.`, so the last two never load as mods.

On macOS and Linux, the downloads use `curl` (Ubuntu's snap `curl` works) or, failing that, `wget`. Zips are extracted with `unzip`, `python3` or `bsdtar`.

## Examples

**Windows, the one-line installer (PowerShell):**

```powershell
# Install without questions
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1))) -Yes

# Install into a specific game folder, and don't have Steam start the game in your DewDrop profile
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1))) -GamePath "D:\Games\Stardew Valley" -NoStartInProfile
```

**Windows, the downloaded installer (Command Prompt, in the installer folder):**

```cmd
REM Preview what the installer would do
"Install on Windows.bat" -DryRun

REM Install into a specific game folder without questions
"Install on Windows.bat" -GamePath "D:\Games\Stardew Valley" -Yes

REM Install without questions, but don't have Steam start the game in your DewDrop profile
"Install on Windows.bat" -Yes -NoStartInProfile

REM Update DewDrop only: leave SMAPI, Steam and the desktop alone, and don't start the game
"Install on Windows.bat" -SkipSmapi -NoSteamLaunchOptions -NoShortcut -NoLaunch
```

**macOS and Linux, the one-line installer (Terminal):**

```bash
# Preview what the installer would do
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --dry-run

# Linux: also set up Steam to start the game with DewDrop's launcher
curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --steam-setup
```

**macOS and Linux, the downloaded installer (Terminal, in the installer folder):**

```bash
# Install into the Steam copy on Linux without questions
bash "Install on Linux.sh" --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --yes

# Install into the GOG copy on macOS, and don't start the game
bash "Install on macOS.command" --game-path "/Applications/Stardew Valley.app" --yes --no-launch
```
