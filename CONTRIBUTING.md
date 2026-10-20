If reporting an issue with DewDrop, please attach your SMAPI log. You can upload it at
[smapi.io/log](https://smapi.io/log) and share the link. If the problem was with the installer, attach
`DewDrop-installer.log` from your temp folder too.

For a pull request, make sure `dotnet build DewDrop.sln` succeeds and the unit tests pass
(`dotnet test DewDrop.Core.Tests/DewDrop.Core.Tests.csproj`; CI runs them too). Then test the change in the game.

- **Installer** changes (`installer/`): run `bash installer/tests/install-sh.test.sh` and
  `powershell -NoProfile -ExecutionPolicy Bypass -File installer\tests\install-ps1.test.ps1`. Keep `installer/internal/install.ps1` plain ASCII and
  working in Windows PowerShell 5.1, and keep `install.sh` working in bash 3.2 (what macOS ships).
- **Starting in the profile** (`ProfileStart.cs`, `SteamLaunchOption.cs`, `Relauncher.cs`): CI runs the tests on
  Ubuntu, where `RelauncherScriptTests` don't run the script, so run the tests on Windows too. Turning
  **Start in my profile from Steam** on or off closes the real Steam and edits its settings when you quit the
  game, so test that with a Debug build and the `DEWDROP_FAKE_STEAM` variable. See
  `docs/contributing/debug-bridge.md`.
- **Docs** live in `docs/` and sync to the GitBook. Every image in them is generated: change
  `tools/docs-art/` and run `node tools/docs-art/build.mjs` rather than editing PNGs by hand. See
  `docs/contributing/docs-and-art.md`.
- **Releases** are packaged with `node tools/package-release.mjs`, which builds in Release and writes both
  downloads to `artifacts/release/<version>/`.
