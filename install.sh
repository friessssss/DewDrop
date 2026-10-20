#!/usr/bin/env bash
#
# One-line DewDrop installer for macOS and Linux. Paste this into Terminal:
#
#   curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash
#
# It downloads the real installer (installer/internal/install.sh) for your system and runs it, which then
# downloads the latest DewDrop release. Options go after "bash -s --", for example:
#
#   curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash -s -- --dry-run
#
# Nothing has to be allowed in Privacy & Security: macOS only blocks files opened from Finder.
#
# Written for bash 3.2, which is what macOS ships.

# Everything is inside main, so a download cut off halfway runs nothing.
main() {
    set -u
    local repo="friessssss/DewDrop" script runner dir
    # DEWDROP_INSTALLER_BASE points at another copy of installer/internal (the tests use a local folder).
    local base="${DEWDROP_INSTALLER_BASE:-https://raw.githubusercontent.com/$repo/master/installer/internal}"

    case "$(uname -s)" in
        Darwin|Linux) script="install.sh" ;;
        MINGW*|MSYS*|CYGWIN*) script="install.ps1" ;;
        *) echo "DewDrop's installer runs on macOS, Linux and Windows. See https://github.com/$repo#install"; exit 1 ;;
    esac

    dir="$(mktemp -d "${TMPDIR:-/tmp}/dewdrop-get.XXXXXX")" || { echo "Couldn't make a temporary folder."; exit 1; }
    # Expanded now: $dir is local to main and gone by the time the script exits.
    trap "rm -rf '$dir'" EXIT

    # The shell writes the file rather than curl -o: Ubuntu's snap curl can't see every folder.
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL --retry 2 "$base/$script" > "$dir/$script"
    elif command -v wget >/dev/null 2>&1; then
        wget -q -O - "$base/$script" > "$dir/$script"
    else
        false
    fi || { echo "Couldn't download the DewDrop installer from $base/$script. Check your internet connection."; exit 1; }

    # Piped into bash, stdin is this script rather than the keyboard, so the installer reads its answers from the
    # terminal instead. Only the installer's stdin changes: bash is still reading the rest of this script from the
    # pipe, so with no terminal at all (CI, a script) the installer gets no input and takes the recommended answers.
    local input="/dev/null"
    if [ -t 0 ]; then
        input="/dev/stdin"
    elif (: < /dev/tty) 2>/dev/null; then
        input="/dev/tty"
    fi

    if [ "$script" = "install.ps1" ]; then
        command -v cygpath >/dev/null 2>&1 && runner="$(cygpath -w "$dir/$script")" || runner="$dir/$script"
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$runner" "$@" < "$input"
    else
        bash "$dir/$script" "$@" < "$input"
    fi
}

main "$@"
