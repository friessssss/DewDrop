#!/usr/bin/env bash
#
# DewDrop installer for macOS and Linux.
#
# Finds Stardew Valley, installs or updates SMAPI with SMAPI's own installer if needed, then copies DewDrop into
# the Mods folder (keeping your DewDrop settings when updating). It never deletes your mods, saves or settings: an
# old DewDrop folder is only replaced once the new copy is in place.
#
# Usage: install.sh [--game-path DIR] [--mod-source DIR_OR_ZIP] [--yes] [--dry-run] [--skip-smapi] [--no-launch]
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
        -h|--help) sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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
    wait "$pid" 2>/dev/null
    printf '\n%s==> Back in the DewDrop installer%s\n' "$C_CYAN" "$C_RESET"

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
    existing="$(find_installed "$mods")"
    if [ -n "$existing" ]; then
        keep="$(printf '%s\n' "$existing" | grep -Fx "$target" | head -n 1)"
        [ -n "$keep" ] || keep="$(printf '%s\n' "$existing" | head -n 1)"
        target="$keep"
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
# Main

GAME=""
SOURCE=""
SOURCE_VERSION=""
SMAPI_VERSION=""

banner
find_game
check_native_game
check_first_run
wait_game_closed
install_smapi
get_source
install_dewdrop

if [ -f "$HOME/.config/Stardrop/Data/Settings.json" ]; then
    step "Stardrop is installed too"
    note "DewDrop shares Stardrop's profiles and Nexus login. The first time, start the game from Steam or"
    note "the game's own launcher (not from Stardrop) so DewDrop can add itself to your Stardrop profiles."
fi

printf '\n%s  ---------------------------------------------------------------%s\n' "$C_CYAN" "$C_RESET"
printf '   %sAll done!%s\n' "$C_GREEN" "$C_RESET"
printf '   SMAPI %s and DewDrop %s are installed in:\n   %s\n\n' "$SMAPI_VERSION" "$SOURCE_VERSION" "$GAME"
printf '   %sStart the game the way you usually do (SMAPI set itself up to load mods),%s\n' "$C_BOLD" "$C_RESET"
printf '   %sthen click the new Mods button on the title screen.%s\n' "$C_BOLD" "$C_RESET"
printf '   Guide: https://github.com/%s#readme\n' "$DEWDROP_REPO"
printf '%s  ---------------------------------------------------------------%s\n' "$C_CYAN" "$C_RESET"

if [ "$NO_LAUNCH" = 0 ] && ask_yes_no "Start Stardew Valley now?" y; then
    launch_game
fi

finish 0
