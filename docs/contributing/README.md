---
description: "Help build DewDrop's mod, installer, docs and art, and see what's in progress right now."
icon: code
---

# Contributing

DewDrop is open source under the GPL-3.0 license, and help is welcome. A clearer sentence in these docs is as useful as a bug fix.

The code lives at [github.com/friessssss/DewDrop](https://github.com/friessssss/DewDrop). Work from the `development` branch and open your pull requests against it. `master` only holds releases (see [Releasing](releasing.md)).

Found a bug but don't want to write code? [Reporting issues](../help/reporting-issues.md) shows you what to include.

## Before you open a pull request

1. Build DewDrop and run the unit tests. The build needs the game and SMAPI installed (see [Building from source](building.md)); the tests don't.

   ```bash
   dotnet build DewDrop.sln
   dotnet test DewDrop.Core.Tests/DewDrop.Core.Tests.csproj
   ```
2. Try the change in the game. CI can't play it.
3. If you changed the installer, run [its tests](building.md#testing-the-installer) too.
4. If players will see the change, update these docs. [Docs and art style](docs-and-art.md) explains how.

A pull request merges once CI passes and a maintainer has reviewed it. [CONTRIBUTING.md](https://github.com/friessssss/DewDrop/blob/development/CONTRIBUTING.md) lists the extra checks for each part of the code, and the [Code of Conduct](https://github.com/friessssss/DewDrop/blob/development/CODE_OF_CONDUCT.md), which is the Contributor Covenant, applies everywhere.

## In this section

* [Building from source](building.md): set up your computer, build DewDrop, run the tests, see what CI checks, and package a release.
* [Releasing](releasing.md): how a release goes from `development` to `master` and gets published by CI, and the plan for 0.2.0.
* [The debug bridge](debug-bridge.md): control the title screen from a script and take screenshots. It's only in Debug builds.
* [Testing on a Steam Deck](steam-deck-testing.md): the checklist for trying DewDrop's Steam Deck support on a real Deck.
* [Docs and art style](docs-and-art.md): how this guide is written, how it syncs to GitBook, and how every image is made.

## What's in progress

This is where things stand on October 20, 2026. DewDrop 0.1.0, made by pull requests #1 to #7, is the latest release. Everything below came after it. None of it is released yet: it's planned for 0.2.0.

| Pull request | What it adds | Where it stands |
| --- | --- | --- |
| [#8](https://github.com/friessssss/DewDrop/pull/8) | The one-line installer for macOS and Linux (`curl ... \| bash`). | Merged into `development`. |
| [#9](https://github.com/friessssss/DewDrop/pull/9) | The installer sets up Rosetta before SMAPI's installer on Apple Silicon Macs. | Merged into `development`. |
| [#10](https://github.com/friessssss/DewDrop/pull/10) | The one-line installer for Windows (`irm ... \| iex`). | Merged into `development`. |
| [#12](https://github.com/friessssss/DewDrop/pull/12) | Uninstalling mods from the **Installed** tab or a mod's page, **Installed** and **Update** tags on **Browse**, the installed file marked on a mod's page, and an **Installed twice** warning. | Merged into `development`. |
| [#11](https://github.com/friessssss/DewDrop/pull/11) | Steam Deck support: set up once in Desktop Mode, then manage mods in Game Mode. It also brings **Start in my profile from Steam** to Linux. | Open, as a draft. CI passes. Waiting for a review and for testing on a real Deck. |

#11 has been tried on a real Steam Deck as far as installing, which caught one bug that's now fixed. These still need checking on the device:

* whether the game comes back in Game Mode after **Apply & Restart**;
* whether Steam's overlay browser and on-screen keyboard open from the game;
* screenshots for the docs.

It also hasn't been tried on Windows with `DEWDROP_FAKE_STEAM`, where only the order of the steps as the game closes changed. [Testing on a Steam Deck](steam-deck-testing.md) is the checklist, if you have a Deck and want to help.

The version bump to 0.2.0 isn't in a pull request yet.

Until 0.2.0 is released, the one-line installers don't work: `master` doesn't have them yet. [The next release](releasing.md#the-next-release) lists what's left to do, in order.

## How the repo is laid out

| Folder | What's in it |
| --- | --- |
| `DewDrop.Core/` | A plain .NET 6 library with no game references. It handles profiles, settings, mod scanning and linking, installs and where each mod came from, updates and uninstalls that wait for the game to close, update checks, the Nexus Mods API, the `nxm://` bridge and the relauncher. It also handles Steam: the launch option on Windows, and spotting a Steam Deck and its Game Mode. |
| `DewDrop.Core.Tests/` | Unit tests for `DewDrop.Core`. They run without the game. |
| `DewDrop.Mod/` | The SMAPI mod itself. It holds the title-screen button (Harmony patches on `TitleMenu`), the menu and its tabs, the **Load your profile?** box, background downloads, Steam's overlay and keyboard for Game Mode, and the debug bridge. |
| `DewDrop.Mod/assets/steam-launch.sh` | DewDrop's Steam launcher for Linux and the Steam Deck. It ships in the mod folder, and DewDrop copies it to its data folder for Steam to run. |
| `installer/` | The installer: `internal/install.ps1` for Windows, `internal/install.sh` for macOS, Linux and the Steam Deck, the double-click launchers, and its tests in `tests/`. |
| `install.ps1`, `install.sh` | The one-line installers (`irm ... \| iex` and `curl ... \| bash`). They download the installer from `master` and run it. See [Installer options](../reference/installer-options.md#what-the-launchers-do). |
| `tools/bump-version.mjs`, `tools/check-version.mjs` | Set the version, and check it before a release. |
| `tools/package-release.mjs` | Builds the two release zips. |
| `tools/docs-art/` | Draws every image in these docs. |
| `docs/` | This guide. |
| `.github/` | The CI workflows, issue forms and pull request template. |

To see how these pieces work together while the game runs, read [How DewDrop works](../reference/how-it-works.md).

DewDrop started as a fork of Stardrop's code (both are GPL-3.0), so the git history includes Stardrop's commits. DewDrop isn't affiliated with or endorsed by Stardrop's authors. [Using DewDrop with Stardrop](../stardrop.md) explains how the two share data.
