#!/usr/bin/env bash
# Tests installer/internal/install.sh against a fake game folder in a throwaway HOME. Offline: SMAPI is faked as
# already installed, and the mod comes from a small zip built here. Runs on Linux and macOS (bash 3.2).
#
#   bash installer/tests/install-sh.test.sh
set -u
HERE="$(cd "$(dirname "$0")" && pwd)"
SCRIPT="$HERE/../internal/install.sh"
ROOT="$(mktemp -d "${TMPDIR:-/tmp}/dewdrop-test.XXXXXX")"
[ -d "$ROOT" ] || { echo "mktemp failed"; exit 1; }
trap 'rm -rf "$ROOT"' EXIT
export HOME="$ROOT/home"
export TMPDIR="$ROOT/tmp"
# CI runners set these, and the installer follows them (as .NET does), so the throwaway HOME needs them gone
unset XDG_CONFIG_HOME XDG_CACHE_HOME
# The installer reads the computer's maker and model to spot a Steam Deck; the tests say what they are
export DEWDROP_DMI="$ROOT/no-dmi"
mkdir -p "$TMPDIR"

if [ "$(uname -s)" = "Darwin" ]; then
    GAME="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"
else
    GAME="$HOME/.local/share/Steam/steamapps/common/Stardew Valley"
fi

pass=0; fail=0
check() { if eval "$2"; then echo "PASS $1"; pass=$((pass + 1)); else echo "FAIL $1"; fail=$((fail + 1)); fi; }
run() { bash "$SCRIPT" --yes --no-launch --skip-smapi "$@" < /dev/null > "$ROOT/out.txt" 2>&1; code=$?; }
show() { sed 's/^/    | /' "$ROOT/out.txt"; }
snapshot() { (cd "$GAME" && find . | sort | cksum); }

# A stand-in DewDrop release zip: DewDrop/manifest.json + a dummy DLL.
mkdir -p "$ROOT/mod/DewDrop"
cat > "$ROOT/mod/DewDrop/manifest.json" <<'EOF'
{
  "Name": "DewDrop",
  "Author": "friessssss",
  "Version": "9.9.9",
  "UniqueID": "friessssss.DewDrop",
  "EntryDll": "DewDrop.dll"
}
EOF
echo "dll" > "$ROOT/mod/DewDrop/DewDrop.dll"
mkdir -p "$ROOT/mod/DewDrop/assets"
cp "$HERE/../../DewDrop.Mod/assets/steam-launch.sh" "$ROOT/mod/DewDrop/assets/steam-launch.sh"
MOD_ZIP="$ROOT/DewDrop-9.9.9.zip"
(cd "$ROOT/mod" && python3 -c 'import sys, zipfile; z = zipfile.ZipFile(sys.argv[1], "w"); [z.write(p) for p in sys.argv[2:]]' "$MOD_ZIP" DewDrop DewDrop/manifest.json DewDrop/DewDrop.dll DewDrop/assets/steam-launch.sh)

setup() {
    rm -rf "$HOME/Library" "$HOME/.local" "$HOME/My Games"
    mkdir -p "$GAME/Mods" "$HOME/.config/StardewValley"
    : > "$GAME/Stardew Valley.dll"
    printf 'MZ 4.5.2.0 4.5.2+821167e5c511bf3a2d98f604e5e838561c469219 ' > "$GAME/StardewModdingAPI.dll"
}

echo "== fresh install"
setup
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "reads the SMAPI version from the DLL" 'grep -q "SMAPI 4.5.2 is installed" "$ROOT/out.txt"'
check "manifest installed" '[ -f "$GAME/Mods/DewDrop/manifest.json" ]'
check "dll installed" '[ -f "$GAME/Mods/DewDrop/DewDrop.dll" ]'
check "no staging folders left" '! ls -a "$GAME/Mods" | grep -q dewdrop-'

echo "== update keeps config.json and turns off a duplicate"
echo '{"ShowAdultContent": true}' > "$GAME/Mods/DewDrop/config.json"
mkdir -p "$GAME/Mods/Utilities/DewDrop old" "$GAME/Mods/.hidden/DewDrop"
cp "$GAME/Mods/DewDrop/manifest.json" "$GAME/Mods/Utilities/DewDrop old/"
cp "$GAME/Mods/DewDrop/manifest.json" "$GAME/Mods/.hidden/DewDrop/"
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "updated in place" 'grep -q "already installed in .*Mods/DewDrop; it.ll be updated in place" "$ROOT/out.txt"'
check "config kept" 'grep -q "\"ShowAdultContent\": true" "$GAME/Mods/DewDrop/config.json"'
check "duplicate renamed with a dot" '[ -d "$GAME/Mods/Utilities/.DewDrop old-duplicate" ] && [ ! -d "$GAME/Mods/Utilities/DewDrop old" ]'
check "dot-folders left alone" '[ -d "$GAME/Mods/.hidden/DewDrop" ]'

echo "== dry run changes nothing"
setup
before="$(snapshot)"
run --dry-run --mod-source "$MOD_ZIP"
after="$(snapshot)"
check "exit 0" '[ $code = 0 ]' || show
check "nothing changed" '[ "$before" = "$after" ]'
check "says what it would do" 'grep -q "(dry run) would copy DewDrop 9.9.9" "$ROOT/out.txt"'

echo "== game not found"
rm -rf "$HOME/Library" "$HOME/.local"
run --mod-source "$MOD_ZIP"
check "exit 1" '[ $code = 1 ]'
check "explains --game-path" 'grep -q -- "--game-path" "$ROOT/out.txt"'

echo "== --game-path with quotes and spaces (as pasted or dragged in)"
setup
custom="$HOME/My Games/Stardew Valley"
mkdir -p "$(dirname "$custom")"
mv "$GAME" "$custom"
run --game-path "'$custom' " --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "installed into the custom folder" '[ -f "$custom/Mods/DewDrop/manifest.json" ]'

echo "== a folder named DewDrop that isn't DewDrop is never overwritten"
setup
mkdir -p "$GAME/Mods/DewDrop"
echo '{"UniqueID": "someone.else"}' > "$GAME/Mods/DewDrop/manifest.json"
run --mod-source "$MOD_ZIP"
check "exit 1" '[ $code = 1 ]'
check "other mod untouched" 'grep -q someone.else "$GAME/Mods/DewDrop/manifest.json"'

echo "== a mod that depends on DewDrop is never mistaken for it"
setup
mkdir -p "$GAME/Mods/DewDropAddon"
echo '{"UniqueID":"someone.Addon","Dependencies":[{"UniqueID":"friessssss.DewDrop"}],"Name":"Addon"}' > "$GAME/Mods/DewDropAddon/manifest.json"
echo "addon" > "$GAME/Mods/DewDropAddon/Addon.dll"
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "addon untouched" '[ -f "$GAME/Mods/DewDropAddon/Addon.dll" ] && grep -q someone.Addon "$GAME/Mods/DewDropAddon/manifest.json"'
check "DewDrop installed beside it" '[ -f "$GAME/Mods/DewDrop/manifest.json" ]'

echo "== a manifest with comments and a dependency listed first is read correctly"
cat > "$GAME/Mods/DewDrop/manifest.json" <<'EOF'
{
  // Hand-edited: "UniqueID": "not.this"
  "Dependencies": [ { "UniqueID": "someone.Lib" } ], /* also not this */
  "Name": "DewDrop",
  "Version": "1.0.0",
  "UniqueID": "friessssss.DewDrop",
}
EOF
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "recognised and updated in place" 'grep -q "DewDrop 1.0.0 is already installed" "$ROOT/out.txt" && grep -q 9.9.9 "$GAME/Mods/DewDrop/manifest.json"'

echo "== a newer installed DewDrop is kept by default"
sed -i.bak 's/9.9.9/99.0.0/' "$GAME/Mods/DewDrop/manifest.json" && rm -f "$GAME/Mods/DewDrop/manifest.json.bak"
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "kept the newer copy" 'grep -q 99.0.0 "$GAME/Mods/DewDrop/manifest.json" && grep -q "Keeping DewDrop 99.0.0" "$ROOT/out.txt"'

echo "== an SMAPI whose version can't be read (like SMAPI 3) isn't reported as fine"
setup
printf 'MZ 3.18.6.0 3.18.6 ' > "$GAME/StardewModdingAPI.dll"
run --mod-source "$MOD_ZIP"
check "warns about the unknown version" 'grep -q "couldn.t tell which version" "$ROOT/out.txt"'

echo "== a symlinked Mods folder is searched"
setup
mv "$GAME/Mods" "$ROOT/real-mods"
ln -s "$ROOT/real-mods" "$GAME/Mods"
mkdir -p "$ROOT/real-mods/Utilities"
(cd "$ROOT/mod" && cp -R DewDrop "$ROOT/real-mods/Utilities/DewDrop")
run --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "updated the existing copy instead of adding a second" '[ ! -e "$ROOT/real-mods/DewDrop" ] && [ -f "$ROOT/real-mods/Utilities/DewDrop/manifest.json" ]'
rm -f "$GAME/Mods"; mv "$ROOT/real-mods" "$GAME/Mods"

echo "== --game-path accepts the folder above Contents/MacOS"
setup
outer="$HOME/My Games/Stardew Valley"
mkdir -p "$outer/Contents"
mv "$GAME" "$outer/Contents/MacOS"
run --game-path "$outer" --mod-source "$MOD_ZIP"
check "exit 0" '[ $code = 0 ]' || show
check "installed into Contents/MacOS/Mods" '[ -f "$outer/Contents/MacOS/Mods/DewDrop/manifest.json" ]'

if [ "$(uname -s)" = "Linux" ]; then
    echo "== the Windows version (run through Proton) is flagged"
    setup
    : > "$GAME/Stardew Valley.exe"
    run --mod-source "$MOD_ZIP"
    check "stops by default" '[ $code = 1 ] && grep -q "Proton" "$ROOT/out.txt"'
fi

if [ "$(uname -s)" = "Linux" ]; then
    echo "== Steam Deck: Steam's launch option is pointed at DewDrop's launcher"
    setup
    STEAM="$HOME/.local/share/Steam"
    LAUNCHER="$HOME/.config/Stardrop/Data/Cache/DewDrop/steam-launch.sh"
    ACCOUNT="$STEAM/userdata/52079950/config/localconfig.vdf"
    OTHER="$STEAM/userdata/11111/config/localconfig.vdf"
    FAKE="$ROOT/fake-bin"
    vdf() { # option -> a trimmed localconfig.vdf
        printf '"UserLocalConfigStore"\n{\n\t"Software"\n\t{\n\t\t"Valve"\n\t\t{\n\t\t\t"Steam"\n\t\t\t{\n\t\t\t\t"apps"\n\t\t\t\t{\n'
        printf '\t\t\t\t\t"105600"\n\t\t\t\t\t{\n\t\t\t\t\t\t"LaunchOptions"\t\t"-other"\n\t\t\t\t\t}\n'
        printf '\t\t\t\t\t"413150"\n\t\t\t\t\t{\n\t\t\t\t\t\t"LastPlayed"\t\t"1"\n\t\t\t\t\t\t"LaunchOptions"\t\t"%s"\n\t\t\t\t\t}\n' "$1"
        printf '\t\t\t\t}\n\t\t\t}\n\t\t}\n\t}\n}\n'
    }
    mkdir -p "$(dirname "$ACCOUNT")" "$(dirname "$OTHER")" "$STEAM/config"
    vdf '\"/old/StardewModdingAPI\" %command%' > "$ACCOUNT"
    vdf 'someone else' > "$OTHER"
    touch "$OTHER"
    # The signed-in account is the MostRecent one, even though the other account's file is newer
    printf '"users"\n{\n\t"76561198000000000"\n\t{\n\t\t"MostRecent"\t\t"0"\n\t}\n\t"76561198012345678"\n\t{\n\t\t"AccountName"\t\t"deck"\n\t\t"MostRecent"\t\t"1"\n\t}\n}\n' > "$STEAM/config/loginusers.vdf"
    printf 'NAME="SteamOS"\nID=steamos\nID_LIKE=arch\nVARIANT_ID=steamdeck\n' > "$ROOT/os-release"
    # A Steam that runs until it's told to shut down, and logs what it was asked. As it shuts down, it saves the
    # settings in steam-saves if there are any, and starts again by itself while steam-restarts is there (as a service
    # would) until that service is stopped.
    mkdir -p "$FAKE"
    cat > "$FAKE/steam" <<EOF
#!/bin/sh
echo "steam \$*" >> "$ROOT/steam.log"
case "\$1" in
    --fake-running) echo \$\$ > "$ROOT/steam.pid"; while :; do sleep 0.2; done ;;
    -shutdown)
        kill "\$(cat "$ROOT/steam.pid")"
        [ -f "$ROOT/steam-saves" ] && cp "$ROOT/steam-saves" "$ACCOUNT"
        if [ -f "$ROOT/steam-restarts" ] && [ ! -f "$ROOT/steam-stopped" ]; then ("$FAKE/steam" --fake-running &); fi ;;
esac
EOF
    chmod +x "$FAKE/steam"
    deck() { PATH="$FAKE:$PATH" DEWDROP_OS_RELEASE="$ROOT/os-release" bash "$SCRIPT" --yes --no-launch --skip-smapi --mod-source "$MOD_ZIP" "$@" < /dev/null > "$ROOT/out.txt" 2>&1; code=$?; }
    option() { grep LaunchOptions "$ACCOUNT" | sed -n 2p | sed 's/^[[:space:]]*"LaunchOptions"[[:space:]]*"\(.*\)"$/\1/'; }
    : > "$ROOT/steam.log"
    # Started from a subshell, so it isn't this script's child: killed, it would stay a zombie that pgrep still sees
    ("$FAKE/steam" --fake-running &)
    sleep 0.5
    DEWDROP_NEXUS_KEY="my-key" deck
    check "exit 0" '[ $code = 0 ]' || show
    check "closed Steam first" 'grep -q "steam -shutdown" "$ROOT/steam.log" && grep -q "Steam closed" "$ROOT/out.txt"'
    check "set the launch option" '[ "$(option)" = "\\\"$LAUNCHER\\\" %command%" ]' || option
    check "left other games and accounts alone" 'grep -q "\"-other\"" "$ACCOUNT" && grep -q "someone else" "$OTHER"'
    check "backed up Steam's settings" 'ls "$(dirname "$ACCOUNT")" | grep -q "localconfig.vdf.dewdrop-backup-"'
    check "copied the launcher" '[ -x "$LAUNCHER" ] && cmp -s "$LAUNCHER" "$GAME/Mods/DewDrop/assets/steam-launch.sh"'
    check "start in profile on by default" '[ -f "$(dirname "$LAUNCHER")/start-in-profile" ]'
    check "saved the Nexus key for the game, readable only by you" '[ "$(cat "$(dirname "$LAUNCHER")/nexus-key.txt")" = "my-key" ] && ls -l "$(dirname "$LAUNCHER")/nexus-key.txt" | grep -q "^-rw-------"'
    check "opened Steam again" 'grep -q "steam -silent" "$ROOT/steam.log"'
    check "says to go back to Game Mode" 'grep -q "Return to Gaming Mode" "$ROOT/out.txt"'
    # On a Deck, Steam's on-screen keyboard and the controls close with Steam: nothing may be asked while it's away
    at() { grep -n "$1" "$ROOT/out.txt" | head -n 1 | cut -d: -f1; }
    check "took the Nexus key before closing Steam" '[ "$(at "Saved. DewDrop logs in")" -lt "$(at "Closing Steam")" ]'
    check "Steam was back straight after the edit" '[ "$(at "Steam is starting again")" -lt "$(at "All done")" ]'

    echo "== Steam Deck: the game isn't offered in Desktop Mode"
    PATH="$FAKE:$PATH" DEWDROP_OS_RELEASE="$ROOT/os-release" bash "$SCRIPT" --yes --skip-smapi --mod-source "$MOD_ZIP" < /dev/null > "$ROOT/out.txt" 2>&1
    code=$?
    check "exit 0" '[ $code = 0 ]' || show
    check "no Start Stardew Valley question" '! grep -q "Start Stardew Valley now" "$ROOT/out.txt"'

    echo "== Steam Deck: running it again changes nothing in Steam"
    : > "$ROOT/steam.log"
    cp "$ACCOUNT" "$ROOT/before.vdf"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "already set" 'grep -q "already starts the game with DewDrop" "$ROOT/out.txt" && cmp -s "$ACCOUNT" "$ROOT/before.vdf"'
    check "Steam left alone" '[ ! -s "$ROOT/steam.log" ]'

    echo "== Steam Deck: other launch options keep their parts, and the profile setting can be turned off"
    vdf 'gamemoderun %command% -novid' > "$ACCOUNT"
    echo '{"StartInProfileFromSteam": true, "AskToLoadProfile": true}' > "$GAME/Mods/DewDrop/config.json"
    deck --no-start-in-profile
    check "exit 0" '[ $code = 0 ]' || show
    check "launcher added before %command%" '[ "$(option)" = "gamemoderun \\\"$LAUNCHER\\\" %command% -novid" ]' || option
    check "start in profile off" '[ ! -f "$(dirname "$LAUNCHER")/start-in-profile" ] && grep -q "\"StartInProfileFromSteam\": false" "$GAME/Mods/DewDrop/config.json"'

    echo "== Steam Deck: a dry run changes nothing"
    vdf '' > "$ACCOUNT"
    cp "$ACCOUNT" "$ROOT/before.vdf"
    rm -f "$LAUNCHER"
    deck --dry-run
    check "exit 0" '[ $code = 0 ]' || show
    check "says what it would set" 'grep -q "(dry run) would close Steam, back up" "$ROOT/out.txt"'
    check "Steam's settings untouched" 'cmp -s "$ACCOUNT" "$ROOT/before.vdf" && [ ! -e "$LAUNCHER" ]'

    echo "== Steam Deck: a settings file with an unexpected layout is never written"
    printf '"UserLocalConfigStore"\n{\n}\n' > "$ACCOUNT"
    cp "$ACCOUNT" "$ROOT/before.vdf"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "warns and leaves it" 'grep -q "unexpected layout" "$ROOT/out.txt" && cmp -s "$ACCOUNT" "$ROOT/before.vdf"'

    echo "== Steam Deck: a Steam that its service starts again is stopped through that service"
    vdf '' > "$ACCOUNT"
    mkdir -p "$ROOT/fake-systemd"
    # As procps reports a process the user's systemd runs: the system unit is the user manager itself
    printf '#!/bin/sh\ncase "$*" in *uunit=*) echo app-steam@autostart.service ;; *unit=*) echo user@1000.service ;; esac\n' > "$ROOT/fake-systemd/ps"
    cat > "$ROOT/fake-systemd/systemctl" <<EOF
#!/bin/sh
echo "systemctl \$*" >> "$ROOT/steam.log"
case "\$2" in stop) touch "$ROOT/steam-stopped"; kill "\$(cat "$ROOT/steam.pid")" ;; esac
EOF
    chmod +x "$ROOT/fake-systemd/"*
    : > "$ROOT/steam.log"
    touch "$ROOT/steam-restarts"
    ("$FAKE/steam" --fake-running &)
    sleep 0.5
    PATH="$ROOT/fake-systemd:$PATH" deck
    check "exit 0" '[ $code = 0 ]' || show
    check "stopped the user service" 'grep -q "systemctl --user stop app-steam@autostart.service" "$ROOT/steam.log"' || show
    check "set the launch option" '[ "$(option)" = "\\\"$LAUNCHER\\\" %command%" ]' || option
    check "started the service again" 'grep -q "systemctl --user start app-steam@autostart.service" "$ROOT/steam.log"'
    rm -f "$ROOT/steam-restarts" "$ROOT/steam-stopped"

    echo "== Steam Deck: launch options Steam saves as it closes are never written over"
    vdf '' > "$ACCOUNT"
    vdf 'mangohud %command%' > "$ROOT/steam-saves"
    : > "$ROOT/steam.log"
    ("$FAKE/steam" --fake-running &)
    sleep 0.5
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "left Steam's new options alone" '[ "$(option)" = "mangohud %command%" ] && grep -q "Steam saved different launch options" "$ROOT/out.txt"' || option
    check "opened Steam again anyway" 'grep -q "steam -silent" "$ROOT/steam.log"'
    rm -f "$ROOT/steam-saves"

    echo "== Steam Deck: an option that starts SMAPI keeps its wrappers, not SMAPI's own options"
    vdf 'gamemoderun \"/games/Stardew Valley/StardewModdingAPI\" --mods-path \"Mods-Deck\" %command% -novid' > "$ACCOUNT"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "launcher in SMAPI's place" '[ "$(option)" = "gamemoderun \\\"$LAUNCHER\\\" %command% -novid" ]' || option
    check "says which SMAPI options go" 'grep -q "options go: --mods-path \"Mods-Deck\"" "$ROOT/out.txt"'

    echo "== Steam Deck: options left as they are still get a launcher to point at, and no Game Mode promise"
    vdf '-some-arg' > "$ACCOUNT"
    rm -f "$LAUNCHER"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "left them" '[ "$(option)" = "-some-arg" ]' || option
    check "the manual steps point at a real launcher" '[ -x "$LAUNCHER" ] && grep -q "Launch Options enter" "$ROOT/out.txt"'
    check "says Steam isn't set up yet" '! grep -q "Return to Gaming Mode" "$ROOT/out.txt" && grep -q "isn.t set to start the game" "$ROOT/out.txt"'

    echo "== Steam Deck: turning the profile setting off in the game survives running the installer again"
    vdf "\\\"$LAUNCHER\\\" %command%" > "$ACCOUNT"
    echo '{"StartInProfileFromSteam": false, "AskToLoadProfile": true}' > "$GAME/Mods/DewDrop/config.json"
    rm -f "$(dirname "$LAUNCHER")/start-in-profile"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "still off, without asking" '[ ! -f "$(dirname "$LAUNCHER")/start-in-profile" ] && grep -q "\"StartInProfileFromSteam\": false" "$GAME/Mods/DewDrop/config.json" && ! grep -q "when you press Play in Steam?" "$ROOT/out.txt"'

    echo "== Steam Deck: the Nexus key is only skipped when one is saved"
    mkdir -p "$HOME/.config/Stardrop/Data"
    printf '{\n  "NexusDetails": null\n}\n' > "$HOME/.config/Stardrop/Data/Settings.json"
    rm -f "$(dirname "$LAUNCHER")/nexus-key.txt"
    DEWDROP_NEXUS_KEY="key-two" deck
    check "asked when Stardrop has no login" '[ "$(cat "$(dirname "$LAUNCHER")/nexus-key.txt" 2>/dev/null)" = "key-two" ]'
    printf '{\n  "NexusDetails": {\n    "Username": "me",\n    "IsPremium": false,\n    "Key": "c2VjcmV0"\n  }\n}\n' > "$HOME/.config/Stardrop/Data/Settings.json"
    rm -f "$(dirname "$LAUNCHER")/nexus-key.txt"
    DEWDROP_NEXUS_KEY="key-three" deck
    check "skipped when a key is saved" '[ ! -e "$(dirname "$LAUNCHER")/nexus-key.txt" ]'
    rm -f "$HOME/.config/Stardrop/Data/Settings.json"

    echo "== a Steam Deck running another system is set up as one"
    printf 'NAME="Bazzite"\nID=bazzite\n' > "$ROOT/os-release"
    mkdir -p "$ROOT/dmi"
    echo Valve > "$ROOT/dmi/sys_vendor"
    echo Jupiter > "$ROOT/dmi/product_name"
    vdf '' > "$ACCOUNT"
    DEWDROP_DMI="$ROOT/dmi" deck
    check "exit 0" '[ $code = 0 ]' || show
    check "set the launch option" '[ "$(option)" = "\\\"$LAUNCHER\\\" %command%" ] && grep -q "Return to Gaming Mode" "$ROOT/out.txt"' || option

    echo "== other Linux systems aren't changed unless asked"
    vdf '' > "$ACCOUNT"
    cp "$ACCOUNT" "$ROOT/before.vdf"
    printf 'NAME="Ubuntu"\nID=ubuntu\n' > "$ROOT/os-release"
    deck
    check "exit 0" '[ $code = 0 ]' || show
    check "Steam's settings untouched" 'cmp -s "$ACCOUNT" "$ROOT/before.vdf"'
    deck --steam-setup
    check "--steam-setup sets it up" '[ "$(option)" = "\\\"$LAUNCHER\\\" %command%" ]' || option
    pkill -f "$FAKE/steam" 2>/dev/null
    rm -rf "${FAKE:?}" "${HOME:?}/.config/Stardrop"
fi

echo "== an Apple Silicon Mac without Rosetta is offered Rosetta before SMAPI's (Intel) installer runs"
setup
rm -f "$GAME/StardewModdingAPI.dll"
# Stand-ins: a Mac (uname), Apple Silicon (sysctl), no Rosetta (arch fails), and GitHub's SMAPI release (curl).
mkdir -p "$ROOT/bin"
printf '#!/bin/sh\necho Darwin\n' > "$ROOT/bin/uname"
printf '#!/bin/sh\necho 1\n' > "$ROOT/bin/sysctl"
printf '#!/bin/sh\nexit 1\n' > "$ROOT/bin/arch"
printf '#!/bin/sh\necho %s\n' "'{\"tag_name\": \"4.5.2\", \"browser_download_url\": \"https://example.com/SMAPI-4.5.2-installer.zip\"}'" > "$ROOT/bin/curl"
chmod +x "$ROOT/bin/"*
PATH="$ROOT/bin:$PATH" bash "$SCRIPT" --yes --no-launch --dry-run --game-path "$GAME" --mod-source "$MOD_ZIP" < /dev/null > "$ROOT/out.txt" 2>&1
code=$?
check "exit 0" '[ $code = 0 ]' || show
check "offers Rosetta" 'grep -q "needs Rosetta" "$ROOT/out.txt" && grep -q "softwareupdate --install-rosetta" "$ROOT/out.txt"'
check "then goes on to SMAPI" 'grep -q "SMAPI.Installer --install" "$ROOT/out.txt"'
rm -rf "$ROOT/bin"

echo "== the one-line installer (curl ... | bash) downloads install.sh and runs it"
setup
# The extra line checks the installer didn't read the rest of the piped script as its answers.
{ cat "$HERE/../../install.sh"; echo 'echo "still reading the pipe"'; } \
    | DEWDROP_INSTALLER_BASE="file://$(cd "$HERE/../internal" && pwd)" bash -s -- --yes --no-launch --skip-smapi --mod-source "$MOD_ZIP" \
    > "$ROOT/out.txt" 2>&1
code=$?
check "exit 0" '[ $code = 0 ]' || show
check "installed" '[ -f "$GAME/Mods/DewDrop/manifest.json" ] && grep -q "All done" "$ROOT/out.txt"'
check "left the rest of the pipe alone" 'grep -q "still reading the pipe" "$ROOT/out.txt"'
check "cleaned up its download" '! ls "$TMPDIR" | grep -q dewdrop-get'

echo "== $pass passed, $fail failed"
[ "$fail" = 0 ]
