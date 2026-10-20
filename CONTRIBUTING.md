# Contributing to DewDrop

Thanks for helping. The full contributor guide is in the
[DewDrop docs](https://friessssss.gitbook.io/dewdrop/contributing/contributing), including
[what's in progress](https://friessssss.gitbook.io/dewdrop/contributing/contributing#whats-in-progress) right now.
Everyone taking part follows the [Code of Conduct](CODE_OF_CONDUCT.md).

## Reporting a problem

Open an [issue](https://github.com/friessssss/DewDrop/issues/new/choose) and attach your SMAPI log: upload it at
[smapi.io/log](https://smapi.io/log) and share the link. If the problem was with the installer, attach
`DewDrop-installer.log` from your temp folder too. On Linux and the Steam Deck, when Steam starts the game through
DewDrop's launcher, also attach `steam-launch.log` from `~/.config/Stardrop/Data/Cache/DewDrop/`.
[Reporting issues](https://friessssss.gitbook.io/dewdrop/help/reporting-issues) lists everything that helps.

## Pull requests

Branch from `development` and open your pull request against it. `master` only holds releases. Before you open it:

1. Build DewDrop and run the unit tests. The build needs Stardew Valley and SMAPI installed; the tests don't.

   ```bash
   dotnet build DewDrop.sln
   dotnet test DewDrop.Core.Tests/DewDrop.Core.Tests.csproj
   ```

2. Test the change in the game. CI builds and tests DewDrop, but it can't play the game.

3. Do the extra checks below for the parts you changed.

[Building from source](https://friessssss.gitbook.io/dewdrop/contributing/building) explains the setup and what CI
checks.

### Extra checks by area

- **Installer** (`installer/`, and the one-line installers `install.ps1` and `install.sh` in the repo root): run
  its tests.

  ```bash
  bash installer/tests/install-sh.test.sh
  ```

  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File installer\tests\install-ps1.test.ps1
  ```

  Keep both `install.ps1` files plain ASCII and working in Windows PowerShell 5.1, and both `install.sh` files
  working in bash 3.2, which is what macOS ships. The one-line installers download the installer from `master`, so a
  change to the installer reaches them only with the next release.
- **Starting in the profile on Windows** (`ProfileStart.cs`, `SteamLaunchOption.cs`, `Relauncher.cs`): CI runs the
  tests on Ubuntu, where `RelauncherScriptTests` don't run the script, so run the tests on Windows too. Turning
  **Start in my profile from Steam** on or off closes the real Steam and edits its settings when you quit the game,
  so test it with a Debug build and the `DEWDROP_FAKE_STEAM` variable. See
  [The debug bridge](https://friessssss.gitbook.io/dewdrop/contributing/debug-bridge).
- **Linux and the Steam Deck** (`DewDrop.Mod/assets/steam-launch.sh`, `SteamLauncher.cs`, `SteamDeck.cs`,
  `SteamOverlay.cs`): Steam runs the launcher with `/bin/sh`, so keep it plain POSIX `sh`. `SteamLauncherTests` only
  run it on macOS and Linux (CI runs them on Ubuntu). Anything that changes Game Mode needs a real Deck: see
  [Testing on a Steam Deck](https://friessssss.gitbook.io/dewdrop/contributing/steam-deck-testing).
- **Docs** live in `docs/` and sync to the GitBook site from `development`. Every image in them is generated:
  change `tools/docs-art/` and redraw the images rather than editing PNGs by hand.

  ```bash
  node tools/docs-art/build.mjs
  ```

  See [Docs and art style](https://friessssss.gitbook.io/dewdrop/contributing/docs-and-art).

## Releases

CI publishes a release when `development` is merged into `master`. A pull request into `master` has to come from
`development` and carry a version higher than the last release, which this sets:

```bash
node tools/bump-version.mjs patch
```

Use `minor` or `major` instead of `patch` as needed. See
[Releasing](https://friessssss.gitbook.io/dewdrop/contributing/releasing).

To package by hand, run this. It builds in Release and writes both downloads to `artifacts/release/<version>/`.

```bash
node tools/package-release.mjs
```
