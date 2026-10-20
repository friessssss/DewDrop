---
description: "Build DewDrop from source, try your changes in the game, and package a release."
icon: hammer
---

# Building from source

This page is for changing DewDrop's code or making a release. If you only want to play with DewDrop, [install it](../getting-started/installing/README.md) instead.

## What you need

* **The .NET 6 SDK or later.** Every project targets `net6.0`. Run `dotnet --version` to check what you have.
* **Stardew Valley 1.6 or later with SMAPI 4 installed.** The build compiles against the game's and SMAPI's own files, so it fails if either is missing. Only the [unit tests](#run-the-unit-tests) work without them. If you need SMAPI, see [About SMAPI](../getting-started/installing/smapi.md).
* **Git**, to get the code.
* **Node.js**, any current LTS version. You only need it to package a release or redraw the docs images. Both scripts use only Node's built-in modules, so there is nothing to `npm install`.
* **An editor (optional).** In Visual Studio on Windows, pressing **F5** starts the game through SMAPI with the debugger attached. The build package sets this up for you.

{% hint style="info" %}
The maintainer builds on Windows. The build package also supports macOS and Linux, but building DewDrop there hasn't been tested yet.
{% endhint %}

## Get the code

```bash
git clone https://github.com/friessssss/DewDrop.git
cd DewDrop
git checkout development
```

## Tell the build where your game is

`DewDrop.Mod` uses SMAPI's build package, `Pathoschild.Stardew.ModBuildConfig` 4.3.2. The package needs your game folder, called `GamePath`: the folder that holds `Stardew Valley.dll` and `StardewModdingAPI.dll`. It uses the first of these that applies:

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

* It **deploys** the mod into `<GamePath>/Mods/DewDrop`, so your next launch uses it.
* It writes a **zip** to `DewDrop.Mod/bin/<Debug or Release>/net6.0/DewDrop <version>.zip`.

Add `-p:EnableModDeploy=false` to skip the deploy, or `-p:EnableModZip=false` to skip the zip.

If SMAPI starts from Stardrop's `Selected Mods` folder on your computer, DewDrop needs a link there the first time. Turn it on once in Stardrop, or launch once without `--mods-path`. After that, DewDrop keeps itself turned on in every profile.

## Run the unit tests

The `DewDrop.Core.Tests` project holds unit tests for `DewDrop.Core`. They use xUnit and target `net6.0`, like the rest of DewDrop. Run them from the repo folder:

```bash
dotnet test DewDrop.Core.Tests/DewDrop.Core.Tests.csproj
```

This builds only `DewDrop.Core` and its tests, so it works without Stardew Valley or SMAPI. `dotnet build DewDrop.sln` builds the tests too, but only `dotnet test` runs them. A pull request needs them to pass, and [CI](#what-ci-checks) runs them as well.

They check the parts of [starting in your profile](../guides/start-in-your-profile.md) that don't need the game:

| File | What it checks |
| --- | --- |
| `SessionDiffTests.cs` | Whether the mods a normal launch loaded match what your profile would load: mods that are off in the profile, mods that didn't load, mods SMAPI refused, and a different copy of a mod. |
| `StartupStateTests.cs` | Reading and saving `startup.json`: the restart note, the list of mods DewDrop has seen, taking mods off that list, a damaged file, and a save that fails. |
| `DewDropPathsTests.cs` | Comparing folder paths, including a `Mods` folder reached through a symbolic link or a junction. |
| `SteamLaunchOptionTests.cs` | Reading Stardew Valley's launch option from Steam's `localconfig.vdf`, and adding or removing `--mods-path` the way SMAPI reads it: the rest of the option is kept, an older `--mods-path` is replaced, and an option that starts another program is left alone. |
| `RelauncherScriptTests.cs` | The PowerShell script that runs after the game closes, tried on a scratch `localconfig.vdf`: it writes the new launch option and keeps a backup, leaves an option that changed meanwhile alone, and adds the game's entry when Steam has none. Steam is never closed, because the test points the script at a `steam.exe` that isn't running. |

The tests only write files inside a `dewdrop-tests` folder in your temp folder, so they never touch your game, your mods or Steam. Some tests pass without checking anything where they can't run:

* `RelauncherScriptTests` run Windows PowerShell, so they only check anything on Windows. If you change the script in `Relauncher.cs`, run the tests on Windows.
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
`dotnet build DewDrop.sln` with no `-c` makes a **Debug** build and deploys it to your `Mods` folder. When you've finished testing, build with `-c Release` so the copy you play with leaves the bridge out. To check which one is loaded, look in the SMAPI console: a Debug build logs `Debug bridge listening in <folder>` at startup.
{% endhint %}

## What CI checks

The workflow in `.github/workflows/build.yml` runs on every push to `development`, on every pull request, and when started by hand. It has four jobs:

* **`build`** builds `DewDrop.Core` on Ubuntu with .NET 6 and runs the [unit tests](#run-the-unit-tests). On Ubuntu, `RelauncherScriptTests` pass without running the script.
* **`build-mod`** builds and packages `DewDrop.Mod` exactly as a release is, against metadata-only reference assemblies of the game and SMAPI (the runner has no game install), and attaches both zips to the run. See [Releasing](releasing.md#how-ci-builds-the-mod-without-the-game).
* **`installer-unix`** runs on Ubuntu and on macOS (which ships bash 3.2). It checks the syntax of `install.sh` and both launchers, then runs `installer/tests/install-sh.test.sh`.
* **`installer-windows`** checks that `install.ps1` parses in Windows PowerShell and is plain ASCII, then runs `installer/tests/install-ps1.test.ps1`.

CI can't play the game, though, so still test your change in the game before you open a pull request. Pull requests into `master` also run a version check, and merging one publishes a release: see [Releasing](releasing.md).

## Packaging a release

Releases are built and published by CI (see [Releasing](releasing.md)), with the same script you can run yourself, for example to test the installer zip:

1. To change the version, run `node tools/bump-version.mjs patch` (or `minor`, `major`, or an exact version). It sets `DewDrop.Mod/manifest.json` and `DewDrop.Mod/DewDrop.Mod.csproj` together; the packaging script stops if they differ.
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
| `DewDrop-<version>.zip` | The mod zip exactly as the build made it: a `DewDrop/` folder with `manifest.json`, `DewDrop.dll`, `DewDrop.Core.dll`, `DewDrop.Core.pdb`, `SharpCompress.dll` and `ZstdSharp.dll`. | [Installing by hand](../getting-started/installing/manual.md) and other mod managers. |
| `DewDrop-<version>-installer.zip` | A `DewDrop <version> installer/` folder with the three launchers (`Install on Windows.bat`, `Install on macOS.command`, `Install on Linux.sh`), `README.txt`, and `internal/` holding `install.ps1`, `install.sh` and the mod zip as `DewDrop.zip`. | [The installer](../getting-started/installing/installer.md). |

<figure><img src="../.gitbook/assets/diagram-install-paths.png" alt="Diagram comparing the two ways to install DewDrop side by side: running the installer, or dragging the DewDrop folder into the Mods folder by hand"><figcaption><p>Each zip serves one of the two ways to install.</p></figcaption></figure>

The mod goes inside the installer as a zip, not a folder. If someone extracts the whole installer into their `Mods` folder by mistake, SMAPI won't find a second copy of DewDrop to load.

### Why the script writes its own zips

The script builds the installer zip byte by byte rather than with `Compress-Archive` or Explorer. That way it controls three things:

* **Unix permissions.** Each entry is marked as made on Unix. `Install on macOS.command`, `Install on Linux.sh` and `internal/install.sh` get mode `755` (executable), and every other file gets `644`. Unzip tools on macOS and Linux restore those modes, so the launchers work with a double-click. Zips made by Windows tools carry no Unix modes, and double-clicking a launcher from one of them fails.
* **Line endings.** The `.bat`, `README.txt` and `install.ps1` get Windows (CRLF) line endings. The `.command` and `.sh` files get Unix (LF) line endings, whatever your git checkout used. `.gitattributes` keeps `.bat`, `.sh` and `.command` files the same way in git checkouts.
* **Plain ASCII.** The script stops if `install.ps1` contains any non-ASCII character. Windows PowerShell 5.1 reads a script without a byte-order mark as ANSI, which garbles anything else.

### Publishing

CI publishes releases when `development` is merged into `master`: see [Releasing](releasing.md). It keeps the conventions the installer and the update check rely on:

* The plain zip is named `DewDrop-<version>.zip`. When `install.ps1` or `install.sh` runs without a bundled copy, it downloads the mod from the latest release and looks for an asset with that name.
* Releases are normal releases, not drafts. GitHub's "latest release", which the installer asks for, skips drafts and pre-releases.
* The tag is the version number, the same as `Version` in `manifest.json` (for example `0.2.0`). DewDrop's manifest has the update key `GitHub:friessssss/DewDrop`, so SMAPI and DewDrop's own update check (both through smapi.io) compare that tag with installed versions. See [Updating DewDrop](../getting-started/updating-dewdrop.md).

### Testing the installer

The installer has tests of its own, which CI also runs. They work on fake game folders and never touch your real game or Steam. If you change the installer, run them before you open a pull request.

You can also run the installer scripts straight from the repo against your own build. `-DryRun` and `--dry-run` show what would happen without changing anything. Remove that flag to install for real.

{% tabs %}
{% tab title="Windows" %}
```powershell
# Run the installer's tests
powershell -NoProfile -ExecutionPolicy Bypass -File installer\tests\install-ps1.test.ps1

# Preview installing your own build
powershell -NoProfile -ExecutionPolicy Bypass -File installer\internal\install.ps1 -ModSource "DewDrop.Mod\bin\Release\net6.0\DewDrop 0.1.0.zip" -DryRun
```
{% endtab %}

{% tab title="macOS and Linux" %}
```bash
# Run the installer's tests
bash installer/tests/install-sh.test.sh

# Preview installing your own build
bash installer/internal/install.sh --mod-source "DewDrop.Mod/bin/Release/net6.0/DewDrop 0.1.0.zip" --dry-run
```
{% endtab %}
{% endtabs %}

Always pass `-ModSource` or `--mod-source` here, with a mod folder or a zip. Without it, a script run from the repo has no bundled copy, so it downloads the latest GitHub release instead of testing your build. If the path doesn't exist, `install.ps1` stops, but `install.sh` downloads the release too, so check that the output doesn't say "No bundled copy of DewDrop". The other options are listed in [Installer command-line options](../reference/installer-options.md).
