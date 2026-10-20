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
MOD_ZIP="$ROOT/DewDrop-9.9.9.zip"
(cd "$ROOT/mod" && python3 -c 'import sys, zipfile; z = zipfile.ZipFile(sys.argv[1], "w"); [z.write(p) for p in sys.argv[2:]]' "$MOD_ZIP" DewDrop DewDrop/manifest.json DewDrop/DewDrop.dll)

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

echo "== $pass passed, $fail failed"
[ "$fail" = 0 ]
