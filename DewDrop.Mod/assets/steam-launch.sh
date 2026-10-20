#!/bin/sh
# DewDrop's Steam launcher, for Linux and the Steam Deck. Stardew Valley's launch option in Steam runs it as
#
#   "<this file>" %command%
#
# so it gets the command Steam was going to run. That command ends with the game's own launcher, StardewValley (which
# SMAPI's installer replaces with one that opens a terminal window, and Game Mode on the Steam Deck can't show one).
# This puts SMAPI itself in its place, with no terminal, and runs the rest of Steam's command unchanged, so the game
# still starts inside Steam's runtime. SMAPI loads DewDrop's profile folder ("Selected Mods") when that's wanted and
# ready, and the game's Mods folder otherwise. Because it starts SMAPI directly, a game update that puts the original
# StardewValley back doesn't turn mods off. When SMAPI isn't there, Steam's command runs unchanged.
#
# DewDrop restarts the game through this file too, with just the game's launcher as the command.
#
# DewDrop keeps a copy in its data folder and writes it again when it changes, so edits here don't last.
# Files it reads, next to it:
#   start-in-profile   present when "Start in my profile from Steam" is on
#   restart            written by Apply & Restart with the restart's ID: this launch starts in the profile whatever
#                      the setting says, and the file is taken
# What it did goes in steam-launch.log beside it.

here="$(cd "$(dirname "$0")" && pwd)"
data="$(dirname "$(dirname "$here")")"
selected="$data/Selected Mods"
log="$here/steam-launch.log"

say() {
    # Kept short: only the latest launches matter
    if [ -f "$log" ] && [ "$(wc -c < "$log" 2>/dev/null || echo 0)" -gt 65536 ]; then
        tail -n 100 "$log" > "$log.tmp" 2>/dev/null && mv "$log.tmp" "$log"
    fi
    printf '%s %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$*" >> "$log" 2>/dev/null
}

# The game's launcher in Steam's command, if there's one (the native game; not the Windows version through Proton)
game=""
for arg in "$@"; do
    case "$arg" in
        */StardewValley)
            if [ -f "$(dirname "$arg")/Stardew Valley.dll" ]; then game="$(dirname "$arg")"; fi ;;
    esac
done

if [ -z "$game" ]; then
    say "No StardewValley in Steam's command, so it runs unchanged: $*"
    exec "$@"
fi
if [ ! -f "$game/StardewModdingAPI" ]; then
    say "SMAPI isn't in $game, so the game starts without mods"
    exec "$@"
fi

# A profile folder without DewDrop in it would start the game with no Mods button to fix it from
profile_ready() {
    for manifest in "$selected"/*/manifest.json; do
        [ -f "$manifest" ] || continue
        if grep -qi '"UniqueID"[[:space:]]*:[[:space:]]*"friessssss\.DewDrop"' "$manifest" 2>/dev/null; then
            return 0
        fi
    done
    return 1
}

use_profile=0
reason="Start in my profile from Steam is off"
if [ -f "$here/restart" ]; then
    DEWDROP_LAUNCH="$(cat "$here/restart" 2>/dev/null)"
    export DEWDROP_LAUNCH
    rm -f "$here/restart"
    use_profile=1
    reason="restarting into the profile"
elif [ -f "$here/start-in-profile" ]; then
    use_profile=1
    reason="Start in my profile from Steam is on"
fi
if [ "$use_profile" = 1 ] && ! profile_ready; then
    use_profile=0
    reason="$reason, but $selected has no DewDrop in it yet"
fi

# Steam's command again, with SMAPI in place of the game's launcher. A variable from Steam could point SMAPI
# elsewhere, so it goes; --mods-path decides.
unset SMAPI_MODS_PATH
first=1
for arg in "$@"; do
    if [ "$first" = 1 ]; then set --; first=0; fi
    if [ "$arg" = "$game/StardewValley" ]; then
        set -- "$@" "$game/StardewModdingAPI" --no-terminal
        if [ "$use_profile" = 1 ]; then set -- "$@" --mods-path "$selected"; fi
    else
        set -- "$@" "$arg"
    fi
done

if [ "$use_profile" = 1 ]; then
    say "Starting SMAPI on $selected ($reason)"
else
    say "Starting SMAPI on the Mods folder ($reason)"
fi
cd "$game" || exit 1
exec "$@"
