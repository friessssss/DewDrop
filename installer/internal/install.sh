#!/usr/bin/env bash
#
# DewDrop installer for macOS and Linux.
#
# Finds Stardew Valley, installs or updates SMAPI with SMAPI's own installer if needed, then copies DewDrop into
# the Mods folder (keeping your DewDrop settings when updating). It never deletes your mods, saves or settings: an
# old DewDrop folder is only replaced once the new copy is in place.
#
# Usage: install.sh [--game-path DIR] [--mod-source DIR_OR_ZIP] [--yes] [--dry-run] [--skip-smapi] [--no-launch]
#                   [--steam-setup | --no-steam-setup] [--start-in-profile | --no-start-in-profile]
#
# On a Steam Deck (SteamOS), it also sets Stardew Valley's launch option in Steam to DewDrop's launcher, so the game
# starts with mods in Game Mode and DewDrop can restart it there. --steam-setup does that on other Linux systems too.
#
# Written for bash 3.2, which is what macOS ships.

set -u

DEWDROP_ID="friessssss.DewDrop"
DEWDROP_REPO="friessssss/DewDrop"
SMAPI_REPO="Pathoschild/SMAPI"
MIN_SMAPI="4.0.0"
STEAM_APP_ID="413150"
USER_AGENT="DewDrop-Installer"

GAME_PATH=""
MOD_SOURCE=""
YES=0
DRY_RUN=0
SKIP_SMAPI=0
NO_LAUNCH=0
FROM_LAUNCHER=0
STEAM_SETUP=""
START_IN_PROFILE=""

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# The work folder isn't in /tmp: some systems mount /tmp noexec, and SMAPI's installer runs from here.
CACHE_ROOT="${XDG_CACHE_HOME:-$HOME/.cache}"
mkdir -p "$CACHE_ROOT" 2>/dev/null
WORK="$(mktemp -d "$CACHE_ROOT/dewdrop-installer.XXXXXX" 2>/dev/null || mktemp -d "${TMPDIR:-/tmp}/dewdrop-installer.XXXXXX")"
LOG="${TMPDIR:-/tmp}/DewDrop-installer.log"
: > "$LOG" 2>/dev/null || LOG="/dev/null"

case "$(uname -s)" in
    Darwin) OS="mac" ;;
    Linux) OS="linux" ;;
    *) echo "This installer is for macOS and Linux. On Windows, run 'Install on Windows.bat'."; exit 1 ;;
esac

while [ $# -gt 0 ]; do
    case "$1" in
        --game-path) GAME_PATH="${2:-}"; shift ;;
        --mod-source) MOD_SOURCE="${2:-}"; shift ;;
        -y|--yes) YES=1 ;;
        --dry-run) DRY_RUN=1 ;;
        --skip-smapi) SKIP_SMAPI=1 ;;
        --no-launch) NO_LAUNCH=1 ;;
        --from-launcher) FROM_LAUNCHER=1 ;;
        --steam-setup) STEAM_SETUP=1 ;;
        --no-steam-setup) STEAM_SETUP=0 ;;
        --start-in-profile) START_IN_PROFILE=1 ;;
        --no-start-in-profile) START_IN_PROFILE=0 ;;
        -h|--help) sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "Unknown option: $1 (try --help)"; exit 1 ;;
    esac
    shift
done

# ----------------------------------------------------------------------------------------------------------------
# Output

if [ -t 1 ]; then
    C_CYAN=$'\033[36m'; C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'; C_RED=$'\033[31m'; C_GREY=$'\033[90m'
    C_BOLD=$'\033[1m'; C_MAGENTA=$'\033[35m'; C_RESET=$'\033[0m'
else
    C_CYAN=""; C_GREEN=""; C_YELLOW=""; C_RED=""; C_GREY=""; C_BOLD=""; C_MAGENTA=""; C_RESET=""
fi

log() { printf '%s\n' "$*" >> "$LOG" 2>/dev/null; }
step() { printf '\n%s==> %s%s\n' "$C_CYAN" "$1" "$C_RESET"; log "==> $1"; }
ok() { printf '    %s[ok]%s %s\n' "$C_GREEN" "$C_RESET" "$1"; log "    [ok] $1"; }
note() { printf '         %s%s%s\n' "$C_GREY" "$1" "$C_RESET"; log "         $1"; }
warn() { printf '    %s[!]  %s%s\n' "$C_YELLOW" "$1" "$C_RESET"; log "    [!] $1"; }
bad() { printf '    %s[x]  %s%s\n' "$C_RED" "$1" "$C_RESET"; log "    [x] $1"; }
dry() { printf '    %s(dry run) would %s%s\n' "$C_MAGENTA" "$1" "$C_RESET"; log "    (dry run) would $1"; }

finish() {
    local code="$1"
    rm -rf "$WORK" 2>/dev/null
    [ "$LOG" != "/dev/null" ] && printf '    %sLog saved to %s%s\n' "$C_GREY" "$LOG" "$C_RESET"
    if [ "$FROM_LAUNCHER" = 1 ] && [ -t 0 ]; then
        printf '\n    %sPress Enter to close this window.%s' "$C_GREY" "$C_RESET"
        read -r _ || true
    fi
    exit "$code"
}

die() {
    printf '\n'
    bad "$1"
    finish 1
}

banner() {
    printf '\n'
    printf '%s   .    %s\n' "$C_CYAN" "$C_RESET"
    printf '%s  / \\   %s%s DewDrop installer%s\n' "$C_CYAN" "$C_RESET" "$C_BOLD" "$C_RESET"
    printf '%s (   )  %s%s The mod manager that lives inside Stardew Valley%s\n' "$C_CYAN" "$C_RESET" "$C_GREY" "$C_RESET"
    printf "%s  \`-\`   %s\n\n" "$C_CYAN" "$C_RESET"
    if [ "$DRY_RUN" = 1 ]; then
        printf '  %sDRY RUN: nothing will be changed. Lines starting with (dry run) show what would happen.%s\n' "$C_MAGENTA" "$C_RESET"
    fi
}

# ----------------------------------------------------------------------------------------------------------------
# Questions

# ask_yes_no "Question?" y|n  -> returns 0 for yes
ask_yes_no() {
    local question="$1" default="${2:-y}" hint reply
    if [ "$default" = "y" ]; then hint="[Y/n]"; else hint="[y/N]"; fi
    if [ "$YES" = 1 ] || [ ! -t 0 ]; then
        printf '    ? %s %s %s%s (automatic)%s\n' "$question" "$hint" "$C_GREY" "$([ "$default" = y ] && echo yes || echo no)" "$C_RESET"
        [ "$default" = "y" ]
        return
    fi
    while true; do
        printf '    %s? %s %s%s ' "$C_BOLD" "$question" "$hint" "$C_RESET"
        read -r reply || reply=""
        reply="$(printf '%s' "$reply" | tr '[:upper:]' '[:lower:]' | tr -d '[:space:]')"
        case "$reply" in
            "") [ "$default" = "y" ]; return ;;
            y|yes) return 0 ;;
            n|no) return 1 ;;
            *) note "Please type Y or N, then press Enter." ;;
        esac
    done
}

# ----------------------------------------------------------------------------------------------------------------
# Helpers

# version_lt A B -> true if A < B (numeric, dot-separated)
version_lt() {
    local a="$1" b="$2" i x y
    local IFS=.
    set -- $a; local a1="${1:-0}" a2="${2:-0}" a3="${3:-0}"
    set -- $b; local b1="${1:-0}" b2="${2:-0}" b3="${3:-0}"
    for i in 1 2 3; do
        eval "x=\$a$i; y=\$b$i"
        x="${x%%[!0-9]*}"; y="${y%%[!0-9]*}"
        [ "${x:-0}" -lt "${y:-0}" ] && return 0
        [ "${x:-0}" -gt "${y:-0}" ] && return 1
    done
    return 1
}

# The shell writes the file rather than curl -o: Ubuntu's snap curl can't see this script's folders.
download() { # url file
    if command -v curl >/dev/null 2>&1 && curl -fsSL --retry 2 -A "$USER_AGENT" "$1" > "$2"; then return 0; fi
    if command -v wget >/dev/null 2>&1 && wget -q -U "$USER_AGENT" -O - "$1" > "$2"; then return 0; fi
    return 1
}

fetch() { # url -> stdout
    if command -v curl >/dev/null 2>&1 && curl -fsSL --retry 2 -A "$USER_AGENT" -H "Accept: application/vnd.github+json" "$1"; then return 0; fi
    if command -v wget >/dev/null 2>&1 && wget -q -U "$USER_AGENT" -O - "$1"; then return 0; fi
    return 1
}

extract_zip() { # zip dir
    mkdir -p "$2"
    if command -v unzip >/dev/null 2>&1; then
        unzip -q -o "$1" -d "$2"
    elif command -v python3 >/dev/null 2>&1; then
        python3 -c 'import sys, zipfile; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$1" "$2"
    elif command -v bsdtar >/dev/null 2>&1; then
        bsdtar -xf "$1" -C "$2"
    else
        return 1
    fi
}

lower() { printf '%s' "$1" | tr '[:upper:]' '[:lower:]'; }

# Reads a top-level string field (UniqueID, Version) from a manifest.json without jq. It tracks nesting, strings and
# comments, so a dependency that names DewDrop never reads as DewDrop itself.
manifest_field() { # file field
    awk -v want="$(lower "$2")" '
    BEGIN { depth = 0; instr = 0; esc = 0; incomment = 0; key = ""; take = 0 }
    {
        line = $0
        n = length(line)
        for (i = 1; i <= n; i++) {
            c = substr(line, i, 1)
            if (incomment) { if (c == "*" && substr(line, i + 1, 1) == "/") { incomment = 0; i++ }; continue }
            if (instr) {
                if (esc) { esc = 0; tok = tok c; continue }
                if (c == "\\") { esc = 1; continue }
                if (c != "\"") { tok = tok c; continue }
                instr = 0
                if (take) { print tok; exit }
                key = (depth == 1) ? tolower(tok) : ""
                continue
            }
            if (c == "/" && substr(line, i + 1, 1) == "/") break
            if (c == "/" && substr(line, i + 1, 1) == "*") { incomment = 1; i++; continue }
            if (c == "\"") { instr = 1; tok = ""; continue }
            if (c == "{" || c == "[") { depth++; take = 0; continue }
            if (c == "}" || c == "]") { depth--; take = 0; continue }
            if (c == ":") { take = (depth == 1 && key == want); continue }
            if (c == ",") { key = ""; take = 0 }
        }
    }' "$1" 2>/dev/null
}

is_game_dir() { [ -n "$1" ] && [ -f "$1/Stardew Valley.dll" ]; }

game_store() {
    case "$1" in
        *steamapps*) echo "Steam" ;;
        *GOG*|*gog*) echo "GOG" ;;
        *) echo "Other" ;;
    esac
}

smapi_version() { # game dir -> version or empty
    local dll="$1/StardewModdingAPI.dll" v
    [ -f "$dll" ] || return 0
    # The informational version is stored as plain text like "4.5.2+<commit>".
    v="$(LC_ALL=C grep -aoE '[0-9]+\.[0-9]+\.[0-9]+\+[0-9a-f]{7,}' "$dll" 2>/dev/null | head -n 1)"
    printf '%s' "${v%%+*}"
}

# ----------------------------------------------------------------------------------------------------------------
# Steps

CANDIDATES=""
add_candidate() {
    local p="$1" real
    [ -n "$p" ] || return 0
    is_game_dir "$p" || return 0
    real="$(cd "$p" 2>/dev/null && pwd -P)" || return 0
    case "
$CANDIDATES
" in
        *"
$real
"*) return 0 ;;
    esac
    CANDIDATES="${CANDIDATES:+$CANDIDATES
}$real"
}

steam_roots() {
    if [ "$OS" = "mac" ]; then
        echo "$HOME/Library/Application Support/Steam"
    else
        echo "$HOME/.steam/steam"
        echo "$HOME/.local/share/Steam"
        echo "$HOME/.steam/debian-installation"
        echo "$HOME/.var/app/com.valvesoftware.Steam/data/Steam"
        echo "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam"
        echo "$HOME/snap/steam/common/.local/share/Steam"
    fi
}

game_in_library() { # library root
    if [ "$OS" = "mac" ]; then
        echo "$1/steamapps/common/Stardew Valley/Contents/MacOS"
    else
        echo "$1/steamapps/common/Stardew Valley"
    fi
}

normalize_picked() { # user input -> path
    local p="$1"
    # Dragging a folder into Terminal pastes it with quotes or backslash-escaped spaces.
    p="$(printf '%s' "$p" | sed -e "s/^[[:space:]]*['\"]//" -e "s/['\"][[:space:]]*$//" -e 's/\\ / /g' -e 's/[[:space:]]*$//')"
    case "$p" in "~"*) p="$HOME${p#\~}" ;; esac
    printf '%s' "$p"
}

find_game() {
    step "Looking for Stardew Valley"
    if [ -n "$GAME_PATH" ]; then
        GAME_PATH="$(normalize_picked "$GAME_PATH")"
        local given
        for given in "$GAME_PATH" "${GAME_PATH%/}/Contents/MacOS" "${GAME_PATH%/}/game" "${GAME_PATH%/}/Stardew Valley"; do
            if is_game_dir "$given"; then
                GAME="$(cd "$given" && pwd -P)"
                ok "Using $GAME"
                return
            fi
        done
        die "There's no 'Stardew Valley.dll' in $GAME_PATH."
    fi

    local root lib vdf line
    while IFS= read -r root; do
        [ -d "$root" ] || continue
        add_candidate "$(game_in_library "$root")"
        for vdf in "$root/steamapps/libraryfolders.vdf" "$root/config/libraryfolders.vdf"; do
            [ -f "$vdf" ] || continue
            while IFS= read -r lib; do
                add_candidate "$(game_in_library "$lib")"
            done <<EOF
$(sed -n 's/^[[:space:]]*"path"[[:space:]]*"\(.*\)"[[:space:]]*$/\1/p' "$vdf" | sed 's/\\\\/\\/g')
EOF
        done
    done <<EOF
$(steam_roots)
EOF
    if [ "$OS" = "mac" ]; then
        add_candidate "/Applications/Stardew Valley.app/Contents/MacOS"
        add_candidate "$HOME/Applications/Stardew Valley.app/Contents/MacOS"
    else
        add_candidate "$HOME/GOG Games/Stardew Valley/game"
        add_candidate "$HOME/Games/stardew-valley/game"
    fi
    if [ -f "$HOME/stardewvalley.targets" ]; then
        line="$(sed -n 's/.*<GamePath>[[:space:]]*\([^<]*[^<[:space:]]\)[[:space:]]*<\/GamePath>.*/\1/p' "$HOME/stardewvalley.targets" | head -n 1)"
        add_candidate "$line"
    fi

    local count=0
    [ -n "$CANDIDATES" ] && count="$(printf '%s\n' "$CANDIDATES" | wc -l | tr -d ' ')"
    if [ "$count" = 1 ]; then
        GAME="$CANDIDATES"
        ok "Found it: $GAME ($(game_store "$GAME"))"
        return
    fi
    if [ "$count" -gt 1 ]; then
        note "Found more than one copy of the game:"
        local i=1 choice
        while IFS= read -r line; do
            printf '      [%s] %s (%s)\n' "$i" "$line" "$(game_store "$line")"
            i=$((i + 1))
        done <<EOF
$CANDIDATES
EOF
        if [ "$YES" = 1 ] || [ ! -t 0 ]; then
            choice=1
        else
            while true; do
                printf '    %s? Which one do you play?%s ' "$C_BOLD" "$C_RESET"
                read -r choice || choice=""
                case "$choice" in ''|*[!0-9]*) ;; *) [ "$choice" -ge 1 ] && [ "$choice" -le "$count" ] && break ;; esac
                note "Type a number from 1 to $count, then press Enter."
            done
        fi
        GAME="$(printf '%s\n' "$CANDIDATES" | sed -n "${choice}p")"
        ok "Using $GAME"
        return
    fi

    warn "I couldn't find Stardew Valley automatically."
    { [ "$YES" = 1 ] || [ ! -t 0 ]; } && die "Run the installer again with --game-path \"<your game folder>\"."
    if [ "$OS" = "mac" ]; then
        note "Drag your Stardew Valley app (or its game folder) into this window, then press Enter."
        note "Tip: in Steam, right-click Stardew Valley > Manage > Browse local files."
    else
        note "Type or paste the path to your game folder (the one with 'Stardew Valley.dll'), then press Enter."
        note "Tip: in Steam, right-click Stardew Valley > Manage > Browse local files."
    fi
    local picked p
    while true; do
        printf '    %s? Game folder:%s ' "$C_BOLD" "$C_RESET"
        read -r picked || picked=""
        [ -n "$picked" ] || die "No game folder was chosen."
        picked="$(normalize_picked "$picked")"
        for p in "$picked" "${picked%/}/Contents/MacOS" "${picked%/}/game" "${picked%/}/Stardew Valley"; do
            if is_game_dir "$p"; then
                GAME="$(cd "$p" && pwd -P)"
                ok "Using $GAME"
                return
            fi
        done
        bad "That folder doesn't contain 'Stardew Valley.dll'. Try again."
    done
}

check_first_run() {
    local data
    # Flatpak and Snap Steam keep the game's data in their own sandbox folders.
    for data in "$HOME/.config/StardewValley" "$HOME/.var/app/com.valvesoftware.Steam/.config/StardewValley" "$HOME/snap/steam/common/.config/StardewValley"; do
        [ -d "$data" ] && return 0
    done
    warn "It looks like Stardew Valley has never been started on this computer."
    note "SMAPI recommends starting the game once without mods first, so it can finish its first-time setup."
    ask_yes_no "Continue anyway?" y || die "Start Stardew Valley once, close it at the title screen, then run this installer again."
}

# On Linux, a game forced to run through Proton is the Windows version, which the Linux SMAPI can't mod.
check_native_game() {
    [ "$OS" = "linux" ] || return 0
    if [ -f "$GAME/Stardew Valley.exe" ] && [ ! -e "$GAME/StardewValley" ] && [ ! -e "$GAME/Stardew Valley" ]; then
        warn "This is the Windows version of the game, which Steam runs through Proton."
        note "SMAPI for Linux can't load mods into it. In Steam, open Stardew Valley > Properties > Compatibility,"
        note "turn off the Steam Play compatibility tool, let Steam download the Linux version, then run this again."
        ask_yes_no "Continue anyway?" n || die "Switch Stardew Valley to the Linux version first (see above)."
    fi
}

# SMAPI's installer retries forever on a file it can't delete when it runs without prompts, so check first.
check_smapi_writable() {
    local bad="" p
    [ -w "$GAME" ] || bad="$GAME"
    for p in "$GAME/smapi-internal" "$GAME/Mods/ConsoleCommands" "$GAME/Mods/SaveBackup"; do
        [ -n "$bad" ] && break
        [ -e "$p" ] || continue
        bad="$(find "$p" -type d 2>/dev/null | while IFS= read -r d; do [ -w "$d" ] || { echo "$d"; break; }; done)"
    done
    [ -z "$bad" ] && [ -d "$GAME/Mods" ] && [ ! -w "$GAME/Mods" ] && bad="$GAME/Mods"
    if [ -n "$bad" ]; then
        die "I can't change files in $bad (it may belong to another user, for example after installing with sudo). Fix it with:  sudo chown -R \"$(id -un)\" \"$GAME\"  then run this installer again."
    fi
}

# SMAPI's macOS installer is an Intel program, so an Apple Silicon Mac needs Apple's Rosetta to run it. Without
# Rosetta, macOS refuses to start it ("Bad CPU type in executable").
rosetta_works() { arch -x86_64 /usr/bin/true >/dev/null 2>&1; }

ensure_rosetta() {
    [ "$OS" = "mac" ] || return 0
    # Not uname -m, which says x86_64 when this shell itself is running under Rosetta.
    [ "$(sysctl -n hw.optional.arm64 2>/dev/null)" = 1 ] || return 0
    rosetta_works && return 0
    warn "SMAPI's installer is made for Intel Macs, so this Mac needs Rosetta to run it."
    note "Rosetta is Apple's free add-on for running Intel apps. Installing it asks for your Mac password."
    ask_yes_no "Install Rosetta now?" y || die "SMAPI can't be installed without Rosetta. Install it with:  softwareupdate --install-rosetta  then run this installer again."
    if [ "$DRY_RUN" = 1 ]; then
        dry "run: sudo softwareupdate --install-rosetta --agree-to-license"
        return
    fi
    sudo /usr/sbin/softwareupdate --install-rosetta --agree-to-license
    rosetta_works || die "Rosetta didn't install. Install it with:  softwareupdate --install-rosetta  then run this installer again."
    ok "Rosetta is installed."
}

game_running() {
    pgrep -x 'StardewModdingAPI|StardewModdingA|Stardew Valley|StardewValley' >/dev/null 2>&1
}

wait_game_closed() {
    game_running || return 0
    warn "Stardew Valley is running. Please close the game first."
    local waited=0 reply
    while game_running; do
        if [ "$YES" = 1 ] || [ ! -t 0 ]; then
            [ "$waited" -ge 300 ] && die "The game is still running."
            sleep 2; waited=$((waited + 2)); continue
        fi
        printf '    %s? Press Enter once the game is closed (or type Q to quit):%s ' "$C_BOLD" "$C_RESET"
        read -r reply || reply=""
        [ "$(lower "$reply")" = "q" ] && die "Cancelled while the game was running."
    done
    ok "The game is closed."
}

install_smapi() {
    step "Checking SMAPI (the mod loader DewDrop runs on)"
    local installed latest="" url="" json
    installed="$(smapi_version "$GAME")"
    [ -f "$GAME/StardewModdingAPI.dll" ] && [ -z "$installed" ] && installed="unknown"

    if json="$(fetch "https://api.github.com/repos/$SMAPI_REPO/releases/latest" 2>/dev/null)"; then
        latest="$(printf '%s' "$json" | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
        url="$(printf '%s' "$json" | grep -oE '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]*/SMAPI-[0-9.]+-installer\.zip"' | head -n 1 | sed 's/.*"\(http[^"]*\)"$/\1/')"
    else
        warn "Couldn't check GitHub for the latest SMAPI."
    fi

    if [ "$installed" = "unknown" ]; then
        warn "SMAPI is installed, but I couldn't tell which version. It may be too old for DewDrop (SMAPI 3 can't run Stardew Valley 1.6)."
        if [ "$SKIP_SMAPI" = 1 ] || ! ask_yes_no "Install the latest SMAPI${latest:+ ($latest)} over it? (recommended)" y; then
            SMAPI_VERSION="(unknown version)"; return
        fi
    elif [ -n "$installed" ]; then
        ok "SMAPI $installed is installed."
        if version_lt "$installed" "$MIN_SMAPI"; then
            warn "DewDrop needs SMAPI $MIN_SMAPI or later, so SMAPI has to be updated."
            [ "$SKIP_SMAPI" = 1 ] && die "SMAPI $installed is too old for DewDrop and --skip-smapi was given. Update SMAPI from https://smapi.io."
        elif [ -n "$latest" ] && version_lt "$installed" "$latest"; then
            note "SMAPI $latest is available."
            if [ "$SKIP_SMAPI" = 1 ] || ! ask_yes_no "Update SMAPI to $latest? (recommended)" y; then
                SMAPI_VERSION="$installed"; return
            fi
        else
            SMAPI_VERSION="$installed"; return
        fi
    else
        note "SMAPI isn't installed yet. I'll download the official SMAPI installer from github.com/$SMAPI_REPO"
        note "and run it for you."
        [ "$SKIP_SMAPI" = 1 ] && die "SMAPI is missing and --skip-smapi was given."
        ask_yes_no "Install SMAPI${latest:+ $latest} now?" y || die "DewDrop needs SMAPI. Install it from https://smapi.io, then run this installer again."
    fi

    [ -n "$url" ] || die "Couldn't download SMAPI automatically. Get it from https://smapi.io, run its installer, then run this one again."
    local internal="linux"
    [ "$OS" = "mac" ] && internal="macOS"
    ensure_rosetta
    if [ "$DRY_RUN" = 1 ]; then
        dry "download $url and run: internal/$internal/SMAPI.Installer --install --game-path \"$GAME\" --no-prompt"
        SMAPI_VERSION="$latest"; return
    fi

    check_smapi_writable
    note "Downloading $(basename "$url")..."
    download "$url" "$WORK/smapi.zip" || die "The SMAPI download failed. Check your internet connection, or get it from https://smapi.io."
    extract_zip "$WORK/smapi.zip" "$WORK/smapi" || die "Couldn't unzip SMAPI: install 'unzip' (or python3) and try again."
    local exe
    exe="$(find "$WORK/smapi" -type f -path "*/internal/$internal/SMAPI.Installer" | head -n 1)"
    [ -n "$exe" ] || die "The SMAPI download didn't contain internal/$internal/SMAPI.Installer."
    chmod +x "$exe" 2>/dev/null
    [ "$OS" = "mac" ] && xattr -r -d com.apple.quarantine "$WORK/smapi" 2>/dev/null

    note "Running the SMAPI installer..."
    (cd "$(dirname "$(dirname "$(dirname "$exe")")")" && exec "$exe" --install --game-path "$GAME" --no-prompt) &
    local pid=$! waited=0
    # It takes seconds; if it gets stuck on a file it can't replace, stop it rather than wait forever.
    while kill -0 "$pid" 2>/dev/null; do
        if [ "$waited" -ge 180 ]; then
            kill "$pid" 2>/dev/null
            wait "$pid" 2>/dev/null
            die "The SMAPI installer didn't finish within 3 minutes, so I stopped it. Install SMAPI yourself from https://smapi.io, then run this installer again."
        fi
        sleep 1
        waited=$((waited + 1))
    done
    local status=0
    wait "$pid" 2>/dev/null || status=$?
    printf '\n%s==> Back in the DewDrop installer%s\n' "$C_CYAN" "$C_RESET"
    # 126 and 127 come from the shell, not SMAPI: the installer never started.
    if [ "$status" = 126 ] || [ "$status" = 127 ]; then
        die "The SMAPI installer couldn't start (see the message above). Install SMAPI yourself from https://smapi.io, then run this installer again."
    fi

    local now
    now="$(smapi_version "$GAME")"
    if [ ! -f "$GAME/StardewModdingAPI.dll" ] || { [ -n "$latest" ] && [ -n "$now" ] && version_lt "$now" "$latest"; }; then
        die "The SMAPI installer finished, but SMAPI still looks missing or out of date. See the messages above, or install it from https://smapi.io."
    fi
    SMAPI_VERSION="${now:-$latest}"
    ok "SMAPI $SMAPI_VERSION is installed."
}

find_manifest_dir() { # dir -> first folder whose manifest is DewDrop
    local m
    find "$1" -name manifest.json -type f 2>/dev/null | while IFS= read -r m; do
        if [ "$(lower "$(manifest_field "$m" UniqueID)")" = "$(lower "$DEWDROP_ID")" ]; then
            dirname "$m"
            break
        fi
    done | head -n 1
}

get_source() {
    step "Getting DewDrop ready"
    local c dir
    for c in "$MOD_SOURCE" "$SCRIPT_DIR/DewDrop.zip" "$SCRIPT_DIR/DewDrop"; do
        [ -n "$c" ] && [ -e "$c" ] || continue
        dir="$c"
        if [ -f "$c" ]; then
            dir="$WORK/dewdrop-zip"
            extract_zip "$c" "$dir" || die "Couldn't unzip $c: install 'unzip' (or python3) and try again."
        fi
        SOURCE="$(find_manifest_dir "$dir")"
        if [ -n "$SOURCE" ]; then
            SOURCE_VERSION="$(manifest_field "$SOURCE/manifest.json" Version)"
            ok "DewDrop $SOURCE_VERSION (from $SOURCE)"
            return
        fi
        [ "$c" = "$MOD_SOURCE" ] && die "'$MOD_SOURCE' doesn't contain DewDrop's manifest.json."
    done

    note "No bundled copy of DewDrop next to the installer, so I'll download the latest release from github.com/$DEWDROP_REPO"
    local json url
    json="$(fetch "https://api.github.com/repos/$DEWDROP_REPO/releases/latest" 2>/dev/null)" || die "Couldn't reach GitHub. Download DewDrop from https://github.com/$DEWDROP_REPO/releases and try again."
    url="$(printf '%s' "$json" | grep -oE '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]*/DewDrop[-. ][0-9.]+\.zip"' | head -n 1 | sed 's/.*"\(http[^"]*\)"$/\1/')"
    [ -n "$url" ] || die "The latest DewDrop release has no mod zip. Download it from https://github.com/$DEWDROP_REPO/releases."
    if [ "$DRY_RUN" = 1 ]; then
        dry "download $url"
        SOURCE=""; SOURCE_VERSION="(latest)"; return
    fi
    download "$url" "$WORK/dewdrop.zip" || die "The DewDrop download failed."
    extract_zip "$WORK/dewdrop.zip" "$WORK/dewdrop-release" || die "Couldn't unzip DewDrop."
    SOURCE="$(find_manifest_dir "$WORK/dewdrop-release")"
    [ -n "$SOURCE" ] || die "The downloaded DewDrop zip doesn't contain DewDrop."
    SOURCE_VERSION="$(manifest_field "$SOURCE/manifest.json" Version)"
    ok "Downloaded DewDrop $SOURCE_VERSION"
}

# Lists folders under Mods whose manifest is DewDrop (three levels deep, skipping dot-folders like SMAPI does).
find_installed() {
    local mods="$1" m d
    find -H "$mods" -mindepth 2 -maxdepth 4 -name manifest.json -type f 2>/dev/null | while IFS= read -r m; do
        # Skip dot-folders inside Mods (the game folder itself may sit under ~/.steam or ~/.local).
        case "/${m#"$mods"/}" in */.*) continue ;; esac
        if [ "$(lower "$(manifest_field "$m" UniqueID)")" = "$(lower "$DEWDROP_ID")" ]; then
            d="$(dirname "$m")"
            [ -L "$d" ] && continue
            [ "$d" = "$SOURCE" ] && continue
            echo "$d"
        fi
    done
}

install_dewdrop() {
    step "Installing DewDrop into your Mods folder"
    local mods="$GAME/Mods" target existing keep extra parent name n
    if [ ! -d "$mods" ]; then
        if [ "$DRY_RUN" = 1 ]; then dry "create $mods"; else mkdir -p "$mods"; fi
    fi
    target="$mods/DewDrop"
    DEWDROP_DIR="$target"
    existing="$(find_installed "$mods")"
    if [ -n "$existing" ]; then
        keep="$(printf '%s\n' "$existing" | grep -Fx "$target" | head -n 1)"
        [ -n "$keep" ] || keep="$(printf '%s\n' "$existing" | head -n 1)"
        target="$keep"
        DEWDROP_DIR="$target"
        note "DewDrop $(manifest_field "$target/manifest.json" Version) is already installed in $target; it'll be updated in place."
        extra="$(printf '%s\n' "$existing" | grep -Fxv "$keep")"
        if [ -n "$extra" ]; then
            warn "There are other copies of DewDrop in your Mods folder. SMAPI refuses to load a mod twice:"
            printf '%s\n' "$extra" | while IFS= read -r d; do note "  $d"; done
            if ask_yes_no "Turn the extra copies off? (they get a '.' in front of their folder name, so SMAPI skips them)" y; then
                printf '%s\n' "$extra" | while IFS= read -r d; do
                    parent="$(dirname "$d")"; name=".$(basename "$d")-duplicate"; n=2
                    while [ -e "$parent/$name" ]; do name=".$(basename "$d")-duplicate-$n"; n=$((n + 1)); done
                    if [ "$DRY_RUN" = 1 ]; then dry "rename $d to $name"; else mv "$d" "$parent/$name" && ok "Turned off $d"; fi
                done
            fi
        fi
    elif [ -e "$target" ]; then
        die "There's already a folder at $target that isn't DewDrop. Rename or move it, then run the installer again."
    fi

    if [ -n "$existing" ]; then
        local have
        have="$(manifest_field "$target/manifest.json" Version)"
        if [ -n "$have" ] && [ -n "$SOURCE_VERSION" ] && version_lt "$SOURCE_VERSION" "$have"; then
            warn "The DewDrop you have ($have) is newer than the one in this installer ($SOURCE_VERSION)."
            if ! ask_yes_no "Replace it with the older $SOURCE_VERSION?" n; then
                ok "Keeping DewDrop $have"
                SOURCE_VERSION="$have"
                return
            fi
        fi
    fi

    if [ -n "$SOURCE" ] && [ "$SOURCE" = "$target" ]; then
        ok "DewDrop $SOURCE_VERSION is already in $target"
        return
    fi
    if [ "$DRY_RUN" = 1 ]; then
        dry "copy DewDrop $SOURCE_VERSION into $target (keeping config.json)"
        return
    fi

    parent="$(dirname "$target")"
    local token staging old
    token="$$"
    staging="$parent/.dewdrop-installer-$token"
    old="$parent/.dewdrop-old-$token"
    cp -R "$SOURCE" "$staging" || die "Couldn't copy DewDrop into $parent."
    [ "$OS" = "mac" ] && xattr -r -d com.apple.quarantine "$staging" 2>/dev/null
    if [ -d "$target" ]; then
        [ -f "$target/config.json" ] && cp "$target/config.json" "$staging/config.json"
        mv "$target" "$old" || { rm -rf "$staging"; die "Couldn't move the old DewDrop aside. Is the game still running?"; }
        if ! mv "$staging" "$target"; then
            mv "$old" "$target"
            die "Couldn't put the new DewDrop in place; the old one was restored."
        fi
        rm -rf "$old"
    else
        mv "$staging" "$target" || die "Couldn't put DewDrop in place."
    fi
    ok "DewDrop $SOURCE_VERSION is in $target"
}

launch_game() {
    local store
    store="$(game_store "$GAME")"
    if [ "$DRY_RUN" = 1 ]; then dry "start the game"; return; fi
    if [ "$store" = "Steam" ]; then
        if [ "$OS" = "mac" ]; then open "steam://rungameid/$STEAM_APP_ID"; else xdg-open "steam://rungameid/$STEAM_APP_ID" >/dev/null 2>&1 || steam "steam://rungameid/$STEAM_APP_ID" >/dev/null 2>&1 & fi
    elif [ "$OS" = "mac" ]; then
        case "$GAME" in
            *.app/Contents/MacOS) open "${GAME%/Contents/MacOS}" ;;
            *) open -a Terminal "$GAME/StardewModdingAPI" ;;
        esac
    else
        (cd "$GAME" && nohup ./StardewValley >/dev/null 2>&1 &)
    fi
}


# ----------------------------------------------------------------------------------------------------------------
# Steam on Linux and the Steam Deck

# SteamOS: a Steam Deck, or another device running it. DEWDROP_OS_RELEASE points at another file (the tests use it).
is_steamos() {
    [ "$OS" = "linux" ] || return 1
    grep -qiE '^ID="?steamos"?[[:space:]]*$' "${DEWDROP_OS_RELEASE:-/etc/os-release}" 2>/dev/null
}

# Where DewDrop (and Stardrop) keep their data: .NET's ApplicationData folder.
dewdrop_data() { printf '%s' "${XDG_CONFIG_HOME:-$HOME/.config}/Stardrop/Data"; }

# Steam's own folder (not Flatpak's or Snap's: games they start see sandboxed folders, and their Steam has to be
# closed another way). The Steam Deck's is ~/.local/share/Steam.
linux_steam_root() {
    local r
    for r in "$HOME/.steam/steam" "$HOME/.local/share/Steam" "$HOME/.steam/debian-installation"; do
        [ -d "$r/userdata" ] && { (cd "$r" && pwd -P); return 0; }
    done
    return 1
}

# The signed-in account's localconfig.vdf: the one marked MostRecent in loginusers.vdf, else the newest.
steam_user_config() { # steam root
    local id file
    id="$(awk '
        /^[[:space:]]*"[0-9]+"[[:space:]]*$/ { gsub(/[^0-9]/, ""); account = length($0) == 17 ? $0 : ""; next }
        tolower($0) ~ /"mostrecent"[[:space:]]*"1"/ { if (account != "") { print account; exit } }
    ' "$1/config/loginusers.vdf" 2>/dev/null)"
    if [ -n "$id" ]; then
        # SteamID64 minus the base of individual accounts gives the userdata folder's name (in the shell, which counts
        # in 64 bits; awk's numbers would round it)
        id="$((id - 76561197960265728))"
        file="$1/userdata/$id/config/localconfig.vdf"
        [ -f "$file" ] && { printf '%s' "$file"; return 0; }
    fi
    file="$(ls -t "$1"/userdata/*/config/localconfig.vdf 2>/dev/null | head -n 1)"
    [ -n "$file" ] && printf '%s' "$file"
}

# Reads (get) or changes (set) Stardew Valley's launch option in a localconfig.vdf, with the same parsing as the
# Windows installer's Find-SteamAppBlock: quoted strings and braces, the game's block found under
# Software > Valve > Steam > apps. get prints the option (empty when none is set); set prints the whole file with the
# option set to $DD_OPTION. Both fail (exit 3) when the file doesn't have that layout, so nothing is written to it.
vdf_launch_option() { # get|set file
    DD_MODE="$1" DD_APP="$STEAM_APP_ID" awk '
    function tail_is(n, a, b, c, d, e) {
        if (n == 4) return depth >= 4 && names[depth - 3] == a && names[depth - 2] == b && names[depth - 1] == c && names[depth] == d
        return depth >= 5 && names[depth - 4] == a && names[depth - 3] == b && names[depth - 2] == c && names[depth - 1] == d && names[depth] == e
    }
    function unescape(s,    out, i, c, n) {
        out = ""; n = length(s)
        for (i = 1; i <= n; i++) {
            c = substr(s, i, 1)
            if (c == "\\" && i < n && (substr(s, i + 1, 1) == "\"" || substr(s, i + 1, 1) == "\\")) { i++; c = substr(s, i, 1) }
            out = out c
        }
        return out
    }
    function escape(s,    out, i, c) {
        out = ""
        for (i = 1; i <= length(s); i++) {
            c = substr(s, i, 1)
            if (c == "\\" || c == "\"") out = out "\\"
            out = out c
        }
        return out
    }
    function tabs(n,    s) { s = ""; while (n-- > 0) s = s "\t"; return s }
    BEGIN { mode = ENVIRON["DD_MODE"]; app = ENVIRON["DD_APP"]; depth = 0; pending = 0; instr = 0
            appsLine = 0; appLine = 0; appClosed = 0; optLine = 0 }
    {
        line = $0; n = length(line)
        for (i = 1; i <= n; i++) {
            c = substr(line, i, 1)
            if (instr) {
                if (c == "\\") { tok = tok c substr(line, i + 1, 1); i++; continue }
                if (c != "\"") { tok = tok c; continue }
                instr = 0
                if (!pending) { pending = 1; key = tok; continue }
                pending = 0
                if (appLine && !appClosed && depth == appDepth && tolower(key) == "launchoptions" && sLine == NR) {
                    optLine = NR; optStart = sCol; optEnd = i - 1; value = tok
                }
                continue
            }
            if (c == "\"") { instr = 1; tok = ""; sLine = NR; sCol = i + 1; continue }
            if (c == "{") {
                depth++; names[depth] = pending ? tolower(key) : ""; opened[depth] = NR SUBSEP i; pending = 0
                if (tail_is(4, "software", "valve", "steam", "apps")) { appsLine = NR; appsCol = i; appsDepth = depth }
                else if (!appLine && tail_is(5, "software", "valve", "steam", "apps", app)) { appLine = NR; appCol = i; appDepth = depth }
                continue
            }
            if (c == "}") {
                if (depth > 0) {
                    if (appLine && opened[depth] == appLine SUBSEP appCol) appClosed = 1
                    depth--
                }
                pending = 0
            }
        }
        lines[NR] = line
    }
    END {
        if (!appsLine) exit 3
        if (mode == "get") { printf "%s\n", unescape(value); exit 0 }
        wanted = escape(ENVIRON["DD_OPTION"])
        for (l = 1; l <= NR; l++) {
            line = lines[l]
            if (optLine && l == optLine) {
                line = substr(line, 1, optStart - 1) wanted substr(line, optEnd + 1)
            } else if (!optLine && appLine && l == appLine) {
                line = substr(line, 1, appCol) "\n" tabs(appDepth) "\"LaunchOptions\"\t\t\"" wanted "\"" substr(line, appCol + 1)
            } else if (!optLine && !appLine && l == appsLine) {
                ind = "\n" tabs(appsDepth)
                line = substr(line, 1, appsCol) ind "\"" app "\"" ind "{" ind "\t\"LaunchOptions\"\t\t\"" wanted "\"" ind "}" substr(line, appsCol + 1)
            }
            print line
        }
    }' "$2"
}

steam_running() { pgrep -x steam >/dev/null 2>&1; }

wait_steam_closed() { # half-seconds
    local waited=0
    while steam_running; do
        [ "$waited" -ge "$1" ] && return 1
        sleep 0.5
        waited=$((waited + 1))
    done
}

# Steam writes localconfig.vdf over when it exits, so it has to be closed before the file is changed. On SteamOS,
# Desktop Mode's Steam can run as a systemd service that starts it again as soon as it exits; then the service is
# stopped instead, and started again afterwards.
close_steam() {
    steam_running || return 0
    local unit
    unit="$(ps -o unit= -p "$(pgrep -x steam | head -n 1)" 2>/dev/null | tr -d '[:space:]')"
    note "Closing Steam for a moment (it saves its settings as it exits, so it has to be closed first)..."
    steam -shutdown >/dev/null 2>&1 &
    wait_steam_closed 120 || [ -n "$unit" ] || return 1
    # Back by itself?
    sleep 3
    if steam_running; then
        case "$unit" in
            *.service)
                note "Steam started again by itself, so I'm stopping its service ($unit) instead."
                systemctl --user stop "$unit" >/dev/null 2>&1 && wait_steam_closed 60 || return 1
                STEAM_UNIT="$unit" ;;
            *) return 1 ;;
        esac
    fi
    CLOSED_STEAM=1
    ok "Steam closed."
}

reopen_steam() {
    [ "$CLOSED_STEAM" = 1 ] || return 0
    if [ -n "$STEAM_UNIT" ]; then
        systemctl --user start "$STEAM_UNIT" >/dev/null 2>&1 && { ok "Steam is starting again."; return 0; }
    fi
    # On its own session, so closing this window doesn't close Steam too
    if command -v setsid >/dev/null 2>&1; then
        (setsid steam -silent >/dev/null 2>&1 < /dev/null &)
    else
        (nohup steam -silent >/dev/null 2>&1 < /dev/null &)
    fi
    ok "Steam is starting again."
}

# Sets one true/false/null setting in DewDrop's config.json when the file already has it, so the game agrees with
# what was set here. A config.json without it reads the launcher's setting the first time the game starts.
set_config_value() { # name value
    local config="$DEWDROP_DIR/config.json"
    [ -f "$config" ] && grep -q "\"$1\"" "$config" || return 0
    if [ "$DRY_RUN" = 1 ]; then dry "set $1 to $2 in $config"; return 0; fi
    sed -E "s/(\"$1\"[[:space:]]*:[[:space:]]*)(null|true|false)/\\1$2/" "$config" > "$config.tmp" && mv "$config.tmp" "$config"
}

steam_manual_steps() {
    note "To set it yourself: in Steam, open Stardew Valley's Properties, and under General > Launch Options enter:"
    printf '         %s\n' "$1"
}

# Points Stardew Valley's launch option in Steam at DewDrop's launcher (DewDrop's assets/steam-launch.sh, copied to
# the data folder). It starts SMAPI without a terminal window, which Game Mode can't show, and on the DewDrop profile
# when that's wanted, so the game can restart itself in Game Mode and never needs Steam's settings changed again.
setup_steam_launcher() {
    step "Setting up Steam to start the game with SMAPI and DewDrop"
    local data folder script source root config current wanted text backup
    data="$(dewdrop_data)"
    folder="$data/Cache/DewDrop"
    script="$folder/steam-launch.sh"
    source="$DEWDROP_DIR/assets/steam-launch.sh"
    [ -f "$source" ] || source="${SOURCE:+$SOURCE/assets/steam-launch.sh}"
    if [ -z "$source" ] || [ ! -f "$source" ]; then
        if [ "$DRY_RUN" = 1 ]; then source="(DewDrop's assets/steam-launch.sh)"; else
            warn "This DewDrop has no Steam launcher (assets/steam-launch.sh); it needs a newer DewDrop."
            return 1
        fi
    fi

    root="$(linux_steam_root)" || { warn "Couldn't find Steam's folder (Flatpak and Snap Steam aren't supported for this yet)."; return 1; }
    config="$(steam_user_config "$root")"
    [ -n "$config" ] || { warn "Couldn't find your Steam account's settings. Start Steam and sign in once, then run this again."; return 1; }
    current="$(vdf_launch_option get "$config")" || { warn "Steam's settings file has an unexpected layout, so I won't edit it."; return 1; }

    wanted="\"$script\" %command%"
    case "$current" in
        *"$script"*) wanted="$current" ;;
        "") ;;
        *StardewModdingAPI*)
            note "Steam starts SMAPI directly right now: $current"
            note "DewDrop's launcher starts SMAPI too, and can also start it in your profile." ;;
        *%command%*)
            note "Stardew Valley has launch options in Steam already: $current"
            wanted="${current%%%command%*}\"$script\" %command%${current#*%command%}"
            ask_yes_no "Add DewDrop's launcher to them? (they become: $wanted)" y || { steam_manual_steps "\"$script\" %command%"; return 1; } ;;
        *)
            warn "Stardew Valley has launch options in Steam already: $current"
            ask_yes_no "Replace them with DewDrop's launcher?" n || { steam_manual_steps "\"$script\" %command%"; return 1; } ;;
    esac

    # Starting in the profile. The launcher only does it once the profile folder has DewDrop in it, so a profile that
    # hasn't been linked yet still starts with the whole Mods folder (and the Mods button).
    if [ -z "$START_IN_PROFILE" ] && [ -f "$folder/start-in-profile" ]; then
        START_IN_PROFILE=1
    elif [ -z "$START_IN_PROFILE" ]; then
        note "Steam can start the game in your DewDrop profile, so mods you turn off stay off without an Apply & Restart."
        note "(You can change this any time in the game: Mods > Account > \"Start in my profile from Steam\".)"
        if ask_yes_no "Start the game in your DewDrop profile when you press Play in Steam?" y; then START_IN_PROFILE=1; else START_IN_PROFILE=0; fi
    fi

    if [ "$DRY_RUN" = 1 ]; then
        dry "copy $source to $script"
        if [ "$START_IN_PROFILE" = 1 ]; then dry "turn on Start in my profile from Steam"; else dry "turn off Start in my profile from Steam"; fi
        [ "$wanted" = "$current" ] || dry "close Steam, back up $config and set the launch option to: $wanted"
        return 0
    fi

    mkdir -p "$folder" && cp "$source" "$script.tmp" && chmod 755 "$script.tmp" && mv "$script.tmp" "$script" \
        || { warn "Couldn't copy DewDrop's launcher to $script."; return 1; }
    if [ "$START_IN_PROFILE" = 1 ]; then
        printf 'Written by DewDrop: Steam starts the game in your profile while this file is here.\n' > "$folder/start-in-profile"
        set_config_value StartInProfileFromSteam true
    else
        rm -f "$folder/start-in-profile"
        set_config_value StartInProfileFromSteam false
    fi

    if [ "$wanted" = "$current" ]; then
        ok "Steam already starts the game with DewDrop's launcher."
        return 0
    fi
    close_steam || { warn "Steam didn't close."; steam_manual_steps "$wanted"; return 1; }
    # Read again: Steam wrote the file as it closed
    current="$(vdf_launch_option get "$config")" || { warn "Steam's settings file has an unexpected layout, so I won't edit it."; return 1; }
    case "$current" in *"$script"*) wanted="$current" ;; esac
    text="$(DD_OPTION="$wanted" vdf_launch_option set "$config")" \
        && [ "$(printf '%s\n' "$text" | vdf_launch_option get /dev/stdin)" = "$wanted" ] \
        || { warn "The change to Steam's settings didn't check out, so I left them alone."; steam_manual_steps "$wanted"; return 1; }
    backup="$config.dewdrop-backup-$(date +%Y%m%d-%H%M%S)"
    cp "$config" "$backup" && printf '%s\n' "$text" > "$config" || { warn "Couldn't write $config."; return 1; }
    ok "Set the launch option (backup: $backup)"
    note "  $wanted"
}

# Typing a Nexus API key in Game Mode means the on-screen keyboard, so the installer takes it instead. The game reads
# the file at its next start, saves the key like one typed in, and deletes the file.
ask_nexus_key() {
    local data file key
    data="$(dewdrop_data)"
    file="$data/Cache/DewDrop/nexus-key.txt"
    # Already logged in (DewDrop and Stardrop share the login)
    grep -q '"NexusDetails"' "$data/Settings.json" 2>/dev/null && return 0
    key="${DEWDROP_NEXUS_KEY:-}"
    if [ -z "$key" ]; then
        { [ "$YES" = 1 ] || [ ! -t 0 ]; } && return 0
        step "Nexus Mods (optional)"
        note "To install mods from inside the game, DewDrop needs your Nexus Mods API key. Typing it in Game Mode"
        note "is slow, so you can paste it here instead: open https://www.nexusmods.com/users/myaccount?tab=api in"
        note "your browser, copy the Personal API Key at the bottom, then paste it here (Ctrl+Shift+V)."
        printf '    %s? API key (or just press Enter to skip):%s ' "$C_BOLD" "$C_RESET"
        read -r key || key=""
        key="$(printf '%s' "$key" | tr -d '[:space:]')"
        [ -n "$key" ] || { note "Skipped. You can log in later on the Account tab in the game."; return 0; }
    fi
    if [ "$DRY_RUN" = 1 ]; then dry "save the API key for DewDrop to log in with at its next start"; return 0; fi
    mkdir -p "$(dirname "$file")" && (umask 077 && printf '%s\n' "$key" > "$file") \
        && ok "Saved. DewDrop logs in with it the next time the game starts." \
        || warn "Couldn't save the key; log in on the Account tab in the game instead."
}

# ----------------------------------------------------------------------------------------------------------------
# Main

GAME=""
SOURCE=""
SOURCE_VERSION=""
SMAPI_VERSION=""
DEWDROP_DIR=""
CLOSED_STEAM=0
STEAM_UNIT=""
STEAM_LAUNCHER=0
DECK=0
is_steamos && DECK=1

banner
find_game
check_native_game
check_first_run
wait_game_closed
install_smapi
get_source
install_dewdrop

[ -z "$STEAM_SETUP" ] && STEAM_SETUP="$DECK"
if [ "$OS" = "linux" ] && [ "$STEAM_SETUP" = 1 ]; then
    if [ "$(game_store "$GAME")" = "Steam" ]; then
        setup_steam_launcher && STEAM_LAUNCHER=1
    else
        note "Only the Steam version of the game can be set up to start with DewDrop's launcher."
    fi
fi
[ "$DECK" = 1 ] && ask_nexus_key

if [ -f "$HOME/.config/Stardrop/Data/Settings.json" ]; then
    step "Stardrop is installed too"
    note "DewDrop shares Stardrop's profiles and Nexus login. The first time, start the game from Steam or"
    note "the game's own launcher (not from Stardrop) so DewDrop can add itself to your Stardrop profiles."
fi

printf '\n%s  ---------------------------------------------------------------%s\n' "$C_CYAN" "$C_RESET"
printf '   %sAll done!%s\n' "$C_GREEN" "$C_RESET"
printf '   SMAPI %s and DewDrop %s are installed in:\n   %s\n\n' "$SMAPI_VERSION" "$SOURCE_VERSION" "$GAME"
if [ "$DECK" = 1 ]; then
    printf '   %sSwitch back to Game Mode (double-click "Return to Gaming Mode" on the desktop),%s\n' "$C_BOLD" "$C_RESET"
    printf '   %sstart Stardew Valley and press Mods on the title screen.%s\n' "$C_BOLD" "$C_RESET"
    printf '   From now on, installing and switching mods happens in the game: no Desktop Mode needed.\n'
elif [ "$STEAM_LAUNCHER" = 1 ]; then
    printf '   %sStart the game from Steam, then click the new Mods button on the title screen.%s\n' "$C_BOLD" "$C_RESET"
else
    printf '   %sStart the game the way you usually do (SMAPI set itself up to load mods),%s\n' "$C_BOLD" "$C_RESET"
    printf '   %sthen click the new Mods button on the title screen.%s\n' "$C_BOLD" "$C_RESET"
fi
printf '   Guide: https://github.com/%s#readme\n' "$DEWDROP_REPO"
printf '%s  ---------------------------------------------------------------%s\n' "$C_CYAN" "$C_RESET"

# On a Steam Deck the game is for Game Mode; Steam closed for its settings opens again either way
start_default=y
[ "$DECK" = 1 ] && start_default=n
start_question="Start Stardew Valley now?"
[ "$DECK" = 1 ] && start_question="Start Stardew Valley now, here in Desktop Mode?"
if [ "$NO_LAUNCH" = 0 ] && ask_yes_no "$start_question" "$start_default"; then
    [ "$DRY_RUN" = 1 ] || reopen_steam
    launch_game
else
    [ "$DRY_RUN" = 1 ] || reopen_steam
fi

finish 0
