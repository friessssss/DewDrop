---
description: "Build DewDrop from source, run the tests, try your changes in the game, and package a release."
icon: hammer
---

# Building from source

This page is for changing DewDrop's code or making a release. If you only want to play with DewDrop, [install it](../getting-started/installing/README.md) instead.

## What you need

* **The .NET 6 SDK or later.** Every project targets `net6.0`. To see which version you have:

  ```bash
  dotnet --version
  ```
* **Stardew Valley 1.6 or later, with SMAPI 4.** The mod builds against the game's and SMAPI's own files, so the build fails if either is missing. Only the [unit tests](#run-the-unit-tests) work without them. If you need SMAPI, see [About SMAPI](../getting-started/installing/smapi.md).
* **Git**, to get the code.
* **Node.js**, any current LTS version, to set the version, package a release or redraw the docs images. These scripts use only Node's built-in modules, so there's nothing to `npm install`.
* **For the installer's tests:** bash and `python3` on macOS and Linux, or Windows PowerShell on Windows.
* **An editor (optional).** In Visual Studio on Windows, **F5** starts the game through SMAPI with the debugger attached. The build package sets this up for you.

{% hint style="info" %}
The maintainer builds on Windows. CI builds DewDrop on Ubuntu, but against stand-in copies of the game's files (see [What CI checks](#what-ci-checks)). Building against a real game on macOS or Linux hasn't been tried yet.
{% endhint %}

## Get the code

```bash
git clone https://github.com/friessssss/DewDrop.git
cd DewDrop
git checkout development
```

## Tell the build where your game is

`DewDrop.Mod` uses SMAPI's build package, `Pathoschild.Stardew.ModBuildConfig` 4.3.2. It needs your game folder, called `GamePath`: the folder that holds `Stardew Valley.dll` and `StardewModdingAPI.dll`. It uses the first of these that applies:

1. `-p:GamePath="<folder>"` on the `dotnet build` command line.
2. A `stardewvalley.targets` file in your user folder (`%USERPROFILE%` on Windows, `~` on macOS and Linux).
3. The project's built-in default, `C:\Steam\steamapps\common\Stardew Valley`. This is the maintainer's Steam library, and it's only used if that folder exists.
4. Automatic detection of the usual Steam, GOG and Xbox app folders.

If your game isn't found automatically, create `stardewvalley.targets` in your user folder:

{% code title="stardewvalley.targets" %}
```xml
<Project>
  <PropertyGroup>
    <GamePath>D:\Games\Steam\steamapps\common\Stardew Valley</GamePath>
  </PropertyGroup>
</Project>
```
{% endcode %}

On macOS, `GamePath` is the `Contents/MacOS` folder inside the game's app. The DewDrop installer reads this file too when it searches for your game.

If the build says it "can't find your game folder" or that the folder "doesn't contain SMAPI", this path is wrong. SMAPI's guide to [custom game paths](https://smapi.io/package/custom-game-path) has more detail.

## Build and try it

1. Close the game. On Windows, SMAPI keeps `DewDrop.dll` locked while the game runs, so the build can't replace it.
2. Build the solution:

   ```bash
   dotnet build DewDrop.sln -c Release
   ```
3. Start the game through SMAPI as you normally would, and click **Mods** on the title screen.

Every build, Debug or Release, does two things after compiling:

* It **deploys** the mod into `<GamePath>/Mods/DewDrop`, with DewDrop's Steam launcher in `assets/steam-launch.sh`, so your next launch uses it.
* It writes a **zip** to `DewDrop.Mod/bin/<Debug or Release>/net6.0/DewDrop <version>.zip`.

Add `-p:EnableModDeploy=false` to skip the deploy, or `-p:EnableModZip=false` to skip the zip.

Two things to watch for:

* **A `Selected Mods` launch.** If SMAPI starts from Stardrop's `Selected Mods` folder on your computer, DewDrop needs a link there the first time. Turn it on once in Stardrop, or start the game once without `--mods-path`. After that, DewDrop keeps itself turned on in every profile.
* **No SMAPI console.** On Linux and the Steam Deck, when Steam starts the game through DewDrop's Steam launcher, there's no SMAPI console window. Read the log instead: `~/.config/StardewValley/ErrorLogs/SMAPI-latest.txt`.

## Run the unit tests

The `DewDrop.Core.Tests` project holds the unit tests for `DewDrop.Core`. They use xUnit and target `net6.0`. Run them from the repo folder:

```bash
dotnet test DewDrop.Core.Tests/DewDrop.Core.Tests.csproj
```

This builds only `DewDrop.Core` and its tests, so it works without Stardew Valley or SMAPI. `dotnet build DewDrop.sln` builds the tests too, but only `dotnet test` runs them. A pull request needs them to pass, and [CI](#what-ci-checks) runs them as well.

They check the parts of [starting in your profile](../guides/start-in-your-profile.md), installing and [uninstalling](../guides/installed.md#uninstall-a-mod) mods, and the [Steam Deck](../getting-started/steam-deck.md) support that don't need the game:

| File | What it checks |
| --- | --- |
| `SessionDiffTests.cs` | Whether the mods a normal launch loaded match what your profile would load: mods that are off in the profile, mods that didn't load, mods SMAPI refused, and a different copy of a mod. |
| `StartupStateTests.cs` | Reading and saving `startup.json`: the restart note, the list of mods DewDrop has seen, taking mods off that list, a damaged file, and a save that fails. |
| `DewDropPathsTests.cs` | Comparing folder paths, including a `Mods` folder reached through a symbolic link or a junction. |
| `SteamLaunchOptionTests.cs` | Reading Stardew Valley's launch option from Steam's `localconfig.vdf`, and adding or removing `--mods-path` the way SMAPI reads it: the rest of the option is kept, an older `--mods-path` is replaced, and an option that starts another program is left alone. |
| `RelauncherScriptTests.cs` | The PowerShell script that runs after the game closes, tried on a scratch `localconfig.vdf`: it writes the new launch option and keeps a backup, leaves an option that changed meanwhile alone, and adds the game's entry when Steam has none. Steam is never closed, because the test points the script at a `steam.exe` that isn't running. |
| `PendingRemovalTests.cs` | Uninstalls waiting in `pending-updates.json`: the hidden name a removal reserves, an uninstall and an update for the same folder, Undo, moving the mod aside as the game closes, deleting it at the next start, a folder that now holds another mod, and a blocked removal that's retried, then given up. |
| `RelauncherRemovalTests.cs` | The script finishing uninstalls after the game closes: it removes the mod and its link, leaves a folder that now holds another mod, and keeps a blocked uninstall while still applying updates. Also removing only the right links from `Selected Mods`. |
| `ModInstallerTests.cs` | The installer reporting every folder it wrote, and installing a mod again into a folder that's waiting to be uninstalled. |
| `ModSourcesTests.cs` | Reading and saving `mod-sources.json`: install records, smapi.io's Nexus IDs, a mod bundled in several downloads, and damaged or hand-edited files. |
| `InstalledFilesTests.cs` | Which of a mod's files its page marks **Installed**: the install record, then matching versions, and comparing versions written in different ways. |
| `UpdateCheckerTests.cs` | Reading smapi.io's answers: only mods with their own update keys are offered updates, and the Nexus ID is read for the rest. |
| `SteamDeckTests.cs` | Telling SteamOS or a Steam Deck (by its maker and model) apart from other Linux systems, also from inside Steam's runtime, and telling Game Mode from Desktop Mode. |
| `SteamLauncherTests.cs` | DewDrop's Steam launcher, run the way Steam runs it, with small stand-ins for SMAPI, the game and Steam's runtime: SMAPI takes the game's place, the profile is used only when it has DewDrop in it, a restart starts in the profile once, and Steam's command runs unchanged without SMAPI. Also keeping the launcher's copy up to date and runnable, finding Steam's folder, and a restart that waits for the game to close. |

`TestLog.cs` holds helpers the tests share. The test project also copies `DewDrop.Mod/assets/steam-launch.sh` next to the tests, so `SteamLauncherTests` run the same file the mod ships.

The tests only write files inside a `dewdrop-tests` folder in your temp folder, so they never touch your game, your mods or Steam. Some tests pass without checking anything where they can't run:

* `RelauncherScriptTests`, and the script tests in `RelauncherRemovalTests`, run Windows PowerShell, so they only check anything on Windows. If you change the script in `Relauncher.cs`, run the tests on Windows.
* `SteamLauncherTests`, apart from two, run the launcher with `/bin/sh`, so they only check anything on macOS and Linux. CI runs them on Ubuntu.
* Two tests in `PendingRemovalTests` that hold a file open, on macOS and Linux: only Windows refuses to move a folder while a file in it is open.
* The two link tests in `RelauncherRemovalTests`, on macOS and Linux when the test runner isn't allowed to make symbolic links.
* Two path tests: the symbolic-link test on Windows without Developer Mode, and the junction test on macOS and Linux.

## Debug vs Release

The only differences between the two are the [debug bridge](debug-bridge.md) and the `DEWDROP_FAKE_STEAM` variable for [testing with a fake Steam folder](debug-bridge.md#testing-with-a-fake-steam-folder). They sit inside `#if DEBUG` blocks in `DewDrop.Mod/Framework/DebugBridge.cs`, `DewDrop.Mod/ModEntry.cs` and `DewDrop.Mod/Framework/ProfileStart.cs`.

|  | Debug | Release |
| --- | --- | --- |
| Command | `dotnet build DewDrop.sln` (Debug is the default) | `dotnet build DewDrop.sln -c Release` |
| Debug bridge | Included | Left out |
| `DEWDROP_FAKE_STEAM` | Read | Ignored |
| Use it for | Screenshots and scripted testing | Playing, and every zip you share |

The bridge lets any program on your computer control the title screen, and that includes quitting the game. A Debug build is fine on your own machine while you test. Never give one to anyone else.

{% hint style="warning" %}
`dotnet build DewDrop.sln` with no `-c` makes a **Debug** build and deploys it to your `Mods` folder. When you've finished testing, build with `-c Release` so the copy you play with leaves the bridge out. To check which one is loaded, look in the SMAPI console or log: a Debug build logs `Debug bridge listening in <folder>` at startup.
{% endhint %}

## What CI checks

The **Build** workflow, `.github/workflows/build.yml`, runs on every push to `development` and every pull request. To run it by hand, open **Actions** > **Build** > **Run workflow** on GitHub. It has four jobs:

| Job | What it does |
| --- | --- |
| `build` | Builds `DewDrop.Core` on Ubuntu with .NET 6 and runs the [unit tests](#run-the-unit-tests). There, `RelauncherScriptTests` pass without running the script, and `SteamLauncherTests` really run the launcher. |
| `build-mod` | Checks that `manifest.json` and `DewDrop.Mod.csproj` have the same, valid version, then builds and packages `DewDrop.Mod` exactly as a release is, against metadata-only reference assemblies of the game and SMAPI (the runner has no game). It attaches both zips to the run as **DewDrop-zips**. See [Releasing](releasing.md#how-ci-builds-the-mod-without-the-game). |
| `installer-unix` | Runs on Ubuntu and on macOS, which ships bash 3.2. It checks the syntax of `installer/internal/install.sh`, both launchers and the one-line `install.sh`, then runs `installer/tests/install-sh.test.sh`. That also pipes the one-line installer into bash against this checkout. |
| `installer-windows` | Checks that both `install.ps1` files (the installer and the one-line installer) parse in Windows PowerShell and are plain ASCII, then runs `installer/tests/install-ps1.test.ps1`. That also runs the one-line installer against this checkout. |

Every pull request also gets two **GitBook (./docs)** checks, which link to the changed pages in GitBook's editor and to a preview of the site.

CI can't play the game, so still test your change in the game before you open a pull request. Pull requests into `master` also run a `version` check, and merging one publishes a release: see [Releasing](releasing.md).

## Packaging a release

CI builds and publishes releases (see [Releasing](releasing.md)) with the same script you can run yourself, for example to test the installer zip:

1. To change the version, run:

   ```bash
   node tools/bump-version.mjs patch
   ```

   Use `minor`, `major` or an exact version such as `1.0.0-beta.1` instead of `patch` as needed. With no argument, the script prints the current version. It sets `DewDrop.Mod/manifest.json` and `DewDrop.Mod/DewDrop.Mod.csproj` together, and the packaging script stops if they differ.
2. Run the packaging script from the repo folder:

   ```bash
   node tools/package-release.mjs
   ```

The script:

* builds `DewDrop.Mod` in **Release** with `-p:EnableModDeploy=false`, so your game's `Mods` folder isn't touched;
* takes the mod zip from `DewDrop.Mod/bin/Release/net6.0/` and checks the Release `DewDrop.dll` the zip was built from. It stops if that DLL is missing, more than a minute older than the zip, or contains the debug bridge;
* writes two files to `artifacts/release/<version>/`, a folder git ignores;
* prints each file's path, size and SHA-256 hash.

Add `--no-build` to reuse the Release zip you already built. The fresh Release `DewDrop.dll` must still be next to it, or the script stops. Add `--game-path=<folder>` to build against another game folder than the one the project finds, such as the reference assemblies CI uses.

| File | What's inside | Who it's for |
| --- | --- | --- |
| `DewDrop-<version>.zip` | The mod zip exactly as the build made it: a `DewDrop/` folder with `manifest.json`, `DewDrop.dll`, `DewDrop.Core.dll`, `DewDrop.Core.pdb`, `SharpCompress.dll`, `ZstdSharp.dll` and `assets/steam-launch.sh`. | [Installing by hand](../getting-started/installing/manual.md) and other mod managers. |
| `DewDrop-<version>-installer.zip` | A `DewDrop <version> installer/` folder with the three launchers (`Install on Windows.bat`, `Install on macOS.command`, `Install on Linux.sh`), `README.txt`, and `internal/` holding `install.ps1`, `install.sh` and the mod zip as `DewDrop.zip`. | [The installer](../getting-started/installing/installer.md). |

<figure><img src="../.gitbook/assets/diagram-install-paths.png" alt="Diagram comparing the two ways to install DewDrop side by side: running the installer, or dragging the DewDrop folder into the Mods folder by hand"><figcaption><p>Each zip serves one of the two ways to install.</p></figcaption></figure>

The mod goes inside the installer as a zip, not a folder. If someone extracts the whole installer into their `Mods` folder by mistake, SMAPI won't find a second copy of DewDrop to load.

Don't count on `assets/steam-launch.sh` being executable in the mod zip: SMAPI's build package makes that zip, and the packaging script copies it unchanged without setting any permissions. That's fine: the installer and DewDrop copy the launcher to DewDrop's data folder and make that copy executable (`chmod 755`).

### Why the script writes its own zips

The script builds the installer zip byte by byte rather than with `Compress-Archive` or Explorer. That way it controls three things:

* **Unix permissions.** Each entry is marked as made on Unix. `Install on macOS.command`, `Install on Linux.sh` and `internal/install.sh` get mode `755` (executable), and every other file gets `644`. Unzip tools on macOS and Linux restore those modes, so the launchers work with a double-click. Zips made by Windows tools carry no Unix modes, and double-clicking a launcher from one of them fails.
* **Line endings.** The `.bat`, `README.txt` and `install.ps1` get Windows (CRLF) line endings. The `.command` and `.sh` files get Unix (LF) line endings, whatever your git checkout used. `.gitattributes` keeps `.bat`, `.sh` and `.command` files the same way in git checkouts.
* **Plain ASCII.** The script stops if `install.ps1` contains any non-ASCII character. Windows PowerShell 5.1 reads a script without a byte-order mark as ANSI, which garbles anything else.

### Publishing

CI publishes releases when `development` is merged into `master`: see [Releasing](releasing.md). It keeps the conventions the installer and the update check rely on:

* The plain zip is named `DewDrop-<version>.zip`. When `install.ps1` or `install.sh` runs without a bundled copy, as the one-line installers always do, it downloads the mod from the latest release and looks for an asset with that name.
* Releases are normal releases, not drafts. GitHub's "latest release", which the installer asks for, skips drafts and pre-releases.
* The tag is the version number, the same as `Version` in `manifest.json` (for example `0.2.0`). DewDrop's manifest has the update key `GitHub:friessssss/DewDrop`, so SMAPI and DewDrop's own update check (both through smapi.io) compare that tag with installed versions. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

## Testing the installer

The installer has tests of its own, which CI also runs. They work on fake game folders (and, for `install.sh`, a throwaway home folder and a fake Steam), and never touch your real game or Steam. If you change the installer, run them before you open a pull request.

{% tabs %}
{% tab title="Windows" %}
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\tests\install-ps1.test.ps1
```
{% endtab %}

{% tab title="macOS and Linux" %}
```bash
bash installer/tests/install-sh.test.sh
```
{% endtab %}
{% endtabs %}

### Try the installer on your own build

You can run the installer straight from the repo against the zip your last Release build made. `-DryRun` and `--dry-run` show what would happen without changing anything. Remove that flag to install for real.

{% tabs %}
{% tab title="Windows" %}
```powershell
$zip = (Get-ChildItem 'DewDrop.Mod\bin\Release\net6.0\DewDrop *.zip' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName
powershell -NoProfile -ExecutionPolicy Bypass -File installer\internal\install.ps1 -ModSource $zip -DryRun
```
{% endtab %}

{% tab title="macOS and Linux" %}
```bash
zip="$(ls -t DewDrop.Mod/bin/Release/net6.0/DewDrop\ *.zip | head -n 1)"
bash installer/internal/install.sh --mod-source "$zip" --dry-run
```
{% endtab %}
{% endtabs %}

Always pass `-ModSource` or `--mod-source` here, with a mod folder or a zip. Without it, a script run from the repo has no bundled copy, so it downloads the latest GitHub release instead of testing your build. If the path doesn't exist, `install.ps1` stops, but `install.sh` downloads the release too, so check that the output doesn't say "No bundled copy of DewDrop". The other options are listed in [Installer command-line options](../reference/installer-options.md).

### Try the one-line installers

The one-line installers, `install.ps1` and `install.sh` in the repo root, download `installer/internal/install.ps1` or `install.sh` from `master`. To make them use your checkout instead, set `DEWDROP_INSTALLER_BASE`. They then run your installer the way the one-liner would. A one-liner has no bundled copy of DewDrop either, so pass the `$zip` from the previous step:

{% tabs %}
{% tab title="Windows" %}
```powershell
$env:DEWDROP_INSTALLER_BASE = "$PWD\installer\internal"
& ([scriptblock]::Create((Get-Content .\install.ps1 -Raw))) -ModSource $zip -DryRun
Remove-Item env:DEWDROP_INSTALLER_BASE
```

`install.ps1` takes a URL or a folder here.
{% endtab %}

{% tab title="macOS and Linux" %}
```bash
DEWDROP_INSTALLER_BASE="file://$PWD/installer/internal" bash -s -- --mod-source "$zip" --dry-run < install.sh
```

`install.sh` needs a URL here, so a folder is written as a `file://` URL.
{% endtab %}
{% endtabs %}

### Try the Steam Deck setup

On a Steam Deck, `install.sh` sets up Steam by default. On other Linux it only does with `--steam-setup`, and only for the Steam version of the game. Either way it closes the real Steam to change its settings, so try it on your own PC with `--dry-run`, which prints the launch option it would set and never closes Steam:

```bash
bash installer/internal/install.sh --mod-source "$zip" --steam-setup --dry-run
```

The installer's tests use these variables to stand in for a Steam Deck. You can use them too:

| Variable | What it does |
| --- | --- |
| `DEWDROP_OS_RELEASE` | A file read instead of `/etc/os-release`. One with the line `ID=steamos` makes the installer treat the computer as a Steam Deck. |
| `DEWDROP_DMI` | A folder read instead of `/sys/devices/virtual/dmi/id`. A `sys_vendor` file saying `Valve` and a `product_name` file saying `Jupiter` or `Galileo` make it a Steam Deck too. |
| `DEWDROP_NEXUS_KEY` | On a Steam Deck, a Nexus Mods API key to save for DewDrop, as if it had been pasted at the installer's question. It's ignored when a key is saved already. |

To try DewDrop's side of the launcher without a Steam Deck, see [Testing with a fake Steam folder](debug-bridge.md#testing-with-a-fake-steam-folder). To try the whole thing on a real Deck, follow [Testing on a Steam Deck](steam-deck-testing.md).
