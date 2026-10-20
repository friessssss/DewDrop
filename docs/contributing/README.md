---
description: "Help build DewDrop: the mod, the installer, these docs and their art."
icon: code
---

# Contributing

DewDrop is open source under the GPL-3.0 license, and help is welcome. A clearer sentence in these docs is as useful as a bug fix.

The code lives at [github.com/friessssss/DewDrop](https://github.com/friessssss/DewDrop). Work from the `development` branch and open your pull requests against it. `master` only holds releases (see [Releasing](releasing.md)).

Before you start, please read:

* [CONTRIBUTING.md](https://github.com/friessssss/DewDrop/blob/development/CONTRIBUTING.md). A pull request needs `dotnet build DewDrop.sln` to succeed and the [unit tests](building.md#run-the-unit-tests) to pass, and you should test the change in the game. If you change the installer, also run [its tests](building.md#testing-the-installer).
* The [Code of Conduct](https://github.com/friessssss/DewDrop/blob/development/CODE_OF_CONDUCT.md), which is the Contributor Covenant.

Found a bug but don't want to write code? [Reporting issues](../help/reporting-issues.md) shows you what to include.

## In this section

* [Building from source](building.md): set up your computer, build DewDrop, and package a release with `tools/package-release.mjs`.
* [Releasing](releasing.md): how a release goes from `development` to `master` and gets published by CI.
* [The debug bridge](debug-bridge.md): control the title screen from a script and take screenshots. It's only in Debug builds.
* [Docs and art style](docs-and-art.md): how this guide is written, how it syncs to GitBook, and how every image is made.

## How the repo is laid out

| Folder | What's in it |
| --- | --- |
| `DewDrop.Core/` | A plain .NET 6 library with no game references. It handles profiles, settings, mod scanning, linking, the Nexus Mods API, installs, update checks, the `nxm://` bridge, the relauncher and Steam's launch option for the game. |
| `DewDrop.Core.Tests/` | Unit tests for `DewDrop.Core`. They run without the game. |
| `DewDrop.Mod/` | The SMAPI mod itself. It holds the title-screen button (Harmony patches on `TitleMenu`), the menu and its tabs, the **Load your profile?** box, background downloads and the debug bridge. |
| `installer/` | The installer: `internal/install.ps1` for Windows, `internal/install.sh` for macOS and Linux, the double-click launchers, and its tests in `tests/`. |
| `tools/package-release.mjs` | Builds the two release zips. |
| `tools/docs-art/` | Draws every image in these docs. |
| `docs/` | This guide. |

To see how these pieces work together while the game runs, read [How DewDrop works](../reference/how-it-works.md).

DewDrop started as a fork of Stardrop's code (both are GPL-3.0), so the git history includes Stardrop's commits. DewDrop isn't affiliated with or endorsed by Stardrop's authors. [Using DewDrop with Stardrop](../stardrop.md) explains how the two share data.
